namespace CourseCenter.Api.Students.Dtos
{
    public class CreateStudentRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string? NationalId { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }

        // Parents Info
        public string? RelativeName { get; set; }
        public string? ParentPhoneNumber { get; set; }

        // Academic Info
        public string? Level { get; set; }
    }
}
