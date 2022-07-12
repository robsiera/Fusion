namespace Stl.Requirements;

public abstract record CustomizableRequirementBase<T> : Requirement<T>
{
    public ExceptionBuilder ExceptionBuilder { get; init; }

    public override T Require(T? value)
    {
        if (!IsSatisfied(value))
            throw ExceptionBuilder.Build(value);
        return value!;
    }
}
