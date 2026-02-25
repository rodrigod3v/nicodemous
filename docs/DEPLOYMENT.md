# Nicodemouse Deployment Guide

Detailed documentation for deploying the **Signaling Server** and distributing the **Local Client**.

## 🏗️ Architecture Overview

Nicodemouse uses a hybrid architecture to balance ease of discovery with high-performance control:

-   **Cloud (Signaling Server)**: A centralized registry running on a public VM. Its only job is to pair devices via 6-digit codes. It does **not** process input data.
-   **Local (Client App)**: The P2P engine that runs on user machines. It uses the Signaling Server to find the IP of the remote machine and then establishes a direct, encrypted TLS connection.

---

## ☁️ VM Infrastructure (Oracle Cloud)

The signaling server is hosted on the following infrastructure:

### Instance Specifications
-   **Instance Name**: `accounting-control-system-instance`
-   **Region**: `sa-saopaulo-1` (Brazil East – São Paulo)
-   **OS**: Ubuntu 24.04 (Canonical-Ubuntu-24.04-2025.10.31-0)
-   **Shape**: `VM.Standard.E2.1.Micro` (1 OCPU, 1 GB RAM)
-   **Storage**: Block Storage (Paravirtualized, encrypted in transit)
-   **Network**: 0.48 Gbps bandwidth

### Connection Details
-   **Public IP**: `144.22.254.132`
-   **User**: `ubuntu`
-   **Auth**: SSH Key (Stored at `C:\Users\777\Documents\.conti\ssh-key-2026-01-26.key`)
-   **VPC / VCN**: `vcn-20260116-1106`

---

## 🚀 Server-Side Deployment (Ubuntu VM)

### 1. Prerequisites
Ensure Docker is installed on the VM. The provided deployment script handles this.

### 2. Steps to Deploy
1.  **Transfer Files**: Copy the `server/` directory to the VM:
    ```bash
    scp -i "C:\Users\777\Documents\.conti\ssh-key-2026-01-26.key" -r ./server ubuntu@144.22.254.132:~/app/
    ```
2.  **Run Deploy Script**:
    ```bash
    ssh -i "C:\Users\777\Documents\.conti\ssh-key-2026-01-26.key" ubuntu@144.22.254.132 "cd ~/app/server && chmod +x deploy.sh && sudo ./deploy.sh"
    ```
3.  **Start Container**:
    ```bash
    ssh -i "C:\Users\777\Documents\.conti\ssh-key-2026-01-26.key" ubuntu@144.22.254.132 "cd ~/app/server && sudo docker-compose up -d --build"
    ```

### 3. Firewall Configuration (Security Lists)
You MUST open the following port in Oracle Cloud Console (Ingress Rules):
-   **Protocol**: TCP
-   **Port Range**: `8080`
-   **Source**: `0.0.0.0/0` (Public Access for signaling)

---

## 💻 Local App Distribution

### 1. Build Process
To generate a distribution package for users:
1.  Open PowerShell in the project root.
2.  Run the release script:
    ```powershell
    ./release.ps1
    ```
3.  The package will be generated at `releases/nicodemouse-release.zip`.

### 2. Content of the Package
-   `bin/nicodemouse_backend.exe`: The P2P engine.
-   `bin/wwwroot/`: Optimized frontend files.
-   `bin/settings.json`: Configuration file.

### 3. Client Requirements (Developer/Receiver)
-   **Port 8890 (TCP)**: The machines must be able to communicate on this port. If on different networks, port forwarding on the Router (NAT) might be required unless a Relay server is implemented in the future.

---

## 🛠️ Maintenance & Monitoring

-   **Server Logs**: `sudo docker logs -f signaling-server`
-   **App Settings**: The app defaults to `http://144.22.254.132:8080` for signaling. This can be changed in `settings.json`.
-   **Pairing Registry**: The registry is stored in memory. If the server restarts, clients will re-register within 30 seconds (Heartbeat).
