// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Service.HealthChecks;

namespace Yarp.ReverseProxy.Sample.Controllers
{
    /// <summary>
    /// Controller for health check api.
    /// </summary>
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IActiveHealthCheckMonitor _healthCheckMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController" /> class.
        /// </summary>
        public HealthController(IActiveHealthCheckMonitor healthCheckMonitor)
        {
            _healthCheckMonitor = healthCheckMonitor;
        }

        /// <summary>
        /// Returns 200 if Proxy is healthy.
        /// </summary>
        [HttpGet]
        [Route("/api/health")]
        public IActionResult CheckHealth()
        {
            // TODO: Implement health controller, use guid in route.
            return _healthCheckMonitor.InitialDestinationsProbed ? Ok() : StatusCode(503);
        }
    }
}
