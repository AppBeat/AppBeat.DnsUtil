using AppBeat.DnsUtil.HealthyDns.Services;
using AppBeat.DnsUtil.HealthyDns.Util;
using Microsoft.Extensions.Options;

namespace AppBeat.DnsUtil.HealthyDns
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TerraformDnsService _terraformService;
        private readonly WorkerOptions _options;

        public Worker(ILogger<Worker> logger, TerraformDnsService terraformService, IOptions<WorkerOptions> options)
        {
            _logger = logger;
            _terraformService = terraformService;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _terraformService.RunAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Unhandled exception, exiting ...");
                        return;
                    }

                    if (ConfigUtil.StringToBool(_options.RunAsService) == true)
                    {
                        var frequency = EnsureValidServiceFrequency(_options.Frequency);
                        _logger.LogInformation($"Next run will be after {Convert.ToInt32(frequency.TotalSeconds)} seconds ...");
                        await Task.Delay(frequency, stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation($"{nameof(WorkerOptions.RunAsService)} not set to true, exiting after single run ...");
                        return;
                    }
                }

                _logger.LogInformation("Cancellation was requested, exiting service ...");
            }
            catch (TaskCanceledException tce)
            {
                //service was probably stopped
                _logger.LogInformation(tce, $"{nameof(TaskCanceledException)}, service was probably stopped, exiting ...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                throw;
            }
        }

        private static int GetFrequencyDuration(string frequency)
        {
            var duration = frequency.Substring(0, frequency.Length - 1);
            if (int.TryParse(duration, out var parsedDuration) && parsedDuration > 0)
            {
                return parsedDuration;
            }

            throw new Exception($"{nameof(WorkerOptions.Frequency)} is not valid: '{frequency}'");
        }

        private static TimeSpan EnsureValidServiceFrequency(string? frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
            {
                throw new Exception($"{nameof(WorkerOptions.Frequency)} is not set");
            }

            frequency = frequency.Trim();

            if (frequency.EndsWith("s", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromSeconds(GetFrequencyDuration(frequency));
            }

            if (frequency.EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromMinutes(GetFrequencyDuration(frequency));
            }

            if (frequency.EndsWith("h", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromHours(GetFrequencyDuration(frequency));
            }

            throw new Exception($"{nameof(WorkerOptions.Frequency)} is not valid: '{frequency}'");
        }
    }
}