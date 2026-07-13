using CreditFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace CreditFlow.Api.IntegrationTests.Infrastructure
{
	/// <summary>
	/// Boots the real API against an ephemeral PostgreSQL container
	/// (same image as docker-compose uses), with only one service replaced:
	/// ICreditBureauService, swapped for a deterministic in-memory fake so
	/// tests control underwriting outcomes and never call the real
	/// CreditBureau.Api. Migrations run automatically via the API's own
	/// startup logic once the host is first built.
	/// </summary>
	public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
	{
		private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.Build();

		public FakeCreditBureauService CreditBureau { get; } = new();

		public async Task InitializeAsync()
		{
			await _dbContainer.StartAsync();
		}

		async Task IAsyncLifetime.DisposeAsync()
		{
			await base.DisposeAsync(); // stop the host (flushes Serilog) before killing its database
			await _dbContainer.DisposeAsync();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());

			// AddInfrastructure builds a Uri from this at startup, so it must be
			// non-empty and well-formed even though the HTTP client it configures
			// is never used — ICreditBureauService is replaced below.
			builder.UseSetting("CreditBureau:BaseUrl", "http://credit-bureau.test.invalid");

			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<ICreditBureauService>();
				services.AddSingleton<ICreditBureauService>(CreditBureau);
			});
		}
	}

	/// <summary>
	/// Single collection shared by every integration test class: one container
	/// and one host per test run, and no parallel execution across classes
	/// (they share a database and the fake bureau's configured report).
	/// </summary>
	[CollectionDefinition(Name)]
	public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
	{
		public const string Name = "Integration";
	}
}
