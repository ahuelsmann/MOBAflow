# Contributing to MOBAflow

Thank you for your interest in MOBAflow!

We welcome contributions of all kinds - from bug fixes and new features to documentation improvements.

## Code of Conduct

This project follows the Contributor Covenant. Please be respectful, constructive, and helpful.

## How Can I Contribute?

### Reporting Bugs

1. Check if the bug has already been reported
2. Create a new Issue with the bug label
3. Describe in detail: What happens? What should happen? Steps to reproduce.

### Suggesting Features

1. Create an Issue with the enhancement label
2. Describe the use case and benefits
3. Wait for feedback before starting implementation

### Contributing Code

1. Fork the repository
2. Create a branch (feature/your-feature or fix/your-bugfix)
3. Implement your changes
4. Write tests (if applicable)
5. Create a Pull Request

## Development Setup

### Prerequisites

- Visual Studio 2022 (or later)
- .NET 10 SDK
- Git

### Clone and Build

git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow
dotnet restore
dotnet build
dotnet test

## Coding Guidelines

1. Fluent Design First - Follow Microsoft Fluent Design 2 principles
2. MVVM Pattern - Use CommunityToolkit.Mvvm for all ViewModels
3. Dependency Injection - No new for services, always use DI
4. Async Everywhere - Use async/await for all I/O operations

## Pull Request Process

- Code compiles without errors and warnings
- All tests pass
- Copyright header present in new files
- Documentation updated if needed

## Commit Convention

We follow Conventional Commits: feat, fix, docs, style, refactor, test, chore

## Azure Speech Service (Optional)

MOBAflow supports Azure Cognitive Services for Text-to-Speech:

- Free up to 500,000 characters/month (Free Tier F0)
- Premium Neural Voices
- Multi-language support

See docs/wiki/AZURE-SPEECH-SETUP.md for setup instructions.

Thank you for your contribution!
