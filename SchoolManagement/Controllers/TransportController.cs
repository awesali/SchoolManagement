using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;

namespace SchoolManagement.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TransportController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TransportController(AppDbContext context) => _context = context;

        private IActionResult Success(object? data, string message = "Success") =>
            Ok(new { success = true, message, data });

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(int schoolId)
        {
            var today = DateTime.Today;
            return Success(new
            {
                totalVehicles = await _context.TransportVehicles.CountAsync(x => x.SchoolId == schoolId && x.IsActive),
                activeRoutes = await _context.TransportRoutes.CountAsync(x => x.SchoolId == schoolId && x.IsActive),
                allocatedStudents = await _context.StudentTransportAllocations.CountAsync(x => x.SchoolId == schoolId && x.IsActive),
                availableSeats = await AvailableSeats(schoolId),
                pendingFees = await _context.TransportFees.Where(x => x.SchoolId == schoolId && x.Status != "Paid").SumAsync(x => x.Amount - x.PaidAmount),
                expiringDocuments = await _context.TransportVehicles.CountAsync(x => x.SchoolId == schoolId &&
                    ((x.InsuranceExpiry >= today && x.InsuranceExpiry <= today.AddDays(30)) ||
                     (x.FitnessExpiry >= today && x.FitnessExpiry <= today.AddDays(30)) ||
                     (x.PollutionExpiry >= today && x.PollutionExpiry <= today.AddDays(30))))
            });
        }

        [HttpGet("vehicle-types")]
        public async Task<IActionResult> VehicleTypes(int schoolId) =>
            Success(await _context.VehicleTypes.Where(x => x.SchoolId == schoolId).OrderBy(x => x.VehicleTypeName).ToListAsync());

        [HttpPost("vehicle-types")]
        public async Task<IActionResult> SaveVehicleType(VehicleType item)
        {
            if (string.IsNullOrWhiteSpace(item.VehicleTypeName) || item.DefaultCapacity <= 0)
                return BadRequest(new { success = false, message = "Name and capacity are required." });
            _context.VehicleTypes.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Vehicle type created.");
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> Vehicles(int schoolId) => Success(await (
            from vehicle in _context.TransportVehicles
            join type in _context.VehicleTypes on vehicle.VehicleTypeId equals type.Id
            where vehicle.SchoolId == schoolId
            orderby vehicle.VehicleName
            select new { vehicle.Id, vehicle.VehicleName, vehicle.VehicleNumber, vehicle.RegistrationNumber,
                vehicle.VehicleTypeId, vehicle.Capacity, vehicle.GpsDeviceId, vehicle.InsuranceExpiry,
                vehicle.FitnessExpiry, vehicle.PollutionExpiry, vehicle.IsActive, vehicle.SchoolId,
                vehicleTypeName = type.VehicleTypeName }).ToListAsync());

        [HttpPost("vehicles")]
        public async Task<IActionResult> SaveVehicle(TransportVehicle item)
        {
            if (await _context.TransportVehicles.AnyAsync(x => x.SchoolId == item.SchoolId && x.VehicleNumber == item.VehicleNumber))
                return Conflict(new { success = false, message = "Vehicle number already exists." });
            _context.TransportVehicles.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Vehicle created.");
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> Drivers(int schoolId) =>
            Success(await _context.TransportDrivers.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).ToListAsync());

        [HttpPost("drivers")]
        public async Task<IActionResult> SaveDriver(TransportDriver item)
        {
            if (await _context.TransportDrivers.AnyAsync(x => x.SchoolId == item.SchoolId && x.LicenseNumber == item.LicenseNumber))
                return Conflict(new { success = false, message = "License number already exists." });
            _context.TransportDrivers.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Driver created.");
        }

        [HttpGet("conductors")]
        public async Task<IActionResult> Conductors(int schoolId) =>
            Success(await _context.TransportConductors.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).ToListAsync());

        [HttpPost("conductors")]
        public async Task<IActionResult> SaveConductor(TransportConductor item)
        {
            _context.TransportConductors.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Conductor created.");
        }

        [HttpGet("routes")]
        public async Task<IActionResult> Routes(int schoolId)
        {
            var routes = await _context.TransportRoutes.Where(x => x.SchoolId == schoolId).OrderBy(x => x.RouteName).ToListAsync();
            var routeIds = routes.Select(x => x.Id).ToList();
            var stops = await _context.TransportRouteStops.Where(x => routeIds.Contains(x.RouteId)).OrderBy(x => x.StopOrder).ToListAsync();
            return Success(routes.Select(route => new { route.Id, route.SchoolId, route.RouteName, route.RouteCode,
                route.StartPoint, route.EndPoint, route.DistanceKm, route.EstimatedMinutes, route.IsActive,
                stops = stops.Where(x => x.RouteId == route.Id) }));
        }

        [HttpPost("routes")]
        public async Task<IActionResult> SaveRoute(TransportRoute item)
        {
            if (await _context.TransportRoutes.AnyAsync(x => x.SchoolId == item.SchoolId && x.RouteCode == item.RouteCode))
                return Conflict(new { success = false, message = "Route code already exists." });
            _context.TransportRoutes.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Route created.");
        }

        [HttpPost("route-stops")]
        public async Task<IActionResult> SaveStop(TransportRouteStop item)
        {
            _context.TransportRouteStops.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Route stop created.");
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> Assignments(int schoolId) => Success(await (
            from assignment in _context.TransportVehicleAssignments
            join vehicle in _context.TransportVehicles on assignment.VehicleId equals vehicle.Id
            join driver in _context.TransportDrivers on assignment.DriverId equals driver.Id
            join conductor in _context.TransportConductors on assignment.ConductorId equals conductor.Id into conductorGroup
            from conductor in conductorGroup.DefaultIfEmpty()
            join route in _context.TransportRoutes on assignment.RouteId equals route.Id
            where assignment.SchoolId == schoolId
            select new { assignment.Id, assignment.AcademicSessionId, assignment.VehicleId, assignment.DriverId,
                assignment.ConductorId, assignment.RouteId, assignment.StartDate, assignment.EndDate, assignment.IsActive,
                vehicleName = vehicle.VehicleName, driverName = driver.Name,
                conductorName = conductor != null ? conductor.Name : null, routeName = route.RouteName }).ToListAsync());

        [HttpPost("assignments")]
        public async Task<IActionResult> SaveAssignment(TransportVehicleAssignment item)
        {
            var conflict = await _context.TransportVehicleAssignments.AnyAsync(x => x.IsActive &&
                (x.VehicleId == item.VehicleId || x.DriverId == item.DriverId));
            if (conflict) return Conflict(new { success = false, message = "Vehicle or driver already has an active assignment." });
            _context.TransportVehicleAssignments.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Vehicle assignment created.");
        }

        [HttpGet("allocations")]
        public async Task<IActionResult> Allocations(int schoolId) => Success(await (
            from allocation in _context.StudentTransportAllocations
            join student in _context.Students on allocation.StudentId equals student.Id
            join assignment in _context.TransportVehicleAssignments on allocation.VehicleAssignmentId equals assignment.Id
            join vehicle in _context.TransportVehicles on assignment.VehicleId equals vehicle.Id
            join route in _context.TransportRoutes on assignment.RouteId equals route.Id
            join pickup in _context.TransportRouteStops on allocation.PickupStopId equals pickup.Id
            join drop in _context.TransportRouteStops on allocation.DropStopId equals drop.Id
            where allocation.SchoolId == schoolId
            select new { allocation.Id, allocation.StudentId, allocation.VehicleAssignmentId, allocation.AcademicSessionId,
                allocation.PickupStopId, allocation.DropStopId, allocation.PickupShift, allocation.DropShift,
                allocation.SeatNumber, allocation.MonthlyFee, allocation.StartDate, allocation.EndDate, allocation.IsActive,
                studentName = student.StudentName, vehicleName = vehicle.VehicleName, routeName = route.RouteName,
                pickupStop = pickup.StopName, dropStop = drop.StopName }).ToListAsync());

        [HttpPost("allocations")]
        public async Task<IActionResult> SaveAllocation(StudentTransportAllocation item)
        {
            var enrollment = await _context.StudentEnrollment.FirstOrDefaultAsync(x => (item.EnrollmentId > 0 ? x.Id == item.EnrollmentId : x.StudentId == item.StudentId) && x.StudentId == item.StudentId && x.SchoolId == item.SchoolId && x.SessionId == item.AcademicSessionId && x.IsActive);
            if (enrollment == null) return BadRequest(new { success = false, message = "A valid active enrollment is required for transport allocation." });
            item.EnrollmentId = enrollment.Id;
            var assignment = await _context.TransportVehicleAssignments.FindAsync(item.VehicleAssignmentId);
            if (assignment == null) return BadRequest(new { success = false, message = "Assignment not found." });
            var vehicle = await _context.TransportVehicles.FindAsync(assignment.VehicleId);
            var occupied = await _context.StudentTransportAllocations.CountAsync(x => x.VehicleAssignmentId == item.VehicleAssignmentId && x.IsActive);
            if (vehicle == null || occupied >= vehicle.Capacity)
                return Conflict(new { success = false, message = "Vehicle has no available seats." });
            if (await _context.StudentTransportAllocations.AnyAsync(x => x.EnrollmentId == item.EnrollmentId && x.IsActive))
                return Conflict(new { success = false, message = "Student already has an active transport allocation." });
            _context.StudentTransportAllocations.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Student allocated.");
        }

        [HttpGet("fees")]
        public async Task<IActionResult> Fees(int schoolId) => Success(await (
            from fee in _context.TransportFees
            join allocation in _context.StudentTransportAllocations on fee.StudentTransportAllocationId equals allocation.Id
            join student in _context.Students on allocation.StudentId equals student.Id
            where fee.SchoolId == schoolId
            orderby fee.FeeYear descending, fee.FeeMonth descending
            select new { fee.Id, fee.StudentTransportAllocationId, fee.FeeMonth, fee.FeeYear, fee.Amount,
                fee.PaidAmount, fee.Status, fee.DueDate, studentName = student.StudentName }).ToListAsync());

        [HttpPost("fees/generate")]
        public async Task<IActionResult> GenerateFees(int schoolId, int month, int year, DateTime dueDate)
        {
            var allocations = await _context.StudentTransportAllocations.Where(x => x.SchoolId == schoolId && x.IsActive).ToListAsync();
            var existing = await _context.TransportFees.Where(x => x.SchoolId == schoolId && x.FeeMonth == month && x.FeeYear == year)
                .Select(x => x.StudentTransportAllocationId).ToListAsync();
            var fees = allocations.Where(x => !existing.Contains(x.Id)).Select(x => new TransportFee
                { SchoolId = schoolId, StudentTransportAllocationId = x.Id, FeeMonth = month, FeeYear = year,
                  Amount = x.MonthlyFee, DueDate = dueDate }).ToList();
            _context.TransportFees.AddRange(fees); await _context.SaveChangesAsync();
            return Success(new { generated = fees.Count }, "Transport fees generated.");
        }

        [HttpPost("payments")]
        public async Task<IActionResult> Pay(TransportFeePayment item)
        {
            var fee = await _context.TransportFees.FindAsync(item.TransportFeeId);
            if (fee == null) return NotFound(new { success = false, message = "Fee not found." });
            item.ReceiptNumber = string.IsNullOrWhiteSpace(item.ReceiptNumber)
                ? $"TR{DateTime.Now:yyyyMMddHHmmss}" : item.ReceiptNumber;
            fee.PaidAmount += item.Amount;
            fee.Status = fee.PaidAmount >= fee.Amount ? "Paid" : "Partial";
            _context.TransportFeePayments.Add(item); await _context.SaveChangesAsync();
            return Success(item, "Payment recorded.");
        }

        [HttpGet("payments")]
        public async Task<IActionResult> Payments(int schoolId) => Success(await (
            from payment in _context.TransportFeePayments
            join fee in _context.TransportFees on payment.TransportFeeId equals fee.Id
            join allocation in _context.StudentTransportAllocations on fee.StudentTransportAllocationId equals allocation.Id
            join student in _context.Students on allocation.StudentId equals student.Id
            where fee.SchoolId == schoolId orderby payment.PaymentDate descending
            select new { payment.Id, payment.ReceiptNumber, payment.Amount, payment.PaymentDate,
                payment.PaymentMode, payment.ReferenceNumber, studentName = student.StudentName }).ToListAsync());

        [HttpGet("fuel-logs")]
        public async Task<IActionResult> FuelLogs(int schoolId) => Success(await _context.TransportFuelLogs.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.FuelDate).ToListAsync());
        [HttpPost("fuel-logs")]
        public async Task<IActionResult> SaveFuel(TransportFuelLog item) { _context.TransportFuelLogs.Add(item); await _context.SaveChangesAsync(); return Success(item); }

        [HttpGet("maintenance")]
        public async Task<IActionResult> Maintenance(int schoolId) => Success(await _context.TransportVehicleMaintenance.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.ServiceDate).ToListAsync());
        [HttpPost("maintenance")]
        public async Task<IActionResult> SaveMaintenance(TransportVehicleMaintenance item) { _context.TransportVehicleMaintenance.Add(item); await _context.SaveChangesAsync(); return Success(item); }

        private async Task<int> AvailableSeats(int schoolId)
        {
            var capacity = await (from a in _context.TransportVehicleAssignments join v in _context.TransportVehicles on a.VehicleId equals v.Id where a.SchoolId == schoolId && a.IsActive select v.Capacity).SumAsync();
            var occupied = await _context.StudentTransportAllocations.CountAsync(x => x.SchoolId == schoolId && x.IsActive);
            return Math.Max(0, capacity - occupied);
        }
    }
}
