// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Yarp.ReverseProxy.Service.RuntimeModel.Transforms
{
    /// <summary>
    /// Sets or appends the X-Forwarded-PathBase header with the request's original PathBase.
    /// </summary>
    public class RequestHeaderXForwardedPathBaseTransform : RequestTransform
    {
        public RequestHeaderXForwardedPathBaseTransform(string headerName, bool append)
        {
            HeaderName = headerName ?? throw new System.ArgumentNullException(nameof(headerName));
            Append = append;
        }

        internal string HeaderName { get; }
        internal bool Append { get; }

        /// <inheritdoc/>
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            var existingValues = TakeHeader(context, HeaderName);

            var pathBase = context.HttpContext.Request.PathBase;

            if (!pathBase.HasValue)
            {
                if (Append && !string.IsNullOrEmpty(existingValues))
                {
                    AddHeader(context, HeaderName, existingValues);
                }
            }
            else if (Append)
            {
                var values = StringValues.Concat(existingValues, pathBase.ToUriComponent());
                AddHeader(context, HeaderName, values);
            }
            else
            {
                // Set
                AddHeader(context, HeaderName, pathBase.ToUriComponent());
            }

            return default;
        }
    }
}
