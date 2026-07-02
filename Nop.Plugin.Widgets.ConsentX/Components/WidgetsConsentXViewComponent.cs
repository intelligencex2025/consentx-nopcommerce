using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.ConsentX.Components;

/// <summary>
/// Renders the ConsentX embed into the public <c>head_html_tag</c> widget zone:
///
///   (optional) Google Consent Mode v2 denied-by-default stub
///   &lt;script type="module" src="{AppUrl}/api/{SiteKey}/embed.js"&gt;&lt;/script&gt;
///
/// embed.js is self-contained: it injects the scoped widget CSS + JS, mounts the
/// banner into a #consentx-cookie-consent div, reads geo/config from the server,
/// and emits the cx:consent event.
/// </summary>
public class WidgetsConsentXViewComponent : NopViewComponent
{
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    public WidgetsConsentXViewComponent(
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _settingService = settingService;
        _storeContext = storeContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeId);

        // Nothing to render until the store is connected.
        if (string.IsNullOrWhiteSpace(settings.SiteKey))
            return new HtmlContentViewComponentResult(HtmlString.Empty);

        var appUrl = NormalizeAppUrl(settings.AppUrl);

        // Build the embed exactly once, URL-encoding the site key path segment.
        var src = $"{appUrl}/api/{Uri.EscapeDataString(settings.SiteKey)}/embed.js";

        var html = new System.Text.StringBuilder();

        if (settings.EnableConsentMode)
        {
            html.AppendLine("<!-- ConsentX: Google Consent Mode v2 defaults -->");
            html.AppendLine("<script>");
            html.AppendLine("window.dataLayer = window.dataLayer || [];");
            html.AppendLine("function gtag(){dataLayer.push(arguments);}");
            html.AppendLine("gtag('consent','default',{ad_storage:'denied',analytics_storage:'denied',ad_user_data:'denied',ad_personalization:'denied',functionality_storage:'denied',personalization_storage:'denied',security_storage:'granted',wait_for_update:500});");
            html.AppendLine("</script>");
        }

        // The site key is server-issued; HtmlEncode the attribute defensively.
        var encodedSrc = System.Net.WebUtility.HtmlEncode(src);
        var encodedKey = System.Net.WebUtility.HtmlEncode(settings.SiteKey);
        html.AppendLine("<!-- ConsentX cookie consent embed -->");
        html.AppendLine($"<script type=\"module\" data-consentx=\"{encodedKey}\" src=\"{encodedSrc}\"></script>");

        return new HtmlContentViewComponentResult(new HtmlString(html.ToString()));
    }

    /// <summary>
    /// Trim trailing slash and fall back to the production host if unset.
    /// </summary>
    private static string NormalizeAppUrl(string appUrl)
    {
        if (string.IsNullOrWhiteSpace(appUrl))
            return "https://app.consentx.io";
        return appUrl.TrimEnd('/');
    }
}
