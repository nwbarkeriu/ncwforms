# 🚀 NCWForms Multi-Site Deployment Guide

Deploying NCWForms on an existing droplet that already runs bin-buddies.

## 📋 Prerequisites ✅

Since your droplet already has:
- ✅ .NET 7 SDK installed
- ✅ Nginx configured and running
- ✅ Firewall configured
- ✅ bin-buddies site running

We only need to add NCWForms as a second application.

## 🔧 Step 1: Clone NCWForms Repository

```bash
# SSH into your droplet
ssh root@YOUR_DROPLET_IP

# Navigate to web directory (assuming similar structure to bin-buddies)
cd /var/www

# Clone NCWForms repository
sudo git clone https://github.com/nwbarkeriu/ncwforms.git ncwforms

# Set proper ownership
sudo chown -R www-data:www-data /var/www/ncwforms

# Navigate to project
cd /var/www/ncwforms
```

## 🔨 Step 2: Build NCWForms Application

```bash
# Navigate to project directory
cd /var/www/ncwforms

# Restore dependencies
dotnet restore

# Build for production
dotnet build --configuration Release

# Test build (optional)
dotnet run --urls="http://localhost:5001" &
# Test with: curl http://localhost:5001
# Kill test: pkill -f JobCompare
```

## 🔄 Step 3: Create NCWForms Systemd Service

```bash
# Create service file for NCWForms
sudo nano /etc/systemd/system/ncwforms.service
```

Add the following content:
```ini
[Unit]
Description=NCWForms Reconciliation Dashboard
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/ncwforms/bin/Release/net7.0/JobCompare.dll
Restart=always
RestartSec=5
TimeoutStopSec=90
KillMode=mixed
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5001
WorkingDirectory=/var/www/ncwforms

[Install]
WantedBy=multi-user.target
```

**Note**: Using port 5001 to avoid conflicts with bin-buddies (likely on 5000)

```bash
# Enable and start the service
sudo systemctl daemon-reload
sudo systemctl enable ncwforms
sudo systemctl start ncwforms

# Check status
sudo systemctl status ncwforms
```

## 🌐 Step 4: Configure Nginx for Multiple Sites

### Option A: Subdomain Setup (Recommended)
If you want `jobcompare.yourdomain.com` and `binbuddies.yourdomain.com`:

```bash
# Create NCWForms site config
sudo nano /etc/nginx/sites-available/ncwforms
```

Add configuration:
```nginx
server {
    listen 80;
    server_name ncwforms.yourdomain.com;  # Replace with your subdomain
    
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
If you want `yourdomain.com/ncwforms` and `yourdomain.com/binbuddies`:

```bash
# Edit your existing Nginx config (likely in sites-available)
sudo nano /etc/nginx/sites-available/default
# OR
sudo nano /etc/nginx/sites-available/binbuddies
```

Add NCWForms location block to existing server block:
```nginx
server {
    listen 80;
    server_name yourdomain.com;
    
    # Existing bin-buddies location
    location /binbuddies {
        proxy_pass http://localhost:5000;
        # ... existing proxy settings
    }
    
    # New NCWForms location
    location /ncwforms {
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
# Enable NCWForms site
sudo ln -s /etc/nginx/sites-available/ncwforms /etc/nginx/sites-enabled/

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

If using path-based routing, update NCWForms's base path:

```bash
# Edit Program.cs if needed for path-based routing
sudo nano /var/www/ncwforms/Program.cs
```

You might need to add path base configuration, but the current setup should work as-is.

## 🔍 Step 6: Verify Both Applications

```bash
# Check both services
sudo systemctl status binbuddies
sudo systemctl status ncwforms

# Check ports are listening
sudo netstat -tlnp | grep :5000  # bin-buddies
sudo netstat -tlnp | grep :5001  # ncwforms

# Check Nginx configuration
sudo nginx -t

# Test applications
curl http://localhost:5000  # bin-buddies
curl http://localhost:5001  # ncwforms
```

## 🔒 Step 7: SSL Certificates (If Using Subdomains)

If you chose subdomain setup and want SSL:

```bash
# Add SSL for NCWForms subdomain
sudo certbot --nginx -d ncwforms.yourdomain.com

# Verify both certificates
sudo certbot certificates
```

## 📊 Step 8: Monitoring Both Applications

```bash
# View NCWForms logs
sudo journalctl -u ncwforms -f

# View bin-buddies logs
sudo journalctl -u binbuddies -f

# Monitor both applications
sudo systemctl status ncwforms binbuddies
```

## 🔄 Step 9: Future Updates

### Update NCWForms:
```bash
cd /var/www/ncwforms
git pull origin main
dotnet build --configuration Release
sudo systemctl restart ncwforms
```

### Update Both Applications:
```bash
# Update NCWForms
cd /var/www/ncwforms
git pull origin main
dotnet build --configuration Release
sudo systemctl restart ncwforms

# Update bin-buddies (adjust path as needed)
cd /var/www/binbuddies
git pull origin main
dotnet build --configuration Release
sudo systemctl restart binbuddies
```

## 🎯 Access Your Applications

### Option A - Subdomains:
- **bin-buddies**: `https://binbuddies.yourdomain.com`
- **NCWForms**: `https://ncwforms.yourdomain.com/recon`

### Option B - Paths:
- **bin-buddies**: `https://yourdomain.com/binbuddies`
- **NCWForms**: `https://yourdomain.com/ncwforms/recon`

## 🚨 Quick Troubleshooting

### Port Conflicts:
```bash
# Check what's using ports
sudo netstat -tlnp | grep :5000
sudo netstat -tlnp | grep :5001

# If port conflict, change NCWForms port in service file
sudo nano /etc/systemd/system/ncwforms.service
# Change to available port like 5002, 5003, etc.
```

### Service Issues:
```bash
# Restart services
sudo systemctl restart ncwforms
sudo systemctl restart nginx

# Check logs
sudo journalctl -u ncwforms --no-pager
```

## ✅ Quick Setup Checklist

- [ ] Repository cloned to `/var/www/ncwforms`
- [ ] Application built successfully
- [ ] NCWForms service created and running on port 5001
- [ ] Nginx configured for both applications
- [ ] Both applications accessible
- [ ] SSL certificates updated (if using subdomains)

Your droplet now runs both bin-buddies and NCWForms! 🎉
