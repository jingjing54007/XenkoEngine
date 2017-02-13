﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml.Events;
using SiliconStudio.Core.Yaml.Serialization;
using SiliconStudio.Core.Yaml.Serialization.Serializers;

namespace SiliconStudio.Core.Yaml
{
    /// <summary>
    /// Internal class used when serializing/deserializing an object.
    /// </summary>
    public class AssetObjectSerializerBackend : DefaultObjectSerializerBackend
    {
        private readonly ITypeDescriptorFactory typeDescriptorFactory;
        private static readonly PropertyKey<YamlAssetPath> MemberPathKey = new PropertyKey<YamlAssetPath>("MemberPath", typeof(AssetObjectSerializerBackend));
        public static readonly PropertyKey<Dictionary<YamlAssetPath, OverrideType>> OverrideDictionaryKey = new PropertyKey<Dictionary<YamlAssetPath, OverrideType>>("OverrideDictionary", typeof(AssetObjectSerializerBackend));

        public AssetObjectSerializerBackend(ITypeDescriptorFactory typeDescriptorFactory)
        {
            if (typeDescriptorFactory == null)
                throw new ArgumentNullException(nameof(typeDescriptorFactory));
            this.typeDescriptorFactory = typeDescriptorFactory;
        }

        public override object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue, Type memberType)
        {
            var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType));

            var member = memberDescriptor as MemberDescriptorBase;
            if (member != null && objectContext.Settings.Attributes.GetAttribute<NonIdentifiableCollectionItemsAttribute>(member.MemberInfo) != null)
            {
                memberObjectContext.Properties.Add(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, true);
            }

            var path = GetCurrentPath(ref objectContext, true);
            path.PushMember(memberDescriptor.Name);
            SetCurrentPath(ref memberObjectContext, path);

            var result = ReadYaml(ref memberObjectContext);
            return result;
        }

        public override void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue, Type memberType)
        {
            var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType))
            {
                ScalarStyle = memberDescriptor.ScalarStyle,
            };

            bool nonIdentifiableItems;
            // We allow compact style only for collection with non-identifiable items
            var allowCompactStyle = objectContext.SerializerContext.Properties.TryGetValue(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, out nonIdentifiableItems) && nonIdentifiableItems;

            var member = memberDescriptor as MemberDescriptorBase;
            if (member != null && objectContext.Settings.Attributes.GetAttribute<NonIdentifiableCollectionItemsAttribute>(member.MemberInfo) != null)
            {
                memberObjectContext.Properties.Add(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, true);
                allowCompactStyle = true;
            }

            if (allowCompactStyle)
            {
                memberObjectContext.Style = memberDescriptor.Style;
            }

            var path = GetCurrentPath(ref objectContext, true);
            path.PushMember(memberDescriptor.Name);
            SetCurrentPath(ref memberObjectContext, path);

            WriteYaml(ref memberObjectContext);
        }

        public override string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
        {
            var objectType = objectContext.Instance.GetType();

            OverrideType[] overrideTypes;
            var realMemberName = TrimAndParseOverride(memberName, out overrideTypes);

            // For member names, we have a single override, so we always take the last one of the array (In case of legacy property serialized with ~Name)
            var overrideType = overrideTypes[overrideTypes.Length - 1];
            if (overrideType != OverrideType.Base)
            {
                Dictionary<YamlAssetPath, OverrideType> overrides;
                if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
                {
                    overrides = new Dictionary<YamlAssetPath, OverrideType>();
                    objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
                }

                var path = GetCurrentPath(ref objectContext, true);
                path.PushMember(realMemberName);
                overrides.Add(path, overrideType);
            }

            var resultMemberName = base.ReadMemberName(ref objectContext, realMemberName, out skipMember);
            // If ~Id was not found as a member, don't generate an error, as we may have switched an object
            // to NonIdentifiable but we don't want to write an upgrader for this
            if (!ShadowId.IsTypeIdentifiable(objectType) && memberName == ShadowId.YamlSpecialId)
            {
                skipMember = true;
            }
            return resultMemberName;
        }

        public override void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string memberName)
        {
            // Replace the key with SiliconStudio.Core.Reflection IMemberDescriptor
            // Cache previous 
            if (member != null)
            {
                var customDescriptor = (IMemberDescriptor)member.Tag;
                if (customDescriptor == null)
                {
                    customDescriptor = typeDescriptorFactory.Find(objectContext.Instance.GetType()).TryGetMember(memberName);
                    member.Tag = customDescriptor;
                }

                if (customDescriptor != null)
                {
                    Dictionary<YamlAssetPath, OverrideType> overrides;
                    if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
                    {
                        var path = GetCurrentPath(ref objectContext, true);
                        path.PushMember(memberName);

                        OverrideType overrideType;
                        if (overrides.TryGetValue(path, out overrideType))
                        {
                            if ((overrideType & OverrideType.New) != 0)
                            {
                                memberName += OverridePostfixes.PostFixNew;
                            }
                            if ((overrideType & OverrideType.Sealed) != 0)
                            {
                                memberName += OverridePostfixes.PostFixSealed;
                            }
                        }
                    }
                }
            }

            base.WriteMemberName(ref objectContext, member, memberName);
        }

        public override object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index)
        {
            var path = GetCurrentPath(ref objectContext, true);
            path.PushIndex(index);
            var itemObjectContext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(itemType));
            SetCurrentPath(ref itemObjectContext, path);
            return ReadYaml(ref itemObjectContext);
        }

        public override void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index)
        {
            var path = GetCurrentPath(ref objectContext, true);
            path.PushIndex(index);
            var itemObjectcontext = new ObjectContext(objectContext.SerializerContext, item, objectContext.SerializerContext.FindTypeDescriptor(itemType));
            SetCurrentPath(ref itemObjectcontext, path);
            WriteYaml(ref itemObjectcontext);
        }

        public override object ReadDictionaryKey(ref ObjectContext objectContext, Type keyType)
        {
            var key = objectContext.Reader.Peek<Scalar>();
            OverrideType[] overrideTypes;
            var keyName = TrimAndParseOverride(key.Value, out overrideTypes);
            key.Value = keyName;

            var keyValue = base.ReadDictionaryKey(ref objectContext, keyType);

            if (overrideTypes[0] != OverrideType.Base)
            {
                Dictionary<YamlAssetPath, OverrideType> overrides;
                if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
                {
                    overrides = new Dictionary<YamlAssetPath, OverrideType>();
                    objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
                }

                var path = GetCurrentPath(ref objectContext, true);
                ItemId id;
                object actualKey;
                if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, keyValue, out id, out actualKey))
                {
                    path.PushItemId(id);
                }
                else
                {
                    path.PushIndex(key);
                }
                overrides.Add(path, overrideTypes[0]);
            }

            if (overrideTypes.Length > 1 && overrideTypes[1] != OverrideType.Base)
            {
                ItemId id;
                object actualKey;
                if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, keyValue, out id, out actualKey))
                {
                    Dictionary<YamlAssetPath, OverrideType> overrides;
                    if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
                    {
                        overrides = new Dictionary<YamlAssetPath, OverrideType>();
                        objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
                    }

                    var path = GetCurrentPath(ref objectContext, true);
                    path.PushIndex(actualKey);
                    overrides.Add(path, overrideTypes[1]);
                }
            }

            return keyValue;
        }

        public override void WriteDictionaryKey(ref ObjectContext objectContext, object key, Type keyType)
        {
            Dictionary<YamlAssetPath, OverrideType> overrides;
            if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
            {
                var itemPath = GetCurrentPath(ref objectContext, true);
                YamlAssetPath keyPath = null;
                ItemId id;
                object actualKey;
                if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out id, out actualKey))
                {
                    keyPath = itemPath.Clone();
                    keyPath.PushIndex(actualKey);
                    itemPath.PushItemId(id);
                }
                else
                {
                    itemPath.PushIndex(key);
                }
                OverrideType overrideType;
                if (overrides.TryGetValue(itemPath, out overrideType))
                {
                    if ((overrideType & OverrideType.New) != 0)
                    {
                        objectContext.SerializerContext.Properties.Set(ItemIdSerializerBase.OverrideInfoKey, OverridePostfixes.PostFixNew.ToString());
                    }
                    if ((overrideType & OverrideType.Sealed) != 0)
                    {
                        objectContext.SerializerContext.Properties.Set(ItemIdSerializerBase.OverrideInfoKey, OverridePostfixes.PostFixSealed.ToString());
                    }
                }
                if (keyPath != null && overrides.TryGetValue(keyPath, out overrideType))
                {
                    if ((overrideType & OverrideType.New) != 0)
                    {
                        objectContext.SerializerContext.Properties.Set(KeyWithIdSerializer.OverrideKeyInfoKey, OverridePostfixes.PostFixNew.ToString());
                    }
                    if ((overrideType & OverrideType.Sealed) != 0)
                    {
                        objectContext.SerializerContext.Properties.Set(KeyWithIdSerializer.OverrideKeyInfoKey, OverridePostfixes.PostFixSealed.ToString());
                    }
                }
            }
            base.WriteDictionaryKey(ref objectContext, key, keyType);
        }

        public override object ReadDictionaryValue(ref ObjectContext objectContext, Type valueType, object key)
        {
            var path = GetCurrentPath(ref objectContext, true);
            ItemId id;
            if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out id))
            {
                path.PushItemId(id);
            }
            else
            {
                path.PushIndex(key);
            }
            var valueObjectContext = new ObjectContext(objectContext.SerializerContext, null, objectContext.SerializerContext.FindTypeDescriptor(valueType));
            SetCurrentPath(ref valueObjectContext, path);
            return ReadYaml(ref valueObjectContext);
        }

        public override void WriteDictionaryValue(ref ObjectContext objectContext, object key, object value, Type valueType)
        {
            var path = GetCurrentPath(ref objectContext, true);
            ItemId id;
            if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out id))
            {
                path.PushItemId(id);
            }
            else
            {
                path.PushIndex(key);
            }
            var itemObjectcontext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(valueType));
            SetCurrentPath(ref itemObjectcontext, path);
            WriteYaml(ref itemObjectcontext);
        }

        public override bool ShouldSerialize(IMemberDescriptor member, ref ObjectContext objectContext)
        {
            Dictionary<YamlAssetPath, OverrideType> overrides;
            if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out overrides))
            {
                var path = GetCurrentPath(ref objectContext, true);
                path.PushMember(member.Name);

                OverrideType overrideType;
                if (overrides.TryGetValue(path, out overrideType))
                {
                    if (overrideType != OverrideType.Base)
                        return true;
                }
            }

            return base.ShouldSerialize(member, ref objectContext);
        }

        private static YamlAssetPath GetCurrentPath(ref ObjectContext objectContext, bool clone)
        {
            YamlAssetPath path;
            path = objectContext.Properties.TryGetValue(MemberPathKey, out path) ? path : new YamlAssetPath();
            if (clone)
            {
                path = path.Clone();
            }
            return path;
        }

        private static void SetCurrentPath(ref ObjectContext objectContext, YamlAssetPath path)
        {
            objectContext.Properties.Set(MemberPathKey, path);
        }

        internal static string TrimAndParseOverride(string name, out OverrideType[] overrideTypes)
        {
            var split = name.Split('~');

            overrideTypes = new OverrideType[split.Length];
            int i = 0;
            var trimmedName = string.Empty;
            foreach (var namePart in split)
            {
                var realName = namePart.Trim(OverridePostfixes.PostFixSealed, OverridePostfixes.PostFixNew);

                var overrideType = OverrideType.Base;
                if (realName.Length != namePart.Length)
                {
                    if (namePart.Contains(OverridePostfixes.PostFixNewSealed) || namePart.EndsWith(OverridePostfixes.PostFixNewSealedAlt))
                    {
                        overrideType = OverrideType.New | OverrideType.Sealed;
                    }
                    else if (namePart.EndsWith(OverridePostfixes.PostFixNew))
                    {
                        overrideType = OverrideType.New;
                    }
                    else if (namePart.EndsWith(OverridePostfixes.PostFixSealed))
                    {
                        overrideType = OverrideType.Sealed;
                    }
                }
                overrideTypes[i] = overrideType;
                if (i > 0)
                    trimmedName += '~';
                trimmedName += realName;
                ++i;
            }
            return trimmedName;
        }
    }
}
