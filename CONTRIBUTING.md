# Contributing to ClaudeAlert

First off, thanks for taking the time to contribute! 🎉

The following is a set of guidelines for contributing to ClaudeAlert. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue list as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* **Use a clear and descriptive title**
* **Describe the exact steps which reproduce the problem** in as much detail as possible
* **Provide specific examples to demonstrate the steps** Include links to files or GitHub projects, or copy/pasteable snippets
* **Describe the behavior you observed after following the steps** and point out what exactly is the problem with that behavior
* **Explain which behavior you expected to see instead and why.**
* **Include screenshots and animated GIFs if possible**
* **Include your environment details:**
  - Windows version and build number
  - .NET Runtime version (`dotnet --version`)
  - ClaudeAlert version
  - Claude Code version

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

* **Use a clear and descriptive title** for the issue to identify the suggestion
* **Provide a step-by-step description of the suggested enhancement** in as much detail as possible
* **Provide specific examples to demonstrate the steps**
* **Describe the current behavior** and **explain the expected behavior** and why that would be better
* **Include screenshots and animated GIFs** if you can, to help you demonstrate the steps or point out the part of ClaudeAlert which the suggestion is related to
* **Explain why this enhancement would be useful** to most ClaudeAlert users

### Pull Requests

* Fill in the required template
* Follow the C# style guidelines (see below)
* Include appropriate test cases
* Document new code as per the style guide
* End all files with a newline

## Style Guidelines

### C# Style Guide

We follow the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and Microsoft's [Code Style Rules](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/).

Key points:

1. **Naming Conventions:**
   - PascalCase for class names, method names, and public properties
   - camelCase for private fields and local variables
   - Use meaningful names that describe the purpose

2. **Formatting:**
   - Use 4 spaces for indentation (not tabs)
   - Limit lines to 120 characters where possible
   - One statement per line

3. **Comments:**
   - Use `//` for single-line comments
   - Use `///` for XML documentation comments on public members
   - Comments should explain the "why", not the "what"

4. **Examples:**

```csharp
// Good
public class ClaudeStatusManager
{
    private readonly IClaudeEventSource _eventSource;

    public ClaudeState CurrentState { get; private set; }

    /// <summary>
    /// Processes an incoming Claude event and updates the internal state.
    /// </summary>
    public void ProcessEvent(ClaudeEvent e)
    {
        // Implementation
    }
}

// Avoid
public class claudeStatusManager  // Wrong: should be PascalCase
{
    public IClaudeEventSource eventSource; // Wrong: should be private field with _prefix

    public void processEvent(ClaudeEvent e) // Wrong: should be PascalCase
    {
        // ...
    }
}
```

### Git Commit Messages

* Use the present tense ("Add feature" not "Added feature")
* Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line
* Consider starting the commit message with an applicable emoji:
  - 🎨 `:art:` when improving the format/structure of the code
  - 🐛 `:bug:` when fixing a bug
  - ✨ `:sparkles:` when introducing a new feature
  - 📝 `:memo:` when writing docs
  - ♻️ `:recycle:` when refactoring code
  - 🧪 `:test_tube:` when adding tests
  - 🔒 `:lock:` when dealing with security
  - ⬆️ `:arrow_up:` when upgrading dependencies
  - ⬇️ `:arrow_down:` when downgrading dependencies

### Documentation

* Use [Markdown](https://daringfireball.net/projects/markdown/) for documentation
* Include code examples where applicable
* Keep documentation up-to-date with code changes

## Development Setup

### Prerequisites

- Windows 10/11 or later
- Visual Studio 2022 (Community edition is free) or Visual Studio Code with C# extension
- .NET 7.0 SDK
- Git

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/hllee/ClaudeAlert.git
cd ClaudeAlert
```

2. Open in Visual Studio or build from command line:
```bash
dotnet build
```

3. Run in Visual Studio (F5) or from command line:
```bash
dotnet run --project src/ClaudeAlert/ClaudeAlert
```

### Running Tests

Currently, ClaudeAlert has limited test coverage. Tests can be run using:
```bash
dotnet test
```

We welcome contributions to improve test coverage!

## Pull Request Process

1. Fork the repository and create your feature branch (`git checkout -b feature/AmazingFeature`)
2. Commit your changes (`git commit -m '✨ Add AmazingFeature'`)
3. Push to the branch (`git push origin feature/AmazingFeature`)
4. Open a Pull Request with a clear title and description
5. Link any related issues in the PR description
6. Ensure all checks pass (CI pipeline, code style, etc.)
7. Request review from maintainers
8. Address review feedback and push additional commits

## Additional Notes

### Issue and Pull Request Labels

This section lists the labels we use to help organize and categorize issues and pull requests.

* `bug` - Something isn't working
* `enhancement` - New feature or request
* `documentation` - Improvements or additions to documentation
* `good first issue` - Good for newcomers
* `help wanted` - Extra attention is needed
* `question` - Further information is requested
* `wontfix` - This will not be worked on

## Recognition

Contributors will be recognized in:
- Project README (Contributors section - to be added)
- Release notes for contributed features/fixes

## Questions?

Feel free to open a GitHub Discussion or issue with your question. We're here to help!

Thank you for contributing to ClaudeAlert! 🚀
