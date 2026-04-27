# SqlTrainer Backend (ASP.NET Core + MySQL)

## Követelmények
- .NET SDK 8
- MySQL 8.x (ajánlott 8.4 LTS) + phpMyAdmin opcionális

## 1) Adatbázis és userek létrehozása
Futtasd le MySQL-ben:

```sql
CREATE DATABASE sqltrainer_app CHARACTER SET utf8mb4 COLLATE utf8mb4_hungarian_ci;
CREATE DATABASE sqltrainer_sandbox CHARACTER SET utf8mb4 COLLATE utf8mb4_hungarian_ci;

CREATE USER 'sqltrainer_app'@'%' IDENTIFIED BY 'Strong_App_Pass!';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, DROP
ON sqltrainer_app.* TO 'sqltrainer_app'@'%';

CREATE USER 'sqltrainer_runner'@'%' IDENTIFIED BY 'Strong_Runner_Pass!';
GRANT CREATE, DROP, ALTER, INDEX, SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES
ON sqltrainer_sandbox.* TO 'sqltrainer_runner'@'%';

FLUSH PRIVILEGES;
```

## 2) appsettings.json
Állítsd be a connection stringeket és a Jwt:SigningKey értéket.

## 3) EF migráció
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p SqlTrainer.Api -s SqlTrainer.Api
dotnet ef database update -p SqlTrainer.Api -s SqlTrainer.Api
```

## 4) Futtatás
```bash
dotnet run --project SqlTrainer.Api
```

Swagger: /swagger

## Admin fiók
Regisztrálj, majd MySQL-ben:
```sql
UPDATE Users SET Role = 1 WHERE Email = 'admin@email.hu';
```


## Fontos: dotnet-ef verzió
Használd EF8-hoz:
```bash
dotnet tool uninstall --global dotnet-ef
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet tool install --global dotnet-ef --version 8.0.11
```
