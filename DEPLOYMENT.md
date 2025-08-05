# 🚀 JobCompare Droplet Deployment Guide

Complete step-by-step guide to deploy your JobCompare application on a DigitalOcean droplet.

## 📋 Prerequisites

### Droplet Requirements
- **OS**: Ubuntu 20.04 LTS or 22.04 LTS
- **RAM**: Minimum 2GB (4GB recommended)
- **Storage**: Minimum 25GB SSD
- **CPU**: 1 vCPU minimum (2 vCPU recommended)

## 🔧 Step 1: Initial Droplet Setup

### Connect to Your Droplet
```bash
# SSH into your droplet (replace YOUR_DROPLET_IP with actual IP)
ssh root@YOUR_DROPLET_IP
```

### Update System Packages
```bash
# Update package lists and upgrade system
sudo apt update && sudo apt upgrade -y

# Install essential packages
sudo apt install -y curl wget git unzip software-properties-common apt-transport-https
```

## 🔷 Step 2: Install .NET 8 SDK

### Add Microsoft Package Repository
```bash
# Download Microsoft signing key
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

# Install Microsoft signing key and repository
sudo dpkg -i packages-microsoft-prod.deb

# Update package lists
sudo apt update
```

### Install .NET 8 SDK
```bash
# Install .NET 8 SDK
sudo apt install -y dotnet-sdk-8.0

# Verify installation
dotnet --version
# Should output: 8.0.x
```

### Verify .NET Installation
```bash
# Check installed SDKs
dotnet --list-sdks

# Check installed runtimes
dotnet --list-runtimes
```

## 🌐 Step 3: Install and Configure Nginx (Reverse Proxy)

### Install Nginx
```bash
# Install Nginx
sudo apt install -y nginx

# Start and enable Nginx
sudo systemctl start nginx
sudo systemctl enable nginx

# Check status
sudo systemctl status nginx
```

### Configure Firewall
```bash
# Allow Nginx through firewall
sudo ufw allow 'Nginx Full'
sudo ufw allow ssh
sudo ufw --force enable

# Check firewall status
sudo ufw status
```

## 📦 Step 4: Clone Your Repository

### Navigate to Web Directory
```bash
# Create application directory
sudo mkdir -p /var/www
cd /var/www

# Clone your repository
sudo git clone https://github.com/nwbarkeriu/ncwforms.git jobcompare

# Change ownership to current user
sudo chown -R $USER:$USER /var/www/jobcompare

# Navigate to project directory
cd /var/www/jobcompare
```

### Verify Project Structure
```bash
# List project files
ls -la

# You should see:
# - JobCompare.csproj
# - Program.cs
# - Models/
# - Services/
# - Pages/
# - etc.
```

## 🔨 Step 5: Build and Test the Application

### Restore Dependencies
```bash
# Navigate to project directory
cd /var/www/jobcompare

# Restore NuGet packages
dotnet restore

# Should complete without errors
```

### Build the Application
```bash
# Build the project
dotnet build --configuration Release

# Should complete with "Build succeeded"
```

### Test Run (Optional)
```bash
# Test the application locally
dotnet run --urls="http://localhost:5000"

# Press Ctrl+C to stop after testing
# Test by opening another terminal and running:
# curl http://localhost:5000
```

## 🔧 Step 6: Configure Production Settings

### Update Application Settings
```bash
# Edit production settings
sudo nano appsettings.json
```

Update the content:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

### Create Production Configuration
```bash
# Create production appsettings
sudo nano appsettings.Production.json
```

Add production settings:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

## 🔄 Step 7: Create Systemd Service

### Create Service File
```bash
# Create systemd service file
sudo nano /etc/systemd/system/jobcompare.service
```

Add service configuration:
```ini
[Unit]
Description=JobCompare Reconciliation Dashboard
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/jobcompare/bin/Release/net8.0/JobCompare.dll
Restart=always
RestartSec=5
TimeoutStopSec=90
KillMode=mixed
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
WorkingDirectory=/var/www/jobcompare

[Install]
WantedBy=multi-user.target
```

