using Microsoft.AspNetCore.Http;

namespace SchoolSystemAPI.Services;

public class UrlHelperService : IUrlHelperService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UrlHelperService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetAbsoluteUrl(string relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return string.Empty;

        if (relativeUrl.StartsWith("http://") || relativeUrl.StartsWith("https://"))
            return relativeUrl; // Already absolute

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return relativeUrl;

        var scheme = request.Scheme;
        var host = request.Host.Value;

        // Ensure proper formatting (no double slashes)
        if (!relativeUrl.StartsWith("/"))
            relativeUrl = "/" + relativeUrl;

        return $"{scheme}://{host}{relativeUrl}";
    }
}
