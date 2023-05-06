#pragma warning disable SA1122
#pragma warning disable SA1611
#pragma warning disable SA1121

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using N4O.Plugin.PlaylistGen.Configuration;
using N4O.Plugin.PlaylistGen.Extensions;

namespace N4O.Plugin.PlaylistGen.Api;

/// <summary>
/// The playlist generator controller.
/// </summary>
[ApiController]
[Route("PlaylistGen")]
public class GeneratorController : ControllerBase
{
    private readonly Assembly _assembly;
    private readonly string _playlistGenScriptPath;

    private readonly ILogger<GeneratorController> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IApplicationPaths _appPaths;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly IUserManager _userManager;

    private readonly PluginConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorController"/> class.
    /// </summary>
    public GeneratorController(
        ILibraryManager libraryManager,
        IFileSystem fileSystem,
        ILogger<GeneratorController> logger,
        ILoggerFactory loggerFactory,
        IApplicationPaths appPaths,
        ILibraryMonitor libraryMonitor,
        IUserManager userManager)
    {
        _assembly = Assembly.GetExecutingAssembly();
        _playlistGenScriptPath = GetType().Namespace + ".playlistgen.js";

        _libraryManager = libraryManager;
        _logger = logger;
        _fileSystem = fileSystem;
        _loggerFactory = loggerFactory;
        _appPaths = appPaths;
        _libraryMonitor = libraryMonitor;
        _userManager = userManager;

        _config = PlaylistGenPlugin.Instance!.Configuration;
    }

    /// <summary>
    /// Gets the client script.
    /// </summary>
    /// <response code="200">Javascript file successfully returned.</response>
    /// <response code="404">File not found.</response>
    /// <returns>The "playlistgen.js" embedded file.</returns>
    [HttpGet("ClientScript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/javascript")]
    public ActionResult GetClientScript()
    {
        var scriptStream = _assembly.GetManifestResourceStream(_playlistGenScriptPath);

        if (scriptStream != null)
        {
            return File(scriptStream, "application/javascript");
        }

        return NotFound();
    }

    /// <summary>
    /// Gets the generated playlist.
    /// </summary>
    /// <response code="200">A generated playlist from the provided item.</response>
    /// <response code="404">Provided item ID cannot be found.</response>
    /// <response code="415">The item provided is not supported for generation.</response>
    /// <returns>The generated playlist from the provided item IDs.</returns>
    [HttpGet("Items/{itemId}")]
    [HttpGet("Items/{itemId}.m3u")]
    [HttpGet("Items/{itemId}.m3u8")]
    [Authorize(Policy = "Download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [Produces("application/x-mpegURL")]
    public ActionResult GetM3U8Stream([FromRoute, Required] System.Guid itemId)
    {
        var item = _libraryManager.GetItemById(itemId);

        _logger.LogInformation("Generating playlist for item {0} ({1})", item?.Name, item?.Id);
        if (item == null)
        {
            return NotFound();
        }

        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.Equals(default)
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;
        var downloadToken = User.GetToken();

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = false,
            ParentId = item.Id,
        });
        _logger.LogInformation("Recursively getting items for {0} ({1}) [{2}]", item?.Name, item?.Id, item?.GetBaseItemKind());

        if (items == null)
        {
            return NotFound();
        }

        var m3u8Contents = "";
        var itemType = item!.GetBaseItemKind();
        if (item!.IsItemTypes(new List<BaseItemKind> { BaseItemKind.BoxSet, BaseItemKind.CollectionFolder }))
        {
            _logger.LogInformation("Generating collection type playlist for {0} ({1})", item?.Name, item?.Id);
            m3u8Contents = GetCollectionTypeM3U8(items, user!, downloadToken);
        }
        else if (item!.IsItemTypes(new List<BaseItemKind> { BaseItemKind.MusicAlbum, BaseItemKind.Playlist, BaseItemKind.Season }))
        {
            _logger.LogInformation("Generating child-like items for {0} ({1})", item?.Name, item?.Id);
            m3u8Contents = GetChildItemsM3U8(items, user!, downloadToken);
        }
        else if (item!.GetBaseItemKind() == BaseItemKind.Series)
        {
            _logger.LogInformation("Generating series items for {0} ({1})", item?.Name, item?.Id);
            m3u8Contents = GetNestedInfoM3U8(items, user!, downloadToken, "Unknown Series", "Season");
        }
        else if (item!.GetBaseItemKind() == BaseItemKind.MusicArtist)
        {
            _logger.LogInformation("Generating music artist items for {0} ({1})", item?.Name, item?.Id);
            m3u8Contents = GetNestedInfoM3U8(items, user!, downloadToken, "Unknown Artist", "Album");
        }

        if (String.IsNullOrEmpty(m3u8Contents.Trim()))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        byte[] m3u8Bytes = Encoding.UTF8.GetBytes(m3u8Contents);
        return File(m3u8Bytes, "application/x-mpegURL", item?.Id + ".m3u8");
    }

