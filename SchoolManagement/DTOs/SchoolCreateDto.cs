namespace SchoolManagement.DTOs
{
    public class SchoolCreateDto
    {
        public string? SchoolName { get; set; }

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
    }
}
