﻿// <copyright file="InMemoryEndpointsRepo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslandGateway.Core.Abstractions;
using IslandGateway.Utilities;

namespace IslandGateway.Core.Service
{
    internal class InMemoryEndpointsRepo : IBackendEndpointsRepo
    {
        private readonly object lockObject = new object();
        private readonly Dictionary<string, IList<BackendEndpoint>> backendEndpoints = new Dictionary<string, IList<BackendEndpoint>>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public Task<IList<BackendEndpoint>> GetEndpointsAsync(string backendId, CancellationToken cancellation)
        {
            Contracts.CheckNonEmpty(backendId, nameof(backendId));

            lock (this.lockObject)
            {
                this.backendEndpoints.TryGetValue(backendId, out var results);
                return Task.FromResult(results?.DeepClone());
            }
        }

        /// <inheritdoc/>
        public Task SetEndpointsAsync(string backendId, IList<BackendEndpoint> endpoints, CancellationToken cancellation)
        {
            Contracts.CheckNonEmpty(backendId, nameof(backendId));

            lock (this.lockObject)
            {
                this.backendEndpoints[backendId] = endpoints?.DeepClone();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveEndpointsAsync(string backendId, CancellationToken cancellation)
        {
            Contracts.CheckNonEmpty(backendId, nameof(backendId));

            lock (this.lockObject)
            {
                this.backendEndpoints.Remove(backendId);
            }

            return Task.CompletedTask;
        }
    }
}
