using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClassesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.TotalSeats <= 0)
        {
            return BadRequest(new
            {
                error = "Invalid class name or total seats"
            });
        }

        var schoolClass = new SchoolClass
        {
            Id = "c_" + Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats
        };

        bool exists = await _context.Classes
            .AnyAsync(c => c.Name == schoolClass.Name);

        if (exists)
        {
            return Conflict(new
            {
                error = "Class already exists"
            });
        }

        _context.Classes.Add(schoolClass);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                error = "Class already exists"
            });
        }

        return StatusCode(201, new
        {
            id = schoolClass.Id,
            name = schoolClass.Name,
            totalSeats = schoolClass.TotalSeats
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var now = DateTime.UtcNow;

        var classes = await _context.Classes
            .AsNoTracking()
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                totalSeats = c.TotalSeats,

                heldSeats = c.Holds.Count(h =>
                    h.Status == "HELD"
                    && h.ExpiresAt > now),

                confirmedSeats = c.Holds.Count(h =>
                    h.Status == "CONFIRMED")
            })
            .ToListAsync();

        var result = classes.Select(c => new
        {
            c.id,
            c.name,
            c.totalSeats,
            c.heldSeats,
            c.confirmedSeats,

            availableSeats = c.totalSeats
                - c.heldSeats
                - c.confirmedSeats
        });

        return Ok(result);
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetClass(
        string classId)
    {
        var now = DateTime.UtcNow;

        var schoolClass = await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                totalSeats = c.TotalSeats,

                heldSeats = c.Holds.Count(h =>
                    h.Status == "HELD"
                    && h.ExpiresAt > now),

                confirmedSeats = c.Holds.Count(h =>
                    h.Status == "CONFIRMED")
            })
            .FirstOrDefaultAsync();

        if (schoolClass == null)
        {
            return NotFound(new
            {
                error = "Class not found"
            });
        }

        return Ok(new
        {
            schoolClass.id,
            schoolClass.name,
            schoolClass.totalSeats,
            schoolClass.heldSeats,
            schoolClass.confirmedSeats,

            availableSeats = schoolClass.totalSeats
                - schoolClass.heldSeats
                - schoolClass.confirmedSeats
        });
    }
}

public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}
