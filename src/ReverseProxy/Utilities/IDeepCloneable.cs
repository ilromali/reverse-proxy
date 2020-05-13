﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ReverseProxy
{
    /// <summary>
    /// Interface for a class that can be deep-cloned.
    /// </summary>
    /// <typeparam name="T">Type of the object that can be deep-cloned.</typeparam>
    internal interface IDeepCloneable<T>
    {
        /// <summary>
        /// Creates and returns a deep copy of the current object.
        /// </summary>
        T DeepClone();
    }
}
