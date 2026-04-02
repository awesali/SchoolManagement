using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagement.Model
{
    [Table("SubjectTeachers")]
    public class SubjectTeachers
    {
        [Key]
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int StaffId { get; set; }
        public int SchoolId { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? Modified_Date { get; set; }
        public int? Created_By { get; set; }
        public int? Updated_By { get; set; }
        public bool IsActive { get; set; }
    }
}
