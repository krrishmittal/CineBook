using System;
using System.Collections.Generic;
using System.Text;

namespace CineBook.Application.DTOs.Requests
{
    public class RegisterCinemaRequest
    {
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? CinemaLogo { get; set; }
        public string LicenseNumber { get; set; }
    }

    public class ApproveCinemaRequest
    {
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
