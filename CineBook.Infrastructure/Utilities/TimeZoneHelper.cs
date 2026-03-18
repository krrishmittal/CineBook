using CineBook.Domain.Enums;

namespace CineBook.Infrastructure.Utilities
{
    public static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo IndianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime ConvertToIST(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTime(utcDateTime, IndianTimeZone);
        }

        public static DateTime ConvertToUTC(DateTime istDateTime)
        {
            if (istDateTime.Kind != DateTimeKind.Unspecified)
                istDateTime = DateTime.SpecifyKind(istDateTime, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(istDateTime, IndianTimeZone);
        }
        public static string FormatHallType(HallType hallType)
        {
            return hallType switch
            {
                HallType.TwoD => "2D",
                HallType.ThreeD => "3D",
                HallType.IMAX => "IMAX",
                _ => hallType.ToString()
            };
        }
    }
}