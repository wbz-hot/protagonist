# Engine

Responds to requests from API to ingest both A/V and Image files. Exposes HTTP endpoint for synchronous ingestion or listens to queues for asynchronous ingestion.

## Getting Started

### Local Development

The engine can be run locally without the need for AWS access for SQS, e.g. by using [goaws](https://github.com/p4tin/goaws) or [localstack](https://github.com/localstack/localstack). e.g. 

```bash
docker pull pafortin/goaws
docker run -d --name goaws -p 4100:4100 pafortin/goaws
```

The `QueueSettings:UseLocal` and `QueueSettings:LocalRoot` appsettings will be used to configure SQS listeners. 
