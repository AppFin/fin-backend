[🇧🇷 Em portugês](./assets/DOCKER_pt-br.md)


# Docker Compose - Fin Backend Infrastructure

## How to run PostgreSQL and Redis databases

1. **Start the services (PostgreSQL + Redis):**
   ```
   docker compose up -d
   ```
   This will create the following containers:

   **PostgreSQL:**
    - Container: `fin_app`
    - Database: `fin_app`
    - User: `fin_app`
    - Password: `fin_app`
    - Port: `5432`

   **Redis:**
    - Container: `fin_redis`
    - Port: `6379`
    - Version: Redis 7 (Alpine)

   **Check if the containers are running:**
    ```
   docker ps -a
   ```

2. **Configure the connection strings in your `appsettings.json`:**
   **To run the application locally:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=fin_app;Username=fin_app;Password=fin_app"
   },
   "ApiSettings": {
     "Redis": "localhost:6379"
   }
   ```

3. **Verify everything is ok:**
   ```
   dotnet build
   ```

4. **Start the application:**
   ```
   dotnet run --project .\Fin.Api\
   ```
---
## Notes

### PostgreSQL
- The PostgreSQL container can be accessed via any PostgreSQL client on port `5432`.
- Entity Framework Core migrations create all necessary tables for all project domains.

### Redis
- Redis is used for caching and sessions in the project.
- Default port: `6379`
- Redis data is persisted in the `redis_data` volume
- Health check configured to verify if the service is active

### Development
- The `dotnet build` command checks if the project compiles correctly.
- The `dotnet run` command starts the API.
- Make sure both PostgreSQL and Redis are running before starting the application.