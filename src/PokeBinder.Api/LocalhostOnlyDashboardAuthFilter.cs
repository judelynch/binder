using Hangfire.Dashboard;

namespace PokeBinder.Api;

/// <summary>
/// This API is JWT-bearer-only (no cookie auth), so Hangfire's default dashboard auth has
/// nothing to check when hit from a browser. Restricting to localhost matches this app's existing
/// "local instance, managed by the owner" framing rather than standing up a second auth scheme
/// just for one admin's own dashboard.
/// </summary>
public class LocalhostOnlyDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var ip = context.GetHttpContext().Connection.RemoteIpAddress;
        return ip is not null && System.Net.IPAddress.IsLoopback(ip);
    }
}