### Set Permissions and Start Service
```bash
# Set correct permissions
sudo chown -R www-data:www-data /var/www/jobcompare

# Reload systemd and start service
sudo systemctl daemon-reload
sudo systemctl enable jobcompare
sudo systemctl start jobcompare

# Check service status
sudo systemctl status jobcompare
```

## 🌐 Step 8: Configure Nginx Reverse Proxy

### Create Nginx Configuration
```bash
# Create Nginx site configuration
sudo nano /etc/nginx/sites-available/jobcompare
```

Add Nginx configuration:
```nginx
server {
    listen 80;
    server_name YOUR_DOMAIN_OR_IP;  # Replace with your domain or droplet IP
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Enable the Site
```bash
# Enable the site
sudo ln -s /etc/nginx/sites-available/jobcompare /etc/nginx/sites-enabled/

# Remove default site
sudo rm /etc/nginx/sites-enabled/default

# Test Nginx configuration
sudo nginx -t

# Should output: "syntax is ok" and "test is successful"

# Reload Nginx
sudo systemctl reload nginx
```

## 🔍 Step 9: Verify Deployment

### Check All Services
```bash
# Check JobCompare service
sudo systemctl status jobcompare

# Check Nginx service
sudo systemctl status nginx

# Check application logs
sudo journalctl -u jobcompare -f
```

### Test the Application
```bash
# Test locally
curl http://localhost

# Test externally (replace with your droplet IP)
curl http://YOUR_DROPLET_IP
```

## 🔒 Step 10: Optional - SSL Certificate (Let's Encrypt)

### Install Certbot
```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get SSL certificate (replace YOUR_DOMAIN with actual domain)
sudo certbot --nginx -d YOUR_DOMAIN

# Test automatic renewal
sudo certbot renew --dry-run
```

## 📊 Step 11: Monitoring and Maintenance

### View Application Logs
```bash
# Real-time logs
sudo journalctl -u jobcompare -f

# Recent logs
sudo journalctl -u jobcompare --since "1 hour ago"
```

### Restart Services
```bash
# Restart JobCompare
sudo systemctl restart jobcompare

# Restart Nginx
sudo systemctl restart nginx
```

### Update Application
```bash
# Navigate to project directory
cd /var/www/jobcompare

# Pull latest changes
git pull origin main

# Rebuild application
dotnet build --configuration Release

# Restart service
sudo systemctl restart jobcompare
```

## 🚨 Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check detailed error logs
sudo journalctl -u jobcompare --no-pager

# Check file permissions
sudo chown -R www-data:www-data /var/www/jobcompare
```

#### Application Not Accessible
```bash
# Check if application is running
sudo netstat -tlnp | grep :5000

# Check firewall
sudo ufw status

# Check Nginx logs
sudo tail -f /var/log/nginx/error.log
```

#### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

## ✅ Quick Deployment Checklist

- [ ] Droplet created and accessible via SSH
- [ ] System updated (`apt update && apt upgrade`)
- [ ] .NET 7 SDK installed and verified
- [ ] Nginx installed and running
- [ ] Repository cloned to `/var/www/jobcompare`
- [ ] Application built successfully
- [ ] Systemd service created and running
- [ ] Nginx reverse proxy configured
- [ ] Application accessible via web browser
- [ ] SSL certificate installed (optional)

## 🔗 Access Your Application

Once deployed, access your application at:
- **HTTP**: `http://YOUR_DROPLET_IP/recon`
- **HTTPS**: `https://YOUR_DOMAIN/recon` (if SSL configured)

## 📞 Support

If you encounter issues:
1. Check service logs: `sudo journalctl -u jobcompare -f`
2. Check Nginx logs: `sudo tail -f /var/log/nginx/error.log`
3. Verify all services are running: `sudo systemctl status jobcompare nginx`

Your JobCompare Reconciliation Dashboard should now be running on your droplet! 🎉
