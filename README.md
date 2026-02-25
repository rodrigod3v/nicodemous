# 👁️ nicodemouse

**Universal Control & High-Fidelity Audio Sharing**

nicodemouse is a high-performance, cross-platform bridge designed to unify your digital workspace. It allows you to share mouse control, keyboard input, clipboard data, and system audio between devices on a local network with near-zero latency.

---

## 🚀 The Vision

In a world of multiple devices, nicodemouse aims to make transitions invisible. Whether you are controlling a secondary machine or streaming high-fidelity audio across your setup, nicodemouse provides the "gravity" that keeps your workflow grounded and fluid.

## ✨ Core Features

### 🖱️ Universal Input Hook
- **Zero-Lag Binary Protocol**: Proprietary binary serialization reduces packet size by 80% vs JSON, achieving sub-millisecond input latency.
- **Cross-Platform**: Native capture and injection on Windows and macOS.

### 🎧 Audio Loopback Streaming
- **Opus Compression**: High-fidelity audio capture with intelligent bitrate management.
- **Real-time Sync**: Low-latency system audio sharing for a unified sound experience.

### 📋 Clipboard & File synchronization
- **Instant Clipboard**: Automatic synchronization of text and binary clipboard data.
- **Secure File Transfer**: High-speed TCP streaming for dragging and dropping files between machines.

### 🛰️ Connectivity & Discovery
- **Pairing Code System**: Each instance generates a unique local "Pairing Code" (IP) for instant manual link.
- **mDNS/Zeroconf**: Automatic background discovery for a "it just works" experience.
- **Universal Mode**: A single binary acts as either Controller (Server) or Receiver (Client) dynamically.

---

## 🛠️ Technical Architecture

nicodemouse is built on a modern, distributed architecture combining the power of .NET with the elegance of React.

-   **Backend**: [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) + C#
    -   **GUI Layer**: [Photino.NET](https://www.tryphotino.io/) (Lightweight native windowing)
    -   **Networking**: UDP for real-time input/audio, TCP for reliable file/clipboard streams.
    -   **Audio Proccessing**: Opus codec for low-latency compression.
-   **Frontend**: [React 19](https://reactjs.org/) + [Vite](https://vitejs.dev/)
    -   **UI Design**: Modern Glassmorphism aesthetic using [Framer Motion](https://www.framer.com/motion/) and [Lucide React](https://lucide.dev/).
    -   **State Management**: Real-time service status monitoring.

---

## 📂 Project Structure

```text
nicodemouse/
├── backend/            # .NET 8 Core Services
│   ├── Services/       # Input, Audio, Discovery, and Network layers
│   └── Program.cs      # Photino Host Entry Point
├── frontend/           # React + Vite Premium UI
│   ├── src/            # Components, Hooks, and Styles
│   └── public/         # Static Assets
└── .agent/             # Project blueprints and AI context
```

For a detailed technical breakdown of services, protocol, and internal logic, see the [Architecture Documentation](ARCHITECTURE.md).
For information on how to deploy the signaling server and distribute the application, see the [Deployment Guide](DEPLOYMENT.md).

---

## 🛠️ Getting Started

### Prerequisites
- **.NET 8 SDK**
- **Node.js** (v18 or higher)
- **NPM** or **PNPM**

### Setup & Run

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/rodrigod3v/nicodemouse.git
    cd nicodemouse
    ```

2.  **Start the Frontend (Vite)**:
    ```bash
    cd frontend
    npm install
    npm run dev
    ```

3.  **Run the Backend (Photino)**:
    ```bash
    cd ../backend
    dotnet run
    ```

---

## 🛡️ License
[MIT License](LICENSE) — Feel free to use and contribute!

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request or open an issue for any feature requests or bugs.

---
*Created with ❤️ by rodrigod3v*
