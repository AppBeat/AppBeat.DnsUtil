This folder contains:

* .NET 6 source code for service which periodically checks your destination IP addresses
    * Sends HTTP GET request to http[s]://subdomain.example.com/ for each IP address and checks if it receives HTTP status code 2xx
* Dockerfile definition which packs .NET 6 periodic service with Terraform
* docker-compose.yml with example how to set environment variables