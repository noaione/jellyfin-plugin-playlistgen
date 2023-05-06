# Playlist Generator

An automatic m3u8 file generator that can be played anywhere for your items.

Currently the following are item type are supported:
- `BoxSet` (Library/Nested Folder)
- `CollectionFolder` (Collection)
- `MusicAlbum` (Music Album)
- `MusicArtist` (Music Artist)
- `Playlist`
- `Season`
- `Series`

You can report any issues here: https://github.com/noaione/jellyfin-plugin-playlistgen/issues

## Requirements
- .NET 6.0

## Building
Either use dotnet: `dotnet build --configuration Release`<br />
Or use jprm: `jprm plugin build`

## Installing
### Manifest/Repo
SOON

### Manually
1. Open your Jellyfin `data` folder
2. Open the `plugins` folder
3. Make a new folder called `PlaylistGenerator`
4. Put the generated `.dll` there
5. Restart your server.

### Problems
1. WebUI injection is a bit hacky and need to be fixed
2. Unable to save config on latest version because of wrong type getting sanitized
3. Would sometimes fails to make M3U8 contents.