    private static string GetM3U8Header()
    {
        return "#EXTM3U\n";
    }

    private string GetCollectionTypeM3U8(List<BaseItem> items, Jellyfin.Data.Entities.User user, string? downloadToken)
    {
        var m3u8Contents = GetM3U8Header();

        var itemsType = new List<BaseItemKind> { BaseItemKind.Audio, BaseItemKind.AudioBook, BaseItemKind.Movie, BaseItemKind.Video, BaseItemKind.Episode };
        var nestedItemsType = new List<BaseItemKind> { BaseItemKind.MusicAlbum, BaseItemKind.MusicArtist, BaseItemKind.Playlist, BaseItemKind.Season, BaseItemKind.Series };

        foreach (var item in items)
        {
            var itemId = item.Id;
            var itemName = item.Name;
            var parentName = item.GetParent().Name;

            if (item.IsItemTypes(itemsType))
            {
                if (!item.CanDownload(user))
                {
                    continue;
                }

                var url = item.GetDownloadURL(_config.BasePath, downloadToken ?? "");

                m3u8Contents += MakeM3U8InfoHeader(parentName!, itemName!);
                m3u8Contents += url + "\n";
            }
            else if (item.IsItemTypes(nestedItemsType))
            {
                var url = item.GetChildM3U8Url(_config.BasePath, downloadToken ?? "");

                m3u8Contents += MakeM3U8InfoHeader(parentName!, itemName!);
                m3u8Contents += url + "\n";
            }
        }

        return m3u8Contents.TrimEnd();
    }

    private string GetChildItemsM3U8(List<BaseItem> items, Jellyfin.Data.Entities.User user, string? downloadToken)
    {
        var downloadableItems = items.GetDownloadableItems(user);

        if (downloadableItems.Count == 0)
        {
            return "";
        }

        var m3u8Contents = GetM3U8Header();

        foreach (var item in downloadableItems)
        {
            var episodeId = item.Id;
            var episodeTitle = item.GetProperName();
            var seasonName = item.GetParent().GetProperName();

            var episodeDownloadUrl = item.GetDownloadURL(_config.BasePath, downloadToken ?? "");

            if (!String.IsNullOrEmpty(episodeDownloadUrl))
            {
                m3u8Contents += MakeM3U8InfoHeader(seasonName!, episodeTitle!);
                m3u8Contents += episodeDownloadUrl + "\n";
            }
        }

        return m3u8Contents.TrimEnd();
    }

    private string GetNestedInfoM3U8(
        List<BaseItem> items,
        Jellyfin.Data.Entities.User user,
        string? downloadToken,
        string groupTitleFallback,
        string groupNameFallback)
    {
        var m3u8Contents = GetM3U8Header();

        foreach (var item in items)
        {
            var seasonId = item.Id;
            var seasonName = item.GetProperName() ?? String.Format(new CultureInfo("en-US"), "{0} {1}", groupNameFallback, item.IndexNumber);
            var seriesName = item.GetParent().Name ?? groupTitleFallback;

            m3u8Contents += MakeM3U8InfoHeader(seriesName, seasonName);
            m3u8Contents += item.GetChildM3U8Url(_config.BasePath, downloadToken ?? "") + "\n";
        }

        return m3u8Contents.TrimEnd();
    }

    private static string MakeM3U8InfoHeader(string groupTitle, string groupName)
    {
        return String.Format(new CultureInfo("en-US"), "#EXTINF:-1 group-title=\"{0}\", {1}\n", groupTitle, groupName);
    }
}
