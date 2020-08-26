#!/bin/bash

# From https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-7#ubuntu-1804

# Download the Microsoft repository GPG keys
wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb

# Register the Microsoft repository GPG keys
sudo dpkg -i packages-microsoft-prod.deb

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell
