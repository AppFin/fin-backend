# Docker Compose - Fin Backend Infrastructure

## Como rodar o banco de dados PostgreSQL e Redis

1. **Suba os serviços (PostgreSQL + Redis):**
   ```
   docker compose up -d
   ```
   Isso irá criar os seguintes containers:

   **PostgreSQL:**
   - Container: `fin_app`
   - Banco: `fin_app`
   - Usuário: `fin_app`
   - Senha: `fin_app`
   - Porta: `5432`

   **Redis:**
   - Container: `fin_redis`
   - Porta: `6379`
   - Versão: Redis 7 (Alpine)

   **Verificar se os containers estão rodando:**
    ```
   docker ps -a
   ```

2. **Configure as connection strings no seu `appsettings.json`:**
   **Para rodar a aplicação localmente:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=fin_app;Username=fin_app;Password=fin_app"
   },
   "ApiSettings": {
     "Redis": "localhost:6379"
   }
   ```

3. **Rode as migrations para criar as tabelas:**
   ```
   dotnet ef database update --project .\Fin.Infrastructure\
   ```
   > Se necessário, instale o pacote de ferramentas:
   > ```
   > dotnet tool install --global dotnet-ef
   > ```

4. **Verifique se tudo está ok:**
   ```
   dotnet build
   ```

5. **Inicie a aplicação:**
   ```
   dotnet run --project .\Fin.Api\
   ```
---
## Observações

### PostgreSQL
- O container do PostgreSQL pode ser acessado via qualquer cliente PostgreSQL na porta `5432`.
- As migrations do Entity Framework Core criam todas as tabelas necessárias para todos os domínios do projeto.

### Redis  
- O Redis é usado para cache e sessões no projeto.
- Porta padrão: `6379`
- Os dados do Redis são persistidos no volume `redis_data`
- Health check configurado para verificar se o serviço está ativo

### Desenvolvimento
- O comando `dotnet build` verifica se o projeto está compilando corretamente.
- O comando `dotnet run` inicia a API.
- Certifique-se de que tanto PostgreSQL quanto Redis estejam rodando antes de iniciar a aplicação.

**Se já possui o banco e Redis configurados, utilize suas variáveis de ambiente em um `.env`:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
   },
   "ApiSettings": {
     "Redis": "${REDIS_HOST}:${REDIS_PORT}"
   }
   ```

   **Exemplo de arquivo `.env`:**
   ```env
   POSTGRES_HOST=localhost
   POSTGRES_PORT=5432
   POSTGRES_DB=fin_app
   POSTGRES_USER=fin_app
   POSTGRES_PASSWORD=fin_app
   REDIS_HOST=localhost
   REDIS_PORT=6379
   ```

---
## Comandos úteis

**Para parar os serviços:**
```
docker compose down
```

**Para parar e remover volumes (CUIDADO: apaga os dados):**
```
docker compose down -v
```

**Para ver os logs dos containers:**
```
docker compose logs -f
```

**Para conectar diretamente ao PostgreSQL:**
```
docker exec -it fin_app psql -U fin_app -d fin_app
```

**Para conectar diretamente ao Redis:**
```
docker exec -it fin_redis redis-cli
```