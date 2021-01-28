🐇 RabbitHole 🐇

A playground for creating publishers and subscribers to a RabbitMQ broker using C#!

## Development: running the worker

Go to the `RabbitcsNewOrdersWorker` project:

```sh
# starts rabbitmq broker
docker-compose up -d rabbitmq

# dotnet run's the worker
cd RabbitcsNewOrdersWorker
dotnet run
```

The worker should be up and running to a queue `/queue/test-destination` (must have been previously created on RabbitMQ):

```text
[00:17:21 INF] Starting subscription...
[00:17:21 INF] =^.^=: Waiting for messages...
[00:17:21 INF] =^.^=: Press [enter] to exit...
```
