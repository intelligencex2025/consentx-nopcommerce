using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.ConsentX;

/// <summary>
/// ConsentX cookie-consent widget plugin for nopCommerce 4.6 / 4.7.
///
/// Injects the self-contained ConsentX embed (<c>{AppUrl}/api/{SiteKey}/embed.js</c>)
/// into the HTML head on every public page. The Site Key is obtained through the
/// 1-click Connect handshake (Model A) which redirects the admin to ConsentX,
/// auto-registers this store's domain in the allowlist, and returns a scoped key.
/// </summary>
public class ConsentXPlugin : BasePlugin, IWidgetPlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    public ConsentXPlugin(
        ILocalizationService localizationService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    /// <summary>
    /// We render in the head so the banner + Consent Mode signals are ready before
    /// the body and any analytics tags fire.
    /// </summary>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            PublicWidgetZones.HeadHtmlTag
        });
    }

    /// <summary>
    /// The view component that emits the embed script for a given widget zone.
    /// </summary>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(Components.WidgetsConsentXViewComponent);
    }

    /// <summary>
    /// Admin configuration page URL (Connect button + Site Key + options).
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/ConsentX/Configure";
    }

    /// <summary>
    /// This widget never needs to hide the checkout/conditional UI, so it always
    /// participates. nopCommerce uses this flag to skip widget execution in some
    /// embedded contexts; the consent banner must always be available.
    /// </summary>
    public bool HideInWidgetList => false;

    /// <summary>
    /// Install: seed default settings and locale resources.
    /// </summary>
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new ConsentXSettings
        {
            AppUrl = "https://app.consentx.io",
            EnableConsentMode = true,
            SiteKey = string.Empty,
            Token = string.Empty,
            ConnectState = string.Empty
        });

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Widgets.ConsentX.Fields.SiteKey"] = "Site Key",
            ["Plugins.Widgets.ConsentX.Fields.SiteKey.Hint"] = "Your ConsentX Site Key. Use the Connect button to populate this automatically, or paste it from the ConsentX dashboard (Websites → your site).",
            ["Plugins.Widgets.ConsentX.Fields.AppUrl"] = "ConsentX app host",
            ["Plugins.Widgets.ConsentX.Fields.AppUrl.Hint"] = "The ConsentX application host (scheme + host, no trailing slash). Leave as https://app.consentx.io unless you are on a staging or self-hosted host.",
            ["Plugins.Widgets.ConsentX.Fields.EnableConsentMode"] = "Google Consent Mode v2 defaults",
            ["Plugins.Widgets.ConsentX.Fields.EnableConsentMode.Hint"] = "Print the denied-by-default Google Consent Mode v2 stub before any analytics tag. The ConsentX widget emits the consent 'update' signals on the visitor's choice.",
            ["Plugins.Widgets.ConsentX.Connect"] = "Connect to ConsentX",
            ["Plugins.Widgets.ConsentX.Disconnect"] = "Disconnect",
            ["Plugins.Widgets.ConsentX.Status.Connected"] = "Connected. The ConsentX banner is live on your store.",
            ["Plugins.Widgets.ConsentX.Status.NotConnected"] = "Not connected. Click \"Connect to ConsentX\" to install the cookie banner in one click.",
            ["Plugins.Widgets.ConsentX.Connected.Success"] = "Successfully connected to ConsentX. The cookie banner is now live.",
            ["Plugins.Widgets.ConsentX.Connected.Disconnected"] = "Disconnected from ConsentX. The cookie banner has been removed.",
            ["Plugins.Widgets.ConsentX.Connect.Error.State"] = "Connect failed: the security state did not match. Please try again.",
            ["Plugins.Widgets.ConsentX.Connect.Error.NoKey"] = "Connect failed: ConsentX did not return a site key. Please try again.",
            ["Plugins.Widgets.ConsentX.Dashboard"] = "Open ConsentX dashboard"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall: remove settings and locale resources.
    /// </summary>
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<ConsentXSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Widgets.ConsentX");

        await base.UninstallAsync();
    }
}
