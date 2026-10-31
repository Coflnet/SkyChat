ARG dotnetversion=10.0
FROM mcr.microsoft.com/dotnet/sdk:${dotnetversion} AS build
WORKDIR /build
RUN git init dev \
    && git -C dev fetch --depth=1 https://github.com/Coflnet/HypixelSkyblock.git 77c78f9a46d81bc6a01ccda8c81334d3976145ac \
    && git -C dev checkout --detach FETCH_HEAD
WORKDIR /build/sky
COPY SkyChat.csproj SkyChat.csproj
RUN dotnet restore
COPY . .
RUN dotnet test --no-restore
RUN dotnet publish -c release -o /app /p:UseAppHost=false /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:${dotnetversion}-noble-chiseled-extra
WORKDIR /app

COPY --from=build --chown=$APP_UID:$APP_UID /app .

ENV ASPNETCORE_URLS=http://+:8000 \
    DOTNET_EnableDiagnostics=0 \
    COMPlus_EnableDiagnostics=0 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    HOME=/tmp \
    TMPDIR=/tmp

USER $APP_UID

ENTRYPOINT ["dotnet", "SkyChat.dll", "--hostBuilder:reloadConfigOnChange=false"]
