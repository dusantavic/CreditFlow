using CreditFlow.Application.Common.Models;
using CreditFlow.Application.LoanApplications.Commands.ApproveLoanApplication;
using CreditFlow.Application.LoanApplications.Commands.CancelLoanApplication;
using CreditFlow.Application.LoanApplications.Commands.DisburseLoan;
using CreditFlow.Application.LoanApplications.Commands.RejectLoanApplication;
using CreditFlow.Application.LoanApplications.Commands.RunUnderwriting;
using CreditFlow.Application.LoanApplications.Commands.SubmitLoanApplication;
using CreditFlow.Application.LoanApplications.Queries.GetApplicationById;
using CreditFlow.Application.LoanApplications.Queries.GetLoanApplications;
using CreditFlow.Domain.LoanApplications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditFlow.Api.Controllers
{
	[ApiController]
	[Route("api/v1/loan-applications")]
	public sealed class LoanApplicationsController : ControllerBase
	{
		private readonly IMediator _mediator; 

		public LoanApplicationsController(IMediator mediator)
		{
			_mediator = mediator; 
		}

		[HttpPost]
		[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Submit([FromBody] SubmitLoanApplicationCommand command, CancellationToken cancellationToken)
		{
			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, id); 
		}

		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(LoanApplicationDetailsDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<LoanApplicationDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetLoanApplicationByIdQuery(id), cancellationToken);
			return Ok(result); 
		}

		[HttpGet]
		[ProducesResponseType(typeof(PagedResult<LoanApplicationSummaryDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetAll(
					[FromQuery] LoanApplicationStatus? status,
					[FromQuery] Guid? applicantId,
					[FromQuery] DateTime? submittedFromUtc,
					[FromQuery] DateTime? submittedToUtc,
					[FromQuery] int pageNumber = 1,
					[FromQuery] int pageSize = 20,
					CancellationToken cancellationToken = default)
		{
			var query = new GetLoanApplicationsQuery(
				status, applicantId, submittedFromUtc, submittedToUtc, pageNumber, pageSize);

			var result = await _mediator.Send(query, cancellationToken);
			return Ok(result); 
		}

		[HttpPost("{id:guid}/underwrite")]
		[ProducesResponseType(typeof(UnderwritingResultDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<ActionResult<UnderwritingResultDto>> Underwrite(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new RunUnderwritingCommand(id), cancellationToken);
			return Ok(result); 
		}

		[HttpPost("{id:guid}/approve")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLoanApplicationRequest request, CancellationToken cancellationToken)
		{
			var command = new ApproveLoanApplicationCommand(id, request.ApprovedAmount, request.Currency, request.AnnualInterestRatePercentage, request.TermMonths);

			await _mediator.Send(command, cancellationToken);
			return Ok(); 
		}


		[HttpPost("{id:guid}/reject")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<IActionResult> Reject(
				 Guid id,
				 [FromBody] RejectLoanApplicationRequest request,
				 CancellationToken cancellationToken)
		{
			await _mediator.Send(new RejectLoanApplicationCommand(id, request.Reasons), cancellationToken);
			return Ok();
		}

		[HttpPost("{id:guid}/disburse")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<IActionResult> Disburse(Guid id, CancellationToken cancellationToken)
		{
			await _mediator.Send(new DisburseLoanCommand(id), cancellationToken);
			return Ok();
		}

		[HttpPost("{id:guid}/cancel")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
		{
			await _mediator.Send(new CancelLoanApplicationCommand(id), cancellationToken);
			return Ok();
		}
	}

	/// <summary>
	/// Request body shapes for endpoints that need more than a route parameter.
	/// Kept here, next to the controller, since they're pure HTTP-layer
	/// concerns (JSON body shape) rather than Application-layer commands —
	/// the controller maps each into its corresponding MediatR command.
	/// </summary>
	public sealed record ApproveLoanApplicationRequest(decimal ApprovedAmount, string Currency, decimal AnnualInterestRatePercentage, int TermMonths);

	public sealed record RejectLoanApplicationRequest(IReadOnlyList<string> Reasons);

}
