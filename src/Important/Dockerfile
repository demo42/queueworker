ARG REGISTRY_NAME
FROM ${REGISTRY_NAME}baseimages/microsoft/aspnetcore/runtime:linux-2.1 AS base
WORKDIR /app
ARG REGISTRY_NAME
ENV REGISTRY_NAME=${REGISTRY_NAME}


FROM ${REGISTRY_NAME}baseimages/microsoft/dotnet/sdk:linux-2.1 AS build
WORKDIR /src
COPY src/Important/Important.csproj src/Important/
RUN dotnet restore src/Important/Important.csproj
COPY . .
WORKDIR /src/src/Important
RUN dotnet build Important.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Important.csproj -c Release -o /app

#FROM build AS test


FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV VERSION=0.0.25
ENTRYPOINT ["dotnet", "Important.dll"]
