FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
# -m:1 keeps MSBuild from spawning one process per core, which is what
# tends to blow past the RAM budget on a small ARM VM.
RUN dotnet restore Fin.Api/Fin.Api.csproj -m:1 \
    && dotnet publish Fin.Api/Fin.Api.csproj -c Release -m:1 -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
# Workstation GC uses far less memory than the default server GC, which
# allocates a heap per CPU core - not worth it for a small container.
ENV DOTNET_gcServer=0
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fin.Api.dll"]
