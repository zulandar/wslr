using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace Wslr.App.Controls;

/// <summary>
/// A terminal control using WebView2 and xterm.js for rendering.
/// </summary>
public partial class TerminalControl : UserControl
{
    private bool _isInitialized;
    private bool _isReady;
    private readonly Queue<string> _pendingOutput = new();

    /// <summary>
    /// Raised when user input is received from the terminal.
    /// </summary>
    public event EventHandler<string>? InputReceived;

    /// <summary>
    /// Raised when the terminal is resized.
    /// </summary>
    public event EventHandler<TerminalSizeEventArgs>? Resized;

    /// <summary>
    /// Raised when the terminal is ready to receive input.
    /// </summary>
    public event EventHandler? Ready;

    /// <summary>
    /// Gets whether the terminal is ready to receive input.
    /// </summary>
    public bool IsReady => _isReady;

    public TerminalControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await InitializeWebViewAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to initialize WebView2: {ex.Message}");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        TerminalWebView.CoreWebView2?.Stop();
    }

    private async Task InitializeWebViewAsync()
    {
        // Initialize WebView2
        var env = await CoreWebView2Environment.CreateAsync();
        await TerminalWebView.EnsureCoreWebView2Async(env);

        var coreWebView = TerminalWebView.CoreWebView2;

        // Configure WebView2 settings
        coreWebView.Settings.IsScriptEnabled = true;
        coreWebView.Settings.AreDefaultScriptDialogsEnabled = false;
        coreWebView.Settings.IsStatusBarEnabled = false;
        coreWebView.Settings.AreDevToolsEnabled = false;
        coreWebView.Settings.IsZoomControlEnabled = false;
        coreWebView.Settings.AreDefaultContextMenusEnabled = false;

        // Handle messages from JavaScript
        coreWebView.WebMessageReceived += OnWebMessageReceived;

        // Load the terminal HTML
        var html = GetTerminalHtml();
        coreWebView.NavigateToString(html);

        // Show the WebView
        LoadingPanel.Visibility = Visibility.Collapsed;
        TerminalWebView.Visibility = Visibility.Visible;
    }

    private string GetTerminalHtml()
    {
        // Try to load from embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Wslr.App.Assets.Terminal.terminal.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: try to load from file (for development)
        var assemblyLocation = Path.GetDirectoryName(assembly.Location) ?? ".";
        var filePath = Path.Combine(assemblyLocation, "Assets", "Terminal", "terminal.html");

        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        // Embedded fallback HTML with CDN references
        return GetFallbackHtml();
    }

    private static string GetFallbackHtml()
    {
        return """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/css/xterm.min.css" />
                <style>
                    body { margin: 0; background: #1a1a1a; height: 100vh; }
                    #terminal { height: 100%; width: 100%; }
                </style>
            </head>
            <body>
                <div id="terminal"></div>
                <script src="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/lib/xterm.min.js"></script>
                <script src="https://cdn.jsdelivr.net/npm/@xterm/addon-fit@0.10.0/lib/addon-fit.min.js"></script>
                <script>
                    const theme = {
                        background: '#1a1a1a', foreground: '#e0e0e0', cursor: '#60a5fa',
                        black: '#1a1a1a', red: '#f87171', green: '#4ade80', yellow: '#facc15',
                        blue: '#60a5fa', magenta: '#c084fc', cyan: '#22d3ee', white: '#e0e0e0',
                        brightBlack: '#404040', brightRed: '#fca5a5', brightGreen: '#86efac',
                        brightYellow: '#fde047', brightBlue: '#93c5fd', brightMagenta: '#d8b4fe',
                        brightCyan: '#67e8f9', brightWhite: '#ffffff',
                    };
                    const terminal = new Terminal({
                        theme, fontFamily: 'Cascadia Code, Consolas, monospace',
                        fontSize: 14, cursorBlink: true, scrollback: 10000
                    });
                    const fitAddon = new FitAddon.FitAddon();
                    terminal.loadAddon(fitAddon);
                    terminal.open(document.getElementById('terminal'));
                    setTimeout(() => fitAddon.fit(), 0);
                    new ResizeObserver(() => fitAddon.fit()).observe(document.getElementById('terminal'));
                    terminal.onData(data => {
                        if (window.chrome?.webview) {
                            window.chrome.webview.postMessage(JSON.stringify({ type: 'input', data }));
                        }
                    });
                    if (window.chrome?.webview) {
                        window.chrome.webview.addEventListener('message', e => {
                            const msg = JSON.parse(e.data);
                            if (msg.type === 'output') terminal.write(msg.data);
                            else if (msg.type === 'clear') terminal.clear();
                        });
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'ready', data: { cols: terminal.cols, rows: terminal.rows }
                        }));
                    }
                </script>
            </body>
            </html>
            """;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var type = root.GetProperty("type").GetString();
            var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;

            switch (type)
            {
                case "ready":
                    _isReady = true;
                    FlushPendingOutput();
                    var cols = data.TryGetProperty("cols", out var colsEl) ? colsEl.GetInt32() : 80;
                    var rows = data.TryGetProperty("rows", out var rowsEl) ? rowsEl.GetInt32() : 24;
                    Resized?.Invoke(this, new TerminalSizeEventArgs(cols, rows));
                    Ready?.Invoke(this, EventArgs.Empty);
                    break;

                case "input":
                    var input = data.GetString();
                    if (!string.IsNullOrEmpty(input))
                    {
                        InputReceived?.Invoke(this, input);
                    }
                    break;

                case "binary":
                    var binaryData = data.GetString();
                    if (!string.IsNullOrEmpty(binaryData))
                    {
                        InputReceived?.Invoke(this, binaryData);
                    }
                    break;

                case "resize":
                    var newCols = data.TryGetProperty("cols", out var c) ? c.GetInt32() : 80;
                    var newRows = data.TryGetProperty("rows", out var r) ? r.GetInt32() : 24;
                    Resized?.Invoke(this, new TerminalSizeEventArgs(newCols, newRows));
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing terminal message: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes output to the terminal.
    /// </summary>
    /// <param name="output">The text to write.</param>
    public void WriteOutput(string output)
    {
        if (!_isReady)
        {
            _pendingOutput.Enqueue(output);
            return;
        }

        SendMessage("output", output);
    }

    /// <summary>
    /// Clears the terminal screen.
    /// </summary>
    public void Clear()
    {
        SendMessage("clear", null);
    }

    /// <summary>
    /// Focuses the terminal.
    /// </summary>
    public new void Focus()
    {
        TerminalWebView.Focus();
        SendMessage("focus", null);
    }

    /// <summary>
    /// Resets the terminal to its initial state.
    /// </summary>
    public void Reset()
    {
        SendMessage("reset", null);
    }

    /// <summary>
    /// Scrolls the terminal to the bottom.
    /// </summary>
    public void ScrollToBottom()
    {
        SendMessage("scrollToBottom", null);
    }

    /// <summary>
    /// Sets the terminal font size.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    public void SetFontSize(int size)
    {
        SendMessage("setFontSize", size);
    }

    private void FlushPendingOutput()
    {
        while (_pendingOutput.Count > 0)
        {
            var output = _pendingOutput.Dequeue();
            SendMessage("output", output);
        }
    }

    private void SendMessage(string type, object? data)
    {
        if (TerminalWebView.CoreWebView2 == null) return;

        var message = data != null
            ? JsonSerializer.Serialize(new { type, data })
            : JsonSerializer.Serialize(new { type });

        TerminalWebView.CoreWebView2.PostWebMessageAsJson(message);
    }

    private void ShowError(string message)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        TerminalWebView.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorMessage.Text = message;
    }
}

/// <summary>
/// Event arguments for terminal resize events.
/// </summary>
public class TerminalSizeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Rows { get; }

    public TerminalSizeEventArgs(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }
}
