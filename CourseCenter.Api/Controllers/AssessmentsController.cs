using CourseCenter.Api.Assessment;
using CourseCenter.Api.Assessment.DTO;
using CourseCenter.Api.Assessment.Services;
using CourseCenter.Api.Leads;
using CourseCenter.Api.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/assessments")]
    public class AssessmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssessmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Start Assessment
        // =========================
        [HttpPost("tests/{testId}/start")]
        [AllowAnonymous]
        public IActionResult StartAssessment(int testId, StartAssessmentDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request");

            if (string.IsNullOrEmpty(dto.Email) && string.IsNullOrEmpty(dto.Phone))
                return BadRequest("Email or Phone is required");

            var test = _context.AssessmentTests
                .FirstOrDefault(t => t.Id == testId && t.IsActive);

            if (test == null)
                return NotFound("Test not found or inactive");

            Student student = null;
            Lead lead = null;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                student = _context.Students.FirstOrDefault(x => x.Email == dto.Email);
                lead = _context.Leads.FirstOrDefault(x => x.Email == dto.Email);
            }

            if (student == null && lead == null && !string.IsNullOrEmpty(dto.Phone))
            {
                student = _context.Students.FirstOrDefault(x => x.PhoneNumber == dto.Phone);
                lead = _context.Leads.FirstOrDefault(x => x.Phone == dto.Phone);
            }

            if (student == null && lead == null)
            {
                if (string.IsNullOrEmpty(dto.FullName))
                    return BadRequest("FullName is required");

                lead = new Lead
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Source = "Assessment"

                };

                _context.Leads.Add(lead);
                _context.SaveChanges();
            }

            var attempt = new AssessmentAttempt
            {
                TestId = testId,
                LeadId = lead?.Id,
                StudentId = student?.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.AssessmentAttempts.Add(attempt);
            _context.SaveChanges();

            return Ok(new
            {
                attemptId = attempt.Id,
                testId,
                startedAt = attempt.CreatedAt,
                expiresAt = attempt.CreatedAt.AddMinutes(test.DurationMinutes)
            });
        }

        // =========================
        // Submit Assessment
        // =========================
        [HttpPost("attempts/{attemptId}/submit")]
        [AllowAnonymous]
        public IActionResult SubmitAssessment(
            int attemptId,
            SubmitAssessmentDto dto,
            [FromServices] IAssessmentService assessmentService)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = assessmentService.SubmitAssessment(attemptId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // Admin: Create Test
        // =========================
        [HttpPost("tests")]
        [Authorize(Policy = "ASSESSMENTS_TESTS_CREATE")]
        public IActionResult CreateTest(CreateAssessmentTestDto dto)
        {
            var test = new AssessmentTest
            {
                Name = dto.Name,
                Description = dto.Description,
                DurationMinutes = dto.DurationMinutes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AssessmentTests.Add(test);
            _context.SaveChanges();

            return Ok(test);
        }

        // =========================
        // Admin: Add Question
        // =========================
        [HttpPost("tests/{testId}/questions")]
        [Authorize(Policy = "ASSESSMENTS_QUESTIONS_CREATE")]
        public IActionResult AddQuestion(int testId, CreateQuestionDto dto)
        {
            var testExists = _context.AssessmentTests.Any(t => t.Id == testId);
            if (!testExists)
                return NotFound("Test not found");

            var question = new AssessmentQuestion
            {
                TestId = testId,
                Text = dto.Text,
                Type = dto.Type,   
                Order = dto.Order
            };

            _context.AssessmentQuestions.Add(question);
            _context.SaveChanges();

            return Ok(question);
        }

        // =========================
        // Admin: Add Answer
        // =========================
        [HttpPost("questions/{questionId}/answers")]
        [Authorize(Policy = "ASSESSMENTS_ANSWERS_CREATE")]
        public IActionResult AddAnswer(int questionId, CreateAnswerDto dto)
        {
            var questionExists = _context.AssessmentQuestions.Any(q => q.Id == questionId);
            if (!questionExists)
                return NotFound("Question not found");

            var answer = new AssessmentAnswer
            {
                QuestionId = questionId,
                Text = dto.Text,
                Score = dto.Score
            };

            _context.AssessmentAnswers.Add(answer);
            _context.SaveChanges();

            return Ok(new
            {
                answer.Id,
                answer.QuestionId,
                answer.Text
            });
        }

        // =========================
        // GET: Active Tests
        // =========================
        [HttpGet("tests")]
        [AllowAnonymous]
        public IActionResult GetActiveTests()
        {
            return Ok(_context.AssessmentTests
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.DurationMinutes
                })
                .ToList());
        }

        // =========================
        // GET: Test with Questions
        // =========================
        [HttpGet("tests/{testId}")]
        [AllowAnonymous]
        public IActionResult GetTestWithQuestions(int testId)
        {
            var test = _context.AssessmentTests
                .Where(t => t.Id == testId && t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    Questions = _context.AssessmentQuestions
                        .Where(q => q.TestId == t.Id)
                        .OrderBy(q => q.Order)
                        .Select(q => new
                        {
                            q.Id,
                            q.Text,
                            type = q.Type.ToString(),
                            Answers = _context.AssessmentAnswers
                                .Where(a => a.QuestionId == q.Id)
                                .Select(a => new
                                {
                                    a.Id,
                                    a.Text
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (test == null)
                return NotFound("Test not found");

            return Ok(test);
        }

        // =========================
        // GET: Test Questions
        // =========================
        [HttpGet("tests/{testId}/questions")]
        [AllowAnonymous]
        public IActionResult GetTestQuestions(int testId)
        {
            return Ok(_context.AssessmentQuestions
                .Where(q => q.TestId == testId)
                .OrderBy(q => q.Order)
                .Select(q => new
                {
                    q.Id,
                    q.Text,
                    type = q.Type.ToString(),
                    q.Order
                })
                .ToList());
        }

        // =========================
        // GET: Question Answers
        // =========================
        [HttpGet("questions/{questionId}/answers")]
        [AllowAnonymous]
        public IActionResult GetQuestionAnswers(int questionId)
        {
            return Ok(_context.AssessmentAnswers
                .Where(a => a.QuestionId == questionId)
                .Select(a => new
                {
                    a.Id,
                    a.Text
                })
                .ToList());
        }

        // =========================
        // GET: Question with Answers
        // =========================
        [HttpGet("questions/{questionId}")]
        [AllowAnonymous]
        public IActionResult GetQuestionWithAnswers(int questionId)
        {
            var question = _context.AssessmentQuestions
                .Where(q => q.Id == questionId)
                .Select(q => new
                {
                    q.Id,
                    q.Text,
                    type = q.Type.ToString(),
                    Answers = _context.AssessmentAnswers
                        .Where(a => a.QuestionId == q.Id)
                        .Select(a => new
                        {
                            a.Id,
                            a.Text
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (question == null)
                return NotFound("Question not found");

            return Ok(question);
        }

        // =========================
        // GET: Attempt Result
        // =========================
        [HttpGet("attempts/{attemptId}")]
        [AllowAnonymous]
        public IActionResult GetAttemptResult(int attemptId)
        {
            var attempt = _context.AssessmentAttempts
                .Where(a => a.Id == attemptId)
                .Select(a => new
                {
                    a.Id,
                    a.TestId,
                    a.TotalScore,
                    a.ResultLabel,
                    a.SubmittedAt
                })
                .FirstOrDefault();

            if (attempt == null)
                return NotFound("Attempt not found");

            return Ok(attempt);
        }

        // =========================
        // GET: Attempt Details (Admin)
        // =========================
        [HttpGet("attempts/{attemptId}/details")]
        [Authorize(Policy = "ASSESSMENTS_ATTEMPTS_VIEW")]
        public IActionResult GetAttemptDetails(int attemptId)
        {
            var attempt = _context.AssessmentAttempts
                .Where(a => a.Id == attemptId)
                .Select(a => new
                {
                    a.Id,
                    a.TestId,
                    a.TotalScore,
                    a.ResultLabel,
                    a.SubmittedAt,
                    Answers = _context.AssessmentAttemptAnswers
                        .Where(aa => aa.AttemptId == a.Id)
                        .Select(aa => new
                        {
                            aa.QuestionId,
                            QuestionText = _context.AssessmentQuestions
                                .Where(q => q.Id == aa.QuestionId)
                                .Select(q => q.Text)
                                .FirstOrDefault(),
                            AnswerId = aa.AnswerId,
                            AnswerText = _context.AssessmentAnswers
                                .Where(ans => ans.Id == aa.AnswerId)
                                .Select(ans => ans.Text)
                                .FirstOrDefault()
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (attempt == null)
                return NotFound("Attempt not found");

            return Ok(attempt);
        }

        // =========================
        // GET: Test Attempts (Admin)
        // =========================
        [HttpGet("tests/{testId}/attempts")]
        [Authorize(Policy = "ASSESSMENTS_ATTEMPTS_VIEW")]
        public IActionResult GetTestAttempts(int testId)
        {
            return Ok(_context.AssessmentAttempts
                .Where(a => a.TestId == testId && a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .Select(a => new
                {
                    a.Id,
                    a.StudentId,
                    a.LeadId,
                    a.TotalScore,
                    a.ResultLabel,
                    a.SubmittedAt
                })
                .ToList());
        }

        // =========================
        // GET: Result Ranges
        // =========================
        [HttpGet("tests/{testId}/result-ranges")]
        [Authorize(Policy = "ASSESSMENTS_RESULT_RANGES_VIEW")]
        public IActionResult GetResultRanges(int testId)
        {
            return Ok(_context.AssessmentResultRanges
                .Where(r => r.TestId == testId)
                .OrderBy(r => r.FromScore)
                .Select(r => new
                {
                    r.Id,
                    r.FromScore,
                    r.ToScore,
                    r.ResultLabel
                })
                .ToList());
        }

        // =========================
        // GET: Lead Assessment History
        // =========================
        [HttpGet("leads/{leadId}")]
        [Authorize(Policy = "ASSESSMENTS_ATTEMPTS_VIEW")]
        public IActionResult GetLeadAssessmentHistory(int leadId)
        {
            return Ok(_context.AssessmentAttempts
                .Where(x => x.LeadId == leadId && x.SubmittedAt != null)
                .Select(x => new
                {
                    x.Id,
                    x.TestId,
                    x.TotalScore,
                    x.ResultLabel,
                    x.SubmittedAt
                })
                .ToList());
        }
        // =========================
        // Admin: Add Result Range
        // =========================
        [HttpPost("tests/{testId}/result-ranges")]
        [Authorize(Policy = "ASSESSMENTS_RESULT_RANGES_CREATE")]
        public IActionResult AddResultRange(int testId, CreateResultRangeDto dto)
        {
            var testExists = _context.AssessmentTests.Any(t => t.Id == testId);
            if (!testExists)
                return NotFound("Test not found");

            if (dto.FromScore > dto.ToScore)
                return BadRequest("FromScore cannot be greater than ToScore");

            var range = new AssessmentResultRange
            {
                TestId = testId,
                FromScore = dto.FromScore,
                ToScore = dto.ToScore,
                ResultLabel = dto.ResultLabel
            };

            _context.AssessmentResultRanges.Add(range);
            _context.SaveChanges();

            return Ok(new
            {
                range.Id,
                range.TestId,
                range.FromScore,
                range.ToScore,
                range.ResultLabel
            });
        }
        // =========================
        // Admin: Generate Public Link
        // =========================
        [HttpPost("tests/{testId}/public-link")]
        [Authorize(Policy = "ASSESSMENTS_TESTS_MANAGE")]
        public IActionResult GeneratePublicLink(int testId)
        {
            var test = _context.AssessmentTests.FirstOrDefault(t => t.Id == testId);
            if (test == null)
                return NotFound("Test not found");

            test.PublicKey = Guid.NewGuid().ToString();
            test.IsPublic = true;

            _context.SaveChanges();

            return Ok(new
            {
                testId = test.Id,
                publicUrl = $"https://crmcourses.vercel.app/assessments/start/{test.PublicKey}"
            });
        }
        // =========================
        // Public: Get Test By Public Link
        // =========================
        [HttpGet("public/{publicKey}")]
        [AllowAnonymous]
        public IActionResult GetPublicTest(string publicKey)
        {
            var test = _context.AssessmentTests
                .Where(t => t.PublicKey == publicKey && t.IsPublic)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    Questions = _context.AssessmentQuestions
                        .Where(q => q.TestId == t.Id)
                        .OrderBy(q => q.Order)
                        .Select(q => new
                        {
                            q.Id,
                            q.Text,
                            type = q.Type.ToString(),
                            Answers = _context.AssessmentAnswers
                                .Where(a => a.QuestionId == q.Id)
                                .Select(a => new { a.Id, a.Text })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (test == null)
                return NotFound("Invalid or inactive public link");

            return Ok(test);
        }
        [HttpPost("public/{publicKey}/start")]
        [AllowAnonymous]
        public IActionResult StartPublicAssessment(
            string publicKey,
            StartAssessmentDto dto)
        {
            var test = _context.AssessmentTests
                .FirstOrDefault(t => t.PublicKey == publicKey && t.IsPublic);

            if (test == null)
                return NotFound("Invalid public link");

            // نفس منطق StartAssessment
            return StartAssessment(test.Id, dto);
        }
        [HttpPut("tests/{testId}")]
        [Authorize(Policy = "ASSESSMENTS_TESTS_EDIT")]
        public IActionResult UpdateTest(int testId, CreateAssessmentTestDto dto)
        {
            var test = _context.AssessmentTests.FirstOrDefault(t => t.Id == testId);
            if (test == null)
                return NotFound("Test not found");

            test.Name = dto.Name;
            test.Description = dto.Description;
            test.DurationMinutes = dto.DurationMinutes;

            _context.SaveChanges();

            return Ok(test);
        }
        [HttpPut("questions/{questionId}")]
        [Authorize(Policy = "ASSESSMENTS_QUESTIONS_EDIT")]
        public IActionResult UpdateQuestion(int questionId, CreateQuestionDto dto)
        {
            var question = _context.AssessmentQuestions
                .FirstOrDefault(q => q.Id == questionId);

            if (question == null)
                return NotFound("Question not found");

            question.Text = dto.Text;
            question.Type = dto.Type;
            question.Order = dto.Order;

            _context.SaveChanges();

            return Ok(question);
        }
        [HttpPut("answers/{answerId}")]
        [Authorize(Policy = "ASSESSMENTS_ANSWERS_EDIT")]
        public IActionResult UpdateAnswer(int answerId, CreateAnswerDto dto)
        {
            var answer = _context.AssessmentAnswers
                .FirstOrDefault(a => a.Id == answerId);

            if (answer == null)
                return NotFound("Answer not found");

            answer.Text = dto.Text;
            answer.Score = dto.Score;

            _context.SaveChanges();

            return Ok(answer);
        }
        [HttpPut("result-ranges/{rangeId}")]
        [Authorize(Policy = "ASSESSMENTS_RESULT_RANGES_EDIT")]
        public IActionResult UpdateResultRange(int rangeId, CreateResultRangeDto dto)
        {
            var range = _context.AssessmentResultRanges
                .FirstOrDefault(r => r.Id == rangeId);

            if (range == null)
                return NotFound("Result range not found");

            if (dto.FromScore > dto.ToScore)
                return BadRequest("FromScore cannot be greater than ToScore");

            range.FromScore = dto.FromScore;
            range.ToScore = dto.ToScore;
            range.ResultLabel = dto.ResultLabel;

            _context.SaveChanges();

            return Ok(range);
        }
        [HttpPut("tests/{testId}/toggle")]
        [Authorize(Policy = "ASSESSMENTS_TESTS_EDIT")]
        public IActionResult ToggleTest(int testId)
        {
            var test = _context.AssessmentTests.FirstOrDefault(t => t.Id == testId);
            if (test == null)
                return NotFound("Test not found");

            test.IsActive = !test.IsActive;
            _context.SaveChanges();

            return Ok(new
            {
                test.Id,
                test.IsActive
            });
        }


    }
}
