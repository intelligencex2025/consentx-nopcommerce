using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Widgets.ConsentX.Infrastructure;

/// <summary>
/// Registers stable admin routes for the ConsentX configuration screen and the
/// Connect handshake endpoints. The callback route MUST resolve on the store's
/// own host so its host matches the authorized `domain`.
/// </summary>
public class RouteProvider : IRouteProvider
{
    public int Priority => 0;

    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(
            name: ConsentXDefaults.ConfigureRouteName,
            pattern: "Admin/ConsentX/Configure",
            defaults: new { controller = "ConsentX", action = "Configure", area = AreaNames.Admin });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Widgets.ConsentX.ConnectStart",
            pattern: "Admin/ConsentX/ConnectStart",
            defaults: new { controller = "ConsentX", action = "ConnectStart", area = AreaNames.Admin });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Widgets.ConsentX.ConnectCallback",
            pattern: "Admin/ConsentX/ConnectCallback",
            defaults: new { controller = "ConsentX", action = "ConnectCallback", area = AreaNames.Admin });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Widgets.ConsentX.Disconnect",
            pattern: "Admin/ConsentX/Disconnect",
            defaults: new { controller = "ConsentX", action = "Disconnect", area = AreaNames.Admin });
    }
}
