using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
//using CineBook.Infrastructure.Jobs;
using CineBook.Infrastructure.Persistence;
using CineBook.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CineBook.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Services
            services.AddScoped<ISmsService, SmsService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ICinemaService, CinemaService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IHallService, HallService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IShowtimeService, ShowtimeService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IMovieService, MovieService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IFavouriteService, FavouriteService>();

            return services;
        }
    }
}