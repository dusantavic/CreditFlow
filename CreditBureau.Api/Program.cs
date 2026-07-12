using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

// Single endpoint: simulates a third-party credit bureau lookup.
// Deliberately minimal - no layers, no CQRS, no persistence. This service exists
// only to stand in for a real, paid external bureau API that CreditFlow.Api would 
// call over HTTP in a real deployment
app.MapGet("/api/credit-report/{applicantId:guid}", (Guid applicantId) =>
{
	// Deterministic per-applicant result
	var hash = MD5.HashData(applicantId.ToByteArray());
	var seed = BitConverter.ToInt32(hash, 0);
	var random = new Random(seed);

	var creditScore = random.Next(300, 851);
	var existingMonthlyDebt = random.Next(0, 800);

	return Results.Ok(new CreditReportResponse(creditScore, existingMonthlyDebt, "EUR")); 
});

app.Run();


public sealed record CreditReportResponse(int CreditScore, int ExistingMonthlyDebt, string Currency); 
