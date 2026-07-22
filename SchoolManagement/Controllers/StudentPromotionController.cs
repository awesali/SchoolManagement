using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/student-promotion")]
public class StudentPromotionController : ControllerBase
{
    private static readonly HashSet<string> Actions = new(StringComparer.OrdinalIgnoreCase)
        { "Promote", "Repeat", "Transfer", "Left" };
    private readonly AppDbContext _db;
    public StudentPromotionController(AppDbContext db) => _db = db;
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private bool CanAccessSchool(int schoolId)
    {
        if (User.FindFirstValue("RoleId") == "1") return true;
        return int.TryParse(User.FindFirstValue("SchoolId"), out var claimSchool) && claimSchool == schoolId;
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students([FromQuery] PromotionStudentsQuery request)
    {
        if (!CanAccessSchool(request.SchoolId)) return Forbid();
        var sessionExists = await _db.AcademicSessions.AnyAsync(x => x.Id == request.CurrentSessionId && x.SchoolId == request.SchoolId);
        if (!sessionExists) return BadRequest(new { success = false, message = "Current academic session was not found." });

        var data = await (from enrollment in _db.StudentEnrollment.AsNoTracking()
                          join student in _db.Students.AsNoTracking() on enrollment.StudentId equals student.Id
                          join schoolClass in _db.Classes.AsNoTracking() on enrollment.ClassId equals schoolClass.Id
                          join section in _db.SectionDetails.AsNoTracking() on enrollment.SectionId equals section.Id
                          where enrollment.SchoolId == request.SchoolId && enrollment.SessionId == request.CurrentSessionId
                             && enrollment.ClassId == request.ClassId && enrollment.SectionId == request.SectionId
                             && enrollment.IsActive && enrollment.EnrollmentStatus == "Active" && student.IsActive
                          let result = _db.ExamResults.Where(x => x.EnrollmentId == enrollment.Id && x.Published)
                              .OrderByDescending(x => x.Created_Date).Select(x => x.ResultStatus).FirstOrDefault()
                          orderby student.StudentName
                          select new PromotionStudentDto
                          {
                              EnrollmentId = enrollment.Id,
                              StudentId = student.Id,
                              AdmissionNo = enrollment.RollNumber ?? student.Rollnumber ?? $"STU{student.Id:D5}",
                              StudentName = student.StudentName,
                              ClassName = schoolClass.ClassName,
                              SectionName = section.SectionName,
                              Result = result ?? "Not Available",
                              SuggestedAction = result == "FAIL" ? "Repeat" : "Promote"
                          }).ToListAsync();
        return Ok(new { success = true, data });
    }

    [HttpPost("promote")]
    public async Task<IActionResult> Promote(PromoteStudentsRequest request)
    {
        if (!CanAccessSchool(request.SchoolId)) return Forbid();
        if (request.CurrentSessionId == request.NextSessionId)
            return BadRequest(new { success = false, message = "Current and next sessions must be different." });
        if (request.Students.Count == 0)
            return BadRequest(new { success = false, message = "Select at least one student." });
        if (request.Students.Any(x => !Actions.Contains(x.Action)))
            return BadRequest(new { success = false, message = "Action must be Promote, Repeat, Transfer, or Left." });

        var sessions = await _db.AcademicSessions.Where(x => x.SchoolId == request.SchoolId &&
            (x.Id == request.CurrentSessionId || x.Id == request.NextSessionId)).ToListAsync();
        var currentSession = sessions.FirstOrDefault(x => x.Id == request.CurrentSessionId);
        var nextSession = sessions.FirstOrDefault(x => x.Id == request.NextSessionId);
        if (currentSession == null || nextSession == null)
            return BadRequest(new { success = false, message = "Both academic sessions must exist in the selected school." });
        if (nextSession.Year_Start <= currentSession.Year_Start)
            return BadRequest(new { success = false, message = "Next session must start after the current session." });

        var requestedIds = request.Students.Select(x => x.EnrollmentId).Distinct().ToList();
        if (requestedIds.Count != request.Students.Count)
            return BadRequest(new { success = false, message = "The same enrollment cannot be processed twice." });
        var sources = await _db.StudentEnrollment.Where(x => requestedIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var warnings = new List<string>();

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var created = 0; var transferred = 0; var left = 0; var repeated = 0;
            foreach (var item in request.Students)
            {
                if (!sources.TryGetValue(item.EnrollmentId, out var source) || source.SchoolId != request.SchoolId ||
                    source.SessionId != request.CurrentSessionId || !source.IsActive || source.EnrollmentStatus != "Active")
                    throw new InvalidOperationException($"Enrollment {item.EnrollmentId} is not an active enrollment in the current session.");

                var action = char.ToUpperInvariant(item.Action[0]) + item.Action[1..].ToLowerInvariant();
                if (action is "Transfer" or "Left")
                {
                    source.IsActive = false;
                    source.EnrollmentStatus = action == "Transfer" ? "Transferred" : "Left";
                    source.PromotionStatus = action;
                    source.Updated_By = UserId; source.Updated_Date = DateTime.UtcNow;
                    AddHistory(source, null, action, item.Remarks);
                    if (action == "Transfer") transferred++; else left++;
                    continue;
                }

                var toClassId = action == "Repeat" ? (item.ToClassId ?? source.ClassId) : item.ToClassId;
                var toSectionId = item.ToSectionId;
                if (!toClassId.HasValue || !toSectionId.HasValue)
                    throw new InvalidOperationException($"Destination class and section are required for enrollment {source.Id}.");
                var validSection = await _db.SectionDetails.AnyAsync(x => x.Id == toSectionId && x.ClassId == toClassId && x.SchoolId == request.SchoolId);
                if (!validSection) throw new InvalidOperationException($"Destination class/section is invalid for enrollment {source.Id}.");
                if (await _db.StudentEnrollment.AnyAsync(x => x.StudentId == source.StudentId && x.SessionId == request.NextSessionId))
                    throw new InvalidOperationException($"Student {source.StudentId} already has an enrollment in the next session.");

                var roll = item.RollNumber;
                if (request.GenerateRollNumbers && string.IsNullOrWhiteSpace(roll))
                    roll = await NextRollNumber(request.SchoolId, request.NextSessionId, toClassId.Value, toSectionId.Value);
                var destination = new StudentEnrollment
                {
                    StudentId = source.StudentId, SchoolId = source.SchoolId, SessionId = request.NextSessionId,
                    ClassId = toClassId.Value, SectionId = toSectionId.Value, RollNumber = roll,
                    AdmissionType = action == "Repeat" ? "Repeat" : "Promoted",
                    EnrollmentStatus = "Active", PromotionStatus = "NotProcessed", EnrollmentDate = DateTime.UtcNow,
                    Created_At = DateTime.UtcNow, Created_By = UserId, Updated_By = UserId, IsActive = true
                };
                _db.StudentEnrollment.Add(destination);
                await _db.SaveChangesAsync();

                source.IsActive = false; source.EnrollmentStatus = "Completed";
                source.PromotionStatus = action == "Repeat" ? "Repeated" : "Promoted";
                source.Updated_By = UserId; source.Updated_Date = DateTime.UtcNow;
                AddHistory(source, destination, action, item.Remarks);

                if (request.AssignFees) await CopyFees(source, destination);
                if (request.CopyTransport || request.CopyBusRoute) await CopyTransport(source, destination, warnings);
                if (request.AssignBookKit) await AssignKit(destination, "Book", warnings);
                if (request.AssignUniformKit) await AssignKit(destination, "Uniform", warnings);
                if (request.CarryOptionalSettings)
                    warnings.Add($"Student {source.StudentId}: profile and parent data are already shared; no separate optional-settings table exists.");
                created++; if (action == "Repeat") repeated++;
            }
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { success = true, message = "Student promotion completed successfully.", data = new { processed = request.Students.Count, newEnrollments = created, repeated, transferred, left, warnings = warnings.Distinct() } });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(int schoolId, int? sessionId = null)
    {
        if (!CanAccessSchool(schoolId)) return Forbid();
        var query = from promotion in _db.StudentPromotions.AsNoTracking()
                    join student in _db.Students.AsNoTracking() on promotion.StudentId equals student.Id
                    join fromSession in _db.AcademicSessions.AsNoTracking() on promotion.FromSessionId equals fromSession.Id
                    join fromClass in _db.Classes.AsNoTracking() on promotion.FromClassId equals fromClass.Id
                    join toSession0 in _db.AcademicSessions.AsNoTracking() on promotion.ToSessionId equals toSession0.Id into toSessions
                    from toSession in toSessions.DefaultIfEmpty()
                    join toClass0 in _db.Classes.AsNoTracking() on promotion.ToClassId equals toClass0.Id into toClasses
                    from toClass in toClasses.DefaultIfEmpty()
                    where promotion.SchoolId == schoolId && (!sessionId.HasValue || promotion.FromSessionId == sessionId)
                    orderby promotion.PromotionDate descending
                    select new { promotion.Id, promotion.StudentId, student.StudentName,
                        fromSession = fromSession.Year_Start.Year + "-" + fromSession.Year_End.Year,
                        toSession = toSession == null ? "-" : toSession.Year_Start.Year + "-" + toSession.Year_End.Year,
                        fromClass = fromClass.ClassName, toClass = toClass == null ? "-" : toClass.ClassName,
                        type = promotion.PromotionType, promotion.PromotionDate, promotion.Remarks };
        return Ok(new { success = true, data = await query.ToListAsync() });
    }

    private void AddHistory(StudentEnrollment source, StudentEnrollment? destination, string action, string? remarks) =>
        _db.StudentPromotions.Add(new StudentPromotion { StudentId = source.StudentId, FromEnrollmentId = source.Id,
            ToEnrollmentId = destination?.Id, FromSessionId = source.SessionId, ToSessionId = destination?.SessionId,
            FromClassId = source.ClassId, ToClassId = destination?.ClassId, FromSectionId = source.SectionId,
            ToSectionId = destination?.SectionId, PromotionType = action, PromotionDate = DateTime.UtcNow,
            SchoolId = source.SchoolId, CreatedBy = UserId, Remarks = remarks });

    private async Task<string> NextRollNumber(int schoolId, int sessionId, int classId, int sectionId)
    {
        var rolls = await _db.StudentEnrollment.Where(x => x.SchoolId == schoolId && x.SessionId == sessionId && x.ClassId == classId && x.SectionId == sectionId)
            .Select(x => x.RollNumber).ToListAsync();
        var max = rolls.Select(x => int.TryParse(x, out var n) ? n : 0).DefaultIfEmpty().Max();
        return (max + 1).ToString();
    }

    private async Task CopyFees(StudentEnrollment source, StudentEnrollment destination)
    {
        var oldFees = await _db.StudentFees.AsNoTracking().Where(x => x.EnrollmentId == source.Id && x.IsActive).ToListAsync();
        foreach (var fee in oldFees) _db.StudentFees.Add(new StudentFee { StudentId = destination.StudentId, EnrollmentId = destination.Id,
            FeeTypeId = fee.FeeTypeId, Amount = fee.Amount, SessionId = destination.SessionId, SchoolId = destination.SchoolId,
            Status = "Pending", Created_Date = DateTime.UtcNow, Created_By = UserId, IsActive = true });
    }

    private async Task CopyTransport(StudentEnrollment source, StudentEnrollment destination, List<string> warnings)
    {
        var old = await _db.StudentTransportAllocations.AsNoTracking().FirstOrDefaultAsync(x => x.EnrollmentId == source.Id && x.IsActive);
        if (old == null) return;
        var oldAssignment = await _db.TransportVehicleAssignments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == old.VehicleAssignmentId);
        var nextAssignment = oldAssignment == null ? null : await _db.TransportVehicleAssignments.FirstOrDefaultAsync(x =>
            x.SchoolId == destination.SchoolId && x.AcademicSessionId == destination.SessionId && x.RouteId == oldAssignment.RouteId && x.IsActive);
        if (nextAssignment == null) { warnings.Add($"Student {source.StudentId}: transport not copied because the route has no assignment in the next session."); return; }
        _db.StudentTransportAllocations.Add(new StudentTransportAllocation { SchoolId = destination.SchoolId, AcademicSessionId = destination.SessionId,
            StudentId = destination.StudentId, EnrollmentId = destination.Id, VehicleAssignmentId = nextAssignment.Id,
            PickupStopId = old.PickupStopId, DropStopId = old.DropStopId, PickupShift = old.PickupShift, DropShift = old.DropShift,
            SeatNumber = null, MonthlyFee = old.MonthlyFee, StartDate = destination.EnrollmentDate, IsActive = true });
    }

    private async Task AssignKit(StudentEnrollment destination, string kitType, List<string> warnings)
    {
        var kit = await _db.InventoryKits.AsNoTracking().FirstOrDefaultAsync(x => x.SchoolId == destination.SchoolId &&
            x.AcademicSessionId == destination.SessionId && x.ClassId == destination.ClassId && x.KitType == kitType && x.IsActive);
        if (kit == null) { warnings.Add($"Student {destination.StudentId}: no {kitType.ToLower()} kit configured for the destination class."); return; }
        var items = await _db.InventoryKitItems.AsNoTracking().Where(x => x.KitId == kit.Id).ToListAsync();
        if (items.Count == 0) { warnings.Add($"Student {destination.StudentId}: {kitType.ToLower()} kit is empty."); return; }
        var productIds = items.Select(x => x.ProductId).ToList();
        var products = await _db.InventoryProducts.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        if (items.Any(x => !products.TryGetValue(x.ProductId, out var p) || p.CurrentStock - p.ReservedStock < x.Quantity))
        { warnings.Add($"Student {destination.StudentId}: {kitType.ToLower()} kit not assigned because stock is insufficient."); return; }
        var order = new InventoryStudentOrder { SchoolId = destination.SchoolId, AcademicSessionId = destination.SessionId,
            StudentId = destination.StudentId, EnrollmentId = destination.Id, OrderNumber = $"SO{DateTime.UtcNow:yyyyMMddHHmmssfff}{destination.StudentId}", Status = "Approved" };
        _db.InventoryStudentOrders.Add(order);
        await _db.SaveChangesAsync();
        foreach (var item in items) { var product = products[item.ProductId]; product.ReservedStock += item.Quantity;
            order.TotalAmount += item.Quantity * product.SellingPrice;
            _db.InventoryStudentOrderItems.Add(new InventoryStudentOrderItem { StudentOrderId = order.Id, ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId, Quantity = item.Quantity, UnitPrice = product.SellingPrice, GstPercent = product.GstPercent }); }
    }
}
