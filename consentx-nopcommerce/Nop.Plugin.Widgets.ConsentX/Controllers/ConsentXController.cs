using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.ConsentX.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.ConsentX.Controllers;

/// <summary>
/// Admin controller: configuration screen + the 1-click Connect handshake
/// (Model A) per the ConsentX connect spec, section 1.
///
///   Configure            GET/POST  — settings screen with Connect button
///   ConnectStart         GET       — mint CSRF state, redirect to ConsentX
///   ConnectCallback      GET       — verify state, persist site_key + token
///   Disconnect           GET       — clear stored credentials
///
/// The callback is a same-host admin URL on the merchant's own domain, so its
/// host equals the `domain` we authorize — which is exactly what ConsentX
/// requires (it rejects mismatched hosts to prevent token leakage).
/// </summary>
[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public class ConsentXController : BasePluginController
{
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWebHelper _webHelper;

    public ConsentXController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _webHelper = webHelper;
    }

    #region Configuration screen

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
            return AccessDeniedView();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeScope);

        var model = new ConfigurationModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            SiteKey = settings.SiteKey,
            AppUrl = string.IsNullOrWhiteSpace(settings.AppUrl) ? "https://app.consentx.io" : settings.AppUrl,
            EnableConsentMode = settings.EnableConsentMode,
            IsConnected = !string.IsNullOrWhiteSpace(settings.SiteKey),
            SiteHost = SiteHost(),
            DashboardUrl = NormalizeAppUrl(settings.AppUrl)
        };

        if (storeScope > 0)
        {
            model.SiteKey_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.SiteKey, storeScope);
            model.AppUrl_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AppUrl, storeScope);
            model.EnableConsentMode_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.EnableConsentMode, storeScope);
        }

        return View("~/Plugins/Widgets.ConsentX/Views/Configure.cshtml", model);
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("save")]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeScope);

        // SiteKey is normally managed by Connect, but allow a manual fallback entry.
        settings.SiteKey = (model.SiteKey ?? string.Empty).Trim();
        settings.AppUrl = NormalizeAppUrl(model.AppUrl);
        settings.EnableConsentMode = model.EnableConsentMode;

        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.SiteKey, model.SiteKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AppUrl, model.AppUrl_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.EnableConsentMode, model.EnableConsentMode_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion

    #region Connect handshake (Model A)

    /// <summary>
    /// Step 1 — generate a CSRF state, store it server-side, and redirect the
    /// admin's browser to the ConsentX authorize screen with a same-host callback.
    /// </summary>
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ConnectStart()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
            return AccessDeniedView();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeScope);

        var state = GenerateState();
        settings.ConnectState = state;
        await _settingService.SaveSettingAsync(settings, x => x.ConnectState, storeScope, false);
        await _settingService.ClearCacheAsync();

        // Callback is an admin URL on THIS store's own domain (host == domain).
        var callback = $"{_webHelper.GetStoreLocation().TrimEnd('/')}/Admin/ConsentX/ConnectCallback";

        var appUrl = NormalizeAppUrl(settings.AppUrl);

        // Build the query with single-pass URL encoding (do not pre-encode values).
        var query = new Dictionary<string, string>
        {
            ["client"] = ConsentXDefaults.ConnectClient,
            ["redirect_uri"] = callback,
            ["state"] = state,
            ["domain"] = SiteHost(),
            ["site_name"] = StoreName()
        };

        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var authorizeUrl = $"{appUrl}/admin/connect?{qs}";

        return Redirect(authorizeUrl);
    }

    /// <summary>
    /// Step 3 — ConsentX 302s back here with ?site_key=&amp;token=&amp;state=.
    /// Verify the round-tripped state (constant-time), persist credentials.
    /// </summary>
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ConnectCallback(string site_key, string token, string state)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
            return AccessDeniedView();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeScope);

        var expected = settings.ConnectState ?? string.Empty;

        // Always clear the one-time state so it cannot be replayed.
        settings.ConnectState = string.Empty;
        await _settingService.SaveSettingAsync(settings, x => x.ConnectState, storeScope, false);

        if (string.IsNullOrEmpty(expected) || !FixedTimeEquals(expected, state ?? string.Empty))
        {
            await _settingService.ClearCacheAsync();
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.ConsentX.Connect.Error.State"));
            return RedirectToAction("Configure");
        }

        if (string.IsNullOrWhiteSpace(site_key))
        {
            await _settingService.ClearCacheAsync();
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.ConsentX.Connect.Error.NoKey"));
            return RedirectToAction("Configure");
        }

        settings.SiteKey = site_key.Trim();
        settings.Token = (token ?? string.Empty).Trim();

        await _settingService.SaveSettingAsync(settings, x => x.SiteKey, storeScope, false);
        await _settingService.SaveSettingAsync(settings, x => x.Token, storeScope, false);
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.ConsentX.Connected.Success"));

        return RedirectToAction("Configure");
    }

    /// <summary>
    /// Clear stored credentials locally. The scoped token can additionally be
    /// revoked from the ConsentX admin → API Tokens.
    /// </summary>
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Disconnect()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
            return AccessDeniedView();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<ConsentXSettings>(storeScope);

        settings.SiteKey = string.Empty;
        settings.Token = string.Empty;
        settings.ConnectState = string.Empty;

        await _settingService.SaveSettingAsync(settings, x => x.SiteKey, storeScope, false);
        await _settingService.SaveSettingAsync(settings, x => x.Token, storeScope, false);
        await _settingService.SaveSettingAsync(settings, x => x.ConnectState, storeScope, false);
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.ConsentX.Connected.Disconnected"));

        return RedirectToAction("Configure");
    }

    #endregion

    #region Helpers

    /// <summary>Bare host for this store (no scheme, no www, no port).</summary>
    private string SiteHost()
    {
        var location = _webHelper.GetStoreLocation();
        if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
            return string.Empty;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www."))
            host = host[4..];
        return host;
    }

    private string StoreName()
    {
        try
        {
            var store = _storeContext.GetCurrentStoreAsync().GetAwaiter().GetResult();
            return store?.Name ?? SiteHost();
        }
        catch
        {
            return SiteHost();
        }
    }

    private static string NormalizeAppUrl(string appUrl)
    {
        if (string.IsNullOrWhiteSpace(appUrl))
            return "https://app.consentx.io";
        return appUrl.Trim().TrimEnd('/');
    }

    private static string GenerateState()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }

    /// <summary>Constant-time string comparison for the CSRF state.</summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    #endregion
}
