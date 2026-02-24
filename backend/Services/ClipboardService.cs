using System.Runtime.InteropServices;
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
    private static readonly bool _isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
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
        if (_isMac)
        {
            try { return MacClipboardNative.GetText() ?? ""; }
            catch { return ""; }
        }

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
        if (_isMac)
        {
            try 
            { 
                MacClipboardNative.SetText(text); 
                Console.WriteLine($"[CLIPBOARD] Applied to macOS ({text.Length} chars)");
                return;
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[CLIPBOARD] macOS SetText error: {ex.Message}"); 
            }
        }

        try 
        { 
            _clipboard.SetText(text); 
            Console.WriteLine($"[CLIPBOARD] Applied ({text.Length} chars)");
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[CLIPBOARD] SetText error: {ex.Message}"); 
        }
#endif
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

#if WINDOWS
        // On Windows, use a hidden window to listen for clipboard events instead of polling
        var thread = new Thread(() =>
        {
            try
            {
                using var window = new ClipboardMonitorWindow(token, () =>
                {
                    string current = GetText();
                    if (!string.IsNullOrEmpty(current) && current != _lastText)
                    {
                        _lastText = current;
                        onChange(current);
                    }
                });
                Application.Run(); // Starts a message loop for the hidden window
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIPBOARD] Windows monitor error: {ex.Message}");
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
#else
        Task.Run(async () =>
        {
            Console.WriteLine($"[CLIPBOARD] Monitor started (polling: {intervalMs}ms).");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string current = GetText();
                    if (!string.IsNullOrEmpty(current) && current != _lastText)
                    {
                        Console.WriteLine($"[CLIPBOARD-MAC] Local change detected ({current.Length} chars) — syncing.");
                        _lastText = current;
                        onChange(current);
                    }
                }
                catch { /* ignore transient clipboard access errors */ }

                await Task.Delay(intervalMs, token).ContinueWith(_ => { }); // swallow cancellation
            }
            Console.WriteLine("[CLIPBOARD] Monitor stopped.");
        }, token);
#endif
    }

    /// <summary>Stops the clipboard monitor if running.</summary>
    public void StopMonitoring()
    {
        _monitorCts?.Cancel();
#if WINDOWS
        Application.ExitThread(); // Close the hidden window message loop
#endif
        _monitorCts?.Dispose();
        _monitorCts = null;
    }
}

#if WINDOWS
internal class ClipboardMonitorWindow : Form
{
    private readonly Action _onChanged;
    private readonly CancellationToken _token;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    public ClipboardMonitorWindow(CancellationToken token, Action onChanged)
    {
        _token = token;
        _onChanged = onChanged;
        
        // Hidden window setup
        this.ShowInTaskbar = false;
        this.WindowState = FormWindowState.Minimized;
        this.Visible = false;
        this.FormBorderStyle = FormBorderStyle.None;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        AddClipboardFormatListener(this.Handle);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        RemoveClipboardFormatListener(this.Handle);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            if (!_token.IsCancellationRequested)
            {
                _onChanged();
            }
        }
        base.WndProc(ref m);
    }
}
#endif

#if !WINDOWS
/// <summary>
/// Direct macOS P/Invoke for NSPasteboard.
/// Avoids TextCopy/AppKit threading issues by being extremely explicit.
/// </summary>
internal static class MacClipboardNative
{
    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    static extern IntPtr objc_getClass(string name);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    static extern IntPtr UTF8String(IntPtr nsString);

    private static IntPtr _nsStringClass = objc_getClass("NSString");
    private static IntPtr _nsPasteboardClass = objc_getClass("NSPasteboard");
    private static IntPtr _utf8Type;

    static MacClipboardNative()
    {
        _utf8Type = CreateNSString("public.utf8-plain-text");
    }

    private static IntPtr CreateNSString(string str)
    {
        IntPtr alloc = objc_msgSend(_nsStringClass, sel_registerName("alloc"));
        IntPtr init = sel_registerName("initWithUTF8String:");
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(str + "\0");
        GCHandle handle = GCHandle.Alloc(utf8, GCHandleType.Pinned);
        try
        {
            return objc_msgSend(alloc, init, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private static string? GetStringFromIntPtr(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return null;
        IntPtr utf8Ptr = objc_msgSend(nsString, sel_registerName("UTF8String"));
        if (utf8Ptr == IntPtr.Zero) return null;
        return Marshal.PtrToStringUTF8(utf8Ptr);
    }

    public static string? GetText()
    {
        try
        {
            IntPtr pb = objc_msgSend(_nsPasteboardClass, sel_registerName("generalPasteboard"));
            IntPtr str = objc_msgSend(pb, sel_registerName("stringForType:"), _utf8Type);
            return GetStringFromIntPtr(str);
        }
        catch { return null; }
    }

    public static void SetText(string text)
    {
        try
        {
            IntPtr pb = objc_msgSend(_nsPasteboardClass, sel_registerName("generalPasteboard"));
            objc_msgSend(pb, sel_registerName("clearContents"));
            
            IntPtr nsStr = CreateNSString(text);
            try
            {
                objc_msgSend(pb, sel_registerName("setString:forType:"), nsStr, _utf8Type);
            }
            finally
            {
                objc_msgSend(nsStr, sel_registerName("release"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAC-CLIP] Native set failed: {ex.Message}");
        }
    }
}
#endif
