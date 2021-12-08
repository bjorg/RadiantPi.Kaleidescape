using System;
using RadiantPi.Kaleidescape;

// check arguments
if(args.Length != 1) {
    Console.WriteLine("ERROR: missing Device ID as commandline argument");
    return;
}

// initialize client
using var client = new KaleidescapeClient(new() {
    Host = "192.168.1.147",
    Port = 10000,
    DeviceId = args[0]
});

// hook-up event handlers
client.HighlightedSelectionChanged += delegate (object? sender, HighlightedSelectionChangedEventArgs args) {
    Console.WriteLine($"Selection: '{args.SelectionId}'");
};

// connect to device
await client.ConnectAsync();

// wait for exit
Console.WriteLine("Connected to Kaleidescape. Press ENTER to exit.");
Console.ReadLine();