#if WINDOWS
using System.Windows.Forms;
#else
using TextCopy;
#endif

namespace Nicodemous.Backend.Services;

/// <summary>
/// Cross-platform clipboard read/write with auto-sync monitoring.
/// Monitors the local clipboard every 300ms and calls onChange when new text is detected.
/// SetText tracks what it set to avoid feedback loops (received text won't re-trigger a push).
/// </summary>
public class ClipboardService
{
    private string _lastText = "";
    private CancellationTokenSource? _monitorCts;

#if !WINDOWS
    private readonly IClipboard _clipboard = new Clipboard();
#endif

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Returns the current clipboard text, or empty string if unavailable.</summary>
    public string GetText()
    {
#if WINDOWS
        string result = "";
        var thread = new Thread(() =>
        {
            try
            {
                result = System.Windows.Forms.Clipboard.ContainsText()
                    ? System.Windows.Forms.Clipboard.GetText()
                    : "";
            }
            catch { result = ""; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
#else
        try { return _clipboard.GetText() ?? ""; }
        catch { return ""; }
#endif
    }

    /// <summary>
    /// Writes text to the local clipboard.
    /// Guards against feedback: if this text was already set by us, skips the write
    /// so the monitor won't re-broadcast it back to the sender.
    /// </summary>
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text) || text == _lastText) return;
        _lastText = text; // set BEFORE writing so monitor sees no change

#if WINDOWS
        var thread = new Thread(() =>
        {
            try { System.Windows.Forms.Clipboard.SetText(text); }
            catch (Exception ex) { Console.WriteLine($"[CLIPBOARD] SetText error: {ex.Message}"); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#else
        try 
        { 
            // TextCopy's Clipboard.SetText might have platform-specific quirks as a background service
            _clipboard.SetText(text); 
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[CLIPBOARD] SetText error: {ex.Message}"); 
        }
#endif
        Console.WriteLine($"[CLIPBOARD] Applied ({text.Length} chars)");
    }

    // -----------------------------------------------------------------------
    // Auto-sync monitoring
    // -----------------------------------------------------------------------

    /// <summary>
    /// Starts polling the clipboard every <paramref name="intervalMs"/> ms.
    /// Calls <paramref name="onChange"/> with the new text whenever it changes.
    /// Stops any previous monitor before starting a new one.
    /// </summary>
    public void StartMonitoring(Action<string> onChange, int intervalMs = 300)
    {
        StopMonitoring();
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        Task.Run(async () =>
        {
            Console.WriteLine("[CLIPBOARD] Monitor started.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string current = GetText();
                    if (!string.IsNullOrEmpty(current) && current != _lastText)
                    {
                        _lastText = current;
                        Console.WriteLine($"[CLIPBOARD] Local change detected ({current.Length} chars) — syncing.");
                        onChange(current);
                    }
                }
                catch { /* ignore transient clipboard access errors */ }

                await Task.Delay(intervalMs, token).ContinueWith(_ => { }); // swallow cancellation
            }
            Console.WriteLine("[CLIPBOARD] Monitor stopped.");
        }, token);
    }

    /// <summary>Stops the clipboard monitor if running.</summary>
    public void StopMonitoring()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }
}
