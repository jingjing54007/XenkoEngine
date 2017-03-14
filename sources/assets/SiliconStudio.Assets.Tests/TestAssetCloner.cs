﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Assets.Tests
{
    [DataContract("TestAssetClonerObject")]
    public class TestAssetClonerObject
    {
        public string Name { get; set; }

        public TestAssetClonerObject SubObject { get; set; }

        public TestObjectReference ObjectWithAttachedReference { get; set; }
    }

    [DataContract("TestObjectWithCollection")]
    public class TestObjectWithCollection
    {
        public string Name { get; set; }

        public List<string> Items { get; set; } = new List<string>();
    }

    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<TestObjectReference>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<TestObjectReference>), Profile = "Asset")]
    public class TestObjectReference
    {        
    }

    [TestFixture]
    public class TestAssetCloner
    {
        [Test]
        public void TestHash()
        {
            var obj1 = new TestAssetClonerObject
            {
                Name = "Test1",
                SubObject = new TestAssetClonerObject() { Name = "Test2" },
                ObjectWithAttachedReference = new TestObjectReference()
            };

            // Create a fake reference to make sure that the attached reference will not be serialized
            var attachedReference = AttachedReferenceManager.GetOrCreateAttachedReference(obj1.ObjectWithAttachedReference);
            attachedReference.Url = "just_for_test";
            attachedReference.Id = AssetId.New();

            var obj2 = AssetCloner.Clone(obj1);

            var hash1 = AssetHash.Compute(obj1);
            var hash2 = AssetHash.Compute(obj2);
            Assert.AreEqual(hash1, hash2);

            obj1.Name = "Yes";
            var hash11 = AssetHash.Compute(obj1);
            Assert.AreNotEqual(hash11, hash2);
            obj1.Name = "Test1";

            var hash12 = AssetHash.Compute(obj1);
            Assert.AreEqual(hash12, hash2);

            obj2 = AssetCloner.Clone(obj1);

            var hash1WithOverrides = AssetHash.Compute(obj1);
            var hash2WithOverrides = AssetHash.Compute(obj2);
            Assert.AreEqual(hash1WithOverrides, hash2WithOverrides);
        }

        [Test]
        public void TestCloneCollectionIds()
        {
            var obj = new TestObjectWithCollection { Name = "Test", Items = { "aaa", "bbb" } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Items);
            ids.Add(0, new ItemId());
            ids.Add(1, new ItemId());
            ids.MarkAsDeleted(new ItemId());
            var clone = AssetCloner.Clone(obj);
            CollectionItemIdentifiers cloneIds;
            var idsExist = CollectionItemIdHelper.TryGetCollectionItemIds(clone.Items, out cloneIds);
            Assert.True(idsExist);
            Assert.AreEqual(ids.KeyCount, cloneIds.KeyCount);
            Assert.AreEqual(ids.DeletedCount, cloneIds.DeletedCount);
            Assert.AreEqual(ids[0], cloneIds[0]);
            Assert.AreEqual(ids[1], cloneIds[1]);
            Assert.AreEqual(ids.DeletedItems.Single(), cloneIds.DeletedItems.Single());

            clone = AssetCloner.Clone(obj, AssetClonerFlags.RemoveItemIds);
            idsExist = CollectionItemIdHelper.TryGetCollectionItemIds(clone.Items, out cloneIds);
            Assert.False(idsExist);
        }

        [Test]
        public void TestDiscardCollectionIds()
        {
            var obj = new TestObjectWithCollection { Name = "Test", Items = { "aaa", "bbb" } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Items);
            ids.Add(0, new ItemId());
            ids.Add(1, new ItemId());
            ids.MarkAsDeleted(new ItemId());
            var clone = AssetCloner.Clone(obj);
            CollectionItemIdentifiers cloneIds;
            var idsExist = CollectionItemIdHelper.TryGetCollectionItemIds(clone.Items, out cloneIds);
            Assert.True(idsExist);
            Assert.AreEqual(ids.KeyCount, cloneIds.KeyCount);
            Assert.AreEqual(ids.DeletedCount, cloneIds.DeletedCount);
            Assert.AreEqual(ids[0], cloneIds[0]);
            Assert.AreEqual(ids[1], cloneIds[1]);
            Assert.AreEqual(ids.DeletedItems.Single(), cloneIds.DeletedItems.Single());

            clone = AssetCloner.Clone(obj, AssetClonerFlags.RemoveItemIds);
            idsExist = CollectionItemIdHelper.TryGetCollectionItemIds(clone.Items, out cloneIds);
            Assert.False(idsExist);
        }
    }
}
