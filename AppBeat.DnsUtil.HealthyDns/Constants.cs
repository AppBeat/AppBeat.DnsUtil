namespace AppBeat.DnsUtil.HealthyDns
{
    internal abstract class Constants
    {
        public const string RootSubdomain = "@";
        public const string TerraformDir = "/data/terraform";
        //private const string TerraformDir = @"C:\Data\terraform";
        public const int MaxDnsDefinitions = 100;
        public const int DefaultHealthCheckTimeoutSeconds = 15;
        public const int DefaultHealthCheckRetriesOnFailure = 0;
    }
}
