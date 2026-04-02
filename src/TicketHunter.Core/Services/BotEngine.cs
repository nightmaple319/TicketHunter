using Microsoft.Extensions.Logging;
using TicketHunter.Core.Browser;
using TicketHunter.Core.Models;
using TicketHunter.Core.Platforms;

namespace TicketHunter.Core.Services;

public class BotEngine : IAsyncDisposable
{
    private readonly ILogger<BotEngine> _logger;
    private readonly ConfigService _configService;
    private readonly BrowserManager _browserManager;
    private readonly CloudflareHandler _cloudflareHandler;
    private readonly OcrService _ocrService;
    private readonly NotificationService _notificationService;
    private readonly SoundService _soundService;
    private readonly List<IPlatformHandler> _platformHandlers;

    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public BotStatus Status { get; } = new();

    public BotEngine(
        ILogger<BotEngine> logger,
        ConfigService configService,
        BrowserManager browserManager,
        CloudflareHandler cloudflareHandler,
        OcrService ocrService,
        NotificationService notificationService,
        SoundService soundService,
        IEnumerable<IPlatformHandler> platformHandlers)
    {
        _logger = logger;
        _configService = configService;
        _browserManager = browserManager;
        _cloudflareHandler = cloudflareHandler;
        _ocrService = ocrService;
        _notificationService = notificationService;
        _soundService = soundService;
        _platformHandlers = platformHandlers.ToList();
    }

    public void Start()
    {
        if (Status.State == BotState.Running) return;

        _cts = new CancellationTokenSource();
        Status.State = BotState.Running;
        Status.Message = "Starting...";

        _runTask = Task.Run(() => RunLoopAsync(_cts.Token));
        _logger.LogInformation("Bot started");
    }

    public void Pause()
    {
        Status.State = BotState.Paused;
        Status.Message = "Paused";
        _logger.LogInformation("Bot paused");
    }

