using CineBook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CineBook.Application.DTOs.Responses
{
    public class CinemaResponse
    {
        public Guid Id { get; set; }
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PinCode { get; set; }
        public string State { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? CinemaLogo { get; set; }
        public string LicenseNumber { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public string ApprovalStatusLabel { get; set; }
        public string? RejectionReason { get; set; }
        public string ManagerId { get; set; }
        public string ManagerName { get; set; }
        public string ManagerPhone { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
