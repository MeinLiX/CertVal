# CertVal Docker Orchestration

## Structure
- `docker-compose.yml`: main aggregator; includes infra and services.
- `infra/docker-compose.yml`: Postgres, RabbitMQ, MinIO, shared network/volumes.
- `services/docker-compose.yml`: API, Email, Web definitions and dependencies.
- `services/networks.overlay.yml`: declares shared network as external for services.
- `.env`: central configuration for both app and infra.

## Usage (from 'CertVal.Docker' directory)

### Scripts (Windows + Linux/Mac)

- Windows PowerShell:
  - `./scripts/manage.ps1 up all` – start infra then services (builds images)
  - `./scripts/manage.ps1 up infra` – start only infra
  - `./scripts/manage.ps1 up app` – start only services
  - `./scripts/manage.ps1 down app` – stop only services
  - `./scripts/manage.ps1 down infra` – stop only infra
  - `./scripts/manage.ps1 down all` – stop services first, then infra
  - Optional: pass `-noBuild` to skip image build

- Linux/Mac (bash):
  - `./scripts/manage.sh up all` – start infra then services (builds images)
  - `./scripts/manage.sh up infra` – start only infra
  - `./scripts/manage.sh up app` – start only services
  - `./scripts/manage.sh down app` – stop only services
  - `./scripts/manage.sh down infra` – stop only infra
  - `./scripts/manage.sh down all` – stop services first, then infra
  - Optional: `--no-build` to skip image build

Both scripts automatically use `.env` and compose files in this folder.

### Manual docker compose

Common start:
```pwsh
docker compose up -d

#stop
docker compose down
```

Start infra:

```pwsh
docker compose --env-file .env -f infra/docker-compose.yml up -d

#stop
docker compose -f infra/docker-compose.yml down
```

Start services (infra already running):

```pwsh
docker compose --env-file .env -f services/docker-compose.yml -f services/networks.overlay.yml up -d --build

#stop
docker compose -f services/docker-compose.yml down
```

### Auto-update running services

1) Add GHCR credentials to `.env` :

```
GHCR_USER=your-gh-username
GHCR_PAT=ghp_xxx_or_fine_grained_PAT_with_read:packages
```

2) Start services with watchtower override:

```pwsh
docker compose --env-file .env -f services/docker-compose.yml -f services/networks.overlay.yml -f services/docker-compose.watchtower.yml up -d
```