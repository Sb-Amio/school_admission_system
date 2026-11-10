using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace backend.Controllers;

[ApiController]
public class HoldsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly int _holdDurationSeconds;

    public HoldsController(
        ApplicationDbContext context,
        IOptions<HoldSettings> settings)
    {
        _context = context;
        _holdDurationSeconds = settings.Value.DurationSeconds;
    }

    [HttpPost("classes/{classId}/holds")]
    public async Task<IActionResult> CreateHold(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                error = "Student ID must contain exactly 17 digits"
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();


        var schoolClass = await _context.Classes
            .FromSqlInterpolated(
                $"SELECT * FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (schoolClass == null)
        {
            return NotFound(new
            {
                error = "Class not found"
            });
        }

        var now = DateTime.UtcNow;

        bool alreadyApplied = await _context.Holds
            .AnyAsync(h =>
                h.ClassId == classId &&
                h.StudentId == studentId &&
                (
                    h.Status == "CONFIRMED" ||
                    (h.Status == "HELD" && h.ExpiresAt > now)
                ));

        if (alreadyApplied)
        {
            return Conflict(new
            {
                error = "Student already has an active or confirmed application"
            });
        }

        int heldSeats = await _context.Holds
            .CountAsync(h =>
                h.ClassId == classId &&
                h.Status == "HELD" &&
                h.ExpiresAt > now);

        int confirmedSeats = await _context.Holds
            .CountAsync(h =>
                h.ClassId == classId &&
                h.Status == "CONFIRMED");

        int availableSeats =
            schoolClass.TotalSeats - heldSeats - confirmedSeats;

        if (availableSeats <= 0)
        {
            return Conflict(new
            {
                error = "No seats available"
            });
        }

        var hold = new SeatHold
        {
            Id = "h_" + Guid.NewGuid().ToString("N"),
            ClassId = classId,
            StudentId = studentId,
            Status = "HELD",
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_holdDurationSeconds)
        };

        _context.Holds.Add(hold);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return StatusCode(201, new
        {
            holdId = hold.Id,
            classId = hold.ClassId,
            studentId = hold.StudentId,
            status = hold.Status,
            expiresAt = hold.ExpiresAt
        });
    }

    [HttpGet("holds/{holdId}")]
    public async Task<IActionResult> GetHold(string holdId)
    {
        var hold = await _context.Holds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId);

        if (hold == null)
        {
            return NotFound(new
            {
                error = "Hold not found"
            });
        }

        var status = hold.Status;

        if (status == "HELD" &&
            hold.ExpiresAt <= DateTime.UtcNow)
        {
            status = "EXPIRED";
        }

        return Ok(new
        {
            holdId = hold.Id,
            classId = hold.ClassId,
            status = status,
            expiresAt = hold.ExpiresAt
        });
    }


    [HttpPost("holds/{holdId}/confirm")]
    public async Task<IActionResult> ConfirmHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                error = "Valid 17-digit Student ID is required"
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var hold = await _context.Holds
            .FromSqlInterpolated(
                $"SELECT * FROM \"Holds\" WHERE \"Id\" = {holdId} FOR UPDATE")
            .SingleOrDefaultAsync();

        if (hold == null || hold.StudentId != studentId)
        {
            return NotFound(new
            {
                error = "Hold not found"
            });
        }

        if (hold.Status == "CONFIRMED")
        {
            await transaction.CommitAsync();

            return Ok(new
            {
                holdId = hold.Id,
                status = "CONFIRMED"
            });
        }

        if (hold.Status != "HELD")
        {
            return Conflict(new
            {
                error = "Hold is not active"
            });
        }

        if (hold.ExpiresAt <= DateTime.UtcNow)
        {
            hold.Status = "EXPIRED";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Conflict(new
            {
                error = "Hold has expired"
            });
        }

        hold.Status = "CONFIRMED";

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            holdId = hold.Id,
            status = "CONFIRMED"
        });
    }

    [HttpDelete("holds/{holdId}")]
    public async Task<IActionResult> CancelHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                error = "Valid 17-digit Student ID is required"
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var hold = await _context.Holds
            .FromSqlInterpolated(
                $"SELECT * FROM \"Holds\" WHERE \"Id\" = {holdId} FOR UPDATE")
            .SingleOrDefaultAsync();

        if (hold == null || hold.StudentId != studentId)
        {
            return NotFound(new
            {
                error = "Hold not found"
            });
        }

        if (hold.Status != "HELD" ||
            hold.ExpiresAt <= DateTime.UtcNow)
        {
            return Conflict(new
            {
                error = "Only an active hold can be cancelled"
            });
        }

        hold.Status = "CANCELLED";

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    [HttpGet("me/holds")]
    public async Task<IActionResult> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                error = "Valid 17-digit Student ID is required"
            });
        }

        var now = DateTime.UtcNow;

        var holds = await _context.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                holdId = h.Id,
                classId = h.ClassId,
                className = h.Class.Name,
                status = h.Status == "HELD" &&
                         h.ExpiresAt <= now
                    ? "EXPIRED"
                    : h.Status,
                expiresAt = h.ExpiresAt
            })
            .ToListAsync();

        return Ok(holds);
    }
    

}
