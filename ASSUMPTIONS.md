
# Project Assumptions

## 1. Student Identification

Students are identified using a 17-digit birth
registration number provided through the
X-Student-Id request header.

Authentication is not implemented.

## 2. Class and Shift

Each class and shift combination is treated
as a separate admission class.

Example:
- Class 6 - Morning Shift
- Class 6 - Day Shift

## 3. Seat Reservation

A successful application creates a temporary
seat hold.

The default duration is 600 seconds (10 Minutes).

## 4. Admission Confirmation

Clicking "Pay Admission Fee" simulates a
successful payment and confirms admission.

No payment gateway is integrated.

## 5. Expired Applications

An expired hold releases its reserved seat.

A background service updates expired hold
statuses in the database.

## 6. Duplicate Applications

A student cannot have multiple active or
confirmed applications for the same class.

## 7. Concurrency

PostgreSQL transactions and row-level locking
are used to prevent seat overbooking.

## 8. Data Persistence

PostgreSQL data is persisted using a Docker
volume so that container restarts do not
remove admission records.
