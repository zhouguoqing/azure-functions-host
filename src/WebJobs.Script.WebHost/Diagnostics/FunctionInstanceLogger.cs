// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Loggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Azure.WebJobs.Script.WebHost.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics
{
    // Adapter for capturing SDK events and logging them to tables.
    internal class FunctionInstanceLogger : IAsyncCollector<FunctionInstanceLogEntry>
    {
        public FunctionInstanceLogger(
            IFunctionMetadataManager metadataManager,
            IMetricsLogger metrics,
            IHostIdProvider hostIdProvider,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IDelegatingHandlerProvider delegatingHandlerProvider)
            : this(metadataManager, metrics)
        {
        }

        internal FunctionInstanceLogger(IFunctionMetadataManager metadataManager, IMetricsLogger metrics)
        {
        }

        public Task AddAsync(FunctionInstanceLogEntry item, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        public static void OnException(Exception exception, ILogger logger)
        {
        }
    }
}
