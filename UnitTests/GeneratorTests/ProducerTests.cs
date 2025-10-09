using Xunit;

namespace UnitTests.GeneratorTests;

public class ProducerTests
{
    [Fact]
    public void Test()
    {
        // The Producer does not allow RunAsync to reliably complete in the unit test due to the while loop.
    }
}