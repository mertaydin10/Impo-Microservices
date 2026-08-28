# Impo CLI - Project Management Tool

SHELL := /bin/bash
.PHONY: help install build test clean start stop restart logs health status seed demo

# Colors
GREEN  := \033[0;32m
YELLOW := \033[1;33m
RED    := \033[0;31m
NC     := \033[0m

help: ## Show this help message
	@echo "Impo - Project Management Commands"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2}'
	@echo ""

install: ## Install dependencies and setup project
	@echo "$(GREEN)Installing dependencies...$(NC)"
	@dotnet restore
	@chmod +x start.sh
	@mkdir -p logs data
	@echo "$(GREEN)✓ Installation complete!$(NC)"

build: ## Build all projects
	@echo "$(GREEN)Building projects...$(NC)"
	@dotnet build --nologo --verbosity quiet
	@echo "$(GREEN)✓ Build complete!$(NC)"

test: ## Run all tests
	@echo "$(GREEN)Running tests...$(NC)"
	@dotnet test --nologo --verbosity quiet
	@echo "$(GREEN)✓ All tests passed!$(NC)"

clean: ## Clean build artifacts
	@echo "$(YELLOW)Cleaning...$(NC)"
	@dotnet clean --nologo --verbosity quiet
	@rm -rf logs/*.log logs/*.pid
	@echo "$(GREEN)✓ Clean complete!$(NC)"

start: ## Start all services
	@echo "$(GREEN)Starting all services...$(NC)"
	@./start.sh

stop: ## Stop all services
	@echo "$(YELLOW)Stopping services...$(NC)"
	@pkill -f "Spectator.Api" || true
	@pkill -f "LocalCrm.Api" || true
	@pkill -f "LocalCrm.App" || true
	@pkill -f "PromptGateway.Api" || true
	@rm -f logs/*.pid
	@echo "$(GREEN)✓ Services stopped!$(NC)"

restart: stop start ## Restart all services

logs: ## Show logs from all services
	@tail -f logs/*.log 2>/dev/null || echo "No logs found. Start services first."

health: ## Check health of all services
	@echo "$(GREEN)Checking service health...$(NC)"
	@echo -n "Spectator (5185): "
	@curl -sf http://localhost:5185/health >/dev/null && echo "$(GREEN)✓ OK$(NC)" || echo "$(RED)✗ DOWN$(NC)"
	@echo -n "LocalCrm API (5057): "
	@curl -sf http://localhost:5057/health >/dev/null && echo "$(GREEN)✓ OK$(NC)" || echo "$(RED)✗ DOWN$(NC)"
	@echo -n "PromptGateway (5047): "
	@curl -sf http://localhost:5047/health >/dev/null && echo "$(GREEN)✓ OK$(NC)" || echo "$(RED)✗ DOWN$(NC)"

status: ## Show detailed status of all services
	@echo "$(GREEN)Service Status:$(NC)"
	@echo ""
	@lsof -ti:5185 >/dev/null 2>&1 && echo "  🏟️  Spectator      [$(GREEN)RUNNING$(NC)] http://localhost:5185" || echo "  🏟️  Spectator      [$(RED)STOPPED$(NC)]"
	@lsof -ti:5057 >/dev/null 2>&1 && echo "  👥 LocalCrm API   [$(GREEN)RUNNING$(NC)] http://localhost:5057" || echo "  👥 LocalCrm API   [$(RED)STOPPED$(NC)]"
	@lsof -ti:5209 >/dev/null 2>&1 && echo "  📱 LocalCrm App   [$(GREEN)RUNNING$(NC)] http://localhost:5209" || echo "  📱 LocalCrm App   [$(RED)STOPPED$(NC)]"
	@lsof -ti:5047 >/dev/null 2>&1 && echo "  🤖 PromptGateway  [$(GREEN)RUNNING$(NC)] http://localhost:5047" || echo "  🤖 PromptGateway  [$(RED)STOPPED$(NC)]"

seed: ## Seed databases with sample data
	@echo "$(GREEN)Seeding sample data...$(NC)"
	@dotnet run --project tools/DataSeeder --nologo

demo: ## Run a full demo workflow
	@echo "$(GREEN)Running demo workflow...$(NC)"
	@echo "$(YELLOW)1. Starting services...$(NC)"
	@make start
	@sleep 5
	@echo "$(YELLOW)2. Seeding data...$(NC)"
	@make seed
	@echo "$(YELLOW)3. Running demo scenarios...$(NC)"
	@dotnet run --project tools/DemoRunner --nologo
	@echo "$(GREEN)✓ Demo complete! Check http://localhost:5185$(NC)"

dev-spectator: ## Start Spectator in development mode
	@dotnet watch --project src/Spectator/Spectator.Api

dev-localcrm: ## Start LocalCrm in development mode
	@dotnet watch --project src/LocalCrm/LocalCrm.Api & dotnet watch --project src/LocalCrm/LocalCrm.App

dev-gateway: ## Start PromptGateway in development mode
	@dotnet watch --project src/PromptGateway/PromptGateway.Api

analyze: ## Run Archeologist code analysis
	@echo "$(GREEN)Running code analysis...$(NC)"
	@dotnet run --project src/Archeologist/Archeologist.Cli -- src --min-severity MEDIUM

dashboard: ## Open dashboard in browser
	@open dashboard.html 2>/dev/null || xdg-open dashboard.html 2>/dev/null || echo "Open dashboard.html manually"

swagger: ## Open all Swagger UIs
	@open http://localhost:5185/swagger 2>/dev/null || true
	@open http://localhost:5057/swagger 2>/dev/null || true
	@open http://localhost:5047/swagger 2>/dev/null || true

urls: ## Show all service URLs
	@echo "$(GREEN)Service URLs:$(NC)"
	@echo "  Dashboard:         file://$(PWD)/dashboard.html"
	@echo "  Spectator:         http://localhost:5185"
	@echo "  Spectator Swagger: http://localhost:5185/swagger"
	@echo "  LocalCrm App:      http://localhost:5209"
	@echo "  LocalCrm API:      http://localhost:5057"
	@echo "  LocalCrm Swagger:  http://localhost:5057/swagger"
	@echo "  PromptGateway:     http://localhost:5047"
	@echo "  Gateway Swagger:   http://localhost:5047/swagger"

ci: clean build test ## Run CI pipeline (clean, build, test)
	@echo "$(GREEN)✓ CI pipeline passed!$(NC)"
