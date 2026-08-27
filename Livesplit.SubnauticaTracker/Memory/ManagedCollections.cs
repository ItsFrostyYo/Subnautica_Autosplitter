using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.SubnauticaTracker.Memory
{
    internal sealed class ManagedDictionaryEntry
    {
        public int IntKey { get; set; }
        public string StringKey { get; set; }
        public IntPtr Value { get; set; }
    }

    internal sealed class ManagedCollections
    {
        private const int MaximumItems = 8192;

        private readonly ProcessMemory memory;
        private readonly MonoRuntime runtime;

        public ManagedCollections(ProcessMemory memory, MonoRuntime runtime)
        {
            this.memory = memory;
            this.runtime = runtime;
        }

        public string LastError { get; private set; } = string.Empty;

        public bool TryReadIntSet(IntPtr setObject, out HashSet<int> values)
        {
            values = new HashSet<int>();
            if (setObject == IntPtr.Zero)
                return false;

            IntPtr setClass;
            if (!runtime.TryGetObjectClass(setObject, out setClass))
                return false;

            ManagedField countField;
            int count = -1;
            if (runtime.TryFindFieldAny(setClass, new[] { "_count", "count", "touched", "touchedSlots" }, out countField))
                memory.TryReadInt32(ProcessMemory.Add(setObject, countField.Offset), out count);

            if (count == 0)
                return true;

            ManagedField slotsField;
            IntPtr slotsArray = IntPtr.Zero;
            if (runtime.TryFindFieldAny(setClass, new[] { "_slots", "slots" }, out slotsField))
                memory.TryReadPointer(ProcessMemory.Add(setObject, slotsField.Offset), out slotsArray);

            // Mono's older HashSet stores T[] directly. The field metadata is not
            // always inflated, so retain the stable legacy offsets as a fallback.
            if (slotsArray == IntPtr.Zero && runtime.Flavor == MonoRuntimeFlavor.Legacy)
                memory.TryReadPointer(ProcessMemory.Add(setObject, 0x20), out slotsArray);

            if (slotsArray == IntPtr.Zero)
                return false;

            int length;
            if (!TryGetArrayLength(slotsArray, out length))
                return false;

            if (runtime.Flavor == MonoRuntimeFlavor.Legacy)
            {
                int directCount = count > 0 ? Math.Min(count, length) : length;
                HashSet<int> direct = ReadDirectIntArray(slotsArray, directCount);
                if (direct.Count > 0 || count <= 0)
                {
                    values = direct;
                    return true;
                }
            }

            HashSet<int> best = null;
            int bestScore = -1;
            foreach (int stride in new[] { 12, 16, 20, 24 })
            {
                var candidate = new HashSet<int>();
                int occupied = 0;
                int upper = Math.Min(length, MaximumItems);

                for (int i = 0; i < upper; i++)
                {
                    IntPtr entry = ProcessMemory.Add(slotsArray, ArrayDataOffset + (long)i * stride);
                    int hashCode;
                    int value;
                    if (!memory.TryReadInt32(entry, out hashCode)
                        || !memory.TryReadInt32(ProcessMemory.Add(entry, 8), out value))
                    {
                        break;
                    }

                    if (hashCode < 0 || value <= 0 || value > 200000)
                        continue;

                    candidate.Add(value);
                    occupied++;
                }

                int target = count > 0 ? Math.Min(count, length) : occupied;
                int score = occupied - Math.Abs(target - occupied) * 2;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null)
                return false;

            values = best;
            return true;
        }

        public bool TryReadStringSet(IntPtr setObject, out HashSet<string> values)
        {
            values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (setObject == IntPtr.Zero)
                return false;

            IntPtr setClass;
            if (!runtime.TryGetObjectClass(setObject, out setClass))
                return false;

            ManagedField countField;
            int count = -1;
            if (runtime.TryFindFieldAny(
                setClass,
                new[] { "_count", "count", "touched", "touchedSlots" },
                out countField))
            {
                memory.TryReadInt32(ProcessMemory.Add(setObject, countField.Offset), out count);
            }

            if (count == 0)
                return true;
            if (count < -1 || count > MaximumItems)
                return false;

            ManagedField slotsField;
            IntPtr slotsArray = IntPtr.Zero;
            if (runtime.TryFindFieldAny(setClass, new[] { "_slots", "slots" }, out slotsField))
                memory.TryReadPointer(ProcessMemory.Add(setObject, slotsField.Offset), out slotsArray);

            if (slotsArray == IntPtr.Zero && runtime.Flavor == MonoRuntimeFlavor.Legacy)
                memory.TryReadPointer(ProcessMemory.Add(setObject, 0x20), out slotsArray);

            if (slotsArray == IntPtr.Zero)
                return false;

            int length;
            if (!TryGetArrayLength(slotsArray, out length))
                return false;

            // The 2018 Mono HashSet stores T[] directly. Later runtimes use
            // Slot<T>[] (hash, next, value), whose pointer padding varies.
            if (runtime.Flavor == MonoRuntimeFlavor.Legacy)
            {
                var direct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int upper = count > 0 ? Math.Min(count, length) : length;
                upper = Math.Min(upper, MaximumItems);
                for (int i = 0; i < upper; i++)
                {
                    IntPtr stringObject;
                    if (!memory.TryReadPointer(
                        ProcessMemory.Add(slotsArray, ArrayDataOffset + (long)i * memory.PointerSize),
                        out stringObject))
                    {
                        return false;
                    }

                    string text = memory.ReadMonoString(stringObject, 512);
                    if (!string.IsNullOrWhiteSpace(text))
                        direct.Add(text);
                }

                if (direct.Count > 0 || count <= 0)
                {
                    values = direct;
                    return true;
                }
            }

            HashSet<string> best = null;
            int bestScore = int.MinValue;
            int entries = Math.Min(length, MaximumItems);
            foreach (int stride in new[] { 16, 20, 24, 28, 32, 40, 48 })
            {
                foreach (int valueOffset in new[] { 8, 12, 16, 20, 24 })
                {
                    var candidate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < entries; i++)
                    {
                        IntPtr slot = ProcessMemory.Add(slotsArray, ArrayDataOffset + (long)i * stride);
                        int hashCode;
                        IntPtr stringObject;
                        if (!memory.TryReadInt32(slot, out hashCode))
                            break;
                        if (hashCode < 0
                            || !memory.TryReadPointer(ProcessMemory.Add(slot, valueOffset), out stringObject))
                        {
                            continue;
                        }

                        string text = memory.ReadMonoString(stringObject, 512);
                        if (!string.IsNullOrWhiteSpace(text))
                            candidate.Add(text);
                    }

                    int target = count > 0 ? count : candidate.Count;
                    int score = candidate.Count - Math.Abs(target - candidate.Count) * 4;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            if (best == null || (count > 0 && best.Count == 0))
                return false;

            values = best;
            return true;
        }

        public bool TryReadObjectArray(IntPtr arrayObject, out IList<IntPtr> values)
        {
            values = new List<IntPtr>();
            int length;
            if (arrayObject == IntPtr.Zero || !TryGetArrayLength(arrayObject, out length))
                return false;

            var result = new List<IntPtr>(length);
            for (int i = 0; i < length; i++)
            {
                IntPtr value;
                if (!memory.TryReadPointer(
                    ProcessMemory.Add(arrayObject, ArrayDataOffset + (long)i * memory.PointerSize),
                    out value))
                {
                    return false;
                }
                result.Add(value);
            }

            values = result;
            return true;
        }

        public bool TryReadIntArray(IntPtr arrayObject, out IList<int> values)
        {
            values = new List<int>();
            int length;
            if (arrayObject == IntPtr.Zero || !TryGetArrayLength(arrayObject, out length))
                return false;

            var result = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                int value;
                if (!memory.TryReadInt32(
                    ProcessMemory.Add(arrayObject, ArrayDataOffset + i * 4L),
                    out value))
                {
                    return false;
                }
                result.Add(value);
            }

            values = result;
            return true;
        }

        public bool TryReadObjectList(IntPtr listObject, out IList<IntPtr> values)
        {
            values = new List<IntPtr>();
            if (listObject == IntPtr.Zero)
                return false;

            IntPtr listClass;
            ManagedField itemsField;
            ManagedField sizeField;
            if (!runtime.TryGetObjectClass(listObject, out listClass)
                || !runtime.TryFindFieldAny(listClass, new[] { "_items", "items" }, out itemsField)
                || !runtime.TryFindFieldAny(listClass, new[] { "_size", "size", "_count", "count" }, out sizeField))
            {
                return false;
            }

            int size;
            if (!memory.TryReadInt32(ProcessMemory.Add(listObject, sizeField.Offset), out size)
                || size < 0
                || size > MaximumItems)
            {
                return false;
            }

            if (size == 0)
                return true;

            IntPtr itemsArray;
            if (!memory.TryReadPointer(ProcessMemory.Add(listObject, itemsField.Offset), out itemsArray)
                || itemsArray == IntPtr.Zero)
            {
                return false;
            }

            var result = new List<IntPtr>(size);
            for (int i = 0; i < size; i++)
            {
                IntPtr value;
                if (!memory.TryReadPointer(
                    ProcessMemory.Add(itemsArray, ArrayDataOffset + (long)i * memory.PointerSize),
                    out value))
                {
                    return false;
                }
                result.Add(value);
            }

            values = result;
            return true;
        }

        public bool TryReadIntList(IntPtr listObject, out IList<int> values)
        {
            values = new List<int>();
            if (listObject == IntPtr.Zero)
                return false;

            IntPtr listClass;
            ManagedField itemsField;
            ManagedField sizeField;
            if (!runtime.TryGetObjectClass(listObject, out listClass)
                || !runtime.TryFindFieldAny(listClass, new[] { "_items", "items" }, out itemsField)
                || !runtime.TryFindFieldAny(listClass, new[] { "_size", "size", "_count", "count" }, out sizeField))
            {
                return false;
            }

            int size;
            if (!memory.TryReadInt32(ProcessMemory.Add(listObject, sizeField.Offset), out size)
                || size < 0
                || size > MaximumItems)
            {
                return false;
            }

            if (size == 0)
                return true;

            IntPtr itemsArray;
            if (!memory.TryReadPointer(ProcessMemory.Add(listObject, itemsField.Offset), out itemsArray)
                || itemsArray == IntPtr.Zero)
            {
                return false;
            }

            var result = new List<int>(size);
            for (int i = 0; i < size; i++)
            {
                int value;
                if (!memory.TryReadInt32(
                    ProcessMemory.Add(itemsArray, ArrayDataOffset + i * 4L),
                    out value))
                {
                    return false;
                }
                result.Add(value);
            }

            values = result;
            return true;
        }

        public bool TryReadIntObjectDictionary(
            IntPtr dictionaryObject,
            out IList<ManagedDictionaryEntry> entries)
        {
            return TryReadDictionary(dictionaryObject, false, out entries);
        }

        public bool TryReadStringObjectDictionary(
            IntPtr dictionaryObject,
            out IList<ManagedDictionaryEntry> entries)
        {
            return TryReadDictionary(dictionaryObject, true, out entries);
        }

        private bool TryReadDictionary(
            IntPtr dictionaryObject,
            bool stringKeys,
            out IList<ManagedDictionaryEntry> entries)
        {
            LastError = string.Empty;
            entries = new List<ManagedDictionaryEntry>();
            if (dictionaryObject == IntPtr.Zero)
            {
                LastError = "dictionary object is null";
                return false;
            }

            IntPtr dictionaryClass;
            if (!runtime.TryGetObjectClass(dictionaryObject, out dictionaryClass))
            {
                LastError = "dictionary class could not be resolved at 0x"
                    + dictionaryObject.ToInt64().ToString("X");
                return false;
            }

            ManagedField countField;
            int count = -1;
            if (runtime.TryFindFieldAny(
                dictionaryClass,
                new[] { "_count", "count", "touchedSlots", "touched" },
                out countField))
            {
                memory.TryReadInt32(ProcessMemory.Add(dictionaryObject, countField.Offset), out count);
                if (count == 0)
                    return true;
            }

            ManagedField modernEntriesField;
            if (runtime.TryFindFieldAny(dictionaryClass, new[] { "_entries", "entries" }, out modernEntriesField))
            {
                IntPtr entriesArray;
                if (memory.TryReadPointer(
                    ProcessMemory.Add(dictionaryObject, modernEntriesField.Offset),
                    out entriesArray)
                    && entriesArray != IntPtr.Zero)
                {
                    return TryReadModernDictionary(entriesArray, stringKeys, out entries);
                }
            }

            if (TryReadLegacyDictionary(dictionaryObject, dictionaryClass, stringKeys, count, out entries))
                return true;

            LastError = "no readable modern or legacy dictionary storage (class 0x"
                + dictionaryClass.ToInt64().ToString("X") + ", count " + count + ")";
            return false;
        }

        private bool TryReadModernDictionary(
            IntPtr entriesArray,
            bool stringKeys,
            out IList<ManagedDictionaryEntry> entries)
        {
            entries = new List<ManagedDictionaryEntry>();
            int length;
            if (!TryGetArrayLength(entriesArray, out length))
                return false;

            var result = new List<ManagedDictionaryEntry>();
            int upper = Math.Min(length, MaximumItems);
            const int stride = 24;

            for (int i = 0; i < upper; i++)
            {
                IntPtr entryAddress = ProcessMemory.Add(entriesArray, ArrayDataOffset + (long)i * stride);
                int hashCode;
                if (!memory.TryReadInt32(entryAddress, out hashCode))
                    return false;
                if (hashCode < 0)
                    continue;

                var entry = new ManagedDictionaryEntry();
                if (stringKeys)
                {
                    IntPtr stringObject;
                    if (!memory.TryReadPointer(ProcessMemory.Add(entryAddress, 8), out stringObject)
                        || stringObject == IntPtr.Zero)
                    {
                        continue;
                    }
                    entry.StringKey = memory.ReadMonoString(stringObject, 512);
                    if (string.IsNullOrWhiteSpace(entry.StringKey))
                        continue;
                }
                else
                {
                    if (!memory.TryReadInt32(ProcessMemory.Add(entryAddress, 8), out int key))
                        continue;
                    entry.IntKey = key;
                }

                memory.TryReadPointer(ProcessMemory.Add(entryAddress, 16), out IntPtr value);
                entry.Value = value;
                result.Add(entry);
            }

            entries = result;
            return true;
        }

        private bool TryReadLegacyDictionary(
            IntPtr dictionaryObject,
            IntPtr dictionaryClass,
            bool stringKeys,
            int count,
            out IList<ManagedDictionaryEntry> entries)
        {
            entries = new List<ManagedDictionaryEntry>();
            ManagedField keySlotsField;
            ManagedField valueSlotsField;
            if (!runtime.TryFindFieldAny(dictionaryClass, new[] { "keySlots", "_keySlots" }, out keySlotsField)
                || !runtime.TryFindFieldAny(dictionaryClass, new[] { "valueSlots", "_valueSlots" }, out valueSlotsField))
            {
                return false;
            }

            IntPtr keyArray;
            IntPtr valueArray;
            if (!memory.TryReadPointer(ProcessMemory.Add(dictionaryObject, keySlotsField.Offset), out keyArray)
                || keyArray == IntPtr.Zero
                || !memory.TryReadPointer(ProcessMemory.Add(dictionaryObject, valueSlotsField.Offset), out valueArray)
                || valueArray == IntPtr.Zero)
            {
                return false;
            }

            int length;
            if (!TryGetArrayLength(keyArray, out length))
                return false;

            int upper = count > 0 ? Math.Min(length, count + 32) : length;
            upper = Math.Min(upper, MaximumItems);
            var result = new List<ManagedDictionaryEntry>();

            for (int i = 0; i < upper; i++)
            {
                var entry = new ManagedDictionaryEntry();
                if (stringKeys)
                {
                    IntPtr stringObject;
                    if (!memory.TryReadPointer(
                        ProcessMemory.Add(keyArray, ArrayDataOffset + (long)i * memory.PointerSize),
                        out stringObject)
                        || stringObject == IntPtr.Zero)
                    {
                        continue;
                    }

                    entry.StringKey = memory.ReadMonoString(stringObject, 512);
                    if (string.IsNullOrWhiteSpace(entry.StringKey))
                        continue;
                }
                else
                {
                    if (!memory.TryReadInt32(ProcessMemory.Add(keyArray, ArrayDataOffset + i * 4L), out int key)
                        || key == 0)
                    {
                        continue;
                    }
                    entry.IntKey = key;
                }

                memory.TryReadPointer(
                    ProcessMemory.Add(valueArray, ArrayDataOffset + (long)i * memory.PointerSize),
                    out IntPtr value);
                entry.Value = value;
                result.Add(entry);
            }

            entries = result;
            return true;
        }

        private HashSet<int> ReadDirectIntArray(IntPtr array, int length)
        {
            var values = new HashSet<int>();
            int upper = Math.Min(length, MaximumItems);
            for (int i = 0; i < upper; i++)
            {
                int value;
                if (!memory.TryReadInt32(ProcessMemory.Add(array, ArrayDataOffset + i * 4L), out value))
                    break;
                if (value > 0 && value <= 200000)
                    values.Add(value);
            }
            return values;
        }

        private bool TryGetArrayLength(IntPtr array, out int length)
        {
            return memory.TryReadInt32(ProcessMemory.Add(array, ArrayLengthOffset), out length)
                && length >= 0
                && length <= MaximumItems;
        }

        private int ArrayLengthOffset => memory.PointerSize == 8 ? 0x18 : 0x0C;
        private int ArrayDataOffset => memory.PointerSize == 8 ? 0x20 : 0x10;
    }
}
