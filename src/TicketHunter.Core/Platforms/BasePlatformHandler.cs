using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using TicketHunter.Core.Models;
using TicketHunter.Core.Services;
using TicketHunter.Core.Utils;

namespace TicketHunter.Core.Platforms;

public abstract class BasePlatformHandler : IPlatformHandler
{
    protected readonly ILogger Logger;

    public abstract string PlatformName { get; }
    public abstract PlatformType PlatformType { get; }
    public abstract bool CanHandle(string url);

    protected BasePlatformHandler(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task InjectAuthAsync(IPage page, AppConfig config);
    public abstract Task<PageState> DetectPageStateAsync(IPage page);
    public abstract Task<bool> SelectDateAsync(IPage page, string keyword, string mode);
    public abstract Task<bool> SelectAreaAsync(IPage page, string keyword, string mode, string excludeKeyword);
    public abstract Task<bool> FillQuantityAsync(IPage page, int quantity);
    public abstract Task<bool> SolveCaptchaAsync(IPage page, OcrService ocrService);
    public virtual Task<bool> SolveVerifyQuestionAsync(IPage page) => Task.FromResult(false);
    public abstract Task<bool> AcceptTermsAsync(IPage page);
    public abstract Task<bool> SubmitOrderAsync(IPage page);

    protected async Task<List<string>> GetOptionTexts(IPage page, string selector)
    {
        return await page.EvaluateFunctionAsync<List<string>>(@"(sel) => {
            return Array.from(document.querySelectorAll(sel)).map(el => el.innerText.trim());
        }", selector);
    }

    protected async Task<bool> ClickElementByText(IPage page, string selector, string text)
    {
        return await page.EvaluateFunctionAsync<bool>(@"(sel, text) => {
            const elements = document.querySelectorAll(sel);
            for (const el of elements) {
                if (el.innerText.trim().includes(text)) {
                    el.click();
                    return true;
                }
            }
            return false;
        }", selector, text);
    }

    protected async Task<bool> WaitForSelectorSafe(IPage page, string selector, int timeoutMs = 3000)
    {
        try
        {
            await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions { Timeout = timeoutMs });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
