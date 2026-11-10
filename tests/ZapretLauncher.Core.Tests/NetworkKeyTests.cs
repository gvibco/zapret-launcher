using ZapretLauncher.Core.Networking;

namespace ZapretLauncher.Core.Tests;

public class NetworkKeyTests
{
    [Fact]
    public void BuildKey_JoinsIdGatewayAndSsid()
    {
        Assert.Equal("nic|10.0.0.1|Home", NetworkKeyProvider.BuildKey("nic", "10.0.0.1", "Home"));
        Assert.Equal("nic|10.0.0.1|", NetworkKeyProvider.BuildKey("nic", "10.0.0.1", null));
    }
}
