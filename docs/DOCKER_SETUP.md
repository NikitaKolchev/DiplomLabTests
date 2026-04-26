# Пошаговая инструкция: запуск проекта через Docker

## Что будем создавать

```
diplomm/
├── src/
│   └── Bmz.LabTests.API/
│       ├── Dockerfile              ← 1. Образ для API
│       └── appsettings.Docker.json  ← 5. Конфиг для Docker
├── frontend/
│   ├── Dockerfile                  ← 2. Образ для фронтенда
│   └── nginx.conf                  ← 2.1 Конфиг nginx
├── docker-compose.yml              ← 3. Оркестрация всех сервисов
└── .dockerignore                   ← 4. Исключения при сборке
```

---

## Шаг 1: Dockerfile для API

**Где:** `d:\diplomm\src\Bmz.LabTests.API\Dockerfile`

**Зачем:** Собирает .NET-приложение в образ Docker.

### Что создаём

```dockerfile
# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов (для восстановления зависимостей)
# Контекст сборки — корень diplomm, пути относительно него
COPY src/Bmz.LabTests.API/Bmz.LabTests.API.csproj Bmz.LabTests.API/
COPY src/Bmz.LabTests.Application/Bmz.LabTests.Application.csproj Bmz.LabTests.Application/
COPY src/Bmz.LabTests.Domain/Bmz.LabTests.Domain.csproj Bmz.LabTests.Domain/
COPY src/Bmz.LabTests.Infrastructure/Bmz.LabTests.Infrastructure.csproj Bmz.LabTests.Infrastructure/

# Восстанавливаем пакеты (кэшируется при неизменных csproj)
RUN dotnet restore Bmz.LabTests.API/Bmz.LabTests.API.csproj

# Копируем весь исходный код
COPY src/ ./

# Собираем
WORKDIR /src/Bmz.LabTests.API
RUN dotnet build -c Release -o /app/build

# Этап 2: Публикация (финальный образ)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS publish
WORKDIR /app

# Копируем результат сборки
COPY --from=build /app/build .

# Порт приложения
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bmz.LabTests.API.dll"]
```

**Смысл:**
- `FROM mcr.microsoft.com/dotnet/sdk:10.0` — образ с SDK для сборки
- `FROM mcr.microsoft.com/dotnet/aspnet:10.0` — образ с рантаймом (меньше размер)
- `AS build` / `AS publish` — многоэтапная сборка (multi-stage), уменьшает итоговый образ
- `COPY --from=build` — берём из образа `build`, не из исходников

---

## Шаг 2: Dockerfile для фронтенда

**Где:** `d:\diplomm\frontend\Dockerfile`

**Зачем:** Собирает React-приложение и отдаёт через nginx.

### Что создаём

```dockerfile
# Этап 1: Сборка
FROM node:22-alpine AS build
WORKDIR /app

# Копируем package.json (и package-lock.json, если есть)
COPY package*.json ./

# Устанавливаем зависимости
RUN npm install

# Копируем исходники
COPY . .

# Собираем (VITE_API_BASE_URL будет подставлен при сборке)
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# Этап 2: Публикация через nginx
FROM nginx:alpine AS publish
COPY --from=build /app/dist /usr/share/nginx/html

# Конфиг nginx: проксировать /api на бэкенд
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Важно:** Нужен `nginx.conf` для проксирования API.

---

## Шаг 3: nginx.conf для фронтенда

**Где:** `d:\diplomm\frontend\nginx.conf`

**Зачем:** nginx отдаёт статику и перенаправляет запросы `/api/*` на API.

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # SPA: все маршруты на index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Проксирование API на бэкенд
    location /api/ {
        proxy_pass http://api:8080/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

**Смысл:** `proxy_pass http://api:8080/api/` — имя `api` будет из `docker-compose` (имя сервиса).

---

## Шаг 4: appsettings для Docker

**Где:** `d:\diplomm\src\Bmz.LabTests.API\appsettings.Docker.json`

**Важно:** Файл должен быть в папке API проекта, чтобы он попал в сборку.

**Зачем:** Строка подключения к SQL Server в контейнере.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=BmzLabTestsDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "Bmz.LabTests",
    "Audience": "Bmz.LabTests.Client",
    "SigningKey": "Docker_STRONG_SECRET_KEY_32_CHARS_MINIMUM",
    "ExpireMinutes": 480
  },
  "Ldap": {
    "Host": "localhost",
    "Port": 389,
    "UseSsl": false,
    "Domain": "BMZ"
  }
}
```

**Важно:** `Server=sqlserver` — имя сервиса из docker-compose.

---

## Шаг 5: docker-compose.yml

**Где:** `d:\diplomm\docker-compose.yml`

**Зачем:** Запускает все сервисы в одной сети.

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "YourStrong@Passw0rd"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q \"SELECT 1\" -b -o /dev/null || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  api:
    build:
      context: .
      dockerfile: src/Bmz.LabTests.API/Dockerfile
    container_name: api
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=BmzLabTestsDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
    ports:
      - "5287:8080"
    depends_on:
      sqlserver:
        condition: service_healthy

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: /api
    container_name: frontend
    ports:
      - "80:80"
    depends_on:
      - api

volumes:
  sqlserver_data:
```

**Смысл:**
- `sqlserver` — SQL Server 2022, порт 1433
- `api` — .NET API, порт 5287 снаружи
- `frontend` — nginx, порт 80
- `depends_on` — API ждёт готовности SQL Server

**Примечание:** В SQL Server 2022 используется `mssql-tools18`. Если healthcheck не стартует, можно убрать его и оставить `depends_on: sqlserver` без `condition`.

---

## Шаг 6: Подключение appsettings.Docker.json

**Где:** `d:\diplomm\src\Bmz.LabTests.API\Program.cs`

**Зачем:** При `ASPNETCORE_ENVIRONMENT=Docker` ASP.NET Core автоматически загружает `appsettings.Docker.json`.

Ничего менять в `Program.cs` не нужно — достаточно создать `appsettings.Docker.json` и задать в docker-compose переменную `ASPNETCORE_ENVIRONMENT: Docker`.

---

## Шаг 7: .dockerignore

**Где:** `d:\diplomm\.dockerignore`

**Зачем:** Не копировать в образ лишние файлы (node_modules, bin, obj и т.д.).

```
**/bin/
**/obj/
**/node_modules/
**/.git/
**/.vs/
**/.vscode/
**/*.user
**/frontend/dist/
frontend/node_modules/
```

---

## Порядок запуска

1. Создай все файлы по инструкции.
2. В корне проекта выполни:

```powershell
cd d:\diplomm
docker-compose up --build
```

3. Открой в браузере: http://localhost

---

## Возможные проблемы

### SQL Server не стартует

- Убедись, что пароль соответствует требованиям: минимум 8 символов, буквы, цифры, спецсимволы.
- На Windows может потребоваться Hyper-V.

### API не подключается к SQL Server

- Проверь, что `Server=sqlserver` (имя сервиса).
- Проверь, что `depends_on` дожидается SQL Server.

### Фронтенд не видит API

- Убедись, что `VITE_API_BASE_URL=/api` (относительный путь).
- nginx должен проксировать `/api` на `http://api:8080/api`.

---

## Упрощённый вариант (без nginx)

Можно запускать только API и SQL Server, а фронтенд — локально:

```yaml
# docker-compose.yml — только api и sqlserver
services:
  sqlserver:
    # ... как выше

  api:
    # ... как выше
```

Запуск фронтенда: `cd frontend && npm run dev` и в `.env` указать `VITE_API_BASE_URL=http://localhost:5287/api`.
