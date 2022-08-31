using AppBeat.DnsUtil.HealthyDns.Util;

namespace AppBeat.DnsUtil.HealthyDns.Models
{
    public class TerraformDnsRecordCloudflareOptions : TerraformDnsRecordOptions
    {
        public string? DnsProxied { get; set; }

        public string? ZoneId
        {
            get
            {
                return _zoneId;
            }

            set
            {
                _zoneId = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _zoneId;
    }
}
