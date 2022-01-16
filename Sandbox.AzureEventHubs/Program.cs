using Sandbox.AzureEventHubs;

Console.WriteLine( "Start" );

var app = new AzureEventHubsApp();

await app.Start();

