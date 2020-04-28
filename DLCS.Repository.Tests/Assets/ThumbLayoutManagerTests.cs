using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Policies;
using DLCS.Model.Storage;
using DLCS.Repository.Assets;
using DLCS.Test.Helpers;
using DLCS.Test.Helpers.Storage;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace DLCS.Repository.Tests.Assets
{
    public class ThumbLayoutManagerTests
    {
        private readonly IAssetRepository assetRepository;
        private readonly IPolicyRepository thumbPolicyRepository;

        public ThumbLayoutManagerTests()
        {
            assetRepository = A.Fake<IAssetRepository>();
            thumbPolicyRepository = A.Fake<IPolicyRepository>();
        }

        private ThumbLayoutManager GetSut(IBucketReader bucketReader)
            => new ThumbLayoutManager(bucketReader, new NullLogger<ThumbLayoutManager>(), assetRepository, thumbPolicyRepository);

        [Fact]
        public async Task EnsureNewLayout_DoesNothing_IfSizesJsonExists()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .Returns(new[] {"2/1/the-astronaut/s.json", "2/1/the-astronaut/200.jpg"});
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert
            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task EnsureNewLayout_CreatesExpectedResources_AllOpen()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .Returns(new[] {"2/1/the-astronaut/200.jpg"});

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset {Width = 4000, Height = 8000, ThumbnailPolicy = "TheBestOne"});
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert

            // move jpg per thumbnail size
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/low.jpg",
                        "2/1/the-astronaut/open/400.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/100,200/0/default.jpg",
                        "2/1/the-astronaut/open/200.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/50,100/0/default.jpg",
                        "2/1/the-astronaut/open/100.jpg"))
                .MustHaveHappened();

            // create sizes.json
            const string expected = "{\"o\":[[200,400],[100,200],[50,100]],\"a\":[]}";
            A.CallTo(() =>
                    bucketReader.WriteToBucket(
                        A<ObjectInBucket>.That.Matches(o =>
                            o.Bucket == "the-bucket" && o.Key == "2/1/the-astronaut/s.json"), expected,
                        "application/json"))
                .MustHaveHappened();
        }

        [Fact]
        public async Task EnsureNewLayout_CreatesExpectedResources_AllAuth()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .Returns(new[] {"2/1/the-astronaut/200.jpg"});

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset
                {
                    Width = 2000, Height = 4000, ThumbnailPolicy = "TheBestOne", MaxUnauthorised = 0,
                    Roles = new List<string> {"admin"}
                });
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert

            // move jpg per thumbnail size
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/low.jpg",
                        "2/1/the-astronaut/auth/400.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/100,200/0/default.jpg",
                        "2/1/the-astronaut/auth/200.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/50,100/0/default.jpg",
                        "2/1/the-astronaut/auth/100.jpg"))
                .MustHaveHappened();

            // create sizes.json
            const string expected = "{\"o\":[],\"a\":[[200,400],[100,200],[50,100]]}";
            A.CallTo(() =>
                    bucketReader.WriteToBucket(
                        A<ObjectInBucket>.That.Matches(o =>
                            o.Bucket == "the-bucket" && o.Key == "2/1/the-astronaut/s.json"), expected,
                        "application/json"))
                .MustHaveHappened();
        }

        [Fact]
        public async Task EnsureNewLayout_CreatesExpectedResources_MixedAuthAndOpen()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .Returns(new[] {"2/1/the-astronaut/200.jpg"});

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset
                {
                    Width = 2000, Height = 4000, ThumbnailPolicy = "TheBestOne", MaxUnauthorised = 350,
                    Roles = new List<string> {"admin"}
                });
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {1024, 400, 200, 100}});
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert

            // move jpg per thumbnail size
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/low.jpg",
                        "2/1/the-astronaut/auth/1024.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/200,400/0/default.jpg",
                        "2/1/the-astronaut/auth/400.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/100,200/0/default.jpg",
                        "2/1/the-astronaut/open/200.jpg"))
                .MustHaveHappened();
            A.CallTo(() =>
                    bucketReader.CopyWithinBucket("the-bucket",
                        "2/1/the-astronaut/full/50,100/0/default.jpg",
                        "2/1/the-astronaut/open/100.jpg"))
                .MustHaveHappened();

            // create sizes.json
            const string expected = "{\"o\":[[100,200],[50,100]],\"a\":[[512,1024],[200,400]]}";
            A.CallTo(() =>
                    bucketReader.WriteToBucket(
                        A<ObjectInBucket>.That.Matches(o =>
                            o.Bucket == "the-bucket" && o.Key == "2/1/the-astronaut/s.json"), expected,
                        "application/json"))
                .MustHaveHappened();
        }

        [Fact]
        public async Task EnsureNewLayout_DeletesOldConfinedSquareLayout()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .Returns(new[]
                {
                    "2/1/the-astronaut/low.jpg", "2/1/the-astronaut/100.jpg", "2/1/the-astronaut/sizes.json",
                    "2/1/the-astronaut/full/50,100/0/default.jpg"
                });

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset {Width = 4000, Height = 8000, ThumbnailPolicy = "TheBestOne"});
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {200, 100}});
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert
            var expectedDeletions = new[]
            {
                "the-bucket:::2/1/the-astronaut/100.jpg", "the-bucket:::2/1/the-astronaut/sizes.json"
            };

            A.CallTo(() => bucketReader.DeleteFromBucket(A<ObjectInBucket[]>.That.Matches(a =>
                expectedDeletions.Contains(a[0].ToString()) && expectedDeletions.Contains(a[1].ToString())
            ))).MustHaveHappened();
        }

        [Fact]
        public async Task EnsureNewLayout_DoesNotMakeConcurrentAttempts_ForSameKey()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var fakeBucketContents = new List<string> {"2/1/the-astronaut/200.jpg"};

            A.CallTo(() => bucketReader.GetMatchingKeys(rootKey))
                .ReturnsLazily(() => fakeBucketContents.ToArray());

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset {Width = 200, Height = 250, ThumbnailPolicy = "TheBestOne"});
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});

            // Once called, add sizes.json to return list of bucket contents
            A.CallTo(() => bucketReader.WriteToBucket(A<ObjectInBucket>._, A<string>._, A<string>._))
                .Invokes(() => fakeBucketContents.Add("2/1/the-astronaut/s.json"));

            A.CallTo(() => bucketReader.CopyWithinBucket(A<string>._, A<string>._, A<string>._))
                .Invokes(async () => await Task.Delay(500));

            var sut = GetSut(bucketReader);

            var ensure1 = Task.Factory.StartNew(() => sut.EnsureNewLayout(rootKey));
            var ensure2 = Task.Factory.StartNew(() => sut.EnsureNewLayout(rootKey));

            // Act
            await Task.WhenAll(ensure1, ensure2);

            // Assert
            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task EnsureNewLayout_AllowsConcurrentAttempts_ForDifferentKey()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var key1 = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var key2 = new ObjectInBucket("another-bucket", "2/1/the-astronaut/");
            var key3 = new ObjectInBucket("the-bucket", "3/1/the-astronaut/");

            var fakeBucketContents = new List<string> {"2/1/the-astronaut/200.jpg"};

            A.CallTo(() => bucketReader.GetMatchingKeys(key1))
                .ReturnsLazily(() => fakeBucketContents.ToArray());

            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .Returns(new Asset {Width = 200, Height = 250, ThumbnailPolicy = "TheBestOne"});
            A.CallTo(() => thumbPolicyRepository.GetThumbnailPolicy("TheBestOne"))
                .Returns(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});

            // Once called, add sizes.json to return list of bucket contents
            A.CallTo(() => bucketReader.WriteToBucket(A<ObjectInBucket>._, A<string>._, A<string>._))
                .Invokes((ObjectInBucket dest, string content, string contentType) =>
                    fakeBucketContents.Add(dest.Key + "sizes.json"));

            A.CallTo(() => bucketReader.CopyWithinBucket(A<string>._, A<string>._, A<string>._))
                .Invokes(async () => await Task.Delay(500));

            var sut = GetSut(bucketReader);

            var ensure1 = Task.Factory.StartNew(() => sut.EnsureNewLayout(key1));
            var ensure2 = Task.Factory.StartNew(() => sut.EnsureNewLayout(key2));
            var ensure3 = Task.Factory.StartNew(() => sut.EnsureNewLayout(key3));

            // Act
            await Task.WhenAll(ensure1, ensure2, ensure3);

            // Assert
            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .MustHaveHappened(3, Times.Exactly);
        }

        [Fact]
        public async Task EnsureNewLayout_AssetNotFound()
        {
            // Arrange
            var bucketReader = A.Fake<IBucketReader>();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/doesnotexit/");
            
            A.CallTo(() => assetRepository.GetAsset(rootKey.Key.TrimEnd('/')))
                .Returns<Asset>(null);
            
            var sut = GetSut(bucketReader);

            // Act
            await sut.EnsureNewLayout(rootKey);

            // Assert
            A.CallTo(() => assetRepository.GetAsset(A<string>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task CreateNewThumbs_DoesNothing_IfThumbsOnDiskNull()
        {
            // Arrange
            var bucketReader = new TestBucketReader("the-bucket");
            var asset = new Asset();
            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var sut = GetSut(bucketReader);

            // Act
            await sut.CreateNewThumbs(asset, Enumerable.Empty<ThumbOnDisk>(), rootKey);

            bucketReader.Operations.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateNewThumbs_CreatesExpectedResources_AllOpen()
        {
            // Arrange
            var bucketReader = new TestBucketReader("the-bucket");
            var asset = new Asset {Width = 4000, Height = 8000};
            asset.WithThumbnailPolicy(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});

            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var sut = GetSut(bucketReader);

            var thumbsOnDisk = new List<ThumbOnDisk>
            {
                new ThumbOnDisk {Width = 200, Height = 400, Path = "/test/size_400.jpg"},
                new ThumbOnDisk {Width = 100, Height = 200, Path = "/test/size_200.jpg"},
                new ThumbOnDisk {Width = 50, Height = 100, Path = "/test/size_100.jpg"},
            };
            var fileSizes = new ThumbnailSizes(new List<int[]>
            {
                new[] {200, 400}, new[] {100, 200}, new[] {50, 100}
            }, new List<int[]>());

            // Act
            await sut.CreateNewThumbs(asset, thumbsOnDisk, rootKey);

            // Assert

            // new
            bucketReader.ShouldHaveKey("2/1/the-astronaut/open/400.jpg").WithFilePath("/test/size_400.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/open/200.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/open/100.jpg").WithFilePath("/test/size_100.jpg");
            var sizesJson = JsonConvert.SerializeObject(fileSizes);
            bucketReader.ShouldHaveKey("2/1/the-astronaut/s.json").WithContents(sizesJson);

            // legacy
            bucketReader.ShouldHaveKey("2/1/the-astronaut/low.jpg").WithFilePath("/test/size_400.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,/0/default.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,200/0/default.jpg")
                .WithFilePath("/test/size_200.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,/0/default.jpg").WithFilePath("/test/size_100.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,100/0/default.jpg")
                .WithFilePath("/test/size_100.jpg");

            bucketReader.ShouldHaveNoUnverifiedPaths();
        }

        [Fact]
        public async Task CreateNewThumbs_CreatesExpectedResources_AllAuth()
        {
            // Arrange
            var bucketReader = new TestBucketReader("the-bucket");
            var asset = new Asset
                {Width = 4000, Height = 8000, MaxUnauthorised = 0, Roles = new List<string> {"admin"}};
            asset.WithThumbnailPolicy(new ThumbnailPolicy {Sizes = new List<int> {400, 200, 100}});

            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var sut = GetSut(bucketReader);

            var thumbsOnDisk = new List<ThumbOnDisk>
            {
                new ThumbOnDisk {Width = 200, Height = 400, Path = "/test/size_400.jpg"},
                new ThumbOnDisk {Width = 100, Height = 200, Path = "/test/size_200.jpg"},
                new ThumbOnDisk {Width = 50, Height = 100, Path = "/test/size_100.jpg"},
            };
            var fileSizes = new ThumbnailSizes(new List<int[]>(),
                new List<int[]>
                {
                    new[] {200, 400}, new[] {100, 200}, new[] {50, 100}
                });

            // Act
            await sut.CreateNewThumbs(asset, thumbsOnDisk, rootKey);

            // Assert

            // new
            bucketReader.ShouldHaveKey("2/1/the-astronaut/auth/400.jpg").WithFilePath("/test/size_400.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/auth/200.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/auth/100.jpg").WithFilePath("/test/size_100.jpg");
            var sizesJson = JsonConvert.SerializeObject(fileSizes);
            bucketReader.ShouldHaveKey("2/1/the-astronaut/s.json").WithContents(sizesJson);

            // legacy
            bucketReader.ShouldHaveKey("2/1/the-astronaut/low.jpg").WithFilePath("/test/size_400.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,/0/default.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,200/0/default.jpg")
                .WithFilePath("/test/size_200.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,/0/default.jpg").WithFilePath("/test/size_100.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,100/0/default.jpg")
                .WithFilePath("/test/size_100.jpg");

            bucketReader.ShouldHaveNoUnverifiedPaths();
        }

        [Fact]
        public async Task CreateNewThumbs_CreatesExpectedResources_MixedAuthAndOpen()
        {
            // Arrange
            var bucketReader = new TestBucketReader("the-bucket");
            var asset = new Asset
                {Width = 4000, Height = 8000, MaxUnauthorised = 350, Roles = new List<string> {"admin"}};
            asset.WithThumbnailPolicy(new ThumbnailPolicy {Sizes = new List<int> {1024, 400, 200, 100}});

            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var sut = GetSut(bucketReader);

            var thumbsOnDisk = new List<ThumbOnDisk>
            {
                new ThumbOnDisk {Width = 512, Height = 1024, Path = "/test/size_1024.jpg"},
                new ThumbOnDisk {Width = 200, Height = 400, Path = "/test/size_400.jpg"},
                new ThumbOnDisk {Width = 100, Height = 200, Path = "/test/size_200.jpg"},
                new ThumbOnDisk {Width = 50, Height = 100, Path = "/test/size_100.jpg"},
            };
            var fileSizes = new ThumbnailSizes(
                new List<int[]> {new[] {100, 200}, new[] {50, 100}},
                new List<int[]> {new[] {512, 1024}, new[] {200, 400}});

            // Act
            await sut.CreateNewThumbs(asset, thumbsOnDisk, rootKey);

            // Assert

            // new
            bucketReader.ShouldHaveKey("2/1/the-astronaut/auth/1024.jpg").WithFilePath("/test/size_1024.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/auth/400.jpg").WithFilePath("/test/size_400.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/open/200.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/open/100.jpg").WithFilePath("/test/size_100.jpg");
            var sizesJson = JsonConvert.SerializeObject(fileSizes);
            bucketReader.ShouldHaveKey("2/1/the-astronaut/s.json").WithContents(sizesJson);

            // legacy
            bucketReader.ShouldHaveKey("2/1/the-astronaut/low.jpg").WithFilePath("/test/size_1024.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/200,/0/default.jpg").WithFilePath("/test/size_400.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/200,400/0/default.jpg")
                .WithFilePath("/test/size_400.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,/0/default.jpg").WithFilePath("/test/size_200.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/100,200/0/default.jpg")
                .WithFilePath("/test/size_200.jpg");

            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,/0/default.jpg").WithFilePath("/test/size_100.jpg");
            bucketReader.ShouldHaveKey("2/1/the-astronaut/full/50,100/0/default.jpg")
                .WithFilePath("/test/size_100.jpg");

            bucketReader.ShouldHaveNoUnverifiedPaths();
        }

        [Fact]
        public async Task CreateNewThumbs_DeletesOldConfinedSquareLayout()
        {
            // Arrange
            var bucketReader = new TestBucketReader("the-bucket");
            const string oldSizesJson = "2/1/the-astronaut/sizes.json";
            const string oldConfinedThumb = "2/1/the-astronaut/100.jpg";
            bucketReader.SeedPaths(new[]
            {
                "2/1/the-astronaut/low.jpg", oldConfinedThumb, oldSizesJson,
                "2/1/the-astronaut/full/50,100/0/default.jpg"
            });

            var asset = new Asset
                {Width = 4000, Height = 8000, MaxUnauthorised = 350, Roles = new List<string> {"admin"}};
            asset.WithThumbnailPolicy(new ThumbnailPolicy {Sizes = new List<int> {200, 100}});

            var rootKey = new ObjectInBucket("the-bucket", "2/1/the-astronaut/");
            var sut = GetSut(bucketReader);

            var thumbsOnDisk = new List<ThumbOnDisk>
            {
                new ThumbOnDisk {Width = 100, Height = 200, Path = "/test/size_200.jpg"},
                new ThumbOnDisk {Width = 50, Height = 100, Path = "/test/size_100.jpg"},
            };

            // Act
            await sut.CreateNewThumbs(asset, thumbsOnDisk, rootKey);

            // Assert
            bucketReader
                .ShouldNotHaveKey(oldConfinedThumb)
                .ShouldNotHaveKey(oldSizesJson);
        }
    }
}