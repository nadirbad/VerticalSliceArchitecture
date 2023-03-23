using NUnit.Framework;

using static VerticalSliceArchitecture.Application.IntegrationTests.Testing;

namespace VerticalSliceArchitecture.Application.IntegrationTests;
public class TestBase
{
    [SetUp]
    public async Task TestSetUp()
    {
        await ResetState();
    }
}
