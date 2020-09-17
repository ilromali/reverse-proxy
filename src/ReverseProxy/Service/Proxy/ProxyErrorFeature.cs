// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.ReverseProxy.Service.Proxy
{
    internal class ProxyErrorFeature : IProxyErrorFeature
    {
        internal ProxyErrorFeature(ProxyError error, Exception ex)
        {
            Error = error;
            Exception = ex;
        }

        /// <summary>
        /// The specified ProxyErrorCode.
        /// </summary>
        public ProxyError Error { get; }

        /// <summary>
        /// The error, if any.
        /// </summary>
        public Exception Exception { get; }
    }
}
