using System.Net;
using System.Text.Json;
using ClaudeAlert.Core;

namespace ClaudeAlert.EventSources;

public class HookHttpListener : IClaudeEventSource
{
    private readonly int _port;
    private HttpListener? _listener;

    public event Action<ClaudeEvent>? OnEvent;

    public HookHttpListener(int port)
    {
        _port = port;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequestAsync(context);
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/health")
            {
                response.StatusCode = 200;
                var bytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                await response.OutputStream.WriteAsync(bytes);
            }
            else if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/event")
            {
                using var reader = new System.IO.StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync();

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var type = doc.RootElement.GetProperty("type").GetString();
                    if (!string.IsNullOrEmpty(type))
                    {
                        OnEvent?.Invoke(ClaudeEvent.Now(type));
                    }
                }
                catch { }

                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 404;
            }

            response.Close();
        }
        catch { }
    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
    }
}
