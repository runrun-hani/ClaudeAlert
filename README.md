# ClaudeAlert

A delightful Windows desktop application that monitors Claude Code's task status and provides real-time visual feedback through physics-based character animations.

## 🎯 Features

- **Real-time Status Monitoring**: Track Claude Code's work status (Active, Done, Waiting for Input, Stuck, Error, etc.)
- **Physics-Based Animations**: A cute character responds with jump, roll, and bounce animations based on work state and duration
- **Escalation System**: Progressive animation intensity increases when tasks take too long (Jump → Roll → Bounce)
- **Multiple Event Sources**: Monitors via HTTP webhooks, log files, and session tracking
- **System Tray Integration**: Minimize to tray, quick access to controls
- **Toast Notifications & Sound Alerts**: Get notified of important state changes
- **Custom Image Support**: Replace the default character with your own PNG/GIF image
- **Auto-Configuration**: Automatically sets up Claude Code hooks with zero manual configuration
- **Fully Customizable**: Adjust timing thresholds, enable/disable sounds, and more

## 🛠 Tech Stack

| Component | Technology |
|-----------|-----------|
| **Platform** | .NET 7.0 for Windows |
| **UI Framework** | WPF (XAML) + Windows Forms |
| **Physics Engine** | Custom 2D physics simulation (gravity, friction, elasticity) |
| **Notifications** | Windows Toast Notifications API |
| **System Integration** | System Tray Icon, Win32 P/Invoke |
| **Language** | C# (nullable enabled, implicit usings) |

### Key Dependencies

- **Hardcodet.NotifyIcon.Wpf.NetCore** (1.1.5) - WPF system tray icon
- **Microsoft.Toolkit.Uwp.Notifications** (7.1.3) - Windows toast notifications
- **WpfAnimatedGif** (2.0.2) - GIF animation support

## 📋 Requirements

- Windows 10/11 or later
- .NET 7.0 Runtime (or build from source with .NET 7.0 SDK)
- Claude Code installed and configured

## 🚀 Installation

### From Release (Recommended)

