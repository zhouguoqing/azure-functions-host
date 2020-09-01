// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Script.Configuration
{
    internal class AppServiceOptionsSetup : IConfigureOptions<AppServiceOptions>
    {
        private readonly IEnvironment _environment;

        public AppServiceOptionsSetup(IEnvironment environment)
        {
            _environment = environment;
        }

        public void Configure(AppServiceOptions options)
        {
            options.AppName = _environment.GetAzureWebsiteUniqueSlotName() ?? string.Empty;
            options.SubscriptionId = _environment.GetSubscriptionId() ?? string.Empty;
            options.RuntimeSiteName = _environment.GetRuntimeSiteName() ?? string.Empty;
            options.SlotName = _environment.GetSlotName() ?? string.Empty;
            options.RoleInstance = GetRoleInstance() ?? string.Empty;
        }

        private string GetRoleInstance()
        {
            string instanceName = _environment.GetEnvironmentVariable(EnvironmentSettingNames.AzureWebsiteInstanceId);
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = _environment.GetEnvironmentVariable(EnvironmentSettingNames.ComputerName);
                if (string.IsNullOrEmpty(instanceName))
                {
                    instanceName = _environment.GetEnvironmentVariable(EnvironmentSettingNames.ContainerName);
                }
            }

            return instanceName;
        }
    }
}