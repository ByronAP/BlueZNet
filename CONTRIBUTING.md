# Contributing to BlueZNet

First off, thank you for considering contributing to BlueZNet! It's people like you that make BlueZNet such a great tool for the Bluetooth development community.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How Can I Contribute?](#how-can-i-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Code Contributions](#code-contributions)
- [Development Setup](#development-setup)
- [Development Process](#development-process)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to [project maintainers].

### Our Standards

- **Be respectful and inclusive**: Value all contributions
- **Be patient**: Not everyone has the same experience level
- **Be constructive**: Focus on helping the project improve
- **Be open**: Embrace new ideas and approaches

## Getting Started

1. **Fork the repository** on GitHub
2. **Star the repository** to show your support! ?
3. **Clone your fork** locally
4. **Read the documentation** to understand the project
5. **Run the tests** to ensure everything works

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible.

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening)

#### Bug Report Template

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Create controller with '...'
2. Connect to device '...'
3. Call method '...'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Actual behavior**
What actually happened, including any error messages.

**Code sample**
Minimal code to reproduce the issue

**Environment (please complete the following):**
- OS: [e.g. Raspberry Pi OS 11]
- .NET Version: [e.g. 9.0.100]
- BlueZ Version: [e.g. 5.66]
- Device: [e.g. iPhone 13]
- BlueZNet Version: [e.g. 1.0.0]

**Logs**
Attach relevant debug logs

**Additional context**
Add any other context about the problem here.
```

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description** of the suggested enhancement
- **Provide specific examples** to demonstrate the steps
- **Describe the current behavior** and **explain which behavior you expected to see instead**
- **Explain why this enhancement would be useful** to most BlueZNet users

#### Feature Request Template

```markdown
**Is your feature request related to a problem? Please describe.**
A clear and concise description of what the problem is. Ex. I'm always frustrated when [...]

**Describe the solution you'd like**
A clear and concise description of what you want to happen.

**Describe alternatives you've considered**
A clear and concise description of any alternative solutions or features you've considered.

**Example API**
Show how you'd like to use the feature

**Additional context**
Add any other context or screenshots about the feature request here.
```

### Code Contributions

Unsure where to begin contributing? You can start by looking through these issues:

- `good-first-issue` - issues which should only require a few lines of code
- `help-wanted` - issues which need extra attention
- `documentation` - improvements or additions to documentation

## Development Setup

### Prerequisites

- Linux system with Bluetooth hardware (or Raspberry Pi)
- .NET SDK
- BlueZ 5.50+ with experimental features
- Git
- Your favorite C# IDE (Visual Studio Code, Rider, Visual Studio)

### Setting Up Your Development Environment

1. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR-USERNAME/BlueZNet.git
   cd BlueZNet
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/ByronAP/BlueZNet.git
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Install system dependencies**
   ```bash
   # Follow setup.md for BlueZ and PulseAudio configuration
   sudo apt install bluez bluez-tools pulseaudio pulseaudio-module-bluetooth
   ```

5. **Run tests**
   ```bash
   dotnet test
   ```

6. **Create a branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

### Project Structure

```
BlueZNet/
├── src/
│   ├── Interfaces/          # Public API and D-Bus interface definitions
│   │   └── DBus/            # BlueZ D-Bus protocol interfaces
│   ├── Services/            # Core service implementations
│   ├── Models/              # Data models and DTOs
│   │   ├── Device/          # Bluetooth device representations
│   │   ├── Media/           # Media player and content models
│   │   ├── Audio/           # Audio stream and codec models
│   │   └── Capabilities/    # Device capability information
│   ├── Events/              # Event argument classes for notifications
│   ├── Enums/               # Enumerations and constants
│   └── Exceptions/          # Custom exception types
├── tests/                   # Unit and integration tests
├── samples/                 # Example applications and usage demos
├── docs/                    # Documentation and API guides
├── assets/                  # Images, diagrams, and miscellaneous files
└── .github/                 # GitHub workflows and issue templates
```

## Development Process

1. **Check existing work**
   - Search issues and PRs first
   - Check the project board for planned features

2. **Discuss major changes**
   - Open an issue for discussion before starting major work
   - Get feedback on API design early

3. **Write code**
   - Follow the coding standards
   - Write tests for new functionality
   - Update documentation

4. **Test thoroughly**
   - Run unit tests
   - Run integration tests with real devices
   - Test on different Linux distributions if possible

5. **Submit PR**
   - Follow the pull request template
   - Link related issues
   - Be responsive to feedback

## Coding Standards

### C# Coding Conventions

We follow the [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with some additions:

#### Naming

```csharp
// Interfaces start with 'I'
public interface IBlueZNetController

// Private fields start with underscore
private readonly ILogger<BlueZNetControllerService> _logger;

// Constants are UPPER_CASE
private const string BLUEZ_SERVICE = "org.bluez";

// Async methods end with 'Async'
public async Task<bool> PlayAsync(string deviceAddress)
```

#### Structure

```csharp
// Order of class members:
public class ExampleClass
{
    // 1. Constants
    private const int DEFAULT_TIMEOUT = 5000;
    
    // 2. Static fields
    private static readonly Regex MacAddressRegex;
    
    // 3. Fields
    private readonly ILogger _logger;
    private bool _isRunning;
    
    // 4. Constructors
    public ExampleClass(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // 5. Properties
    public bool IsRunning => _isRunning;
    
    // 6. Events
    public event EventHandler<EventArgs> StateChanged;
    
    // 7. Public methods
    public async Task StartAsync()
    {
        // Implementation
    }
    
    // 8. Protected methods
    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    // 9. Private methods
    private void Initialize()
    {
        // Implementation
    }
}
```

#### Best Practices

```csharp
// Always use 'var' when type is obvious
var devices = new List<BluetoothDevice>();

// Use meaningful names
// Bad:
var d = GetDevices();
// Good:
var connectedDevices = GetConnectedDevicesAsync();

// Validate parameters
public void SetVolume(string deviceAddress, double volume)
{
    if (string.IsNullOrWhiteSpace(deviceAddress))
        throw new ArgumentException("Device address cannot be empty", nameof(deviceAddress));
        
    if (volume < 0.0 || volume > 1.0)
        throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
}

// Use cancellation tokens
public async Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    // Implementation
}

// Dispose resources properly
public void Dispose()
{
    if (!_disposed)
    {
        try
        {
            // Cleanup
        }
        finally
        {
            _disposed = true;
        }
    }
}
```

### Documentation Standards

```csharp
/// <summary>
/// Starts or resumes playback on the specified device.
/// </summary>
/// <param name="deviceAddress">The MAC address of the device.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>True if the command was sent successfully; otherwise, false.</returns>
/// <exception cref="ArgumentException">Thrown when deviceAddress is invalid.</exception>
/// <exception cref="InvalidOperationException">Thrown when monitoring is not active.</exception>
/// <remarks>
/// This method requires the device to support AVRCP playback commands.
/// Use <see cref="IsFeatureSupportedAsync"/> to check capability first.
/// </remarks>
/// <example>
/// <code>
/// var success = await controller.PlayAsync("AA:BB:CC:DD:EE:FF");
/// if (success)
/// {
///     Console.WriteLine("Playback started");
/// }
/// </code>
/// </example>
public async Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

### Logging Standards

```csharp
// Use structured logging
_logger.LogInformation("Device {DeviceName} ({DeviceAddress}) connected", 
    device.Name, device.Address);

// Log levels:
// - Trace: Method entry/exit
// - Debug: Detailed flow information
// - Information: Important events
// - Warning: Unexpected but handled situations
// - Error: Errors that don't stop the service
// - Critical: Errors that stop the service

// Include context
using (_logger.BeginScope("DeviceAddress: {DeviceAddress}", deviceAddress))
{
    _logger.LogDebug("Executing media command");
    // Operations
}
```

## Testing Guidelines

### Unit Tests

```csharp
[TestClass]
public class BlueZNetControllerServiceTests
{
    [TestMethod]
    public async Task StartMonitoringAsync_WhenNotRunning_StartsSuccessfully()
    {
        // Arrange
        var logger = new TestLogger<BlueZNetControllerService>();
        using var controller = new BlueZNetControllerService(logger);
        
        // Act
        await controller.StartMonitoringAsync();
        
        // Assert
        Assert.IsTrue(controller.IsMonitoring);
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task StartMonitoringAsync_WhenAlreadyRunning_ThrowsException()
    {
        // Arrange
        using var controller = new BlueZNetControllerService();
        await controller.StartMonitoringAsync();
        
        // Act
        await controller.StartMonitoringAsync(); // Should throw
    }
}
```

### Integration Tests

```csharp
[TestClass]
[TestCategory("Integration")]
public class DeviceIntegrationTests
{
    [TestMethod]
    public async Task RealDevice_PlayPause_WorksCorrectly()
    {
        // NOTE: Requires actual Bluetooth device
        // Skip in CI environment
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            Assert.Inconclusive("Skipping in CI environment");
        }
        
        // Test with real device
    }
}
```

### Test Coverage

- Aim for >90% code coverage
- Test error conditions
- Test edge cases
- Test cancellation scenarios
- Mock external dependencies

## Documentation

### Code Documentation

- All public APIs must have XML documentation
- Include examples for complex methods
- Document exceptions that can be thrown
- Explain non-obvious parameters

### Markdown Documentation

- Update README.md for new features
- Add examples to docs/examples/
- Update API documentation
- Include in troubleshooting guide if applicable

### Example Documentation

When adding a new feature, create an example:

```csharp
// docs/examples/FeatureName.md

# Feature Name Example
Brief description of what this example demonstrates.

## Prerequisites
- What's needed to run this example

## Code
// Complete, runnable example
using BlueZNet.Services;
// full example

## Expected Output
What the user should see

## Troubleshooting
Common issues and solutions
```

## Pull Request Process

### Before Submitting

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] Branch is up to date with main

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## How Has This Been Tested?
- [ ] Unit tests
- [ ] Integration tests
- [ ] Manual testing with device: [device name]

## Checklist
- [ ] My code follows the style guidelines
- [ ] I have performed a self-review
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally
- [ ] Any dependent changes have been merged and published

## Related Issues
Fixes #(issue number)
```

### Review Process

1. **Automated checks** must pass
2. **Code review** by at least one maintainer
3. **Testing** on real hardware when applicable
4. **Documentation review**
5. **Final approval** and merge

### After Your PR is Merged

- Delete your branch
- Pull the latest dev branch
- Thank you for contributing!

## Community

### Getting Help

- **GitHub Discussions**: Ask questions and share ideas
- **Issues**: Report bugs or request features

### Staying Updated

- Watch the repository for updates
- Subscribe to releases

### Recognition

Contributors are recognized in:
- The project README
- Release notes

## Questions?

Feel free to open an issue with the `question` label or start a discussion. We're here to help!

Thank you for contributing to BlueZNet!