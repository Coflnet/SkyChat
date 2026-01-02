ARG dotnetversion=10.0
FROM mcr.microsoft.com/dotnet/sdk:${dotnetversion} AS build
WORKDIR /build
RUN git clone --depth=1 https://github.com/Coflnet/HypixelSkyblock.git dev
WORKDIR /build/sky
COPY SkyChat.csproj SkyChat.csproj
RUN dotnet restore
COPY . .
RUN dotnet publish -c release -o /artifact

FROM mcr.microsoft.com/dotnet/aspnet:${dotnetversion}
WORKDIR /app

COPY --from=build /artifact .

ENV ASPNETCORE_URLS=http://+:8000

RUN useradd --uid $(shuf -i 2000-65000 -n 1) app-user
USER app-user

ENTRYPOINT ["dotnet", "SkyChat.dll", "--hostBuilder:reloadConfigOnChange=false"]
