using System;
using Microsoft.SPOT;
using imBMW.Tools;
using imBMW.Features.Localizations;

namespace imBMW.Multimedia.Models
{
    public static class ModelHelpers
    {
        public static string GetTrackPlaylistPosition(this TrackInfo n)
        {
            return n.TrackNumber + " / " + n.TotalTracks;
        }

        public static string GetTitleWithLabel(this TrackInfo n)
        {
            if (StringHelpers.IsNullOrEmpty(n.Title))
            {
                return "";
            }
            return Localization.Current.TrackTitle + ": " + n.Title;
        }

        public static string GetArtistWithLabel(this TrackInfo n)
        {
            if (StringHelpers.IsNullOrEmpty(n.Artist))
            {
                return "";
            }
            return Localization.Current.Artist + ": " + n.Artist;
        }

        public static string GetAlbumWithLabel(this TrackInfo n)
        {
            if (StringHelpers.IsNullOrEmpty(n.Album))
            {
                return "";
            }
            return Localization.Current.Album + ": " + n.Album;
        }

        public static string GetGenreWithLabel(this TrackInfo n)
        {
            if (StringHelpers.IsNullOrEmpty(n.Genre))
            {
                return "";
            }
            return Localization.Current.Genre + ": " + n.Genre;
        }
    }
}
