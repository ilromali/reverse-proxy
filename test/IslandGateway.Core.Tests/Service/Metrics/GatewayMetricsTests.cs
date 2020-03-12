﻿// <copyright file="GatewayMetricsTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using FluentAssertions;
using IslandGateway.Common.Abstractions.Telemetry;
using Tests.Common;
using Xunit;

namespace IslandGateway.Core.Service.Metrics.Tests
{
    public class GatewayMetricsTests : TestAutoMockBase
    {
        private readonly TestMetricCreator metricCreator;

        public GatewayMetricsTests()
        {
            this.metricCreator = this.Provide<IMetricCreator, TestMetricCreator>();
        }

        [Fact]
        public void Constructor_Works()
        {
            this.Create<GatewayMetrics>();
        }

        [Fact]
        public void StreamCopyBytes_Works()
        {
            // Arrange
            var metrics = this.Create<GatewayMetrics>();

            // Act
            metrics.StreamCopyBytes(123, "upstream", "be1", "rt1", "ep1", "prot");

            // Assert
            this.metricCreator.MetricsLogged.Should().BeEquivalentTo("StreamCopyBytes=123;direction=upstream;backendId=be1;routeId=rt1;endpointId=ep1;protocol=prot");
        }

        [Fact]
        public void StreamCopyIops_Works()
        {
            // Arrange
            var metrics = this.Create<GatewayMetrics>();

            // Act
            metrics.StreamCopyIops(123, "upstream", "be1", "rt1", "ep1", "prot");

            // Assert
            this.metricCreator.MetricsLogged.Should().BeEquivalentTo("StreamCopyIops=123;direction=upstream;backendId=be1;routeId=rt1;endpointId=ep1;protocol=prot");
        }
    }
}
