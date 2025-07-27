# Contributing to Blazor WhyDidYouRender

Thank you for your interest in contributing to Blazor WhyDidYouRender! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

### Reporting Issues

Before creating an issue, please:

1. **Search existing issues** to avoid duplicates
2. **Use the issue templates** when available
3. **Provide clear reproduction steps** for bugs
4. **Include environment information** (.NET version, browser, etc.)

#### Bug Reports

Include the following information:
- **Description**: Clear description of the issue
- **Steps to Reproduce**: Minimal steps to reproduce the problem
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Environment**: 
  - .NET version
  - Blazor hosting model (Server/WebAssembly)
  - Browser and version
  - Operating system

#### Feature Requests

Include the following information:
- **Description**: Clear description of the proposed feature
- **Use Case**: Why this feature would be useful
- **Proposed Implementation**: If you have ideas about implementation
- **Alternatives**: Any alternative solutions you've considered

### Pull Requests

1. **Fork the repository** and create a feature branch
2. **Follow coding standards** (see below)
3. **Add tests** for new functionality (when testing framework compatibility is resolved)
4. **Update documentation** as needed
5. **Ensure all builds pass**
6. **Create a clear pull request description**

#### Pull Request Process

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make your changes following the coding standards
3. Test your changes thoroughly
4. Update documentation if needed
5. Commit with clear, descriptive messages
6. Push to your fork: `git push origin feature/your-feature-name`
7. Create a pull request with a clear description

## 📝 Coding Standards

### C# Code Style

Follow standard C# conventions:

```csharp
// ✅ Good: PascalCase for public members
public class RenderTrackerService
{
    public void TrackRender(ComponentBase component, string trigger)
    {
        // Implementation
    }
}

// ✅ Good: camelCase with underscore prefix for private fields
private readonly ILogger _logger;
private bool _isEnabled;

// ✅ Good: Descriptive names
public record ParameterChange
{
    public string Name { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}
```

### Documentation Standards

- **XML Documentation**: All public APIs must have XML documentation
- **Code Comments**: Complex logic should be commented
- **README Updates**: Update README.md for new features

```csharp
/// <summary>
/// Tracks a render event for the specified component.
/// </summary>
/// <param name="component">The component that rendered.</param>
/// <param name="trigger">The method that triggered the render.</param>
/// <param name="firstRender">Whether this is the first render of the component.</param>
public void TrackRender(ComponentBase component, string trigger, bool firstRender)
{
    // Implementation with clear comments for complex logic
}
```

### Project Structure

```
Blazor.WhyDidYouRender/
├── Components/          # Component base classes
├── Configuration/       # Configuration classes
├── Core/               # Core tracking services
├── Diagnostics/        # Error tracking and diagnostics
├── Extensions/         # Service collection extensions
├── Helpers/            # Utility classes
└── Records/            # Data transfer objects
```

## 🧪 Testing Guidelines

**Note**: Testing is currently deferred due to .NET 9.0 compatibility issues with testing frameworks.

When testing becomes available:

### Unit Tests
- Test all public APIs
- Test edge cases and error conditions
- Use descriptive test names
- Follow Arrange-Act-Assert pattern

### Integration Tests
- Test service registration
- Test end-to-end workflows
- Test with sample Blazor applications

### Test Naming Convention
```csharp
[Test]
public void TrackRender_WhenComponentIsNull_ShouldThrowArgumentNullException()
{
    // Arrange
    var service = new RenderTrackerService();
    
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => 
        service.TrackRender(null!, "test", false));
}
```

## 🔄 Development Workflow

### Setting Up Development Environment

1. **Clone the repository**:
```bash
git clone https://github.com/your-username/blazor-why-did-you-render.git
cd blazor-why-did-you-render
```

2. **Install .NET 9.0 SDK**:
Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

3. **Restore packages**:
```bash
dotnet restore
```

4. **Build the solution**:
```bash
dotnet build
```

5. **Run the sample application**:
```bash
cd RenderTracker.SampleApp
dotnet run
```

### Making Changes

1. **Create a feature branch**:
```bash
git checkout -b feature/your-feature-name
```

2. **Make your changes** following the coding standards

3. **Test your changes** with the sample application

4. **Update documentation** if needed

5. **Commit your changes**:
```bash
git add .
git commit -m "feat: add new feature description"
```

### Commit Message Format

Use conventional commit format:

- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

Examples:
```
feat: add parameter change detection
fix: resolve null reference in render tracking
docs: update integration guide
refactor: improve performance tracker efficiency
```

## 📚 Documentation Contributions

### Types of Documentation

1. **API Documentation**: XML comments and API reference
2. **User Guides**: Integration guides and tutorials
3. **Examples**: Code samples and best practices
4. **Architecture**: Design decisions and technical details

### Documentation Standards

- **Clear and Concise**: Write for developers of all skill levels
- **Code Examples**: Include working code samples
- **Up-to-Date**: Keep documentation synchronized with code changes
- **Consistent Style**: Follow existing documentation patterns

## 🐛 Debugging and Troubleshooting

### Common Development Issues

1. **Build Failures**: Ensure .NET 9.0 SDK is installed
2. **Missing Dependencies**: Run `dotnet restore`
3. **Sample App Issues**: Check configuration in `Program.cs`

### Debugging Tips

1. **Use the Sample App**: Test changes with the included sample application
2. **Browser Console**: Monitor console output for tracking information
3. **Breakpoints**: Use debugger to step through tracking logic
4. **Logging**: Add temporary logging for complex debugging scenarios

## 🚀 Release Process

### Version Numbering

We follow [Semantic Versioning](https://semver.org/):
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes (backward compatible)

### Release Checklist

- [ ] All tests pass (when available)
- [ ] Documentation is updated
- [ ] Version number is bumped
- [ ] Release notes are prepared
- [ ] Sample application works correctly

## 📞 Getting Help

### Communication Channels

- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Pull Request Comments**: For code review discussions

### Code Review Process

1. **Automated Checks**: Ensure all CI checks pass
2. **Peer Review**: At least one maintainer review required
3. **Documentation Review**: Verify documentation is updated
4. **Testing**: Manual testing with sample application

## 🎯 Contribution Areas

We welcome contributions in these areas:

### High Priority
- **Performance Optimizations**: Improve tracking efficiency
- **Browser Compatibility**: Ensure cross-browser support
- **Error Handling**: Robust error handling and recovery

### Medium Priority
- **Additional Metrics**: New performance metrics and insights
- **Configuration Options**: More granular configuration
- **Documentation**: Examples and tutorials

### Future Enhancements
- **Visual Studio Extension**: IDE integration
- **React DevTools Integration**: Familiar debugging experience
- **Advanced Analytics**: Pattern detection and recommendations

## 📋 Contributor License Agreement

By contributing to this project, you agree that your contributions will be licensed under the same license as the project (GNU General Public License v3.0 or later).

## 🙏 Recognition

Contributors will be recognized in:
- **README.md**: Contributors section
- **Release Notes**: Acknowledgment of contributions
- **GitHub**: Contributor statistics and recognition

Thank you for contributing to Blazor WhyDidYouRender! 🎉
