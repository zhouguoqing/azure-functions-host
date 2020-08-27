#!/bin/bash

# From https://docs.microsoft.com/dotnet/core/install/linux-ubuntu#install-the-sdk

sudo apt-get update
sudo apt-get install -y apt-transport-https && \
sudo apt-get update && \
sudo apt-get install -y dotnet-sdk-3.1
