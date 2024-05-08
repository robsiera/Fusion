using ActualLab.OS;

namespace ActualLab.Fusion.Blazor;

public static class ComputedStateComponent
{
    private static readonly MethodInfo GetDefaultOptionsImplMethod = typeof(ComputedStateComponent)
        .GetMethod(nameof(CreateDefaultStateOptionsImpl), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, string> StateCategoryCache = new();
    private static readonly ConcurrentDictionary<Type, IComputedState.IOptions> StateOptionsCache = new();
    private static readonly ConcurrentDictionary<Type, Func<Type, IComputedState.IOptions>> CreateDefaultStateOptionsCache = new();

    public static ComputedStateComponentOptions DefaultOptions { get; set; }
    public static Func<Type, IComputedState.IOptions> DefaultStateOptionsFactory { get; set; } = CreateDefaultStateOptions;

    static ComputedStateComponent()
    {
        DefaultOptions = ComputedStateComponentOptions.SynchronizeComputeState
                         | ComputedStateComponentOptions.RecomputeOnParametersSet;
        if (HardwareInfo.IsSingleThreaded)
            DefaultOptions = ComputedStateComponentOptions.RecomputeOnParametersSet;
    }

    public static ComputedState<TState>.Options GetStateOptions<TState>(
        Type componentType, Func<Type, ComputedState<TState>.Options>? optionsFactory = null)
        => (ComputedState<TState>.Options)StateOptionsCache.GetOrAdd(componentType,
            optionsFactory as Func<Type, IComputedState.IOptions> ?? DefaultStateOptionsFactory);

    public static IComputedState.IOptions GetStateOptions(
        Type componentType, Func<Type, IComputedState.IOptions>? optionsFactory = null)
        => StateOptionsCache.GetOrAdd(componentType, optionsFactory ?? DefaultStateOptionsFactory);

    public static string GetStateCategory(Type componentType)
        => StateCategoryCache.GetOrAdd(componentType, static t => $"{t.GetName()}.State");

    public static string GetMutableStateCategory(Type componentType)
        => StateCategoryCache.GetOrAdd(componentType, static t => $"{t.GetName()}.MutableState");

    public static IComputedState.IOptions CreateDefaultStateOptions(Type componentType)
        => CreateDefaultStateOptionsCache.GetOrAdd(componentType, static componentType1 => {
            var type = componentType1;
            while (type != null) {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() is var gtd
                    && gtd == typeof(ComputedStateComponent<>)) {
                    var stateType = type.GetGenericArguments().Single();
                    return (Func<Type, IComputedState.IOptions>)GetDefaultOptionsImplMethod
                        .MakeGenericMethod(stateType)
                        .CreateDelegate(typeof(Func<Type, IComputedState.IOptions>));
                }
                type = type.BaseType;
            }
            throw new ArgumentOutOfRangeException(nameof(componentType));
        }).Invoke(componentType);

    // Private methods

    private static IComputedState.IOptions CreateDefaultStateOptionsImpl<TState>(Type componentType)
        => new ComputedState<TState>.Options() {
            Category = GetStateCategory(componentType),
        };
}
