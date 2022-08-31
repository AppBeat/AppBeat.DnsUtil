# AppBeat.DnsUtil (Terraform your Cloudflare DNS with healthy IPs)

This Docker image tries to ensure that your [Cloudflare](https://www.cloudflare.com/) domain has DNS records with healthy destination IPs. It checks this periodically.

# How it works?

.NET 6 worker service periodically checks your destinations, prepares Terraform file with healthy IP addresses and applies changes with Terraform Cloudflare plugin. All source code is available on [GitHub](https://github.com/AppBeat/AppBeat.DnsUtil/tree/master/AppBeat.DnsUtil.HealthyDns).

# Have questions or suggestions?

Please [contact us](https://www.appbeat.io/contact) or [report issue on GitHub](https://github.com/AppBeat/AppBeat.DnsUtil/issues).

# Disclaimer

This is early version of tool. **Use at your own risk!** Ideally, use it first with test domains to ensure that everything works as expected for you. Please report any issues here.

# Examples

## docker-compose.yml example for example.com, A records for test-subdomain pointing to YOUR_WEB_SERVER_IP_1 and YOUR_WEB_SERVER_IP_2

All configuration is provided via environment variables. You must also provide Cloudflare API Token with Zone.DNS edit permission and Zone ID.

```
version: "3.9"
services:
  healthy-dns-util:
    image: appbeat/healthy-dns-util:latest
    container_name: appbeat-healthy-dns-util
    restart: "no" #should be checked if something goes wrong

    environment:
      - DnsUtil_RunAsService="true" #periodic or one time job
      - DnsUtil_Frequency="1m" #how frequently do we run Terraform. Unit examples: 30s, 1m, 1h

      - DnsUtil_Terraform__ApplyAndAutoApprove="true" #if not set then defaults to false which will run plan only

      - DnsUtil_Cloudflare__ApiToken="YOUR_API_TOKEN" #Cloudlare specific global setting

      - DnsUtil_DNS[0]__Provider="Cloudflare" #currently only Cloudflare is supported
      - DnsUtil_DNS[0]__Domain="example.com"
      - DnsUtil_DNS[0]__Subdomain="test-subdomain"
      - DnsUtil_DNS[0]__IPAddresses="YOUR_WEB_SERVER_IP_1, YOUR_WEB_SERVER_IP_2" #A or AAAA destination addresses
      - DnsUtil_DNS[0]__HealthCheckServiceProtocol="https" #when checking destination addresses for HTTP status 2xx, should we use https or http
      - DnsUtil_DNS[0]__HealthCheckTimeoutSeconds="15" #how much do we wait for check
      - DnsUtil_DNS[0]__HealthCheckRetriesOnFailure="2" #if destination check fails with unhandled exception, should we retry it?
      - DnsUtil_DNS[0]__HealthCheckIgnoreSslIssues="true" #should we ignore SSL issues when checking destinations?
      - DnsUtil_DNS[0]__DnsProxied="true" #Cloudlare specific DNS setting
      - DnsUtil_DNS[0]__ZoneId="YOUR_ZONE_ID" #Cloudlare specific DNS setting
```

* [docker-compose.yml](https://github.com/AppBeat/AppBeat.DnsUtil/blob/master/AppBeat.DnsUtil.HealthyDns/docker-compose.yml)

## Dockerfile definition
* [Dockerfile](https://github.com/AppBeat/AppBeat.DnsUtil/blob/master/AppBeat.DnsUtil.HealthyDns/Dockerfile)
