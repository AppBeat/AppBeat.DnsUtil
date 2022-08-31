namespace AppBeat.DnsUtil.HealthyDns.Services
{
    public class TerraformProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TerraformProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITerraformProvider? GetInstance(string provider)
        {
            if (_providerImplementations.TryGetValue(provider, out var typeName) && typeName != null)
            {
                var instance = ActivatorUtilities.CreateInstance(_serviceProvider, Type.GetType(typeName) ?? throw new Exception($"Could not get type '{typeName}'"));
                return instance as ITerraformProvider;
            }

            return null;
        }

        private readonly Dictionary<string, string> _providerImplementations = new Dictionary<string, string>() {
            { "Cloudflare", "AppBeat.DnsUtil.HealthyDns.Services.TerraformProviderCloudflare, AppBeat.DnsUtil.HealthyDns" }
        };
    }
}
