using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TicketHunter.Core.Models;
using TicketHunter.Core.Services;
using TicketHunter.Core.Utils;

namespace TicketHunter.Core.Platforms;

public class TicketmasterHandler : BasePlatformHandler
{
    public override string PlatformName => "Ticketmaster";
    public override PlatformType PlatformType => PlatformType.Ticketmaster;

    public TicketmasterHandler(ILogger<TicketmasterHandler> logger) : base(logger) { }

    public override bool CanHandle(string url)
        => url.Contains("ticketmaster", StringComparison.OrdinalIgnoreCase);

    public override async Task InjectAuthAsync(IPage page, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Accounts.TicketmasterCookie)) return;

        var domain = ExtractDomain(config.Homepage);
        var pairs = config.Accounts.TicketmasterCookie.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex <= 0) continue;

            await page.SetCookieAsync(new CookieParam
            {
                Name = pair[..eqIndex].Trim(),
                Value = pair[(eqIndex + 1)..].Trim(),
                Domain = domain,
                Path = "/",
                Secure = true
            });
        }

        Logger.LogInformation("Ticketmaster cookies injected for domain {Domain}", domain);
    }

    public override async Task<PageState> DetectPageStateAsync(IPage page)
    {
        var url = page.Url;

        // Queue/waiting room
        var isQueue = await page.EvaluateExpressionAsync<bool>(
            "document.body?.innerText?.includes('Queue') || " +
            "document.body?.innerText?.includes('waiting room') || " +
            "document.body?.innerText?.includes('in line') || " +
            "!!document.querySelector('#queue-it_log') || false");
        if (isQueue) return PageState.Queue;

        // Cloudflare
        var isCf = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('iframe[src*=\"challenges.cloudflare.com\"]') || false");
        if (isCf) return PageState.CloudflareChallenge;

        // Event page with dates
        var hasDateOptions = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('[data-testid=\"event-listing\"], .event-listing, .date-selector') || false");
        if (hasDateOptions) return PageState.DateSelection;

        // Seat/area selection
        var hasSeats = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('.offer-card, .ticket-type, [data-testid=\"offer-card\"], .seat-selection') || false");
        if (hasSeats) return PageState.AreaSelection;

        // Checkout / quantity
        var isCheckout = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('.checkout, [data-testid=\"checkout\"], .cart-summary') || false");
        if (isCheckout) return PageState.QuantityAndCaptcha;

        // Order confirmed
        var isComplete = await page.EvaluateExpressionAsync<bool>(
            "document.body?.innerText?.includes('Order Confirmed') || " +
            "document.body?.innerText?.includes('order confirmation') || false");
        if (isComplete) return PageState.OrderComplete;

        // Sold out
        var isSoldOut = await page.EvaluateExpressionAsync<bool>(
            "document.body?.innerText?.includes('Sold Out') || " +
            "document.body?.innerText?.includes('No tickets available') || false");
        if (isSoldOut) return PageState.SoldOut;

        return PageState.Unknown;
    }

    public override async Task<bool> SelectDateAsync(IPage page, string keyword, string mode)
    {
        Logger.LogInformation("Selecting Ticketmaster date: keyword={Keyword}", keyword);

        var dateTexts = await page.EvaluateFunctionAsync<List<string>>(@"() => {
            const items = document.querySelectorAll('[data-testid=""event-listing""] a, .event-listing a, .event-card');
            return Array.from(items).map(el => el.innerText.trim()).filter(t => t.length > 0);
        }");

        if (dateTexts.Count == 0) return false;

        var matches = KeywordMatcher.FindAllMatches(dateTexts, keyword);
        var selected = KeywordMatcher.SelectByMode(matches.Count > 0 ? matches : dateTexts, mode);
        if (selected == null) return false;

        Logger.LogInformation("Selected: {Date}", selected);
        return await ClickElementByText(page, "[data-testid='event-listing'] a, .event-listing a, .event-card", selected);
    }

    public override async Task<bool> SelectAreaAsync(IPage page, string keyword, string mode, string excludeKeyword)
    {
        Logger.LogInformation("Selecting Ticketmaster area: keyword={Keyword}", keyword);

        // Wait for offer cards to load
        await WaitForSelectorSafe(page, ".offer-card, .ticket-type, [data-testid='offer-card']", 5000);

        var offerTexts = await page.EvaluateFunctionAsync<List<string>>(@"() => {
            const offers = document.querySelectorAll('.offer-card, .ticket-type, [data-testid=""offer-card""]');
            return Array.from(offers).map(o => o.innerText.trim()).filter(t => t.length > 0);
        }");

        if (offerTexts.Count == 0) return false;

        var matches = KeywordMatcher.FindAllMatches(offerTexts, keyword, excludeKeyword);
        var selected = KeywordMatcher.SelectByMode(matches.Count > 0 ? matches : offerTexts, mode);
        if (selected == null) return false;

        Logger.LogInformation("Selected offer: {Offer}", selected);

        // Click the offer and then the "Get Tickets" or equivalent button
        var clicked = await ClickElementByText(page, ".offer-card, .ticket-type, [data-testid='offer-card']", selected);
        if (!clicked) return false;

        // Try clicking the action button within the selected offer
        await page.EvaluateExpressionAsync(
            "document.querySelector('.offer-card.selected button, .offer-card:hover button, [data-testid=\"get-tickets-button\"]')?.click()");

        return true;
    }

    public override async Task<bool> FillQuantityAsync(IPage page, int quantity)
    {
        Logger.LogInformation("Setting Ticketmaster quantity: {Quantity}", quantity);

        return await page.EvaluateFunctionAsync<bool>(@"(qty) => {
            // Try quantity dropdown
            const select = document.querySelector('select[data-testid=""quantity-selector""], select.quantity-selector, select[name*=""quantity""]');
            if (select) {
                select.value = qty.toString();
                select.dispatchEvent(new Event('change', { bubbles: true }));
                return true;
            }
            // Try plus button
            const plusBtn = document.querySelector('[data-testid=""quantity-plus""], button.plus, .qty-increase');
            if (plusBtn) {
                for (let i = 0; i < qty - 1; i++) plusBtn.click();
                return true;
            }
            return false;
        }", quantity);
    }

    public override async Task<bool> SolveCaptchaAsync(IPage page, OcrService ocrService)
    {
        // Ticketmaster typically uses reCAPTCHA/hCaptcha which requires manual intervention
        Logger.LogWarning("Ticketmaster CAPTCHA detected - may require manual intervention");

        var hasRecaptcha = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('iframe[src*=\"recaptcha\"], iframe[src*=\"hcaptcha\"], .g-recaptcha') || false");

        if (hasRecaptcha)
        {
            Logger.LogWarning("reCAPTCHA/hCaptcha detected - waiting for manual solution");
            return false;
        }

        return true;
    }

    public override async Task<bool> AcceptTermsAsync(IPage page)
    {
        return await page.EvaluateFunctionAsync<bool>(@"() => {
            const checkbox = document.querySelector('input[type=""checkbox""][name*=""terms""], input[type=""checkbox""][name*=""agree""]');
            if (checkbox && !checkbox.checked) { checkbox.click(); return true; }
            return checkbox?.checked || false;
        }");
    }

    public override async Task<bool> SubmitOrderAsync(IPage page)
    {
        Logger.LogInformation("Submitting Ticketmaster order");
        await AcceptTermsAsync(page);

        return await page.EvaluateFunctionAsync<bool>(@"() => {
            const btn = document.querySelector('[data-testid=""checkout-button""], button.checkout-btn, button[type=""submit""], .place-order-btn');
            if (btn) { btn.click(); return true; }
            return false;
        }");
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return "." + uri.Host;
        }
        catch
        {
            return ".ticketmaster.com";
        }
    }
}
