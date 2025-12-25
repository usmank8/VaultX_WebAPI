using System.ComponentModel.DataAnnotations;

namespace VaultX_WebAPI.DTOs
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email must be at most 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [StringLength(255, ErrorMessage = "Password must be at most 255 characters")]
        public string Password { get; set; }
    }

    public class OtpRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }
    }

    public class OtpResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public class DashboardDataDto
    {
        public string TodaysDate { get; set; }
        public int TotalUsers { get; set; }
        public int TotalGuests { get; set; }
        public int TotalVehicles { get; set; }
        public int PendingResidents { get; set; }
        public int ApprovedResidents { get; set; }
    }
    public class AddVehicleDto
    {
        [Required]
        public string VehicleName { get; set; } = string.Empty;
        
        public string? VehicleModel { get; set; }
        
        [Required]
        public string VehicleType { get; set; } = string.Empty;
        
        [Required]
        public string VehicleLicensePlateNumber { get; set; } = string.Empty;
        
        public string? VehicleRfidTagId { get; set; }
        
        public string? VehicleColor { get; set; }
        
        // ✅ Optional: Specify which residence (defaults to primary if not provided)
        public Guid? ResidenceId { get; set; }
    }

    public class UpdateVehicleDto
    {
        public string? VehicleName { get; set; }
        public string? VehicleModel { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleLicensePlateNumber { get; set; }
        public string? VehicleRfidTagId { get; set; }
        public string? VehicleColor { get; set; }
    }

    public class AddGuestVehicleDto
    {
        public Guid ResidenceId { get; set; }  // ← ADD THIS LINE
        public string VehicleName { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public string VehicleType { get; set; } = null!;
        public string VehicleLicensePlateNumber { get; set; } = null!;
        public string VehicleRFIDTagId { get; set; } = null!;
        public string VehicleColor { get; set; } = null!;
    }

    public class VehicleDto
    {
        public string VehicleId { get; set; }
        public string OwnerName { get; set; }
        public string VehicleName { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleType { get; set; }
        public string VehicleLicensePlateNumber { get; set; }
        public string VehicleRFIDTagId { get; set; }
        public string VehicleColor { get; set; }
        public bool IsGuest { get; set; }
    }

    public class CreateSocietyDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }
    }

    public class UpdateSocietyDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }
    }

    public class SocietyDto
    {
        public string SocietyId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }

    public class PendingApprovalDto
    {
        public string ResidentId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Cnic { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public ResidenceSummaryDto Residence { get; set; }
    }

    public class ResidenceSummaryDto
    {
        public string AddressLine1 { get; set; }
        public string Block { get; set; }
        public string Residence { get; set; }
        public string ResidenceType { get; set; }
    }

    public class ResidentByStatusDto
    {
        public string ResidentId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Cnic { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }
        public string Block { get; set; }
        public string Residence { get; set; }
        public string ResidenceType { get; set; }
    }

    public class AddGuestDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhoneNumber { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime Eta { get; set; }
        public DateTime CheckoutTime { get; set; }
        public Guid? ResidenceId { get; set; }
        
        // ✅ Vehicle details (optional - will auto-create if provided)
        public string? VehicleName { get; set; }
        public string? VehicleModel { get; set; }
        public string? VehicleLicensePlateNumber { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
    }

    public class ExtendGuestTimeDto
    {
        [Required(ErrorMessage = "New checkout time is required")]
        public DateTime NewCheckoutTime { get; set; }
    }

    public enum ResidenceEnum
    {
        [Display(Name = "apartment")]
        Apartment,
        [Display(Name = "flat")]
        Flat,
        [Display(Name = "house")]
        House
    }

    public enum ResidenceType
    {
        [Display(Name = "rented")]
        Rented,
        [Display(Name = "owned")]
        Owned
    }

    public class CreateProfileDto
    {
        [Required(ErrorMessage = "Firstname is required")]
        [StringLength(100, ErrorMessage = "Firstname must be at most 100 characters")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Lastname is required")]
        [StringLength(100, ErrorMessage = "Lastname must be at most 100 characters")]
        public string Lastname { get; set; }

        [Required(ErrorMessage = "Phonenumber is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phonenumber { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "CNIC must be exactly 13 digits")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "CNIC must be 13 digits")]
        public string Cnic { get; set; }

        [Required(ErrorMessage = "Residence is required")]
        [EnumDataType(typeof(ResidenceEnum))]
        public ResidenceEnum Residence { get; set; }

        [Required(ErrorMessage = "ResidenceType is required")]
        [EnumDataType(typeof(ResidenceType))]
        public ResidenceType ResidenceType { get; set; }

        [Required(ErrorMessage = "Block is required")]
        [StringLength(50, ErrorMessage = "Block must be at most 50 characters")]
        public string Block { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address must be at most 500 characters")]
        public string Address { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Phonenumber { get; set; }
        public string? Cnic { get; set; }
        public ResidenceEnum? Residence { get; set; }
        public ResidenceType? ResidenceType { get; set; }
        public string? Block { get; set; }
        public string? Address { get; set; }
    }

    public class UpdatePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        [MinLength(6, ErrorMessage = "Current password must be at least 8 characters")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "New password must be at least 8 characters")]
        public string NewPassword { get; set; }
    }

    public class GetUserProfileDto
    {
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ResidenceDto? Residence { get; set; }
    }

    public class ResidenceDto
    {
        public string? AddressLine1 { get; set; }
        public string? Block { get; set; }
        public string? Residence { get; set; }
        public string? ResidenceType { get; set; }
    }

    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "Firstname is required")]
        [StringLength(100)]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Lastname is required")]
        [StringLength(100)]
        public string Lastname { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        public string Cnic { get; set; }

        [Required(ErrorMessage = "InternalRole is required")]
        [StringLength(100)]
        public string InternalRole { get; set; }

        public string? Department { get; set; }

        public string? Shift { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoiningDate { get; set; }
    }

    public class GetEmployeeProfileDto
    {
        public string EmployeeId { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? Cnic { get; set; }
        public string InternalRole { get; set; }
        public string? Department { get; set; }
        public string? Shift { get; set; }
        public DateTime? JoiningDate { get; set; }
    }

    public class GuestWithVehicleDto
    {
        public string GuestId { get; set; }
        public string GuestName { get; set; }
        public DateTime? Eta { get; set; }
        public string VehicleId { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleLicensePlateNumber { get; set; }
        public string VehicleColor { get; set; }
        public bool? IsGuest { get; set; }
    }

    public class GuestDetailDto
    {
        public string GuestId { get; set; }
        public string GuestName { get; set; }
        public string GuestPhoneNumber { get; set; }
        public DateTime? Eta { get; set; }
        public bool VisitCompleted { get; set; }
        public GuestResidenceDto Residence { get; set; }
        public GuestVehicleDto GuestVehicle { get; set; }
    }

    public class GuestResidenceDto
    {
        public string AddressLine1 { get; set; }
        public string Id { get; set; }

        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string FlatNumber { get; set; }
        public string Block { get; set; }
    }

    public class GuestVehicleDto
    {
        public string VehicleId { get; set; }
        public string VehicleType { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleName { get; set; }
        public string VehicleLicensePlateNumber { get; set; }
        public string VehicleRFIDTagId { get; set; }
        public string VehicleColor { get; set; }
        public bool IsGuest { get; set; }
    }

    public class VerifyGuestDto
    {
        [Required]
        public string GuestId { get; set; }
    }

    public class QrCodeResponse
    {
        public string QrCodeImage { get; set; }
    }

    public class PaginatedGuestsDto
    {
        public List<GuestDetailDto> Data { get; set; }
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Limit { get; set; }
        public int Page { get; set; }
    }

    public class VerifyResponse
    {
        public bool Valid { get; set; }
        public string Reason { get; set; }
    }

    public class ApprovalResponse
    {
        public string Message { get; set; }
    }

    public class changePasswordDto
    {
        [Required]
        public string newPassword { get; set; } = string.Empty;
        [Required]
        public string email { get; set; } = string.Empty;

    }

    public class AddResidenceDto
    {
        public string Residence { get; set; } = null!;
        public string ResidenceType { get; set; } = null!;
        public string Block { get; set; } = null!;
        public string Address { get; set; } = null!;
    }
}
