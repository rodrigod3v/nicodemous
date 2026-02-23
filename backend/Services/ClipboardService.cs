using System.Windows.Forms;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class ClipboardService
{
    private readonly Action<string> _onClipboardChanged;
    private string _lastText = "";

    public ClipboardService(Action<string> onClipboardChanged)
    {
        _onClipboardChanged = onClipboardChanged;
    }

    public void StartMonitoring()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000); // Poll every second for simplicity in this MVP
                CheckClipboard();
            }
        });
    }

    private void CheckClipboard()
    {
        // Must be in STA thread for WinForms Clipboard access
        var thread = new Thread(() =>
        {
            if (Clipboard.ContainsText())
            {
                string currentText = Clipboard.GetText();
                if (currentText != _lastText)
                {
                    _lastText = currentText;
                    var data = new { type = "clipboard", content = currentText };
                    _onClipboardChanged(JsonSerializer.Serialize(data));
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    public void SetClipboard(string text)
    {
        var thread = new Thread(() =>
        {
            Clipboard.SetText(text);
            _lastText = text;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
