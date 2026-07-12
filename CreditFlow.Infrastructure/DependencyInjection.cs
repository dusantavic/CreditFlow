
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Infrastructure.ExternalServices;
using CreditFlow.Infrastructure.Persistence;
using CreditFlow.Infrastructure.Persistence.Queries;
using CreditFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace CreditFlow.Infrastructure
{
	/// <summary>
	/// Registers everything the Infrastructure layer owns: the DbContext,
	/// repositories, the query service, external service implementations, and
	/// options bound from configuration. Mirrors Application's
	/// DependencyInjection so CreditFlow.Api's Program.cs wires up the whole
	/// stack with a couple of clearly named calls.
	/// </summary>
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure( 
			this IServiceCollection services, 
			IConfiguration configuration
			)
		{
			services.AddDbContext<CreditFlowDbContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))); 

			services.AddScoped<IApplicantRepository, ApplicantRepository>();
			services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
			services.AddScoped<ILoanApplicationQueryService, LoanApplicationQueryService>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();

			services.AddOptions<UnderwritingPolicyOptions>()
				.Bind(configuration.GetSection("UnderwritingPolicy"))
				.ValidateOnStart();

			services.AddScoped<IUnderwritingPolicy>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<UnderwritingPolicyOptions>>().Value;
				return new UnderwritingPolicy(options);
			});

			var creditBureauBaseUrl = configuration["CreditBureau:BaseUrl"]
				?? throw new InvalidOperationException(
					"Configuration 'CreditBureau:BaseUrl' is missing. See it via appsettings.json r the CreditBureau__BaseUrl environment variable (Docker/production).");

			services.AddHttpClient<ICreditBureauService, HttpCreditBureauService>(client =>
			{
				client.BaseAddress = new Uri(creditBureauBaseUrl);
				client.Timeout = TimeSpan.FromSeconds(10);
			})
				.AddResilienceHandler("credit-bureau-resilience", builder =>
				{
					// Retries transient failures (network erros, 5xx, timeouts) up to 3
					// times with exponential backoff, then opens a circuit breaker if 
					// failures keep happening - prevents hammering a struggling downstream
					// servise and gives it room to recover. 
					builder.AddRetry(new HttpRetryStrategyOptions
					{
						MaxRetryAttempts = 3,
						BackoffType = DelayBackoffType.Exponential,
						Delay = TimeSpan.FromMilliseconds(300)
					});

					builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
					{
						FailureRatio = 0.5,
						SamplingDuration = TimeSpan.FromSeconds(30),
						MinimumThroughput = 5,
						BreakDuration = TimeSpan.FromSeconds(15)
					});

					builder.AddTimeout(TimeSpan.FromSeconds(5));
				});


			return services; 
		}
	}
}
