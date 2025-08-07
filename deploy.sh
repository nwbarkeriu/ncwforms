#!/bin/bash

# Deploy script for NCW Forms to DigitalOcean droplet
# This script pulls latest changes, builds, and deploys to /var/www/ncwforms/publish

set -e  # Exit on any error

echo "Starting deployment..."

# Pull latest changes
echo "Pulling latest changes from GitHub..."
git pull origin main

# Stop the service
echo "Stopping ncwforms service..."
sudo systemctl stop ncwforms

# Build and publish the application
echo "Building and publishing application..."
dotnet publish -c Release -o /var/www/ncwforms/publish

# Set proper permissions
echo "Setting permissions..."
sudo chown -R www-data:www-data /var/www/ncwforms/publish
sudo chmod -R 755 /var/www/ncwforms/publish

# Start the service
echo "Starting ncwforms service..."
sudo systemctl start ncwforms

# Check service status
echo "Checking service status..."
sudo systemctl status ncwforms --no-pager

echo "Deployment completed successfully!"
echo "Application should be available at your domain."
