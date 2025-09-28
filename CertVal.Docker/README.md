# CertVal Docker Orchestration

## Structure
- `docker-compose.yml`: app services (API, Email, Web).
- `docker-compose.override.yml`: environment variables and build args.
- `infra/docker-compose.yml`: infra (Postgres, RabbitMQ, MinIO, network).
- `.env`: central configuration for both app and infra. Defaults target Production.

## Usage (from 'CertVal.Docker' directory)

### Scripts (Windows + Linux/Mac)

- Windows PowerShell:
	- `./scripts/manage.ps1 up all` – start infra then app (builds images)
	- `./scripts/manage.ps1 up infra` – start only infra
	- `./scripts/manage.ps1 up app` – start only app
	- `./scripts/manage.ps1 down app` – stop only app
	- `./scripts/manage.ps1 down infra` – stop only infra
	- `./scripts/manage.ps1 down all` – stop app then infra
	- Optional: pass `--noBuild` to skip image build

- Linux/Mac (bash):
	- `./scripts/manage.sh up all` – start infra then app (builds images)
	- `./scripts/manage.sh up infra` – start only infra
	- `./scripts/manage.sh up app` – start only app
	- `./scripts/manage.sh down app` – stop only app
	- `./scripts/manage.sh down infra` – stop only infra
	- `./scripts/manage.sh down all`– stop app then infra
	- Optional: `--no-build` to skip image build

Both scripts automatically use `.env` and compose files in this folder.

### Manual docker compose

Start infra:

```pwsh
docker compose --env-file .env -f infra/docker-compose.yml up -d
```

Start app services:

```pwsh
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d --build
```

Stop app services:

```pwsh
docker compose -f docker-compose.yml -f docker-compose.override.yml down
```

Stop infra (keeps volumes):

```pwsh
docker compose --env-file .env -f infra/docker-compose.yml down
```
