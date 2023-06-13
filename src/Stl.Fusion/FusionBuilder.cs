using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Fusion.Multitenancy;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Fusion.Rpc.Cache;
using Stl.Fusion.Rpc.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Fusion.UI;
using Stl.Multitenancy;
using Stl.Rpc;
using Stl.Versioning.Providers;

namespace Stl.Fusion;

public readonly struct FusionBuilder
{
    public IServiceCollection Services { get; }
    public CommanderBuilder Commander { get; }
    public RpcBuilder Rpc { get; }
    public RpcServiceMode ServiceMode { get; }

    internal FusionBuilder(
        IServiceCollection services,
        Action<FusionBuilder>? configure = null,
        RpcServiceMode serviceMode = default,
        bool setDefaultServiceShareMode = false)
    {
        Services = services;
        Commander = services.AddCommander();
        Rpc = services.AddRpc();
        var dFusionTag = services.FirstOrDefault(d => d.ServiceType == typeof(FusionTag));
        if (dFusionTag is { ImplementationInstance: FusionTag fusionTag }) {
            ServiceMode = serviceMode.Or(fusionTag.ServiceMode);
            if (setDefaultServiceShareMode)
                fusionTag.ServiceMode = ServiceMode;

            configure?.Invoke(this);
            return;
        }

        // We want above FusionTag lookup to run in O(1), so...
        ServiceMode = serviceMode.OrNone();
        services.RemoveAll<FusionTag>();
        services.Insert(0, new ServiceDescriptor(
            typeof(FusionTag),
            new FusionTag(setDefaultServiceShareMode ? ServiceMode : RpcServiceMode.None)));

        // Common services
        services.AddOptions();
        services.AddConverters();
        services.TryAddSingleton(_ => MomentClockSet.Default);
        services.TryAddSingleton(c => c.GetRequiredService<MomentClockSet>().SystemClock);
        services.TryAddSingleton(_ => LTagVersionGenerator.Default);
        services.TryAddSingleton(_ => ClockBasedVersionGenerator.DefaultCoarse);
        services.TryAddSingleton(c => new FusionInternalHub(c));

        // Compute services & their dependencies
        services.TryAddSingleton(_ => new ComputedOptionsProvider());
        services.TryAddSingleton(_ => TransientErrorDetector.DefaultPreferTransient.For<IComputed>());
        services.TryAddSingleton(_ => new ComputeServiceInterceptor.Options());
        services.TryAddSingleton(c => new ComputeServiceInterceptor(
            c.GetRequiredService<ComputeServiceInterceptor.Options>(), c));

        // States
        services.TryAddSingleton(c => new MixedModeService<IStateFactory>.Singleton(new StateFactory(c), c));
        services.TryAddScoped(c => new MixedModeService<IStateFactory>.Scoped(new StateFactory(c), c));
        services.TryAddTransient(c => c.GetRequiredMixedModeService<IStateFactory>());
        services.TryAddSingleton(typeof(MutableState<>.Options));
        services.TryAddTransient(typeof(IMutableState<>), typeof(MutableState<>));

        // Update delayer & UI action tracker
        services.TryAddSingleton(_ => new UIActionTracker.Options());
        services.TryAddScoped<UIActionTracker>(c => new UIActionTracker(
            c.GetRequiredService<UIActionTracker.Options>(), c));
        services.TryAddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker()));

        // CommandR, command completion and invalidation
        var commander = Commander;
        services.TryAddSingleton(_ => new AgentInfo());
        services.TryAddSingleton(c => new InvalidationInfoProvider(
            c.Commander(), c.GetRequiredService<CommandHandlerResolver>()));

        // Transient operation scope & its provider
        if (!services.HasService<TransientOperationScopeProvider>()) {
            services.AddSingleton(c => new TransientOperationScopeProvider(c));
            commander.AddHandlers<TransientOperationScopeProvider>();
        }

        // Nested command logger
        if (!services.HasService<NestedCommandLogger>()) {
            services.AddSingleton(c => new NestedCommandLogger(c));
            commander.AddHandlers<NestedCommandLogger>();
        }

        // Operation completion - notifier & producer
        services.TryAddSingleton(_ => new OperationCompletionNotifier.Options());
        services.TryAddSingleton<IOperationCompletionNotifier>(c => new OperationCompletionNotifier(
            c.GetRequiredService<OperationCompletionNotifier.Options>(), c));
        services.TryAddSingleton(_ => new CompletionProducer.Options());
        services.TryAddEnumerable(ServiceDescriptor.Singleton(
            typeof(IOperationCompletionListener),
            typeof(CompletionProducer)));

        // Command completion handler performing invalidations
        services.TryAddSingleton(_ => new PostCompletionInvalidator.Options());
        if (!services.HasService<PostCompletionInvalidator>()) {
            services.AddSingleton(c => new PostCompletionInvalidator(
                c.GetRequiredService<PostCompletionInvalidator.Options>(), c));
            commander.AddHandlers<PostCompletionInvalidator>();
        }

        // Completion terminator
        if (!services.HasService<CompletionTerminator>()) {
            services.AddSingleton(_ => new CompletionTerminator());
            commander.AddHandlers<CompletionTerminator>();
        }

        // Core authentication services
        services.TryAddScoped<ISessionResolver>(c => new SessionResolver(c));
        services.TryAddScoped(c => c.GetRequiredService<ISessionResolver>().Session);
        services.TryAddSingleton<ISessionFactory>(_ => new SessionFactory());

        // Core multitenancy services
        services.TryAddSingleton<ITenantRegistry<Unit>>(_ => new SingleTenantRegistry<Unit>());
        services.TryAddSingleton<DefaultTenantResolver<Unit>.Options>();
        services.TryAddSingleton<ITenantResolver<Unit>>(c => new DefaultTenantResolver<Unit>(
            c.GetRequiredService<DefaultTenantResolver<Unit>.Options>(), c));
        // And make it default
        services.TryAddAlias<ITenantRegistry, ITenantRegistry<Unit>>();
        services.TryAddAlias<ITenantResolver, ITenantResolver<Unit>>();

        // RPC

        // Compute system calls service + call type
        if (!Rpc.Configuration.Services.ContainsKey(typeof(IRpcComputeSystemCalls))) {
            Rpc.Service<IRpcComputeSystemCalls>().HasServer<RpcComputeSystemCalls>().HasName(RpcComputeSystemCalls.Name);
            services.AddSingleton(c => new RpcComputeSystemCalls(c));
            services.AddSingleton(c => new RpcComputeSystemCallSender(c));
            Rpc.Configuration.InboundCallTypes.Add(
                RpcComputeCall.CallTypeId,
                typeof(RpcInboundComputeCall<>));
        }

        // Compute call interceptor
        services.TryAddSingleton(_ => new RpcComputeClientInterceptor.Options());
        services.TryAddTransient(c => new RpcComputeClientInterceptor(
            c.GetRequiredService<RpcComputeClientInterceptor.Options>(), c));

        // Compute call cache
        services.AddSingleton(c => (RpcComputedCache)new RpcNoComputedCache(c));

        configure?.Invoke(this);
    }

    public FusionBuilder WithServiceMode(
        RpcServiceMode serviceMode,
        bool makeDefault = false)
        => new(Services, null, serviceMode, makeDefault);

    // ComputeService

    public FusionBuilder AddService<TService>(RpcServiceMode mode)
        where TService : class, IComputeService
        => AddService(typeof(TService), typeof(TService), ServiceLifetime.Singleton, mode);
    public FusionBuilder AddService<TService, TImplementation>(RpcServiceMode mode)
        where TService : class
        where TImplementation : class, TService, IComputeService
        => AddService(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton, mode);
    public FusionBuilder AddService<TService>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        RpcServiceMode mode = RpcServiceMode.Default)
        where TService : class, IComputeService
        => AddService(typeof(TService), typeof(TService), lifetime, mode);
    public FusionBuilder AddService<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        RpcServiceMode mode = RpcServiceMode.Default)
        where TService : class
        where TImplementation : class, TService, IComputeService
        => AddService(typeof(TService), typeof(TImplementation), lifetime, mode);

    public FusionBuilder AddService(Type serviceType, RpcServiceMode mode)
        => AddService(serviceType, serviceType, ServiceLifetime.Singleton, mode);
    public FusionBuilder AddService(Type serviceType, Type implementationType, RpcServiceMode mode)
        => AddService(serviceType, implementationType, ServiceLifetime.Singleton, mode);
    public FusionBuilder AddService(
        Type serviceType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        RpcServiceMode mode = RpcServiceMode.Default)
        => AddService(serviceType, serviceType, lifetime, mode);
    public FusionBuilder AddService(
        Type serviceType, Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        RpcServiceMode mode = RpcServiceMode.Default)
    {
        if (!serviceType.IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));
        if (!typeof(IComputeService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(implementationType, nameof(implementationType));

        if (lifetime != ServiceLifetime.Singleton)
            return AddServiceImpl(serviceType, implementationType, lifetime);

        mode = mode.Or(ServiceMode);
        return mode switch {
            RpcServiceMode.Server => AddServer(serviceType, implementationType),
            RpcServiceMode.Router => AddRouter(serviceType, implementationType),
            RpcServiceMode.RoutingServer => AddRoutingServer(serviceType, implementationType),
            RpcServiceMode.ServingRouter => AddServingRouter(serviceType, implementationType),
            _ => AddServiceImpl(serviceType, implementationType)
        };
    }

    public FusionBuilder AddServer<TService, TImplementation>(Symbol name = default)
        => AddServer(typeof(TService), typeof(TImplementation), name);
    public FusionBuilder AddServer(Type serviceType, Type implementationType, Symbol name = default)
    {
        if (!serviceType.IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));
        if (!typeof(IComputeService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(implementationType, nameof(implementationType));

        AddServiceImpl(serviceType, implementationType);
        Rpc.Service(serviceType).HasServer(serviceType).HasName(name);
        return this;
    }

    public FusionBuilder AddClient<TService>(Symbol name = default)
        => AddClient(typeof(TService), name);
    public FusionBuilder AddClient(Type serviceType, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!typeof(IComputeService).IsAssignableFrom(serviceType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(serviceType, nameof(serviceType));

        Services.AddSingleton(serviceType, c => FusionProxies.NewClientProxy(c, serviceType));
        Commander.AddHandlers(serviceType);
        Rpc.Service(serviceType).HasName(name);
        return this;
    }

    public FusionBuilder AddRouter<TService, TImplementation>(Symbol name = default)
        => AddRouter(typeof(TService), typeof(TImplementation), name);
    public FusionBuilder AddRouter(Type serviceType, Type implementationType, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!serviceType.IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));
        if (!typeof(IComputeService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(implementationType, nameof(implementationType));

        var serverResolver = ServiceResolver.New(c => FusionProxies.NewServiceProxy(c, implementationType));
        Services.AddSingleton(serviceType, c => FusionProxies.NewRoutingProxy(c, serviceType, serverResolver));
        Commander.AddHandlers(serviceType);
        Rpc.Service(serviceType).HasName(name);
        return this;
    }

    public FusionBuilder AddServingRouter<TService, TImplementation>(Symbol name = default)
        => AddServingRouter(typeof(TService), typeof(TImplementation), name);
    public FusionBuilder AddServingRouter(Type serviceType, Type implementationType, Symbol name = default)
    {
        AddRouter(serviceType, implementationType);
        Rpc.Service(serviceType).HasServer(serviceType).HasName(name);
        return this;
    }

    public FusionBuilder AddRoutingServer<TService, TImplementation>(Symbol name = default)
        => AddRoutingServer(typeof(TService), typeof(TImplementation), name);
    public FusionBuilder AddRoutingServer(Type serviceType, Type implementationType, Symbol name = default)
    {
        if (!serviceType.IsInterface)
            throw Stl.Internal.Errors.MustBeInterface(serviceType, nameof(serviceType));
        if (!serviceType.IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));
        if (!typeof(IComputeService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<IComputeService>(implementationType, nameof(implementationType));

        Services.AddSingleton(implementationType, c => FusionProxies.NewServiceProxy(c, implementationType));
        Services.AddSingleton(serviceType, c => FusionProxies.NewRoutingProxy(c, serviceType, implementationType));
        Commander.AddHandlers(serviceType, implementationType);
        Rpc.Service(serviceType).HasServer(implementationType).HasName(name);
        return this;
    }

    // AddOperationReprocessor

    public FusionBuilder AddOperationReprocessor(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        => AddOperationReprocessor<OperationReprocessor>(optionsFactory);

    public FusionBuilder AddOperationReprocessor<TOperationReprocessor>(
        Func<IServiceProvider, OperationReprocessorOptions>? optionsFactory = null)
        where TOperationReprocessor : class, IOperationReprocessor
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => OperationReprocessorOptions.Default);
        if (services.HasService<TOperationReprocessor>())
            return this;

        services.AddTransient<TOperationReprocessor>();
        services.AddAlias<IOperationReprocessor, TOperationReprocessor>(ServiceLifetime.Transient);
        Commander.AddHandlers<TOperationReprocessor>();
        services.AddSingleton(TransientErrorDetector.DefaultPreferNonTransient.For<IOperationReprocessor>());
        return this;
    }

    // AddComputedGraphPruner

    public FusionBuilder AddComputedGraphPruner(
        Func<IServiceProvider, ComputedGraphPruner.Options>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => ComputedGraphPruner.Options.Default);
        if (services.HasService<ComputedGraphPruner>())
            return this;

        services.AddSingleton(c => new ComputedGraphPruner(
            c.GetRequiredService<ComputedGraphPruner.Options>(), c));
        services.AddHostedService(c => c.GetRequiredService<ComputedGraphPruner>());
        return this;
    }

    // Private methods

    private FusionBuilder AddServiceImpl(
        Type serviceType, Type implementationType,
        bool addCommandHandlers)
        => AddServiceImpl(serviceType, implementationType, ServiceLifetime.Singleton, addCommandHandlers);
    private FusionBuilder AddServiceImpl(
        Type serviceType, Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        bool addCommandHandlers = true)
    {
        var descriptor = new ServiceDescriptor(serviceType, c => FusionProxies.NewServiceProxy(c, implementationType), lifetime);
        Services.Add(descriptor);
        if (addCommandHandlers)
            Commander.AddHandlers(serviceType);
        return this;
    }

    // Nested types

    public class FusionTag
    {
        private RpcServiceMode _serviceMode;

        public RpcServiceMode ServiceMode {
            get => _serviceMode;
            set => _serviceMode = value.OrNone();
        }

        public FusionTag(RpcServiceMode serviceMode)
            => ServiceMode = serviceMode;
    }
}
