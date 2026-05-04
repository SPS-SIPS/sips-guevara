SHELL := /bin/bash

.DEFAULT_GOAL := help

API_PROJECT ?= Apps/API/API.csproj
API_DLL ?= bin/$(CONFIGURATION)/$(FRAMEWORK)/SPSS.API.dll
API_DIR ?= Apps/API
APP_URL ?= http://localhost:5007
CONFIGURATION ?= Debug
FRAMEWORK ?= net8.0
INFRASTRUCTURE_DI ?= Core/Infrastructure/DI.cs
OPENAPI_DOC ?= v1
OPENAPI_OUTPUT ?= openapi.yaml
README_OUTPUT ?= README.md
SWAGGER_CLI_VERSION ?= 6.5.0
SWAGGER_TOOL_DIR ?= /tmp/spss-backend-dotnet-tools

.PHONY: help dev build openapi swagger-tool readme

help:
	@printf 'SPS System backend tasks\n\n'
	@printf '  make dev       Start the API in Development mode at %s\n' '$(APP_URL)'
	@printf '  make openapi   Generate %s from the %s Swagger document\n' '$(OPENAPI_OUTPUT)' '$(OPENAPI_DOC)'
	@printf '  make readme    Generate %s from connected Infrastructure services\n' '$(README_OUTPUT)'

dev:
	@ASPNETCORE_ENVIRONMENT=Development dotnet run --project '$(API_PROJECT)' --launch-profile http

build:
	@dotnet build '$(API_PROJECT)' --configuration '$(CONFIGURATION)'

swagger-tool:
	@mkdir -p '$(SWAGGER_TOOL_DIR)'
	@if [ ! -x '$(SWAGGER_TOOL_DIR)/swagger' ]; then \
		dotnet tool install Swashbuckle.AspNetCore.Cli \
			--tool-path '$(SWAGGER_TOOL_DIR)' \
			--version '$(SWAGGER_CLI_VERSION)'; \
	fi

openapi: build swagger-tool
	@cd '$(API_DIR)' && \
	ASPNETCORE_ENVIRONMENT=Development \
	DOTNET_ROLL_FORWARD=LatestMajor \
	OpenApiExport=true \
	'$(SWAGGER_TOOL_DIR)/swagger' tofile \
		--yaml \
		--output '../../$(OPENAPI_OUTPUT)' \
		'$(API_DLL)' \
		'$(OPENAPI_DOC)'
	@printf 'Generated %s\n' '$(OPENAPI_OUTPUT)'

readme:
	@{ \
		printf '# SPS System Backend\n\n'; \
		printf '## Development\n\n'; \
		printf 'Run the API locally in Development mode:\n\n'; \
		printf '```sh\nmake dev\n```\n\n'; \
		printf 'The API uses the `http` launch profile from `%s` and listens at `%s` by default.\n\n' 'Apps/API/Properties/launchSettings.json' '$(APP_URL)'; \
		printf '## OpenAPI\n\n'; \
		printf 'Generate the OpenAPI specification:\n\n'; \
		printf '```sh\nmake openapi\n```\n\n'; \
		printf 'The generated file is `%s` and is exported from the `%s` Swagger document. The export runs with `OpenApiExport=true` so database initialization and background workers are skipped.\n\n' '$(OPENAPI_OUTPUT)' '$(OPENAPI_DOC)'; \
		printf '## Features\n\n'; \
		printf 'This list is generated from connected services registered in `%s`. Each registered service is treated as an application feature.\n\n' '$(INFRASTRUCTURE_DI)'; \
		perl -ne ' \
			sub trim { my $$s = shift; $$s =~ s/^\s+|\s+$$//g; return $$s; } \
			sub human { \
				my $$s = shift; \
				$$s =~ s/^I(?=[A-Z])//; \
				$$s =~ s/(Service|Worker|Job)$$//; \
				$$s =~ s/([A-Z]+)([A-Z][a-z])/$$1 $$2/g; \
				$$s =~ s/([a-z0-9])([A-Z])/$$1 $$2/g; \
				return $$s; \
			} \
			sub emit { \
				my ($$kind, $$lifetime, $$service, $$implementation) = @_; \
				$$service = trim($$service); \
				$$implementation = trim($$implementation || $$service); \
				return if $$implementation =~ /Interceptor$$/; \
				my $$feature = human($$implementation); \
				print "- **$$feature** (`$$service` -> `$$implementation`, $$lifetime $$kind)\n"; \
			} \
			if (/services\.Add(Scoped|Singleton|Transient)<([^>]+)>/) { \
				my ($$lifetime, $$types) = (lc $$1, $$2); \
				my @types = split /,/, $$types; \
				emit("service", $$lifetime, $$types[0], $$types[1] || $$types[0]); \
			} elsif (/services\.AddHostedService<([^>]+)>/) { \
				emit("worker", "hosted", $$1, $$1); \
			} elsif (/services\.AddCronJob<([^>]+)>/) { \
				emit("worker", "scheduled", $$1, $$1); \
			} \
		' '$(INFRASTRUCTURE_DI)'; \
	} > '$(README_OUTPUT)'
	@printf 'Generated %s\n' '$(README_OUTPUT)'
