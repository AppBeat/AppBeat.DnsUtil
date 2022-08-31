using AppBeat.DnsUtil.HealthyDns.Models;
using AppBeat.DnsUtil.HealthyDns.Util;
using Microsoft.Extensions.Options;

namespace AppBeat.DnsUtil.HealthyDns.Services
{
    public class TerraformProviderCloudflare : ITerraformProvider
    {
        private readonly CloudflareOptions _cloudflareOptions;

        public TerraformProviderCloudflare(IOptions<CloudflareOptions> cloudflareOptions)
        {
            _cloudflareOptions = cloudflareOptions.Value;
        }

        public async Task PrepareDefinitionAsync(StreamWriter sw, CancellationToken cancellationToken)
        {
            await sw.WriteAsync(@$"
terraform {{
  required_providers {{
    cloudflare = {{
      source  = ""cloudflare/cloudflare""
      version = ""~> 3.0""
    }}
  }}
}}

provider ""cloudflare"" {{
  api_token = ""{_cloudflareOptions.ApiToken}""
}}
");
        }

        private static string Serialize(DnsRecordType recordType)
        {
            switch (recordType)
            {
                case DnsRecordType.A:
                    return "A";

                case DnsRecordType.AAAA:
                    return "AAAA";

                default:
                    throw new Exception($"Unsupported record type: {recordType}");
            }
        }

        private static string Serialize(bool value)
        {
            return value ? "true" : "false";
        }

        private static string GetResourceName(string? domain, string? subdomain, string ip)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (subdomain == null)
            {
                throw new ArgumentNullException(nameof(subdomain));
            }

            if (subdomain == Constants.RootSubdomain)
            {
                return $"root_{domain}_{ip}".Replace('.', '_');
            }

            return $"subdomain_{subdomain}_{domain}_{ip}".Replace('.', '_');
        }

        private static Exception CreateExceptionForMissingProperty(TerraformDnsRecordCloudflareOptions options, string property)
        {
            return new Exception($"{property} for Cloudflare domain {options.Subdomain}.{options.Domain} is not defined");
        }

        private static Exception CreateExceptionForInvalidPropertyValue(TerraformDnsRecordCloudflareOptions options, string property, string value)
        {
            return new Exception($"{property} for Cloudflare domain {options.Subdomain}.{options.Domain} has invalid value: '{value}'");
        }

        public async Task WriteDnsResourceAsync(IConfiguration config, string healthyIP, DnsRecordType recordType, StreamWriter sw, CancellationToken cancellationToken)
        {
            var options = ConfigUtil.GetDnsOptions<TerraformDnsRecordCloudflareOptions>(config);
            if (options.DnsProxied == null)
            {
                throw CreateExceptionForMissingProperty(options, nameof(TerraformDnsRecordCloudflareOptions.DnsProxied));
            }

            if (string.IsNullOrWhiteSpace(options.ZoneId))
            {
                throw CreateExceptionForMissingProperty(options, nameof(TerraformDnsRecordCloudflareOptions.ZoneId));
            }

            if (string.IsNullOrWhiteSpace(options.DnsProxied))
            {
                throw CreateExceptionForMissingProperty(options, nameof(TerraformDnsRecordCloudflareOptions.DnsProxied));
            }

            var dnsProxied = ConfigUtil.StringToBool(options.DnsProxied);
            if (dnsProxied == null)
            {
                throw CreateExceptionForInvalidPropertyValue(options, nameof(TerraformDnsRecordCloudflareOptions.DnsProxied), options.DnsProxied);
            }

            await sw.WriteAsync(@$"
resource ""cloudflare_record"" ""{GetResourceName(options.Domain, options.Subdomain, healthyIP)}"" {{
  zone_id = ""{options.ZoneId}""
  name    = ""{options.Subdomain}""
  value   = ""{healthyIP}""
  type    = ""{Serialize(recordType)}""
  proxied = {Serialize(dnsProxied.Value)}
  allow_overwrite = true
}}
");

        }
    }
}
