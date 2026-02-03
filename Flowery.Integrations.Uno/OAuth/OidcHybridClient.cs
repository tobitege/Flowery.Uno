using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Windows.Security.Authentication.Web;

namespace Flowery.Integrations.Uno.OAuth;

public sealed class OidcHybridClient
{
    private readonly OidcClientOptions _options;
    private OidcClient? _client;
    private AuthorizeState? _loginState;
    private bool _forceLoopback;

    public OidcHybridClient(OidcClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (!UseWebAuthenticationBroker())
        {
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(_options.RedirectUri))
            {
                _options.RedirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;
            }
            if (string.IsNullOrWhiteSpace(_options.PostLogoutRedirectUri))
            {
                _options.PostLogoutRedirectUri = _options.RedirectUri;
            }

            _client ??= new OidcClient(_options);
            _loginState = await _client.PrepareLoginAsync(cancellationToken: cancellation);
        }
        catch (Exception ex) when (ex is NotImplementedException or COMException)
        {
            _forceLoopback = true;
            _loginState = null;
        }
    }

    public async Task<LoginResult> LoginAsync(CancellationToken cancellation = default)
    {
        if (UseWebAuthenticationBroker())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.RedirectUri))
                {
                    _options.RedirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;
                }

                if (string.IsNullOrWhiteSpace(_options.PostLogoutRedirectUri))
                {
                    _options.PostLogoutRedirectUri = _options.RedirectUri;
                }

                _client ??= new OidcClient(_options);
                _loginState ??= await _client.PrepareLoginAsync(cancellationToken: cancellation);

                var startUri = new Uri(_loginState.StartUrl);
                var brokerResult = await WebAuthenticationBroker.AuthenticateAsync(
                    WebAuthenticationOptions.None,
                    startUri);

                if (brokerResult.ResponseStatus != WebAuthenticationStatus.Success)
                {
                    return new LoginResult(brokerResult.ResponseStatus.ToString(), null);
                }

                return await _client.ProcessResponseAsync(brokerResult.ResponseData, _loginState);
            }
            catch (Exception ex) when (ex is NotImplementedException or COMException)
            {
                _forceLoopback = true;
                _loginState = null;
                return await LoginLoopbackAsync(cancellation);
            }
        }

        return await LoginLoopbackAsync(cancellation);
    }

    private bool UseWebAuthenticationBroker()
    {
        if (_forceLoopback)
        {
            return false;
        }

#if WINDOWS || __SKIA__
        return false;
#else
        return true;
#endif
    }

    private async Task<LoginResult> LoginLoopbackAsync(CancellationToken cancellation)
    {
        var loopbackOptions = EnsureLoopbackOptions();
        var loopbackClient = new OidcClient(loopbackOptions);
        var loginRequest = new LoginRequest
        {
            FrontChannelExtraParameters = new Parameters
            {
                { "prompt", "login" }
            }
        };
        return await loopbackClient.LoginAsync(loginRequest, cancellationToken: cancellation);
    }

    private OidcClientOptions EnsureLoopbackOptions()
    {
#if WINDOWS || __SKIA__
        if (_options.Browser == null)
        {
            _options.Browser = new LoopbackBrowser();
        }

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            _options.RedirectUri = LoopbackBrowser.GetRedirectUri(7890);
        }

        if (string.IsNullOrWhiteSpace(_options.PostLogoutRedirectUri))
        {
            _options.PostLogoutRedirectUri = _options.RedirectUri;
        }

        if (_options.BrowserTimeout != TimeSpan.FromSeconds(30))
        {
            _options.BrowserTimeout = TimeSpan.FromSeconds(30);
        }

        return _options;
#else
        return _options;
#endif
    }

#if WINDOWS || __SKIA__
    private sealed class LoopbackBrowser : IBrowser
    {
        private const string ResponseHtml = "<html><head><title>Flowery</title></head><body>You can return to the app.</body></html>";

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var endUrl = options.EndUrl;
            if (string.IsNullOrWhiteSpace(endUrl))
            {
                return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Missing redirect URL." };
            }

            var listener = new HttpListener();
            try
            {
                listener.IgnoreWriteExceptions = true;
                listener.Prefixes.Add(endUrl);
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    return new BrowserResult
                    {
                        ResultType = BrowserResultType.UnknownError,
                        Error = $"Failed to start loopback listener: {ex.Message}"
                    };
                }

                OpenBrowser(options.StartUrl);

                var timeout = options.Timeout;
                if (timeout <= TimeSpan.Zero)
                {
                    timeout = TimeSpan.FromMinutes(5);
                }

                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().WaitAsync(timeout);
                }
                catch (TimeoutException)
                {
                    return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = "Timed out waiting for the browser response." };
                }
                catch (Exception ex)
                {
                    return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
                }
                var request = context.Request;
                var response = context.Response;
                var resultUrl = request.Url?.AbsoluteUri;

                var buffer = Encoding.UTF8.GetBytes(ResponseHtml);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.KeepAlive = false;
                response.ContentType = "text/html; charset=utf-8";
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["Pragma"] = "no-cache";
                response.Headers["Connection"] = "close";
                try
                {
                    response.Close(buffer, willBlock: false);
                }
                catch (ObjectDisposedException)
                {
                }
                catch
                {
                }
                TryDrainExtraRequests(listener, buffer, cancellationToken);
                return new BrowserResult
                {
                    ResultType = BrowserResultType.Success,
                    Response = resultUrl
                };
            }
            finally
            {
                try
                {
                    listener.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoopbackBrowser listener close failed: {ex.Message}");
                }
            }
        }

        public static string GetRedirectUri(int port = 0)
        {
            if (port == 0)
            {
                port = GetRandomUnusedPort();
            }

            return $"http://127.0.0.1:{port}/";
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoopbackBrowser failed to launch browser: {ex.Message}");
            }
        }

        private static void TryDrainExtraRequests(HttpListener listener, byte[] responseBuffer, CancellationToken token)
        {
            // Some browsers issue a follow-up request (e.g., favicon) after the callback.
            // Keep the listener alive briefly to avoid ERR_CONNECTION_REFUSED.
            const int maxExtraRequests = 1;
            var remaining = maxExtraRequests;
            while (remaining-- > 0)
            {
                try
                {
                    var contextTask = listener.GetContextAsync();
                    var context = contextTask.WaitAsync(TimeSpan.FromSeconds(1), token).GetAwaiter().GetResult();
                    var response = context.Response;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.StatusDescription = "OK";
                    response.KeepAlive = false;
                    response.ContentType = "text/html; charset=utf-8";
                    response.Headers["Cache-Control"] = "no-store";
                    response.Headers["Pragma"] = "no-cache";
                    response.Headers["Connection"] = "close";
                    response.Close(responseBuffer, willBlock: false);
                }
                catch
                {
                    // Ignore any follow-up failures.
                    break;
                }
            }
        }
    }
#endif
}
