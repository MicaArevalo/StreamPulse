FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/StreamPulse.Api/*.csproj ./src/StreamPulse.Api/
RUN dotnet restore src/StreamPulse.Api/StreamPulse.Api.csproj
COPY src/StreamPulse.Api/ ./src/StreamPulse.Api/
RUN dotnet publish src/StreamPulse.Api/StreamPulse.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "StreamPulse.Api.dll"]
