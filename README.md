# AppBeat.DnsUtil (Terraform your Cloudflare DNS with healthy IPs)

This Docker image tries to ensure that your [Cloudflare](https://www.cloudflare.com/) domain always has DNS records with healthy destination IPs.

This is done by .NET 6 worker service which periodically checks your destinations, prepares Terraform file accordingly and applies changes with Cloudflare plugin.

Have questions? [Contact us](https://www.appbeat.io/contact) or [report issue on GitHub](https://github.com/AppBeat/AppBeat.DnsUtil/issues).

> :warning: **Use at your own risk**: This is early version of tool. Ideally use it initially with test domains. Please report any issues here.

All configuration is provided via environment variables and for this reason it is easier to use with docker-compose files.

## docker-compose.yml example

```
version: "3.9"
services:
  healthy-dns-util:
    image: appbeat/healthy-dns-util:latest
    container_name: appbeat-healthy-dns-util
    restart: "no" #should be checked if something goes wrong

#    volumes:
#      - /app/publish/linux-x64/:/app

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

* [docker-compose.yml](AppBeat.DnsUtil.HealthyDns/docker-compose.yml)

## Dockerfile definition
* [Dockerfile](AppBeat.DnsUtil.HealthyDns/Dockerfile)
