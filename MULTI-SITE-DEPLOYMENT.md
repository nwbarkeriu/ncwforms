# 🚀 JobCompare Multi-Site Deployment Guide

Deploying JobCompare (ncwforms) on an existing droplet that already runs bin-buddies.

## 📋 Prerequisites ✅

Since your droplet already has:
- ✅ .NET 7 SDK installed
- ✅ Nginx configured and running
- ✅ Firewall configured
- ✅ bin-buddies site running

We only need to add JobCompare as a second application.

## 🔧 Step 1: Clone JobCompare Repository

```bash
# SSH into your droplet
ssh root@YOUR_DROPLET_IP

# Navigate to web directory (assuming similar structure to bin-buddies)
cd /var/www

# Clone JobCompare repository
sudo git clone https://github.com/nwbarkeriu/ncwforms.git jobcompare

# Set proper ownership
sudo chown -R www-data:www-data /var/www/jobcompare

# Navigate to project
cd /var/www/jobcompare
```

## 🔨 Step 2: Build JobCompare Application

```bash
# Navigate to project directory
cd /var/www/jobcompare

# Restore dependencies
dotnet restore

# Build for production
dotnet build --configuration Release

# Test build (optional)
dotnet run --urls="http://localhost:5001" &
# Test with: curl http://localhost:5001
# Kill test: pkill -f JobCompare
```

## 🔄 Step 3: Create JobCompare Systemd Service

```bash
# Create service file for JobCompare
sudo nano /etc/systemd/system/jobcompare.service
```

Add the following content:
```ini
[Unit]
Description=JobCompare Reconciliation Dashboard
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/jobcompare/bin/Release/net7.0/JobCompare.dll
Restart=always
RestartSec=5
TimeoutStopSec=90
KillMode=mixed
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5001
WorkingDirectory=/var/www/jobcompare

[Install]
WantedBy=multi-user.target
```

**Note**: Using port 5001 to avoid conflicts with bin-buddies (likely on 5000)

```bash
# Enable and start the service
sudo systemctl daemon-reload
sudo systemctl enable jobcompare
sudo systemctl start jobcompare

# Check status
sudo systemctl status jobcompare
```

## 🌐 Step 4: Configure Nginx for Multiple Sites

### Option A: Subdomain Setup (Recommended)
If you want `jobcompare.yourdomain.com` and `binbuddies.yourdomain.com`:

```bash
# Create JobCompare site config
sudo nano /etc/nginx/sites-available/jobcompare
```

Add configuration:
```nginx
server {
    listen 80;
    server_name jobcompare.yourdomain.com;  # Replace with your subdomain
    
    location / {
        proxy_pass http://localhost:5001;
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

### Option B: Path-Based Setup
If you want `yourdomain.com/jobcompare` and `yourdomain.com/binbuddies`:

```bash
# Edit your existing Nginx config (likely in sites-available)
sudo nano /etc/nginx/sites-available/default
# OR
sudo nano /etc/nginx/sites-available/binbuddies
```

Add JobCompare location block to existing server block:
```nginx
server {
    listen 80;
    server_name yourdomain.com;
    
    # Existing bin-buddies location
    location /binbuddies {
        proxy_pass http://localhost:5000;
        # ... existing proxy settings
    }
    
    # New JobCompare location
    location /jobcompare {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    
    # Root can redirect to one of the apps
    location = / {
        return 301 /binbuddies;
    }
}
```

### Enable Configuration and Restart

**For Option A (Subdomain):**
```bash
# Enable JobCompare site
sudo ln -s /etc/nginx/sites-available/jobcompare /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

**For Option B (Path-based):**
```bash
# Test configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

## 🔧 Step 5: Update Application Configuration (If Needed)

If using path-based routing, update JobCompare's base path:

```bash
# Edit Program.cs if needed for path-based routing
sudo nano /var/www/jobcompare/Program.cs
```

You might need to add path base configuration, but the current setup should work as-is.

## 🔍 Step 6: Verify Both Applications

```bash
# Check both services
sudo systemctl status binbuddies
sudo systemctl status jobcompare

# Check ports are listening
sudo netstat -tlnp | grep :5000  # bin-buddies
sudo netstat -tlnp | grep :5001  # jobcompare

# Check Nginx configuration
sudo nginx -t

# Test applications
curl http://localhost:5000  # bin-buddies
curl http://localhost:5001  # jobcompare
```

## 🔒 Step 7: SSL Certificates (If Using Subdomains)

If you chose subdomain setup and want SSL:

```bash
# Add SSL for JobCompare subdomain
sudo certbot --nginx -d jobcompare.yourdomain.com

# Verify both certificates
sudo certbot certificates
```

## 📊 Step 8: Monitoring Both Applications

```bash
# View JobCompare logs
sudo journalctl -u jobcompare -f

# View bin-buddies logs
sudo journalctl -u binbuddies -f

# Monitor both applications
sudo systemctl status jobcompare binbuddies
```

## 🔄 Step 9: Future Updates

### Update JobCompare:
```bash
cd /var/www/jobcompare
git pull origin main
dotnet build --configuration Release
sudo systemctl restart jobcompare
```

### Update Both Applications:
```bash
# Update JobCompare
cd /var/www/jobcompare
git pull origin main
dotnet build --configuration Release
sudo systemctl restart jobcompare

# Update bin-buddies (adjust path as needed)
cd /var/www/binbuddies
git pull origin main
dotnet build --configuration Release
sudo systemctl restart binbuddies
```

## 🎯 Access Your Applications

### Option A - Subdomains:
- **bin-buddies**: `https://binbuddies.yourdomain.com`
- **JobCompare**: `https://jobcompare.yourdomain.com/recon`

### Option B - Paths:
- **bin-buddies**: `https://yourdomain.com/binbuddies`
- **JobCompare**: `https://yourdomain.com/jobcompare/recon`

## 🚨 Quick Troubleshooting

### Port Conflicts:
```bash
# Check what's using ports
sudo netstat -tlnp | grep :5000
sudo netstat -tlnp | grep :5001

# If port conflict, change JobCompare port in service file
sudo nano /etc/systemd/system/jobcompare.service
# Change to available port like 5002, 5003, etc.
```

### Service Issues:
```bash
# Restart services
sudo systemctl restart jobcompare
sudo systemctl restart nginx

# Check logs
sudo journalctl -u jobcompare --no-pager
```

## ✅ Quick Setup Checklist

- [ ] Repository cloned to `/var/www/jobcompare`
- [ ] Application built successfully
- [ ] JobCompare service created and running on port 5001
- [ ] Nginx configured for both applications
- [ ] Both applications accessible
- [ ] SSL certificates updated (if using subdomains)

Your droplet now runs both bin-buddies and JobCompare! 🎉
