#if WINDOWS
using System.Windows.Forms;
#else
using TextCopy;
#endif

namespace Nicodemous.Backend.Services;

/// <summary>
/// Cross-platform clipboard read/write.
/// Polling has been removed — clipboard is now read on-demand (triggered by Ctrl+C/V interception).
/// </summary>
public class ClipboardService
{
    private string _lastText = "";

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
            try { result = System.Windows.Forms.Clipboard.ContainsText()
                    ? System.Windows.Forms.Clipboard.GetText()
                    : ""; }
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
    /// Skips the write if the text matches what we last set (avoids feedback loops).
    /// </summary>
    public void SetText(string text)
    {
        if (text == _lastText) return;
        _lastText = text;

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
        try { _clipboard.SetText(text); }
        catch (Exception ex) { Console.WriteLine($"[CLIPBOARD] SetText error: {ex.Message}"); }
#endif
        Console.WriteLine($"[CLIPBOARD] Set ({text.Length} chars)");
    }
}
