using AppBeat.DnsUtil.HealthyDns.Util;

namespace AppBeat.DnsUtil.HealthyDns
{
    /// <summary>
    /// Cloudflare specifics.
    /// </summary>
    public class CloudflareOptions
    {
        public string? ApiToken
        {
            get
            {
                return _apiToken;
            }

            set
            {
                _apiToken = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _apiToken;
    }
}
