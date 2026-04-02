using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace TicketHunter.Core.Services;

public class SoundService
{
    private readonly ILogger<SoundService> _logger;

    private const string TicketFoundSound = "assets/sounds/ticket_found.mp3";
    private const string OrderPlacedSound = "assets/sounds/order_placed.mp3";

    public SoundService(ILogger<SoundService> logger)
    {
        _logger = logger;
    }

    public void PlayTicketFound()
    {
        PlayAsync(TicketFoundSound);
    }

    public void PlayOrderPlaced()
    {
        PlayAsync(OrderPlacedSound);
    }

    private void PlayAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Sound file not found: {Path}", filePath);
            return;
        }

        // Play on background thread to avoid blocking
        Task.Run(() =>
        {
            try
            {
                using var audioFile = new AudioFileReader(filePath);
                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                    Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to play sound: {Path}", filePath);
            }
        });
    }
}
