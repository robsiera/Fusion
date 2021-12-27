﻿using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisSequenceSetTest : RedisTestBase
{
    public RedisSequenceSetTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return; // No Redis on build agent for now

        var set = GetRedisDb().GetSequenceSet("seq");
        await set.Clear();

        (await set.Next("a")).Should().Be(1);
        (await set.Next("a")).Should().Be(2);
        (await set.Next("a", 500)).Should().Be(501);
        (await set.Next("a", 300)).Should().Be(502);
        (await set.Next("a")).Should().Be(503);
        (await set.Next("a", 1000_000_000).WithTimeout(TimeSpan.FromMilliseconds(100)))
            .Should().Be(Option.Some(1000_000_001L)); // Auto-reset test
        await set.Reset("a", 10);
        (await set.Next("a", 5)).Should().Be(11);
    }
}