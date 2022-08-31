namespace AppBeat.DnsUtil.HealthyDns.Models
{
    public class CheckDnsDestinationResult
    {
        public CheckDnsDestinationResult(int id, IConfiguration config, IEnumerable<string>? healthyIPAddresses)
        {
            Id = id;
            Configuration = config;
            HealthyIPAddresses = healthyIPAddresses;
        }

        public int Id { get; }
        public IConfiguration Configuration { get; }
        public IEnumerable<string>? HealthyIPAddresses { get; }
    }
}
