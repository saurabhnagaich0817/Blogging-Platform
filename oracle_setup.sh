#!/bin/bash

# InkWell Oracle Cloud Setup Script
# This script installs Docker, Docker Compose, and prepares the environment.

echo "🚀 Starting InkWell Setup on Oracle Cloud..."

# 1. Update system
sudo apt-get update
sudo apt-get upgrade -y

# 2. Install Docker
if ! command -v docker &> /dev/null
then
    echo "🐳 Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
fi

# 3. Install Docker Compose
if ! command -v docker-compose &> /dev/null
then
    echo "🐙 Installing Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# 4. Open Firewall Ports (Oracle specific)
echo "🛡️ Opening Firewall Ports (80, 5000)..."
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 5000 -j ACCEPT
sudo netfilter-persistent save

echo "✅ Setup Complete! Now you can run: docker-compose up -d"
