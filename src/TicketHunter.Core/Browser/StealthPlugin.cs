using PuppeteerSharp;

namespace TicketHunter.Core.Browser;

public static class StealthPlugin
{
    public static async Task ApplyAsync(IPage page)
    {
        await page.EvaluateFunctionOnNewDocumentAsync(@"() => {
            // Override navigator.webdriver
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });

            // Override navigator.plugins (make it look like a real browser)
            Object.defineProperty(navigator, 'plugins', {
                get: () => [1, 2, 3, 4, 5]
            });

            // Override navigator.languages
            Object.defineProperty(navigator, 'languages', {
                get: () => ['zh-TW', 'zh', 'en-US', 'en']
            });

            // Override chrome.runtime to avoid detection
            window.chrome = { runtime: {} };

            // Override permissions query
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) =>
                parameters.name === 'notifications'
                    ? Promise.resolve({ state: Notification.permission })
                    : originalQuery(parameters);

            // Prevent WebGL fingerprinting detection
            const getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                if (parameter === 37445) return 'Intel Inc.';
                if (parameter === 37446) return 'Intel Iris OpenGL Engine';
                return getParameter.call(this, parameter);
            };

            // Override User-Agent Client Hints
            if (navigator.userAgentData) {
                Object.defineProperty(navigator, 'userAgentData', {
                    get: () => ({
                        brands: [
                            { brand: 'Chromium', version: '120' },
                            { brand: 'Google Chrome', version: '120' },
                            { brand: 'Not_A Brand', version: '8' }
                        ],
                        mobile: false,
                        platform: 'Windows'
                    })
                });
            }
        }");
    }
}
