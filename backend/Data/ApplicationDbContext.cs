using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options
    ) : base(options)
    {
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<SeatHold> Holds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasIndex(c => c.Name)
                  .IsUnique();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Classes_TotalSeats",
                    "\"TotalSeats\" > 0"
                )
            );
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.StudentId)
                  .IsRequired()
                  .HasMaxLength(17);

            entity.Property(h => h.Status)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.HasOne(h => h.Class)
                  .WithMany(c => c.Holds)
                  .HasForeignKey(h => h.ClassId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(h => new
            {
                h.ClassId,
                h.Status,
                h.ExpiresAt
            });

            entity.HasIndex(h => h.StudentId);
        });
    }
}
