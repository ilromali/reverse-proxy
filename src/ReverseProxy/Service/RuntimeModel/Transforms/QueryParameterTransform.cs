// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.ReverseProxy.Service.RuntimeModel.Transforms
{
    public abstract class QueryParameterTransform : RequestTransform
    {
        public QueryParameterTransform(QueryStringTransformMode mode, string key)
        {
            Mode = mode;
            Key = key;
        }

        internal QueryStringTransformMode Mode { get; }

        internal string Key { get; }

        /// <inheritdoc/>
        public override Task ApplyAsync(RequestTransformContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var value = GetValue(context);
            if (!string.IsNullOrEmpty(value))
            {
                switch (Mode)
                {
                    case QueryStringTransformMode.Append:
                        StringValues newValue = value;
                        if (context.Query.Collection.TryGetValue(Key, out var currentValue))
                        {
                             newValue = StringValues.Concat(currentValue, value);
                        }
                        context.Query.Collection[Key] = newValue;
                        break;
                    case QueryStringTransformMode.Set:
                        context.Query.Collection[Key] = value;
                        break;
                    default:
                        throw new NotImplementedException(Mode.ToString());
                }
            }

            return Task.CompletedTask;
        }

        protected abstract string GetValue(RequestTransformContext context);
    }

    public enum QueryStringTransformMode
    {
        Append,
        Set
    }
}
