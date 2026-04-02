using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TicketHunter.Core.Models;

namespace TicketHunter.Core.Browser;

public class CookieManager
{
    private readonly ILogger<CookieManager> _logger;

    public CookieManager(ILogger<CookieManager> logger)
    {
        _logger = logger;
    }

    public async Task InjectCookiesAsync(IPage page, AppConfig config)
    {
        var platform = DetectPlatform(config.Homepage);

        switch (platform)
        {
            case PlatformType.Tixcraft:
                await InjectTixcraftCookies(page, config.Accounts.TixcraftSid);
                break;
            case PlatformType.Ticketmaster:
                await InjectTicketmasterCookies(page, config.Accounts.TicketmasterCookie);
                break;
        }
    }

    public static PlatformType DetectPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return PlatformType.Unknown;

        var lower = url.ToLowerInvariant();
        if (lower.Contains("tixcraft.com")) return PlatformType.Tixcraft;
        if (lower.Contains("ticketmaster")) return PlatformType.Ticketmaster;

        return PlatformType.Unknown;
    }

    private async Task InjectTixcraftCookies(IPage page, string sid)
    {
        if (string.IsNullOrWhiteSpace(sid))
        {
            _logger.LogWarning("Tixcraft SID is empty, skipping cookie injection");
            return;
        }

        await page.SetCookieAsync(new CookieParam
        {
            Name = "TIXUISID",
            Value = sid,
            Domain = ".tixcraft.com",
            Path = "/",
            HttpOnly = false,
            Secure = true
        });

        _logger.LogInformation("Tixcraft cookie injected (TIXUISID)");
    }

    private async Task InjectTicketmasterCookies(IPage page, string cookieString)
    {
        if (string.IsNullOrWhiteSpace(cookieString))
        {
            _logger.LogWarning("Ticketmaster cookie is empty, skipping cookie injection");
            return;
        }

        var cookies = ParseCookieString(cookieString, ".ticketmaster.com");
        if (cookies.Count > 0)
        {
            await page.SetCookieAsync(cookies.ToArray());
            _logger.LogInformation("Ticketmaster cookies injected ({Count} cookies)", cookies.Count);
        }
    }

    private static List<CookieParam> ParseCookieString(string cookieString, string domain)
    {
        var cookies = new List<CookieParam>();
        var pairs = cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex <= 0) continue;

            var name = pair[..eqIndex].Trim();
            var value = pair[(eqIndex + 1)..].Trim();

            cookies.Add(new CookieParam
            {
                Name = name,
                Value = value,
                Domain = domain,
                Path = "/",
                HttpOnly = false,
                Secure = true
            });
        }

        return cookies;
    }
}
