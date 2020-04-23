using System;
using System.Threading.Tasks;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Security;
using DLCS.Test.Helpers;
using FakeItEasy;
using FluentAssertions;
using LazyCache;
using LazyCache.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DLCS.Repository.Tests.Security
{
    public class CredentialsRepositoryTests
    {
        private readonly IAppCache appCache;
        private readonly IBucketReader bucketReader;
        private readonly CredentialsRepository sut;

        public CredentialsRepositoryTests()
        {
            bucketReader = A.Fake<IBucketReader>();
            appCache = new MockCachingService();
            
            sut = new CredentialsRepository(bucketReader, appCache, new NullLogger<CredentialsRepository>());
        }
        
        [Fact]
        public void GetBasicCredentialsForOriginStrategy_Throws_IfStrategyNull()
        {
            // Arrange
            Func<Task> action = () => sut.GetBasicCredentialsForOriginStrategy(null);

            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'customerOriginStrategy')");
        }
        
        [Fact]
        public void GetBasicCredentialsForOriginStrategy_Throws_IfCredentialsNull()
        {
            // Arrange
            Func<Task> action = () => sut.GetBasicCredentialsForOriginStrategy(new CustomerOriginStrategy());

            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'Credentials')");
        }
        
        [Fact]
        public void GetBasicCredentialsForOriginStrategy_Throws_IfNotS3String_AndNotValid()
        {
            // Arrange
            var customerOriginStrategy = new CustomerOriginStrategy {Credentials = "foo-bar"};
            
            // Act
            Func<Task> action = () => sut.GetBasicCredentialsForOriginStrategy(customerOriginStrategy);
            
            // Assert
            action.Should().Throw<Exception>();
        }

        [Fact]
        public async Task GetBasicCredentialsForOriginStrategy_DeserializesCredentialsFromDb_IfNotS3String()
        {
            // Arrange
            const string username = "the-user";
            const string password = "wheesht";
            var customerOriginStrategy = new CustomerOriginStrategy
            {
                Credentials = $"{{\"user\":\"{username}\",\"password\": \"{password}\"}}"
            };
            
            // Act
            var result = await sut.GetBasicCredentialsForOriginStrategy(customerOriginStrategy);
            
            // Assert
            result.Password.Should().Be(password);
            result.User.Should().Be(username);
        }
        
        [Fact]
        public async Task GetBasicCredentialsForOriginStrategy_GetsS3Credentials()
        {
            // Arrange
            var customerOriginStrategy = new CustomerOriginStrategy
            {
                Credentials = "s3://eu-west-1/secret-storage/my-credentials"
            };
            
            const string username = "the-user";
            const string password = "wheesht";
            string json = $"{{\"user\":\"{username}\",\"password\": \"{password}\"}}";
            A.CallTo(() =>
                    bucketReader.GetObjectContentFromBucket(A<ObjectInBucket>.That.Matches(o =>
                        o.Key == "my-credentials" && o.Bucket == "secret-storage")))
                .Returns(json.ToMemoryStream());
            
            // Act
            var result = await sut.GetBasicCredentialsForOriginStrategy(customerOriginStrategy);
            
            // Assert
            result.Password.Should().Be(password);
            result.User.Should().Be(username);
        }
    }
}