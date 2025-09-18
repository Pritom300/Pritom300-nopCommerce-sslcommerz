using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.SslCommerz
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Admin configure route
            endpointRouteBuilder.MapControllerRoute(
                name: "PaymentSslCommerz.Configure",
                pattern: "Admin/PaymentSslCommerz/Configure",
                defaults: new { controller = "AdminPaymentSslCommerz", action = "Configure" });

            // Public callback route (SSLCommerz will POST here)
            endpointRouteBuilder.MapControllerRoute(
                name: "PaymentSslCommerz.Callback",
                pattern: "PaymentSslCommerz/Callback",
                defaults: new { controller = "PaymentSslCommerz", action = "Callback" });
        }

        public int Priority => 0;
    }
}
