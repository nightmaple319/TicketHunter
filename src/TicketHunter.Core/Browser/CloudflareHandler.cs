using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace TicketHunter.Core.Browser;

public class CloudflareHandler
{
    private readonly ILogger<CloudflareHandler> _logger;
    private const int MaxRetries = 3;

    public CloudflareHandler(ILogger<CloudflareHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> DetectAndHandleAsync(IPage page)
    {
        if (!await IsCloudflareChallenge(page))
            return false;

        _logger.LogInformation("Cloudflare Turnstile challenge detected");

        for (int i = 0; i < MaxRetries; i++)
        {
            var handled = await TryHandleTurnstile(page);
            if (handled)
            {
                _logger.LogInformation("Cloudflare challenge solved on attempt {Attempt}", i + 1);
                return true;
            }

            await Task.Delay(1000);
        }

        _logger.LogWarning("Failed to solve Cloudflare challenge after {MaxRetries} attempts", MaxRetries);
        return false;
    }

    private static async Task<bool> IsCloudflareChallenge(IPage page)
    {
        // Layer 1: Check for Turnstile iframe
        var hasTurnstileFrame = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('iframe[src*=\"challenges.cloudflare.com\"]')");
        if (hasTurnstileFrame) return true;

        // Layer 2: Check for cf-turnstile div
        var hasTurnstileDiv = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('.cf-turnstile, #cf-turnstile-response')");
        if (hasTurnstileDiv) return true;

        // Layer 3: Check page content keywords
        var hasKeywords = await page.EvaluateExpressionAsync<bool>(
            "document.body?.innerText?.includes('Checking your browser') || " +
            "document.body?.innerText?.includes('Just a moment') || false");

        return hasKeywords;
    }

    private async Task<bool> TryHandleTurnstile(IPage page)
    {
        try
        {
            // Method 1: Try clicking via CDP DOM piercing (shadow DOM)
            var clicked = await TryClickViaCdp(page);
            if (clicked)
            {
                await WaitForChallengeResolution(page);
                return !await IsCloudflareChallenge(page);
            }

            // Method 2: Try finding and clicking the checkbox by position
            clicked = await TryClickByPosition(page);
            if (clicked)
            {
                await WaitForChallengeResolution(page);
                return !await IsCloudflareChallenge(page);
            }

            // Method 3: Wait for auto-resolution
            await WaitForChallengeResolution(page);
            return !await IsCloudflareChallenge(page);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Turnstile handling attempt failed");
            return false;
        }
    }

    private static async Task<bool> TryClickViaCdp(IPage page)
    {
        try
        {
            var iframe = await page.QuerySelectorAsync("iframe[src*='challenges.cloudflare.com']");
            if (iframe == null) return false;

            var frame = await iframe.ContentFrameAsync();
            if (frame == null) return false;

            var checkbox = await frame.QuerySelectorAsync("input[type='checkbox'], .mark");
            if (checkbox == null) return false;

            await checkbox.ClickAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryClickByPosition(IPage page)
    {
        try
        {
            var iframe = await page.QuerySelectorAsync("iframe[src*='challenges.cloudflare.com']");
            if (iframe == null) return false;

            var box = await iframe.BoundingBoxAsync();
            if (box == null) return false;

            // Click near the center-left of the iframe where the checkbox usually is
            await page.Mouse.ClickAsync(box.X + 30, box.Y + box.Height / 2);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForChallengeResolution(IPage page)
    {
        // Wait up to 5 seconds for the challenge to resolve
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);
            if (!await IsCloudflareChallenge(page))
                return;
        }
    }
}
