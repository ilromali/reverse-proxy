// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Service.Config;
using Yarp.ReverseProxy.Service.RuntimeModel.Transforms;

namespace Yarp.ReverseProxy.Abstractions.Config
{
    /// <summary>
    /// Extensions for adding path transforms.
    /// </summary>
    public static class PathTransformExtensions
    {
        /// <summary>
        /// Clones the route and adds the transform which sets the request path with the given value.
        /// </summary>
        public static ProxyRoute WithTransformPathSet(this ProxyRoute proxyRoute, PathString path)
        {
            return proxyRoute.WithTransform(transform =>
            {
                transform[PathTransformFactory.PathSetKey] = path.Value;
            });
        }

        /// <summary>
        /// Adds the transform which sets the request path with the given value.
        /// </summary>
        public static TransformBuilderContext AddPathSet(this TransformBuilderContext context, PathString path)
        {
            context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Set, path));
            return context;
        }

        /// <summary>
        /// Clones the route and adds the transform which will prefix the request path with the given value.
        /// </summary>
        public static ProxyRoute WithTransformPathPrefix(this ProxyRoute proxyRoute, PathString prefix)
        {
            return proxyRoute.WithTransform(transform =>
            {
                transform[PathTransformFactory.PathPrefixKey] = prefix.Value;
            });
        }

        /// <summary>
        /// Adds the transform which will prefix the request path with the given value.
        /// </summary>
        public static TransformBuilderContext AddPathPrefix(this TransformBuilderContext context, PathString prefix)
        {
            context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Prefix, prefix));
            return context;
        }

        /// <summary>
        /// Clones the route and adds the transform which will remove the matching prefix from the request path.
        /// </summary>
        public static ProxyRoute WithTransformPathRemovePrefix(this ProxyRoute proxyRoute, PathString prefix)
        {
            return proxyRoute.WithTransform(transform =>
            {
                transform[PathTransformFactory.PathRemovePrefixKey] = prefix.Value;
            });
        }

        /// <summary>
        /// Adds the transform which will remove the matching prefix from the request path.
        /// </summary>
        public static TransformBuilderContext AddPathRemovePrefix(this TransformBuilderContext context, PathString prefix)
        {
            context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.RemovePrefix, prefix));
            return context;
        }

        /// <summary>
        /// Clones the route and adds the transform which will set the request path with the given value.
        /// </summary>
        public static ProxyRoute WithTransformPathRouteValues(this ProxyRoute proxyRoute, PathString pattern)
        {
            return proxyRoute.WithTransform(transform =>
            {
                transform[PathTransformFactory.PathPatternKey] = pattern.Value;
            });
        }

        /// <summary>
        /// Clones the route and adds the transform which will set the request path with the given value.
        /// </summary>
        public static TransformBuilderContext AddPathRouteValues(this TransformBuilderContext context, PathString pattern)
        {
            var binder = context.Services.GetRequiredService<TemplateBinderFactory>();
            context.RequestTransforms.Add(new PathRouteValuesTransform(pattern.Value, binder));
            return context;
        }
    }
}
