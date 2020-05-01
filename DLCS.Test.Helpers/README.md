# DLCS.Test.Helpers

This project contains a few helper classes that are useful when unit testing. These are detailed below:

## OptionsHelpers.cs

Simplifies setting up `IOptionsMonitor<T>` objects for testing. Uses [FakeItEasy](https://fakeiteasy.github.io/) for mocking.

### Usage

```csharp
var engineSettings = new EngineSettings { MySetting = "string1" };
IOptionsMonitor<EngineSettings> optionsMonitor = OptionsHelpers.GetOptionsMonitor(engineSettings);
```

## TestBucketReader

Fake object that can be used in place of `IBucketReader` when testing classes that use `IBucketReader.Delete*`, `IBucketReader.Write*` and `IBucketReader.Copy*` methods. 

Internally stores a list of operations, including key and `.Content` or `.FilePath` of operations (if an object is copied, it uses the original objects `.Content` or `.FilePath`).

Primarily this is to make verifying that specific keys have been written/copied without verifying individual calls (e.g. with `A.CallTo() => mockReader.CopyWithinBucket("", "", "")).MustHaveHappened()`), which is handy when doing things like uploading X thumbnails.

### Usage

```csharp
public class SystemUnderTest
{
    private readonly IBucketReader bucketReader;
    public SystemUnderTest(IBucketReader bucketReader)
    {
        this.bucketReader = bucketReader;
    }

    public async Task MethodUnderTest(string path)
    {
        // Upload file to /key1
        var key = new ObjectInBucket("myBucket", "/key1");
        await bucketReader.WriteFileToBucket(key, path);

        // Clone key (same bucket, /key2)
        var otherKey = key.CloneWithKey("/key2");

        // The following 2 calls have the same net effect
        // using TestBucketReader saves test knowing how the object was uploaded
        await bucketReader.CopyWithinBucket(key.Bucket, key.Key, otherKey.Key); // OR
        await bucketReader.WriteFileToBucket(otherKey, path);
    }
}

public class UnitTests
{
    [Fact]
    public async Task TheTest()
    {
        // Arrange
        var bucketReader = new BucketReader();
        // bucketReader.SeedPaths("/existingkey1");
        
        var sut = new SystemUnderTest(bucketReader);
        const string filePath = "/path/to/file";

        // Act
        await sut.MethodUnderTest(filePath);

        // Assert
        bucketReader.ShouldHaveKey("/key1").WithFilePath(filePath);
        bucketReader.ShouldHaveKey("/key2").WithFilePath(filePath);
        // OR bucketReader.ShouldHaveKey("/key2").WithContents("{\"foo\":\"bar\"}");
        bucketReader.ShouldNotHaveKey("/key3"); // can be used if a key has been deleted
        bucketReader.ShouldHaveNoUnverifiedPaths(); // ensures no additional keys uploaded
    }
}
```

## ControllableHttpMessageHandler.cs

A basic [`HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=netcore-3.1) implementation to assist when testing a class that uses `HttpClient`. `ControllableHttpMessageHandler` stores an internal list of all URLs called and can return a precanned response, or a callback can be registered which is called whenever a request is made.

### Usage

```csharp
var httpHandler = new ControllableHttpMessageHandler();

// Register a specific response
var response = httpHandler.GetResponseMessage("{\"foo\":\"bar\"}", HttpStatusCode.OK);
httpHandler.SetResponse(response);

// Or register a callback which records authorization header, which can be verified later
string authHeader = null;
httpHandler.RegisterCallback(message => actualAuthHeader = message.Headers.Authorization.ToString());

var httpClient = new HttpClient(httpHandler);
var sut = new SystemUnderTest(httpClient);
await sut.MethodUnderTest();

// .CallsMade is a list of all URLs called.
httpHandler.CallsMade.Should().Contain(originUri);
```