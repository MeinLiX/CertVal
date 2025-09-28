# CertVal Docker Orchestration

## Structure
- `docker-compose.yml`: app services (API, Email, Web).
- `docker-compose.override.yml`: environment variables and build args.
- `infra/docker-compose.yml`: infra (Postgres, RabbitMQ, MinIO, network).
- `.env`: central configuration for both app and infra. Defaults target Production.

## Usage (from CertVal.Docker)

### Start services:
1) Infrastructure:

```pwsh
docker compose --env-file .env -f infra/docker-compose.yml up -d
```

2) Application services:

```pwsh
docker compose up -d --build
# docker compose -f docker-compose.yml -f docker-compose.override.yml up -d --build
```

### Stop services:

1) Application services:
```pwsh
docker compose down
# docker compose -f docker-compose.yml -f docker-compose.override.yml down
```

2) infrastructure (keeps volumes):
```pwsh
docker compose --env-file .env -f infra/docker-compose.yml down
```
