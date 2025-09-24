# � Fin Backend - Docker Setup

## Como rodar a aplicação completa

### 1. Subir tudo com Docker
```bash
cd Container
docker-compose up -d
```

### 2. Acessar a aplicação
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger

### 3. Rodar migrations (se necessário)
```bash
docker-compose exec fin-api dotnet ef database update --project /src/Fin.Infrastructure
```

## Comandos úteis

```bash
# Ver logs
docker-compose logs -f fin-api

# Parar tudo
docker-compose down

# Rebuild após mudanças no código
docker-compose build fin-api
docker-compose up -d fin-api

# Reset completo (apaga dados do banco)
docker-compose down -v
docker-compose up -d
```

## O que está incluído
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379  
- **API .NET 9**: localhost:5000

Pronto! Funciona em qualquer máquina que tenha Docker, independente da versão do .NET instalada.
