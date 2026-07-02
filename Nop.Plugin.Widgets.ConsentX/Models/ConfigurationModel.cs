using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.ConsentX.Models;

/// <summary>
/// View model for the ConsentX admin configuration screen.
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.ConsentX.Fields.SiteKey")]
    public string SiteKey { get; set; } = string.Empty;
    public bool SiteKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.ConsentX.Fields.AppUrl")]
    public string AppUrl { get; set; } = "https://app.consentx.io";
    public bool AppUrl_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.ConsentX.Fields.EnableConsentMode")]
    public bool EnableConsentMode { get; set; } = true;
    public bool EnableConsentMode_OverrideForStore { get; set; }

    /// <summary>True when a non-empty Site Key is stored for the active scope.</summary>
    public bool IsConnected { get; set; }

    /// <summary>Bare host (no scheme/www/port) this store will authorize.</summary>
    public string SiteHost { get; set; } = string.Empty;

    /// <summary>The ConsentX dashboard URL for the "Open dashboard" link.</summary>
    public string DashboardUrl { get; set; } = "https://app.consentx.io";
}
