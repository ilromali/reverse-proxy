// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ReverseProxy.Service.Config;
using Microsoft.ReverseProxy.Service.RuntimeModel.Transforms;

namespace Microsoft.ReverseProxy.Abstractions.Config
{
    /// <summary>
    /// Extensions for adding query transforms.
    /// </summary>
    public static class QueryTransformExtensions
    {
        /// <summary>
        /// Clones the route and adds the transform that will append or set the query parameter from the given value.
        /// </summary>
        public static ProxyRoute WithTransformQueryValue(this ProxyRoute proxyRoute, string queryKey, string value, bool append = true)
        {
            var type = append ? QueryTransformFactory.AppendKey : QueryTransformFactory.SetKey;
            return proxyRoute.WithTransform(transform =>
            {
                transform[QueryTransformFactory.QueryValueParameterKey] = queryKey;
                transform[type] = value;
            });
        }

        /// <summary>
        /// Adds the transform that will append or set the query parameter from the given value.
        /// </summary>
        public static TransformBuilderContext AddQueryValue(this TransformBuilderContext context, string queryKey, string value, bool append = true)
        {
            context.RequestTransforms.Add(new QueryParameterFromStaticTransform(
                append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set,
                queryKey, value));
            return context;
        }

        /// <summary>
        /// Clones the route and adds the transform that will append or set the query parameter from a route value.
        /// </summary>
        public static ProxyRoute WithTransformQueryRouteValue(this ProxyRoute proxyRoute, string queryKey, string routeValueKey, bool append = true)
        {
            var type = append ? QueryTransformFactory.AppendKey : QueryTransformFactory.SetKey;
            return proxyRoute.WithTransform(transform =>
            {
                transform[QueryTransformFactory.QueryRouteParameterKey] = queryKey;
                transform[type] = routeValueKey;
            });
        }

        /// <summary>
        /// Adds the transform that will append or set the query parameter from a route value.
        /// </summary>
        public static TransformBuilderContext AddQueryRouteValue(this TransformBuilderContext context, string queryKey, string routeValueKey, bool append = true)
        {
            context.RequestTransforms.Add(new QueryParameterRouteTransform(
                append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set,
                queryKey, routeValueKey));
            return context;
        }

        /// <summary>
        /// Clones the route and adds the transform that will remove the given query key.
        /// </summary>
        public static ProxyRoute WithTransformQueryRemoveKey(this ProxyRoute proxyRoute, string queryKey)
        {
            return proxyRoute.WithTransform(transform =>
            {
                transform[QueryTransformFactory.QueryRemoveParameterKey] = queryKey;
            });
        }

        /// <summary>
        /// Adds the transform that will remove the given query key.
        /// </summary>
        public static TransformBuilderContext AddQueryRemoveKey(this TransformBuilderContext context, string queryKey)
        {
            context.RequestTransforms.Add(new QueryParameterRemoveTransform(queryKey));
            return context;
        }
    }
}
