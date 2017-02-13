﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.Serialization.Serializers
{
    /// <summary>
    /// Data serializer for Nullable{T}.
    /// </summary>
    /// <typeparam name="T">The generic type in Nullable{T}.</typeparam>
    [DataSerializerGlobal(typeof(NullableSerializer<>), typeof(Nullable<>), DataSerializerGenericMode.GenericArguments)]
    public class NullableSerializer<T> : DataSerializer<T?> where T : struct
    {
        private DataSerializer<T> itemSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemSerializer = serializerSelector.GetSerializer<T>();
        }

        /// <inheritdoc/>
        public override void Serialize(ref T? obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                var hasValue = obj.HasValue;
                stream.Serialize(ref hasValue);
                if (obj.HasValue)
                {
                    var value = obj.Value;
                    itemSerializer.Serialize(ref value, mode, stream);
                }
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                var hasValue = false;
                stream.Serialize(ref hasValue);
                if (hasValue)
                {
                    var value = default(T);
                    itemSerializer.Serialize(ref value, mode, stream);
                    obj = value;
                }
                else
                    obj = null;
            }
        }
    }
}