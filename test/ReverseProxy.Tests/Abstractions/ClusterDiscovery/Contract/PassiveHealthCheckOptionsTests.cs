// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Xunit;

namespace Yarp.ReverseProxy.Abstractions.Tests
{
    public class PassiveHealthCheckOptionsTests
    {
        [Fact]
        public void Equals_Same_Value_Returns_True()
        {
            var options1 = new PassiveHealthCheckOptions
            {
                Enabled = true,
                Policy = "Passive",
                ReactivationPeriod = TimeSpan.FromSeconds(5),
            };

            var options2 = new PassiveHealthCheckOptions
            {
                Enabled = true,
                Policy = "Passive",
                ReactivationPeriod = TimeSpan.FromSeconds(5),
            };

            var equals = options1.Equals(options2);

            Assert.True(equals);
        }

        [Fact]
        public void Equals_Different_Value_Returns_False()
        {
            var options1 = new PassiveHealthCheckOptions
            {
                Enabled = true,
                Policy = "Passive",
                ReactivationPeriod = TimeSpan.FromSeconds(5),
            };

            var options2 = new PassiveHealthCheckOptions
            {
                Enabled = false,
                Policy = "Passive",
                ReactivationPeriod = TimeSpan.FromSeconds(1),
            };

            var equals = options1.Equals(options2);

            Assert.False(equals);
        }

        [Fact]
        public void Equals_Second_Null_Returns_False()
        {
            var options1 = new PassiveHealthCheckOptions();

            var equals = options1.Equals(null);

            Assert.False(equals);
        }
    }
}
