namespace ActualLab.Fusion;

public static class AnonymousComputedSourceExt
{
    // Computed-like methods

    public static ValueTask<T> Use<T>(
        this AnonymousComputedSource<T> source, CancellationToken cancellationToken = default)
        => source.Computed.Use(cancellationToken);

    public static void Invalidate(this IAnonymousComputedSource source, bool immediately = false)
        => source.Computed.Invalidate(immediately);

    public static async ValueTask<TAnonymousComputedSource> Update<TAnonymousComputedSource>(
        this TAnonymousComputedSource source, CancellationToken cancellationToken = default)
        where TAnonymousComputedSource : class, IAnonymousComputedSource
    {
        await source.Computed.Update(cancellationToken).ConfigureAwait(false);
        return source;
    }

    // When

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
        => source.Computed.When(predicate, cancellationToken);

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, bool> predicate,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
        => source.Computed.When(predicate, updateDelayer, cancellationToken);

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, Exception?, bool> predicate,
        CancellationToken cancellationToken = default)
        => source.Computed.When(predicate, cancellationToken);

    public static Task<Computed<T>> When<T>(this AnonymousComputedSource<T> source,
        Func<T, Exception?, bool> predicate,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
        => source.Computed.When(predicate, updateDelayer, cancellationToken);

    // Changes

    public static IAsyncEnumerable<Computed<T>> Changes<T>(
        this AnonymousComputedSource<T> source,
        CancellationToken cancellationToken = default)
        => source.Computed.Changes(cancellationToken);
    public static IAsyncEnumerable<Computed<T>> Changes<T>(
        this AnonymousComputedSource<T> source,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
        => source.Computed.Changes(updateDelayer, cancellationToken);
}
