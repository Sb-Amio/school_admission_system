
# School Admission System

A full-stack School Admission System.

## Technologies

- Backend: ASP.NET Core Web API (.NET 8)
- Database: PostgreSQL
- ORM: Entity Framework Core
- Frontend: React, TypeScript, Vite
- Containerization: Docker Compose
- Backend Testing: xUnit

## Features

- Create classes with limited seats
- View available classes
- Apply for admission using a 17-digit Student ID
- Temporary seat reservation
- Confirm admission
- Cancel applications
- Automatic hold expiration
- Concurrent seat allocation protection
- View student applications
- Live seat availability updates

## Requirements

- Docker Desktop with Docker Compose

## How to Run

1. Clone this repository.
2. Open the project root directory.
3. Copy `.env.example` to `.env`.
4. Set POSTGRES_PASSWORD in `.env`.
5. Run:

```bash
docker compose up -d --build
```

## Application URLs

Frontend:
http://localhost:5173

Backend API:
http://localhost:8080

Swagger (development):
http://localhost:8080/swagger

## Running Backend Tests

With Docker services running:

```bash
dotnet test tests/BackendTests/BackendTests.csproj
```

This test command requires .NET 8 SDK on the machine
running the tests.

## Stop Application

```bash
docker compose down
```

Database data is retained using a Docker volume.

