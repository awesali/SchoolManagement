namespace SchoolManagement.Model
{
    public class VehicleType
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public string VehicleTypeName { get; set; } = string.Empty;
        public int DefaultCapacity { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TransportVehicle
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int VehicleTypeId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string? GpsDeviceId { get; set; }
        public DateTime? InsuranceExpiry { get; set; }
        public DateTime? FitnessExpiry { get; set; }
        public DateTime? PollutionExpiry { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TransportDriver
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime? LicenseExpiry { get; set; }
        public string? Address { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TransportConductor
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TransportRoute
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int EstimatedMinutes { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TransportRouteStop
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public TimeSpan PickupTime { get; set; }
        public TimeSpan? DropTime { get; set; }
        public int StopOrder { get; set; }
    }

    public class TransportVehicleAssignment
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int AcademicSessionId { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public int? ConductorId { get; set; }
        public int RouteId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class StudentTransportAllocation
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int AcademicSessionId { get; set; }
        public int StudentId { get; set; }
        public int? EnrollmentId { get; set; }
        public int VehicleAssignmentId { get; set; }
        public int? PickupStopId { get; set; }
        public int? DropStopId { get; set; }
        public string? PickupStop { get; set; }
        public string? DropStop { get; set; }
        public string? PickupShift { get; set; }
        public string? DropShift { get; set; }
        public string? SeatNumber { get; set; }
        public decimal MonthlyFee { get; set; }
        public string FeeType { get; set; } = "Monthly";
        public DateTime? DueDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class VehicleSetupRequest
    {
        public int SchoolId { get; set; }
        public int? VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; } = string.Empty;
        public int DefaultCapacity { get; set; }
        public string? Description { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime? InsuranceExpiry { get; set; }
        public DateTime? FitnessExpiry { get; set; }
        public DateTime? PollutionExpiry { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BulkStudentTransportAllocationRequest
    {
        public int SchoolId { get; set; }
        public List<StudentTransportAllocationRequest> Items { get; set; } = new();
    }

    public class StudentTransportAllocationRequest
    {
        public int AcademicSessionId { get; set; }
        public int StudentId { get; set; }
        public int VehicleAssignmentId { get; set; }
        public string PickupStop { get; set; } = string.Empty;
        public string DropStop { get; set; } = string.Empty;
        public string? PickupShift { get; set; }
        public string? DropShift { get; set; }
        public decimal MonthlyFee { get; set; }
        public string FeeType { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public string? SeatNumber { get; set; }
    }

    public class TransportFee
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int StudentTransportAllocationId { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime DueDate { get; set; }
    }

    public class TransportFeePayment
    {
        public int Id { get; set; }
        public int TransportFeeId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentMode { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }
    }

    public class TransportFuelLog
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int VehicleId { get; set; }
        public DateTime FuelDate { get; set; }
        public decimal Litres { get; set; }
        public decimal Amount { get; set; }
        public decimal? OdometerReading { get; set; }
        public string? PaymentMode { get; set; }
        public string? PaidTo { get; set; }
    }

    public class TransportVehicleMaintenance
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int VehicleId { get; set; }
        public DateTime ServiceDate { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public decimal Cost { get; set; }
        public string? Workshop { get; set; }
        public string? Remarks { get; set; }
        public string? BillAttachmentUrl { get; set; }
    }

    public class TransportDriverAttendance
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int DriverId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = "Present";
        public string? Remarks { get; set; }
    }

    public class TransportGpsLocation
    {
        public long Id { get; set; }
        public int VehicleId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? Speed { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
