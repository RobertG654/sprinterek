using DotNetNuke.Web.Api;

namespace DNNHello.DNNHello
{
    public class RouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute(
                "DNNHello",
                "default",
                "{controller}/{action}",
                new[] { "DNNHello.DNNHello.Controllers" }
            );
        }
    }
}