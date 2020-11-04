// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.ReverseProxy.Service.Management
{
    /// <summary>
    /// Manages the runtime state of items.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    internal interface IItemManager<T>
        where T : class
    {
        /// <summary>
        /// Gets a snapshot representing the tracked items.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets an item identified by its <paramref name="itemId"/>,
        /// or null if it doesn't exist.
        /// Implementations must be thread-safe.
        /// </summary>
        T TryGetItem(string itemId);

        /// <summary>
        /// Gets an item identified by its <paramref name="itemId"/> or creates
        /// one if it doesn't exist. Action <paramref name="setupAction"/> is invoked on the
        /// existing or the newly created instance, ensuring the new instance is fully initialized
        /// before it is made available to subsequent calls of other methods in this class.
        /// Implementations must be thread-safe.
        /// </summary>
        T GetOrCreateItem(string itemId, Action<T> setupAction);

        /// <summary>
        /// Gets all tracked items.
        /// Implementations must be thread-safe.
        /// </summary>
        /// <remarks>
        /// Note that this is not strictly equivalent to <c>this.Items</c> because
        /// <see cref="GetItems"/> will wait for ongoing operations to complete, whereas
        /// <see cref="Items"/> will immediately return the most recent snapshot, without
        /// waiting for any ongoing writes.
        /// </remarks>
        IList<T> GetItems();

        /// <summary>
        /// Removes an item identified by its <paramref name="itemId"/>
        /// if it exists.
        /// Implementations must be thread-safe.
        /// </summary>
        /// <returns>True if the item was removed. False if it did not exist.</returns>
        bool TryRemoveItem(string itemId);
    }
}
