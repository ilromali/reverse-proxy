﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ReverseProxy.Core.Abstractions;
using Xunit;

namespace Microsoft.ReverseProxy.Core.Service.Tests
{
    public class InMemoryRoutesRepoTests
    {
        [Fact]
        public void Constructor_Works()
        {
            new InMemoryRoutesRepo();
        }

        [Fact]
        public void GetRoutesAsync_CompletesSynchronously()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();

            // Act
            var task = repo.GetRoutesAsync(CancellationToken.None);

            // Assert
            task.IsCompleted.Should().BeTrue("should complete synchronously");
        }

        [Fact]
        public void SetRoutesAsync_CompletesSynchronously()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();

            // Act
            var task = repo.SetRoutesAsync(new ProxyRoute[0], CancellationToken.None);

            // Assert
            task.IsCompleted.Should().BeTrue("should complete synchronously");
        }

        [Fact]
        public async Task GetRoutesAsync_IgnoresCancellation()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Act & Assert
                await repo.GetRoutesAsync(cts.Token);
            }
        }

        [Fact]
        public async Task SetRoutesAsync_IgnoresCancellation()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Act & Assert
                await repo.SetRoutesAsync(new ProxyRoute[0], cts.Token);
            }
        }

        [Fact]
        public async Task GetRoutesAsync_StartsNull()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();

            // Act
            var result = await repo.GetRoutesAsync(CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetRoutesAsync_DeepClones()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();
            var routes = new List<ProxyRoute>
            {
                new ProxyRoute { RouteId = "route1" },
            };

            // Act
            await repo.SetRoutesAsync(routes, CancellationToken.None);

            // Modify input, should not affect output
            routes[0].RouteId = "modified";
            routes.Add(new ProxyRoute { RouteId = "route2" });

            var result = await repo.GetRoutesAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result.Should().NotBeSameAs(routes);
            result[0].RouteId.Should().Be("route1");
        }

        [Fact]
        public async Task GetRoutesAsync_DeepClones()
        {
            // Arrange
            var repo = new InMemoryRoutesRepo();
            var routes = new[]
            {
                new ProxyRoute { RouteId = "route1" },
            };

            // Act
            await repo.SetRoutesAsync(routes, CancellationToken.None);
            var result1 = await repo.GetRoutesAsync(CancellationToken.None);

            // Modify first results, should not affect future results
            result1[0].RouteId = "modified";
            result1.Add(new ProxyRoute { RouteId = "route2" });

            var result2 = await repo.GetRoutesAsync(CancellationToken.None);

            // Assert
            result2.Should().HaveCount(1);
            result2.Should().NotBeSameAs(result1);
            result2.Should().NotBeSameAs(routes);
            result2[0].RouteId.Should().Be("route1");
        }
    }
}
