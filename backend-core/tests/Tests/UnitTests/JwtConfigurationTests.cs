using Xunit;
using Microsoft.Extensions.Configuration;
using Infrastructure.Security;

namespace Tests.UnitTests;

public class JwtConfigurationTests
{
    [Fact]
    public void RequireSigningKey_Throws_WhenMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.Throws<InvalidOperationException>(() => JwtConfiguration.RequireSigningKey(config, "Jwt:Key"));
    }

    [Fact]
    public void RequireSigningKey_Throws_OnPlaceholder()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "YourSuperSecretKeyHereThatIsAtLeast32CharactersLong"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => JwtConfiguration.RequireSigningKey(config, "Jwt:Key"));
    }

    [Fact]
    public void RequireSigningKey_AcceptsStrongKey()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "unit-test-signing-key-value-32chars-min!!"
        }).Build();
        var key = JwtConfiguration.RequireSigningKey(config, "Jwt:Key");
        Assert.Equal("unit-test-signing-key-value-32chars-min!!", key);
    }
}
