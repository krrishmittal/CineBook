namespace CineBook.Application.DTOs.Requests
{
    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class LoginRequest
    {
        public string UserNameorPhone { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string PhoneNumber { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string PhoneNumber {  get; set;}
        public string Otp {  get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
    
    public class AddCinemaRequest
    {
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string LicenseNumber { get; set; }
    }
}
