namespace SchoolManagement.DTOs;

public class PromotionStudentsQuery
{
    public int SchoolId { get; set; }
    public int CurrentSessionId { get; set; }
    public int ClassId { get; set; }
    public int SectionId { get; set; }
}

public class PromotionStudentDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string AdmissionNo { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Result { get; set; } = "Not Available";
    public string SuggestedAction { get; set; } = "Promote";
}

public class PromoteStudentsRequest
{
    public int SchoolId { get; set; }
    public int CurrentSessionId { get; set; }
    public int NextSessionId { get; set; }
    public bool AssignFees { get; set; }
    public bool CopyTransport { get; set; }
    public bool CopyBusRoute { get; set; }
    public bool AssignBookKit { get; set; }
    public bool AssignUniformKit { get; set; }
    public bool GenerateRollNumbers { get; set; }
    public bool CarryOptionalSettings { get; set; }
    public List<PromotionStudentRequest> Students { get; set; } = new();
}

public class PromotionStudentRequest
{
    public int EnrollmentId { get; set; }
    public string Action { get; set; } = "Promote";
    public int? ToClassId { get; set; }
    public int? ToSectionId { get; set; }
    public string? RollNumber { get; set; }
    public string? Remarks { get; set; }
}
