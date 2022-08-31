# AppBeat.DnsUtil (Terraform your Cloudflare DNS with healthy IPs)

This Docker image tries to ensure that your [Cloudflare](https://www.cloudflare.com/) domain always has DNS records with healthy destination IPs.

This is done by .NET 6 service which periodically checks your destinations, prepares Terraform file accordingly and applies changes with Cloudflare plugin.

> :warning: **Use at your own risk**: This is early version of tool. Ideally use it initially with test domains. Please report any issues here.

All configuration is provided via environment variables and for this reason it is easier to use with docker-compose files.

## docker-compose.yml example
* [docker-compose.yml](AppBeat.DnsUtil.HealthyDns/docker-compose.yml)

## Dockerfile definition
* [Dockerfile](AppBeat.DnsUtil.HealthyDns/Dockerfile)
