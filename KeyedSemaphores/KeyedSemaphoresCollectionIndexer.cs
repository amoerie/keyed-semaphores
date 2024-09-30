using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KeyedSemaphores
{
    internal static class KeyedSemaphoresCollectionIndexer
    {
        public static IKeyedSemaphoresCollectionIndexer<TKey> Get<TKey>() where TKey : notnull
        {
            if (typeof(TKey) == typeof(string))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)StringKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(int))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)IntKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(uint))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)UIntKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(short))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)ShortKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(ushort))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)UShortKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(long))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)LongKeyedSemaphoresCollectionIndexer.Instance;
            }
            if (typeof(TKey) == typeof(ulong))
            {
                return (IKeyedSemaphoresCollectionIndexer<TKey>)ULongKeyedSemaphoresCollectionIndexer.Instance;
            }
            return DefaultKeyedSemaphoresCollectionIndexer<TKey>.Instance;
        }
    }

    /// <summary>
    /// Indexer that is responsible for mapping a key to one of the semaphores by index
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public interface IKeyedSemaphoresCollectionIndexer<in TKey> where TKey : notnull
    {
        /// <summary>
        /// Maps the provided <paramref name="key"/> to an index ranging from 0 to <paramref name="length"/>
        /// </summary>
        /// <param name="key">The key that should be mapped</param>
        /// <param name="length">The length of the semaphores array</param>
        /// <returns>An unsigned integer ranging from 0 to length (non-inclusive)</returns>
        uint ToIndex(TKey key, int length);
    }

    internal sealed class DefaultKeyedSemaphoresCollectionIndexer<TKey> : IKeyedSemaphoresCollectionIndexer<TKey>
        where TKey : notnull
    {
        internal static readonly DefaultKeyedSemaphoresCollectionIndexer<TKey> Instance =
            new DefaultKeyedSemaphoresCollectionIndexer<TKey>();

        private readonly EqualityComparer<TKey> _comparer = EqualityComparer<TKey>.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(TKey key, int length)
        {
            return (uint)_comparer.GetHashCode(key) % (uint)length;
        }
    }

    internal class IntKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<int>
    {
        internal static readonly IntKeyedSemaphoresCollectionIndexer Instance =
            new IntKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(int key, int length)
        {
            return (uint)key % (uint)length;
        }
    }

    internal class UIntKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<uint>
    {
        internal static readonly UIntKeyedSemaphoresCollectionIndexer Instance =
            new UIntKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(uint key, int length)
        {
            return key % (uint)length;
        }
    }

    internal class ShortKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<short>
    {
        internal static readonly ShortKeyedSemaphoresCollectionIndexer Instance =
            new ShortKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(short key, int length)
        {
            return (uint)key % (uint)length;
        }
    }

    internal class UShortKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<ushort>
    {
        internal static readonly UShortKeyedSemaphoresCollectionIndexer Instance =
            new UShortKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(ushort key, int length)
        {
            return key % (uint)length;
        }
    }

    internal class LongKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<long>
    {
        internal static readonly LongKeyedSemaphoresCollectionIndexer Instance =
            new LongKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(long key, int length)
        {
            var hashCode = unchecked((int)key) ^ (int)(key >> 32);
            return (uint)hashCode % (uint)length;
        }
    }

    internal class ULongKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<ulong>
    {
        internal static readonly ULongKeyedSemaphoresCollectionIndexer Instance =
            new ULongKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(ulong key, int length)
        {
            var hashCode = unchecked((int)key) ^ (int)(key >> 32);
            return (uint)hashCode % (uint)length;
        }
    }

    internal class StringKeyedSemaphoresCollectionIndexer : IKeyedSemaphoresCollectionIndexer<string>
    {
        internal static readonly StringKeyedSemaphoresCollectionIndexer Instance =
            new StringKeyedSemaphoresCollectionIndexer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToIndex(string key, int length)
        {
            var hashCode = key.GetHashCode();
            return (uint) hashCode % (uint)length;
        }
    }
}
