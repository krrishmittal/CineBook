# 🎬 CineBook - Cinema Booking Platform

A modern, full-featured cinema booking system built with **ASP.NET Core (.NET 10)** and **Razor Pages**, allowing users to browse movies, book seats, manage cinemas, and handle payments seamlessly.

---

## 📋 Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Authentication & Authorization](#authentication--authorization)
- [Database Schema](#database-schema)
- [Running the Application](#running-the-application)
- [Development](#development)
- [Contributors](#contributors)

---

## ✨ Features

### 👤 For Users
- ✅ Browse and search movies
- ✅ View cinema locations and showtimes
- ✅ Real-time seat selection with WebSocket support
- ✅ Book movie tickets
- ✅ Secure payment processing (Stripe integration)
- ✅ View booking history
- ✅ Add movies to favorites
- ✅ Write and read reviews
- ✅ OTP-based authentication
- ✅ Email notifications

### 🎭 For Cinema Managers
- ✅ Manage cinema details
- ✅ Create and manage halls
- ✅ Add and manage showtimes
- ✅ View bookings and revenue
- ✅ Manage seat configurations

### 👨‍💼 For Admins
- ✅ Manage all movies
- ✅ Approve cinema registrations
- ✅ View system analytics
- ✅ Manage users and roles
- ✅ System-wide reporting

---

## 🏗️ Architecture

**Clean Architecture Pattern** with layers:

```
CineBook.API              → Presentation Layer (Controllers, Views)
CineBook.Application      → Business Logic Layer (Services, DTOs)
CineBook.Infrastructure   → Data Access Layer (EF Core, Repositories)
CineBook.Domain           → Domain Layer (Entities, Enums)
```

### Key Patterns Used
- Repository Pattern
- Dependency Injection
- JWT Authentication
- Entity Framework Core (ORM)
- Async/Await
- Clean Architecture

---

## 🛠️ Tech Stack

### Backend
- **Framework**: ASP.NET Core 10.0
- **Language**: C# 14.0
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Real-time**: SignalR (Seat management)
- **Payment**: Stripe API
- **SMS/WhatsApp**: Twilio
- **Database**: SQL Server

### Frontend
- **HTML/CSS/JavaScript**
- **Tailwind CSS** (Styling)
- **Vanilla JS** (Client-side logic)
- **Fetch API** (HTTP requests)

---

## 📁 Project Structure

```
CineBook/
├── CineBook.API/                 # Presentation Layer
│   ├── Controllers/              # API & MVC Controllers
│   ├── Views/
│   │   ├── AuthView/            # Login, Register pages
│   │   ├── Admin/               # Admin dashboard
│   │   ├── Manager/             # Manager dashboard
│   │   └── Home/                # Public pages
│   ├── Hubs/                    # SignalR hubs
│   ├── Models/                  # API models
│   ├── Program.cs               # Application startup
│   └── appsettings.json         # Configuration
│
├── CineBook.Application/         # Business Logic Layer
│   ├── DTOs/
│   │   ├── Requests/            # Request DTOs
│   │   └── Responses/           # Response DTOs
│   └── Interfaces/              # Service contracts
│
├── CineBook.Infrastructure/      # Data Access Layer
│   ├── Services/                # Implementation of services
│   ├── Persistence/             # DB context, migrations
│   └── Repositories/            # Data repositories
│
└── CineBook.Domain/              # Domain Layer
    ├── Entities/                # Database entities
    └── Enums/                   # Domain enums
```

---

## 📋 Prerequisites

- **.NET SDK 10.0** or later
- **SQL Server** 2019 or later (or compatible)
- **Node.js** (for frontend tooling, if needed)
- **Git**
- **Visual Studio 2026** or VS Code

---

## 🚀 Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/krrishmittal/CineBook.git
cd CineBook
```

### 2. Restore NuGet Packages
```bash
dotnet restore
```

### 3. Update Database
```bash
# Navigate to CineBook.API directory
cd CineBook.API

# Apply migrations
dotnet ef database update
```

### 4. Configure Settings
Update `appsettings.json` in `CineBook.API/`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Connection-String"
  },
  "JwtSettings": {
    "SecretKey": "Your-Long-Secret-Key-Min-64-Chars",
    "Issuer": "CineBook",
    "Audience": "CineBookUsers",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### 5. Build the Solution
```bash
dotnet build
```

---

## ⚙️ Configuration

### JWT Settings
```json
"JwtSettings": {
  "SecretKey": "your-long-secret-key-for-hmac-512-encryption",
  "Issuer": "CineBook",
  "Audience": "CineBookUsers",
  "AccessTokenExpiryMinutes": 60,
  "RefreshTokenExpiryDays": 7
}
```

### Third-Party Services
- **Stripe**: Payment processing
- **Twilio**: SMS and WhatsApp notifications
- **Database**: SQL Server

Add credentials to `appsettings.json` or use **User Secrets** for sensitive data.

---

## 🔌 API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Auth/register/user` | Register regular user |
| POST | `/api/Auth/register/cinema-manager` | Register cinema manager |
| POST | `/api/Auth/login` | Login user |
| POST | `/api/Auth/forgot-password` | Request password reset |
| POST | `/api/Auth/reset-password` | Reset password with token |
| POST | `/api/Auth/logout` | Logout user |

### Movies
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Movie` | Get all movies |
| GET | `/api/Movie/{id}` | Get movie details |
| POST | `/api/Movie` | Create movie (Admin) |
| PUT | `/api/Movie/{id}` | Update movie (Admin) |
| DELETE | `/api/Movie/{id}` | Delete movie (Admin) |

### Cinemas
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Cinema` | Get all cinemas |
| GET | `/api/Cinema/{id}` | Get cinema details |
| POST | `/api/Cinema` | Create cinema |
| PUT | `/api/Cinema/{id}` | Update cinema |

### Bookings
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Booking` | Create booking |
| GET | `/api/Booking/{id}` | Get booking details |
| GET | `/api/Booking/user/{userId}` | Get user bookings |
| DELETE | `/api/Booking/{id}` | Cancel booking |

### Reviews
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Review` | Add review |
| GET | `/api/Review/movie/{movieId}` | Get movie reviews |
| DELETE | `/api/Review/{id}` | Delete review |

### Favorites
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Favourite` | Add to favorites |
| GET | `/api/Favourite/user/{userId}` | Get user favorites |
| DELETE | `/api/Favourite/{id}` | Remove from favorites |

---

## 🔐 Authentication & Authorization

### JWT Token Flow
1. **Login** → Issue access token (1 hour) + refresh token (7 days)
2. **Refresh** → Use refresh token to get new access token
3. **Logout** → Clear tokens

### Role-Based Access Control
```csharp
[Authorize(Roles = "Admin")]          // Admin only
[Authorize(Roles = "CinemaManager")]  // Cinema Manager only
[Authorize]                            // Authenticated users
[AllowAnonymous]                       // Public endpoints
```

### Roles
- **User** - Regular cinema-goer
- **CinemaManager** - Cinema administration
- **Admin** - System administration

---

## 📊 Database Schema

### Core Entities
- **ApplicationUser** - User accounts
- **Movie** - Movie catalog
- **Cinema** - Cinema locations
- **Hall** - Theater halls
- **Showtime** - Movie showtimes
- **Seat** - Hall seats
- **Booking** - User bookings
- **BookingSeat** - Booked seats
- **Payment** - Payment records
- **Review** - Movie reviews
- **UserFavourite** - Favorite movies
- **RefreshToken** - Token management

---

## ▶️ Running the Application

### Development Mode
```bash
cd CineBook.API
dotnet run
```

### With Watch Mode (Auto-reload)
```bash
dotnet watch run
```

### Production Build
```bash
dotnet publish -c Release -o ./publish
```

### Access the Application
- **Frontend**: `http://localhost:5000` (or configured port)
- **Swagger API Docs**: `http://localhost:5000/swagger`

---

## 👨‍💻 Development

### Adding New Migrations
```bash
dotnet ef migrations add MigrationName -p CineBook.Infrastructure -s CineBook.API
dotnet ef database update
```

### Running Tests
```bash
dotnet test
```

### Code Standards
- Follow **Clean Code** principles
- Use **async/await** for I/O operations
- Apply **SOLID** principles
- Use **dependency injection**
- Write meaningful commit messages

---

## 📞 Support & Contact

- **GitHub**: [https://github.com/krrishmittal/CineBook](https://github.com/krrishmittal/CineBook)
- **Issues**: Report bugs via GitHub Issues
- **Discussions**: Use GitHub Discussions for questions

---

## 📄 License

This project is licensed under the **MIT License**. See `LICENSE` file for details.

---

## 🙏 Contributors

- **Krrish Mittal** - Project Lead & Developer

---

## 🚀 Roadmap

- [ ] Advanced analytics dashboard
- [ ] Multi-language support
- [ ] Machine learning recommendations
- [ ] Social sharing features
- [ ] Group booking discounts
- [ ] Dynamic pricing

---

**Built with ❤️ using ASP.NET Core 10.0**
