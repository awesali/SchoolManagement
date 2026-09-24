using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class StudentDiscussionThread
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SectionId { get; set; }
    public int StaffId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class StudentDiscussionPost
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ThreadId { get; set; }
    public int? StudentId { get; set; }
    public int? StaffId { get; set; }
    [MaxLength(2000)] public string Body { get; set; } = "";
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class SchoolClub
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(200)] public string? Schedule { get; set; }
    [MaxLength(120)] public string? Coordinator { get; set; }
    public bool IsActive { get; set; } = true;
}
public class StudentClubMembership
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ClubId { get; set; }
    public int StudentId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class StudentEventRegistration
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class SchoolLostFoundPost
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    [MaxLength(10)] public string Kind { get; set; } = "Lost";
    [MaxLength(160)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Description { get; set; } = "";
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class SchoolHouse
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;
}
public class StudentHouseMembership
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int HouseId { get; set; }
    public bool IsActive { get; set; } = true;
}
public class StudentTransportAlert
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int? StudentId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Message { get; set; } = "";
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class StudentIdentityToken
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    [MaxLength(64)] public string Token { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class StudentLibraryReservation
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int BookId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
