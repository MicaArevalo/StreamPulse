FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/StreamPulse.Processor/*.csproj ./src/StreamPulse.Processor/
RUN dotnet restore src/StreamPulse.Processor/StreamPulse.Processor.csproj
COPY src/StreamPulse.Processor/ ./src/StreamPulse.Processor/
RUN dotnet publish src/StreamPulse.Processor/StreamPulse.Processor.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "StreamPulse.Processor.dll"]
