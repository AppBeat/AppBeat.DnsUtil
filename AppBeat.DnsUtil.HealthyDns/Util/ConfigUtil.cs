using AppBeat.DnsUtil.HealthyDns.Models;

namespace AppBeat.DnsUtil.HealthyDns.Util
{
    internal class ConfigUtil
    {
        public static T GetDnsOptions<T>(IConfiguration config) where T : TerraformDnsRecordOptions, new()
        {
            var options = new T();
            config.Bind(options);
            return options;
        }

        public static string GetDnsSectionName(int i)
        {
            var sectionName = $"DNS[{i}]";
            return sectionName;
        }

        public static IConfigurationSection GetDnsDefinition(IConfiguration config, int i)
        {
            var sectionName = GetDnsSectionName(i);
            var section = config.GetSection(sectionName);
            return section;
        }

        public static string? NormalizeString(string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                return null;
            }

            val = val.Trim(new char[] { '"', '\'', ' ' });
            return val;
        }

        public static bool? StringToBool(string? val)
        {
            val = NormalizeString(val);

            if (string.IsNullOrWhiteSpace(val))
            {
                return null;
            }

            if (string.Compare(val, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return true;
            }

            if (string.Compare(val, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return false;
            }

            throw new Exception($"Unsupported boolean value: '{val}'");
        }

        public static int StringToInt(string? val, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                return defaultValue;
            }

            return EnsureInt32(val);
        }

        public static bool IsValidNumber(string? val, Func<int, bool> isValid)
        {
            if (int.TryParse(val, out var parsedVal))
            {
                return isValid(parsedVal);
            }

            return false;
        }

        public static int EnsureInt32(string? val)
        {
            if (int.TryParse(val, out var parsedInt))
            {
                return parsedInt;
            }

            throw new Exception($"Could not convert value to integer: '{val}'");
        }
    }
}
