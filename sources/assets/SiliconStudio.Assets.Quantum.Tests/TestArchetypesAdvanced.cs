﻿using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Assets.Quantum.Tests
{
    [TestFixture]
    public class TestArchetypesAdvanced
    {
        [Test]
        public void TestSimpleDictionaryAddWithCollision()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset3>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));

            // Update a key to derived and then the same key to the base
            derivedPropertyNode.Add("String3", new Index("Key3"));
            basePropertyNode.Add("String4", new Index("Key3"));

            Assert.AreEqual(3, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index("Key3")));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String3", derivedPropertyNode.Retrieve(new Index("Key3")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key3")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index("Key3")));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(3, derivedIds.KeyCount);
            Assert.AreEqual(1, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
            Assert.AreNotEqual(baseIds["Key3"], derivedIds["Key3"]);
            Assert.AreEqual(baseIds["Key3"], derivedIds.DeletedItems.Single());
            Assert.AreEqual(3, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyDictionary.Count);
        }

        [Test]
        public void TestSimpleCollectionRemoveDeleted()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            // Delete an item from the derived and then delete the same from the base
            var derivedDeletedId = derivedIds[2];
            var baseDeletedId = baseIds[2];
            derivedPropertyNode.Remove("String3", new Index(2));
            basePropertyNode.Remove("String3", new Index(2));
            Assert.AreEqual(3, context.BaseAsset.MyStrings.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyStrings.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(3, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
            Assert.AreEqual(baseIds[2], derivedIds[2]);
            Assert.False(derivedIds.IsDeleted(derivedDeletedId));
            Assert.False(baseIds.IsDeleted(baseDeletedId));
        }

        [Test]
        public void TestSimpleDictionaryRemoveDeleted()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" }, { "Key3", "String3" }, { "Key4", "String4" } } };
            var context = DeriveAssetTest<Types.MyAsset3>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));

            // Delete an item from the derived and then delete the same from the base
            var derivedDeletedId = derivedIds["Key3"];
            derivedPropertyNode.Remove("String3", new Index("Key3"));
            var baseDeletedId = baseIds["Key3"];
            basePropertyNode.Remove("String3", new Index("Key3"));
            Assert.AreEqual(3, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(3, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
            Assert.AreEqual(baseIds["Key4"], derivedIds["Key4"]);
            Assert.False(derivedIds.IsDeleted(derivedDeletedId));
            Assert.False(baseIds.IsDeleted(baseDeletedId));
        }

        [Test]
        public void TestSimpleCollectionUpdateDeleted()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            // Delete an item from the derived and then update the same from the base
            var derivedDeletedId = derivedIds[2];
            derivedPropertyNode.Remove("String3", new Index(2));
            basePropertyNode.Update("String3.5", new Index(2));
            Assert.AreEqual(4, context.BaseAsset.MyStrings.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyStrings.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String3.5", basePropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index(3)));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(3)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(4, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(3, derivedIds.KeyCount);
            Assert.AreEqual(1, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
            Assert.AreEqual(baseIds[3], derivedIds[2]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        [Test]
        public void TestSimpleDictionaryUpdateDeleted()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" }, { "Key3", "String3" }, { "Key4", "String4" } } };
            var context = DeriveAssetTest<Types.MyAsset3>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));

            // Delete an item from the derived and then update the same from the base
            var derivedDeletedId = derivedIds["Key3"];
            derivedPropertyNode.Remove("String3", new Index("Key3"));
            basePropertyNode.Update("String3.5", new Index("Key3"));
            Assert.AreEqual(4, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(3, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String3.5", basePropertyNode.Retrieve(new Index("Key3")));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key3")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreEqual(4, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(3, derivedIds.KeyCount);
            Assert.AreEqual(1, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
            Assert.AreEqual(baseIds["Key4"], derivedIds["Key4"]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        [Test]
        public void TestSimpleCollectionAddMultipleAndCheckOrder()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = (IAssetNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (IAssetNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            ((ContentNode)derivedPropertyNode).Add("String3.5", new Index(3));
            ((ContentNode)derivedPropertyNode).Add("String1.5", new Index(1));
            Assert.AreEqual(6, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String1", "String1.5", "String2", "String3", "String3.5", "String4");

            ((ContentNode)basePropertyNode).Add("String0.1", new Index(0));
            Assert.AreEqual(5, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String2", "String3", "String4");
            Assert.AreEqual(7, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.5", "String2", "String3", "String3.5", "String4");

            ((ContentNode)basePropertyNode).Add("String1.1", new Index(2));
            Assert.AreEqual(6, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String3", "String4");
            Assert.AreEqual(8, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String3", "String3.5", "String4");

            ((ContentNode)basePropertyNode).Add("String2.1", new Index(4));
            Assert.AreEqual(7, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String4");
            Assert.AreEqual(9, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.5", "String4");

            ((ContentNode)basePropertyNode).Add("String3.1", new Index(6));
            Assert.AreEqual(8, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String3.1", "String4");
            Assert.AreEqual(10, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.1", "String3.5", "String4");

            ((ContentNode)basePropertyNode).Add("String4.1", new Index(8));
            Assert.AreEqual(9, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String3.1", "String4", "String4.1");
            Assert.AreEqual(11, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.1", "String3.5", "String4", "String4.1");

            Assert.AreEqual(9, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(11, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
            Assert.AreEqual(baseIds[2], derivedIds[2]);
            Assert.AreEqual(baseIds[3], derivedIds[4]);
            Assert.AreEqual(baseIds[4], derivedIds[5]);
            Assert.AreEqual(baseIds[5], derivedIds[6]);
            Assert.AreEqual(baseIds[6], derivedIds[7]);
            Assert.AreEqual(baseIds[7], derivedIds[9]);
            Assert.AreEqual(baseIds[8], derivedIds[10]);
        }

        private static void AssertCollection(IContentNode content, params string[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                Assert.AreEqual(item, content.Retrieve(new Index(i)));
            }
        }
    }
}
