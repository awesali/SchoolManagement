namespace SchoolManagement.Model
{
    public class Exams
    {
 public int Id { get; set; }

    public string Name { get; set; }

    public int ExamTypeId { get; set; }

    public int SchoolId { get; set; }
    public int AcademicSessionId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsPublished { get; set; }

    public bool ResultPublished { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public bool IsActive { get; set; }
    }
}
