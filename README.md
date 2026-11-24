# SkillConnect
A full-stack service marketplace built with ASP.NET Core, React, TypeScript and SQL Server.

See the [requirements specification](docs/kravspec.md) for the first release's scope and booking flow.

## Local development

Install the .NET 10 SDK and Node.js 24. Run the following from the repository root to start the API:

```powershell
dotnet run --project backend/SkillConnect.Api --launch-profile http
```

The health endpoint is available at [localhost:5241/health](http://localhost:5241/health).

Start the frontend in a separate terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Open the local URL printed by Vite. Run `npm run lint` and `npm run build` from `frontend`, or `dotnet test backend/SkillConnect.sln` from the repository root.

### Local SQL Server

With Docker running, set `MSSQL_SA_PASSWORD` in your shell to a strong local development password and run `docker compose up -d sqlserver` from the repository root. SQL Server listens on `127.0.0.1:1433`. The API health endpoint runs independently of SQL Server.
