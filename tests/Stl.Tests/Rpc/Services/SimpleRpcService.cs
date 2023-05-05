using Stl.Rpc;

namespace Stl.Tests.Rpc;

public interface ISimpleRpcService : IRpcClient
{
    public Task<int> Sum(int a, int b);
}

public class SimpleRpcService : ISimpleRpcService
{
    public Task<int> Sum(int a, int b)
        => Task.FromResult(a + b);
}
