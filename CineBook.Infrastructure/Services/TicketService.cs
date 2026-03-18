using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using CineBook.Infrastructure.Persistence;
using CineBook.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CineBook.Infrastructure.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateTicketPdfAsync(Guid bookingId, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Hall)
                .Include(b => b.Showtime).ThenInclude(s => s.Cinema)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                throw new Exception("Booking not found");

            // Generate QR code as PNG bytes
            var qrBytes = GenerateQrCode(booking.BookingReference);

            return GeneratePdf(booking, qrBytes);
        }

        private static byte[] GenerateQrCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(6);
        }

        private static byte[] GeneratePdf(Booking booking, byte[] qrBytes)
        {
            var showtime = booking.Showtime;
            var movie = showtime.Movie;
            var hall = showtime.Hall;
            var cinema = showtime.Cinema;
            var seats = booking.BookingSeats.OrderBy(s => s.Seat.Row).ThenBy(s => s.Seat.SeatNumber).ToList();

            // ✅ Convert times from UTC to IST
            var startTime = TimeZoneHelper.ConvertToIST(showtime.StartTime);
            var endTime = TimeZoneHelper.ConvertToIST(showtime.EndTime);
            var bookedAtTime = TimeZoneHelper.ConvertToIST(booking.BookedAt);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Header Band ──────────────────────────────
                        col.Item().Background("#0a0a0a").Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CINEBOOK")
                                    .FontSize(32).Bold()
                                    .FontColor("#E50914")
                                    .LetterSpacing(0.1f);

                                c.Item().Text("MOVIE TICKET")
                                    .FontSize(10)
                                    .FontColor("#888888")
                                    .LetterSpacing(0.15f);
                            });

                            row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().Text($"REF: {booking.BookingReference}")
                                    .FontSize(11).Bold()
                                    .FontColor("#E50914")
                                    .LetterSpacing(0.06f);

                                // ✅ Format BookedAt in IST
                                c.Item().Text(bookedAtTime.ToString("dd MMM yyyy, hh:mm tt"))
                                    .FontSize(9).FontColor("#666666");
                            });
                        });

                        // ── Main Content ──────────────────────────────
                        col.Item().Background("#141414").Row(mainRow =>
                        {
                            // Left — Movie + Show info
                            mainRow.RelativeItem(3).Padding(20).Column(c =>
                            {
                                // Movie title
                                c.Item().Text(movie.Title)
                                    .FontSize(22).Bold()
                                    .FontColor("#f5f5f5");

                                c.Item().PaddingTop(4).Text($"{movie.Language}  •  {movie.CertificateRating}  •  {movie.DurationTime} min")
                                    .FontSize(10).FontColor("#888888");

                                c.Item().PaddingTop(16).LineHorizontal(1).LineColor("#2a2a2a");

                                // Cinema info
                                c.Item().PaddingTop(16).Row(r =>
                                {
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("CINEMA").FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                        inner.Item().Text(cinema.CinemaName).FontSize(13).Bold().FontColor("#f5f5f5");
                                        inner.Item().Text($"{cinema.City}, {cinema.State}").FontSize(10).FontColor("#888");
                                    });

                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("HALL").FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                        inner.Item().Text(hall.HallName).FontSize(13).Bold().FontColor("#f5f5f5");
                                        inner.Item().Text(hall.HallType.ToString()).FontSize(10).FontColor("#888");
                                    });
                                });

                                // ✅ Date + Time (now in IST)
                                c.Item().PaddingTop(14).Row(r =>
                                {
                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("DATE").FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                        inner.Item().Text(startTime.ToString("dddd, dd MMMM yyyy"))
                                            .FontSize(13).Bold().FontColor("#f5f5f5");
                                    });

                                    r.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("TIME").FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                        inner.Item().Text($"{startTime:hh:mm tt} — {endTime:hh:mm tt}")
                                            .FontSize(13).Bold().FontColor("#f5f5f5");
                                    });
                                });

                                // Seats
                                c.Item().PaddingTop(14).Column(inner =>
                                {
                                    inner.Item().Text("SEATS").FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                    inner.Item().PaddingTop(4).Row(seatRow =>
                                    {
                                        foreach (var seat in seats)
                                        {
                                            var color = seat.SeatType == Domain.Enums.SeatType.Standard ? "#3b82f6"
                                                      : seat.SeatType == Domain.Enums.SeatType.Premium ? "#a855f7"
                                                      : "#f59e0b";
                                            seatRow.AutoItem().PaddingRight(6).Background("#1e1e1e")
                                                .Border(1).BorderColor("#2a2a2a")
                                                .Padding(6).Column(sc =>
                                                {
                                                    sc.Item().Text($"{seat.Seat.Row}{seat.Seat.SeatNumber}")
                                                        .FontSize(11).Bold().FontColor(color);
                                                    sc.Item().Text(seat.SeatType.ToString())
                                                        .FontSize(8).FontColor("#666");
                                                });
                                        }
                                    });
                                });
                            });

                            // Divider
                            mainRow.AutoItem().Width(1).Background("#2a2a2a");

                            // Right — QR + Price
                            mainRow.RelativeItem(1.2f).Padding(20).AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("SCAN TO VERIFY")
                                    .FontSize(9).FontColor("#666").LetterSpacing(0.1f);

                                c.Item().PaddingTop(8).AlignCenter()
                                    .Width(100).Height(100)
                                    .Image(qrBytes);

                                c.Item().PaddingTop(12).LineHorizontal(1).LineColor("#2a2a2a");

                                c.Item().PaddingTop(12).AlignCenter().Column(priceCol =>
                                {
                                    priceCol.Item().AlignCenter().Text("TOTAL PAID")
                                        .FontSize(9).FontColor("#666").LetterSpacing(0.1f);
                                    priceCol.Item().AlignCenter().Text($"₹{booking.TotalAmount:F0}")
                                        .FontSize(22).Bold().FontColor("#E50914");
                                    priceCol.Item().AlignCenter().Text($"{seats.Count} Ticket{(seats.Count > 1 ? "s" : "")}")
                                        .FontSize(10).FontColor("#888");
                                });

                                c.Item().PaddingTop(10).AlignCenter().Text(booking.BookingReference)
                                    .FontSize(9).FontColor("#444").LetterSpacing(0.08f);
                            });
                        });

                        // ── Footer ────────────────────────────────────
                        col.Item().Background("#0d0d0d").Padding(10).Row(row =>
                        {
                            row.RelativeItem().AlignMiddle().Text("Thank you for booking with CineBook!")
                                .FontSize(9).FontColor("#555");
                            row.AutoItem().AlignRight().AlignMiddle()
                                .Text("Please arrive 15 minutes before showtime • No refunds after show starts")
                                .FontSize(8).FontColor("#444");
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}