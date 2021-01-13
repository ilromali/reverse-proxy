// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.ReverseProxy.Utilities;
using Xunit;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    public class RequestHeaderForwardedTransformTests
    {
        [Theory]
        // Using "|" to represent multi-line headers
        [InlineData("", "https", false, "proto=https")]
        [InlineData("", "https", true, "proto=https")]
        [InlineData("existing,Header", "https", false, "proto=https")]
        [InlineData("existing|Header", "https", false, "proto=https")]
        [InlineData("existing,Header", "https", true, "existing,Header|proto=https")]
        [InlineData("existing|Header", "https", true, "existing|Header|proto=https")]
        public async Task Proto_Added(string startValue, string scheme, bool append, string expected)
        {
            var randomFactory = new TestRandomFactory();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            var proxyRequest = new HttpRequestMessage();
            proxyRequest.Headers.Add("Forwarded", startValue.Split("|", StringSplitOptions.RemoveEmptyEntries));
            var transform = new RequestHeaderForwardedTransform(randomFactory, forFormat: RequestHeaderForwardedTransform.NodeFormat.None,
                byFormat: RequestHeaderForwardedTransform.NodeFormat.None, host: false, proto: true, append);
            await transform.ApplyAsync(new RequestTransformContext()
            {
                HttpContext = httpContext,
                ProxyRequest = proxyRequest,
                HeadersCopied = true,
            });
            Assert.Equal(expected.Split("|", StringSplitOptions.RemoveEmptyEntries), proxyRequest.Headers.GetValues("Forwarded"));
        }

        [Theory]
        // Using "|" to represent multi-line headers
        [InlineData("", "myHost", false, "host=\"myHost\"")]
        [InlineData("", "myHost", true, "host=\"myHost\"")]
        [InlineData("", "ho本st", false, "host=\"xn--host-6j1i\"")]
        [InlineData("", "myHost:80", false, "host=\"myHost:80\"")]
        [InlineData("", "ho本st:80", false, "host=\"xn--host-6j1i:80\"")]
        [InlineData("existing,Header", "myHost", false, "host=\"myHost\"")]
        [InlineData("existing|Header", "myHost", false, "host=\"myHost\"")]
        [InlineData("existing|Header", "myHost:80", false, "host=\"myHost:80\"")]
        [InlineData("existing,Header", "myHost", true, "existing,Header|host=\"myHost\"")]
        [InlineData("existing|Header", "myHost", true, "existing|Header|host=\"myHost\"")]
        [InlineData("existing|Header", "myHost:80", true, "existing|Header|host=\"myHost:80\"")]
        public async Task Host_Added(string startValue, string host, bool append, string expected)
        {
            var randomFactory = new TestRandomFactory();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(host);
            var proxyRequest = new HttpRequestMessage();
            proxyRequest.Headers.Add("Forwarded", startValue.Split("|", StringSplitOptions.RemoveEmptyEntries));
            var transform = new RequestHeaderForwardedTransform(randomFactory, forFormat: RequestHeaderForwardedTransform.NodeFormat.None,
                byFormat: RequestHeaderForwardedTransform.NodeFormat.None, host: true, proto: false, append);
            await transform.ApplyAsync(new RequestTransformContext()
            {
                HttpContext = httpContext,
                ProxyRequest = proxyRequest,
                HeadersCopied = true,
            });
            Assert.Equal(expected.Split("|", StringSplitOptions.RemoveEmptyEntries), proxyRequest.Headers.GetValues("Forwarded"));
        }

        [Theory]
        // Using "|" to represent multi-line headers
        [InlineData("", "", 2, "ip", false, "for=unknown")] // Missing IP falls back to Unknown
        [InlineData("", "", 0, "ipandport", true, "for=unknown")] // Missing port excluded
        [InlineData("", "", 2, "ipandport", true, "for=\"unknown:2\"")]
        [InlineData("", "::1", 2, "unknown", false, "for=unknown")]
        [InlineData("", "::1", 2, "unknownandport", true, "for=\"unknown:2\"")]
        [InlineData("", "::1", 2, "ip", false, "for=\"[::1]\"")]
        [InlineData("", "::1", 0, "ipandport", true, "for=\"[::1]\"")]
        [InlineData("", "::1", 2, "ipandport", true, "for=\"[::1]:2\"")]
        [InlineData("", "127.0.0.1", 2, "ip", false, "for=127.0.0.1")]
        [InlineData("", "127.0.0.1", 2, "ipandport", true, "for=\"127.0.0.1:2\"")]
        [InlineData("", "::1", 2, "random", false, "for=_abcdefghi")]
        [InlineData("", "::1", 2, "randomandport", true, "for=\"_abcdefghi:2\"")]
        [InlineData("existing,header", "::1", 2, "random", false, "for=_abcdefghi")]
        [InlineData("existing,header", "::1", 2, "randomandport", true, "existing,header|for=\"_abcdefghi:2\"")]
        [InlineData("existing|header", "::1", 2, "randomandport", true, "existing|header|for=\"_abcdefghi:2\"")]
        public async Task For_Added(string startValue, string ip, int port, string formatString, bool append, string expected)
        {
            // NodeFormat is on an internal type so we can't put it in a test's public signature.
            var format = Enum.Parse<RequestHeaderForwardedTransform.NodeFormat>(formatString, ignoreCase: true);
            var randomFactory = new TestRandomFactory();
            randomFactory.Instance = new TestRandom() { Sequence = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 } }; 
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = string.IsNullOrEmpty(ip) ? null : IPAddress.Parse(ip);
            httpContext.Connection.RemotePort = port;
            var proxyRequest = new HttpRequestMessage();
            proxyRequest.Headers.Add("Forwarded", startValue.Split("|", StringSplitOptions.RemoveEmptyEntries));
            var transform = new RequestHeaderForwardedTransform(randomFactory, forFormat: format,
                byFormat: RequestHeaderForwardedTransform.NodeFormat.None, host: false, proto: false, append);
            await transform.ApplyAsync(new RequestTransformContext()
            {
                HttpContext = httpContext,
                ProxyRequest = proxyRequest,
                HeadersCopied = true,
            });
            Assert.Equal(expected.Split("|", StringSplitOptions.RemoveEmptyEntries), proxyRequest.Headers.GetValues("Forwarded"));
        }

        [Theory]
        // Using "|" to represent multi-line headers
        [InlineData("", "", 2, "ip", false, "by=unknown")] // Missing IP falls back to Unknown
        [InlineData("", "", 0, "ipandport", true, "by=unknown")] // Missing port excluded
        [InlineData("", "", 2, "ipandport", true, "by=\"unknown:2\"")]
        [InlineData("", "::1", 2, "unknown", false, "by=unknown")]
        [InlineData("", "::1", 2, "unknownandport", true, "by=\"unknown:2\"")]
        [InlineData("", "::1", 2, "ip", false, "by=\"[::1]\"")]
        [InlineData("", "::1", 0, "ipandport", true, "by=\"[::1]\"")]
        [InlineData("", "::1", 2, "ipandport", true, "by=\"[::1]:2\"")]
        [InlineData("", "127.0.0.1", 2, "ip", false, "by=127.0.0.1")]
        [InlineData("", "127.0.0.1", 2, "ipandport", true, "by=\"127.0.0.1:2\"")]
        [InlineData("", "::1", 2, "random", false, "by=_abcdefghi")]
        [InlineData("", "::1", 2, "randomandport", true, "by=\"_abcdefghi:2\"")]
        [InlineData("existing,header", "::1", 2, "random", false, "by=_abcdefghi")]
        [InlineData("existing,header", "::1", 2, "randomandport", true, "existing,header|by=\"_abcdefghi:2\"")]
        [InlineData("existing|header", "::1", 2, "randomandport", true, "existing|header|by=\"_abcdefghi:2\"")]
        public async Task By_Added(string startValue, string ip, int port, string formatString, bool append, string expected)
        {
            // NodeFormat is on an internal type so we can't put it in a test's public signature.
            var format = Enum.Parse<RequestHeaderForwardedTransform.NodeFormat>(formatString, ignoreCase: true);
            var randomFactory = new TestRandomFactory();
            randomFactory.Instance = new TestRandom() { Sequence = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.LocalIpAddress = string.IsNullOrEmpty(ip) ? null : IPAddress.Parse(ip);
            httpContext.Connection.LocalPort = port;
            var proxyRequest = new HttpRequestMessage();
            proxyRequest.Headers.Add("Forwarded", startValue.Split("|", StringSplitOptions.RemoveEmptyEntries));
            var transform = new RequestHeaderForwardedTransform(randomFactory, forFormat: RequestHeaderForwardedTransform.NodeFormat.None,
                byFormat: format, host: false, proto: false, append);
            await transform.ApplyAsync(new RequestTransformContext()
            {
                HttpContext = httpContext,
                ProxyRequest = proxyRequest,
                HeadersCopied = true,
            });
            Assert.Equal(expected.Split("|", StringSplitOptions.RemoveEmptyEntries), proxyRequest.Headers.GetValues("Forwarded"));
        }

        [Theory]
        // Using "|" to represent multi-line headers
        [InlineData("", false, "proto=https;host=\"myHost:80\";for=\"[::1]:10\";by=_abcdefghi")]
        [InlineData("", true, "proto=https;host=\"myHost:80\";for=\"[::1]:10\";by=_abcdefghi")]
        [InlineData("otherHeader", false, "proto=https;host=\"myHost:80\";for=\"[::1]:10\";by=_abcdefghi")]
        [InlineData("otherHeader", true, "otherHeader|proto=https;host=\"myHost:80\";for=\"[::1]:10\";by=_abcdefghi")]
        public async Task AllValues_Added(string startValue, bool append, string expected)
        {
            var randomFactory = new TestRandomFactory();
            randomFactory.Instance = new TestRandom() { Sequence = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("myHost", 80);
            httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;
            httpContext.Connection.RemotePort = 10;
            var proxyRequest = new HttpRequestMessage();
            proxyRequest.Headers.Add("Forwarded", startValue.Split("|", StringSplitOptions.RemoveEmptyEntries));
            var transform = new RequestHeaderForwardedTransform(randomFactory,
                forFormat: RequestHeaderForwardedTransform.NodeFormat.IpAndPort,
                byFormat: RequestHeaderForwardedTransform.NodeFormat.Random,
                host: true, proto: true, append);
            await transform.ApplyAsync(new RequestTransformContext()
            {
                HttpContext = httpContext,
                ProxyRequest = proxyRequest,
                HeadersCopied = true,
            });
            Assert.Equal(expected.Split("|", StringSplitOptions.RemoveEmptyEntries), proxyRequest.Headers.GetValues("Forwarded"));
        }

        internal class TestRandomFactory : IRandomFactory
        {
            internal TestRandom Instance { get; set; }

            public Random CreateRandomInstance()
            {
                return Instance;
            }
        }

        public class TestRandom : Random
        {
            public int[] Sequence { get; set; }
            public int Offset { get; set; }

            public override int Next(int maxValue)
            {
                return Sequence[Offset++];
            }
        }
    }
}