1. Download the latest release from [Releases](https://github.com/hllee/ClaudeAlert/releases)
2. Extract to your preferred location
3. Run `ClaudeAlert.exe`
4. The application will automatically configure Claude Code hooks on first launch

### From Source

1. Clone the repository:
   ```bash
   git clone https://github.com/hllee/ClaudeAlert.git
   cd ClaudeAlert
   ```

2. Open the solution in Visual Studio 2022 (or later):
   ```bash
   start ClaudeAlert.sln
   ```

3. Build the solution (Build → Build Solution)

4. Run the application (Debug → Start Debugging or press F5)

## 📖 Usage

### First Launch

1. **Auto-Configuration**: ClaudeAlert automatically adds webhook hooks to your Claude Code configuration:
   - Adds HTTP POST hooks to `~/.claude/settings.json`
   - Listens on `localhost:19542/event` for status updates

2. **System Tray**: The application runs in the system tray. Click the icon to:
   - Show/Hide the overlay window
   - Access settings
   - Quit the application

### How It Works

```
Claude Code Task Started
         ↓
HTTP Webhook/Log Monitor detects "tool_use" event
         ↓
Status changes to "Active" → Character stands idle
         ↓
Task Completes (30s+ with no activity)
         ↓
Status changes to "Done" → Character starts jumping
         ↓
Still waiting after 60s?
         ↓
Character switches to rolling animation
         ↓
Still waiting after 180s?
         ↓
Character bounces across entire screen
```

### Configuration

Settings are saved in:
```
%USERPROFILE%\Documents\ClaudeAlert\config.json
```

Customizable options:
- **Port**: HTTP webhook listener port (default: 19542)
- **ImagePath**: Path to custom character image (PNG/GIF)
- **SoundEnabled**: Enable/disable audio notifications
- **Thresholds**: Timing for escalation levels (in seconds)

### Status States

| State | Meaning | Animation |
|-------|---------|-----------|
| **Idle** | Claude Code is ready | Stands still |
| **Active** | Processing a task | Stands still (no escalation) |
| **Done** | Task completed, no follow-up | Jumping (30s+) |
| **WaitingForInput** | Awaiting user response | Jumping (30s+) |
| **Stuck** | Active for 120s+ with no updates | Jumping (30s+) |
| **Error** | An error occurred | Jumping (30s+) |
| **Acknowledged** | User acknowledged (click on overlay) | Stops animation |

## 🏗 Architecture

### Core Components

```
ClaudeAlert/
├── Core/
│   ├── ClaudeStatusManager.cs    - State machine & event orchestration
│   ├── ClaudeState.cs            - State enum definition
│   ├── AppSettings.cs            - Configuration management
│   └── ClaudeEvent.cs            - Event record type
│
├── EventSources/
│   ├── HookHttpListener.cs       - HTTP webhook receiver
│   ├── LogFileWatcher.cs         - Claude Code log monitor
│   └── SessionFileMonitor.cs     - Session state tracking
│
├── Physics/
│   ├── PhysicsEngine.cs          - 2D physics simulation
│   ├── PhysicsBody.cs            - Physics properties
│   └── EscalationController.cs   - Animation escalation logic
│
├── Views/
│   ├── OverlayWindow.xaml        - Main UI overlay
│   └── SettingsWindow.xaml       - Settings dialog
│
├── Notifications/
│   ├── ToastNotifier.cs          - Windows notifications
│   └── SoundManager.cs           - Audio alerts
│
└── Setup/
    ├── HookConfigurator.cs       - Claude Code auto-setup
    ├── TrayIconManager.cs        - System tray integration
    ├── AutoStartManager.cs       - Startup registration
    └── FocusHelper.cs            - Window focus control
```

### Key Workflows

**Initialization (App.xaml.cs)**
1. Single-instance check (mutex)
2. Load AppSettings
3. Auto-configure Claude Code hooks
4. Start HTTP listener on localhost:19542
5. Initialize physics engine
6. Start monitoring Claude Code events
7. Register system tray icon

**State Transitions**
- `tool_use` event → Status = `Active`
- `stop` event → Status = `Done` (begins escalation)
- `permission_prompt` → Status = `WaitingForInput` (begins escalation)
- No events for 120s in Active state → Status = `Stuck`
- Error detected in logs → Status = `Error`

**Escalation Timeline**
- 0-30s: No animation (standing)
- 30s+: Jump (every 2.5s, 350-450px high)
- 60s+: Roll (every 3s, left-right movement)
- 180s+: Bounce (every 4s, full-screen bouncing)

## 🎮 Interaction

### Overlay Window
- **Click**: Acknowledge the current state (stops animation)
- **Drag**: Move the overlay window to any location
- **Right-click**: Open context menu (settings, close)

### Keyboard Shortcuts (Future)
- `Ctrl+Alt+C`: Toggle overlay visibility
- `Esc`: Hide overlay

## 📊 Event Sources

ClaudeAlert monitors Claude Code through multiple channels:

1. **HTTP Webhook** (Primary)
   - POST requests to `localhost:19542/event`
   - Events: `tool_use`, `stop`, `permission_prompt`, `idle_prompt`

2. **Log File Monitoring** (Secondary)
   - Watches Claude Code log for `[error]` and `[fatal]` patterns
   - Triggers Error state on detection

3. **Session File Monitoring** (Tertiary)
   - Tracks active session changes

## 🐛 Troubleshooting

### Application doesn't detect Claude Code events

1. Check if Claude Code is running
2. Verify HTTP listener is active (check Windows Firewall settings)
3. Manually add hooks to `~/.claude/settings.json`:
   ```json
   {
     "hooks": {
       "stop": "curl -X POST http://localhost:19542/event -H 'Content-Type: application/json' -d '{\"type\":\"stop\"}'"
     }
   }
   ```

### Custom image not loading

1. Verify image path exists in `config.json`
2. Supported formats: PNG, GIF (animated)
3. Recommended size: 100x100 to 200x200 pixels
4. Try absolute path instead of relative path

### Application crashes on startup

1. Ensure .NET 7.0 Runtime is installed
2. Check if another instance is already running
3. Delete `%USERPROFILE%\Documents\ClaudeAlert\config.json` and restart

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. **Report Bugs**: Open an issue with:
   - Windows version
   - .NET Runtime version
   - Steps to reproduce
   - Expected vs actual behavior

2. **Suggest Features**: Describe your idea and use case

3. **Submit Pull Requests**:
   - Fork the repository
   - Create a feature branch (`git checkout -b feature/amazing-feature`)
   - Commit your changes (`git commit -m 'Add amazing feature'`)
   - Push to your branch (`git push origin feature/amazing-feature`)
   - Open a Pull Request

Please ensure code follows existing style conventions and includes appropriate comments.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**TL;DR**: You can use, modify, and distribute this software freely, including for commercial purposes, as long as you include the original license notice.

## 🙏 Acknowledgments

- Built with [WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/) and [.NET 7.0](https://dotnet.microsoft.com/)
- Physics simulation inspired by game development practices
- Special thanks to the Claude Code community for feedback and ideas

## 📞 Support

- **Issues & Bug Reports**: [GitHub Issues](https://github.com/hllee/ClaudeAlert/issues)
- **Discussions**: [GitHub Discussions](https://github.com/hllee/ClaudeAlert/discussions)
- **Email**: Open an issue on GitHub for quickest response

## 🔮 Roadmap

- [ ] Customizable animation presets (jumping styles, speeds)
- [ ] Discord/Slack notifications for remote monitoring
- [ ] Multi-monitor support improvements
- [ ] Custom sound effects/alerts
- [ ] Statistics dashboard (task duration, frequency)
- [ ] Dark mode for overlay
- [ ] Keyboard shortcuts configuration
- [ ] Cross-platform support (macOS, Linux) - in research phase

---

**Enjoy monitoring your Claude Code tasks with ClaudeAlert!** 🎉
