namespace Nop.Plugin.Widgets.ConsentX;

/// <summary>
/// Static constants for the ConsentX plugin.
/// </summary>
public static class ConsentXDefaults
{
    /// <summary>Plugin system name (must match plugin.json SystemName).</summary>
    public const string SystemName = "Widgets.ConsentX";

    /// <summary>The Connect client id allowlisted server-side for nopCommerce.</summary>
    public const string ConnectClient = "nopcommerce";

    /// <summary>View component name used to render the embed in the head zone.</summary>
    public const string ViewComponentName = "WidgetsConsentX";

    /// <summary>Route name for the admin configuration screen.</summary>
    public const string ConfigureRouteName = "Plugin.Widgets.ConsentX.Configure";
}
