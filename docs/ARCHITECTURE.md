# nicodemouse Architecture & Features

nicodemouse is a high-performance, cross-platform universal control application that allows seamless sharing of mouse, keyboard, clipboard, and audio between computers (Windows and macOS) over a local network.

## 1. System Architecture

nicodemouse uses a hybrid architecture combining a high-performance **.NET 8** backend with a modern **React** frontend, hosted via **Photino.NET** (a lightweight Webview2 wrapper).

### High-Level Flow
1. **Frontend (React)**: Handles the UI, state representation, and user interactions.
2. **Web Message Bridge**: Communication between React and C# via `window.external.sendMessage`.
3. **Backend (C#)**: Manages low-level OS operations (input injection, clipboard monitoring, audio capture) and network communication.

---

## 2. Backend Services (C#)

The backend is organized into specialized services coordinated by a central manager.

### `UniversalControlManager` (The Orchestrator)
The central "Brain" of the application. It:
- Orchestrates all other services.
- Manages the application state (Connected, Disconnected, Controlling, Controlled).
- Handles message routing between the UI and the network.

### `NetworkService` (TCP/SSL Transport)
Provides a robust, encrypted communication channel:
- **TCP based**: Uses a custom 4-byte big-endian length-prefixed framing protocol.
- **Security**: Upgrades every connection to **SSL/TLS** using self-signed certificates.
- **Bi-directional**: Supports sending and receiving packets on the same socket.

### `DiscoveryService` (UDP Discovery)
Handles automatic device detection on the local network (LAN):
- **UDP Broadcast**: Periodically broadcasts device identity (Machine Name, IP, Version).
- **Security PIN**: Includes a 6-character custom PIN in the broadcast for safe pairing.
- **Zero-Config**: Allows devices to find each other without manual IP entry.

### `InputService` & `InjectionService`
Handles the capture and injection of mouse and keyboard events:
- **Windows**: Uses `SendInput` and low-level global hooks.
- **macOS**: Uses `CGEvent` for native input manipulation.
- **Features**: Supports relative mouse movement, scroll wheel, and complex key combinations.

### `ClipboardService`
Provides instant clipboard synchronization:
- **Event-Driven**: Monitors the OS clipboard for changes.
- **Push/Pull**: Automatically syncs text when a change is detected on either side.

### `AudioService`
Captures system audio for low-latency streaming:
- **Windows**: Uses WASAPI (Loopback) to capture "What You Hear".
- **Streaming**: Compresses/Encodes frames for network transmission.

---

## 3. Frontend Architecture (React)

The frontend is a modular Single Page Application (SPA).

### `nicodemouseContext`
A centralized React Context that acts as the bridge to the backend:
- **Message Listener**: Receives and parses messages from Photino.
- **Global State**: Manages connection status, discovered devices, and local machine info.
- **Encapsulation**: Components call `sendMessage` or `updateSettings` without knowing the underlying protocol.

### UI Components
- **Sidebar**: Dynamic navigation that adjusts visibility based on connection state.
- **Overview**: High-level status and quick-connect dashboard.
- **Discovery**: Real-time list of nearby devices found via UDP.
- **Active Device**: Advanced controls for the current session (Input locking, Service toggles).
- **Settings**: System-wide configuration and Custom PIN management.

---

## 4. Communication Protocol

nicodemouse uses a custom binary protocol optimized for low latency.

### Packet Structure
`[4 Bytes: Length] [1 Byte: Type] [N Bytes: Payload]`

### Main Packet Types
| Type ID | Name | Description |
| :--- | :--- | :--- |
| `0` | `MouseMove` | Absolute mouse positioning. |
| `5` | `Handshake` | Initial authentication and PIN verification. |
| `9` | `MouseWheel` | Vertical and horizontal scroll events. |
| `12` | `MouseRelMove` | High-frequency relative mouse delta (Used for gaming/precision). |
| `13` | `ClipboardPush`| Synchronizes text between clipboards. |
| `15` | `Disconnect` | Graceful session termination. |

---

## 5. Core Features

### 🚀 Seamless Edge Crossing
The mouse cursor "crosses" between monitors of different computers as if they were a single desktop.
- **Active Edge**: Configurable (Left/Right/Top/Bottom).
- **Return Gesture**: Snap-back logic to regain local control.

### 🔐 Custom PIN Security
Every machine generates or allows setting a 6-character security PIN.
- **Encryption**: All data is encrypted via TLS.
- **Authentication**: Connection is only accepted if the handshake PIN matches the local settings.

### 🛠️ Input & Service Control
- **Input Locking**: Prevents accidental movement on the local machine while controlling a remote one.
- **Granular Toggles**: Enable/Disable Clipboard, Audio, or Input sharing independently during a session.

---

## 6. Project Structure

```text
/
├── backend/                # .NET 8 Source Code
│   ├── Services/           # Domain Logic (Network, Input, etc.)
│   └── Program.cs          # Photino Entry Point
├── frontend/               # React + Vite Source Code
│   ├── src/components/     # UI Components (Tabs, Layout)
│   └── src/context/        # nicodemouseProvider & Bridge
└── settings.json           # Local persistence (Generated)
```
