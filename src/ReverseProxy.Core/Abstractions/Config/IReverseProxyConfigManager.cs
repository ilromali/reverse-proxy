// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.ReverseProxy.Core.Service;

namespace Microsoft.ReverseProxy.Core.Abstractions
{
    /// <summary>
    /// High-level management of Reverse Proxy state.
    /// </summary>
    public interface IReverseProxyConfigManager
    {
        /// <summary>
        /// Applies latest configurations obtained from <see cref="IDynamicConfigBuilder"/>.
        /// </summary>
        Task<bool> ApplyConfigurationsAsync(IConfigErrorReporter configErrorReporter, CancellationToken cancellation);
    }
}
