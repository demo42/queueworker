# Demo 42 - Queue Worker

See [deploy/readme.md](../deploy/readme.md) for an overview of demo42

## Importing the Base Images

## Importing Base Images

```sh
ACR_NAME=demo42t

az acr import \
  -n ${ACR_NAME} \
  --source mcr.microsoft.com/dotnet/core/runtime:2.1.10 \
  --image base-images/dotnet/core/runtime:2.1.10

az acr import \
  -n ${ACR_NAME} \
  --source mcr.microsoft.com/dotnet/core/sdk:2.1 \
  --image base-images/dotnet/core/sdk:2.1

az acr import \
  -n ${ACR_NAME} \
  --source mcr.microsoft.com/dotnet/core/sdk:2.2 \
  --image base-images/dotnet/core/sdk:2.2
```

## Building the image locally

```sh
docker build \
  -t ${ACR_NAME}.azurecr.io/demo42/queueworker:1 \
  -f ./src/Important/Dockerfile \
  --build-arg ACR_NAME=demo42.azurecr.io/ \
  .
docker push demo42.azurecr.io/demo42/queueworker:1

docker build \
  -t demo42westus.azurecr.io/demo42/queueworker:1 \
  -f ./src/Important/Dockerfile \
  --build-arg ACR_NAME=demo42.azurecr.io/ \
  .
docker push ${ACR_NAME}.azurecr.io/demo42/queueworker:1

# Teleport Baseline Test - noop for the entrypoint
docker build \
  -t ${ACR_NAME}.azurecr.io/demo42/queueworker:no-entrypoint \
  -f ./src/Important/Dockerfile-no-entrypoint \
  --build-arg ACR_NAME=demo42.azurecr.io/ \
  .
docker push ${ACR_NAME}.azurecr.io/demo42/queueworker:no-entrypoint

```

## Building the image with ACR Tasks

```sh
az acr build \
  -t demo42/queueworker:{{.Build.ID}} \
  -f ./src/queueworker/Dockerfile \
  --build-arg REGISTRY_NAME=${REGISTRY_NAME}.azurecr.io \
  .
```

## Build, Test, Deploy the image(s) with ACR Tasks

```sh
az acr run -f acr-task.yaml  .
```

## Create an ACR Task

- Basic Build & Push

```sh
az acr task create \
  -n demo42-queueworker \
  --file acr-task.yaml \
  --context https://github.com/demo42/queueworker.git \
  --git-access-token $(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-git-token \
            --query value -o tsv) \
  --registry $ACR_NAME
```

- Task with Build, Push & Helm Deploy to AKS

```sh
az acr task create \
  -n demo42-queueworker \
  --file acr-task.yaml \
  --context https://github.com/demo42/queueworker.git \
  --branch $BRANCH \
  --set-secret TENANT=$(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-serviceaccount-tenant \
            --query value -o tsv) \
  --set-secret SP=$(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-serviceaccount-user \
            --query value -o tsv) \
  --set-secret PASSWORD=$(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-serviceaccount-pwd \
            --query value -o tsv) \
  --set CLUSTER_NAME=demo42-staging-eus \
  --set CLUSTER_RESOURCE_GROUP=demo42-staging-eus \
  --set-secret REGISTRY_USR=$(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-pull-usr \
            --query value -o tsv) \
  --set-secret REGISTRY_PWD=$(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-pull-pwd \
            --query value -o tsv) \
  --git-access-token $(az keyvault secret show \
            --vault-name ${AKV_NAME} \
            --name demo42-git-token \
            --query value -o tsv) \
  --registry $ACR_NAME
```

Run the scheduled task

```sh
az acr task run -n demo42-queueworker
```

Run QueueWorker, Processing Messages

```sh
export STORAGECONNECTIONSTRING="[]"
docker run -it \
  -e StorageConnectionString=$STORAGECONNECTIONSTRING \
  -e LoopDelay=1 \
  -e ExitOnComplete=true \
  demo42westus.azurecr.io/demo42/queueworker:1

docker run -it \
  demo42westus.azurecr.io/demo42/queueworker:1
az acr run -r demo42westus --cmd "{{.Run.Registry}}/demo42/queueworker:1" /dev/null
az acr run -r demo42westus --cmd "{{.Run.Registry}}/demo42/queueworker:1" /dev/null
```

az acr run -r demo42westus --cmd "{{.Run.Registry}}/demo42/queueworker:1" /dev/null

az acr run -r demo42westus \
  --cmd "orca run {{.Run.Registry}}/demo42.azurecr.io/demo42/web:1 -e StorageConnectionString=$STORAGECONNECTIONSTRING -e LoopDelay=1 -e ExitOnComplete=true" /dev/null

az acr run -r demo42westus --cmd "{{.Run.Registry}}/demo42/queueworker:1" /dev/null

