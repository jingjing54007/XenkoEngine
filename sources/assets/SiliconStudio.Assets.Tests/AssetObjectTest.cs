// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.IO;

namespace SiliconStudio.Assets.Tests
{
    [DataContract("!AssetObjectTest")]
    [AssetDescription(FileExtension)]
    public class AssetObjectTest : TestAssetWithParts, IEquatable<AssetObjectTest>
    {
        private const string FileExtension = ".xktest";

        [DefaultValue(null)]
        public AssetReference Reference { get; set; }

        [DefaultValue(null)]
        public UFile RawAsset { get; set; }

        public bool Equals(AssetObjectTest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Reference, other.Reference) && Equals(RawAsset, other.RawAsset);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AssetObjectTest)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (Reference != null ? Reference.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (RawAsset != null ? RawAsset.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(AssetObjectTest left, AssetObjectTest right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AssetObjectTest left, AssetObjectTest right)
        {
            return !Equals(left, right);
        }
    }

    [DataContract("!TestAssetWithParts")]
    [AssetDescription(FileExtension)]
    public class TestAssetWithParts : AssetComposite
    {
        private const string FileExtension = ".xkpart";

        public TestAssetWithParts()
        {
            Parts = new List<AssetPartTestItem>();
        }

        public string Name { get; set; }

        public List<AssetPartTestItem> Parts { get; set; }

        public override IEnumerable<AssetPart> CollectParts()
        {
            return Parts.Select(it => new AssetPart(it.Id, it.Base, x => it.Base = x));
        }

        public override IIdentifiable FindPart(Guid partId)
        {
            return Parts.FirstOrDefault(x => x.Id == partId);
        }

        public override bool ContainsPart(Guid id)
        {
            return Parts.Any(t => t.Id == id);
        }

        protected override object ResolvePartReference(object referencedObject)
        {
            throw new NotImplementedException();
        }

        public override Asset CreateDerivedAsset(string baseLocation, out Dictionary<Guid, Guid> idRemapping)
        {
            var asset = (TestAssetWithParts)base.CreateDerivedAsset(baseLocation, out idRemapping);

            // Create asset with new base
            var assetRef = new AssetReference(Id, baseLocation);
            var instanceId = Guid.NewGuid();
            for (var i = 0; i < asset.Parts.Count; i++)
            {
                // Properly set the base
                asset.Parts[i].Base = new BasePart(assetRef, Parts[i].Id, instanceId);
            }

            return asset;
        }

        public void AddParts(TestAssetWithParts assetBaseWithParts)
        {
            if (assetBaseWithParts == null) throw new ArgumentNullException(nameof(assetBaseWithParts));

            // The assetPartBase must be a plain child asset
            if (assetBaseWithParts.Archetype == null) throw new InvalidOperationException($"Expecting a Base for {nameof(assetBaseWithParts)}");

            Parts.AddRange(assetBaseWithParts.Parts);
        }
    }

    [DataContract("AssetPartTestItem")]
    public class AssetPartTestItem : IIdentifiable
    {
        public AssetPartTestItem()
        {
        }

        public AssetPartTestItem(Guid id)
        {
            Id = id;
        }

        public AssetPartTestItem(Guid id, AssetReference baseAsset, Guid baseId, Guid basePartInstanceId)
        {
            Base = new BasePart(baseAsset, baseId, basePartInstanceId);
            Id = id;
        }

        public BasePart Base { get; set; }

        [NonOverridable]
        public Guid Id { get; set; }
    }

    [DataContract("!AssetImportObjectTest")]
    [AssetDescription(".xkimptest")]
    public class AssetImportObjectTest : AssetWithSource
    {
        public AssetImportObjectTest()
        {
            References = new Dictionary<string, AssetReference>();
        }

        public string Name { get; set; }

        [DefaultValue(null)]
        public Dictionary<string, AssetReference> References { get; set; }
    }

    [DataContract("!AssetObjectTestSub")]
    [AssetDescription(".xktestsub")]
    public class AssetObjectTestSub : Asset
    {
        public int Value { get; set; }
    }
}

