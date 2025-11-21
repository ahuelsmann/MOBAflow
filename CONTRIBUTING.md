# Contributing to MOBAflow

Thank you for your interest in contributing to MOBAflow! 🎉

This document provides guidelines for contributing to the project.

---

## 🚀 How to Contribute

### 1. Fork the Repository

Fork the repository to your own GitHub account.

### 2. Clone Your Fork

```bash
git clone https://github.com/YOUR_USERNAME/MOBAflow.git
cd MOBAflow
```

### 3. Create a Feature Branch

```bash
git checkout -b feature/AmazingFeature
```

### 4. Make Your Changes

Follow the coding guidelines outlined below and in [.copilot-instructions.md](.copilot-instructions.md).

### 5. Test Your Changes

```bash
dotnet restore
dotnet build
dotnet test
```

Ensure all tests pass before submitting your pull request.

### 6. Commit Your Changes

```bash
git add .
git commit -m 'Add some AmazingFeature'
```

Use clear, descriptive commit messages. Follow [Conventional Commits](https://www.conventionalcommits.org/) if possible:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Adding or updating tests
- `refactor:` Code refactoring

### 7. Push to Your Fork

```bash
git push origin feature/AmazingFeature
```

### 8. Open a Pull Request

Open a Pull Request from your fork to the main repository.

Include:
- Clear description of the changes
- Link to related issues (if applicable)
- Screenshots/videos for UI changes

---

## 📋 Coding Guidelines

### Architecture Principles

⚠️ **CRITICAL**: The `Backend` project MUST remain **100% platform-independent**!

- ❌ **NEVER** add UI thread dispatching to Backend (`DispatcherQueue`, `MainThread`, etc.)
- ✅ **ALWAYS** handle platform-specific threading in platform-specific ViewModels via `IUiDispatcher`

### File Organization

- ✅ **One class per file** (file name = class name)
- ✅ **Namespace matches folder structure**
- ✅ **Use absolute namespaces** (e.g., `using Moba.Backend.Model;`)

### Code Style

- ✅ Use **C# 13** features where appropriate
- ✅ Prefer **async/await** for I/O operations
- ✅ Use **ConfigureAwait(false)** in Backend (not in ViewModels)
- ❌ **NEVER** use `.Result` or `.Wait()` (causes deadlocks)
- ❌ **NEVER** use `async void` (except event handlers)

### MVVM Pattern

- ✅ Use **CommunityToolkit.Mvvm** (`ObservableObject`, `RelayCommand`)
- ✅ **ALL** UI logic in ViewModels (never in code-behind)
- ✅ ViewModels must be **testable** (no direct UI dependencies)

### Dependency Injection

- ✅ Register all services in DI container
- ✅ Use factories for platform-specific ViewModels
- ❌ **NEVER** use `new` keyword for services/ViewModels in UI layers

### Threading (CRITICAL for MAUI!)

⚠️ **Only Main Thread can modify UI properties!**

```csharp
// ❌ BAD: Background thread
private void OnZ21Event(Data data)
{
    StatusText = "Updated!";  // CRASH on Android!
}

// ✅ GOOD: Dispatched to UI thread
private void OnZ21Event(Data data)
{
    _uiDispatcher.InvokeOnUi(() =>
    {
        StatusText = "Updated!";  // Safe on all platforms
    });
}
```

### Documentation

- ✅ Add XML documentation for public APIs
- ✅ Use English for code and documentation
- ✅ Update relevant documentation files when making changes

### Testing

- ✅ Add unit tests for new features
- ✅ Use `ViewModelTestBase` for ViewModel tests
- ✅ Mock dependencies using `Moq`
- ✅ Ensure all tests pass before submitting PR

---

## 📚 Resources

For detailed guidelines, please refer to:
- [.copilot-instructions.md](.copilot-instructions.md) - Comprehensive coding guidelines
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture
- [docs/THREADING.md](docs/THREADING.md) - Threading best practices
- [docs/ASYNC-PATTERNS.md](docs/ASYNC-PATTERNS.md) - Async/await patterns
- [docs/DI-INSTRUCTIONS.md](docs/DI-INSTRUCTIONS.md) - Dependency injection

---

## 🐛 Reporting Bugs

### Before Submitting a Bug Report

- Check existing issues to avoid duplicates
- Ensure you're using the latest version
- Test with the Z21 simulator if hardware-related

### Bug Report Template

**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
A clear description of what you expected to happen.

**Screenshots**
If applicable, add screenshots to help explain your problem.

**Environment:**
- OS: [e.g., Windows 11, Android 14]
- .NET Version: [e.g., .NET 10]
- MOBAflow Version: [e.g., 1.0.0]
- Z21 Firmware: [if applicable]

---

## 💡 Feature Requests

Feature requests are welcome! Please open an issue with:
- Clear description of the feature
- Use case / motivation
- Proposed implementation (optional)

---

## 📄 License

By contributing to MOBAflow, you agree that your contributions will be licensed under the [MIT License](LICENSE).

---

## 🙏 Thank You!

Your contributions make MOBAflow better for everyone! 🚀

If you have questions, feel free to open an issue or reach out to the maintainers.

---

**Made with ❤️, .NET 10, and GitHub Copilot**
