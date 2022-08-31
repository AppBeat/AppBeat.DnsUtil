using AppBeat.DnsUtil.HealthyDns.Util;

namespace AppBeat.DnsUtil.HealthyDns.Models
{
    public class TerraformDnsRecordOptions
    {
        public string? Provider
        {
            get
            {
                return _provider;
            }

            set
            {
                _provider = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _provider;

        public string? IPAddresses
        {
            get
            {
                return _IPAddresses;
            }

            set
            {
                _IPAddresses = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _IPAddresses;

        public string? Domain
        {
            get
            {
                return _domain;
            }

            set
            {
                _domain = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _domain;

        public string? Subdomain
        {
            get
            {
                return _subdomain;
            }

            set
            {
                _subdomain = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _subdomain;

        public string? HealthCheckServiceProtocol
        {
            get
            {
                return _healthCheckServiceProtocol;
            }

            set
            {
                _healthCheckServiceProtocol = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _healthCheckServiceProtocol;

        public string? HealthCheckIgnoreSslIssues { get; set; }

        public string? HealthCheckTimeoutSeconds
        {
            get
            {
                return _healthCheckTimeoutSeconds;
            }

            set
            {
                _healthCheckTimeoutSeconds = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _healthCheckTimeoutSeconds;

        public string? HealthCheckRetriesOnFailure
        {
            get
            {
                return _healthCheckRetriesOnFailure;
            }

            set
            {
                _healthCheckRetriesOnFailure = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _healthCheckRetriesOnFailure;
    }
}
