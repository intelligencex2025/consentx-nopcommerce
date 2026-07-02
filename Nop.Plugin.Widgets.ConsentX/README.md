# ConsentX for nopCommerce

**Cookie consent & CMP — GDPR, CCPA, DPDPA. 1-click connect.**

`Nop.Plugin.Widgets.ConsentX` is a widget plugin for **nopCommerce 4.60 / 4.70**
that installs the [ConsentX](https://consentx.io) cookie-consent banner on your
storefront. Connect your ConsentX account in one click and the widget installs
itself — no manual domain entry, no copy-pasting snippets.

- Injects the self-contained ConsentX embed (`<script type="module">`) into the
  HTML `<head>` of every public page (`head_html_tag` widget zone).
- 1-click **Connect** handshake (Model A): authorizes your store domain on
  ConsentX, auto-registers it in the allowlist, and returns a scoped Site Key.
- Optional **Google Consent Mode v2** denied-by-default signals.
- Per-store configuration (multi-store aware).

---

## Requirements

| | |
|---|---|
| nopCommerce | 4.60 or 4.70 |
| .NET | 7.0 |

## Install

### From a packaged plugin (recommended)

1. In the nopCommerce admin, go to **Configuration → Local plugins**.
2. Click **Upload plugin or theme** and choose the ConsentX plugin zip.
3. Find **ConsentX — Cookie Consent & CMP** in the list and click **Install**.
4. Restart the application when prompted.

### From source

1. Copy the `Nop.Plugin.Widgets.ConsentX` folder into the `Plugins` folder of
   your nopCommerce **source solution** (alongside the other `Nop.Plugin.*`
   projects), and add it to `NopCommerce.sln`.
2. Build the solution. The `.csproj` `OutputPath` places the build artefacts
   into `Presentation/Nop.Web/Plugins/Widgets.ConsentX` automatically.
3. Start the site, then **Configuration → Local plugins → Install**.

## Connect (1-click)

1. Open **Configuration → Widgets** (or **Local plugins**) and click
   **Configure** on ConsentX.
2. Click **Connect to ConsentX**.
3. Log into (or sign up free for) ConsentX and **Approve**. You are redirected
   back to your store admin — the Site Key is stored automatically and the
   banner goes live immediately.

The store domain shown on the Configure screen is authorized and registered in
your ConsentX allowlist during approval, so the embed works the instant the key
is stored.

**Manual fallback:** you can also paste a Site Key from the ConsentX dashboard
(**Websites → your site**) into the **Site Key** field and save.

## How the embed is rendered

For a connected store the plugin renders into `head_html_tag`:

```html
<!-- ConsentX: Google Consent Mode v2 defaults (optional) -->
<script>/* denied-by-default gtag consent stub */</script>

<!-- ConsentX cookie consent embed -->
<script type="module" data-consentx="SITE_KEY"
        src="https://app.consentx.io/api/SITE_KEY/embed.js"></script>
```

`embed.js` is self-contained: it injects the scoped widget CSS + JS, mounts the
banner into a `#consentx-cookie-consent` div it appends to `document.body`, reads
geo/config from the server, and emits the `cx:consent` event
(`detail.granted` = array of granted category slugs). Once loaded it exposes
`window.ConsentX` to re-open preferences.

## Settings

| Setting | Description |
|---|---|
| **Site Key** | Your ConsentX Site Key. Set by Connect or entered manually. |
| **Google Consent Mode v2 defaults** | Print the denied-by-default `gtag` consent stub before analytics tags. |
| **ConsentX app host** | App host override (default `https://app.consentx.io`) for staging / self-hosted. |

## Disconnect

Click **Disconnect** to clear the stored Site Key + token locally. The scoped
token can also be revoked from the ConsentX admin → **API Tokens**.

## Security

- The Connect handshake mints a random 256-bit CSRF `state`, stores it
  server-side, and verifies it with a constant-time comparison on callback.
- The callback is a same-host admin URL on your own store domain (its host
  equals the authorized `domain`), so ConsentX never redirects credentials to a
  foreign host.
- All rendered output is HTML-encoded; the Site Key path segment is URL-encoded.

## License

GPL-2.0-or-later. ConsentX · https://consentx.io
