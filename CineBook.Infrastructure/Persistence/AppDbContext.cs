using CineBook.Domain.Entities;
using CineBook.Domain.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CineBook.Infrastructure.Persistence
{
    public class AppDbContext:IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) :base(options){ }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Movie> Movies {  get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<ShowtimeSeat> ShowtimeSeats { get; set; }
        public DbSet<Booking> Bookings  { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Payment>Payments {  get; set; }
        public DbSet<UserFavourite> UserFavourites { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // ── ApplicationUser ──────────────────────────────
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // ── RefreshToken ─────────────────────────────────
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Cinema ───────────────────────────────────────
            builder.Entity<Cinema>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CinemaName).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Address).IsRequired();
                entity.Property(c => c.City).IsRequired().HasMaxLength(100);
                entity.Property(c => c.State).IsRequired().HasMaxLength(100);
                entity.Property(c => c.PinCode).IsRequired().HasMaxLength(10);
                entity.Property(c => c.LicenseNumber).IsRequired();
                entity.HasOne(c => c.Manager)
                    .WithMany()
                    .HasForeignKey(c => c.ManagerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Hall ─────────────────────────────────────────
            builder.Entity<Hall>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.HallName).IsRequired().HasMaxLength(100);
                entity.HasOne(h => h.Cinema)
                    .WithMany(c => c.Halls)
                    .HasForeignKey(h => h.CinemaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Seat ─────────────────────────────────────────
            builder.Entity<Seat>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Row).IsRequired().HasMaxLength(5);
                entity.HasOne(s => s.Hall)
                    .WithMany(h => h.Seats)
                    .HasForeignKey(s => s.HallId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Movie ─────────────────────────────────────────
            builder.Entity<Movie>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
                entity.Property(m => m.Description).IsRequired();
                entity.Property(m => m.PosterUrl).IsRequired();
                entity.Property(m => m.TrailerUrl).IsRequired();
                entity.Property(m => m.Language).IsRequired().HasMaxLength(50);
                entity.Property(m => m.Genre).IsRequired().HasMaxLength(200);
                entity.Property(m => m.Cast).IsRequired();
                entity.Property(m => m.Director).IsRequired().HasMaxLength(100);
                entity.Property(m => m.CertificateRating).IsRequired().HasMaxLength(5);
                builder.Entity<Review>().HasQueryFilter(r => !r.Movie.IsDeleted);
                builder.Entity<Showtime>().HasQueryFilter(s => !s.Movie.IsDeleted);
                builder.Entity<UserFavourite>().HasQueryFilter(f => !f.Movie.IsDeleted);
                builder.Entity<Review>().HasQueryFilter(r => !r.Movie.IsDeleted);
                builder.Entity<Showtime>().HasQueryFilter(s => !s.Movie.IsDeleted);
                builder.Entity<UserFavourite>().HasQueryFilter(f => !f.Movie.IsDeleted);
            });

            // ── Showtime ──────────────────────────────────────
            builder.Entity<Showtime>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.PriceStandard).HasColumnType("decimal(18,2)");
                entity.Property(s => s.PricePremium).HasColumnType("decimal(18,2)");
                entity.Property(s => s.PriceVIP).HasColumnType("decimal(18,2)");
                entity.HasOne(s => s.Movie)
                    .WithMany(m => m.Showtimes)
                    .HasForeignKey(s => s.MovieId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.Hall)
                    .WithMany(h => h.Showtimes)
                    .HasForeignKey(s => s.HallId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.Cinema)
                    .WithMany(c => c.Showtimes)
                    .HasForeignKey(s => s.CinemaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Showtime>(entity =>
            {
                entity.Property(s => s.StartTime)
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Local));
                entity.Property(s => s.EndTime)
                    .HasConversion(
                        v => v,
                        v => DateTime.SpecifyKind(v, DateTimeKind.Local));
            });

            // ── ShowtimeSeat ──────────────────────────────────
            builder.Entity<ShowtimeSeat>(entity =>
            {
                entity.HasKey(ss => ss.Id);
                entity.HasOne(ss => ss.Showtime)
                    .WithMany(s => s.ShowtimeSeats)
                    .HasForeignKey(ss => ss.ShowtimeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ss => ss.Seat)
                    .WithMany(s => s.ShowtimeSeats)
                    .HasForeignKey(ss => ss.SeatId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ss => ss.LockedByUser)
                    .WithMany()
                    .HasForeignKey(ss => ss.LockedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Booking ───────────────────────────────────────
            builder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.BookingReference).IsRequired().HasMaxLength(20);
                entity.Property(b => b.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(b => b.ConvenienceFee).HasColumnType("decimal(18,2)");
                entity.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasIndex(b => b.BookingReference).IsUnique();
                entity.HasOne(b => b.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.Showtime)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(b => b.ShowtimeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── BookingSeat ───────────────────────────────────
            builder.Entity<BookingSeat>(entity =>
            {
                entity.HasKey(bs => bs.Id);
                entity.Property(bs => bs.PricePaid).HasColumnType("decimal(18,2)");
                entity.HasOne(bs => bs.Booking)
                    .WithMany(b => b.BookingSeats)
                    .HasForeignKey(bs => bs.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(bs => bs.Seat)
                    .WithMany(s => s.BookingSeats)
                    .HasForeignKey(bs => bs.SeatId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Payment ───────────────────────────────────────
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(p => p.Booking)
                    .WithOne(b => b.Payment)
                    .HasForeignKey<Payment>(p => p.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Review ────────────────────────────────────────
            builder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Comment).IsRequired();
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Movie)
                    .WithMany(m => m.Reviews)
                    .HasForeignKey(r => r.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── UserFavourite ─────────────────────────────────
            builder.Entity<UserFavourite>(entity =>
            {
                // Composite primary key
                entity.HasKey(f => new { f.UserId, f.MovieId });
                entity.HasOne(f => f.User)
                    .WithMany(u => u.Favourites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.Movie)
                    .WithMany(m => m.Favourites)
                    .HasForeignKey(f => f.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
