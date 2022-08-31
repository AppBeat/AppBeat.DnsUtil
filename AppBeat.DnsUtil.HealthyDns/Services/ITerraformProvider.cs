namespace AppBeat.DnsUtil.HealthyDns.Services
{
    public interface ITerraformProvider
    {
        Task  PrepareDefinitionAsync(StreamWriter sw, CancellationToken cancellationToken);
        Task WriteDnsResourceAsync(IConfiguration config, string healthyIP, DnsRecordType recordType, StreamWriter sw, CancellationToken cancellationToken);
    }
}
