using CreditFlow.Api.Middleware;
using CreditFlow.Application;
using CreditFlow.Infrastructure;
using CreditFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Fail fast if the connection string is missing entirely — better to
// crash on startup with a clear message than fail confusingly on the
// first database call.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException(
		"Connection string 'DefaultConnection' is not configured. " +
		"Set it via User Secrets (local development) or the " +
		"ConnectionStrings__DefaultConnection environment variable (Docker/production).");
}

var app = builder.Build();

// Applies any pending EF Core migrations automatically on startup.
// A real production system would more likely run migrations as an explicit, separate deployment step
// rather than on every application start.
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<CreditFlowDbContext>();
	dbContext.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>(); 


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
