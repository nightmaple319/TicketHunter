# CLAUDE.md — TicketHunter

## Project Overview

C# .NET 8 automated ticket-purchasing tool targeting **18 platforms** including Tixcraft, KKTIX, iBon, TicketPlus, KHAM, and more.
Inspired by [bouob/tickets_hunter](https://github.com/bouob/tickets_hunter) (Python).

## Solution Structure

```
TicketHunter.sln
├── src/
│   ├── TicketHunter.Core/            # Core logic library (.NET 8 class library)
│   │   ├── Browser/                  # PuppeteerSharp automation layer
│   │   │   ├── BrowserManager.cs     # Chrome lifecycle, launch args, stealth, navigate/reload
│   │   │   ├── CloudflareHandler.cs  # Turnstile detection (3 layers) + 3 click strategies
│   │   │   ├── CookieManager.cs      # Platform-specific cookie injection
│   │   │   └── StealthPlugin.cs      # Anti-detection JS (webdriver, plugins, WebGL, UA)
│   │   ├── Models/
│   │   │   ├── AppConfig.cs          # Root config + all sub-configs (see Config Format below)
│   │   │   ├── BotStatus.cs          # BotState enum (Idle/Running/Paused/WaitingForCaptcha/OrderCompleted/Error) + BotStatus POCO
│   │   │   ├── PageState.cs          # Page state enum (19 states)
│   │   │   ├── PlatformType.cs       # Platform enum (18 platforms)
│   │   │   └── TicketInfo.cs         # Simple ticket data record
│   │   ├── Platforms/
│   │   │   ├── IPlatformHandler.cs   # Interface: CanHandle, InjectAuth, DetectPageState, SelectDate/Area, FillQuantity, SolveCaptcha, SolveVerify, AcceptTerms, SubmitOrder
│   │   │   ├── BasePlatformHandler.cs # Abstract base with shared JS helpers (GetOptionTexts, ClickElementByText, WaitForSelectorSafe)
│   │   │   ├── TixcraftHandler.cs    # Tixcraft full flow (380 lines) — cookie auth, detail→game redirect, date/area/ticket/verify/order
│   │   │   └── TicketmasterHandler.cs # Ticketmaster flow (213 lines) — queue detection, reCAPTCHA left manual
│   │   ├── Services/
│   │   │   ├── BotEngine.cs          # Main async loop: schedule wait → browser launch → platform dispatch → state machine
│   │   │   ├── ConfigService.cs      # JSON settings.json read/write, FileSystemWatcher hot-reload (200ms debounce, thread-safe)
│   │   │   ├── NotificationService.cs # Discord webhook + Telegram Bot API, parallel dispatch
│   │   │   ├── OcrService.cs         # ONNX Runtime inference, CTC greedy decode, auto/fixed-dim resize, universal + ddddocr models
│   │   │   └── SoundService.cs       # NAudio playback, separate ticket/order sounds + custom file path
│   │   └── Utils/
│   │       ├── CaptchaGuesser.cs     # 7 patterns: YES/同意, brackets, quotes, (ans:), math, Chinese numeral
│   │       ├── ChineseNumerals.cs    # 一~萬 → int positional conversion
│   │       ├── KeywordMatcher.cs     # Semicolons=OR, spaces=AND, FindAllMatches (with exclude), SelectByMode (top/bottom/center/random)
│   │       └── RetryHelper.cs        # Generic async retry with linear backoff
│   ├── TicketHunter.Web/             # ASP.NET Core Minimal API (SDK: Microsoft.NET.Sdk.Web, OutputType=Library)
│   │   ├── Endpoints/
│   │   │   ├── BotEndpoints.cs       # /api/run, /api/pause, /api/resume, /api/stop, /api/status, /api/reset, /api/question
│   │   │   ├── ConfigEndpoints.cs    # /api/load, /api/save, /api/version
│   │   │   └── ToolEndpoints.cs      # /api/test-discord, /api/test-telegram, /api/ocr
│   │   └── Program.cs               # WebHost.Build() — DI registration, CORS, static files, endpoint mapping
│   └── TicketHunter.Console/         # Entry point (.NET 8 console app)
│       ├── Program.cs                # Parses port arg, builds WebHost, auto-opens browser, runs async
│       ├── assets/                   # ONNX models + charset JSON (copied to output)
│       └── wwwroot/                  # Static files served by ASP.NET Core
│           ├── index.html            # Bootstrap 5 UI — 7 tabs (Basic, Advanced, Accounts, OCR, Contact, Notifications, Platform-specific)
│           ├── js/settings.js        # Config load/save, bot control, status polling (500ms), theme toggle
│           └── css/styles.css        # Minimal utility styles
├── tests/
│   └── TicketHunter.Tests/           # xUnit test project
│       ├── CaptchaGuesserTests.cs    # 16 theory cases covering all 7 patterns
│       ├── ChineseNumeralsTests.cs   # 11 cases (valid + invalid)
│       └── KeywordMatcherTests.cs    # 8+ cases for IsMatch, FindFirstMatch, FindAllMatches, SelectByMode
└── assets/
    └── ocr_models/                   # ONNX models (gitignored, too large)
        ├── custom.onnx               # Self-trained universal model (99%+ accuracy, A-Z a-z 0-9)
        ├── common.onnx               # ddddocr official model
        ├── common_det.onnx           # ddddocr detection model
        ├── common_old.onnx           # ddddocr legacy model
        └── charsets.json             # Character set definition
```

**Startup project**: `TicketHunter.Console` — runs web UI on `http://localhost:16888`

## Build & Run

```bash
dotnet build TicketHunter.sln
dotnet run --project src/TicketHunter.Console
dotnet test tests/TicketHunter.Tests
```

Or open `TicketHunter.sln` in Visual Studio, set `TicketHunter.Console` as startup project, press F5.

## Key Architecture

### Platform Strategy Pattern
- `IPlatformHandler` interface in `Core/Platforms/` — 9 methods per platform
- `PlatformType` enum (18 values): Tixcraft, Ticketmaster, Kktix, IBon, TicketPlus, Kham, NianDai, Udn, FamiTicket, FunOne, FansiGo, Indievox, Cityline, Urbtix, HkTicketing, GalaxyMacau, TicketekAu
- **Implemented handlers**: `TixcraftHandler`, `TicketmasterHandler`
- **Pending handlers**: KKTIX, iBon, TicketPlus, KHAM, NianDai, UDN, FamiTicket, FunOne, FansiGo, Indievox, Cityline, URBTIX, HKTicketing, GalaxyMacau, TicketekAu
- Add new platform: implement `IPlatformHandler`, register in `WebHost.Build()` DI

### PageState Enum (19 states)
`Unknown`, `EventList`, `DateSelection`, `AreaSelection`, `QuantityAndCaptcha`, `SoldOut`, `ComingSoon`, `Queue`, `OrderComplete`, `CloudflareChallenge`, `Verify`, `Login`, `QueueIT`, `Booking`, `SeatSelection`, `EmailVerification`, `Checkout`, `TicketType`, `PopupBlocking`

### Browser Automation
- **PuppeteerSharp** (Chrome CDP) in `Core/Browser/`
- `BrowserManager` — auto-downloads Chromium, launch args (proxy, window size, no-sandbox), `IAsyncDisposable`
- `StealthPlugin` — anti-detection JS injection (webdriver, plugins, languages, WebGL, UA Client Hints)
- `CloudflareHandler` — 3-layer detection (iframe, div, body text) + 3-strategy resolution (CDP shadow DOM, bounding-box click, passive wait), max 3 retries
- `CookieManager` — platform detection from URL, per-platform cookie injection

### Tixcraft Flow (fully implemented)
1. `/activity/detail/{id}` → URL rewrite to `/activity/game/{id}` (skips "立即購票" button)
2. `/activity/game/{id}` → auto-select date via `#gameList` table rows
3. `/ticket/area/` → auto-select area with keyword matching + exclude
4. `/ticket/ticket/` → fill quantity + solve image CAPTCHA (4-char, ONNX OCR, 3 retries with refresh)
5. `/ticket/verify/` → solve text question via `CaptchaGuesser` pattern matching
6. Order complete → notifications + sound

### CAPTCHA Handling
- **Image CAPTCHA** (`/ticket/ticket/`): `OcrService` with ONNX Runtime, canvas→Base64→OCR, expects 4-char result, 3 retries
- **Text questions** (`/ticket/verify/`): `CaptchaGuesser` — 7 patterns: YES/同意, bracket text `【】`, quoted text, `(ans:)`, add/subtract/multiply/divide math, Chinese numerals in brackets
- **OCR models**: `custom.onnx` (self-trained universal, default), `common.onnx` (ddddocr), toggle via `ocr.use_universal`

### BotEngine State Machine
Main loop in `RunLoopAsync`: schedule wait → browser launch → platform handler selection → cookie injection → navigate → loop { detect page state → dispatch action → 300ms delay }
- `DateSelection` / `AreaSelection`: auto-reload on failure after `auto_reload_interval` seconds
- `SoldOut` / `ComingSoon`: auto-reload with interval
- `QuantityAndCaptcha`: fill quantity → OCR → accept terms → auto-submit or pause for manual confirm
- `Verify`: auto-guess → fallback to manual with search links
- `OrderComplete`: notification + sound → exit
- Supports pause/resume/stop, hot-reload config each iteration

## Web API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/load` | Load current config |
| POST | `/api/save` | Save config (JSON body) |
| GET | `/api/version` | Get version string |
| POST | `/api/run` | Start bot |
| POST | `/api/pause` | Pause bot |
| POST | `/api/resume` | Resume bot |
| POST | `/api/stop` | Stop bot |
| GET | `/api/status` | Get bot status (state, message, currentUrl, captchaQuestion) |
| POST | `/api/reset` | Reset bot state |
| GET | `/api/question` | Get current captcha question |
| POST | `/api/test-discord` | Send Discord test notification |
| POST | `/api/test-telegram` | Send Telegram test notification |
| POST | `/api/ocr` | OCR test (binary image body) |

## NuGet Packages

### TicketHunter.Core
- `PuppeteerSharp` 24.40.0 — Chrome CDP browser automation
- `Microsoft.ML.OnnxRuntime` 1.24.4 — ONNX model inference for CAPTCHA OCR
- `SixLabors.ImageSharp` 3.1.12 — image processing (resize, grayscale)
- `NAudio` 2.3.0 — MP3/WAV sound playback

### TicketHunter.Tests
- `xunit` 2.5.3 + `xunit.runner.visualstudio` 2.5.3
- `Microsoft.NET.Test.Sdk` 17.8.0
- `coverlet.collector` 6.0.0

## Config Format

`settings.json` — full schema in `Core/Models/AppConfig.cs`:

```jsonc
{
  "homepage": "",                    // Target URL
  "ticket_number": 2,               // Quantity to purchase
  "browser": "chrome",
  "date_auto_select": {
    "enable": true,
    "keyword": "",                   // Semicolons=OR, spaces=AND
    "mode": "from_top",             // from_top | from_bottom | center | random
    "auto_fallback": true           // Fallback to first available when no keyword match
  },
  "area_auto_select": {
    "enable": true,
    "keyword": "",
    "keyword_exclude": "",
    "mode": "from_top",
    "auto_fallback": true
  },
  "ocr": {
    "enable": true,
    "force_submit": false,
    "model_path": "assets/ocr_models/custom.onnx",
    "image_source": "canvas",       // canvas | http
    "beta": false,
    "use_universal": true           // true=self-trained model, false=ddddocr
  },
  "accounts": {
    "tixcraft_sid": "",             // TIXUISID cookie
    "indievox_sid": "",
    "ticketmaster_cookie": "",
    "kktix_account": "", "kktix_password": "",
    "ibon_cookie": "",
    "ticketplus_account": "", "ticketplus_password": "",
    "kham_account": "", "kham_password": "",
    "ticket_account": "", "ticket_password": "",       // NianDai
    "udn_account": "", "udn_password": "",
    "fami_account": "", "fami_password": "",
    "funone_session_cookie": "",
    "fansigo_cookie": "", "fansigo_account": "", "fansigo_password": "",
    "cityline_account": "",
    "urbtix_account": "", "urbtix_password": "",
    "hkticketing_account": "", "hkticketing_password": "",
    "facebook_account": "", "facebook_password": ""
  },
  "contact": {
    "real_name": "", "phone": "", "email": "", "credit_card_prefix": ""
  },
  "advanced": {
    "play_sound": { "ticket": true, "order": true, "filename": "" },
    "headless": false,
    "auto_reload_interval": 3,
    "max_retry": 3,
    "auto_reload_overheat_count": 4,  // Consecutive reloads before cooldown
    "auto_reload_overheat_cd": 1,     // Cooldown seconds
    "reset_browser_interval": 0,      // Auto-restart browser (0=disabled)
    "window_size": "1366,768",
    "hide_some_image": false,         // Block fonts/icons/images for speed
    "block_facebook_network": false,  // Block FB tracking scripts
    "disable_adjacent_seat": false,
    "user_guess_string": "",          // Pre-set captcha answers (semicolon-separated)
    "auto_guess_options": true,
    "discount_code": "",              // Promo code (KKTIX, TicketPlus)
    "auto_submit_ticket": true,
    "schedule_start": "",             // HH:MM:SS
    "idle_keyword": "",               // Pause when page contains this text
    "resume_keyword": "",             // Resume when page contains this text
    "verbose": false,
    "proxy_server": "",
    "web_port": 16888,
    "discord_webhook_url": "",
    "telegram_bot_token": "",
    "telegram_chat_id": ""
  },
  "kktix": {
    "auto_press_next_step": true,
    "auto_fill_ticket_number": true,
    "max_dwell_time": 90              // Seconds before auto-submit
  },
  "tixcraft": {
    "pass_date_is_sold_out": true,
    "auto_reload_coming_soon": true
  }
}
```

## Conventions
- All models in `Core/Models/` with `System.Text.Json` `[JsonPropertyName]` attributes (snake_case JSON keys)
- Platform handlers are pluggable — implement `IPlatformHandler`, register as `AddSingleton<IPlatformHandler, XxxHandler>()` in `WebHost.Build()`
- wwwroot lives in Console project (not Web) for correct static file serving
- Web project SDK is `Microsoft.NET.Sdk.Web` with `OutputType=Library`
- Password fields use `type="password"` in the UI for security
- ONNX model files are gitignored (too large); charset JSON files are tracked
- Sound files (`assets/sounds/`) are gitignored; `SoundService` gracefully skips if file not found
- Config hot-reload: `ConfigService` uses `FileSystemWatcher` with 200ms debounce; `BotEngine` re-reads config each loop iteration
