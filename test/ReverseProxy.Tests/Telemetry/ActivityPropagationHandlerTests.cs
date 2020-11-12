// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ReverseProxy.Common.Tests;
using Microsoft.ReverseProxy.Telemetry;
using Xunit;

namespace Microsoft.ReverseProxy.Telemetry.Tests
{
    public class ActivityPropagationHandlerTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SendAsync_CurrentActivitySet_RequestHeadersSet(bool useW3CFormat)
        {
            const string TraceStateString = "CustomTraceStateString";
            string expectedId = null;

            var invoker = new HttpMessageInvoker(new ActivityPropagationHandler(new MockHttpHandler(
                (HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    var headers = request.Headers;

                    Assert.True(headers.TryGetValues(useW3CFormat ? "traceparent" : "Request-Id", out var values));
                    Assert.Equal(expectedId, Assert.Single(values));

                    if (useW3CFormat)
                    {
                        Assert.True(headers.TryGetValues("tracestate", out values));
                        Assert.Equal(TraceStateString, Assert.Single(values));
                    }

                    Assert.True(headers.TryGetValues("Correlation-Context", out values));
                    Assert.Equal("foo=bar", Assert.Single(values));

                    return Task.FromResult<HttpResponseMessage>(null);
                })));

            var activity = new Activity("CustomOperation");

            if (useW3CFormat)
            {
                activity.SetIdFormat(ActivityIdFormat.W3C);
                activity.TraceStateString = TraceStateString;
                activity.SetParentId("00-01234567890123456789012345678901-0123456789012345-01");
            }
            else
            {
                activity.SetIdFormat(ActivityIdFormat.Hierarchical);
                activity.SetParentId("|root");
            }

            activity.AddBaggage("foo", "bar");

            activity.Start();
            expectedId = activity.Id;

            await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            activity.Stop();
        }
    }
}
