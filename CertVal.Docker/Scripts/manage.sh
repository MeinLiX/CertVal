#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: ./manage.sh <up|down> [all|infra|app] [--no-build]" >&2
  echo "Examples:" >&2
  echo "  ./manage.sh up all" >&2
  echo "  ./manage.sh up infra" >&2
  echo "  ./manage.sh up app" >&2
  echo "  ./manage.sh down app" >&2
  echo "  ./manage.sh down all" >&2
}

ACTION=${1:-}
SCOPE=${2:-all}
NO_BUILD=false
if [[ ${3:-} == "--no-build" ]]; then NO_BUILD=true; fi

if [[ -z "$ACTION" ]]; then usage; exit 1; fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &> /dev/null && pwd)
ROOT=$(cd "$SCRIPT_DIR/.." && pwd)
cd "$ROOT"

COMPOSE_INFRA="infra/docker-compose.yml"
COMPOSE_APP="docker-compose.yml"
COMPOSE_OVERRIDE="docker-compose.override.yml"
ENV_FILE=".env"
NETWORK_NAME="certval-net"

compose() {
  local -a FILES=("$@")
  local -a CMD=(docker compose)
  if [[ -f "$ENV_FILE" ]]; then CMD+=(--env-file "$ENV_FILE"); fi
  for f in "${FILES[@]}"; do CMD+=( -f "$f" ); done
  shift $(( $# - 1 )) || true
}

run_compose() {
  local -a FILES=()
  while [[ $# -gt 0 ]]; do
    case "$1" in
      -f) FILES+=("$2"); shift 2;;
      *) break;;
    esac
  done
}

compose_run() {
  local -a FILES=("$1"); shift
  if [[ $# -gt 0 && $1 == "," ]]; then shift; fi
}

dc() {
  local -a CMD=(docker compose)
  if [[ -f "$ENV_FILE" ]]; then CMD+=(--env-file "$ENV_FILE"); fi
  while [[ $# -gt 0 ]]; do CMD+=("$1"); shift; done
  echo "Running: ${CMD[*]}" >&2
  "${CMD[@]}"
}

ensure_network() {
  if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
    echo "Creating network '$NETWORK_NAME'" >&2
    docker network create "$NETWORK_NAME" >/dev/null
  fi
}

case "$ACTION" in
  up)
    case "$SCOPE" in
      infra)
        dc -f "$COMPOSE_INFRA" up -d
        ;;
      app)
        ensure_network
        if [[ "$NO_BUILD" == false ]]; then dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" up -d --build; else dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" up -d; fi
        ;;
      all)
        dc -f "$COMPOSE_INFRA" up -d
        if [[ "$NO_BUILD" == false ]]; then dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" up -d --build; else dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" up -d; fi
        ;;
      *) usage; exit 1;;
    esac
    ;;
  down)
    case "$SCOPE" in
      infra)
        dc -f "$COMPOSE_INFRA" down
        ;;
      app)
        dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" down
        ;;
      all)
        dc -f "$COMPOSE_APP" -f "$COMPOSE_OVERRIDE" down
        dc -f "$COMPOSE_INFRA" down
        ;;
      *) usage; exit 1;;
    esac
    ;;
  *) usage; exit 1;;

esac
