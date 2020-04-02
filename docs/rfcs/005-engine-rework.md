# Rework Engine 

## Existing Implementation

Currently there are 2 deployed engine services in the DLCS - `engine-tizer` and `engine-video`, using the same code base with differing configuration.

### Ingestion 

Both implementations follow the bulk of the same execution path, with only ingestion differing based on the image family.

#### Family = "I"

"I" = Image (jpg, tiff etc).

There are 1:n `engine-tizer` instances running at any given time.

This has 2 execution modes: 

* asynchronous via SQS queues - `IncomingQueue` and `PriorityIncomingQueue`. Both queue handlers execute the same code, the priority queue will be quieter and executed quicker.
* synchronous via an http request to `/image-ingest`

Images are ingested by making HTTP requests to [tizer](https://github.com/tomcrane/jp2iser).

#### Family = "T"

"T" = Timebased media, this is any a/v files (mp3, mp4, wav etc).

There is only ever a single `engine-video` instance running. This uses the concept of a "ActivityGroup" to manage a critical section to ensure that only a single video is processed at any given time.

Ingestion is done by ElasticTranscoder service, via the [SpaceBunny](https://github.com/dlcs/spacebunny) wrapper. All executions are done asynchronously via SQS.

#### Family "F"

"F" = File, this will be anything not covered by other 2 families (e.g. pdf).

This will be handled by `engine-tizer` but no specific ingestion happens (Tizer or SpaceBunny calls are not made).

### HTTP Endpoints

* `/health.aspx` - used for manual calls to check store health.
* `/ping.aspx` - used for health checks.
* `/image-ingest` - used to synchronously ingest an image (called by API). Only `engine-tizer` instances can respond to this as `engine-video` instances are not registered with ELB.

## Proposed Changes

Refactor the engine to use dotnetcore 3.x with more conventional procedural processing, keeping the vast majority of the processing the same (albeit not using behavioural framework but there should be logic that can be borrowed).

1. Remove the SpaceBunny wrapper and called ElasticTranscoder directly. This will simplify the overall process and reduce the number of required SQS queues.
2. Remove the use of "ActivityGroup" table as a semaphore, instead only process 1 message from queue at a time. "ActivityGroup" can be dropped from the database.

The following points need to be remembered or taken into considerations:

* HTTP/message contracts should not change.
* The API can receive batches but the Engine will receive these 1 by 1. The logic for finalising batches will need to be migrated.
* Can dotnetcore 3 apps handle unix signals when running in ECS, if not [Sentinel](https://github.com/fractos/sentinel) still needs to be used.
* Should we have 2 deployments of the same codebase, or multiple instances?
* Use SQS + SNS for subscriptions rather than just SQS?
* Could be an opportunity to switch over to using [Appetiser](https://github.com/digirati-co-uk/appetiser) for image ingestion.

### Switch Over

The following is a rough suggestion for how we could switch over to using the engine rewrite whilst allowing us to rollback if required. These could be done at once or code written in the following order allowing us to do a gradual rollout - how much additional work can drive this decision.

1. Direct ingestion via HTTP (`/image-ingest`). `engine-image-new` could be deployed alongside `engine-tizer`. The API can be updated to call the former rather than the latter for synchronous ingests only.
2. Ingest images from queue (incoming and priority-incoming). API updated to add images to a new ingest queue that `engine-image-new` is subscribed to. All image ingests (sync and async) processed by new rewrite.
3. Ingest a/v from queue. As (2), using new queues to allow backout if things are incorrect.
4. Remove `engine-tizer` and `engine-video` and any associated items (e.g. SQS queues if using new ones).

### Verifying

To verify rewrite has the same behaviour the following tests will be required:

Ingest image (I)
- jpg, png, jp2, tiff - more?
- priority and normal ingest
- queue and direct ingestion
- ingest + reingest

Ingest video (T)
- mp4, webm, mpg - more?
- ingest + reingest

Ingest audio (T)
- mp3, wav - more?
- ingest + reingest

Ingest file (F)
- ingest + reingest

Above for origin-strategies
- basic-http-authentication - Optimised (for I)
- basic-http-authentication - Not Optimised
- s3-ambient - Optimised (for I)
- s3-ambient - Not Optimised
- sftp - Optimised (for I)
- sftp - Not Optimised
- default

Assert files written correctly. DB updated as expected.