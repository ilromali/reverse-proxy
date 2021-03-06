# Destination health checks
In most of the real-world systems, it's expected for their nodes to occasionally experience transient issues and go down completely due to a variety of reasons such as an overload, resource leakage, hardware failures, etc. Ideally, it'd be desirable to completely prevent those unfortunate events from occurring in a proactive way, but the cost of designing and building such an ideal system is generally prohibitively high. However, there is another reactive approach which is cheaper and aimed to minimizing a negative impact failures cause on client requests. The proxy can analyze each nodes health and stop sending client traffic to unhealthy ones until they recover. YARP implements this approach in the form of active and passive destination health checks.

## Active health checks
YARP can proactively monitor destination health by sending periodic probing requests to designated health endpoints and analyzing responses. That analysis is performed by an active health check policy specified for a cluster and results in the calculation of the new destination health states. In the end, the policy marks each destination as healthy or unhealthy based on the HTTP response code (2xx is considered healthy) and rebuilds the cluster's healthy destination collection.

There are several cluster-wide configuration settings controlling active health checks that can be set either in the config file or in code. A dedicated health endpoint can also be specified per destination.

#### File example
```JSON
"Clusters": {
      "cluster1": {
        "HealthCheck": {
          "Active": {
            "Enabled": "true",
            "Interval": "00:00:10",
            "Timeout": "00:00:10",
            "Policy": "ConsecutiveFailures",
            "Path": "/api/health"
          }
        },
        "Metadata": {
          "ConsecutiveFailuresHealthPolicy.Threshold": "3"
        },
        "Destinations": {
          "cluster1/destination1": {
            "Address": "https://localhost:10000/"
          },
          "cluster1/destination2": {
            "Address": "http://localhost:10010/",
            "Health": "http://localhost:10020/"
          }
        }
      }
```

#### Code example
```C#
var clusters = new[]
{
    new Cluster()
    {
        Id = "cluster1",
        HealthCheck = new HealthCheckOptions
        {
            Active = new ActiveHealthCheckOptions
            {
                Enabled = true,
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(10),
                Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                Path = "/api/health"
            }
        },
        Metadata = new Dictionary<string, string> { { ConsecutiveFailuresHealthPolicyOptions.ThresholdMetadataName, "5" } },
        Destinations =
        {
            { "destination1", new Destination() { Address = "https://localhost:10000" } },
            { "destination2", new Destination() { Address = "https://localhost:10010", Health = "https://localhost:10010" } }
        }
    }
};
```

