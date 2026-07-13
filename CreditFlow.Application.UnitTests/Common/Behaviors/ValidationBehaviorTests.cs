using CreditFlow.Application.Common.Behaviors;
using CreditFlow.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace CreditFlow.Application.UnitTests.Common.Behaviors
{
	public class ValidationBehaviorTests
	{
		public sealed record TestRequest(string Name) : IRequest<string>;

		private sealed class TestRequestValidator : AbstractValidator<TestRequest>
		{
			public TestRequestValidator()
			{
				RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
			}
		}

		[Fact]
		public async Task Handle_WithNoValidatorsRegistered_PassesThroughToNext()
		{
			var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
			var nextCalled = false;
			RequestHandlerDelegate<string> next = _ =>
			{
				nextCalled = true;
				return Task.FromResult("handled");
			};

			var result = await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

			nextCalled.Should().BeTrue();
			result.Should().Be("handled");
		}

		[Fact]
		public async Task Handle_WithPassingValidator_PassesThroughToNext()
		{
			var behavior = new ValidationBehavior<TestRequest, string>(new[] { new TestRequestValidator() });
			RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

			var result = await behavior.Handle(new TestRequest("valid name"), next, CancellationToken.None);

			result.Should().Be("handled");
		}

		[Fact]
		public async Task Handle_WithFailingValidator_ThrowsRequestValidationExceptionWithFailureDetails()
		{
			var behavior = new ValidationBehavior<TestRequest, string>(new[] { new TestRequestValidator() });
			RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

			var act = () => behavior.Handle(new TestRequest(""), next, CancellationToken.None);

			var exception = (await act.Should().ThrowAsync<RequestValidationException>()).Which;
			exception.Errors.Should().ContainKey("Name")
				.WhoseValue.Should().Contain("Name is required.");
		}

		[Fact]
		public async Task Handle_WithFailingValidator_NeverCallsNext()
		{
			var behavior = new ValidationBehavior<TestRequest, string>(new[] { new TestRequestValidator() });
			var nextCalled = false;
			RequestHandlerDelegate<string> next = _ =>
			{
				nextCalled = true;
				return Task.FromResult("handled");
			};

			var act = () => behavior.Handle(new TestRequest(""), next, CancellationToken.None);

			await act.Should().ThrowAsync<RequestValidationException>();
			nextCalled.Should().BeFalse();
		}
	}
}
