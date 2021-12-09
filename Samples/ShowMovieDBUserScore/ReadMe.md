# RadiantPi.Kaleidescape - ShowMovieDBUserScore

Use highlighted selection to find move on [TheMovieDB](https://www.themoviedb.org/) and display its user votes score.

## Code

```csharp
using System;
using System.Linq;
using RadiantPi.Kaleidescape;
using Spectre.Console;
using TMDbLib.Client;

// check if there is an environment variable for Kaleidescape player serial number or prompt for it
var deviceId = Environment.GetEnvironmentVariable("KPLAYER_SERIAL_NUMBER");
if(string.IsNullOrEmpty(deviceId)) {
    deviceId = AnsiConsole.Ask<string>("Enter Kaleidescape Player Serial Number:");
}

// check if there is an environment variable for TheMovieDB API key or prompt for it
var movieDbApiKey = Environment.GetEnvironmentVariable("TMDB_APIKEY");
if(string.IsNullOrEmpty(movieDbApiKey)) {
    movieDbApiKey = AnsiConsole.Ask<string>("Enter TheMovieDB API key (or leave blank):");
}

// initialize clients
using KaleidescapeClient kaleidescapeClient = new(new() {
    Host = "192.168.1.147",
    Port = 10000,
    DeviceId = deviceId
});
TMDbClient movieDbClient = new(movieDbApiKey);

// hook-up event handlers
kaleidescapeClient.HighlightedSelectionChanged += async delegate (object? sender, HighlightedSelectionChangedEventArgs args) {
    try {
        var details = await kaleidescapeClient.GetContentDetailsAsync(args.SelectionId);
        if(
            !string.IsNullOrEmpty(details.Title)
            && int.TryParse(details.Year, out var year)
        ) {

            // find movie on TheMovieD
            var searchResults = await movieDbClient.SearchMovieAsync(details.Title, year: year);
            if(searchResults.Results.Any()) {
                var first = searchResults.Results.First();

                // show the Kaleidescape title and the matched title, just in case they don't line up
                Console.WriteLine($"=> {details.Title} ({details.Year}) --> {first.Title} ({first.ReleaseDate?.Year.ToString() ?? "N/A"}): {first.VoteAverage:0.00} ({first.VoteCount:N0} votes)");
            } else {
                Console.WriteLine($"Could not find record for: {details.Title} ({details.Year}) [{args.SelectionId}]");
            }
        } else {
            Console.WriteLine($"Insufficient information to display user votes");
        }
    } catch(Exception e) {
        Console.WriteLine($"ERROR: {e}");
    }
};

// connect to device
await kaleidescapeClient.ConnectAsync();
Console.WriteLine("Connected to Kaleidescape. Press ENTER to exit.");

// wait for exit
Console.ReadLine();
```

## Output

```
Connected to Kaleidescape. Press ENTER to exit.
=> The Night Before (2015) --> The Night Before (2015): 6.10 (1,330 votes)
=> Nightcrawler (2014) --> Nightcrawler (2014): 7.70 (8,519 votes)
=> Nobody (2021) --> Nobody (2021): 8.30 (3,738 votes)
=> Now You See Me 2 (2016) --> Now You See Me 2 (2016): 6.80 (9,116 votes)
=> Office Christmas Party (2016) --> Office Christmas Party (2016): 5.60 (1,658 votes)
=> The Old Man & the Gun (2018) --> The Old Man & the Gun (2018): 6.40 (1,099 votes)
=> Once Upon a Time... in Hollywood (2019) --> Once Upon a Time. in Hollywood (2019): 7.40 (9,671 votes)
=> The Osiris Child (2016) --> The Osiris Child (2016): 5.40 (362 votes)
=> Out of the Past (1947) --> Out of the Past (1947): 7.70 (375 votes)
```
