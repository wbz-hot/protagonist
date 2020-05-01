# Engine

Responds to requests from API to ingest both A/V and Image files. Exposes HTTP endpoint for synchronous ingestion or listens to queues for asynchronous ingestion.

## Getting Started

### Local Development

This section is split by Image and A/V ingestion but both may be relevant depending on what is being run locally.

#### Image Ingestion

If ingesting images then an image-processor implementation (e.g. [Tizer](https://github.com/tomcrane/jp2iser) or [Appetiser](https://github.com/digirati-co-uk/appetiser)) needs to be running locally (e.g. via Docker with a shared volume mount).

The port that this is listening on needs to be set in the `Engine:ImageIngest:ImageProcessorUrl` appsetting (see table below).

Below details the various appSettings that need to be configured (all under `Engine:` prefix which is ommited for clarity):

> There are some settings below, denoted by an asterix (*) that are _only_ for running Engine on a Windows environment. This is to simplify handling paths in dotnet code (Windows, `\` separator) and the ImageProcessor which is unix and requires Unix paths (`/` separator).

| Name                              | Example Value(s)                                          | Description                                                                           | Notes                                                                                                         |
|-----------------------------------|-----------------------------------------------------------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `ScratchRoot`                     | c:\\scratch\\imageproc\\                                  | This is the root of the scratch disk shared between Engine and image-processor        |                                                                                                               |
| `ImageProcessorRoot`              | /scratch/imageproc/                                       | This is the equivalent of the above but only required if running in a Win environment | If developing on Unix, omit this and use `ScratchRoot` only. If on Windows, this wil be used as {root}, below |
| `ImageIngest:SourceTemplate`      | {root}{customer}\{space}\{image}                          | Template used for building paths where image-processor images are located             | {root} is `ScratchRoot`, other elements from `Asset` being processed.                                         |
| `ImageIngest:DestinationTemplate` | {root}{customer}\{space}\{image}\output                   | Template used for building paths where image-processor will output jp2                | {root} is `ScratchRoot`, other elements from `Asset` being processed.                                         |
| `ImageIngest:ThumbsTemplate`      | {root}{customer}\{space}\{image}\output\thumbs            | Template used for building paths where image-processor will output thumbs             | {root} is `ScratchRoot`, other elements from `Asset` being processed.                                         |
| `ImageIngest:ImageProcessorUrl`   | http://localhost:5080/convert                             | URL for calling image-processor                                                       |                                                                                                               |
| `S3OriginRegex`                   | http\:\/\/mydlcs\-storage\-origin\.s3\.amazonaws\.com\/.* | Regex for identifying assets uploaded via Portal                                      |                                                                                                               |
| `S3Template`                      | s3://eu-west-1/mydlcs-prod-storage/{0}/{1}/{2}            | Used to set location for uploading ingested assets from a non-optimised source        |                                                                                                               |
| `Thumbs:ThumbsBucket`             | mydlcs-thumbs                                             | Bucket where thumbs are stored                                                        |                                                                                                               |
| `OrchestrateImageAfterIngest`     | true\false                                                | If True, an image is immediately orchestrated after ingestion                         |                                                                                                               |
| `OrchestratorBaseUrl`             | https://dlcs.mydlcs.io/                                   | Base URL for orchestrating assets                                                     |                                                                                                               |

