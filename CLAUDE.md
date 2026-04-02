# CLAUDE.md — TicketHunter

## Project Overview

C# .NET 8 automated ticket-purchasing tool targeting **18 platforms** including Tixcraft, KKTIX, iBon, TicketPlus, KHAM, and more.
Inspired by [bouob/tickets_hunter](https://github.com/bouob/tickets_hunter) (Python).

## Solution Structure

```
TicketHunter.sln
├── src/TicketHunter.Core/       # Core logic (models, services, browser, platforms, utils)
├── src/TicketHunter.Web/        # ASP.NET Core Minimal API + Bootstrap 5 settings UI
├── src/TicketHunter.Console/    # Entry point — launches web server + bot engine
└── tests/TicketHunter.Tests/    # xUnit tests
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
- `IPlatformHandler` interface in `Core/Platforms/`
- `PlatformType` enum: Tixcraft, Ticketmaster, Kktix, IBon, TicketPlus, Kham, NianDai, Udn, FamiTicket, FunOne, FansiGo, Indievox, Cityline, Urbtix, HkTicketing, GalaxyMacau, TicketekAu
- `TixcraftHandler` — handles tixcraft.com (cookie auth via TIXUISID, date/area selection, image CAPTCHA + text verify)
- `TicketmasterHandler` — handles ticketmaster.com/sg (cookie auth, queue detection)
- More platform handlers to be implemented

### Browser Automation
- **PuppeteerSharp** (Chrome CDP) in `Core/Browser/`
- `BrowserManager` — lifecycle management, launch with stealth
- `StealthPlugin` — anti-detection JS injection (webdriver, plugins, languages, WebGL)
- `CloudflareHandler` — Turnstile challenge detection + 3 click strategies
- `CookieManager` — cookie injection per platform

### Tixcraft Flow
1. `/activity/detail/{id}` → URL rewrite to `/activity/game/{id}` (skips "立即購票" button)
2. `/activity/game/{id}` → auto-select date via `#gameList` table rows
3. `/ticket/area/` → auto-select area with keyword matching
4. `/ticket/ticket/` → fill quantity + solve image CAPTCHA (4-char, needs ONNX model)
5. `/ticket/verify/` → solve text question via `CaptchaGuesser` pattern matching
6. Order complete → notifications + sound

### CAPTCHA Handling
- **Image CAPTCHA** (`/ticket/ticket/`): `OcrService` with ONNX Runtime, expects 4-char result
- **Text questions** (`/ticket/verify/`): `CaptchaGuesser` in `Core/Utils/` — supports YES/同意, bracket text, Chinese numerals→Arabic, math, (ans:) format
- ONNX model path: `assets/ocr_models/custom.onnx` (self-trained universal model)

### Services (all in `Core/Services/`)
- `ConfigService` — JSON config read/write with FileSystemWatcher hot-reload
- `BotEngine` — main loop (detect page state → execute action → repeat), overheat protection, idle/resume keywords
- `OcrService` — ONNX model inference for image CAPTCHA, universal + ddddocr multi-model support
- `NotificationService` — Discord webhook + Telegram bot
- `SoundService` — NAudio MP3/WAV playback, separate ticket/order sound toggles + custom file

### Utils (`Core/Utils/`)
- `KeywordMatcher` — semicolons=OR, spaces=AND, priority ordering
- `CaptchaGuesser` — text question pattern matching for Tixcraft verify
- `ChineseNumerals` — Chinese numeral → integer conversion
- `RetryHelper` — generic async retry with backoff

### Web UI (`Web/`)
- ASP.NET Core Minimal API endpoints in `Web/Endpoints/`
- Static files in `Console/wwwroot/` (HTML/JS/CSS, Bootstrap 5)
- API: `/api/load`, `/api/save`, `/api/run`, `/api/pause`, `/api/resume`, `/api/status`, `/api/version`
- Status polling every 500ms from frontend
- 7 tabs: Basic, Advanced, Accounts (18 platforms), OCR, Contact, Notifications, Platform-specific

## NuGet Packages
- PuppeteerSharp (browser automation)
- Microsoft.ML.OnnxRuntime (CAPTCHA OCR)
- SixLabors.ImageSharp (image processing)
- NAudio (sound playback)

## Config Format
`settings.json` with sections: `homepage`, `ticket_number`, `date_auto_select`, `area_auto_select`, `ocr`, `accounts`, `contact`, `advanced`, `kktix`, `tixcraft`. See `Core/Models/AppConfig.cs` for full schema.

### Key config features:
- `auto_fallback` on date/area selection (fallback to first available when keyword doesn't match)
- `PlaySoundConfig` with separate ticket/order toggles and custom filename
- `auto_reload_overheat_count/cd` — overheat protection to prevent IP bans
- `user_guess_string` — pre-set captcha answer dictionary
- `discount_code` — auto-fill promo codes (KKTIX, TicketPlus)
- Platform-specific: `KktixConfig` (auto_press_next_step, max_dwell_time), `TixcraftConfig` (pass_sold_out, auto_reload_coming_soon)
- 15+ platform account/cookie fields in `AccountsConfig`

## Conventions
- All models in `Core/Models/` with `System.Text.Json` `[JsonPropertyName]` attributes (snake_case)
- Platform handlers are pluggable — add new platform by implementing `IPlatformHandler`
- wwwroot lives in Console project (not Web) for correct static file serving
- Web project SDK is `Microsoft.NET.Sdk.Web` with `OutputType=Library`
- Password fields use `type="password"` in the UI for security
