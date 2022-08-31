using AppBeat.DnsUtil.HealthyDns.Util;

namespace AppBeat.DnsUtil.HealthyDns
{
    public class WorkerOptions
    {
        public string? RunAsService { get; set; }

        public string? Frequency 
        { 
            get
            {
                return _frequency;
            }

            set
            {
                _frequency = ConfigUtil.NormalizeString(value);
            }
        }

        private string? _frequency;
    }
}
