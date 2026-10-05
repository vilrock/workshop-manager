using Domain.Abstractions;
using Domain.Common;
using MediatR;

namespace Service.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ITransactionalCommand
    where TResponse : IResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            return await next(cancellationToken);
        }

        await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);
            await CompleteAsync(response, cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private Task CompleteAsync(TResponse response, CancellationToken cancellationToken) =>
        response.IsSuccess
            ? unitOfWork.CommitAsync(cancellationToken)
            : unitOfWork.RollbackAsync(cancellationToken);
}
