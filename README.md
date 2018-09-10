# Demo 42 - Queue Worker

## Building the image locally
```sh
docker build -t demo42/queueworker:dev  -f ./src/queueworker/Dockerfile --build-arg demo42.azurecr.io .
```

## Building the image with ACR Build
```sh
az acr build -t demo42/queueworker:{{.Build.ID}} -f ./src/queueworker/Dockerfile --build-arg REGISTRY_NAME=demo42.azurecr.io .
```