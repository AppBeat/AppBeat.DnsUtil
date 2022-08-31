using AppBeat.DnsUtil.HealthyDns.Services;

namespace AppBeat.DnsUtil.HealthyDns
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(e => {
                    e.AddEnvironmentVariables(prefix: "DnsUtil_");
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<Worker>()
                        .Configure<WorkerOptions>(e => context.Configuration.Bind(e));

                    services.AddSingleton<TerraformProviderFactory>();

                    var specificsCloudflare = context.Configuration.GetSection("Cloudflare");
                    if (specificsCloudflare?.Exists() == true)
                    {
                        services.Configure<CloudflareOptions>(e => specificsCloudflare.Bind(e));
                    }

                    services.AddSingleton<TerraformDnsService>()
                        .Configure<TerraformDnsServiceOptions>(e => {
                            context.Configuration.GetSection("Terraform").Bind(e);
                        });
                })
                .Build();

            host.Run();
        }
    }
}