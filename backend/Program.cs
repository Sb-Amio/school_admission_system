using backend.Data;
using Microsoft.EntityFrameworkCore;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.Configure<HoldSettings>(options =>
{
    options.DurationSeconds =
        int.TryParse(
            builder.Configuration["HOLD_DURATION_SECONDS"],
            out var seconds
        ) && seconds > 0
            ? seconds
            : builder.Configuration.GetValue<int>(
                "HoldDurationSeconds", 600);
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHostedService<HoldExpirationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();
}


app.UseCors("ReactFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

public class HoldSettings
{
    public int DurationSeconds { get; set; } = 600;
}
