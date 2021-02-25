// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    /// <summary>
    /// Sets or appends the X-Forwarded-Proto header with the request's original url scheme.
    /// </summary>
    public class RequestHeaderXForwardedProtoTransform : RequestTransform
    {
        public RequestHeaderXForwardedProtoTransform(string headerName, bool append)
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

            var scheme = context.HttpContext.Request.Scheme;

            if (Append)
            {
                var values = StringValues.Concat(existingValues, scheme);
                AddHeader(context, HeaderName, values);
            }
            else
            {
                // Set
                AddHeader(context, HeaderName, scheme);
            }

            return default;
        }
    }
}
