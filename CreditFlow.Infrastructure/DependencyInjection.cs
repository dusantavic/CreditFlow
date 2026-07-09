
using CreditFlow.Application.Common.Interfaces;
using CreditFlow.Domain.Underwriting;
using CreditFlow.Infrastructure.ExternalServices;
using CreditFlow.Infrastructure.Persistence;
using CreditFlow.Infrastructure.Persistence.Queries;
using CreditFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

			//@dusan this is temporary
			services.AddScoped<ICreditBureauService, SimulatedCreditBureauService>();

			return services; 
		}
	}
}
