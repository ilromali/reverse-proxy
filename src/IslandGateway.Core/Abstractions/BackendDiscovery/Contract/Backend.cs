﻿// <copyright file="Backend.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace IslandGateway.Core.Abstractions
{
    /// <summary>
    /// A backend is a group of equivalent endpoints and associated policies.
    /// A route maps requests to a backend, and Island Gateway handles that request
    /// by proxying to any endpoint within the matching backend,
    /// honoring load balancing and partitioning policies when applicable.
    /// </summary>
    /// <remarks>
    /// When proxying to Service Fabric services, a <see cref="Backend"/> will generally correspond to a Serice Fabric service instance,
    /// and the backend endpoints correspond to the endpoints of the replicas of said service.
    /// </remarks>
    public sealed class Backend : IDeepCloneable<Backend>
    {
        /// <summary>
        /// Unique identifier of this backend. No other backend may specify the same value.
        /// </summary>
        public string BackendId { get; set; }

        /// <summary>
        /// Circuit breaker options.
        /// </summary>
        public CircuitBreakerOptions CircuitBreakerOptions { get; set; }

        /// <summary>
        /// Quota options.
        /// </summary>
        public QuotaOptions QuotaOptions { get; set; }

        /// <summary>
        /// Partitioning options.
        /// </summary>
        public BackendPartitioningOptions PartitioningOptions { get; set; }

        /// <summary>
        /// Load balancing options.
        /// </summary>
        public LoadBalancingOptions LoadBalancingOptions { get; set; }

        /// <summary>
        /// Active health checking options.
        /// </summary>
        public HealthCheckOptions HealthCheckOptions { get; set; }

        /// <summary>
        /// Arbitrary key-value pairs that further describe this backend.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }

        /// <inheritdoc/>
        Backend IDeepCloneable<Backend>.DeepClone()
        {
            return new Backend
            {
                BackendId = this.BackendId,
                CircuitBreakerOptions = this.CircuitBreakerOptions?.DeepClone(),
                QuotaOptions = this.QuotaOptions?.DeepClone(),
                PartitioningOptions = this.PartitioningOptions?.DeepClone(),
                LoadBalancingOptions = this.LoadBalancingOptions?.DeepClone(),
                HealthCheckOptions = this.HealthCheckOptions?.DeepClone(),
                Metadata = this.Metadata?.DeepClone(StringComparer.Ordinal),
            };
        }
    }
}
