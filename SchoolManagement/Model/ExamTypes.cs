namespace SchoolManagement.Model
{
    public class ExamTypes
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }

        public int schoolId { get; set; }

    }
}
