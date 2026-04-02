using TicketHunter.Web;

Console.WriteLine(@"
 _____ _      _        _   _   _             _
|_   _(_) ___| | _____| |_| | | |_   _ _ __ | |_ ___ _ __
  | | | |/ __| |/ / _ \ __| |_| | | | | '_ \| __/ _ \ '__|
  | | | | (__|   <  __/ |_|  _  | |_| | | | | ||  __/ |
  |_| |_|\___|_|\_\___|\__|_| |_|\__,_|_| |_|\__\___|_|
                                                v1.0.0
");

var port = 16888;

// Parse port from args
if (args.Length > 0 && int.TryParse(args[0], out var p) && p is >= 1024 and <= 65535)
    port = p;

Console.WriteLine($"Starting TicketHunter on http://localhost:{port}");
Console.WriteLine("Open the URL above in your browser to configure settings.");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

var app = WebHost.Build(args, port);

// Open browser automatically
_ = Task.Run(async () =>
{
    await Task.Delay(1500);
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = $"http://localhost:{port}",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
    catch { /* ignore if browser can't be opened */ }
});

await app.RunAsync();
