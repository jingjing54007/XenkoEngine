﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Core
{
    /// <summary>
    /// Delegate ObjectInvalidatorCallback used by <see cref="ObjectInvalidationMetadata"/>.
    /// </summary>
    /// <param name="propertyOwner">The owner of the property that changed.</param>
    /// <param name="propertyKey">The key of the property that changed.</param>
    /// <param name="propertyOldValue">The value of the property before its modification.</param>
    public delegate void ObjectInvalidationCallback<T>(object propertyOwner, PropertyKey<T> propertyKey, T propertyOldValue);

    public delegate void ObjectInvalidationRefCallback<T>(object propertyOwner, PropertyKey<T> propertyKey, ref T propertyOldValue);

    public abstract class ObjectInvalidationMetadata : PropertyKeyMetadata
    {
        [NotNull]
        public static ObjectInvalidationMetadata New<T>([NotNull] ObjectInvalidationCallback<T> invalidationCallback)
        {
            return new ObjectInvalidationMetadata<T>(invalidationCallback);
        }

        [NotNull]
        public static ObjectInvalidationMetadata NewRef<T>([NotNull] ObjectInvalidationRefCallback<T> invalidationRefCallback)
        {
            return new ObjectInvalidationMetadata<T>(invalidationRefCallback);
        }

        public abstract void Invalidate(object propertyOwner, PropertyKey propertyKey, object propertyOldValue);
    }

    /// <summary>
    /// Metadata used to invalidate an object state after a property value modification.
    /// </summary>
    public class ObjectInvalidationMetadata<T> : ObjectInvalidationMetadata
    {
        private readonly ObjectInvalidationCallback<T> objectInvalidationCallback;
        private readonly ObjectInvalidationRefCallback<T> objectInvalidationRefCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateValueMetadata"/> class.
        /// </summary>
        /// <param name="invalidationCallback">The object invalidation callback.</param>
        /// <exception cref="System.ArgumentNullException">Parameter <paramref name="invalidationCallback"/> is null.</exception>
        public ObjectInvalidationMetadata([NotNull] ObjectInvalidationCallback<T> invalidationCallback)
        {
            if (invalidationCallback == null) throw new ArgumentNullException(nameof(invalidationCallback));
            objectInvalidationCallback = invalidationCallback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateValueMetadata"/> class.
        /// </summary>
        /// <param name="invalidationRefCallback">The object invalidation callback.</param>
        /// <exception cref="System.ArgumentNullException">Parameter <paramref name="invalidationRefCallback"/> is null.</exception>
        public ObjectInvalidationMetadata([NotNull] ObjectInvalidationRefCallback<T> invalidationRefCallback)
        {
            if (invalidationRefCallback == null) throw new ArgumentNullException(nameof(invalidationRefCallback));
            objectInvalidationRefCallback = invalidationRefCallback;
        }

        public void Invalidate(object propertyOwner, PropertyKey<T> propertyKey, ref T propertyOldValue)
        {
            if (objectInvalidationCallback != null)
                objectInvalidationCallback(propertyOwner, propertyKey, propertyOldValue);
            else
                objectInvalidationRefCallback(propertyOwner, propertyKey, ref propertyOldValue);
        }

        public override void Invalidate(object propertyOwner, PropertyKey propertyKey, object propertyOldValue)
        {
            var propertyOldValueT = (T)propertyOldValue;

            if (objectInvalidationCallback != null)
                objectInvalidationCallback(propertyOwner, (PropertyKey<T>)propertyKey, propertyOldValueT);
            else
                objectInvalidationRefCallback(propertyOwner, (PropertyKey<T>)propertyKey, ref propertyOldValueT);
        }
    }
}