#Docker build image
docker build -t appbeat/healthy-dns-util:0.1 -t appbeat/healthy-dns-util -t appbeat/healthy-dns-util:latest -f Dockerfile ..

#Push to Docker Hub
docker image push appbeat/healthy-dns-util:latest
docker push appbeat/healthy-dns-util:latest