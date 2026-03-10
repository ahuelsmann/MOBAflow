---
description: MOBAflow architecture, C#/.NET quality rules, DI, async/await, and maintainable implementation style.
trigger: always_on
---

# Architecture and Quality Rules

- Follow C# and .NET best practices.
- Respect SOLID and Clean Code principles.
- Prefer simple, explicit, maintainable code over clever abstractions.
- Keep methods and classes focused on one responsibility.
- Use meaningful names. Avoid vague names such as `data`, `helper`, `tmp`, or `manager` unless the surrounding architecture already establishes that concept.

## Architecture

- Respect the existing MOBAflow layering and dependency flow.
- Do not move platform-specific code into `Domain`, `Backend`, or `Common`.
- Reuse established project patterns before introducing new abstractions.
- Avoid broad refactorings unless they are clearly required by the task.

## Dependency Injection

- Prefer constructor injection.
- Do not introduce service locator patterns.
- Do not hide service creation in random `new` calls when the type should be injected.
- Prefer interfaces and small services where they improve testability and separation of concerns.

## Async and Concurrency

- Use `async` and `await` correctly.
- Do not use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in application code.
- Do not introduce fire-and-forget tasks unless the behavior is explicitly required and failure handling is considered.
- Preserve existing threading boundaries, especially around UI dispatch and the EventBus.

## Static Code

- Do not introduce generic static helper or utility classes as a default design choice.
- Prefer injected services, cohesive domain types, extension methods, or small focused abstractions.
- Static types are acceptable for narrowly scoped pure functions, constants, or extension method containers when that is the clearest design.

## Testing and Validation

- When behavior changes, suggest or add relevant tests.
- Prefer targeted validation commands that match the affected project.
- Follow the existing project conventions for NUnit and surrounding test patterns.
