/*
 * RadiantPi.Kaleidescape - Communication client for Kaleidescape
 * Copyright (C) 2020-2023 - Steve G. Bjorg
 *
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Affero General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with this program. If not, see <https://www.gnu.org/licenses/>.
 */

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