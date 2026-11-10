
# AI Usage Disclosure

## AI Tool Used

ChatGPT (OpenAI)

## Areas of Assistance

- Understanding the assignment requirements
- Planning the project architecture
- Generating and reviewing backend API code
- Creating database models and EF Core configuration
- Implementing React frontend components
- Writing automated tests
- Docker configuration
- Troubleshooting development errors

## Problems Identified and Resolved

### TypeScript Type Errors

Some initially generated frontend code used
JavaScript without TypeScript declarations.

The API service was converted to TypeScript,
and explicit response types were added.

### Backend Build Error

A backend executable was locked by an
existing running process.

The process was stopped before rebuilding.

## Verification

Backend API endpoints were tested manually.

Concurrency behavior was checked using
simultaneous HTTP requests.

Automated backend tests were created using xUnit.

Frontend functionality was checked through
the browser.
