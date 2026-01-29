namespace CourseCenter.Api.Students
{
    public class Student
    {
        public int Id { get; set; }

        // Student Info
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
        public string? Level { get; set; } // مثال: Grade 3 – Beginner – Level A

        // System Info
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
