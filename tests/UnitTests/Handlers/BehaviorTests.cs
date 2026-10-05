using Domain.Abstractions;
using Domain.Common;
using FluentValidation;
using MediatR;
using NSubstitute;
using Service.Behaviors;

namespace UnitTests.Handlers;

public sealed class BehaviorTests
{
    private sealed record SampleCommand(string Name) : IRequest<Result<string>>, ITransactionalCommand;

    private sealed class SampleValidator : AbstractValidator<SampleCommand>
    {
        public SampleValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
        }
    }

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private TransactionBehavior<SampleCommand, Result<string>> CreateTransactionBehavior() => new(unitOfWork);

    [Fact]
    public async Task Validation_failures_short_circuit_the_pipeline_with_field_details()
    {
        var behavior = new ValidationBehavior<SampleCommand, Result<string>>([new SampleValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new SampleCommand(string.Empty), _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("ok"));
        }, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("Name", result.Error.ValidationErrors!.Keys);
    }

    [Fact]
    public async Task Valid_requests_continue_down_the_pipeline()
    {
        var behavior = new ValidationBehavior<SampleCommand, Result<string>>([new SampleValidator()]);

        var result = await behavior.Handle(new SampleCommand("ok"), _ => Task.FromResult(Result<string>.Success("done")), CancellationToken.None);

        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task A_successful_result_commits_the_transaction()
    {
        var result = await CreateTransactionBehavior().Handle(new SampleCommand("x"), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await unitOfWork.Received(1).BeginAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failed_result_rolls_the_transaction_back()
    {
        var failure = Error.Conflict("test.conflict", "Conflict");

        var result = await CreateTransactionBehavior().Handle(new SampleCommand("x"), _ => Task.FromResult(Result<string>.Failure(failure)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        await unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_exception_rolls_the_transaction_back_and_propagates()
    {
        var behavior = CreateTransactionBehavior();

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new SampleCommand("x"), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        await unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_already_open_transaction_is_reused()
    {
        unitOfWork.HasActiveTransaction.Returns(true);

        await CreateTransactionBehavior().Handle(new SampleCommand("x"), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        await unitOfWork.DidNotReceive().BeginAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
