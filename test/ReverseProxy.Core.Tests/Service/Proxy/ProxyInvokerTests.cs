// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.ReverseProxy.Common.Abstractions.Telemetry;
using Microsoft.ReverseProxy.Common.Telemetry;
using Microsoft.ReverseProxy.Core.RuntimeModel;
using Microsoft.ReverseProxy.Core.Service.Management;
using Microsoft.ReverseProxy.Core.Service.Proxy.Infra;
using Moq;
using Tests.Common;
using Xunit;

namespace Microsoft.ReverseProxy.Core.Service.Proxy.Tests
{
    public class ProxyInvokerTests : TestAutoMockBase
    {
        public ProxyInvokerTests()
        {
            Provide<IOperationLogger, TextOperationLogger>();
        }

        [Fact]
        public void Constructor_Works()
        {
            Create<ProxyInvoker>();
        }

        [Fact]
        public async Task InvokeAsync_Works()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.Path = "/api/test";
            httpContext.Request.QueryString = new QueryString("?a=b&c=d");

            var proxyHttpClientFactoryMock = new Mock<IProxyHttpClientFactory>();
            var backend1 = new BackendInfo(
                backendId: "backend1",
                endpointManager: new EndpointManager(),
                proxyHttpClientFactory: proxyHttpClientFactoryMock.Object);
            var endpoint1 = backend1.EndpointManager.GetOrCreateItem(
                "endpoint1",
                endpoint =>
                {
                    endpoint.Config.Value = new EndpointConfig("https://localhost:123/a/b/");
                    endpoint.DynamicState.Value = new EndpointDynamicState(EndpointHealth.Healthy);
                });

            var aspNetCoreEndpoints = new List<Endpoint>();
            var routeConfig = new RouteConfig(
                route: new RouteInfo("route1"),
                matcherSummary: null,
                priority: null,
                backendOrNull: backend1,
                aspNetCoreEndpoints: aspNetCoreEndpoints.AsReadOnly());
            var aspNetCoreEndpoint = CreateAspNetCoreEndpoint(routeConfig);
            aspNetCoreEndpoints.Add(aspNetCoreEndpoint);
            httpContext.SetEndpoint(aspNetCoreEndpoint);

            Mock<ILoadBalancer>()
                .Setup(l => l.PickEndpoint(It.IsAny<IReadOnlyList<EndpointInfo>>(), It.IsAny<IReadOnlyList<EndpointInfo>>(), It.IsAny<BackendConfig.BackendLoadBalancingOptions>()))
                .Returns(endpoint1);

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            Mock<IHttpProxy>()
                .Setup(h => h.ProxyAsync(
                    httpContext,
                    It.Is<Uri>(uri => uri == new Uri("https://localhost:123/a/b/api/test?a=b&c=d")),
                    proxyHttpClientFactoryMock.Object,
                    It.Is<ProxyTelemetryContext>(ctx => ctx.BackendId == "backend1" && ctx.RouteId == "route1" && ctx.EndpointId == "endpoint1"),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<CancellationToken>()))
                .Returns(
                    async () =>
                    {
                        tcs1.TrySetResult(true);
                        await tcs2.Task;
                    })
                .Verifiable();

            var sut = Create<ProxyInvoker>();

            // Act
            Assert.Equal(0, backend1.ConcurrencyCounter.Value);
            Assert.Equal(0, endpoint1.ConcurrencyCounter.Value);

            var task = sut.InvokeAsync(httpContext);
            await tcs1.Task; // Wait until we get to the proxying step.
            Assert.Equal(1, backend1.ConcurrencyCounter.Value);
            Assert.Equal(1, endpoint1.ConcurrencyCounter.Value);

            tcs2.TrySetResult(true);
            await task;
            Assert.Equal(0, backend1.ConcurrencyCounter.Value);
            Assert.Equal(0, endpoint1.ConcurrencyCounter.Value);

            // Assert
            Mock<IHttpProxy>().Verify();
        }

        private static Endpoint CreateAspNetCoreEndpoint(RouteConfig routeConfig)
        {
            var endpointBuilder = new RouteEndpointBuilder(
                requestDelegate: httpContext => Task.CompletedTask,
                routePattern: RoutePatternFactory.Parse("/"),
                order: 0);
            endpointBuilder.Metadata.Add(routeConfig);
            return endpointBuilder.Build();
        }
    }
}
