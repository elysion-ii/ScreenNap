using ScreenNap.Monitors;
using Xunit;

namespace ScreenNap.Tests.Monitors;

public sealed class MonitorIdentityTests
{
    [Fact]
    internal void Default_NoFieldsSet_HasAllZeroFields()
    {
        MonitorIdentity identity = default;

        Assert.Equal((ushort)0, identity.EdidManufacturerId);
        Assert.Equal((ushort)0, identity.EdidProductCodeId);
        Assert.Equal(0u, identity.ConnectorInstance);
    }

    [Theory]
    [InlineData(1, 2, 3, 1, 2, 3, true)]
    [InlineData(1, 2, 3, 1, 2, 4, false)]
    internal void Equality_AnyFieldValues_ComparesByValue(
        ushort manufacturer1,
        ushort product1,
        uint connector1,
        ushort manufacturer2,
        ushort product2,
        uint connector2,
        bool expected)
    {
        var first = new MonitorIdentity(manufacturer1, product1, connector1);
        var second = new MonitorIdentity(manufacturer2, product2, connector2);

        Assert.Equal(expected, first == second);
    }
}
