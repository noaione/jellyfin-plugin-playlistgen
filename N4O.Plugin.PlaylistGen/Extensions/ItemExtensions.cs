#pragma warning disable SA1121
#pragma warning disable CA1002

using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;

namespace N4O.Plugin.PlaylistGen.Extensions;

/// <summary>
/// Extensions for <see cref="BaseItem"/>.
/// </summary>
public static class ItemExtensions
{
    /// <summary>
    /// Get the proper name of an episode or season.
    /// </summary>
    /// <param name="item">Current item.</param>
    /// <returns>Episode name.</returns>
    public static string? GetProperName(this BaseItem item)
    {
        if (item.GetBaseItemKind().Equals(BaseItemKind.Episode))
        {
            if (String.IsNullOrWhiteSpace(item.Name) || String.IsNullOrEmpty(item.Name))
            {
                return String.Format(new CultureInfo("en-US"), "Episode {0}", item.IndexNumber);
            }

            return item.Name;
        }
        else if (item.GetBaseItemKind().Equals(BaseItemKind.Season))
        {
            if (String.IsNullOrWhiteSpace(item.Name) || String.IsNullOrEmpty(item.Name))
            {
                return String.Format(new CultureInfo("en-US"), "Season {0}", item.IndexNumber);
            }

            return item.Name;
        }

        return null;
    }

    /// <summary>
    /// Create a download URL for an episode.
    /// </summary>
    /// <param name="item">Current item.</param>
    /// <param name="basePath">Base path for the server.</param>
    /// <param name="token">Download token.</param>
    /// <returns>Download URL.</returns>
    public static string? GetDownloadURL(this BaseItem item, string? basePath, string token)
    {
        if (item.GetBaseItemKind() != BaseItemKind.Episode)
        {
            return null;
        }

        var downloadUrl = String.Format(new CultureInfo("en-US"), "/Items/{0}/Download?api_key={1}", item.Id, token);

        if (!String.IsNullOrWhiteSpace(basePath))
        {
            basePath = basePath.TrimEnd('/');
            downloadUrl = String.Format(new CultureInfo("en-US"), "{0}{1}", basePath, downloadUrl);
        }

        return downloadUrl;
    }

    /// <summary>
    /// Create a collection/seasonal type of URL that links to other playlist generator data.
    /// </summary>
    /// <param name="item">Current item.</param>
    /// <param name="basePath">Base path for the server.</param>
    /// <param name="token">Download token.</param>
    /// <returns>Download URL.</returns>
    public static string? GetChildM3U8Url(this BaseItem item, string? basePath, string token)
    {
        var m3u8Url = String.Format(new CultureInfo("en-US"), "/PlaylistGen/Items/{0}.m3u8?api_key={1}", item.Id, token);

        if (!String.IsNullOrWhiteSpace(basePath))
        {
            basePath = basePath.TrimEnd('/');
            m3u8Url = String.Format(new CultureInfo("en-US"), "{0}{1}", basePath, m3u8Url);
        }

        return m3u8Url;
    }

    /// <summary>
    /// Get downloadable items from a list of items.
    /// </summary>
    /// <param name="items">Current items.</param>
    /// <param name="user">Current user.</param>
    /// <returns>Download URL.</returns>
    public static List<BaseItem> GetDownloadableItems(this List<BaseItem> items, Jellyfin.Data.Entities.User user)
        => items.FindAll(item => item.CanDownload(user));

    /// <summary>
    /// Check if an item is a type of Xs.
    /// </summary>
    /// <param name="item">Current item.</param>
    /// <param name="types">The type to check.</param>
    /// <returns>Is it true or not.</returns>
    public static bool IsItemTypes(this BaseItem item, List<BaseItemKind> types)
    {
        var itemType = item.GetBaseItemKind();
        foreach (var type in types)
        {
            if (itemType == type)
            {
                return true;
            }
        }

        return false;
    }
}
