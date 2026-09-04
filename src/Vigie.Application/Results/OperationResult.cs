using Vigie.Domain;

namespace Vigie.Application;

#pragma warning disable CA1000 // Generic result factories keep call sites strongly typed and discoverable.
public sealed class OperationResult<T>
{
    private OperationResult(T? value, IReadOnlyList<RuleViolation> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public IReadOnlyList<RuleViolation> Errors { get; }
    public bool IsSuccess => Errors.Count == 0;

    public static OperationResult<T> Success(T value) => new(value, []);
    public static OperationResult<T> Failure(IEnumerable<RuleViolation> errors) => new(default, errors.ToArray());
    public static OperationResult<T> Failure(string code, string message) => new(default, [new RuleViolation(code, message)]);
}
#pragma warning restore CA1000
