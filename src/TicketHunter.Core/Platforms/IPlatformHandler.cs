using PuppeteerSharp;
using TicketHunter.Core.Models;
using TicketHunter.Core.Services;

namespace TicketHunter.Core.Platforms;

public interface IPlatformHandler
{
    string PlatformName { get; }
    PlatformType PlatformType { get; }
    bool CanHandle(string url);

    Task InjectAuthAsync(IPage page, AppConfig config);
    Task<PageState> DetectPageStateAsync(IPage page);
    Task<bool> SelectDateAsync(IPage page, string keyword, string mode);
    Task<bool> SelectAreaAsync(IPage page, string keyword, string mode, string excludeKeyword);
    Task<bool> FillQuantityAsync(IPage page, int quantity);
    Task<bool> SolveCaptchaAsync(IPage page, OcrService ocrService);
    Task<bool> SolveVerifyQuestionAsync(IPage page);
    Task<bool> AcceptTermsAsync(IPage page);
    Task<bool> SubmitOrderAsync(IPage page);
}
