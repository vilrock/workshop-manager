namespace Domain.Common;

public sealed class DuplicateRecordException(string constraintName) : Exception($"A record violating the constraint '{constraintName}' already exists.")
{
    public string ConstraintName { get; } = constraintName;
}
