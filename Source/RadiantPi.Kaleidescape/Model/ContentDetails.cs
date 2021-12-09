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

namespace RadiantPi.Kaleidescape.Model {

    public sealed class ContentDetails {

        //--- Properties ---
        public string? Handle { get; set; }
        public string? Title { get; set; }
        public string? CoverUrl { get; set; }
        public string? HiResCoverUrl { get; set; }
        public string? Rating { get; set; }
        public string? RatingReason { get; set; }
        public string? Year { get; set; }
        public string? RunningTime { get; set; }
        public string? Actors { get; set; }
        public string? Director { get; set; }
        public string? Directors { get; set; }
        public string? Genre { get; set; }
        public string? Genres { get; set; }
        public string? Synopsis { get; set; }
        public string? ColorDescription { get; set; }
        public string? Country { get; set; }
        public string? AspectRatio { get; set; }
        public string? DiscLocation { get; set; }
    }
}