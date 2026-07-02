using Nop.Core.Configuration;

namespace Nop.Plugin.Widgets.ConsentX;

/// <summary>
/// Persisted configuration for the ConsentX widget plugin. Stored through
/// nopCommerce's <c>ISettingService</c> (settings table), per-store aware.
/// </summary>
public class ConsentXSettings : ISettings
{
    /// <summary>
    /// The ConsentX Site Key for this store. Populated automatically by the
    /// 1-click Connect handshake, or entered manually as a fallback. The embed
    /// loads from <c>{AppUrl}/api/{SiteKey}/embed.js</c>.
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// Scoped API token returned alongside the site key during Connect. Optional;
    /// reserved for future server-to-server calls (not required for the embed).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ConsentX application host (scheme + host, no trailing slash). Overridable
    /// so staging / self-hosted installs can point at a custom host. Defaults to
    /// https://app.consentx.io.
    /// </summary>
    public string AppUrl { get; set; } = "https://app.consentx.io";

    /// <summary>
    /// When enabled, prints the Google Consent Mode v2 denied-by-default stub in
    /// the head before any analytics tag. The ConsentX widget emits the matching
    /// <c>gtag('consent','update', …)</c> on the visitor's choice.
    /// </summary>
    public bool EnableConsentMode { get; set; } = true;

    /// <summary>
    /// Transient CSRF state for the in-flight Connect handshake. Generated at
    /// "Connect" start, verified (constant-time) on callback, then cleared.
    /// </summary>
    public string ConnectState { get; set; } = string.Empty;
}
