﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Linq.Expressions;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Presentation.Extensions
{
    public static class ClassFieldExtensions
    {
        [NotNull]
        public static Func<TInstance, TValue> GetFieldAccessor<TInstance, TValue>([NotNull] string fieldName)
        {
            var instanceParam = Expression.Parameter(typeof(TInstance), "instance");
            var member = Expression.Field(instanceParam, fieldName);
            var lambda = Expression.Lambda(typeof(Func<TInstance, TValue>), member, instanceParam);

            return (Func<TInstance, TValue>)lambda.Compile();
        }

        [NotNull]
        public static Func<object, object> GetFieldAccessor([NotNull] string fieldName, [NotNull] Type instanceType, Type valueType)
        {
            var instanceParam = Expression.Parameter(instanceType, "instance");
            var member = Expression.Field(instanceParam, fieldName);
            var lambda = Expression.Lambda(typeof(Func<object, object>), member, instanceParam);

            return (Func<object, object>)lambda.Compile();
        }

        [NotNull]
        public static Action<TInstance, TValue> SetFieldAccessor<TInstance, TValue>([NotNull] string fieldName)
        {
            var instanceParam = Expression.Parameter(typeof(TInstance), "instance");
            var valueParam = Expression.Parameter(typeof(TValue), "value");
            var member = Expression.Field(instanceParam, fieldName);
            var assign = Expression.Assign(member, valueParam);
            var lambda = Expression.Lambda(typeof(Action<TInstance, TValue>), assign, instanceParam, valueParam);

            return (Action<TInstance, TValue>)lambda.Compile();
        }

        [NotNull]
        public static Action<object, object> SetFieldAccessor([NotNull] string fieldName, [NotNull] Type instanceType, [NotNull] Type valueType)
        {
            var instanceParam = Expression.Parameter(instanceType, "instance");
            var valueParam = Expression.Parameter(valueType, "value");
            var member = Expression.Field(instanceParam, fieldName);
            var assign = Expression.Assign(member, valueParam);
            var lambda = Expression.Lambda(typeof(Action<object, object>), assign, instanceParam, valueParam);

            return (Action<object, object>)lambda.Compile();
        }
    }
}
