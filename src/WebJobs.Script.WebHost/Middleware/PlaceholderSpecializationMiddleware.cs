// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Script.Binding;
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

        private IWebHostLanguageWorkerChannelManager _webHostlanguageWorkerChannelManager;

        public PlaceholderSpecializationMiddleware(RequestDelegate next, IScriptWebHostEnvironment webHostEnvironment,
            IStandbyManager standbyManager, IEnvironment environment, IWebHostLanguageWorkerChannelManager webHostLanguageWorkerChannelManager)
        {
            _next = next;
            _invoke = InvokeSpecializationCheck;
            _webHostEnvironment = webHostEnvironment;
            _standbyManager = standbyManager;
            _environment = environment;
            _webHostlanguageWorkerChannelManager = webHostLanguageWorkerChannelManager;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _invoke(httpContext);
        }

        private async Task InvokeSpecializationCheck(HttpContext httpContext)
        {
            if (!_webHostEnvironment.InStandbyMode && _environment.IsContainerReady())
            {
                ILanguageWorkerChannel channel = _webHostlanguageWorkerChannelManager.GetChannels("node").FirstOrDefault();
                BindingMetadata bindingMetadataIn = new BindingMetadata()
                {
                    Type = "httpTrigger",
                    Direction = BindingDirection.In,
                    Name = "req"
                };

                BindingMetadata bindingMetadataOut = new BindingMetadata()
                {
                    Type = "http",
                    Direction = BindingDirection.Out,
                    Name = "res"
                };

                Collection<BindingMetadata> bindings = new Collection<BindingMetadata>()
                {
                    bindingMetadataIn,
                    bindingMetadataOut
                };

                FunctionMetadata functionMetadata = new FunctionMetadata()
                {
                    FunctionDirectory = @"D:\pgopaGit\azure-functions-nodejs-worker\test\end-to-end\testFunctionApp\HttpTrigger",
                    Language = "node",
                    Name = "HttpTrigger",
                    ScriptFile = @"D:\pgopaGit\azure-functions-nodejs-worker\test\end-to-end\testFunctionApp\HttpTrigger\index.js",
                    IsProxy = false,
                    IsDisabled = false,
                    IsDirect = false,
                    Bindings = bindings
                };

                ScriptInvocationContext scriptInvocationContext = new ScriptInvocationContext()
                {
                    FunctionMetadata = functionMetadata,
                    ResultSource = new TaskCompletionSource<ScriptInvocationResult>()
                };

                channel.SendInvocationRequest(scriptInvocationContext);

                var t = await scriptInvocationContext.ResultSource.Task;

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
