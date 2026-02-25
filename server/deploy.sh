#!/bin/bash

# Nicodemouse Signaling Server Deployment Script
# Target: Ubuntu 24.04

# 1. Update and Install Docker
echo "[DEPLOY] Updating system..."
sudo apt-get update
sudo apt-get install -y docker.io docker-compose

# 2. Setup Project Directory
echo "[DEPLOY] Setting up directories..."
mkdir -p ~/app/server
cd ~/app/server

# 3. Create Docker-Compose
cat <<EOF > docker-compose.yml
version: '3.8'
services:
  signaling-server:
    build: .
    ports:
      - "8080:8080"
    restart: always
EOF

# 4. Instructions
echo "----------------------------------------------------"
echo "DEPLOYMENT PREPARED"
echo "1. Copy the 'server' folder from your local machine to ~/nicodemouse/ on the VM."
echo "2. Run 'cd ~/nicodemouse/server && sudo docker-compose up -d --build'"
echo "----------------------------------------------------"
