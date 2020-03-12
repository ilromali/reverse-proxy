﻿// <copyright file="RouteParserTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System.Collections.Generic;
using FluentAssertions;
using IslandGateway.Core.Abstractions;
using Tests.Common;
using Xunit;

namespace IslandGateway.Core.Service.Tests
{
    public class RouteParserTests : TestAutoMockBase
    {
        [Fact]
        public void Constructor_Works()
        {
            this.Create<RouteParser>();
        }

        [Fact]
        public void ParseRoute_ValidRoute_Works()
        {
            // Arrange
            const string TestRouteId = "route1";
            const string TestBackendId = "backend1";
            const string TestRule = "Host('example.com')";
            const int TestPriority = 2;

            var matchers = new[] { new HostMatcher("Host", new[] { "example.com" }) };
            var ruleParseResult = Result<IList<RuleMatcherBase>, string>.Success(matchers);
            this.Mock<IRuleParser>()
                .Setup(r => r.Parse(TestRule))
                .Returns(ruleParseResult);
            var routeParser = this.Create<RouteParser>();
            var route = new GatewayRoute
            {
                RouteId = TestRouteId,
                Rule = TestRule,
                Priority = TestPriority,
                BackendId = TestBackendId,
                Metadata = new Dictionary<string, string>
                {
                    { "key", "value" },
                },
            };
            var errorReporter = new TestConfigErrorReporter();

            // Act
            var result = routeParser.ParseRoute(route, errorReporter);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.RouteId.Should().Be(TestRouteId);
            result.Value.Rule.Should().Be(TestRule);
            result.Value.Priority.Should().Be(TestPriority);
            result.Value.Matchers.Should().BeSameAs(matchers);
            result.Value.BackendId.Should().Be(TestBackendId);
            result.Value.Metadata["key"].Should().Be("value");
            errorReporter.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ParseRoute_NullRule_ReportsError()
        {
            // Arrange
            var route = new GatewayRoute { RouteId = null };
            var errorReporter = new TestConfigErrorReporter();
            var routeParser = this.Create<RouteParser>();

            // Act
            var result = routeParser.ParseRoute(route, errorReporter);

            // Assert
            result.IsSuccess.Should().BeFalse();
            errorReporter.Errors.Should().Contain(err => err.ErrorCode == ConfigErrors.RouteBadRule);
        }

        [Fact]
        public void ParseRoute_ParseError_ReportsError()
        {
            // Arrange
            const string TestRule = "bad rule";
            const string TestParserErrorMessage = "parser error message";

            var ruleParseResult = Result<IList<RuleMatcherBase>, string>.Failure(TestParserErrorMessage);
            this.Mock<IRuleParser>()
                .Setup(r => r.Parse(TestRule))
                .Returns(ruleParseResult);
            var route = new GatewayRoute
            {
                RouteId = null,
                Rule = TestRule,
            };
            var errorReporter = new TestConfigErrorReporter();
            var routeParser = this.Create<RouteParser>();

            // Act
            var result = routeParser.ParseRoute(route, errorReporter);

            // Assert
            result.IsSuccess.Should().BeFalse();
            errorReporter.Errors.Should().Contain(err => err.ErrorCode == ConfigErrors.RouteBadRule && err.Message.Contains(TestParserErrorMessage));
        }
    }
}
