namespace SchoolManagement.Model
{
    public class FeeType
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SchoolId { get; set; }

        public bool IsActive { get; set; }

        // Navigation Property
        public virtual ICollection<StudentFee> StudentFees { get; set; }
    }
}
