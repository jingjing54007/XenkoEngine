﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Core.Serialization
{
    public delegate void SerializeObjectDelegate(SerializationStream stream, ref object obj, ArchiveMode archiveMode);

    public class SerializerContext
    {
        public PropertyContainer Tags;

        public SerializerContext()
        {
            SerializerSelector = SerializerSelector.Default;
            Tags = new PropertyContainer(this);
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        /// <value>
        /// The serializer.
        /// </value>
        public SerializerSelector SerializerSelector { get; set; }

        public T Get<T>([NotNull] PropertyKey<T> key)
        {
            return Tags.Get(key);
        }

        public void Set<T>([NotNull] PropertyKey<T> key, T value)
        {
            Tags.SetObject(key, value);
        }
    }

    /// <summary>
    /// Serializer context. It holds DataSerializer{T} objects and their factories.
    /// </summary>
    public class SerializerSelector
    {
        private readonly object Lock = new object();
        private readonly bool reuseReferences;
        private readonly string[] profiles;
        private Dictionary<Type, DataSerializer> dataSerializersByType = new Dictionary<Type, DataSerializer>();
        private Dictionary<ObjectId, DataSerializer> dataSerializersByTypeId = new Dictionary<ObjectId, DataSerializer>();
        private readonly List<SerializerFactory> serializerFactories = new List<SerializerFactory>();

        /// <summary>
        /// Gets the default instance of Serializer.
        /// </summary>
        /// <value>
        /// The default instance.
        /// </value>
        public static SerializerSelector Default { get; internal set; }

        public static SerializerSelector Asset { get; internal set; }
        public static SerializerSelector AssetWithReuse { get; internal set; }

        public IEnumerable<string> Profiles => profiles;

        public SerializerSelector SelectorOverride;

        private bool invalidated;
        private int dataSerializerFactoryVersion;

        static SerializerSelector()
        {
            // Do a two step initialization to make sure field is set and accessible during construction
            Default = new SerializerSelector(false, true, "Default");
            Default.Initialize();

            Asset = new SerializerSelector(false, true, "Default", "Content");
            Asset.Initialize();

            AssetWithReuse = new SerializerSelector(true, true, "Default", "Content");
            AssetWithReuse.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerSelector"/> class.
        /// </summary>
        /// <param name="reuseReferences">if set to <c>true</c> reuse references (allow cycles in the object graph).</param>
        /// <param name="profiles">The profiles.</param>
        public SerializerSelector(bool reuseReferences, params string[] profiles)
        {
            this.reuseReferences = reuseReferences;
            this.profiles = profiles;
            Initialize();
        }

        public SerializerSelector(params string[] profiles) : this(false, profiles)
        {
        }


        /// <summary>
        /// Checks if this instance supports the specified serialization profile.
        /// </summary>
        /// <param name="profile">Name of the profile</param>
        /// <returns><c>true</c> if this instance supports the specified serialization profile</returns>
        public bool HasProfile([NotNull] string profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            for (var i = 0; i < profiles.Length; i++)
            {
                if (profile == profiles[i])
                {
                    return true;
                }
            }
            return false;
        }

        private SerializerSelector(bool reuseReferences, bool unusedPrivateCtor, params string[] profiles)
        {
            this.reuseReferences = reuseReferences;
            this.profiles = profiles;
        }

        private void Initialize()
        {
            invalidated = true;
            DataSerializerFactory.RegisterSerializerSelector(this);
            UpdateDataSerializers();
        }

        /// <summary>
        /// Gets or sets a value indicating whether serialization reuses references 
        /// (that is, each reference gets assigned an ID and if it is serialized again, same instance will be reused).
        /// </summary>
        /// <value>
        ///   <c>true</c> if serialization reuses references; otherwise, <c>false</c>.
        /// </value>
        public bool ReuseReferences => reuseReferences;

        public List<SerializerFactory> SerializerFactories => serializerFactories;

        [CanBeNull]
        public DataSerializer GetSerializer(ref ObjectId typeId)
        {
            if (invalidated)
                UpdateDataSerializers();

            DataSerializer dataSerializer;
            if (!dataSerializersByTypeId.TryGetValue(typeId, out dataSerializer))
            {
                foreach (var serializerFactory in serializerFactories)
                {
                    dataSerializer = serializerFactory.GetSerializer(this, ref typeId);
                    if (dataSerializer != null)
                        break;
                }
            }

            if (dataSerializer != null && !dataSerializer.Initialized)
                EnsureInitialized(dataSerializer);
            return dataSerializer;
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <param name="type">The type that you want to (de)serialize.</param>
        /// <returns>The <see cref="DataSerializer{T}"/> for this type if it exists or can be created, otherwise null.</returns>
        [CanBeNull]
        public DataSerializer GetSerializer([NotNull] Type type)
        {
            if (invalidated)
                UpdateDataSerializers();

            DataSerializer dataSerializer;
            if (!dataSerializersByType.TryGetValue(type, out dataSerializer))
            {
                foreach (var serializerFactory in serializerFactories)
                {
                    dataSerializer = serializerFactory.GetSerializer(this, type);
                    if (dataSerializer != null)
                        break;
                }
            }

            if (dataSerializer != null && !dataSerializer.Initialized)
                EnsureInitialized(dataSerializer);
            return dataSerializer;
        }

        /// <summary>
        /// Internal function, for use by <see cref="SerializerFactory"/>.
        /// </summary>
        /// <param name="dataSerializer"></param>
        public void EnsureInitialized([NotNull] DataSerializer dataSerializer)
        {
            // Allow reentrency (in case a serializer needs itself)
            if (dataSerializer.InitializeLock.IsHeldByCurrentThread)
                return;

            var gotLock = false;
            try
            {
                dataSerializer.InitializeLock.Enter(ref gotLock);

                // Ensure a serialization type ID has been generated (otherwise do so now)
                EnsureSerializationTypeId(dataSerializer);

                if (!dataSerializer.Initialized)
                {
                    // Initialize (if necessary)
                    dataSerializer.Initialize(SelectorOverride ?? this);

                    // Mark it as initialized
                    dataSerializer.Initialized = true;
                }
            }
            finally
            {
                if (gotLock)
                    dataSerializer.InitializeLock.Exit();
            }
        }

        private static void EnsureSerializationTypeId([NotNull] DataSerializer dataSerializer)
        {
            // Ensure a serialization type ID has been generated (otherwise do so now)
            if (dataSerializer.SerializationTypeId == ObjectId.Empty)
            {
                // Need to generate serialization type id
                var typeName = dataSerializer.SerializationType.FullName;
                dataSerializer.SerializationTypeId = ObjectId.FromBytes(System.Text.Encoding.UTF8.GetBytes(typeName));
            }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <typeparam name="T">The type that you want to (de)serialize.</typeparam>
        /// <returns>The <see cref="DataSerializer{T}"/> for this type if it exists or can be created, otherwise null.</returns>
        [CanBeNull]
        public DataSerializer<T> GetSerializer<T>()
        {
            return (DataSerializer<T>)GetSerializer(typeof(T));
        }

        internal void Invalidate()
        {
            invalidated = true;
        }

        private void UpdateDataSerializers()
        {
            if (invalidated)
            {
                var newDataSerializersByType = new Dictionary<Type, DataSerializer>();
                var newDataSerializersByTypeId = new Dictionary<ObjectId, DataSerializer>();

                // Create list of combined serializers
                var combinedSerializers = new Dictionary<Type, AssemblySerializerEntry>();

                int capturedVersion;

                lock (DataSerializerFactory.Lock)
                {
                    foreach (var profile in profiles)
                    {
                        Dictionary<Type, AssemblySerializerEntry> serializersPerProfile;
                        if (DataSerializerFactory.DataSerializersPerProfile.TryGetValue(profile, out serializersPerProfile))
                        {
                            foreach (var serializer in serializersPerProfile)
                            {
                                combinedSerializers[serializer.Key] = serializer.Value;
                            }
                        }
                    }

                    // Due to multithreading, maybe the current version is already that one, or better
                    // In this case, we can stop right there
                    capturedVersion = DataSerializerFactory.Version;
                    if (dataSerializerFactoryVersion >= capturedVersion)
                    {
                        invalidated = false;
                        return;
                    }
                }

                // Create new list of serializers (it will create new ones, and remove unused ones)
                foreach (var serializer in combinedSerializers)
                {
                    DataSerializer dataSerializer;
                    if (!dataSerializersByType.TryGetValue(serializer.Key, out dataSerializer))
                    {
                        if (serializer.Value.SerializerType != null)
                        {
                            // New serializer, let's create it
                            dataSerializer = (DataSerializer)Activator.CreateInstance(serializer.Value.SerializerType);
                            dataSerializer.SerializationTypeId = serializer.Value.Id;

                            // Ensure a serialization type ID has been generated (otherwise do so now)
                            EnsureSerializationTypeId(dataSerializer);
                        }
                    }

                    newDataSerializersByType[serializer.Key] = dataSerializer;
                    newDataSerializersByTypeId[serializer.Value.Id] = dataSerializer;
                }

                // Do the actual state switch inside a lock
                lock (Lock)
                {
                    // Due to multithreading, make sure we really still need to update
                    if (dataSerializerFactoryVersion < capturedVersion)
                    {
                        dataSerializerFactoryVersion = capturedVersion;
                        dataSerializersByType = newDataSerializersByType;
                        dataSerializersByTypeId = newDataSerializersByTypeId;
                    }

                    invalidated = false;
                }
            }
        }
    }
}
