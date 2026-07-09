using CreditFlow.Application.Applicants.Commands.RegisterApplicant;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditFlow.Api.Controllers
{
	[ApiController]
	[Route("api/v1/applicants")]
	public sealed class ApplicantsController : ControllerBase
	{
		private readonly IMediator _mediator; 

		public ApplicantsController(IMediator mediator)
		{
			_mediator = mediator; 
		}

		[HttpPost]
		[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> Register([FromBody] RegisterApplicantCommand command, CancellationToken cancellationToken)
		{
			var applicantId = await _mediator.Send(command, cancellationToken);

			return CreatedAtAction(nameof(Register), new { id = applicantId }, applicantId); 
		}


	}
}
