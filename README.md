# 🐇 RabbitHole 🐇

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

## Behavior

```text
Publish to /queue/QueueName:
- attempts to create queue "/queue/QueueName"
- queue is bound to the default_exchange by RabbitMQ
- sends to default_exchange "", routing_key = "/queue/QueueName"

Publish to /topic/MyTopic:
- attempts to create fanout_exchange = "/topic/MyTopic"
- sends message to exchange "/topic/MyTopic", routing_key = ""
- let the fanout_exchange route the message to the possible bound keys

Subscribe to /queue/QueueName:
- consumes from queue named /queue/QueueName

Subscribe to /topic/MyTopic:
- ERROR or create a temp queue (delete: true)

Subscribe to "Consumer.ConsumerName.Topic.MyTopic"
- attempts to create queue "/topicQ/MyTopic/ConsumerName"
- attempts to binds queue "/topicQ/MyTopic/ConsumerName" to exchange "/topic/MyTopic"
- consumes from queue "/topicQ/MyTopic/ConsumerName"
```
