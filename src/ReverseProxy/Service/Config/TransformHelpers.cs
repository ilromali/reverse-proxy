// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Yarp.ReverseProxy.Abstractions.Config;

namespace Yarp.ReverseProxy.Service.Config
{
    public static class TransformHelpers
    {
        public static void TryCheckTooManyParameters(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> rawTransform, int expected)
        {
            if (rawTransform.Count > expected)
            {
                context.Errors.Add(new InvalidOperationException("The transform contains more parameters than expected: " + string.Join(';', rawTransform.Keys)));
            }
        }

        public static void CheckTooManyParameters(IReadOnlyDictionary<string, string> rawTransform, int expected)
        {
            if (rawTransform.Count > expected)
            {
                throw new InvalidOperationException("The transform contains more parameters than expected: " + string.Join(';', rawTransform.Keys));
            }
        }
    }
}
