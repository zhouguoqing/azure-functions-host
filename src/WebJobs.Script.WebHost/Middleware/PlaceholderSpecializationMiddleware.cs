// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Rpc;
using FunctionMetadata = Microsoft.Azure.WebJobs.Script.Description.FunctionMetadata;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Middleware
{
    public class PlaceholderSpecializationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScriptWebHostEnvironment _webHostEnvironment;
        private readonly IStandbyManager _standbyManager;
        private readonly IEnvironment _environment;
        private RequestDelegate _invoke;
        private double _specialized = 0;

        private ILanguageWorkerChannelManager _languageWorkerChannelManager;

        public PlaceholderSpecializationMiddleware(RequestDelegate next, IScriptWebHostEnvironment webHostEnvironment,
            IStandbyManager standbyManager, IEnvironment environment, ILanguageWorkerChannelManager languageWorkerChannelManager)
        {
            _next = next;
            _invoke = InvokeSpecializationCheck;
            _webHostEnvironment = webHostEnvironment;
            _standbyManager = standbyManager;
            _environment = environment;
            _languageWorkerChannelManager = languageWorkerChannelManager;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _invoke(httpContext);
        }

        private async Task InvokeSpecializationCheck(HttpContext httpContext)
        {
            if (!_webHostEnvironment.InStandbyMode && _environment.IsContainerReady())
            {
                var channel = _languageWorkerChannelManager.GetChannels("node").FirstOrDefault();

                ScriptInvocationContext scriptInvocationContext = new ScriptInvocationContext()
                {
                    ResultSource = new TaskCompletionSource<ScriptInvocationResult>(),
                    FunctionMetadata = new FunctionMetadata()
                };

                scriptInvocationContext.FunctionMetadata.Name = "Ping";

                channel.SendInvocationRequest(scriptInvocationContext);

                await scriptInvocationContext.ResultSource.Task;

                // We don't want AsyncLocal context (like Activity.Current) to flow
                // here as it will contain request details. Suppressing this context
                // prevents the request context from being captured by the host.
                Task specializeTask;
                using (System.Threading.ExecutionContext.SuppressFlow())
                {
                    // We need this to go async immediately, so use Task.Run.
                    specializeTask = Task.Run(_standbyManager.SpecializeHostAsync);
                }
                await specializeTask;

                if (Interlocked.CompareExchange(ref _specialized, 1, 0) == 0)
                {
                    Interlocked.Exchange(ref _invoke, _next);
                }
            }

            await _next(httpContext);
        }
    }
}
