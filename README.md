# BMZ Lab Tests System

Backend foundation for the laboratory testing registration system.

## Stack

- ASP.NET Core Web API
- Entity Framework Core (Code First)
- SQL Server
- LDAP + JWT authentication

## Solution structure

- `src/Bmz.LabTests.Domain` - entities and core domain models
- `src/Bmz.LabTests.Application` - use-case services + abstractions (repositories/interfaces)
- `src/Bmz.LabTests.Infrastructure` - EF Core repositories, LDAP, JWT, seed
- `src/Bmz.LabTests.API` - Web API host and controllers

## Implemented in current iteration

- Clean Architecture skeleton with 4 projects.
- Controllers are thin: business logic moved to Application services.
- Repository layer added between API/Application and EF Core.
- Domain entities for users, dictionaries, testing protocol, limits, results.
- `RowVersion` concurrency field on all entities.
- EF Core model configuration and SQL Server context.
- Initial migration and automatic startup migration apply.
- Seed data:
  - Roles: `Admin`, `Engineer`, `Assistant`
  - Countries: Belarus/Russia/Kazakhstan
  - Local fallback user: `local-admin` / `VeryHardPassword`
- LDAP auth service + fallback local admin auth.
- JWT generation with role claims.
- `POST /api/auth/login` endpoint with FluentValidation.
- Administrative/engineering module APIs:
  - `Countries` CRUD
  - `Customers` CRUD
  - `WireCodes` CRUD
  - `Parameters` CRUD
  - `PUT/GET /api/wire-codes/{wireCodeId}/limits` for protocol assembly
    (required parameters + min/max intervals)
- Organization and role management:
  - Admin:
    - create engineers (`POST /api/admin/users/engineers`)
    - create assistants (`POST /api/admin/users/assistants`)
    - create laboratories (`POST /api/admin/laboratories`)
    - assign engineer to laboratory (`PUT /api/admin/laboratories/{id}/engineer`)
    - view laboratories and engineers (`GET /api/admin/laboratories`, `GET /api/admin/users/engineers`)
  - Engineer:
    - create own assistants (`POST /api/engineer/users/assistants`)
    - list/filter assistants (`GET /api/engineer/users/assistants?search=&login=`)
    - edit assistants (`PUT /api/engineer/users/assistants/{id}`)
- Laboratory data isolation:
  - assistants can create/save/complete only their own laboratory tests
  - assistants see only tests of their laboratory in test-results journal
- Audit:
  - logs for laboratory create/engineer assignment
  - logs for user create/update in organization flows
  - logs for limits updates
- Laboratory workstation APIs:
  - `GET /api/wire-codes/{wireCodeId}/input-fields` dynamic input schema for selected wire code
  - `POST /api/test-results` create protocol header
  - `GET /api/test-results/{id}` protocol details with values
  - `GET /api/test-results` list with filtering/search
  - `PUT /api/test-results/{id}/values` save values with row-version check
  - `POST /api/test-results/{id}/complete` finalize with validation and auto route:
    - accepted -> `FinalProducts`
    - rejected -> `Rejects`
  - `409 Conflict` on optimistic concurrency mismatch
- Reporting APIs:
  - `GET /api/reports/monthly-journal?year=YYYY&month=MM` -> Excel via ClosedXML
  - `GET /api/reports/test-results/{id}/certificate` -> PDF via QuestPDF
- Frontend skeleton (`frontend`):
  - React + TypeScript + Ant Design + React Router
  - Login page and JWT storage
  - Protected routes by roles
  - Test results table with filtering/search
  - Excel/PDF export actions wired to API
  - Laboratory workstation page:
    - create protocol
    - dynamic fields by selected wire code
    - save values with row-version token
    - complete protocol with accept/reject result
  - Admin dictionaries page:
    - CRUD for countries/customers/wire codes/parameters
    - protocol assembly (required flags + limits per wire code)

## Как запустить приложение полностью

### Требования

- **.NET SDK 10** (или 8/9 — тогда смените `TargetFramework` в `.csproj`).
- **SQL Server** или **LocalDB** (обычно идёт с Visual Studio / Build Tools).
- **Node.js 18+** (LTS) — для фронтенда.

### 1. База данных

По умолчанию используется LocalDB и строка в `appsettings.Development.json`:

```
Server=(localdb)\mssqllocaldb;Database=BmzLabTestsDbDev;Trusted_Connection=True;TrustServerCertificate=True
```

Если используете полный SQL Server — замените на свою строку в `appsettings.Development.json` (секция `ConnectionStrings.DefaultConnection`).

### 2. Запуск backend (API)

В корне репозитория:

```bash
dotnet build Bmz.LabTests.slnx
dotnet run --project src/Bmz.LabTests.API/Bmz.LabTests.API.csproj
```

При старте выполняются миграции и seed (роли, страны, локальный админ).  
API слушает адреса:

- **HTTPS:** https://localhost:7045  
- **HTTP:** http://localhost:5287  

По умолчанию фронтенд настроен на **http://localhost:5287/api**, поэтому не зависит от dev HTTPS-сертификата.  
Если хотите работать через HTTPS, установите переменную окружения:

```bash
VITE_API_BASE_URL=https://localhost:7045/api
```

Swagger/OpenAPI (если включён): https://localhost:7045/openapi/v1.json  

### 3. Запуск frontend

В **другом** терминале, из корня репозитория:

```bash
cd frontend
npm install
npm run dev
```

Если при `npm run dev` появляется ошибка «vite не является внутренней или внешней командой» — убедитесь, что вы выполнили `npm install` в папке `frontend` (должна появиться папка `node_modules`). Затем снова запустите `npm run dev`. В скриптах используется `npx vite`, чтобы запускать локально установленный Vite.

Фронтенд откроется по адресу **http://localhost:5173**.  
Он обращается к API по умолчанию по адресу **http://localhost:5287/api** (настроено в `frontend/src/api/http.ts`).  
Если API на другом адресе — задайте переменную окружения `VITE_API_BASE_URL`.

### 4. Вход в систему

После открытия http://localhost:5173:

- **Логин:** `local-admin`
- **Пароль:** `VeryHardPassword`

Роль — Admin (доступ ко всем разделам: журнал, рабочее место лаборанта, отчёты, справочники).  
Пароль локального админа можно сменить только в коде/БД (seed в `Infrastructure/Persistence/SeedData.cs`).

### 5. Если API на другом хосте/порту

- В **backend**: в `Properties/launchSettings.json` в профиле `https` измените `applicationUrl`.
- Во **frontend**: задайте `VITE_API_BASE_URL` (например `https://localhost:7078/api`).

### 6. CORS и HTTPS

Для разработки CORS в API настроен разрешающе (`SetIsOriginAllowed(_ => true)`).  
При первом открытии https://localhost:7045 браузер может потребовать подтверждение самоподписанного сертификата — примите его для localhost.

---

## Run (кратко)

1. При необходимости измените строку подключения и JWT в `appsettings.Development.json`.
2. Запуск API: `dotnet run --project src/Bmz.LabTests.API/Bmz.LabTests.API.csproj`
3. Запуск фронтенда: `cd frontend` → `npm install` → `npm run dev`
4. В браузере: http://localhost:5173, логин `local-admin`, пароль `VeryHardPassword`

## Notes

- Current environment has .NET SDK 10 template defaults (`net10.0` target). If you need strict `.NET 8/9`, install matching SDK and retarget projects.
- Node.js is currently missing in this environment, so frontend build/dev commands were not executed yet.
