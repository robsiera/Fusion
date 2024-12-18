namespace ActualLab.Identifiers;

#pragma warning disable CA2252

public interface ISymbolIdentifier : IHasId<Symbol>, ICanBeNone
{
    public string Value { get; }
}

#pragma warning disable CA1000

public interface ISymbolIdentifier<TSelf> : ISymbolIdentifier, IEquatable<TSelf>, IComparable<TSelf>, ICanBeNone<TSelf>
    where TSelf : struct, ISymbolIdentifier<TSelf>
{
#if NET6_0_OR_GREATER
    public static abstract TSelf Parse(string? s);
    public static abstract TSelf ParseOrNone(string? s);
    public static abstract bool TryParse(string? s, out TSelf result);
#endif

#if !NETSTANDARD2_0
    int IComparable<TSelf>.CompareTo(TSelf other)
        => string.CompareOrdinal(Id.Value, other.Id.Value);
#endif
}
