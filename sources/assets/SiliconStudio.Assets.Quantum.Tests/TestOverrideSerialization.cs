﻿using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Assets.Quantum.Tests
{
    public static class IdentifierGenerator
    {
        public static ItemId Get(int index)
        {
            var bytes = ToBytes(index);
            return new ItemId(bytes);
        }

        public static bool Match(ItemId guid, int index)
        {
            var bytes = ToBytes(index);
            var id = new ItemId(bytes);
            return guid == id;
        }

        private static byte[] ToBytes(int index)
        {
            var bytes = new byte[16];
            for (int i = 0; i < 4; ++i)
            {
                bytes[4 * i] = (byte)(index);
                bytes[4 * i + 1] = (byte)(index >> 8);
                bytes[4 * i + 2] = (byte)(index >> 16);
                bytes[4 * i + 3] = (byte)(index >> 24);
            }
            return bytes;
        }
    }

    public static class GuidGenerator
    {
        public static Guid Get(int index)
        {
            var bytes = ToBytes(index);
            return new Guid(bytes);
        }

        public static bool Match(Guid guid, int index)
        {
            var bytes = ToBytes(index);
            var id = new Guid(bytes);
            return guid == id;
        }

        private static byte[] ToBytes(int index)
        {
            var bytes = new byte[16];
            for (int i = 0; i < 4; ++i)
            {
                bytes[4 * i] = (byte)(index);
                bytes[4 * i + 1] = (byte)(index >> 8);
                bytes[4 * i + 2] = (byte)(index >> 16);
                bytes[4 * i + 3] = (byte)(index >> 24);
            }
            return bytes;
        }
    }

    [TestFixture]
    public class TestOverrideSerialization
    {
        /* test TODO:
         * Non-abstract class (test result recursively) : simple prop + in collection
         * Abstract (interface) override with different type
         * class prop set to null
         */
        private static readonly AssetId BaseId = (AssetId)GuidGenerator.Get(1);
        private static readonly AssetId DerivedId = (AssetId)GuidGenerator.Get(2);

        private static void SerializeAndCompare(AssetItem assetItem, AssetPropertyGraph graph, string expectedYaml, bool isDerived)
        {
            assetItem.Asset.Id = isDerived ? DerivedId : BaseId;
            Assert.AreEqual(isDerived, assetItem.Asset.Archetype != null);
            if (isDerived)
                assetItem.Asset.Archetype = new AssetReference(BaseId, assetItem.Asset.Archetype?.Location);
            graph.PrepareForSave(null, assetItem);
            var stream = new MemoryStream();
            AssetFileSerializer.Save(stream, assetItem.Asset, null, (Dictionary<YamlAssetPath, OverrideType>)assetItem.Overrides);
            stream.Position = 0;
            var streamReader = new StreamReader(stream);
            var yaml = streamReader.ReadToEnd();
            Assert.AreEqual(expectedYaml, yaml);
        }

        private static void SerializeAndCompare(object instance, Dictionary<YamlAssetPath, OverrideType> overrides, string expectedYaml)
        {
            var stream = new MemoryStream();
            AssetFileSerializer.Default.Save(stream, instance, null, overrides);
            stream.Position = 0;
            var streamReader = new StreamReader(stream);
            var yaml = streamReader.ReadToEnd();
            Assert.AreEqual(expectedYaml, yaml);
        }

        private const string SimplePropertyUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset1,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyString: MyBaseString
";
        private const string SimplePropertyUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset1,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyString*: MyDerivedString
";
        private const string SimpleCollectionUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: MyBaseString
";
        private const string SimpleCollectionUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000*: MyDerivedString
    14000000140000001400000014000000: MyBaseString
";
        private const string SimpleDictionaryUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset3,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: MyBaseString
";
        private const string SimpleDictionaryUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset3,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyDictionary:
    0a0000000a0000000a0000000a000000*~Key1: MyDerivedString
    14000000140000001400000014000000~Key2: MyBaseString
";
        private const string CollectionInStructBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings:
        0a0000000a0000000a0000000a000000: String1
        14000000140000001400000014000000: MyBaseString
MyStrings: {}
";
        private const string CollectionInStructDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings:
        0a0000000a0000000a0000000a000000*: MyDerivedString
        14000000140000001400000014000000: MyBaseString
MyStrings: {}
";
        private const string SimpleCollectionAddBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
    {0}: String4
";
        private const string SimpleCollectionAddDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset2,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
    {0}: String4
    {1}*: String3
";
        private const string SimpleDictionaryAddBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset3,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
    {0}~Key4: String4
";
        private const string SimpleDictionaryAddDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset3,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
    {1}*~Key3: String3
    {0}~Key4: String4
";
        private const string ObjectCollectionUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000*:
        Value: MyDerivedString
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionAddBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: String2
    {0}:
        Value: String4
";
        private const string ObjectCollectionAddDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: String2
    {0}:
        Value: String4
    {1}*:
        Value: String3
";
        private const string ObjectCollectionPropertyUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionPropertyUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset4,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value*: MyDerivedString
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string NonIdentifiableObjectCollectionPropertyUpdateBaseYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset8,SiliconStudio.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    -   Value: String1
    -   Value: MyBaseString
";
        private const string NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+MyAsset8,SiliconStudio.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    -   Value*: MyDerivedString
    -   Value: MyBaseString
";

        [Test]
        public void TestSimplePropertySerialization()
        {
            var asset = new Types.MyAsset1 { MyString = "String" };
            var context = DeriveAssetTest<Types.MyAsset1>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));

            basePropertyNode.Update("MyBaseString");
            derivedPropertyNode.Update("MyDerivedString");
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimplePropertyUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimplePropertyUpdateDerivedYaml, true);

            context = DeriveAssetTest<Types.MyAsset1>.LoadFromYaml(SimplePropertyUpdateBaseYaml, SimplePropertyUpdateDerivedYaml);
            basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));
            derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));

            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve());
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetContentOverride());
        }

        [Test]
        public void TestSimplePropertyDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset1>.LoadFromYaml(SimplePropertyUpdateBaseYaml, SimplePropertyUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset1.MyString));

            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve());
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetContentOverride());
        }

        [Test]
        public void TestSimpleCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            ids.Add(0, IdentifierGenerator.Get(10));
            ids.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            basePropertyNode.Update("MyBaseString", new Index(1));
            derivedPropertyNode.Update("MyDerivedString", new Index(0));
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimpleCollectionUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimpleCollectionUpdateDerivedYaml, true);
        }

        [Test]
        public void TestSimpleCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset2>.LoadFromYaml(SimpleCollectionUpdateBaseYaml, SimpleCollectionUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);

            Assert.AreEqual(2, context.BaseAsset.MyStrings.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyStrings.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("MyBaseString", derivedPropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
        }

        [Test]
        public void TestSimpleDictionaryUpdateSerialization()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            ids.Add("Key1", IdentifierGenerator.Get(10));
            ids.Add("Key2", IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset3>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));

            basePropertyNode.Update("MyBaseString", new Index("Key2"));
            derivedPropertyNode.Update("MyDerivedString", new Index("Key1"));
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimpleDictionaryUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimpleDictionaryUpdateDerivedYaml, true);

            context = DeriveAssetTest<Types.MyAsset3>.LoadFromYaml(SimpleDictionaryUpdateBaseYaml, SimpleDictionaryUpdateDerivedYaml);
            basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.AreEqual(2, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("MyBaseString", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
        }

        [Test]
        public void TestSimpleDictionaryDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset3>.LoadFromYaml(SimpleDictionaryUpdateBaseYaml, SimpleDictionaryUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.AreEqual(2, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("MyBaseString", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
        }

        [Test]
        public void TestCollectionInStructUpdateSerialization()
        {
            var asset = new Types.MyAsset2();
            asset.Struct.MyStrings.Add("String1");
            asset.Struct.MyStrings.Add("String2");
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.Struct.MyStrings);
            ids.Add(0, IdentifierGenerator.Get(10));
            ids.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.Struct)).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.Struct)).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            basePropertyNode.Update("MyBaseString", new Index(1));
            derivedPropertyNode.Update("MyDerivedString", new Index(0));
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, CollectionInStructBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, CollectionInStructDerivedYaml, true);
        }

        [Test]
        public void TestCollectionInStructUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset2>.LoadFromYaml(CollectionInStructBaseYaml, CollectionInStructDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.Struct)).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.Struct)).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.Struct.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.Struct.MyStrings);

            Assert.AreEqual(2, context.BaseAsset.Struct.MyStrings.Count);
            Assert.AreEqual(2, context.DerivedAsset.Struct.MyStrings.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("MyBaseString", basePropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("MyDerivedString", derivedPropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("MyBaseString", derivedPropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
        }

        [Test]
        public void TestSimpleCollectionAddSerialization()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));

            derivedPropertyNode.Add("String3");
            basePropertyNode.Add("String4");
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var expectedBaseYaml = string.Format(SimpleCollectionAddBaseYaml.Replace("{}", "{{}}"), baseIds[2]);
            var expectedDerivedYaml = string.Format(SimpleCollectionAddDerivedYaml.Replace("{}", "{{}}"), baseIds[2], derivedIds[3]);
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Test]
        public void TestSimpleCollectionAddDeserialization()
        {
            var expectedBaseYaml = string.Format(SimpleCollectionAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(SimpleCollectionAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset2>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset2.MyStrings));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);

            Assert.AreEqual(3, context.BaseAsset.MyStrings.Count);
            Assert.AreEqual(4, context.DerivedAsset.MyStrings.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index(0)));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index(1)));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index(2)));
            Assert.AreEqual("String3", derivedPropertyNode.Retrieve(new Index(3)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index(3)));
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(4, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
            Assert.AreEqual(baseIds[2], derivedIds[2]);
        }

        [Test]
        public void TestSimpleDictionaryAddSerialization()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            baseIds.Add("Key1", IdentifierGenerator.Get(10));
            baseIds.Add("Key2", IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset3>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));

            // Update derived and check
            derivedPropertyNode.Add("String3", new Index("Key3"));
            basePropertyNode.Add("String4", new Index("Key4"));

            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var expectedBaseYaml = string.Format(SimpleDictionaryAddBaseYaml.Replace("{}", "{{}}"), baseIds["Key4"]);
            var expectedDerivedYaml = string.Format(SimpleDictionaryAddDerivedYaml.Replace("{}", "{{}}"), baseIds["Key4"], derivedIds["Key3"]);
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Test]
        public void TestSimpleDictionaryAddDeserialization()
        {
            var expectedBaseYaml = string.Format(SimpleDictionaryAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(SimpleDictionaryAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset3>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset3.MyDictionary));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.AreEqual(3, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(4, context.DerivedAsset.MyDictionary.Count);
            Assert.AreEqual("String1", basePropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", basePropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String4", basePropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual("String1", derivedPropertyNode.Retrieve(new Index("Key1")));
            Assert.AreEqual("String2", derivedPropertyNode.Retrieve(new Index("Key2")));
            Assert.AreEqual("String3", derivedPropertyNode.Retrieve(new Index("Key3")));
            Assert.AreEqual("String4", derivedPropertyNode.Retrieve(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key1")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key2")));
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index("Key3")));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index("Key4")));
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(4, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds["Key1"], derivedIds["Key1"]);
            Assert.AreEqual(baseIds["Key2"], derivedIds["Key2"]);
            Assert.AreEqual(baseIds["Key4"], derivedIds["Key4"]);
            Assert.AreEqual(3, context.BaseAsset.MyDictionary.Count);
            Assert.AreEqual(4, context.DerivedAsset.MyDictionary.Count);
        }

        [Test]
        public void TestObjectCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));

            basePropertyNode.Update(new Types.SomeObject { Value = "MyBaseString" }, new Index(1));
            derivedPropertyNode.Update(new Types.SomeObject { Value = "MyDerivedString" }, new Index(0));
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, ObjectCollectionUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, ObjectCollectionUpdateDerivedYaml, true);
        }

        [Test]
        public void TestObjectCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset4>.LoadFromYaml(ObjectCollectionUpdateBaseYaml, ObjectCollectionUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.AreEqual(2, context.BaseAsset.MyObjects.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyObjects.Count);
            Assert.AreEqual("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
        }

        [Test]
        public void TestObjectCollectionAddSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4>.DeriveAsset(asset);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));

            derivedPropertyNode.Add(new Types.SomeObject { Value = "String3" });
            basePropertyNode.Add(new Types.SomeObject { Value = "String4" });
            var expectedBaseYaml = string.Format(ObjectCollectionAddBaseYaml.Replace("{}", "{{}}"), baseIds[2]);
            var expectedDerivedYaml = string.Format(ObjectCollectionAddDerivedYaml.Replace("{}", "{{}}"), baseIds[2], derivedIds[3]);
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Test]
        public void TestObjectCollectionAddDeserialization()
        {
            var expectedBaseYaml = string.Format(ObjectCollectionAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(ObjectCollectionAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset4>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.AreEqual(3, context.BaseAsset.MyObjects.Count);
            Assert.AreEqual(4, context.DerivedAsset.MyObjects.Count);
            Assert.AreEqual("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual("String4", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(2))).Value);
            Assert.AreEqual("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual("String4", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(2))).Value);
            Assert.AreEqual("String3", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(3))).Value);
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(2)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(2)));
            Assert.AreEqual(OverrideType.New, derivedPropertyNode.GetItemOverride(new Index(3)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(2)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(3)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(3, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(4, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
            Assert.AreEqual(baseIds[2], derivedIds[2]);
        }

        [Test]
        public void TestObjectCollectionPropertyUpdateSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));

            basePropertyNode.IndexedTarget(new Index(1))[nameof(Types.SomeObject.Value)].Update("MyBaseString");
            derivedPropertyNode.IndexedTarget(new Index(0))[nameof(Types.SomeObject.Value)].Update("MyDerivedString");
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, ObjectCollectionPropertyUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, ObjectCollectionPropertyUpdateDerivedYaml, true);
        }

        [Test]
        public void TestObjectCollectionPropertyUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset4>.LoadFromYaml(ObjectCollectionPropertyUpdateBaseYaml, ObjectCollectionPropertyUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset4.MyObjects));
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.AreEqual(2, context.BaseAsset.MyObjects.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyObjects.Count);
            Assert.AreEqual("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.New, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreNotSame(baseIds, derivedIds);
            Assert.AreEqual(2, baseIds.KeyCount);
            Assert.AreEqual(0, baseIds.DeletedCount);
            Assert.AreEqual(2, derivedIds.KeyCount);
            Assert.AreEqual(0, derivedIds.DeletedCount);
            Assert.AreEqual(baseIds[0], derivedIds[0]);
            Assert.AreEqual(baseIds[1], derivedIds[1]);
        }

        [Test]
        public void TestNonIdentifiableObjectCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset8 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset8>.DeriveAsset(asset);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset8.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset8.MyObjects));
            // Manually link base of non-identifiable items - this simulates a scenario similar to prefabs
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.IndexedTarget(new Index(0)), basePropertyNode.IndexedTarget(new Index(0)));
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.IndexedTarget(new Index(1)), basePropertyNode.IndexedTarget(new Index(1)));
            context.DerivedGraph.RefreshBase(context.BaseGraph);

            basePropertyNode.IndexedTarget(new Index(1))[nameof(Types.SomeObject.Value)].Update("MyBaseString");
            derivedPropertyNode.IndexedTarget(new Index(0))[nameof(Types.SomeObject.Value)].Update("MyDerivedString");
            SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, NonIdentifiableObjectCollectionPropertyUpdateBaseYaml, false);
            SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml, true);
        }

        [Test]
        public void TestNonIdentifiableObjectCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset8>.LoadFromYaml(NonIdentifiableObjectCollectionPropertyUpdateBaseYaml, NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml);
            var basePropertyNode = (AssetMemberNode)((IContentNode)context.BaseGraph.RootNode).TryGetChild(nameof(Types.MyAsset8.MyObjects));
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode).TryGetChild(nameof(Types.MyAsset8.MyObjects));
            // Manually link base of non-identifiable items - this simulates a scenario similar to prefabs
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.IndexedTarget(new Index(0)), basePropertyNode.IndexedTarget(new Index(0)));
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.IndexedTarget(new Index(1)), basePropertyNode.IndexedTarget(new Index(1)));
            context.DerivedGraph.RefreshBase(context.BaseGraph);

            Assert.AreEqual(2, context.BaseAsset.MyObjects.Count);
            Assert.AreEqual(2, context.DerivedAsset.MyObjects.Count);
            Assert.AreEqual("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(0))).Value);
            Assert.AreEqual("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new Index(1))).Value);
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, basePropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(0)));
            Assert.AreEqual(OverrideType.Base, derivedPropertyNode.GetItemOverride(new Index(1)));
            Assert.AreEqual(OverrideType.New, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(0)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
            Assert.AreEqual(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Reference.AsEnumerable[new Index(1)].TargetNode.TryGetChild(nameof(Types.SomeObject.Value))).GetContentOverride());
        }

        [Test]
        public void TestGenerateOverridesForSerializationOfObjectMember()
        {
            const string expectedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+SomeObject,SiliconStudio.Assets.Quantum.Tests
Value*: OverriddenString
";
            var asset = new Types.MyAsset9 { MyObject = new Types.SomeObject { Value = "String1" } };
            var context = DeriveAssetTest<Types.MyAsset9>.DeriveAsset(asset);
            var derivedPropertyNode = (AssetMemberNode)((IContentNode)context.DerivedGraph.RootNode)[nameof(Types.MyAsset9.MyObject)];
            derivedPropertyNode.Target[nameof(Types.SomeObject.Value)].Update("OverriddenString");
            var expectedPath = new YamlAssetPath();
            expectedPath.PushMember(nameof(Types.SomeObject.Value));

            var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode);
            Assert.AreEqual(1, overrides.Count);
            Assert.True(overrides.ContainsKey(expectedPath));
            Assert.AreEqual(OverrideType.New, overrides[expectedPath]);

            // We expect the same resulting path both from the member node and the target object node
            overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode.Target);
            Assert.AreEqual(1, overrides.Count);
            Assert.True(overrides.ContainsKey(expectedPath));
            Assert.AreEqual(OverrideType.New, overrides[expectedPath]);

            // Test deserialization
            SerializeAndCompare(context.DerivedAsset.MyObject, overrides, expectedYaml);
            bool aliasOccurred;
            var instance = (Types.SomeObject)AssetFileSerializer.Default.Load(DeriveAssetTestBase.ToStream(expectedYaml), null, null, out aliasOccurred, out overrides);
            Assert.AreEqual("OverriddenString", instance.Value);
            Assert.AreEqual(1, overrides.Count);
            Assert.True(overrides.ContainsKey(expectedPath));
            Assert.AreEqual(OverrideType.New, overrides[expectedPath]);
        }

        [Test]
        public void TestGenerateOverridesForSerializationOfCollectionItem()
        {
            const string expectedYaml = @"!SiliconStudio.Assets.Quantum.Tests.Types+SomeObject,SiliconStudio.Assets.Quantum.Tests
Value*: OverriddenString
";
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset4>.DeriveAsset(asset);
            var derivedPropertyNode = (AssetObjectNode)context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)].IndexedTarget(new Index(1));
            derivedPropertyNode[nameof(Types.SomeObject.Value)].Update("OverriddenString");
            var expectedPath = new YamlAssetPath();
            expectedPath.PushMember(nameof(Types.SomeObject.Value));

            var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode);
            Assert.AreEqual(1, overrides.Count);
            Assert.True(overrides.ContainsKey(expectedPath));
            Assert.AreEqual(OverrideType.New, overrides[expectedPath]);

            // Test deserialization
            SerializeAndCompare(context.DerivedAsset.MyObjects[1], overrides, expectedYaml);
            bool aliasOccurred;
            var instance = (Types.SomeObject)AssetFileSerializer.Default.Load(DeriveAssetTestBase.ToStream(expectedYaml), null, null, out aliasOccurred, out overrides);
            Assert.AreEqual("OverriddenString", instance.Value);
            Assert.AreEqual(1, overrides.Count);
            Assert.True(overrides.ContainsKey(expectedPath));
            Assert.AreEqual(OverrideType.New, overrides[expectedPath]);
        }
    }
}

