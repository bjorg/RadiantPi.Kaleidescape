/*
 * RadiantPi.Kaleidescape - Communication client for Kaleidescape
 * Copyright (C) 2020-2021 - Steve G. Bjorg
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

using System;
using System.Linq;
using RadiantPi.Kaleidescape;
using TMDbLib.Client;

// check arguments
if(args.Length != 2) {
    Console.WriteLine("ERROR: missing Device ID and/or MovieDB API Key as commandline argument");
    return;
}
var deviceId = args[0];
var movieDbApiKey = args[1];

// initialize clients
using KaleidescapeClient kaleidescapeClient = new(new() {
    Host = "192.168.1.147",
    Port = 10000,
    DeviceId = deviceId
});
using TMDbClient movieDbClient = new(movieDbApiKey);

// hook-up event handlers
kaleidescapeClient.HighlightedSelectionChanged += async delegate (object? sender, HighlightedSelectionChangedEventArgs args) {
    try {
        var details = await kaleidescapeClient.GetContentDetailsAsync(args.SelectionId);
        if(
            !string.IsNullOrEmpty(details.Title)
            && int.TryParse(details.Year, out var year)
        ) {
            var searchResults = await movieDbClient.SearchMovieAsync(details.Title, year: year);
            if(searchResults.Results.Any()) {
                var first = searchResults.Results.First();
                Console.WriteLine($"{details.Title} ({details.Year}) --> {first.Title} ({first.ReleaseDate?.Year.ToString() ?? "N/A"}): {first.VoteAverage:0.00} ({first.VoteCount:N0} votes)");
            } else {
                Console.WriteLine($"Could not find record for: {details.Title} ({details.Year})");
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
