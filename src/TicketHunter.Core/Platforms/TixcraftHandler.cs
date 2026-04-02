using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TicketHunter.Core.Models;
using TicketHunter.Core.Services;
using TicketHunter.Core.Utils;

namespace TicketHunter.Core.Platforms;

public class TixcraftHandler : BasePlatformHandler
{
    public override string PlatformName => "Tixcraft";
    public override PlatformType PlatformType => PlatformType.Tixcraft;

    public TixcraftHandler(ILogger<TixcraftHandler> logger) : base(logger) { }

    public override bool CanHandle(string url)
        => url.Contains("tixcraft.com", StringComparison.OrdinalIgnoreCase);

    public override async Task InjectAuthAsync(IPage page, AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Accounts.TixcraftSid)) return;

        await page.SetCookieAsync(new CookieParam
        {
            Name = "TIXUISID",
            Value = config.Accounts.TixcraftSid,
            Domain = ".tixcraft.com",
            Path = "/",
            HttpOnly = false,
            Secure = true
        });

        Logger.LogInformation("Tixcraft auth cookie injected");
    }

    public override async Task<PageState> DetectPageStateAsync(IPage page)
    {
        var url = page.Url;

        // Check Cloudflare first
        var isCf = await page.EvaluateExpressionAsync<bool>(
            "!!document.querySelector('iframe[src*=\"challenges.cloudflare.com\"]') || " +
            "document.body?.innerText?.includes('Just a moment') || false");
        if (isCf) return PageState.CloudflareChallenge;

        // Activity detail page → redirect to game page (skip "立即購票" button)
        if (url.Contains("/activity/detail/"))
        {
            await RedirectDetailToGameAsync(page, url);
            return PageState.DateSelection;
        }

        // Activity/game page with date list
        if (url.Contains("/activity/game/"))
        {
            // Wait for date list to load
            await WaitForSelectorSafe(page, "#gameList > table > tbody > tr", 5000);

            var hasSoldOut = await page.EvaluateExpressionAsync<bool>(
                "document.body?.innerText?.includes('已售完') || document.body?.innerText?.includes('Sold Out') || false");
            if (hasSoldOut)
            {
                var hasAvailable = await page.EvaluateExpressionAsync<bool>(
                    "!!document.querySelector('button.btn-primary:not([disabled])')");
                if (!hasAvailable) return PageState.SoldOut;
            }

            return PageState.DateSelection;
        }

        // Ticket area selection
        if (url.Contains("/ticket/area/"))
            return PageState.AreaSelection;

        // Ticket quantity + image captcha page
        if (url.Contains("/ticket/ticket/"))
            return PageState.QuantityAndCaptcha;

        // Text question verify page
        if (url.Contains("/ticket/verify/"))
            return PageState.Verify;

        // Order complete
        if (url.Contains("/ticket/order"))
            return PageState.OrderComplete;

        // Coming soon
        var isComingSoon = await page.EvaluateExpressionAsync<bool>(
            "document.body?.innerText?.includes('Coming Soon') || " +
            "document.body?.innerText?.includes('即將開賣') || false");
        if (isComingSoon) return PageState.ComingSoon;

        return PageState.Unknown;
    }

    /// <summary>
    /// Redirect /activity/detail/{id} → /activity/game/{id} directly,
    /// skipping the "立即購票" button click. This is faster and more reliable.
    /// </summary>
    private async Task RedirectDetailToGameAsync(IPage page, string url)
    {
        var gameUrl = url.Replace("/activity/detail/", "/activity/game/");
        Logger.LogInformation("Redirecting detail → game: {Url}", gameUrl);

        await page.GoToAsync(gameUrl, WaitUntilNavigation.DOMContentLoaded);

        // Wait for date list table to appear
        await WaitForSelectorSafe(page, "#gameList > table > tbody > tr", 5000);
    }

    public override async Task<bool> SelectDateAsync(IPage page, string keyword, string mode)
    {
        Logger.LogInformation("Selecting date with keyword: {Keyword}, mode: {Mode}", keyword, mode);

        // Get all available date/session rows from #gameList table
        // Each row with a "立即訂購" / "Find tickets" button is available
        var dateTexts = await page.EvaluateFunctionAsync<List<string>>(@"() => {
            const rows = document.querySelectorAll('#gameList > table > tbody > tr');
            const texts = [];
            rows.forEach(row => {
                const btn = row.querySelector('button:not([disabled]):not(.disabled), input[type=""submit""]:not([disabled])');
                const btnText = btn?.value || btn?.innerText || '';
                const isAvailable = btnText.includes('立即訂購') || btnText.includes('Find tickets')
                    || btnText.includes('Start ordering') || btnText.includes('お申込みへ進む');
                if (btn && isAvailable) {
                    texts.push(row.innerText.trim());
                }
            });
            return texts;
        }");

        if (dateTexts.Count == 0)
        {
            Logger.LogWarning("No available dates found (all sold out or not on sale)");
            return false;
        }

        var matches = KeywordMatcher.FindAllMatches(dateTexts, keyword);
        var selected = KeywordMatcher.SelectByMode(matches.Count > 0 ? matches : dateTexts, mode);

        if (selected == null)
        {
            Logger.LogWarning("No date matched");
            return false;
        }

        Logger.LogInformation("Selected date: {Date}", selected);

        // Click the "立即訂購" button in the matching row
        var clicked = await page.EvaluateFunctionAsync<bool>(@"(targetText) => {
            const rows = document.querySelectorAll('#gameList > table > tbody > tr');
            for (const row of rows) {
                if (row.innerText.trim().includes(targetText.substring(0, 20))) {
                    const btn = row.querySelector('button:not([disabled]):not(.disabled), input[type=""submit""]:not([disabled])');
                    if (btn) { btn.click(); return true; }
                }
            }
            return false;
        }", selected);

        return clicked;
    }

    public override async Task<bool> SelectAreaAsync(IPage page, string keyword, string mode, string excludeKeyword)
    {
        Logger.LogInformation("Selecting area with keyword: {Keyword}, exclude: {Exclude}", keyword, excludeKeyword);

        var areaTexts = await page.EvaluateFunctionAsync<List<string>>(@"() => {
            const areas = document.querySelectorAll('.zone-list li a, .area-list a, table.table-striped tbody tr');
            return Array.from(areas).map(a => a.innerText.trim()).filter(t => t.length > 0);
        }");

        if (areaTexts.Count == 0)
        {
            Logger.LogWarning("No areas found");
            return false;
        }

        var matches = KeywordMatcher.FindAllMatches(areaTexts, keyword, excludeKeyword);
        var selected = KeywordMatcher.SelectByMode(matches.Count > 0 ? matches : areaTexts, mode);

        if (selected == null)
        {
            Logger.LogWarning("No area matched");
            return false;
        }

        Logger.LogInformation("Selected area: {Area}", selected);

        return await ClickElementByText(page,
            ".zone-list li a, .area-list a, table.table-striped tbody tr a", selected);
    }

    public override async Task<bool> FillQuantityAsync(IPage page, int quantity)
    {
        Logger.LogInformation("Setting ticket quantity to {Quantity}", quantity);

        return await page.EvaluateFunctionAsync<bool>(@"(qty) => {
            // Try all known select selectors for Tixcraft ticket quantity
            const selectors = [
                'select.mobile-select',
                'select[id*=""TicketForm_ticketPrice""]',
                'select[name*=""quantity""]',
                'select.form-select'
            ];
            for (const sel of selectors) {
                const select = document.querySelector(sel);
                if (select) {
                    select.value = qty.toString();
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    return true;
                }
            }
            return false;
        }", quantity);
    }

    /// <summary>
    /// Solve image CAPTCHA on /ticket/ticket/ page.
    /// The captcha is a 4-character distorted text image.
    /// Retries up to maxRetries times on OCR failure (refreshes captcha each time).
    /// </summary>
    public override async Task<bool> SolveCaptchaAsync(IPage page, OcrService ocrService)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Logger.LogInformation("Attempting to solve image captcha (attempt {Attempt}/{Max})", attempt, maxRetries);

            // Get captcha image via canvas
            var captchaBase64 = await page.EvaluateFunctionAsync<string?>(@"() => {
                const img = document.querySelector('#TicketForm_verifyCode-image');
                if (!img) return null;
                const canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth || img.width;
                canvas.height = img.naturalHeight || img.height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                return canvas.toDataURL('image/png').split(',')[1];
            }");

            if (string.IsNullOrEmpty(captchaBase64))
            {
                Logger.LogWarning("Could not find captcha image (#TicketForm_verifyCode-image)");
                return false;
            }

            var imageBytes = Convert.FromBase64String(captchaBase64);
            var result = ocrService.Recognize(imageBytes);

            // Tixcraft image captcha is always 4 characters
            if (string.IsNullOrEmpty(result) || result.Length != 4)
            {
                Logger.LogWarning("OCR result invalid (expected 4 chars, got: '{Result}'), refreshing captcha",
                    result);
                // Click captcha image to refresh and wait for new image to load
                await page.EvaluateExpressionAsync(
                    "document.querySelector('#TicketForm_verifyCode-image')?.click()");
                await Task.Delay(500);
                continue;
            }

            Logger.LogInformation("OCR result: {Result}", result);

            // Fill captcha input
            var filled = await page.EvaluateFunctionAsync<bool>(@"(text) => {
                const input = document.querySelector('#TicketForm_verifyCode');
                if (!input) return false;
                input.value = '';
                input.focus();
                input.value = text;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                return true;
            }", result);

            if (filled) return true;
        }

        Logger.LogWarning("All {Max} OCR attempts failed", maxRetries);
        return false;
    }

    /// <summary>
    /// Solve text question CAPTCHA on /ticket/verify/ page.
    /// e.g., "請輸入引號內文字【同意】", "輸入YES", math questions, etc.
    /// </summary>
    public override async Task<bool> SolveVerifyQuestionAsync(IPage page)
    {
        Logger.LogInformation("Attempting to solve verify question");

        // Get question text from .zone-verify
        var questionText = await page.EvaluateFunctionAsync<string?>(@"() => {
            const el = document.querySelector('.zone-verify');
            return el ? el.innerText.trim() : null;
        }");

        if (string.IsNullOrEmpty(questionText))
        {
            Logger.LogWarning("Could not find verify question (.zone-verify)");
            return false;
        }

        Logger.LogInformation("Verify question: {Question}", questionText);

        // Try auto-guess the answer
        var answer = CaptchaGuesser.Guess(questionText);

        if (string.IsNullOrEmpty(answer))
        {
            Logger.LogWarning("Cannot auto-guess answer for: {Question}", questionText);
            return false;
        }

        Logger.LogInformation("Auto-guessed answer: {Answer}", answer);

        // Fill answer into input
        var filled = await page.EvaluateFunctionAsync<bool>(@"(text) => {
            const input = document.querySelector('input[name=""checkCode""]');
            if (!input) return false;
            input.value = '';
            input.focus();
            input.value = text;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            return true;
        }", answer);

        if (!filled)
        {
            Logger.LogWarning("Could not fill verify answer");
            return false;
        }

        // Click submit button
        await page.EvaluateExpressionAsync(
            "document.querySelector('button.btn.btn-primary')?.click()");

        Logger.LogInformation("Verify answer submitted");
        return true;
    }

    public override async Task<bool> AcceptTermsAsync(IPage page)
    {
        return await page.EvaluateFunctionAsync<bool>(@"() => {
            const checkbox = document.querySelector('#TicketForm_agree, input[name*=""agree""], input[type=""checkbox""]');
            if (checkbox && !checkbox.checked) {
                checkbox.click();
                return true;
            }
            return checkbox?.checked || false;
        }");
    }

    public override async Task<bool> SubmitOrderAsync(IPage page)
    {
        Logger.LogInformation("Submitting order");

        // Accept terms first
        await AcceptTermsAsync(page);

        return await page.EvaluateFunctionAsync<bool>(@"() => {
            // Try submit button by type
            let btn = document.querySelector('button[type=""submit""], input[type=""submit""]');
            if (btn) { btn.click(); return true; }
            // Try by text content (「確認張數」green button)
            const allBtns = document.querySelectorAll('button.btn-primary, a.btn-primary');
            for (const b of allBtns) {
                const text = b.innerText.trim();
                if (text.includes('確認') || text.includes('Submit') || text.includes('送出')) {
                    b.click();
                    return true;
                }
            }
            // Fallback: find form and submit
            const form = document.querySelector('form');
            if (form) { form.submit(); return true; }
            return false;
        }");
    }
}
