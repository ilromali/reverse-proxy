﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ReverseProxy.RuntimeModel;

namespace Microsoft.ReverseProxy.Service.Management
{
    /// <summary>
    /// Manages routes and their runtime states.
    /// </summary>
    internal interface IRouteManager : IItemManager<RouteInfo>
    {
    }
}
