using System.Net.Http;
using System.Threading.Tasks;
using DLCS.Web.Requests;
using FluentAssertions;
using Xunit;

namespace DLCS.Web.Tests.Requests
{
    public class RequestHelpersTests
    {
        [Fact]
        public async Task SetJsonContent_SetsExpected()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var testClass = new TestClass {Name = "Foo", Number = 42};
            var expected = "{\"name\":\"Foo\",\"number\":42}";
            
            // Act
            request.SetJsonContent(testClass);

            // Assert
            request.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
            request.Content.Headers.ContentType.MediaType.Should().Be("application/json");
            (await request.Content.ReadAsStringAsync()).Should().Be(expected);
        }
        
        private class TestClass
        {
            public string Name { get; set; }
            public int Number { get; set; }
        }
    }
}