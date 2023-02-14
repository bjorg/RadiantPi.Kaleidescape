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

namespace RadiantPi.Kaleidescape;

public sealed class HighlightedSelectionChangedEventArgs : EventArgs {

    //--- Constructors ---
    public HighlightedSelectionChangedEventArgs(string id)
        => SelectionId = id ?? throw new ArgumentNullException(nameof(id));

    //--- Properties ---
    public string SelectionId { get; }
}

public sealed class UiStateChangedEventArgs : EventArgs {

    //--- Constructors ---
    public UiStateChangedEventArgs(string screen, string popup, string dialog, string saver) {
        Screen = screen ?? throw new ArgumentNullException(nameof(screen));
        Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        Popup = popup ?? throw new ArgumentNullException(nameof(popup));
        Saver = saver ?? throw new ArgumentNullException(nameof(saver));
    }

    //--- Properties ---
    public string Screen { get; }
    public string Dialog { get; }
    public string Popup { get; }
    public string Saver { get; }
}

public sealed class MovieLocationEventArgs : EventArgs {

    //--- Constructors ---
    public MovieLocationEventArgs(string location)
        => Location = location ?? throw new ArgumentNullException(nameof(location));

    //--- Properties ---
    public string Location { get; }
}