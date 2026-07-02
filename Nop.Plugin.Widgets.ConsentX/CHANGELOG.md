# Changelog

All notable changes to the ConsentX plugin for nopCommerce are documented here.
This project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-06-20

### Added
- Initial release for nopCommerce 4.60 / 4.70 (.NET 7).
- `IWidgetPlugin` rendering into the `head_html_tag` public widget zone.
- Self-contained ConsentX embed injection
  (`<script type="module" src="{AppUrl}/api/{SiteKey}/embed.js">`).
- 1-click Connect handshake (Model A, client id `nopcommerce`): CSRF `state`
  generation + constant-time verification, same-host admin callback, automatic
  storage of `site_key` + scoped `token` via `ISettingService`.
- Admin configuration screen with brandbook logo, connection status, Connect /
  Disconnect, manual Site Key fallback, Consent Mode toggle, app-host override,
  and a dashboard link. Multi-store (per-store override) aware.
- Optional Google Consent Mode v2 denied-by-default stub.
- English locale resources; install/uninstall lifecycle.