### Configuration
All but one of active health check settings are specified on the cluster level in `Cluster/HealthCheck/Active` section. The only exception is an optional `Destination/Health` element specifying a separate active health check endpoint. The actual health probing URI is constructed as `Destination/Address` (or `Destination/Health` when it's set) + `Cluster/HealthCheck/Active/Path`.

Active health check settings can also be defined in code via the corresponding types in [Yarp.ReverseProxy.Abstractions](xref:Yarp.ReverseProxy.Abstractions) namespace mirroring the configuration contract.

`Cluster/HealthCheck/Active` section and [ActiveHealthCheckOptions](xref:Yarp.ReverseProxy.Abstractions.ActiveHealthCheckOptions):

- `Enabled` - flag indicating whether active health check is enabled for a cluster. Default `false`
- `Interval` - period of sending health probing requests. Default `00:00:15`
- `Timeout` - probing request timeout. Default `00:00:10`
- `Policy` - name of a policy evaluating destinations' active health states. Mandatory parameter
- `Path` -  health check path on all cluster's destinations. Default `null`.

`Destination` section and [Destination](xref:Yarp.ReverseProxy.Abstractions.Destination).

- `Health` - A dedicated health probing endpoint such as `http://destination:12345/`. Defaults `null` and falls back to `Destination/Address`.

### Built-in policies
There is currently one built-in active health check policy - `ConsecutiveFailuresHealthPolicy`. It counts consecutive health probe failures and marks a destination as unhealthy once the given threshold is reached. On the first successful response, a destination is marked as healthy and the counter is reset.
The policy parameters are set in the cluster's metadata as follows:

`ConsecutiveFailuresHealthPolicy.Threshold` - number of consecutively failed active health probing requests required to mark a destination as unhealthy. Default `2`.

### Design
The main service in this process is [IActiveHealthCheckMonitor](xref:Yarp.ReverseProxy.Service.HealthChecks.IActiveHealthCheckMonitor) that periodically creates probing requests via [IProbingRequestFactory](xref:Yarp.ReverseProxy.Service.HealthChecks.IProbingRequestFactory), sends them to all [Destinations](xref:Yarp.ReverseProxy.Abstractions.Destination) of each [Cluster](xref:Yarp.ReverseProxy.Abstractions.Cluster) with enabled active health checks and then passes all the responses down to a [IActiveHealthCheckPolicy](xref:Yarp.ReverseProxy.Service.HealthChecks.IActiveHealthCheckPolicy) specified for a cluster. `IActiveHealthCheckMonitor` doesn't make the actual decision on whether a destination is healthy or not, but delegates this duty to an `IActiveHealthCheckPolicy` specified for a cluster. A policy is called to evaluate the new health states once all probing of all cluster's destination completed. It takes in a [ClusterInfo](xref:Yarp.ReverseProxy.RuntimeModel.ClusterInfo) representing the cluster's dynamic state and a set of [DestinationProbingResult](xref:Yarp.ReverseProxy.Service.HealthChecks.DestinationProbingResult) storing cluster's destinations' probing results. Having evaluated a new health state for each destination, the policy calls [IDestinationHealthUpdater](xref:Yarp.ReverseProxy.Service.HealthChecks.IDestinationHealthUpdater) to actually update [DestinationHealthState.Active](xref:Yarp.ReverseProxy.RuntimeModel.DestinationHealthState.Active) values.

```
-{For each cluster's destination}-
IActiveHealthCheckMonitor <--(Create probing request)--> IProbingRequestFactory
        |
        V
 HttpMessageInvoker <--(Send probe and receive response)--> Destination
        |
(Save probing result)
        |
        V
DestinationProbingResult
--------------{END}---------------
        |
(Evaluate new destination active health states using probing results)
        |
        V
IActiveHealthCheckPolicy --(New active health states)--> IDestinationHealthUpdater --(Update each destination's)--> DestinationInfo.Health.Active
```
There are default built-in implementations for all of the aforementioned components which can also be replaced with custom ones when necessary.

### Extensibility
There are 2 main extensibility points in the active health check subsystem.

#### IActiveHealthCheckPolicy
[IActiveHealthCheckPolicy](xref:Yarp.ReverseProxy.Service.HealthChecks.IActiveHealthCheckPolicy) analyzes how destinations respond to active health probes sent by `IActiveHealthCheckMonitor`, evaluates new active health states for all the probed destinations, and then call `IDestinationHealthUpdater.SetActive` to set new active health states and rebuild the healthy destination collection based on the updated values.

The below is a simple example of a custom `IActiveHealthCheckPolicy` marking destination as `Healthy`, if a successful response code was returned for a probe, and as `Unhealthy` otherwise.

```C#
public class FirstUnsuccessfulResponseHealthPolicy : IActiveHealthCheckPolicy
{
    private readonly IDestinationHealthUpdater _healthUpdater;

    public FirstUnsuccessfulResponseHealthPolicy(IDestinationHealthUpdater healthUpdater)
    {
        _healthUpdater = healthUpdater;
    }

    public string Name => "FirstUnsuccessfulResponse";

    public void ProbingCompleted(ClusterInfo cluster, IReadOnlyList<DestinationProbingResult> probingResults)
    {
        if (probingResults.Count == 0)
        {
            return;
        }

        var newHealthStates = new NewActiveDestinationHealth[probingResults.Count];
        for (var i = 0; i < probingResults.Count; i++)
        {
            var response = probingResults[i].Response;
            var newHealth = response != null && response.IsSuccessStatusCode ? DestinationHealth.Healthy : DestinationHealth.Unhealthy;
            newHealthStates[i] = new NewActiveDestinationHealth(probingResults[i].Destination, newHealth);
        }

        _healthUpdater.SetActive(cluster, newHealthStates);
    }
}
```

#### IProbingRequestFactory
[IProbingRequestFactory](xref:Yarp.ReverseProxy.Service.HealthChecks.IProbingRequestFactory) creates active health probing requests to be sent to destination health endpoints. It can take into account `ActiveHealthCheckOptions.Path`, `DestinationConfig.Health`, and other configuration settings to construct probing requests.

The below is a simple example of a customer `IProbingRequestFactory` concatenating `DestinationConfig.Address` and a fixed health probe path to create the probing request URI.

```C#
public class CustomProbingRequestFactory : IProbingRequestFactory
{
    public HttpRequestMessage CreateRequest(ClusterConfig clusterConfig, DestinationConfig destinationConfig)
    {
        var probeUri = new Uri(destinationConfig.Address + "/api/probe-health");
        return new HttpRequestMessage(HttpMethod.Get, probeUri) { Version = ProtocolHelper.Http11Version };
    }
}
```

## Passive health checks
YARP can passively watch for successes and failures in client request proxying to reactively evaluate destination health states. Responses to the proxied requests are intercepted by a dedicated passive health check middleware which passes them to a policy configured on the cluster. The policy analyzes the responses to evaluate if the destinations that produced them are healthy or not. Then, it calculates and assigns new passive health states to the respective destinations and rebuilds the cluster's healthy destination collection.

Note the response is normally send to the client before the passive health policy runs, so a policy cannot intercept the response body, nor modify anything in the response headers unless the proxy application introduces full response buffering.

There is one important difference from the active health check logic. Once a destination gets assigned an unhealthy passive state, it stops receiving all new traffic which blocks future health reevaluation. The policy also schedules a destination reactivation after the configured period. Reactivation means resetting the passive health state from `Unhealthy` back to the initial `Unknown` value which makes destination eligible for traffic again.

There are several cluster-wide configuration settings controlling passive health checks that can be set either in the config file or in code.

#### File example
```JSON
"Clusters": {
      "cluster1": {
        "HealthCheck": {
          "Passive": {
            "Enabled": "true",
            "Policy": "TransportFailureRate",
            "ReactivationPeriod": "00:02:00"
          }
        },
        "Metadata": {
          "TransportFailureRateHealthPolicy.RateLimit": "0.5"
        },
        "Destinations": {
          "cluster1/destination1": {
            "Address": "https://localhost:10000/"
          },
          "cluster1/destination2": {
            "Address": "http://localhost:10010/"
          }
        }
      }
```

#### Code example
```C#
var clusters = new[]
{
    new Cluster()
    {
        Id = "cluster1",
        HealthCheck = new HealthCheckOptions
        {
            Passive = new PassiveHealthCheckOptions
            {
                Enabled = true,
                Policy = HealthCheckConstants.PassivePolicy.TransportFailureRate,
                ReactivationPeriod = TimeSpan.FromMinutes(2)
            }
        },
        Metadata = new Dictionary<string, string> { { TransportFailureRateHealthPolicyOptions.FailureRateLimitMetadataName, "0.5" } },
        Destinations =
        {
            { "destination1", new Destination() { Address = "https://localhost:10000" } },
            { "destination2", new Destination() { Address = "https://localhost:10010" } }
        }
    }
};
```

### Configuration
Passive health check settings are specified on the cluster level in `Cluster/HealthCheck/Passive` section. Alternatively, they can be defined in code via the corresponding types in [Yarp.ReverseProxy.Abstractions](xref:Yarp.ReverseProxy.Abstractions) namespace mirroring the configuration contract.

Passive health checks require the `PassiveHealthCheckMiddleware` added into the pipeline for them to work. The default `MapReverseProxy(this IEndpointRouteBuilder endpoints)` method does it automatically, but in case of a manual pipeline construction the [UsePassiveHealthChecks](xref:Microsoft.AspNetCore.Builder.ProxyMiddlewareAppBuilderExtensions) method must be called to add that middleware as shown in the example below.

```C#
endpoints.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseAffinitizedDestinationLookup();
    proxyPipeline.UseProxyLoadBalancing();
    proxyPipeline.UseRequestAffinitizer();
    proxyPipeline.UsePassiveHealthChecks();
});
```

`Cluster/HealthCheck/Passive` section and [PassiveHealthCheckOptions](xref:Yarp.ReverseProxy.Abstractions.PassiveHealthCheckOptions):

- `Enabled` - flag indicating whether passive health check is enabled for a cluster. Default `false`
- `Policy` - name of a policy evaluating destinations' passive health states. Mandatory parameter
- `ReactivationPeriod` - period after which an unhealthy destination's passive health state is reset to `Unknown` and it starts receiving traffic again. Default value is `null` which means the period will be set by a `IPassiveHealthCheckPolicy`

### Built-in policies
There is currently one built-in passive health check policy - `TransportFailureRateHealthPolicy`. It calculates the proxied requests failure rate for each destination and marks it as unhealthy if the specified limit is exceeded. Rate is calculated as a percentage of failured requests to the total number of request proxied to a destination in the given period of time. Failed and total counters are tracked in a sliding time window which means that only the recent readings fitting in the window are taken into account.
There are two sets of policy parameters defined globally and on per cluster level.

Global parameters are set via the options mechanism using `TransportFailureRateHealthPolicyOptions` type with the following properties:

- `DetectionWindowSize` - period of time while detected failures are kept and taken into account in the rate calculation. Default is `00:01:00`.
- `MinimalTotalCountThreshold` - minimal total number of requests which must be proxied to a destination within the detection window before this policy starts evaluating the destination's health and enforcing the failure rate limit. Default is `10`.
- `DefaultFailureRateLimit` - default failure rate limit for a destination to be marked as unhealthy that is applied if it's not set on a cluster's metadata. The value is in range `(0,1)`. Default is `0.3` (30%).

Global policy options can be set in code as follows:
```C#
services.Configure<TransportFailureRateHealthPolicyOptions>(o =>
{
    o.DetectionWindowSize = TimeSpan.FromSeconds(30);
    o.MinimalTotalCountThreshold = 5;
    o.DefaultFailureRateLimit = 0.5;
});
```

Cluster-specific parameters are set in the cluster's metadata as follows:

`TransportFailureRateHealthPolicy.RateLimit` - failure rate limit for a destination to be marked as unhealhty. The value is in range `(0,1)`. Default value is provided by the global `DefaultFailureRateLimit` parameter.

### Design
The main component is [PassiveHealthCheckMiddleware](xref:Yarp.ReverseProxy.Middleware.PassiveHealthCheckMiddleware) sitting in the request pipeline and analyzing responses returned by destinations. For each response from a destination belonging to a cluster with enabled passive health checks, `PassiveHealthCheckMiddleware` invokes an [IPassiveHealthCheckPolicy](xref:Yarp.ReverseProxy.Service.HealthChecks.IPassiveHealthCheckPolicy) specified for the cluster. The policy analyzes the given response, evaluates a new destination's passive health state and calls [IDestinationHealthUpdater](xref:Yarp.ReverseProxy.Service.HealthChecks.IDestinationHealthUpdater) to actually update [DestinationHealthState.Passive](xref:Yarp.ReverseProxy.RuntimeModel.DestinationHealthState.Passive) value. The update happens asynchronously in the background and doesn't block the request pipeline. When a destination gets marked as unhealthy, it stops receiving new requests until it gets reactivated after a configured period. Reactivation means the destination's `DestinationHealthState.Passive` state is reset from `Unhealthy` to `Unknown` and the cluster's list of healthy destinations is rebuilt to include it. A reactivation is scheduled by `IDestinationHealthUpdater` right after setting the destination's `DestinationHealthState.Passive` to `Unhealthy`.

```
      (Respose to a proxied request)
                  |
      PassiveHealthCheckMiddleware
                  |
                  V
      IPassiveHealthCheckPolicy
                  |
    (Evaluate new passive health state)
                  |
      IDestinationHealthUpdater --(Asynchronously update passive state)--> DestinationInfo.Health.Passive
                  |
                  V
      (Schedule a reactivation) --(Set to Unknown)--> DestinationInfo.Health.Passive
```

### Extensibility
There is one main extensibility point in the passive health check subsystem, the `IPassiveHealthCheckPolicy`.

#### IPassiveHealthCheckPolicy
[IPassiveHealthCheckPolicy](xref:Yarp.ReverseProxy.Service.HealthChecks.IPassiveHealthCheckPolicy) analyzes how a destination responded to a proxied client request, evaluates its new passive health state and finally calls `IDestinationHealthUpdater.SetPassiveAsync` to create an async task actually updating the passive health state and rebuilding the healthy destination collection.

The below is a simple example of a custom `IPassiveHealthCheckPolicy` marking destination as `Unhealthy` on the first unsuccessful response to a proxied request.

```C#
public class FirstUnsuccessfulResponseHealthPolicy : IPassiveHealthCheckPolicy
{
    private static readonly TimeSpan _defaultReactivationPeriod = TimeSpan.FromSeconds(60);
    private readonly IDestinationHealthUpdater _healthUpdater;

    public FirstUnsuccessfulResponseHealthPolicy(IDestinationHealthUpdater healthUpdater)
    {
        _healthUpdater = healthUpdater;
    }

    public string Name => "FirstUnsuccessfulResponse";

    public void RequestProxied(ClusterInfo cluster, DestinationInfo destination, HttpContext context)
    {
        var error = context.Features.Get<IProxyErrorFeature>();
        if (error != null)
        {
            var reactivationPeriod = cluster.Config.HealthCheckOptions.Passive.ReactivationPeriod ?? _defaultReactivationPeriod;
            _ = _healthUpdater.SetPassiveAsync(cluster, destination, DestinationHealth.Unhealthy, reactivationPeriod);
        }
    }
}
```

## Healthy destination collection
Under normal conditions, YARP excludes unhealthy destinations when selecting the one that will receive a request. On each cluster, they are stored in a list which gets rebuilt when any destination's health state changes. Active and passive health states are combined to filter out unhealthy destinations while rebuilding that list. The exact filtering rule as follows.

- Active and passive health state are initialized with `Unkown` value which can be later changed to either `Healty` or `Unhealthy` by the corresponding polices.
- A destination is added on the healthy destination list when all of the following is TRUE:
  - Active health checks are disabled on the cluster OR `DestinationHealthState.Active != DestinationHealth.Unhealthy`
  - Passive health checks are disabled on the cluster OR `DestinationHealthState.Passive != DestinationHealth.Unhealthy`
