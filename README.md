# Follows

A reusable ASP.NET Core MVC web template (.NET 10), meant to be copied into new projects. It
ships with a working Identity setup, a social-style feed, document uploads, an AI chat integration
(Ollama / OpenAI-compatible gateway), and a Danish-language admin Settings area — so a new project
can start from a running app instead of an empty one.

## Features

- **Accounts & roles** — custom UI on top of ASP.NET Core Identity (email as login id). Three
  roles: `Developer` (highest, granted automatically to the first registered user),
  `Administrator`, and `User`. First-run setup wizard (`/Setup/FirstUser`) when no users exist yet.
- **Feed** — a simple social feed with posts, images/video uploads and moderation for
  admins/developers.
- **Documents** — per-user document uploads and listing.
- **Chat** — AI chat backed by either a local [Ollama](https://ollama.com) instance or an
  OpenAI-compatible AI gateway, both configurable per-deployment.
- **Settings** (admin) — tabbed management UI: users, public registration toggle, theme, and a
  live log viewer (backed by a separate SQLite log database).
- **Themes** — database-driven Light/Dark/System theme, served as dynamic CSS.
- **File storage** — uploads are stored on disk under `App_files/<category>/`, never as DB blobs.

## Tech stack

- ASP.NET Core MVC (Razor views) on **.NET 10**
- ASP.NET Core Identity for auth
- Entity Framework Core with **SQLite** (two databases: app data and logs)
- Serilog (console + SQLite sink)
- Bootstrap 5 + Bootstrap Icons

## Project layout

```
Follows.slnx           solution file (single project: web/web.csproj)
web/
  App_dbs/              SQLite databases (app.db, logs.db) — created at runtime, git-ignored
  App_files/             uploaded files (avatars, documents, feed media, ...) — git-ignored
  Controllers/, Views/   MVC
  Data/                  DbContext, entities, migrations, seed/init logic
  Repositories/          data access against this app's own storage (app.db, logs.db, App_files)
  Services/              integrations with external systems (Ollama, mail, SMS, AI gateway)
  ViewModels/            per-view/form models with data annotations
  Infrastructure/        middleware and cross-cutting helpers
```

Internal architectural/coding conventions used throughout this codebase (naming patterns,
folder structure rules, etc.) are documented in `CLAUDE.md`, which is intentionally not tracked
in this repository.

## Getting started (local development)

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
dotnet restore Follows.slnx
dotnet run --project web
```

On first run this will:
- Apply EF Core migrations and seed default data (idempotent — safe to run repeatedly).
- Create `web/App_dbs/app.db` and `web/App_dbs/logs.db`.
- Redirect you to `/Setup/FirstUser` to create the initial account, which is automatically
  granted the `Developer` role.

By default the app listens on `http://localhost:5210` (see
[web/Properties/launchSettings.json](web/Properties/launchSettings.json)).

Local secrets (SMTP credentials, API keys, etc.) go in `web/appsettings.Development.json`, which
is git-ignored and never committed.

### Useful commands

```bash
dotnet build Follows.slnx                            # build
dotnet ef migrations add <Name> --project web         # add a migration after entity changes
dotnet ef database update --project web               # apply migrations manually
```

There is no test project in this repo currently.

## Running with Docker (recommended for servers)

The repo includes a [Dockerfile](Dockerfile) and [docker-compose.yml](docker-compose.yml) for
production-style deployment on any server with Docker installed.

```bash
cp .env.example .env    # fill in site name, mail, and any optional integrations
docker compose up -d --build
```

This builds the app, runs migrations/seeding automatically on startup (same as `dotnet run`), and
persists `App_dbs/` and `App_files/` in named Docker volumes so data survives restarts and
rebuilds. The container listens on `8080` internally; the published host port is controlled by
`HTTP_PORT` in `.env` (default `8080`).

The app expects to run behind a reverse proxy (Nginx, Traefik, Caddy, ...) that terminates TLS —
the compose setup does not include one, so put your own in front of it for HTTPS in production.

See [.env.example](.env.example) for all configurable settings (mail/SMTP, Ollama, AI gateway,
SMS) and comments in [docker-compose.yml](docker-compose.yml) for details on the volumes and
healthcheck.

To stop and remove the containers (keeping data):

```bash
docker compose down
```

To also wipe persisted data (databases and uploaded files):

```bash
docker compose down -v
```

## Configuration reference

Settings live in [web/appsettings.json](web/appsettings.json) and can be overridden via
environment variables (`Section__Key`, e.g. `Mail__Smtp__Host`) or `web/appsettings.Development.json`
locally.

| Section | Purpose |
|---|---|
| `ConnectionStrings` | SQLite paths for the app and log databases |
| `AppSettings` | Site name/logo, storage paths, public registration toggle, default theme |
| `AiGateway` / `Ollama` | Optional AI chat backends — leave blank to disable |
| `Sms` | Optional SMS gateway — leave blank to disable the background worker |
| `Mail` | SMTP (sending) and IMAP (reading) settings |

## License

No license file is currently included; treat this repository as private/internal unless a
license is added.
