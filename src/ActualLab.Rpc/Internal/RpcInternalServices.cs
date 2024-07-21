using System.Diagnostics.CodeAnalysis;
using ActualLab.Interception;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;

namespace ActualLab.Rpc.Internal;

public sealed class RpcInternalServices(RpcHub hub) : IHasServices
{
    public RpcHub Hub = hub;
    public IServiceProvider Services { get; } = hub.Services;
    public RpcServiceDefBuilder ServiceDefBuilder => Hub.ServiceDefBuilder;
    public RpcMethodDefBuilder MethodDefBuilder => Hub.MethodDefBuilder;
    public RpcBackendServiceDetector BackendServiceDetector => Hub.BackendServiceDetector;
    public RpcCommandTypeDetector CommandTypeDetector => Hub.CommandTypeDetector;
    public RpcServiceScopeResolver ServiceScopeResolver => Hub.ServiceScopeResolver;
    public RpcSafeCallRouter CallRouter => Hub.CallRouter;
    public RpcRerouteDelayer RerouteDelayer => Hub.RerouteDelayer;
    public RpcArgumentSerializer ArgumentSerializer => Hub.ArgumentSerializer;
    public RpcInboundContextFactory InboundContextFactory => Hub.InboundContextFactory;
    public RpcInboundMiddlewares InboundMiddlewares => Hub.InboundMiddlewares;
    public RpcOutboundMiddlewares OutboundMiddlewares => Hub.OutboundMiddlewares;
    public RpcPeerFactory PeerFactory => Hub.PeerFactory;
    public RpcClientConnectionFactory ClientConnectionFactory => Hub.ClientConnectionFactory;
    public RpcClientPeerReconnectDelayer ClientPeerReconnectDelayer => Hub.ClientPeerReconnectDelayer;
    public RpcPeerTerminalErrorDetector PeerTerminalErrorDetector => Hub.PeerTerminalErrorDetector;
    public RpcMethodTracerFactory MethodTracerFactory => Hub.MethodTracerFactory;
    public RpcCallLoggerFactory CallLoggerFactory => Hub.CallLoggerFactory;
    public RpcCallLoggerFilter CallLoggerFilter => Hub.CallLoggerFilter;
    public IEnumerable<RpcPeerTracker> PeerTrackers => Hub.PeerTrackers;
    public RpcSystemCallSender SystemCallSender => Hub.SystemCallSender;
    public RpcClient Client => Hub.Client;
    public ConcurrentDictionary<RpcPeerRef, RpcPeer> Peers => Hub.Peers;

    internal readonly RpcNonRoutingInterceptor.Options NonRoutingInterceptorOptions
        = hub.Services.GetRequiredService<RpcNonRoutingInterceptor.Options>();
    internal readonly RpcRoutingInterceptor.Options RoutingInterceptorOptions
        = hub.Services.GetRequiredService<RpcRoutingInterceptor.Options>();
    internal readonly RpcSwitchInterceptor.Options SwitchInterceptorOptions
        = hub.Services.GetRequiredService<RpcSwitchInterceptor.Options>();

    // NewXxx

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
    public IProxy NewNonRoutingProxy(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type proxyBaseType,
        bool initialize = true)
    {
        var interceptor = NewNonRoutingInterceptor(serviceType);
        return Services.ActivateProxy(proxyBaseType, interceptor, null, initialize);
    }

    public RpcNonRoutingInterceptor NewNonRoutingInterceptor(Type serviceType, object? localTarget = null, bool assumeConnected = false)
    {
        var serviceDef = Hub.ServiceRegistry[serviceType];
        return new RpcNonRoutingInterceptor(NonRoutingInterceptorOptions, Hub.Services, serviceDef, localTarget, assumeConnected);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
    public IProxy NewRoutingProxy(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type proxyBaseType,
        bool initialize = true)
    {
        var interceptor = NewRoutingInterceptor(serviceType);
        return Services.ActivateProxy(proxyBaseType, interceptor, null, initialize);
    }

    public RpcRoutingInterceptor NewRoutingInterceptor(Type serviceType, object? localTarget = null, bool assumeConnected = false)
    {
        var serviceDef = Hub.ServiceRegistry[serviceType];
        return new RpcRoutingInterceptor(RoutingInterceptorOptions, Hub.Services, serviceDef, localTarget, assumeConnected);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
    public IProxy NewSwitchProxy(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type proxyBaseType,
        object? localTarget,
        object? remoteTarget,
        bool initialize = true)
    {
        var interceptor = NewSwitchInterceptor(serviceType, localTarget, remoteTarget);
        return Services.ActivateProxy(proxyBaseType, interceptor, null, initialize);
    }

    public RpcSwitchInterceptor NewSwitchInterceptor(Type serviceType, object? localTarget, object? remoteTarget)
    {
        var serviceDef = Hub.ServiceRegistry[serviceType];
        return new RpcSwitchInterceptor(SwitchInterceptorOptions, Hub.Services, serviceDef, localTarget, remoteTarget);
    }
}
