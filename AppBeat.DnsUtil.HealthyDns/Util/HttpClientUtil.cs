using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace AppBeat.DnsUtil.HealthyDns.Util
{
    internal static class HttpClientUtil
    {
        public static async Task<int> SendAsync(Action<HttpClient> configure, string ipAddress, int port, string requestUri, bool ignoreSslIssues, CancellationToken cancellationToken)
        {
            using SocketsHttpHandler handler = new SocketsHttpHandler();

            handler.ConnectCallback = async (ctx, ct) =>
            {
                var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    var ipEndpoint = IPEndPoint.Parse($"{ipAddress}:{port}");
                    await s.ConnectAsync(ipEndpoint, ct);
                    return new NetworkStream(s, ownsSocket: true);
                }
                catch
                {
                    s.Dispose();
                    throw;
                }
            };

            if (ignoreSslIssues)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
            }

            using HttpClient client = new HttpClient(handler, disposeHandler: false);
            configure(client);
            using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var res = await client.SendAsync(req, cancellationToken);
            return (int)res.StatusCode;
        }
    }
}
