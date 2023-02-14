/*
 * RadiantPi.Kaleidescape - Communication client for Kaleidescape
 * Copyright (C) 2020-2022 - Steve G. Bjorg
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

// check if there is an environment variable for Kaleidescape player serial number or prompt for it
var deviceId = Environment.GetEnvironmentVariable("KPLAYER_SERIAL_NUMBER");
if(string.IsNullOrEmpty(deviceId)) {
    deviceId = AnsiConsole.Ask<string>("Enter Kaleidescape Player Serial Number:");
}

// initialize client
using IKaleidescape client = new KaleidescapeClient(new() {
    Host = "192.168.1.147",
    Port = 10000,
    DeviceId = deviceId
});

// hook-up event handlers
client.HighlightedSelectionChanged += async delegate (object? sender, HighlightedSelectionChangedEventArgs args) {
    var details = await client.GetContentDetailsAsync(args.SelectionId);
    Console.WriteLine($"=> Movie Details: {details.Title} ({details.Year}) [{args.SelectionId}]");
};
client.UiStateChanged += async delegate (object? sender, UiStateChangedEventArgs args) {
    Console.WriteLine($"=> UI State: screen={args.Screen}, dialog={args.Dialog}, popup={args.Popup}, saver={args.Saver}");
};
client.MovieLocationChanged += async delegate(object? sender, MovieLocationEventArgs args) {
    Console.WriteLine($"=> Movie Location: location={args.Location}");
};

// connect to device
await client.ConnectAsync();

// wait for exit
Console.WriteLine("Connected to Kaleidescape. Press ENTER to exit.");
Console.ReadLine();