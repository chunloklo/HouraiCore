﻿using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HouraiTeahouse {
     
/// <summary>
/// A CRC32 implementation optimized for speed and GC for smmall
/// buffers.
/// 
/// Altered version of the one used by: https://github.com/force-net/Crc32.NET.
///  * Lookup table is statically allocated in native memory. 
///  * Removed extra offset calculation with pointer arithmetic.
///  * No bounds checking.
/// </summary>
public unsafe static class Crc32 {

    static uint* table;

    static Crc32() {
        const uint poly = 0xedb88320;
        const int size = 16 * byte.MaxValue;
        table = (uint*)UnsafeUtility.Malloc(size * sizeof(uint),
                                            UnsafeUtility.AlignOf<uint>(),
                                            Allocator.Persistent);
        for (uint i = 0; i < byte.MaxValue; i++) {
            uint res = i;
            for (var t = 0; t < 16; t++) {
                for (var k = 0; k < 8; k++) {
                    if ((res & 1) == 1) {
                        res = poly ^ (res >> 1);
                    } else {
                        res = res >> 1;
                    }
                }
                table[(t * 256) + i] = res;
            }
        }

        #if UNITY_EDITOR
        // Native allocations persist across domain reloads. Need to deallocate
        // the table on each reload.
        AssemblyReloadEvents.AssemblyReloadCallback handler = null;
        handler = () => {
            if (table != null) {
                UnsafeUtility.Free(table, Allocator.Persistent);
                table = null;
            }
            AssemblyReloadEvents.beforeAssemblyReload -= handler;
        };
        AssemblyReloadEvents.beforeAssemblyReload += handler;
        #endif
    }

    /// <summary>
    /// Computes the CRC32 checksum from a buffer.
    /// </summary>
    /// <param name="buffer">the start of the buffer.</param>
    /// <param name="len">the len of the buffer</param>
    /// <returns>the CRC32 checksum.</returns>
    /// <exception cref="System.ArgumentNullException">thrown if buffer is null.</exception>
    public static uint ComputeChecksum(byte* buffer, long len) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer));
        }
        
        uint crcLocal = uint.MaxValue;
        while (len >= 16) {
            var a = table[(3 * byte.MaxValue) + buffer[12]]
                ^ table[(2 * byte.MaxValue) + buffer[13]]
                ^ table[(1 * byte.MaxValue) + buffer[14]]
                ^ table[(0 * byte.MaxValue) + buffer[15]];

            var b = table[(7 * byte.MaxValue) + buffer[8]]
                ^ table[(6 * byte.MaxValue) + buffer[9]]
                ^ table[(5 * byte.MaxValue) + buffer[10]]
                ^ table[(4 * byte.MaxValue) + buffer[11]];

            var c = table[(11 * byte.MaxValue) + buffer[4]] 
                ^ table[(10 * byte.MaxValue) + buffer[5]] 
                ^ table[(9 * byte.MaxValue) + buffer[6]] 
                ^ table[(8 * byte.MaxValue) + buffer[7]];

            var d = table[(15 * byte.MaxValue) + ((byte)crcLocal ^ *buffer)]
                ^ table[(14 * byte.MaxValue) + ((byte)(crcLocal >> 8) ^ buffer[1])]
                ^ table[(13 * byte.MaxValue) + ((byte)(crcLocal >> 16) ^ buffer[2])]
                ^ table[(12 * byte.MaxValue) + ((crcLocal >> 24) ^ buffer[3])];

            crcLocal = d ^ c ^ b ^ a;
            buffer += 16;
            len -= 16;
        }

        while (--len >= 0) {
            crcLocal = table[(byte)(crcLocal ^ *buffer++)] ^ crcLocal >> 8;
        }

        return crcLocal ^ uint.MaxValue;
    }

    /// <summary>
    /// Computes the CRC32 checksum from a buffer.
    /// </summary>
    /// <param name="buffer">the buffer to compute the checksum for</param>
    /// <returns>the CRC32 checksum</returns>
    public static uint ComputeChecksum(ReadOnlySpan<byte> buffer) {
        fixed (byte* ptr = buffer) {
            return ComputeChecksum(ptr, buffer.Length);
        }
    }

    /// <summary>
    /// Computes the CRC32 checksum from a buffer.
    /// </summary>
    /// <param name="buffer">the buffer.</param>
    /// <param name="len">the len of the buffer, uses the len of the array null.</param>
    /// <returns>the CRC32 checksum</returns>
    /// <exception cref="System.ArgumentNullException">thrown if buffer is null.</exception>
    public static uint ComputeChecksum(byte[] buffer, long? len = null) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (len == null)  len = buffer.Length;
        fixed (byte* bufPtr = buffer) {
            return ComputeChecksum(bufPtr, len.Value);
        }
    }

}

}
