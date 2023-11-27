using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public sealed class RpcPeerStateMonitor : WorkerBase
{
    private readonly IMutableState<RpcPeerState?> _state;
    private ILogger? _log;

    private IServiceProvider Services { get; }
    private ILogger Log => _log ??= Services.LogFor(GetType());

    public RpcPeerRef PeerRef { get; set; } = RpcPeerRef.Default;
    public TimeSpan StartDelay { get; set; } = TimeSpan.FromSeconds(1);
    public IState<RpcPeerState?> State => _state;

    public RpcPeerStateMonitor(IServiceProvider services)
    {
        Services = services;
        _state = services.StateFactory().NewMutable((RpcPeerState?)null, $"{GetType().Name}.{nameof(State)}");
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var hub = Services.RpcHub();
        while (true) {
            await Task.Delay(StartDelay, cancellationToken).ConfigureAwait(false);
            Log.LogInformation("`{PeerRef}`: monitor (re)started", PeerRef);
            var peer = hub.GetClientPeer(PeerRef);
            var peerCts = cancellationToken.LinkWith(peer.StopToken);
            var peerCancellationToken = peerCts.Token;
            try {
                // This delay gives some time for peer to connect
                var connectionState = peer.ConnectionState;
                while (true) {
                    connectionState = connectionState.Last;
                    var nextConnectionStateTask = connectionState.WhenNext(peerCancellationToken);
                    var isConnected = connectionState.Value.IsConnected();

                    if (isConnected) {
                        _state.Value = new RpcPeerState(true);
                        connectionState = await nextConnectionStateTask.ConfigureAwait(false);
                    }
                    else {
                        _state.Value = new RpcPeerState(false, connectionState.Value.Error);
                        // Disconnected -> update ReconnectsAt value until the nextConnectionStateTask completes
                        using var reconnectAtCts = new CancellationTokenSource();
                        // ReSharper disable once AccessToDisposedClosure
                        _ = nextConnectionStateTask.ContinueWith(_ => reconnectAtCts.Cancel(), TaskScheduler.Default);
                        try {
                            var reconnectAtChanges = peer.ReconnectsAt.Changes(reconnectAtCts.Token);
                            await foreach (var reconnectsAt in reconnectAtChanges.ConfigureAwait(false))
                                _state.Value = _state.Value with { ReconnectsAt = reconnectsAt };
                        }
                        catch (OperationCanceledException) when (reconnectAtCts.IsCancellationRequested) {
                            // Intended
                        }
                    }
                }
            }
            catch (Exception e) {
                if (e.IsCancellationOf(cancellationToken)) {
                    Log.LogInformation("`{PeerRef}`: monitor stopped", PeerRef);
                    return;
                }
                if (peer.StopToken.IsCancellationRequested)
                    Log.LogWarning("`{PeerRef}`: peer is terminated, will restart", PeerRef);
                else
                    Log.LogError(e, "`{PeerRef}`: monitor failed, will restart", PeerRef);
            }
            finally {
                _state.Value = null;
                peerCts.CancelAndDisposeSilently();
            }
        }
    }
}
