using Xunit;
using Core.Domain.Entities;

namespace Tests.UnitTests;

public class CallAcceptTests
{
    [Fact]
    public void Accept_FromRinging_SetsConnected()
    {
        var call = new Call(Guid.NewGuid(), Guid.NewGuid(), isVideo: false);
        call.ClearDomainEvents();
        Assert.Equal(CallStatus.Ringing, call.Status);

        call.Accept();

        Assert.Equal(CallStatus.Connected, call.Status);
        Assert.Single(call.DomainEvents);
    }

    [Fact]
    public void Accept_WhenAlreadyConnected_IsIdempotent()
    {
        var call = new Call(Guid.NewGuid(), Guid.NewGuid(), isVideo: true);
        call.Accept();
        call.ClearDomainEvents();

        call.Accept();

        Assert.Equal(CallStatus.Connected, call.Status);
        Assert.Single(call.DomainEvents);
    }

    [Fact]
    public void Accept_WhenEnded_Throws()
    {
        var call = new Call(Guid.NewGuid(), Guid.NewGuid());
        call.Reject();

        var ex = Assert.Throws<InvalidOperationException>(() => call.Accept());
        Assert.Equal("Call can only be accepted if ringing.", ex.Message);
    }
}
