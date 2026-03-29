namespace SchoolManagement.Model
{
    public class ParentDetails
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Relationship { get; set; } // Father, Mother, Guardian

        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime? Modified_Date { get; set; }

        public int Created_By { get; set; }
        public int? Updated_By { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
