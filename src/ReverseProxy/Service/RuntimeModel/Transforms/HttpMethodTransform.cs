// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    internal class HttpMethodTransform : RequestTransform
    {
        public HttpMethodTransform(string fromMethod, string toMethod)
        {
            FromMethod = GetCanonicalizedValue(fromMethod);
            ToMethod = GetCanonicalizedValue(toMethod);
        }

        internal HttpMethod FromMethod { get; }

        internal HttpMethod ToMethod { get; }

        public override Task ApplyAsync(RequestTransformContext context)
        {
            if (FromMethod.Equals(context.ProxyRequest.Method))
            {
                context.ProxyRequest.Method = ToMethod;
            }

            return Task.CompletedTask;
        }

        private static HttpMethod GetCanonicalizedValue(string method)
        {
            return method switch
            {
                string _ when HttpMethods.IsGet(method) => HttpMethod.Get,
                string _ when HttpMethods.IsPost(method) => HttpMethod.Post,
                string _ when HttpMethods.IsPut(method) => HttpMethod.Put,
                string _ when HttpMethods.IsDelete(method) => HttpMethod.Delete,
                string _ when HttpMethods.IsOptions(method) => HttpMethod.Options,
                string _ when HttpMethods.IsHead(method) => HttpMethod.Head,
                string _ when HttpMethods.IsPatch(method) => HttpMethod.Patch,
                string _ when HttpMethods.IsTrace(method) => HttpMethod.Trace,
                string _ => new HttpMethod(method),
            };
        }
    }
}
