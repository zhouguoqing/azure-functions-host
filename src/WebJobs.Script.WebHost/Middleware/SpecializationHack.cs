// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Script.Extensions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Middleware
{
    public class SpecializationHack
    {
        private readonly RequestDelegate _next;
        private readonly IScriptWebHostEnvironment _webHostEnvironment;
        private readonly IEnvironment _environment;
        private readonly IScriptHostManager _hostManager;

        public SpecializationHack(RequestDelegate next, IScriptWebHostEnvironment webHostEnvironment, IEnvironment environment, IScriptHostManager hostManager)
        {
            _next = next;
            _webHostEnvironment = webHostEnvironment;
            _environment = environment;
            _hostManager = hostManager;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (IsSpecializationHack(httpContext.Request, _webHostEnvironment, _environment))
            {
                _environment.SetEnvironmentVariable(EnvironmentSettingNames.AzureWebsitePlaceholderMode, "0");
                _environment.SetEnvironmentVariable(EnvironmentSettingNames.AzureWebsiteContainerReady, "1");
                _environment.SetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME", "node");
            }

            await _next.Invoke(httpContext);
        }

        public static bool IsSpecializationHack(HttpRequest request, IScriptWebHostEnvironment webHostEnvironment, IEnvironment environment)
        {
            return webHostEnvironment.InStandbyMode &&
                request.Path.StartsWithSegments(new PathString($"/api/specializationhack"));
        }
    }
}
