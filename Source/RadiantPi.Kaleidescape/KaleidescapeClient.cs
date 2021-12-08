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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Telnet;

namespace RadiantPi.Kaleidescape {

    public class KaleidescapeClientConfig {

        //--- Properties ---
        public string? Host { get; set; }
        public ushort? Port { get; set; }
        public string? DeviceId { get; set; }
    }

    public sealed class KaleidescapeClient : IKaleidescape {

        //--- Constants ---
        private static Regex _highlightedSelectionRegex = new Regex(@"^#.+/!/000:HIGHLIGHTED_SELECTION:(?<selectionId>[^:]+):", RegexOptions.Compiled);

        //--- Events ---
        public event EventHandler<HighlightedSelectionChangedEventArgs>? HighlightedSelectionChanged;

        //--- Fields ---
        private readonly ITelnet _telnet;
        private readonly string _deviceId;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        //--- Constructors ---
        public KaleidescapeClient(KaleidescapeClientConfig config, ILoggerFactory? loggerFactory = null)
            : this(
                new TelnetClient(
                    config.Host ?? throw new ArgumentNullException("config.Host"),
                    config.Port ?? 44100,
                    loggerFactory?.CreateLogger<TelnetClient>()
                ),
                config.DeviceId ??  throw new ArgumentNullException("config.DeviceId"),
                loggerFactory?.CreateLogger<KaleidescapeClient>()
            ) { }

        public KaleidescapeClient(ITelnet telnet, string deviceId, ILogger<KaleidescapeClient>? logger) {
            Logger = logger;
            _telnet = telnet ?? throw new ArgumentNullException(nameof(telnet));
            _telnet.ValidateConnectionAsync = ValidateConnectionAsync;
            _telnet.MessageReceived += MessageReceived;
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        //---  Properties ---
        private ILogger? Logger { get; }

        //--- Methods ---
        public Task ConnectAsync() => _telnet.ConnectAsync();

        public void Dispose() {
            _mutex.Dispose();
            _telnet.Dispose();
        }

        private async Task ValidateConnectionAsync(ITelnet client, TextReader reader, TextWriter writer) {
            Logger?.LogDebug("Kaleidescape connection established");

            // enable events for connection
            await writer.WriteLineAsync($"01/1/ENABLE_EVENTS:#{_deviceId}:").ConfigureAwait(false);
        }

        private void MessageReceived(object? sender, TelnetMessageReceivedEventArgs args) {
            Logger?.LogDebug($"Received: {args.Message}");

            // check if message is a highlighted selection event
            var highlightedSelectionMatch = _highlightedSelectionRegex.Match(args.Message);
            if(highlightedSelectionMatch.Success) {
                var selectionId = highlightedSelectionMatch.Groups["selectionId"].Value;
                HighlightedSelectionChanged?.Invoke(this, new(selectionId));
                return;
            }
        }
    }
}