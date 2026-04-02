using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TicketHunter.Core.Models;

namespace TicketHunter.Core.Browser;

public class BrowserManager : IAsyncDisposable
{
    private readonly ILogger<BrowserManager> _logger;
    private IBrowser? _browser;
    private IPage? _page;

    public IPage? Page => _page;
    public bool IsRunning => _browser != null && !_browser.IsClosed;

    public BrowserManager(ILogger<BrowserManager> logger)
    {
        _logger = logger;
    }

    public async Task<IPage> LaunchAsync(AppConfig config)
    {
        _logger.LogInformation("Downloading browser if needed...");
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        var launchOptions = new LaunchOptions
        {
            Headless = config.Advanced.Headless,
            Args = BuildArgs(config),
            DefaultViewport = null
        };

        _logger.LogInformation("Launching browser (headless={Headless})...", config.Advanced.Headless);
        _browser = await Puppeteer.LaunchAsync(launchOptions);

        var pages = await _browser.PagesAsync();
        _page = pages.Length > 0 ? pages[0] : await _browser.NewPageAsync();

        await StealthPlugin.ApplyAsync(_page);

        await _page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        _logger.LogInformation("Browser launched successfully");
        return _page;
    }

    private static string[] BuildArgs(AppConfig config)
    {
        var args = new List<string>
        {
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--window-size=1366,768",
            "--start-maximized"
        };

        if (!string.IsNullOrWhiteSpace(config.Advanced.ProxyServer))
        {
            args.Add($"--proxy-server={config.Advanced.ProxyServer}");
        }

        return args.ToArray();
    }

    public async Task NavigateAsync(string url)
    {
        if (_page == null) throw new InvalidOperationException("Browser not launched");
        _logger.LogInformation("Navigating to {Url}", url);
        await _page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
    }

    public async Task ReloadAsync()
    {
        if (_page == null) return;
        await _page.ReloadAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null)
        {
            try { await _page.CloseAsync(); } catch { }
            _page = null;
        }
        if (_browser != null)
        {
            try { await _browser.CloseAsync(); } catch { }
            _browser = null;
        }
        GC.SuppressFinalize(this);
    }
}
