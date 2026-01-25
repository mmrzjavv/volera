# Voice Call App Backend

A highly professional .NET 8 backend for a Voice Call App using DDD, CQRS, and SignalR.

## Architecture

- **Clean Architecture** with Onion principles
- **Domain-Driven Design (DDD)**
- **CQRS** with MediatR
- **Real-time communication** with SignalR
- **Modular monolith** ready for microservices

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SignalR
- MediatR
- FluentValidation
- AutoMapper
- JWT Authentication
- BCrypt for password hashing
- xUnit for testing

## Project Structure

```
src/
├── Core.Domain/          # Domain entities, events, interfaces
├── Core.Application/     # CQRS handlers, DTOs, validators
├── Infrastructure/       # EF Core, repositories, security
├── WebAPI/               # Controllers, hubs, middleware
└── Shared/               # Common utilities

tests/
└── Tests/                # Unit and integration tests
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (or change connection string for other providers)

### Setup

1. Clone the repository
2. Navigate to the backend directory
3. Restore packages: `dotnet restore`
4. Update connection string in `src/WebAPI/appsettings.json`
5. Run migrations: `dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI`
6. Run the application: `dotnet run --project src/WebAPI`

### API Endpoints

- `POST /api/auth/register` - Register user
- `POST /api/auth/login` - Login
- `GET /api/user/profile` - Get user profile
- `PUT /api/user/profile` - Update profile
- `PUT /api/user/change-password` - Change password
- `POST /api/call/initiate` - Initiate call
- `POST /api/call/{id}/accept` - Accept call
- `POST /api/call/{id}/end` - End call
- `GET /api/call/history` - Get call history

### SignalR Hub

- Hub URL: `/callHub`
- Events: `CallInitiated`, `CallAccepted`, `CallEnded`, `MissedCall`

### Testing

Run tests: `dotnet test`

## Features

- User registration and authentication with JWT
- Profile management
- Voice call initiation, acceptance, and ending
- Real-time notifications via SignalR
- Call history
- Input validation with FluentValidation
- Global exception handling
- CQRS pattern implementation