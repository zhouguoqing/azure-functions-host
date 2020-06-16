// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Features
{
    internal class FunctionExecutionFeature : IFunctionExecutionFeature
    {
        private readonly IScriptJobHost _host;
        private readonly FunctionDescriptor _descriptor;

        public FunctionExecutionFeature(IScriptJobHost host, FunctionDescriptor descriptor, IEnvironment environment, ILoggerFactory loggerFactory)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _descriptor = descriptor;
        }

        public bool CanExecute => _descriptor != null;

        public FunctionDescriptor Descriptor => _descriptor;

        public async Task ExecuteAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            if (!CanExecute)
            {
                throw new InvalidOperationException("Unable to execute function without a target.");
            }

            var arguments = new Dictionary<string, object>()
            {
                { _descriptor.TriggerParameter.Name, request }
            };
            await _host.CallAsync(_descriptor.Name, arguments, cancellationToken);
        }
    }
}
