using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Middleware;
using Prometheus;

namespace Yarp.Sample
{
    public class YarpPrometheusMetrics
    {
        private static readonly CounterConfiguration counterConfig = new CounterConfiguration
        {
            LabelNames = new[] { "Route", "Cluster", "Destination" }
        };

        private static readonly Counter _requestsProcessed = Metrics.CreateCounter(
            "yarp_requests_processed",
            "Number of requests through the proxy",
            counterConfig
        );

        private static readonly Counter _requestContentBytes = Metrics.CreateCounter(
            "yarp_request_content_bytes",
            "Bytes for request bodies sent through the proxy",
            counterConfig
         );

        private static readonly Counter _responseContentBytes = Metrics.CreateCounter(
            "yarp_response_content_bytes",
            "Bytes for request bodies sent through the proxy",
            counterConfig
        );

        private static readonly Histogram _requestDuration = Metrics.CreateHistogram(
            "yarp_request_duration",
            "Histogram of request processing durations.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "Route", "Cluster", "Destination" }
            });

        private static readonly Counter _requestsSuccessfull = Metrics.CreateCounter(
            "yarp_requests_success",
            "Number of requests with a 2xx status code",
            counterConfig
        );

        private static readonly Counter _requests_error_4xx = Metrics.CreateCounter(
            "yarp_requests_error_4xx",
            "Number of requests with a 4xx status code",
            counterConfig
        );

        private static readonly Counter _requests_error_5xx = Metrics.CreateCounter(
            "yarp_requests_error_5xx",
            "Number of requests with a 5xx status code",
            counterConfig
        );

        public async Task ReportForYarp(HttpContext context, Func<Task> next)
        {
            var started = DateTime.Now;
            var proxyFeature = context.GetRequiredProxyFeature();
            await next();
            var duration = (DateTime.Now - started).TotalMilliseconds;

            string[] labelvalues = { proxyFeature.RouteSnapshot.ProxyRoute.RouteId, proxyFeature.ClusterSnapshot.Options.Id, proxyFeature.ProxiedDestination.Config.Options.Address };
            _requestDuration.WithLabels(labelvalues).Observe(duration);
            _requestsProcessed.WithLabels(labelvalues).Inc();
            if (context.Request.ContentLength.HasValue) { _requestContentBytes.WithLabels(labelvalues).Inc(context.Request.ContentLength.Value); }
            if (context.Response.ContentLength.HasValue) { _responseContentBytes.WithLabels(labelvalues).Inc(context.Response.ContentLength.Value); }

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300) { _requestsSuccessfull.Inc(); }
            else if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500) { _requests_error_4xx.Inc(); }
            else if (context.Response.StatusCode >= 500 && context.Response.StatusCode < 600) { _requests_error_5xx.Inc(); }
        }
    }
}
