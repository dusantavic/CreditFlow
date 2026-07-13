using CreditFlow.Application.Common.Behaviors;
using CreditFlow.Application.Common.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CreditFlow.Application.UnitTests.Common.Behaviors
{
	public class UnhandledExceptionBehaviorTests
	{
		public sealed record TestRequest(string Name) : IRequest<string>;

		private readonly ILogger<UnhandledExceptionBehavior<TestRequest, string>> _logger =
			Substitute.For<ILogger<UnhandledExceptionBehavior<TestRequest, string>>>();
		private readonly UnhandledExceptionBehavior<TestRequest, string> _behavior;

		public UnhandledExceptionBehaviorTests()
		{
			_behavior = new UnhandledExceptionBehavior<TestRequest, string>(_logger);
		}

		[Fact]
		public async Task Handle_WhenNextSucceeds_ReturnsResponseWithoutLogging()
		{
			RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

			var result = await _behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

			result.Should().Be("handled");
			_logger.ReceivedCalls().Should().BeEmpty();
		}

		[Fact]
		public async Task Handle_WhenNextThrowsRequestValidationException_PropagatesWithoutLogging()
		{
			var validationException = new RequestValidationException(
				new Dictionary<string, string[]> { ["Name"] = new[] { "Name is required." } });
			RequestHandlerDelegate<string> next = _ => throw validationException;

			var act = () => _behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

			(await act.Should().ThrowAsync<RequestValidationException>())
				.Which.Should().BeSameAs(validationException);
			_logger.ReceivedCalls().Should().BeEmpty();
		}

		[Fact]
		public async Task Handle_WhenNextThrowsNotFoundException_PropagatesWithoutLogging()
		{
			var notFoundException = new NotFoundException("LoanApplication", Guid.NewGuid());
			RequestHandlerDelegate<string> next = _ => throw notFoundException;

			var act = () => _behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

			(await act.Should().ThrowAsync<NotFoundException>())
				.Which.Should().BeSameAs(notFoundException);
			_logger.ReceivedCalls().Should().BeEmpty();
		}

		[Fact]
		public async Task Handle_WhenNextThrowsUnexpectedException_LogsErrorAndRethrows()
		{
			var unexpected = new InvalidOperationException("database exploded");
			RequestHandlerDelegate<string> next = _ => throw unexpected;

			var act = () => _behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

			(await act.Should().ThrowAsync<InvalidOperationException>())
				.Which.Should().BeSameAs(unexpected);

			var logCall = _logger.ReceivedCalls().Should().ContainSingle().Subject;
			logCall.GetMethodInfo().Name.Should().Be("Log");
			logCall.GetArguments()[0].Should().Be(LogLevel.Error);
		}
	}
}
