using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Sandbox.AzureEventHubs;

internal class AzureEventHubsApp
{
    //private static EventHubClient eventHubClient;
    private static bool SetRandomPartitionKey = false;
    private static BlobContainerClient storageClient;
    private static EventProcessorClient processor;
    ConcurrentDictionary<string, int> partitionEventCount = new ConcurrentDictionary<string, int>();

    public async Task Start()
    {
        //await Send();
        await Process();
    }


    // Private methods
    private async Task Send()
    {
        var producer = new EventHubProducerClient( Constants.AzureEventHubConnectionString, Constants.AzureEventHubName );

        try
        {
            using EventDataBatch eventBatch = await producer.CreateBatchAsync();

            for( var counter = 0; counter < 500; ++counter )
            {
                var eventBody = new BinaryData( $"Event Number: { counter }" );
                var eventData = new EventData( eventBody );

                if( !eventBatch.TryAdd( eventData ) )
                {
                    // At this point, the batch is full but our last event was not
                    // accepted.  For our purposes, the event is unimportant so we
                    // will intentionally ignore it.  In a real-world scenario, a
                    // decision would have to be made as to whether the event should
                    // be dropped or published on its own.

                    break;
                }
            }

            // When the producer publishes the event, it will receive an
            // acknowledgment from the Event Hubs service; so long as there is no
            // exception thrown by this call, the service assumes responsibility for
            // delivery.  Your event data will be published to one of the Event Hub
            // partitions, though there may be a (very) slight delay until it is
            // available to be consumed.

            await producer.SendAsync( eventBatch );
        }
        catch
        {
            // Transient failures will be automatically retried as part of the
            // operation. If this block is invoked, then the exception was either
            // fatal or all retries were exhausted without a successful publish.
        }
        finally
        {
            await producer.CloseAsync();
        }

        Console.WriteLine( "Press any key to exit." );
        Console.ReadLine();
    }


    // Legacy Code
    //private async Task Send()
    //{
    //    var connectionStringBuilder = new EventHubsConnectionStringBuilder( Constants.AzureEventHubConnectionString )
    //    {
    //        EntityPath = Constants.AzureEventHubName
    //    };

    //    eventHubClient = EventHubClient.CreateFromConnectionString( connectionStringBuilder.ToString() );

    //    await SendMessagesToEventHub( 100 );

    //    await eventHubClient.CloseAsync();

    //    Console.WriteLine( "Press any key to exit." );
    //    Console.ReadLine();
    //}

    //private static async Task SendMessagesToEventHub( int numMessagesToSend )
    //{
    //    var random = new Random();

    //    for( var i = 0; i < numMessagesToSend; i++ )
    //    {
    //        try
    //        {
    //            var message = $"Message {i}";

    //            if( SetRandomPartitionKey )
    //            {
    //                var pKey = Guid.NewGuid().ToString();
    //                await eventHubClient.SendAsync( new EventData( Encoding.UTF8.GetBytes( message ) ), pKey );
    //                Console.WriteLine( $"Sent message: '{message}' Partition Key: '{pKey}'" );
    //            }
    //            else
    //            {
    //                await eventHubClient.SendAsync( new EventData( Encoding.UTF8.GetBytes( message ) ) );
    //                Console.WriteLine( $"Sent message: '{message}'" );
    //            }
    //        }
    //        catch( Exception exception )
    //        {
    //            Console.WriteLine( $"{DateTime.Now} > Exception: {exception.Message}" );
    //        }

    //        await Task.Delay( 10 );
    //    }

    //    Console.WriteLine( $"{numMessagesToSend} messages sent." );
    //}

    public async Task Process()
    {
        try
        {
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter( TimeSpan.FromSeconds( 30 ) );

            // Storage client
            var storageClient = new BlobContainerClient( Constants.AzureBlobStorageConnectionString, Constants.AzureBlobContainerName );

            // EventProcessorClient
            var processor = new EventProcessorClient( storageClient, "$Default", Constants.AzureEventHubConnectionString, Constants.AzureEventHubName );
            processor.ProcessEventAsync += ProcessEventHandler;
            processor.ProcessErrorAsync += ProcessErrorHandler;


            try
            {
                await processor.StartProcessingAsync( cancellationSource.Token );
                await Task.Delay( Timeout.Infinite, cancellationSource.Token );
            }
            catch( TaskCanceledException )
            {
                // This is expected if the cancellation token is
                // signaled.
            }
            finally
            {
                // This may take up to the length of time defined
                // as part of the configured TryTimeout of the processor;
                // by default, this is 60 seconds.

                await processor.StopProcessingAsync();
            }
        }
        catch
        {
            // The processor will automatically attempt to recover from any
            // failures, either transient or fatal, and continue processing.
            // Errors in the processor's operation will be surfaced through
            // its error handler.
            //
            // If this block is invoked, then something external to the
            // processor was the source of the exception.
        }
        finally
        {
            // It is encouraged that you unregister your handlers when you have
            // finished using the Event Processor to ensure proper cleanup.  This
            // is especially important when using lambda expressions or handlers
            // in any form that may contain closure scopes or hold other references.

            processor.ProcessEventAsync -= ProcessEventHandler;
            processor.ProcessErrorAsync -= ProcessErrorHandler;
        }
    }

    async Task ProcessEventHandler( ProcessEventArgs eventArgs )
    {
        try
        {
            // If the cancellation token is signaled, then the
            // processor has been asked to stop.  It will invoke
            // this handler with any events that were in flight;
            // these will not be lost if not processed.
            //
            // It is up to the handler to decide whether to take
            // action to process the event or to cancel immediately.

            if( eventArgs.CancellationToken.IsCancellationRequested )
            {
                return;
            }

            string partition = eventArgs.Partition.PartitionId;
            byte[] eventBody = eventArgs.Data.EventBody.ToArray();
            Debug.WriteLine( $"Event from partition { partition } with length { eventBody.Length }." );

            int eventsSinceLastCheckpoint = partitionEventCount.AddOrUpdate(
                key: partition,
                addValue: 1,
                updateValueFactory: ( _, currentCount ) => currentCount + 1 );

            if( eventsSinceLastCheckpoint >= 50 )
            {
                await eventArgs.UpdateCheckpointAsync();
                partitionEventCount[partition] = 0;
            }
        }
        catch
        {
            // It is very important that you always guard against
            // exceptions in your handler code; the processor does
            // not have enough understanding of your code to
            // determine the correct action to take.  Any
            // exceptions from your handlers go uncaught by
            // the processor and will NOT be redirected to
            // the error handler.
        }
    }

    Task ProcessErrorHandler( ProcessErrorEventArgs eventArgs )
    {
        try
        {
            Console.WriteLine( "Error in the EventProcessorClient" );
            Console.WriteLine( $"- Operation: { eventArgs.Operation }" );
            Console.WriteLine( $"- Exception: { eventArgs.Exception }" );
            Console.WriteLine( "" );
        }
        catch
        {
            // It is very important that you always guard against
            // exceptions in your handler code; the processor does
            // not have enough understanding of your code to
            // determine the correct action to take.  Any
            // exceptions from your handlers go uncaught by
            // the processor and will NOT be handled in any
            // way.
        }

        return Task.CompletedTask;
    }

}
