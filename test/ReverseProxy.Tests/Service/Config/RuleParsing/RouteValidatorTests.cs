// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.ReverseProxy.Abstractions;
using Microsoft.ReverseProxy.ConfigModel;
using Microsoft.ReverseProxy.Service.Config;
using Moq;
using Tests.Common;
using Xunit;

namespace Microsoft.ReverseProxy.Service.Tests
{
    public class RouteValidatorTests : TestAutoMockBase
    {
        [Fact]
        public void Constructor_Works()
        {
            Create<RouteValidator>();
        }

        [Theory]
        [InlineData("example.com", "/a/", null)]
        [InlineData("example.com", "/a/**", null)]
        [InlineData("example.com", "/a/**", "GET")]
        [InlineData(null, "/a/", null)]
        [InlineData(null, "/a/**", "GET")]
        [InlineData("example.com", null, "get")]
        [InlineData("example.com", null, "gEt,put")]
        [InlineData("example.com", null, "gEt,put,POST,traCE,PATCH,DELETE,Head")]
        [InlineData("example.com,example2.com", null, "get")]
        [InlineData("*.example.com", null, null)]
        [InlineData("a-b.example.com", null, null)]
        [InlineData("a-b.b-c.example.com", null, null)]
        public async Task Accepts_ValidRules(string host, string path, string methods)
        {
            // Arrange
            var route = new ParsedRoute
            {
                RouteId = "route1",
                Hosts = host?.Split(",") ?? Array.Empty<string>(),
                Path = path,
                Methods = methods?.Split(","),
                ClusterId = "cluster1",
            };

            // Act
            var result = await RunScenarioAsync(route);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorReporter.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Rejects_MissingRouteId(string routeId)
        {
            // Arrange
            var errorReporter = new TestConfigErrorReporter();
            var parsedRoute = new ParsedRoute { RouteId = routeId };
            var validator = Create<RouteValidator>();

            // Act
            var isSuccess = await validator.ValidateRouteAsync(parsedRoute, errorReporter);

            // Assert
            Assert.False(isSuccess);
            Assert.Contains(errorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteMissingId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("example.com,")]
        public async Task Rejects_MissingHostAndPath(string host)
        {
            // Arrange
            var route = new ParsedRoute
            {
                RouteId = "route1",
                ClusterId = "cluster1",
                Hosts = host?.Split(",")
            };

            // Act
            var result = await RunScenarioAsync(route);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleHasNoMatchers);
        }

        [Theory]
        [InlineData(".example.com")]
        [InlineData("example*.com")]
        [InlineData("example.*.com")]
        [InlineData("example.*a.com")]
        [InlineData("*example.com")]
        [InlineData("-example.com")]
        [InlineData("example-.com")]
        [InlineData("-example-.com")]
        [InlineData("a.-example.com")]
        [InlineData("a.example-.com")]
        [InlineData("a.-example-.com")]
        [InlineData("example.com,example-.com")]
        public async Task Rejects_InvalidHost(string host)
        {
            // Arrange
            var route = new ParsedRoute
            {
                RouteId = "route1",
                Hosts = host.Split(","),
                ClusterId = "cluster1",
            };

            // Act
            var result = await RunScenarioAsync(route);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleInvalidMatcher && err.Message.Contains("Invalid host name"));
        }

        [Theory]
        [InlineData("/{***a}")]
        [InlineData("/{")]
        [InlineData("/}")]
        [InlineData("/{ab/c}")]
        public async Task Rejects_InvalidPath(string path)
        {
            // Arrange
            var route = new ParsedRoute
            {
                RouteId = "route1",
                Path = path,
                ClusterId = "cluster1",
            };

            // Act
            var result = await RunScenarioAsync(route);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleInvalidMatcher && err.Message.Contains("Invalid path pattern"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("gett")]
        [InlineData("get,post,get")]
        public async Task Rejects_InvalidMethod(string methods)
        {
            // Arrange
            var route = new ParsedRoute
            {
                RouteId = "route1",
                Methods = methods.Split(","),
                ClusterId = "cluster1",
            };

            // Act
            var result = await RunScenarioAsync(route);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleInvalidMatcher && err.Message.Contains("verb"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("defaulT")]
        public async Task Accepts_ReservedAuthorizationPolicy(string policy)
        {
            var route = new ParsedRoute
            {
                RouteId = "route1",
                AuthorizationPolicy = policy,
                Hosts = new[] { "localhost" },
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorReporter.Errors);
        }

        [Fact]
        public async Task Accepts_CustomAuthorizationPolicy()
        {
            var authzOptions = new AuthorizationOptions();
            authzOptions.AddPolicy("custom", builder => builder.RequireAuthenticatedUser());
            Provide<IAuthorizationPolicyProvider>(new DefaultAuthorizationPolicyProvider(Options.Create(authzOptions)));
            var route = new ParsedRoute
            {
                RouteId = "route1",
                AuthorizationPolicy = "custom",
                Hosts = new[] { "localhost" },
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorReporter.Errors);
        }

        [Fact]
        public async Task Rejects_UnknownAuthorizationPolicy()
        {
            var route = new ParsedRoute
            {
                RouteId = "route1",
                AuthorizationPolicy = "unknown",
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleInvalidAuthorizationPolicy && err.Message.Contains("Authorization policy 'unknown' not found"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("defaulT")]
        [InlineData("disAble")]
        public async Task Accepts_ReservedCorsPolicy(string policy)
        {
            var route = new ParsedRoute
            {
                RouteId = "route1",
                CorsPolicy = policy,
                Hosts = new[] { "localhost" },
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorReporter.Errors);
        }

        [Fact]
        public async Task Accepts_CustomCorsPolicy()
        {
            var corsOptions = new CorsOptions();
            corsOptions.AddPolicy("custom", new CorsPolicy());
            Provide<ICorsPolicyProvider>(new DefaultCorsPolicyProvider(Options.Create(corsOptions)));
            var route = new ParsedRoute
            {
                RouteId = "route1",
                CorsPolicy = "custom",
                Hosts = new[] { "localhost" },
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorReporter.Errors);
        }

        [Fact]
        public async Task Rejects_UnknownCorsPolicy()
        {
            var route = new ParsedRoute
            {
                RouteId = "route1",
                CorsPolicy = "unknown",
                ClusterId = "cluster1",
            };

            var result = await RunScenarioAsync(route);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.ErrorReporter.Errors, err => err.ErrorCode == ConfigErrors.ParsedRouteRuleInvalidCorsPolicy && err.Message.Contains("Cors policy 'unknown' not found"));
        }

        private async Task<(bool IsSuccess, TestConfigErrorReporter ErrorReporter)> RunScenarioAsync(ParsedRoute parsedRoute)
        {
            var errorReporter = new TestConfigErrorReporter();

            Mock<ITransformBuilder>().Setup(builder
                => builder.Validate(It.IsAny<IList<IDictionary<string, string>>>(), It.IsAny<string>(), It.IsAny<IConfigErrorReporter>())).Returns(true);
            var validator = Create<RouteValidator>();
            var isSuccess = await validator.ValidateRouteAsync(parsedRoute, errorReporter);
            return (isSuccess, errorReporter);
        }
    }
}
