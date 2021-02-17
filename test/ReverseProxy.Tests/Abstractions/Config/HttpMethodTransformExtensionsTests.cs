// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.Service.Config;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;
using Xunit;

namespace Microsoft.ReverseProxy.Abstractions.Config
{
    public class HttpMethodTransformExtensionsTests : TransformExtentionsTestsBase
    {
        private readonly HttpMethodTransformFactory _factory = new();

        [Fact]
        public void WithTransformHttpMethodChange()
        {
            var proxyRoute = new ProxyRoute();
            proxyRoute = proxyRoute.WithTransformHttpMethodChange(HttpMethods.Put, HttpMethods.Post);

            var builderContext = ValidateAndBuild(proxyRoute, _factory);

            ValidateHttpMethod(builderContext);
        }

        [Fact]
        public void AddHttpMethodChange()
        {
            var builderContext = CreateBuilderContext();
            builderContext.AddHttpMethodChange(HttpMethods.Put, HttpMethods.Post);

            ValidateHttpMethod(builderContext);
        }

        private static void ValidateHttpMethod(TransformBuilderContext builderContext)
        {
            var requestTransform = Assert.Single(builderContext.RequestTransforms);
            var httpMethodTransform = Assert.IsType<HttpMethodChangeTransform>(requestTransform);
            Assert.Equal(HttpMethod.Put, httpMethodTransform.FromMethod);
            Assert.Equal(HttpMethod.Post, httpMethodTransform.ToMethod);
        }
    }
}
