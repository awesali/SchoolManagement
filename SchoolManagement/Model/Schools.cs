namespace SchoolManagement.Model
{
    public class Schools
    {
        public int Id { get; set; }

        public string SchoolName { get; set; }

        public string Address { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? PinCode { get; set; }

        public string? Country { get; set; }

        public string? State { get; set; }

        public string? Landmark { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? SuperAdminId { get; set; }

        public DateTime Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
    }
}
