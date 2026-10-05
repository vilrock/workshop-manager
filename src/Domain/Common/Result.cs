namespace Domain.Common;

public interface IResultBase
{
    bool IsSuccess { get; }

    Error? Error { get; }
}

public interface IResultFactory<TSelf> where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf Failure(Error error);
}

public sealed class Result : IResultBase, IResultFactory<Result>
{
    private Result(Error? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);
}

public sealed class Result<TValue> : IResultBase, IResultFactory<Result<TValue>>
{
    private readonly TValue? value;

    private Result(TValue? value, Error? error)
    {
        this.value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public Error? Error { get; }

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<TValue> Success(TValue value) => new(value, null);

    public static Result<TValue> Failure(Error error) => new(default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
