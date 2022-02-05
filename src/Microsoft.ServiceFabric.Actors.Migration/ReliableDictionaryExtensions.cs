// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data.Collections;

    internal static class ReliableDictionaryExtensions
    {
        public static async Task<TValue> GetAsync<TKey, TValue>(
            this IReliableDictionary2<TKey, TValue> dict,
            Data.ITransaction tx,
            TKey key,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            var result = await dict.TryGetValueAsync(tx, key, timeout, cancellationToken);
            if (result.HasValue)
            {
                return result.Value;
            }

            throw new KeyNotFoundException(key.ToString()); // TODO consider throwing SF exception.
        }
    }
}
