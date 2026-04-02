using Microsoft.Extensions.Logging;

namespace TicketHunter.Core.Utils;

public static class RetryHelper
{
    public static async Task<T?> ExecuteAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        int delayMs = 500,
        ILogger? logger = null)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Retry {Attempt}/{Max} failed", i + 1, maxRetries);
                if (i < maxRetries - 1)
                    await Task.Delay(delayMs * (i + 1));
            }
        }
        return default;
    }

    public static async Task<bool> ExecuteAsync(
        Func<Task<bool>> action,
        int maxRetries = 3,
        int delayMs = 500,
        ILogger? logger = null)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (await action()) return true;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Retry {Attempt}/{Max} failed", i + 1, maxRetries);
            }

            if (i < maxRetries - 1)
                await Task.Delay(delayMs * (i + 1));
        }
        return false;
    }
}
