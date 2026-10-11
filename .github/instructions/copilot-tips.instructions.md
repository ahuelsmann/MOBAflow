---
description: 'Optional prompt examples; not additional repository requirements.'
applyTo: '.github/instructions/copilot-tips.instructions.md'
---

# Copilot prompt examples for MOBAflow

> Reference examples, not an additional set of agent requirements.
> Follow [AGENTS.md](../../AGENTS.md) for workflow, architecture and validation.
> Copilot commands and UI features depend on the installed client; these examples
> do not require a particular extension or chat command.

## Provide a concrete scope

Describe the required behavior, affected area and expected result. Ask the
assistant to inspect current callers and tests before proposing a change.

```text
Improve the regression tests for Backend/Service/ProjectValidator.cs.
Use the existing NUnit fixtures and Moq where appropriate.
Cover the relevant failure cases and assert meaningful results.
Do not change production behavior unless the tests confirm a defect.
```

## Shared ViewModels

```text
Follow the existing CommunityToolkit.Mvvm patterns in SharedUI.
Read operating state through the narrow runtime roles.
Send every operator command through the injected IRuntimeCommandGateway.
Preserve project switching, cancellation and nested property-change propagation.
Keep WinUI and MAUI types outside shared logic.
```

Preserve existing model-wrapper properties when they are needed for auto-save;
do not replace them mechanically with generated properties.

## Async and dependency injection

```text
Inspect the actual caller and lifetime of this service.
Use constructor injection and async/await for asynchronous work.
Do not use .Result, .Wait() or GetAwaiter().GetResult().
Preserve cancellation and existing error handling.
Do not add retries, new service lifetimes or dependencies without a requirement.
```

## Focused diagnosis

```text
Find the cause of this reported behavior using current source and existing tests.
State the reproduction, hypothesis and evidence before recommending a fix.
Use fakes or owned temporary data; do not connect to the real layout.
Do not start MOBAflow or operate hardware without explicit approval.
```

For repository-specific diagnosis and review, see the
[AI development guide](../../docs/AI-DEVELOPMENT.md).

## Request evidence, not arbitrary style changes

- Prefer the smallest cohesive change that satisfies the requirement.
- Use current source as the reference for interfaces and implementation patterns.
- Choose LINQ or loops for clarity and the actual workload; neither is mandatory.
- Keep methods focused without an arbitrary line-count rule.
- Test behavior with NUnit; do not substitute xUnit attributes in this repository.
- Choose checks from the validation table in AGENTS.md rather than always building
  the entire Windows/Android solution.
- Keep task status in GitHub issues, not Azure DevOps.
- Run local secrets scans; Sonar code analysis belongs to the GitHub PR pipeline.

## Before accepting generated changes

Review the diff, check affected contracts and run the checks appropriate to the
change. A documentation-only edit needs documentation checks, not a product build.
A passing filtered test run must have discovered and executed the intended tests.
Neither an assistant's confidence nor generated assertions prove correctness.
