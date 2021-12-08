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