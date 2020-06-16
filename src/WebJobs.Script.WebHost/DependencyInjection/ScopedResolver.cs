// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Script.WebHost.DependencyInjection
{
    internal class ScopedResolver : IDisposable
    {
        private static readonly Setup _rootScopeFactorySetup = Setup.With(preventDisposal: true);

        public ScopedResolver(IContainer resolver, bool isRootResolver = false)
        {
            Container = resolver ?? throw new ArgumentNullException(nameof(resolver));
            IsRootResolver = isRootResolver;
            ChildScopes = new ConcurrentDictionary<ServiceScope, object>();
        }

        public IContainer Container { get; }

        public ConcurrentDictionary<ServiceScope, object> ChildScopes { get; }

        public bool IsRootResolver { get; }

        public void Dispose()
        {
            Task childScopeTasks = Task.WhenAll(ChildScopes.Keys.Select(s => s.DisposalTask));
            Task.WhenAny(childScopeTasks, Task.Delay(5000))
                .ContinueWith(t =>
                {
                    Container.Dispose();
                }, TaskContinuationOptions.ExecuteSynchronously);
        }

        internal IServiceScope CreateChildScope(IServiceScopeFactory rootScopeFactory)
        {
            return rootScopeFactory.CreateScope();
        }
    }
}
