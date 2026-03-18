using CineBook.Application.DTOs.Requests;
using FluentValidation;
using System;

namespace CineBook.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(100).WithMessage("Full Name cannot exceed 100 characters.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("User Name is required.")
                .MaximumLength(50).WithMessage("User Name cannot exceed 50 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone Number is required.")
                .Matches(@"^\d{10}$").WithMessage("Phone Number must be a valid 10-digit number.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.UserNameorPhone)
                .NotEmpty().WithMessage("Username or Phone number is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone Number is required.")
                .Matches(@"^\d{10}$").WithMessage("Phone Number must be a valid 10-digit number.");
        }
    }

    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone Number is required.")
                .Matches(@"^\d{10}$").WithMessage("Phone Number must be a valid 10-digit number.");

            RuleFor(x => x.Otp)
                .NotEmpty().WithMessage("OTP is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }

    public class AddCinemaRequestValidator : AbstractValidator<AddCinemaRequest>
    {
        public AddCinemaRequestValidator()
        {
            RuleFor(x => x.CinemaName).NotEmpty().WithMessage("Cinema Name is required.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
            RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.State).NotEmpty().WithMessage("State is required.");
            RuleFor(x => x.PinCode).NotEmpty().WithMessage("Pin Code is required.");
            RuleFor(x => x.LicenseNumber).NotEmpty().WithMessage("License Number is required.");
        }
    }

    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone Number is required.")
                .Matches(@"^\d{10}$").WithMessage("Phone Number must be a valid 10-digit number.");
        }
    }

    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current Password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }

    public class CreateMovieRequestValidator : AbstractValidator<CreateMovieRequest>
    {
        public CreateMovieRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Language).NotEmpty().WithMessage("Language is required.");
            RuleFor(x => x.Genre).NotEmpty().WithMessage("Genre is required.");
            RuleFor(x => x.Director).NotEmpty().WithMessage("Director is required.");
            RuleFor(x => x.DurationTime).GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");
            RuleFor(x => x.CertificateRating).NotEmpty().WithMessage("Certificate Rating is required.");
            RuleFor(x => x.PosterUrl).NotEmpty().WithMessage("Poster URL is required.");
            RuleFor(x => x.ReleaseDate).NotEmpty().WithMessage("Release Date is required.");
        }
    }

    public class UpdateMovieRequestValidator : AbstractValidator<UpdateMovieRequest>
    {
        public UpdateMovieRequestValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Language).NotEmpty().WithMessage("Language is required.");
            RuleFor(x => x.Genre).NotEmpty().WithMessage("Genre is required.");
            RuleFor(x => x.Director).NotEmpty().WithMessage("Director is required.");
            RuleFor(x => x.DurationTime).GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");
            RuleFor(x => x.CertificateRating).NotEmpty().WithMessage("Certificate Rating is required.");
            RuleFor(x => x.PosterUrl).NotEmpty().WithMessage("Poster URL is required.");
            RuleFor(x => x.ReleaseDate).NotEmpty().WithMessage("Release Date is required.");
        }
    }
}