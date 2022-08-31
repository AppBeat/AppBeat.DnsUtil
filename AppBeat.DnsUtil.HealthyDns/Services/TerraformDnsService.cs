using AppBeat.DnsUtil.HealthyDns.Models;
using AppBeat.DnsUtil.HealthyDns.Util;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace AppBeat.DnsUtil.HealthyDns.Services
{
    public class TerraformDnsService
    {
        private readonly TerraformDnsServiceOptions _options;
        private readonly ILogger<TerraformDnsService> _logger;
        private readonly TerraformProviderFactory _terraformDnsWriterFactory;
        private readonly IConfiguration _config;
        private readonly int _numberOfDnsDefinitions;

        public TerraformDnsService(IOptions<TerraformDnsServiceOptions> options,
            ILogger<TerraformDnsService> logger, TerraformProviderFactory terraformDnsWriterFactory, IConfiguration config)
        {
            _options = options.Value;

            _logger = logger;
            _terraformDnsWriterFactory = terraformDnsWriterFactory;
            _config = config;

            for (_numberOfDnsDefinitions = 0; _numberOfDnsDefinitions < Constants.MaxDnsDefinitions; _numberOfDnsDefinitions++)
            {
                //check if section exists in configuration
                var section = ConfigUtil.GetDnsDefinition(_config, _numberOfDnsDefinitions);
                if (section == null || !section.Exists())
                {
                    break;
                }
            }

            _logger.LogInformation($"Found {_numberOfDnsDefinitions} DNS definitions");
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(Constants.TerraformDir))
            {
                Directory.CreateDirectory(Constants.TerraformDir);
            }

            var dictTasksByProvider = StartCheckingDnsDestinations(cancellationToken);
            await CollectDnsDestinationResulsAndWriteTerraformFileAsync(dictTasksByProvider, cancellationToken);

            await RunTerraformAsync("init", cancellationToken);

            if (ConfigUtil.StringToBool(_options.ApplyAndAutoApprove) == true)
            {
                await RunTerraformAsync("apply --auto-approve", cancellationToken);
            }
            else
            {
                //plan only (for diagnostics)
                await RunTerraformAsync("plan", cancellationToken);
            }
        }

        private async Task RunTerraformAsync(string args, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running terraform {args} ...");
            using var proc = System.Diagnostics.Process.Start("terraform", $"-chdir={Constants.TerraformDir} {args}");
            await proc.WaitForExitAsync(cancellationToken);
            _logger.LogInformation($"Running terraform {args} done, exit code was {proc.ExitCode}");
        }

        private Dictionary<string, List<Task<CheckDnsDestinationResult>>> StartCheckingDnsDestinations(CancellationToken cancellationToken)
        {
            var dictTasksByProvider = new Dictionary<string, List<Task<CheckDnsDestinationResult>>>();

            for (var i = 0; i < _numberOfDnsDefinitions; i++)
            {
                try
                {
                    var section = ConfigUtil.GetDnsDefinition(_config, i);
                    var provider = ConfigUtil.NormalizeString(section[nameof(TerraformDnsRecordOptions.Provider)]);

                    if (string.IsNullOrWhiteSpace(provider))
                    {
                        _logger.LogError($"Could not get {nameof(TerraformDnsRecordOptions.Provider)} for {ConfigUtil.GetDnsSectionName(i)}");
                        continue;
                    }

                    List<Task<CheckDnsDestinationResult>>? lstTasks;
                    if (!dictTasksByProvider.TryGetValue(provider, out lstTasks))
                    {
                        lstTasks = new List<Task<CheckDnsDestinationResult>>();
                        dictTasksByProvider.Add(provider, lstTasks);
                    }

                    var id = i;
                    lstTasks.Add(Task.Run(async () => await CheckDnsDestinationsAsync(id, section, cancellationToken)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unhandled exception while processing {ConfigUtil.GetDnsSectionName(i)}");
                }
            }

            return dictTasksByProvider;
        }

        private static DnsRecordType GetRecordTypeFromIPAddress(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out var parsedAddress))
            {
                switch (parsedAddress.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        return DnsRecordType.A;

                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        return DnsRecordType.AAAA;

                    default:
                        return DnsRecordType.Unknown;
                }
            }

            return DnsRecordType.Unknown;
        }

        private async Task CollectDnsDestinationResulsAndWriteTerraformFileAsync(Dictionary<string, List<Task<CheckDnsDestinationResult>>> dictTasksByProvider, CancellationToken cancellationToken)
        {
            foreach (var tasksByProvider in dictTasksByProvider)
            {
                var provider = tasksByProvider.Key;
                var impl = _terraformDnsWriterFactory.GetInstance(provider);
                if (impl == null)
                {
                    _logger.LogError($"Could not get instance for provider '{provider}'");
                    continue;
                }

                //wait for tasks to finish
                try
                {
                    await Task.WhenAll(tasksByProvider.Value);

                    using var ms = new MemoryStream(); //we will first write everything in-memory
                    using var terraformDefinition = new StreamWriter(ms);

                    await impl.PrepareDefinitionAsync(terraformDefinition, cancellationToken);
                    foreach (var dnsCheckResult in tasksByProvider.Value) //iterate over each domain / subdomain
                    {
                        var result = await dnsCheckResult;
                        if (result.HealthyIPAddresses?.Any() == true)
                        {
                            //we have at least one healthy check
                            foreach (var healthyIPAddress in result.HealthyIPAddresses)
                            {
                                _logger.LogInformation($"IP {healthyIPAddress} seems healthy for {ConfigUtil.GetDnsSectionName(result.Id)}, preparing in-memory resource ...");
                                await impl.WriteDnsResourceAsync(result.Configuration, healthyIPAddress, GetRecordTypeFromIPAddress(healthyIPAddress), terraformDefinition, cancellationToken);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"No health checks found for {ConfigUtil.GetDnsSectionName(result.Id)}, not writing terraform file");
                        }
                    }

                    //everything seems OK
                    terraformDefinition.Flush();

                    //copy from memory stream to file
                    ms.Seek(0, SeekOrigin.Begin);
                    var terraformFile = Path.Combine(Constants.TerraformDir, $"{provider}.tf");
                    using (var fs = new FileStream(terraformFile, FileMode.Create, FileAccess.Write))
                    {
                        await ms.CopyToAsync(fs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unhandled exception while processing provider '{provider}'");
                }
            }
        }

        private CheckDnsDestinationResult CreateCheckDnsDestinationsError(int id, IConfiguration config, string message)
        {
            _logger.LogError($"{message} for {ConfigUtil.GetDnsSectionName(id)}");
            return new CheckDnsDestinationResult(id, config, null);
        }

        private async Task<CheckDnsDestinationResult> CheckDnsDestinationsAsync(int id, IConfiguration config, CancellationToken cancellationToken)
        {
            try
            {
                var options = ConfigUtil.GetDnsOptions<TerraformDnsRecordOptions>(config);

                //validations
                options.IPAddresses = options.IPAddresses?.Trim();
                options.Domain = options.Domain?.Trim();
                options.Subdomain = options.Subdomain?.Trim();
                options.HealthCheckServiceProtocol = options.HealthCheckServiceProtocol?.Trim();

                if (options.HealthCheckTimeoutSeconds != null && !ConfigUtil.IsValidNumber(options.HealthCheckTimeoutSeconds, val => val > 0))
                {
                    return CreateCheckDnsDestinationsError(id, config, $"Invalid {nameof(TerraformDnsRecordOptions.HealthCheckTimeoutSeconds)}");
                }

                if (options.HealthCheckRetriesOnFailure != null && !ConfigUtil.IsValidNumber(options.HealthCheckRetriesOnFailure, val => val >= 0))
                {
                    return CreateCheckDnsDestinationsError(id, config, $"Invalid {nameof(TerraformDnsRecordOptions.HealthCheckRetriesOnFailure)}");
                }

                if (options.IPAddresses == null || options.IPAddresses.Length == 0)
                {
                    return CreateCheckDnsDestinationsError(id, config, $"No {nameof(TerraformDnsRecordOptions.IPAddresses)}");
                }

                var destinationIPAddresses = options.IPAddresses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < destinationIPAddresses.Length; i++)
                {
                    destinationIPAddresses[i] = destinationIPAddresses[i].Trim();
                }

                //check if we have IPv4 and IPv6 values
                foreach (var destinationIPAddress in destinationIPAddresses)
                {
                    var type = GetRecordTypeFromIPAddress(destinationIPAddress);
                    if (type == DnsRecordType.Unknown)
                    {
                        return CreateCheckDnsDestinationsError(id, config, $"Unsupported IP address: {destinationIPAddress}");
                    }
                }

                var hostnameType = Uri.CheckHostName(options.Domain);
                if (hostnameType != UriHostNameType.Dns)
                {
                    return CreateCheckDnsDestinationsError(id, config, $"Invalid domain detected: {options.Domain}");
                }

                if (string.IsNullOrEmpty(options.Subdomain))
                {
                    return CreateCheckDnsDestinationsError(id, config, $"Empty domain detected for {options.Domain}: '{options.Subdomain}'. Root subdomains must have value {Constants.RootSubdomain}");
                }

                int port;
                bool isHttps;

                switch (options.HealthCheckServiceProtocol)
                {
                    case "https":
                        port = 443;
                        isHttps = true;
                        break;

                    case "http":
                        port = 80;
                        isHttps = false;
                        break;

                    default:
                        return CreateCheckDnsDestinationsError(id, config, $"Unsupported {nameof(TerraformDnsRecordOptions.HealthCheckServiceProtocol)}: '{options.HealthCheckServiceProtocol}'");
                }

                //validation seems OK, check if destination is healthy
                var lstCheckTasks = new List<Task<(bool? isHealthy, string ipAddress)>>();
                foreach (var destinationIPAddress in destinationIPAddresses)
                {
                    lstCheckTasks.Add(Task.Run(async () => await CheckIfHealthyWithAutoRetriesAsync(options, destinationIPAddress.Trim(), port, isHttps, cancellationToken)));
                }

                await Task.WhenAll(lstCheckTasks);

                List<string> lstHealthyIPAddresses = new List<string>();
                foreach (var checkTask in lstCheckTasks)
                {
                    var res = await checkTask;
                    if (res.isHealthy == true)
                    {
                        lstHealthyIPAddresses.Add(res.ipAddress);
                    }
                }

                return new CheckDnsDestinationResult(id, config, lstHealthyIPAddresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception for {ConfigUtil.GetDnsSectionName(id)}");
                return new CheckDnsDestinationResult(id, config, null);
            }
        }

        private async Task<(bool? isHealthy, string ipAddress)> CheckIfHealthyWithAutoRetriesAsync(TerraformDnsRecordOptions options, string ipAddress, int port, bool isHttps, CancellationToken cancellationToken)
        {
            try
            {
                var maxNumOfChecks = 1 + ConfigUtil.StringToInt(options.HealthCheckRetriesOnFailure, 0);

                for (var i = 1; i <= maxNumOfChecks; i++)
                {
                    try
                    {
                        var sbRequestUri = new StringBuilder($"{(isHttps ? "https" : "http")}://");
                        if (options.Subdomain != Constants.RootSubdomain)
                        {
                            sbRequestUri.Append($"{options.Subdomain}.{options.Domain}");
                        }
                        else
                        {
                            sbRequestUri.Append(options.Domain);
                        }
                        
                        sbRequestUri.Append('/');

                        var requestUri = sbRequestUri.ToString();
                        bool ignoreSslIssues = ConfigUtil.StringToBool(options.HealthCheckIgnoreSslIssues) == true;
                        _logger.LogInformation($"Checking destination with IP = {ipAddress}, requestUri = '{requestUri}', port = {port}, ignoreSslIssues = {ignoreSslIssues} ...");
                        var httpStatusCode = await HttpClientUtil.SendAsync(
                            client => {
                                if (options.HealthCheckTimeoutSeconds != null)
                                {
                                    client.Timeout = TimeSpan.FromSeconds(ConfigUtil.EnsureInt32(options.HealthCheckTimeoutSeconds));
                                }
                            },
                            ipAddress, port, requestUri, ignoreSslIssues, cancellationToken);

                        bool isHealthy;
                        if (httpStatusCode >= 200 && httpStatusCode <= 299)
                        {
                            _logger.LogInformation($"Destination with IP = {ipAddress} seems healthy");
                            isHealthy = true;
                        }
                        else
                        {
                            _logger.LogWarning($"Destination with IP = {ipAddress} seems unhealthy, status code was {httpStatusCode}");
                            isHealthy = false;
                        }

                        return (isHealthy, ipAddress);
                    }
                    catch (Exception ex)
                    {
                        if (i < maxNumOfChecks)
                        {
                            _logger.LogWarning(ex, $"Exception while checking IP address {ipAddress}, try = {i}");
                            await Task.Delay(500 * i, cancellationToken);
                        }
                        else
                        {
                            return (null, ipAddress);
                        }
                    }
                }

                return (null, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception while checking IP address {ipAddress}");
                return (null, ipAddress);
            }
        }
    }
}
