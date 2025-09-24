# Dockerfile para aplicação .NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["Fin.Api/Fin.Api.csproj", "Fin.Api/"]
COPY ["Fin.Application/Fin.Application.csproj", "Fin.Application/"]
COPY ["Fin.Domain/Fin.Domain.csproj", "Fin.Domain/"]
COPY ["Fin.Infrastructure/Fin.Infrastructure.csproj", "Fin.Infrastructure/"]
COPY ["Fin-Backend.sln", "."]

# Restaurar dependências
RUN dotnet restore "Fin.Api/Fin.Api.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/Fin.Api"
RUN dotnet build "Fin.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Fin.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Instalar dotnet-ef para migrations
RUN dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "Fin.Api.dll"]
