# Docker Compose - Fin Backend Database

## Como rodar o banco de dados PostgreSQL

1. **Suba o banco de dados:**
   ```
   docker compose up -d
   ```
   Isso irá criar um container PostgreSQL com:
   - Banco: `fin_app`
   - Usuário: `fin_app`
   - Senha: `fin_app`
   - Porta: `5432`

   **verificar**
    ```
   docker ps -a
   ```

2. **Configure a connection string no seu `appsettings.json`:**
   **Rodar localmente**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=fin_app;Username=fin_app;Password=fin_app"
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
- O container do banco pode ser acessado via qualquer cliente PostgreSQL na porta 5432.
- As migrations do Entity Framework Core criam todas as tabelas necessárias para todos os domínios do projeto.
- O comando `dotnet build` verifica se o projeto está compilando corretamente.
- O comando `dotnet run` inicia a API.

**se ja possui o banco utilize suas variaveis de ambiente em um .env**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
   }
   ```
   ```
   tem modelo de exemplo .env.example
   usar ou alterar para apenas .env
   como esta localmente pode utilizar

   ```