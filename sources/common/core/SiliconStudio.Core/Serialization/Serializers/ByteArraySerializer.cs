﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

namespace SiliconStudio.Core.Serialization.Serializers
{
    /// <summary>
    /// Implements <see cref="DataSerializer{T}"/> for a byte array.
    /// </summary>
    [DataSerializerGlobal(typeof(ByteArraySerializer))]
    public class ByteArraySerializer : DataSerializer<byte[]>
    {
        /// <inheritdoc/>
        public override void PreSerialize(ref byte[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Length);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                var length = stream.ReadInt32();
                obj = new byte[length];
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref byte[] obj, ArchiveMode mode, SerializationStream stream)
        {
            stream.Serialize(obj, 0, obj.Length);
        }
    }
}