    public void Resume()
    {
        if (Status.State == BotState.Paused)
        {
            Status.State = BotState.Running;
            Status.Message = "Resumed";
            _logger.LogInformation("Bot resumed");
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_runTask != null)
        {
            try { await _runTask; } catch (OperationCanceledException) { }
        }

        Status.State = BotState.Idle;
        Status.Message = "Stopped";
        _logger.LogInformation("Bot stopped");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var config = _configService.Config;

        try
        {
            // Initialize OCR with config paths
            if (config.Ocr.Enable)
            {
                _ocrService.SetPaths(config.Ocr.ModelPath, null);
                _ocrService.Initialize();
            }

            // Launch browser
            var page = await _browserManager.LaunchAsync(config);

            // Detect platform and get handler
            var handler = _platformHandlers.FirstOrDefault(h => h.CanHandle(config.Homepage));
            if (handler == null)
            {
                Status.State = BotState.Error;
                Status.Message = $"No handler found for URL: {config.Homepage}";
                _logger.LogError("No platform handler for {Url}", config.Homepage);
                return;
            }

            _logger.LogInformation("Using platform handler: {Platform}", handler.PlatformName);

            // Inject auth cookies
            await handler.InjectAuthAsync(page, config);

            // Check for scheduled start
            await WaitForScheduleAsync(config.Advanced.ScheduleStart, ct);

            // Navigate to homepage
            await _browserManager.NavigateAsync(config.Homepage);
            Status.Message = $"Navigated to {config.Homepage}";

            // Main loop
            while (!ct.IsCancellationRequested)
            {
                // Check pause state
                while (Status.State == BotState.Paused && !ct.IsCancellationRequested)
                    await Task.Delay(500, ct);

                if (ct.IsCancellationRequested) break;

                // Reload config for hot-reload
                config = _configService.Config;
                Status.CurrentUrl = page.Url;
                Status.LastUpdated = DateTime.UtcNow;

                try
                {
                    // Detect page state
                    var pageState = await handler.DetectPageStateAsync(page);

                    switch (pageState)
                    {
                        case PageState.CloudflareChallenge:
                            Status.Message = "Handling Cloudflare challenge...";
                            await _cloudflareHandler.DetectAndHandleAsync(page);
                            break;

                        case PageState.DateSelection:
                            Status.Message = "Selecting date...";
                            if (config.DateAutoSelect.Enable)
                            {
                                var dateSelected = await handler.SelectDateAsync(page,
                                    config.DateAutoSelect.Keyword,
                                    config.DateAutoSelect.Mode);

                                if (!dateSelected)
                                {
                                    Status.Message = "找不到符合的日期，等待後重新整理...";
                                    _logger.LogInformation("No matching date found, will reload in {Interval}s",
                                        config.Advanced.AutoReloadInterval);
                                    await Task.Delay(config.Advanced.AutoReloadInterval * 1000, ct);
                                    await _browserManager.ReloadAsync();
                                }
                            }
                            break;

                        case PageState.AreaSelection:
                            Status.Message = "Selecting area...";
                            if (config.AreaAutoSelect.Enable)
                            {
                                var selected = await handler.SelectAreaAsync(page,
                                    config.AreaAutoSelect.Keyword,
                                    config.AreaAutoSelect.Mode,
                                    config.AreaAutoSelect.KeywordExclude);

                                if (selected)
                                {
                                    if (config.Advanced.PlaySound.Ticket)
                                        _soundService.PlayTicketFound(config.Advanced.PlaySound.Filename);
                                }
                                else
                                {
                                    Status.Message = "找不到符合的區域，等待後重新整理...";
                                    _logger.LogInformation("No matching area found, will reload in {Interval}s",
                                        config.Advanced.AutoReloadInterval);
                                    await Task.Delay(config.Advanced.AutoReloadInterval * 1000, ct);
                                    await _browserManager.ReloadAsync();
                                }
                            }
                            break;

                        case PageState.QuantityAndCaptcha:
                            Status.Message = "正在填寫張數並辨識驗證碼...";
                            await handler.FillQuantityAsync(page, config.TicketNumber);

                            if (config.Ocr.Enable)
                            {
                                var solved = await handler.SolveCaptchaAsync(page, _ocrService);
                                if (!solved)
                                {
                                    Status.State = BotState.WaitingForCaptcha;
                                    Status.Message = "等待手動輸入驗證碼...";
                                }
                            }

                            // 不論是否自動送出，都先勾選同意條款
                            await handler.AcceptTermsAsync(page);

                            if (!config.Advanced.AutoSubmitTicket)
                            {
                                Status.State = BotState.Paused;
                                Status.Message = "已填好張數、驗證碼並勾選同意條款，等待手動確認張數";
                                if (config.Advanced.PlaySound.Ticket)
                                    _soundService.PlayTicketFound(config.Advanced.PlaySound.Filename);
                            }
                            else if (config.Ocr.ForceSubmit || Status.State != BotState.WaitingForCaptcha)
                            {
                                await handler.SubmitOrderAsync(page);
                                Status.State = BotState.Running;
                            }
                            break;

                        case PageState.Verify:
                            Status.Message = "Solving verify question...";
                            var verifySolved = await handler.SolveVerifyQuestionAsync(page);
                            if (!verifySolved)
                            {
                                var q = await page.EvaluateFunctionAsync<string?>(
                                    "() => document.querySelector('.zone-verify')?.innerText?.trim() || null");
                                Status.CaptchaQuestion = q ?? "";
                                Status.State = BotState.WaitingForCaptcha;
                                Status.Message = "Cannot auto-guess, waiting for manual answer...";
                            }
                            break;

                        case PageState.Queue:
                            Status.Message = "In queue, waiting...";
                            break;

                        case PageState.SoldOut:
                            Status.Message = "Sold out, refreshing...";
                            await Task.Delay(config.Advanced.AutoReloadInterval * 1000, ct);
                            await _browserManager.ReloadAsync();
                            break;

                        case PageState.ComingSoon:
                            Status.Message = "Coming soon, auto-refreshing...";
                            await Task.Delay(config.Advanced.AutoReloadInterval * 1000, ct);
                            await _browserManager.ReloadAsync();
                            break;

                        case PageState.OrderComplete:
                            Status.State = BotState.OrderCompleted;
                            Status.Message = "Order completed!";
                            _logger.LogInformation("ORDER COMPLETED!");

                            if (config.Advanced.PlaySound.Order)
                                _soundService.PlayOrderPlaced(config.Advanced.PlaySound.Filename);

                            await _notificationService.NotifyAllAsync(config.Advanced,
                                $"🎉 Order completed!\nURL: {page.Url}");
                            return;

                        default:
                            Status.Message = $"Page state: {pageState}";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in main loop iteration");
                    Status.Message = $"Error: {ex.Message}";
                }

                await Task.Delay(300, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Bot loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in bot loop");
            Status.State = BotState.Error;
            Status.Message = $"Fatal error: {ex.Message}";
        }
    }

    private async Task WaitForScheduleAsync(string scheduleStart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(scheduleStart)) return;

        if (!TimeSpan.TryParse(scheduleStart, out var targetTime)) return;

        var now = DateTime.Now.TimeOfDay;
        if (now >= targetTime) return;

        var waitTime = targetTime - now;
        Status.Message = $"Waiting for scheduled start at {scheduleStart}...";
        _logger.LogInformation("Waiting {WaitTime} until scheduled start at {Time}", waitTime, scheduleStart);

        await Task.Delay(waitTime, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _browserManager.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
