
# School Admission System

A full-stack School Admission System.

The application allows students to apply for admission, temporarily reserve seats, confirm admission through simulated payments, and track their applications.

## Technologies

- **Backend:** ASP.NET Core Web API (.NET 8)
- **Database:** PostgreSQL 16
- **ORM:** Entity Framework Core
- **Frontend:** React, TypeScript, Vite
- **Containerization:** Docker Compose
- **Backend Testing:** xUnit

## Features

- Create classes with limited seats
- View classes and available seats
- Apply using a 17-digit Birth Registration Number
- Temporarily reserve admission seats
- Confirm admission through simulated payment
- Cancel active applications
- Automatically expire unpaid seat reservations
- Prevent seat overbooking during concurrent requests
- View student application history
- Automatically refresh seat availability

## System Requirements

### Running with Docker

- Docker Desktop
- Docker Compose

### Local Development

- .NET SDK 8.0
- Node.js 20 or later
- PostgreSQL 16

## How to Run

1. Clone the repository:

    git clone https://github.com/Sb-Amio/school_admission_system.git

2. Open the project directory:

    cd school_admission_system

3. Copy `.env.example` to `.env`.

4. Set the database password and API URL in `.env`:

    POSTGRES_PASSWORD=your_database_password
    VITE_API_URL=http://localhost:8080

5. Build and start the application:

    docker compose up -d --build

## Application URLs

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:8080 |
| Swagger (Development) | http://localhost:8080/swagger |

## Admission Workflow

1. An administrator creates a class with a specified number of seats.
2. A student enters their 17-digit Birth Registration Number.
3. The student selects a class and submits an application.
4. The system temporarily reserves a seat with `HELD` status.
5. The student can confirm admission using the simulated payment button.
6. If payment is not completed before the hold expires, the seat becomes available again.
7. The student can also cancel an active reservation.

## Application Status

| Status | Description |
|---|---|
| `HELD` | Seat is temporarily reserved |
| `CONFIRMED` | Admission has been confirmed |
| `CANCELLED` | Application was cancelled |
| `EXPIRED` | Reservation expired before payment |

The default hold duration is 600 seconds and can be configured using `HOLD_DURATION_SECONDS`.

## Student Identification

Students are identified through the `X-Student-Id` HTTP request header.

The Student ID must contain exactly 17 digits.

No authentication system is implemented.

## Backend API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/classes` | Create a new class |
| GET | `/classes` | Get all classes |
| GET | `/classes/{classId}` | Get class details |
| POST | `/classes/{classId}/holds` | Apply for admission |
| GET | `/holds/{holdId}` | Get hold details |
| POST | `/holds/{holdId}/confirm` | Confirm admission |
| DELETE | `/holds/{holdId}` | Cancel an active hold |
| GET | `/me/holds` | View student applications |

## Running Backend Tests

With the Docker services running, execute:

    dotnet test tests/BackendTests/BackendTests.csproj

The .NET 8 SDK is required to execute the tests locally.

## Stop Application

    docker compose down

PostgreSQL data is retained using a persistent Docker volume.

## Additional Documentation

- `ASSUMPTIONS.md` - Project assumptions and implementation decisions
- `AI_USAGE.md` - AI assistance disclosure
