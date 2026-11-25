# SkillConnect
A full-stack service marketplace built with ASP.NET Core, React, TypeScript and SQL Server.

See the [requirements specification](docs/kravspec.md) for the first release's scope and booking flow.

## Local development

Install the .NET 10 SDK and Node.js 24. Configure the database and apply the migration as described below, then run the API from the repository root in the same terminal:

```powershell
dotnet run --project backend/SkillConnect.Api --launch-profile http
```

The API exposes [a health check](http://localhost:5241/health) and [the published service catalog](http://localhost:5241/api/services). The catalog accepts an optional `category` query parameter.

Start the frontend in a separate terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Open the local URL printed by Vite. The development server forwards `/api` requests to the API on port 5241. When serving the production frontend build, route `/api` to the backend on the same origin.

Run `npm run lint` and `npm run build` from `frontend`, or `dotnet test backend/SkillConnect.sln` from the repository root.

### Local SQL Server

With Docker running, set `MSSQL_SA_PASSWORD` in your shell to a strong local development password, then run these commands from the repository root:

```powershell
docker compose up -d sqlserver
$skillConnectDb = [System.Data.Common.DbConnectionStringBuilder]::new()
$skillConnectDb['Server'] = '127.0.0.1,1433'
$skillConnectDb['Database'] = 'SkillConnect'
$skillConnectDb['User ID'] = 'sa'
$skillConnectDb['Password'] = $env:MSSQL_SA_PASSWORD
$skillConnectDb['TrustServerCertificate'] = 'true'
$env:ConnectionStrings__SkillConnect = $skillConnectDb.ConnectionString
```

Install the `dotnet-ef` tool if needed and apply the migration once SQL Server is ready:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef database update --project backend/SkillConnect.Infrastructure
```

The catalog starts empty on a new database. The health endpoint runs independently of SQL Server.
