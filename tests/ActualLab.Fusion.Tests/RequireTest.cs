using System.Security;
using ActualLab.Fusion.Authentication;

namespace ActualLab.Fusion.Tests;

public class RequireTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void SimpleTest()
    {
        // Struct
        Assert.ThrowsAny<InvalidOperationException>(() => 0.Require());
        Assert.ThrowsAny<InvalidOperationException>(() => ((int?)null).Require());
        1.Require();
        ((int?)0).Require();

        // Object
        Assert.ThrowsAny<InvalidOperationException>(() => ((object)null!).Require());
        Assert.ThrowsAny<InvalidOperationException>(() => ((object?)null).Require());
        new object().Require();

        // Custom
        var requirement = Requirement.New<int>(i => i != 1).With("Invalid {0}: {1}!", "int");
        Assert.ThrowsAny<InvalidOperationException>(() => 1.Require(requirement))
            .Message.Should().Be("Invalid int: 1!");
    }

    [Fact]
    public async Task TaskTest()
    {
        // Struct
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Task.FromResult(0).Require());
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Task.FromResult((int?)null).Require());
        await Task.FromResult(1).Require();
        await Task.FromResult((int?)0).Require();

        // Object
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Task.FromResult((object?)null).Require());
        await Task.FromResult(new object())!.Require();

        // Custom
        var requirement = Requirement.New<int>(i => i != 1).With("Invalid {0}: {1}!", "int");
        (await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Task.FromResult(1).Require(requirement)))
            .Message.Should().Be("Invalid int: 1!");
    }

    [Fact]
    public async Task ValueTaskTest()
    {
        // Struct
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => ValueTaskExt.FromResult(0).Require().AsTask());
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => ValueTaskExt.FromResult((int?)null).Require().AsTask());
        await ValueTaskExt.FromResult(1).Require();
        await ValueTaskExt.FromResult((int?)0).Require();

        // Object
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => ValueTaskExt.FromResult((object?)null).Require().AsTask());
        await ValueTaskExt.FromResult(new object())!.Require();

        // Custom
        var requirement = Requirement.New<int>(i => i != 1).With("Invalid {0}: {1}!", "int");
        (await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => ValueTaskExt.FromResult(1).Require(requirement).AsTask())
            ).Message.Should().Be("Invalid int: 1!");
    }

    [Fact]
    public async Task UserTest()
    {
        var user = new User("thisIsUserId", "Bob");
        user.Require(User.MustBeAuthenticated);
        user.Require(User.MustBeAuthenticated.WithServiceException);
        user.Require(User.MustBeAuthenticated.WithServiceException());
        user.Require(User.MustBeAuthenticated & Requirement.New<User>(u => u?.Name == "Bob"));
        await Task.FromResult(user)!.Require(User.MustBeAuthenticated);
        await Task.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException);
        await Task.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException());
        await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated);
        await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException);
        await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException());

        user = User.NewGuest();
        Assert.ThrowsAny<SecurityException>(() => user.Require(User.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => user.Require(User.MustBeAuthenticated.WithServiceException));
        Assert.ThrowsAny<InvalidOperationException>(() => user.Require(Requirement.New<User>(u => u?.Name == "Bob")));
        Assert.ThrowsAny<InvalidOperationException>(() => user.Require(User.MustBeAuthenticated.With("Invalid!")))
            .Message.Should().Be("Invalid!");
        Assert.ThrowsAny<InvalidOperationException>(() => user.Require(User.MustBeAuthenticated.With("Invalid {0}!", "Author")))
            .Message.Should().Be("Invalid Author!");
        Assert.ThrowsAny<NotSupportedException>(() => user.Require(User.MustBeAuthenticated.With("Invalid!", m => new NotSupportedException(m))))
            .Message.Should().Be("Invalid!");
        Assert.ThrowsAny<NotSupportedException>(() => user.Require(User.MustBeAuthenticated.With(() => new NotSupportedException("!"))))
            .Message.Should().Be("!");

        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException));

        user = null;
        Assert.ThrowsAny<SecurityException>(() => user.Require(User.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => user.Require(User.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(user).Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(user).Require(User.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated.WithServiceException));
    }

    [Fact]
    public async Task SessionAuthInfoTest()
    {
        var session = new Session("whatever-long-long-id");
        var authInfo = new SessionAuthInfo(session) { UserId = "1" };
        authInfo.Require(SessionAuthInfo.MustBeAuthenticated);
        authInfo.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException);
        authInfo.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException());
        await Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated);
        await Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException);
        await Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException());
        await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException);
        await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException());

        authInfo = new SessionInfo(session);
        Assert.ThrowsAny<SecurityException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException()));

        authInfo = null;
        Assert.ThrowsAny<SecurityException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(authInfo).Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(authInfo).Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated.WithServiceException));
    }

    [Fact]
    public async Task SessionInfoTest()
    {
        var session = new Session("whatever-long-long-id");
        var sessionInfo = new SessionInfo(session) { UserId = "1" };
        sessionInfo.Require(SessionInfo.MustBeAuthenticated);
        sessionInfo.Require(SessionInfo.MustBeAuthenticated.WithServiceException);
        sessionInfo.Require(SessionInfo.MustBeAuthenticated.WithServiceException());
        await Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated);
        await Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException);
        await Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException());
        await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException);
        await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException());

        sessionInfo = new SessionInfo(session);
        Assert.ThrowsAny<SecurityException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException));

        sessionInfo = null;
        Assert.ThrowsAny<SecurityException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ServiceException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ServiceException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated.WithServiceException));
    }
}
