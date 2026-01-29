namespace CourseCenter.Api.Students
{
    public class ArchivedStudent
    {
        public int Id { get; set; }

        public int OriginalStudentId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalId { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ArchivedAt { get; set; }
        public int ArchivedByUserId { get; set; }
        public string? RelativeName { get; set; }
        public string? ParentPhoneNumber { get; set; }

        // Academic Info
        public string? Level { get; set; }
    }

}
