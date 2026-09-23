using learning_user_service.Data;
using learning_user_service.Infrastructure;
using learning_user_service.Repositories;
using learning_user_service.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// 1. Configure Serilog to write to Console and a daily rolling .txt file
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
// 2. Instruct the Web API host to replace default logger with Serilog
builder.Host.UseSerilog();



var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")!;

// Configure Entity Framework Core to use PostgreSQL with the provided connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options
    .UseNpgsql(postgresConnectionString)
    .UseSnakeCaseNamingConvention() // case-insensitive naming convention for PostgreSQL
);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // browsable Swagger-style UI at /scalar/v1

    // Apply migrations automatically in development environment
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Apply any pending migrations to the database
    await dbContext.Database.MigrateAsync();
}

// Adds middleware to handle exceptions globally and return standardized error responses
app.UseExceptionHandler();

// Redirects HTTP requests to HTTPS, ensuring secure communication
app.UseHttpsRedirection();

// This will log HTTP requests and responses, including status codes and execution times
app.UseSerilogRequestLogging();

// Adds authorization middleware to the request pipeline, enabling role-based access control
app.UseAuthorization();

// Maps controller endpoints to the request pipeline,
// allowing the application to respond to API requests
app.MapControllers();
app.MapHealthChecks("/health");


try
{
    Log.Information("Starting up the Web API execution pipeline.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush(); // Safely flushes remaining log buffers before shutting down
}
