# Project Tracker

## Directory Structure

- **CAVerifierServer.Silo**: Contains the main application logic and configuration for the Silo server.
- **CAVerifierServer.HttpApi.Host**: Hosts the HTTP API, including middleware and controllers.
- **CAVerifierServer.HttpApi**: Defines the HTTP API modules and models.
- **CAVerifierServer.MongoDB**: Manages MongoDB-related configurations and modules.
- **CAVerifierServer.HttpApi.Client**: Contains client-side modules for interacting with the HTTP API.
- **CAVerifierServer.Grains**: Implements the grain logic and state management.
- **CAVerifierServer.Domain**: Defines domain-specific logic and constants.
- **CAVerifierServer.EntityEventHandler**: Handles entity events.
- **CAVerifierServer.EntityEventHandler.Core**: Core logic for entity event handling.
- **CAVerifierServer.AuthServer**: Manages authentication server logic and configuration.
- **CAVerifierServer.DbMigrator**: Handles database migration tasks.
- **CAVerifierServer.Domain.Shared**: Shared domain logic and configurations.
- **CAVerifierServer.Application**: Application-specific logic and modules.
- **CAVerifierServer.Application.Contracts**: Defines contracts for application modules.

## Current Tasks

### Configuration and Setup
- **CAVerifierServer.Silo**: Ensure all configurations are up-to-date.
- **CAVerifierServer.MongoDB**: Verify MongoDB configurations.
- **CAVerifierServer.AuthServer**: Audit authentication processes.
- **CAVerifierServer.DbMigrator**: Test migration scripts.

### Development and Implementation
- **CAVerifierServer.HttpApi.Host**: Review middleware implementations.
- **CAVerifierServer.HttpApi**: Update API models as needed.
- **CAVerifierServer.HttpApi.Client**: Test client interactions.
- **CAVerifierServer.Application**: Implement new application features.

### Optimization and Refactoring
- **CAVerifierServer.Grains**: Optimize grain state management.
- **CAVerifierServer.Domain**: Refactor domain logic for clarity.
- **CAVerifierServer.EntityEventHandler**: Improve event handling efficiency.
- **CAVerifierServer.Domain.Shared**: Ensure shared logic is consistent.

### Review and Update
- **CAVerifierServer.Application.Contracts**: Review and update contracts.

## Task Overview

| Task ID | Description | Status | Assigned To | Priority |
|---------|-------------|--------|-------------|----------|
| 1       | Implement authentication module | 🚧 In Progress | Developer A | High |
| 2       | Set up CI/CD pipeline | 🔜 To Do | Unassigned | Medium |
| 3       | Write unit tests for user service | ✅ Completed | Developer B | Low |
| 4       | Develop ContractEventHandler | 🔜 To Do | Unassigned | High |
| 5       | Create API endpoints in HttpApi | 🔜 To Do | Unassigned | Medium |
| 6       | Implement logging in Logs | 🔜 To Do | Unassigned | Low | 