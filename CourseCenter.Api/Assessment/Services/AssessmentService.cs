using CourseCenter.Api.Assessment.DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CourseCenter.Api.Assessment.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly ApplicationDbContext _context;

        public AssessmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public SubmitAssessmentResult SubmitAssessment(
      int attemptId,
      SubmitAssessmentDto dto)
        {
            if (dto == null || dto.Answers == null || !dto.Answers.Any())
                throw new InvalidOperationException("Answers are required");

            var attempt = _context.AssessmentAttempts
                .FirstOrDefault(x => x.Id == attemptId);

            if (attempt == null)
                throw new KeyNotFoundException("Assessment attempt not found");

            if (attempt.SubmittedAt != null)
                throw new InvalidOperationException("Assessment already submitted");

            var questionIds = dto.Answers.Select(a => a.QuestionId).ToList();
            if (questionIds.Count != questionIds.Distinct().Count())
                throw new InvalidOperationException("Duplicate answers for the same question");

            var answerIds = dto.Answers.Select(a => a.AnswerId).ToList();

            var validAnswers = _context.AssessmentAnswers
                .Include(a => a.Question)
                .Where(a =>
                    answerIds.Contains(a.Id) &&
                    questionIds.Contains(a.QuestionId) &&
                    a.Question.TestId == attempt.TestId
                )
                .ToList();

            if (validAnswers.Count != dto.Answers.Count)
                throw new InvalidOperationException("Invalid answers detected");

            int totalScore = validAnswers.Sum(a => a.Score);

            foreach (var answer in validAnswers)
            {
                _context.AssessmentAttemptAnswers.Add(new AssessmentAttemptAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = answer.QuestionId,
                    AnswerId = answer.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            attempt.TotalScore = totalScore;
            attempt.SubmittedAt = DateTime.UtcNow;

            var result = _context.AssessmentResultRanges
                .FirstOrDefault(r =>
                    r.TestId == attempt.TestId &&
                    totalScore >= r.FromScore &&
                    totalScore <= r.ToScore
                );

            attempt.ResultLabel = result?.ResultLabel;

            _context.SaveChanges();

            return new SubmitAssessmentResult
            {
                AttemptId = attempt.Id,
                TotalScore = totalScore,
                Result = attempt.ResultLabel
            };
        }

    }
}
