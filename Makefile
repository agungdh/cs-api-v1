COMPOSE := docker compose
PROJECT := $(shell basename $(CURDIR))
PG_VOLUME := $(PROJECT)_postgres_data

.DEFAULT_GOAL := help
.PHONY: help up up-postgres down down-postgres logs ps migrate reset-postgres clean

help: ## Tampilkan daftar perintah (default)
	@echo "Usage: make [target]"
	@echo ""
	@grep -E '^[a-zA-Z0-9_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

up: ## Up semua service (postgres, valkey, minio, adminer)
	$(COMPOSE) up -d

up-postgres: ## Up postgres saja
	$(COMPOSE) up -d postgres

down: ## Down semua service (volume dipertahankan)
	$(COMPOSE) down

down-postgres: ## Stop + hapus container postgres saja (volume dipertahankan)
	$(COMPOSE) stop postgres
	$(COMPOSE) rm -f postgres

logs: ## Lihat logs semua service
	$(COMPOSE) logs -f

ps: ## Lihat status container
	$(COMPOSE) ps

migrate: ## Apply EF Core migrations ke DB
	dotnet ef database update

reset-postgres: ## Down postgres + hapus volume postgres_data + up lagi (DATA HILANG)
	$(COMPOSE) stop postgres
	$(COMPOSE) rm -f postgres
	docker volume rm $(PG_VOLUME) || true
	$(COMPOSE) up -d postgres

clean: ## Down semua + hapus SEMUA volume (postgres, valkey, minio — DATA HILANG)
	$(COMPOSE) down -v
