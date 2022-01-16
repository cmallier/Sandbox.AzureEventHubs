# Sandbox.AzureEventHubs


## Disclaimer

This project is done for learning purposes. It is not necessarily production quality code.


## Overview

__Azure Event Hubs__ is a __big data streaming platform and event ingestion service__. It can receive and process __millions of events per second__.

__Data__ sent to an event hub __can be transformed__ and __stored__ by __using__ any __real-time analytics__ provider or __batching/storage__ adapters.

It can also be configured to __scale dynamically__, when required, to handle increased throughput.

![Overview](./Assets/event-hub-overview.png)

An __entity__ that __sends data__ to your event hub is called a __publisher__, and an entity that __reads data__ from an event hub is called a __consumer, or a subscriber__. Your event hub sits between the publisher and subscriber to divide the production (from the publisher) and consumption (to a subscriber) of an event stream


## Events

An event is a __small packet of information (a datagram)__ that contains a notification.

Events can be __published individually or in batches__, but a single publication (individual or batch) __can't exceed 1 MB.__


## Publishers and subscribers

Event publishers are any app or device that can send out events using either

- HTTPS
- Advanced Message Queuing Protocol (AMQP) 1.0
- Apache Kafka


If __publishers__ that __send data frequently__, __AMQP has better performance__. However, it has a higher initial session overhead, because a persistent bidirectional socket and transport-level security (TLS), or SSL/TLS, has to be set up first.

For more __intermittent publishing__, __HTTPS is the better option__. Though HTTPS requires additional overhead for each request, there is no session initialization overhead.

Event Hubs provides an endpoint compatible with the Apache Kafka producer and consumer APIs

Event subscribers are apps that use one of two supported programmatic methods to receive and process events from an event hub.

- EventHubReceiver - A simple method that provides limited management options.
- EventProcessorHost - An efficient method


## Consumer groups

An __event hub consumer__ group __represents__ a __specific view of an event hub data stream__. By using separate consumer groups, __multiple subscriber__ apps __can process__ an event stream __independently__, and without affecting other apps. However, the use of many consumer groups __isn't a requirement__, and for many apps, the single default consumer group is sufficient.

## Configure

- Event Hubs Namespace (Container) [Princing, Performance, Redundancy, Auto-Scaling]
  - Event Hub (Partion count(default=4), Retention(default=7))
  - Event Hub
  - Event Hub
  - ...

\<namespace\>.servicebus.windows.net



## Azure Cli

```bh
> az configure --defaults group=[Resource Group] location=eastus

> NS_NAME=ehubns-$RANDOM

> az eventhubs namespace create --name $NS_NAME

> az eventhubs namespace authorization-rule keys list \
    --name RootManageSharedAccessKey \
    --namespace-name $NS_NAME

> HUB_NAME=hubname-$RANDOM

> az eventhubs eventhub create --name $HUB_NAME --namespace-name $NS_NAME

> az eventhubs eventhub show --namespace-name $NS_NAME --name $HUB_NAME

> STORAGE_NAME=storagename$RANDOM

> az storage account create --name $STORAGE_NAME --sku Standard_RAGRS --encryption-service blob

> az storage account keys list --account-name $STORAGE_NAME

> az storage account show-connection-string -n $STORAGE_NAME
```

## Information / Learn / Credits

- [Module - Learn](https://docs.microsoft.com/en-us/learn/modules/enable-reliable-messaging-for-big-data-apps-using-event-hubs/?WT.mc_id=cloudskillschallenge_4B2F91E9-04C5-4A1C-8F67-443ADEFD0806)

- [Azure Event Hubs from Apache Kafka applications](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-for-kafka-ecosystem-overview)

- [Event Hubs pricing](https://azure.microsoft.com/en-us/pricing/details/event-hubs/)

- [Git Project](https://github.com/Azure/azure-event-hubs.git)




