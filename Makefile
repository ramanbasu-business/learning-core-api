PROJECT := src/LearningUserService

.PHONY: run migration migrate migrations-list db-drop

run:
	dotnet run --project $(PROJECT)

# Usage: make migration name=AddIsDeletedToUsers
migration:
	dotnet ef migrations add $(name) --project $(PROJECT) --startup-project $(PROJECT)

migrate:
	dotnet ef database update --project $(PROJECT) --startup-project $(PROJECT)

migrations-list:
	dotnet ef migrations list --project $(PROJECT) --startup-project $(PROJECT)

db-drop:
	dotnet ef database drop --project $(PROJECT) --startup-project $(PROJECT)
