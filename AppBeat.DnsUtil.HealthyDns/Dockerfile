#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine3.16-amd64 AS base

#install terraform
RUN wget https://releases.hashicorp.com/terraform/1.2.8/terraform_1.2.8_linux_amd64.zip
RUN unzip terraform_1.2.8_linux_amd64.zip && rm terraform_1.2.8_linux_amd64.zip
RUN mv terraform /usr/bin/terraform

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AppBeat.DnsUtil.HealthyDns/AppBeat.DnsUtil.HealthyDns.csproj", "AppBeat.DnsUtil.HealthyDns/"]
RUN dotnet restore "AppBeat.DnsUtil.HealthyDns/AppBeat.DnsUtil.HealthyDns.csproj"
COPY . .
WORKDIR "/src/AppBeat.DnsUtil.HealthyDns"
RUN dotnet build "AppBeat.DnsUtil.HealthyDns.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppBeat.DnsUtil.HealthyDns.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppBeat.DnsUtil.HealthyDns.dll"]