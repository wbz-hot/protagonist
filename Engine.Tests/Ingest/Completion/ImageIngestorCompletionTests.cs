using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Test.Helpers.Settings;
using DLCS.Test.Helpers.Web;
using Engine.Ingest;
using Engine.Ingest.Completion;
using Engine.Ingest.Image;
using Engine.Ingest.Workers;
using Engine.Settings;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Completion
{
    public class ImageIngestorCompletionTests
    {
        private readonly ControllableHttpMessageHandler httpHandler;
        private readonly EngineSettings engineSettings;
        private readonly IAssetRepository assetRepository;
        private readonly ImageIngestorCompletion sut;

        public ImageIngestorCompletionTests()
        {
            httpHandler = new ControllableHttpMessageHandler();
            engineSettings = new EngineSettings();
            assetRepository = A.Fake<IAssetRepository>();

            var optionsMonitor = OptionsHelpers.GetOptionsMonitor(engineSettings);
            
            var httpClient = new HttpClient(httpHandler);
            httpClient.BaseAddress = new Uri("http://orchestrator/");
            var orchestratorClient = new OrchestratorClient(httpClient, new NullLogger<OrchestratorClient>());
            sut = new ImageIngestorCompletion(orchestratorClient, assetRepository, optionsMonitor,
                new NullLogger<ImageIngestorCompletion>());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CompleteIngestion_CallsMarkIngestAsComplete_RegardlessOfSuccess(bool success)
        {
            // Arrange
            var asset = new Asset {Id = "2/1/foo-bar", Customer = 2, Space = 1};
            var imageLocation = new ImageLocation {Id = asset.Id};
            var imageStorage = new ImageStorage {Id = asset.Id};
            var context = new IngestionContext(asset, new AssetFromOrigin("213", 0, null, null));
            context.WithLocation(imageLocation).WithStorage(imageStorage);
            
            // Act
            await sut.CompleteIngestion(context, success, "");
            
            // Assert
            A.CallTo(() => assetRepository.UpdateIngestedAsset(asset, imageLocation, imageStorage)).MustHaveHappened();
        }

        [Theory]
        [InlineData(false, false, false, false)] // don't orchestrate, no success
        [InlineData(false, false, true, false)] // don't orchestrate, ingest success, mark fail
        [InlineData(false, false, false, true)] // don't orchestrate, ingest fail, mark success
        [InlineData(false, false, true, true)] // don't orchestrate, ingest + mark success
        [InlineData(true, false, false, false)] // don't orchestrate due to override, no success
        [InlineData(true, false, true, false)] // don't orchestrate due to override, ingest success, mark fail
        [InlineData(true, false, false, true)] // don't orchestrate due to override, ingest fail, mark success
        [InlineData(true, false, true, true)] // don't orchestrate due to override, ingest + mark success
        [InlineData(false, true, false, false)] // orchestrate due to override, no success
        [InlineData(false, true, true, false)] // orchestrate due to override, ingest success, mark fail
        [InlineData(false, true, false, true)] // orchestrate due to override, ingest fail, mark success
        [InlineData(true, true, false, false)] // orchestrate due to both, no success
        [InlineData(true, true, true, false)] // orchestrate due to both, ingest success, mark fail
        [InlineData(true, true, false, true)] // orchestrate due to both, ingest fail, mark success
        public async Task CompleteIngestion_DoesNotOrchestrate_IfAnyFail_OrOrchestrateAfterIngestFalse(
            bool theDefault, bool theOverride, bool ingestSuccess, bool markAsCompleteSuccess)
        {
            // Arrange
            var asset = new Asset {Id = "2/1/foo-bar", Customer = 2, Space = 1};
            var imageLocation = new ImageLocation {Id = asset.Id};
            var imageStorage = new ImageStorage {Id = asset.Id};
            var context = new IngestionContext(asset, new AssetFromOrigin("213", 0, null, null));
            context.WithLocation(imageLocation).WithStorage(imageStorage);
            engineSettings.OrchestrateImageAfterIngest = theDefault;
            engineSettings.CustomerOverrides.Add(asset.Customer.ToString(),
                new CustomerOverridesSettings {OrchestrateImageAfterIngest = theOverride});
            A.CallTo(() => assetRepository.UpdateIngestedAsset(asset, imageLocation, imageStorage))
                .Returns(markAsCompleteSuccess);

            // Act
            await sut.CompleteIngestion(context, ingestSuccess, "");

            // Assert
            httpHandler.CallsMade.Should().BeNullOrEmpty();
        }
        
        [Theory]
        [InlineData(false, true)] // orchestrate due to override
        [InlineData(true, true)] // orchestrate due to both
        public async Task CompleteIngestion_Orchestrates_IfOperationsSuccessful_AndOverrideOrDefaultTrue(
            bool theDefault, bool theOverride)
        {
            // Arrange
            var asset = new Asset {Id = "2/1/foo-bar", Customer = 2, Space = 1};
            var imageLocation = new ImageLocation {Id = asset.Id};
            var imageStorage = new ImageStorage {Id = asset.Id};
            var context = new IngestionContext(asset, new AssetFromOrigin("213", 0, null, null));
            context.WithLocation(imageLocation).WithStorage(imageStorage);
            engineSettings.OrchestrateImageAfterIngest = theDefault;
            engineSettings.CustomerOverrides.Add(asset.Customer.ToString(),
                new CustomerOverridesSettings {OrchestrateImageAfterIngest = theOverride});
            A.CallTo(() => assetRepository.UpdateIngestedAsset(asset, imageLocation, imageStorage))
                .Returns(true);
            httpHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            await sut.CompleteIngestion(context, true, "");

            // Assert
            httpHandler.CallsMade.Should().Contain("http://orchestrator/iiif-image/2/1/foo-bar/info.json");
        }
    }
}