using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.Multimedia.Models
{
    public class TrackInfo
    {
        public TrackInfo()
        {
            TrackNumber = 1;
            TotalTracks = 1;
        }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string Genre { get; set; }

        /// <summary>
        /// Position of current track in playlist.
        /// </summary>
        public int TrackNumber { get; set; }

        /// <summary>
        /// Number of tracks in playlist.
        /// </summary>
        public int TotalTracks { get; set; }

        /// <summary>
        /// Length in milliseconds.
        /// </summary>
        public int TrackLength { get; set; }
    }
}
