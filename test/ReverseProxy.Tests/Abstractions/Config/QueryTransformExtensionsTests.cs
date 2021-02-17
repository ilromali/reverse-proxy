// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ReverseProxy.Service.Config;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using Xunit;

namespace Microsoft.ReverseProxy.Abstractions.Config
{
    public class QueryTransformExtensionsTests : TransformExtentionsTestsBase
    {
        private readonly QueryTransformFactory _factory = new();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithTransformQueryRouteValue(bool append)
        {
            var proxyRoute = new ProxyRoute();
            proxyRoute = proxyRoute.WithTransformQueryRouteValue("key", "value", append);

            var builderContext = ValidateAndBuild(proxyRoute, _factory);

            ValidateQueryRouteParameter(append, builderContext);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddQueryRouteValue(bool append)
        {
            var builderContext = CreateBuilderContext();
            builderContext.AddQueryRouteValue("key", "value", append);

            ValidateQueryRouteParameter(append, builderContext);
        }

        private static void ValidateQueryRouteParameter(bool append, TransformBuilderContext builderContext)
        {
            var requestTransform = Assert.Single(builderContext.RequestTransforms);
            var queryParameterRouteTransform = Assert.IsType<QueryParameterRouteTransform>(requestTransform);
            Assert.Equal("key", queryParameterRouteTransform.Key);
            Assert.Equal("value", queryParameterRouteTransform.RouteValueKey);
            var expectedMode = append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set;
            Assert.Equal(expectedMode, queryParameterRouteTransform.Mode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithTransformQueryValue(bool append)
        {
            var proxyRoute = new ProxyRoute();
            proxyRoute = proxyRoute.WithTransformQueryValue("key", "value", append);

            var builderContext = ValidateAndBuild(proxyRoute, _factory);

            ValidateQueryValue(append, builderContext);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddQueryValue(bool append)
        {
            var builderContext = CreateBuilderContext();
            builderContext.AddQueryValue("key", "value", append);

            ValidateQueryValue(append, builderContext);
        }

        private static void ValidateQueryValue(bool append, TransformBuilderContext builderContext)
        {
            var requestTransform = Assert.Single(builderContext.RequestTransforms);
            var queryParameterFromStaticTransform = Assert.IsType<QueryParameterFromStaticTransform>(requestTransform);
            Assert.Equal("key", queryParameterFromStaticTransform.Key);
            Assert.Equal("value", queryParameterFromStaticTransform.Value);
            var expectedMode = append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set;
            Assert.Equal(expectedMode, queryParameterFromStaticTransform.Mode);
        }

        [Fact]
        public void WithTransformQueryRemoveKey()
        {
            var proxyRoute = new ProxyRoute();
            proxyRoute = proxyRoute.WithTransformQueryRemoveKey("key");

            var builderContext = ValidateAndBuild(proxyRoute, _factory);

            ValidateQueryRemoveKey(builderContext);
        }

        [Fact]
        public void AddQueryRemoveKey()
        {
            var builderContext = CreateBuilderContext();
            builderContext.AddQueryRemoveKey("key");

            ValidateQueryRemoveKey(builderContext);
        }

        private static void ValidateQueryRemoveKey(TransformBuilderContext builderContext)
        {
            var requestTransform = Assert.Single(builderContext.RequestTransforms);
            var removeQueryParameterTransform = Assert.IsType<QueryParameterRemoveTransform>(requestTransform);
            Assert.Equal("key", removeQueryParameterTransform.Key);
        }
    }
}
