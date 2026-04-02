using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagement.Model
{
    [Table("Subjects")]
    public class Subjects
    {
        [Key]
        public int Id { get; set; }
        public string SubjectName { get; set; }
        public int SchoolId { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? Modified_Date { get; set; }
        public bool IsActive { get; set; }
    }
}
