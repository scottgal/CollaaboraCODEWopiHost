FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_14.x | bash - \
    && apt-get install -y \
        nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["src/WopiHost.Web/WopiHost.Web.csproj", "src/WopiHost.Web/"]
COPY ["src/WopiHost.FileSystemProvider/WopiHost.FileSystemProvider.csproj", "src/WopiHost.FileSystemProvider/"]
COPY ["src/WopiHost.Abstractions/WopiHost.Abstractions.csproj", "src/WopiHost.Abstractions/"]
COPY ["src/WopiHost.Url/WopiHost.Url.csproj", "src/WopiHost.Url/"]
COPY ["src/WopiHost.Discovery/WopiHost.Discovery.csproj", "src/WopiHost.Discovery/"]
RUN dotnet restore "src/WopiHost.Web/WopiHost.Web.csproj"
COPY . .
WORKDIR "/src/src/WopiHost.Web"
RUN dotnet build "WopiHost.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WopiHost.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WopiHost.Web.dll"]
