using MediaBrowser.Model.Plugins;

namespace N4O.Plugin.PlaylistGen.Configuration;

/// <summary>
/// The configuration options.
/// </summary>
public enum SomeOptions
{
    /// <summary>
    /// Option one.
    /// </summary>
    OneOption,

    /// <summary>
    /// Second option.
    /// </summary>
    AnotherOption
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        BasePath = "string";
    }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string BasePath { get; set; }
}
