using CreditFlow.Application;
using CreditFlow.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); 

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
