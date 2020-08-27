#!/bin/bash

dotnet tool install -g Microsoft.Crank.Agent --version "0.1.0-*"

line="@reboot /home/Functions/.dotnet/tools/crank-agent"
(crontab -l; echo "$line" ) | crontab -
