using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
/// <summary>
/// The NativeHashMapExtensions.
/// </summary>
/// 
namespace NativeHashMapExtension
{
    public static class NativeHashMapExtensions
    {
        /// <remarks>
        /// Based off work by eizenhorn https://forum.unity.com/threads/nativehashmap-tryreplacevalue.629512/.
        /// </remarks>
        public static unsafe bool TryReplaceValue<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, TValue item)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var imposter = (NativeHashMapImposter<TKey, TValue>) hashMap;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(imposter.Safety);
#endif
            return NativeHashMapImposter<TKey, TValue>.TryReplaceValue(imposter.Buffer, key, item, false);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeHashMapImposter<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        public NativeHashMapDataImposter* Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle Safety;

        [NativeSetClassTypeToNullOnSchedule]
        public DisposeSentinel DisposeSentinel;
#endif

        public Allocator AllocatorLabel;

        public static implicit operator NativeHashMapImposter<TKey, TValue>(NativeHashMap<TKey, TValue> hashMap)
        {
            var ptr = UnsafeUtility.AddressOf(ref hashMap);
            UnsafeUtility.CopyPtrToStructure(ptr, out NativeHashMapImposter<TKey, TValue> imposter);
            return imposter;
        }


        internal static bool TryReplaceValue(NativeHashMapDataImposter* data, TKey key, TValue item, bool isMultiHashMap)
        {
            if(isMultiHashMap)
            {
                return false;
            }

            return TryReplaceFirstValueAtomic(data, key, item, out _);
        }

        private static bool TryReplaceFirstValueAtomic(NativeHashMapDataImposter* data, TKey key,
            TValue item, out NativeMultiHashMapIteratorImposter<TKey> it)
        {
            it.key = key;
            if(data->AllocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*) data->Buckets;
            int bucket = key.GetHashCode() & data->BucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryReplaceNextValueAtomic(data, item, ref it);
        }

        private static bool TryReplaceNextValueAtomic(NativeHashMapDataImposter* data, TValue item, ref NativeMultiHashMapIteratorImposter<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            if(entryIdx < 0 || entryIdx >= data->Capacity)
            {
                return false;
            }

            int* nextPtrs = (int*) data->Next;
            while(!UnsafeUtility.ReadArrayElement<TKey>(data->Keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if(entryIdx < 0 || entryIdx >= data->Capacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Write the value
            UnsafeUtility.WriteArrayElement(data->Keys, entryIdx, it.key);
            UnsafeUtility.WriteArrayElement(data->Values, entryIdx, item);
            return true;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeHashMapDataImposter
    {
        public byte* Values;
        public byte* Keys;
        public byte* Next;
        public byte* Buckets;
        public int Capacity;

        public int BucketCapacityMask; // = bucket capacity - 1

        // Add padding between fields to ensure they are on separate cache-lines
        private fixed byte padding1[60];

        public fixed int FirstFreeTLS[JobsUtility.MaxJobThreadCount * IntsPerCacheLine];
        public int AllocatedIndexLength;

        // 64 is the cache line size on x86, arm usually has 32 - so it is possible to save some memory there
        public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);


    }

    internal struct NativeMultiHashMapIteratorImposter<TKey>
        where TKey : struct
    {
        public TKey key;
        public int NextEntryIndex;
        public int EntryIndex;

        public static unsafe implicit operator NativeMultiHashMapIteratorImposter<TKey>(NativeMultiHashMapIterator<TKey> it)
        {
            var ptr = UnsafeUtility.AddressOf(ref it);
            UnsafeUtility.CopyPtrToStructure(ptr, out NativeMultiHashMapIteratorImposter<TKey> imposter);
            return imposter;
        }
    }
}