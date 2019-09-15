// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Rpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using FunctionMetadata = Microsoft.Azure.WebJobs.Script.Description.FunctionMetadata;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Middleware
{
    public class PlaceholderSpecializationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScriptWebHostEnvironment _webHostEnvironment;
        private readonly IStandbyManager _standbyManager;
        private readonly IEnvironment _environment;
        private readonly IOptionsMonitor<ScriptApplicationHostOptions> _options;
        private readonly IEnumerable<WorkerConfig> _workerConfigs;
        private RequestDelegate _invoke;
        private double _specialized = 0;
        private ILoggerFactory _loggerFactory;

        private IWebHostLanguageWorkerChannelManager _webHostlanguageWorkerChannelManager;

        public PlaceholderSpecializationMiddleware(RequestDelegate next, IScriptWebHostEnvironment webHostEnvironment, IOptions<LanguageWorkerOptions> workerConfigOptions,
            IStandbyManager standbyManager, IEnvironment environment, IWebHostLanguageWorkerChannelManager webHostLanguageWorkerChannelManager, IOptionsMonitor<ScriptApplicationHostOptions> options, ILoggerFactory loggerFactory)
        {
            _next = next;
            _invoke = InvokeSpecializationCheck;
            _webHostEnvironment = webHostEnvironment;
            _standbyManager = standbyManager;
            _environment = environment;
            _webHostlanguageWorkerChannelManager = webHostLanguageWorkerChannelManager;
            _options = options;
            _workerConfigs = workerConfigOptions.Value.WorkerConfigs;
            _loggerFactory = loggerFactory;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _invoke(httpContext);
        }

        internal static Collection<FunctionMetadata> ReadFunctionsMetadata(string rootScriptPath, ICollection<string> functionsWhiteList, IEnumerable<WorkerConfig> workerConfigs,
           Dictionary<string, ICollection<string>> functionErrors = null)
        {
            IEnumerable<string> functionDirectories = Directory.EnumerateDirectories(rootScriptPath);

            var functions = new Collection<FunctionMetadata>();

            foreach (var scriptDir in functionDirectories)
            {
                var function = FunctionMetadataManager.ReadFunctionMetadata(scriptDir, functionsWhiteList, workerConfigs, functionErrors);
                if (function != null)
                {
                    functions.Add(function);
                }
            }
            return functions;
        }

        private async Task InvokeSpecializationCheck(HttpContext httpContext)
        {
            if (!_webHostEnvironment.InStandbyMode && _environment.IsContainerReady())
            {
                // TODO: this should only be called for ultra mode
                _standbyManager.SpecializeHostReloadConfig();
                string scriptPath = _options.CurrentValue.ScriptPath;
                var channelTask = _webHostlanguageWorkerChannelManager.GetChannels("node").FirstOrDefault();
                ILanguageWorkerChannel channel = await channelTask.Value.Task;

                await channel.SendFunctionEnvironmentReloadRequest();

                IEnumerable<FunctionMetadata> functions = ReadFunctionsMetadata(scriptPath, null, _workerConfigs);
                channel.SetupFunctionInvocationBuffers(functions);
                TaskCompletionSource<bool> loadTask = new TaskCompletionSource<bool>();
                channel.SendFunctionLoadRequest(functions.ElementAt(0), loadTask);
                await loadTask.Task;
                ScriptInvocationContext scriptInvocationContext = new ScriptInvocationContext()
                {
                    FunctionMetadata = functions.ElementAt(0),
                    ResultSource = new TaskCompletionSource<ScriptInvocationResult>(),
                    AsyncExecutionContext = System.Threading.ExecutionContext.Capture(),

                    // TODO: link up cancellation token to parameter descriptors
                    CancellationToken = CancellationToken.None,
                    Logger = _loggerFactory.CreateLogger("HttpTrigger")
                };

                channel.SendInvocationRequest(scriptInvocationContext);

                var t = await scriptInvocationContext.ResultSource.Task;

                // TODO: not always return OKOBjectResult
                var result = new OkObjectResult(t.Return);
                ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
                await result.ExecuteResultAsync(actionContext);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                {
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

                    await _next(httpContext);
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return;
            }

            await _next(httpContext);
        }
    }
}
