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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Kaleidescape.Model;
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
        private static Regex _highlightedSelectionRegex = new(@"^#.+/!/000:HIGHLIGHTED_SELECTION:(?<selectionId>[^:]+):", RegexOptions.Compiled);
        private static Regex _responseRegex = new("01/(?<sequenceId>[0-9])/000:(?<message>[^:]+):(?<data>.+):/");

        //--- Class Fields ---
        private static readonly JsonSerializerOptions g_jsonSerializerOptions = new() {
            WriteIndented = true,
            Converters = {
                new JsonStringEnumConverter()
            }
        };

        //--- Class Methods ---
        private static string[] DecodeData(string data) {
            List<string> result = new();
            StringBuilder buffer = new();
            for(var i = 0; i < data.Length; ++i) {
                var c = data[i];
                switch(c) {
                case ':':

                    // found data delimiter; add buffer to result and start over with fresh buffer
                    result.Add(buffer.ToString());
                    buffer.Clear();
                    break;
                case '\\':

                    // found escape character; move forward and ensure the end of the data value has not been reached
                    if(++i == data.Length) {
                        throw new ArgumentException("Invalid data value: escape character (\\) was found at the end of the data value");
                    }
                    c = data[i];
                    switch(c) {
                    case 'n':
                    case 'r':
                    case 't':
                    case '/':
                    case '\\':
                    case ':':

                        // append escaped character
                        buffer.Append(c);
                        break;
                    case 'd':

                        // format \dnnn, where nnn is the zero-padded three-digit decimal value for the Latin-1 character
                        ++i;
                        if(

                            // not enough characters left
                            ((data.Length - i) < 3)

                            // 3 trailing characters are not a valid integer
                            || !int.TryParse(data.Substring(i, 3), out var characterCode)

                            // character is out of range
                            || (characterCode < 32)
                            || (characterCode > 255)
                        ) {
                            throw new ArgumentException("Invalid data value: escaped Latin-1 character (\\d) must be followed by 3 digit code ranging from 32 to 255");
                        }
                        i += 2;

                        // append character code using Latin-1 codepage
                        buffer.Append(Encoding.GetEncoding(1252).GetChars(new byte[] { (byte)characterCode }));
                        break;
                    default:
                        throw new ArgumentException("Invalid data value: escape character (\\) is followed by an illegal character");
                    }
                    break;
                default:

                    // append character
                    buffer.Append(c);
                    break;
                }
            }

            // add trailling buffer to result
            result.Add(buffer.ToString());
            return result.ToArray();
        }

        //--- Events ---
        public event EventHandler<HighlightedSelectionChangedEventArgs>? HighlightedSelectionChanged;

        //--- Fields ---
        private readonly ITelnet _telnet;
        private readonly string _deviceId;
        private readonly ConcurrentDictionary<string, ContentDetails> _contentDetailsCache = new();
        private int _sequenceId;
        private bool _disposed;

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
            if(_disposed) {
                return;
            }
            _disposed = true;
            _telnet.MessageReceived -= MessageReceived;
            _telnet.Dispose();
        }

        public Task<ContentDetails> GetContentDetailsAsync(string handle, CancellationToken cancellationToken = default) {
            CheckNotDisposed();
            return LogRequestResponse(handle, async () => {

                // TODO (2021-12-08, bjorg): use `cancellationToken` to cancel operation

                // check if cache contains a response already
                if(_contentDetailsCache.TryGetValue(handle, out var result)) {
                    return result;
                }

                // get a new sequence id for request
                var sequenceId = Interlocked.Increment(ref _sequenceId) % 10;

                // send query and collect responses
                TaskCompletionSource responseSource = new();
                result = new();
                _telnet.MessageReceived += ReadResponse;
                try {
                    await _telnet.SendAsync($"01/{sequenceId}/GET_CONTENT_DETAILS:{handle}::\r").ConfigureAwait(false);
                    await responseSource.Task.ConfigureAwait(false);
                } finally {
                    _telnet.MessageReceived -= ReadResponse;
                }
                _contentDetailsCache[handle] = result;
                return result;

                // local functions
                void ReadResponse(object? sender, TelnetMessageReceivedEventArgs args) {

                    // NOTE (2021-12-08, bjorg): Kaleidescape sends multiple responses for a request that need to be assembled together.
                    try {
                        var match = _responseRegex.Match(args.Message);
                        if(
                            match.Success
                            && int.TryParse(match.Groups["sequenceId"].Value, out var responseSequenceId)
                            && (responseSequenceId == sequenceId)
                        ) {
                            var message = match.Groups["message"].Value;
                            var data = match.Groups["data"].Value;
                            if(message == "CONTENT_DETAILS_OVERVIEW") {

                                // ignore; nothing further to do
                            } else if(message == "CONTENT_DETAILS") {
                                var fields = DecodeData(data);
                                if(fields.Length == 3) {
                                    switch(fields[1]) {
                                    case "Content_handle":
                                        result.Handle = fields[2];
                                        break;
                                    case "Title":
                                        result.Title = fields[2];
                                        break;
                                    case "Cover_URL":
                                        result.CoverUrl = fields[2];
                                        break;
                                    case "HiRes_cover_URL":
                                        result.HiResCoverUrl = fields[2];
                                        break;
                                    case "Rating":
                                        result.Rating = fields[2];
                                        break;
                                    case "Rating_reason":
                                        result.RatingReason = fields[2];
                                        break;
                                    case "Year":
                                        result.Year = fields[2];
                                        break;
                                    case "Running_time":
                                        result.RunningTime = fields[2];
                                        break;
                                    case "Actors":
                                        result.Actors = fields[2];
                                        break;
                                    case "Director":
                                        result.Director = fields[2];
                                        break;
                                    case "Directors":
                                        result.Directors = fields[2];
                                        break;
                                    case "Genre":
                                        result.Genre = fields[2];
                                        break;
                                    case "Genres":
                                        result.Genres = fields[2];
                                        break;
                                    case "Synopsis":
                                        result.Synopsis = fields[2];
                                        break;
                                    case "Color_description":
                                        result.ColorDescription = fields[2];
                                        break;
                                    case "Country":
                                        result.Country = fields[2];
                                        break;
                                    case "Aspect_ratio":
                                        result.AspectRatio = fields[2];
                                        break;
                                    case "Disc_location":
                                        result.DiscLocation = fields[2];

                                        // this is the last field received; indicate response is done
                                        responseSource.SetResult();
                                        break;
                                    default:
                                        throw new KaleidescapeResponseException($"Unexpected field for CONTENT_DETAILS: '{fields[1]}' = '{fields[2]}'");
                                    }
                                } else {
                                    throw new KaleidescapeResponseException($"Unexpected format for CONTENT_DETAILS: '{data}' ({fields.Length:N0} fields)");
                                }
                            } else {
                                throw new KaleidescapeResponseException($"Unrecognized message: '{message}' (data: '{data}')");
                            }
                        }
                    } catch(Exception e) {
                        responseSource.SetException(e);
                    }
                }
            });
        }

        private async Task ValidateConnectionAsync(ITelnet client, TextReader reader, TextWriter writer) {
            Logger?.LogInformation("Kaleidescape connection established");

            // enable events for connection
            await writer.WriteLineAsync($"01/1/ENABLE_EVENTS:#{_deviceId}:").ConfigureAwait(false);
        }

        private void MessageReceived(object? sender, TelnetMessageReceivedEventArgs args) {

            // check if message is a highlighted selection event
            var highlightedSelectionMatch = _highlightedSelectionRegex.Match(args.Message);
            if(highlightedSelectionMatch.Success) {
                var selectionId = highlightedSelectionMatch.Groups["selectionId"].Value;
                HighlightedSelectionChanged?.Invoke(this, new(selectionId));
                return;
            }
        }

        private void CheckNotDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException("client was disposed");
            }
        }

        private void LogDebugJson(string message, object? response) {
            if(Logger?.IsEnabled(LogLevel.Debug) ?? false) {
                var serializedResponse = JsonSerializer.Serialize(response, g_jsonSerializerOptions);
                Logger?.LogDebug($"{message}: {serializedResponse}");
            }
        }

        private async Task<TResult> LogRequestResponse<TResult, TParameter>(TParameter parameter, Func<Task<TResult>> callback, [CallerMemberName] string methodName = "") {
            Logger?.LogDebug($"{methodName} request: {parameter}");
            var response = await callback().ConfigureAwait(false);
            LogDebugJson($"{methodName} response", response);
            return response;
        }
    }
}