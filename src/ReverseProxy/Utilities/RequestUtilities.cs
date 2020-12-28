// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.ReverseProxy.Utilities
{
    internal static class RequestUtilities
    {
        // TODO: this list only contains "Transfer-Encoding" because that messes up Kestrel. If we don't need to add any more here then it would be more efficient to
        // check for the single value directly. What about connection headers?
        internal static readonly HashSet<string> ResponseHeadersToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.TransferEncoding
        };

        /// <summary>
        /// Appends the given path and query to the destination prefix while avoiding duplicate '/'.
        /// </summary>
        /// <param name="destinationPrefix">The scheme, host, port, and possibly path base for the destination server.</param>
        /// <param name="path">The path to append.</param>
        /// <param name="query">The query to append</param>
        internal static Uri MakeDestinationAddress(string destinationPrefix, PathString path, QueryString query)
        {
            var builder = new StringBuilder(destinationPrefix);
            if (path.HasValue)
            {
                // When PathString has a value it always starts with a '/'. Avoid double slashes when concatenating.
                if (builder.Length > 0 && builder[^1] == '/')
                {
                    builder.Length--;
                }

                builder.Append(path.ToUriComponent());
            }
            if (query.HasValue)
            {
                builder.Append(query.ToUriComponent());
            }

            var targetAddress = builder.ToString();
            return new Uri(targetAddress, UriKind.Absolute);
        }

        // Note: HttpClient.SendAsync will end up sending the union of
        // HttpRequestMessage.Headers and HttpRequestMessage.Content.Headers.
        // We don't really care where the proxied headers appear among those 2,
        // as long as they appear in one (and only one, otherwise they would be duplicated).
        internal static void AddHeader(HttpRequestMessage request, string headerName, StringValues value)
        {
            // HttpClient wrongly uses comma (",") instead of semi-colon (";") as a separator for Cookie headers.
            // To mitigate this, we concatenate them manually and put them back as a single header value.
            // A multi-header cookie header is invalid, but we get one because of
            // https://github.com/dotnet/aspnetcore/issues/26461
            if (string.Equals(headerName, HeaderNames.Cookie, StringComparison.OrdinalIgnoreCase) && value.Count > 1)
            {
                value = string.Join("; ", value);
            }

            if (value.Count == 1)
            {
                string headerValue = value;
                if (!request.Headers.TryAddWithoutValidation(headerName, headerValue))
                {
                    var added = request.Content?.Headers.TryAddWithoutValidation(headerName, headerValue);
                    // TODO: Log. Today this assert fails for a POST request with Content-Length: 0 header which is valid.
                    // https://github.com/microsoft/reverse-proxy/issues/618
                    // Debug.Assert(added.GetValueOrDefault(), $"A header was dropped; {headerName}: {headerValue}");
                }
            }
            else
            {
                string[] headerValues = value;
                if (!request.Headers.TryAddWithoutValidation(headerName, headerValues))
                {
                    var added = request.Content?.Headers.TryAddWithoutValidation(headerName, headerValues);
                    // TODO: Log. Today this assert fails for a POST request with Content-Length: 0 header which is valid.
                    // https://github.com/microsoft/reverse-proxy/issues/618
                    // Debug.Assert(added.GetValueOrDefault(), $"A header was dropped; {headerName}: {string.Join(", ", headerValues)}");
                }
            }
        }
    }
}
