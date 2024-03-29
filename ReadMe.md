# RadiantPi.Kaleidescape

`KaleidescapeClient` enables control of a Kaleidescape player over Telnet. The library is platform agnostic and works on Windows or Linux, including on a Raspberry Pi.

Run the `dotnet` command from your project folder to add the `RadiantPi.Kaleidescape` assembly:
```
dotnet add package RadiantPi.Kaleidescape
```

Find a description of the latest changes in the [release notes](ReleaseNotes.md).

## Sample: Show Selection

The following code listens for highlighted selection events of a Kaleidescape player. When the selection changes, the Kaleidescape system is queried for details about the selection, such as title and year released, which are then shown on the console.

```csharp
using System;
using RadiantPi.Kaleidescape;
using Spectre.Console;

// check if there is an environment variable for Kaleidescape player serial number or prompt for it
var deviceId = Environment.GetEnvironmentVariable("KPLAYER_SERIAL_NUMBER");
if(string.IsNullOrEmpty(deviceId)) {
    deviceId = AnsiConsole.Ask<string>("Enter Kaleidescape Player Serial Number:");
}

// initialize client
using KaleidescapeClient client = new(new() {
    Host = "192.168.1.147",
    Port = 10000,
    DeviceId = deviceId
});

// hook-up event handlers
client.HighlightedSelectionChanged += async delegate (object? sender, HighlightedSelectionChangedEventArgs args) {
    var details = await client.GetContentDetailsAsync(args.SelectionId);
    Console.WriteLine($"=> {details.Title} ({details.Year}) [{args.SelectionId}]");
};

// connect to device
await client.ConnectAsync();

// wait for exit
Console.WriteLine("Connected to Kaleidescape. Press ENTER to exit.");
Console.ReadLine();
```

## Output

```
Connected to Kaleidescape. Press ENTER to exit.
=> The Mummy: Tomb of the Dragon Emperor (2008) [26-0.0-S_c44e532c]
=> Murder on the Orient Express (2017) [26-0.0-S_c44e8fd1]
=> Mutiny on the Bounty (1962) [26-0.0-S_ff304478]
=> My Fair Lady (1964) [26-0.0-S_c4458748]
=> My Name Is Nobody (1974) [26-0.0-S_c4514898]
=> National Lampoon's Van Wilder (2002) [26-0.0-S_c44c2ee5]
=> National Treasure (2004) [26-0.0-S_c445c6f9]
=> The Natural (1984) [26-0.0-S_c44a365d]
=> Need for Speed (2014) [26-0.0-S_c459aabd]
=> Neil Cowley Trio: Live at Montreux (2012) [26-0.0-S_c44f80d6]
=> Nerve (2016) [26-0.0-S_c4525aea]
```

## License

This application is distributed under the GNU Affero General Public License v3.0 or later.

Copyright (C) 2020-2023 - Steve G. Bjorg