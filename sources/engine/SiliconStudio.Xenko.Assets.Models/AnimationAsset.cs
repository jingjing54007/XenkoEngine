﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;

using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Xenko.Animations;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Assets.Models
{
    [DataContract("Animation")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(AnimationClip))]
    [AssetCompiler(typeof(AnimationAssetCompiler))]
    [Display(1805, "Animation")]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion)]
    [AssetUpgrader(XenkoConfig.PackageName, "0", "1.5.0-alpha02", typeof(EmptyAssetUpgrader))]
    [AssetUpgrader(XenkoConfig.PackageName, "1.5.0-alpha02", "1.10.0-alpha01", typeof(AnimationAssetUpgraderFramerate))]     // Force re-import for Min/Max frames
    public partial class AnimationAsset : Asset, IAssetCompileTimeDependencies
    {
        private const string CurrentVersion = "1.10.0-alpha01";

        /// <summary>
        /// The default file extension used by the <see cref="AnimationAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkanim;.pdxanim";

        public static readonly TimeSpan LongestTimeSpan = TimeSpan.FromMinutes(30);  // Avoid TimeSpan.MaxValue because it results in overflow exception when used in some calculations

        public AnimationAsset()
        {
            ClipDuration = new AnimationAssetDuration()
            {
                Enabled = false,
                StartAnimationTime   = TimeSpan.Zero,
                EndAnimationTime     = LongestTimeSpan,
            };
        }

        /// <summary>
        /// Gets or sets the source file of this asset.
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The source file of this asset.
        /// </userdoc>
        [DataMember(-50)]
        [DefaultValue(null)]
        [SourceFileMember(true)]
        public UFile Source { get; set; } = new UFile("");

        [DataMemberIgnore]
        public override UFile MainSource => Source;

        /// <summary>
        /// Enable clipping of the animation duration
        /// </summary>
        /// <userdoc>
        /// Enable clipping of the animation duration, constraining start and end frames.
        /// </userdoc>
        [DataMember(0)]
        [Display("Clip duration")]
        public AnimationAssetDuration ClipDuration { get; set; }

        // This property is marked as hidden by the AnimationViewModel
        public TimeSpan AnimationTimeMinimum { get; set; }

        // This property is marked as hidden by the AnimationViewModel
        public TimeSpan AnimationTimeMaximum { get; set; }

        /// <summary>
        /// Gets or sets the pivot position, that will be used as center of object.
        /// </summary>
        [DataMember(10)]
        public Vector3 PivotPosition { get; set; }

        /// <summary>
        /// Gets or sets the scale import.
        /// </summary>
        /// <value>The scale import.</value>
        /// <userdoc>The scale factor to apply to the imported animation.</userdoc>
        [DataMember(15)]
        [DefaultValue(1.0f)]
        public float ScaleImport { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the animation repeat mode.
        /// </summary>
        /// <value>The repeat mode</value>
        /// <userdoc>Specifies how the animation should be played. That is played once and stop, infinitely loop, etc...</userdoc>
        [DataMember(20)]
        public AnimationRepeatMode RepeatMode { get; set; } = AnimationRepeatMode.LoopInfinite;

        /// <summary>
        /// Determine the animation type of the asset, which will dictate in what blend mode we can use it
        /// </summary>
        /// <userdoc>
        /// The animation type of the asset decides how we blend it - linear blending will be used for Animation Clip, additive blending for Difference Clip
        /// </userdoc>
        [NotNull]
        [DataMember(30)]
        public AnimationAssetType Type { get; set; } = new StandardAnimationAssetType();

        /// <summary>
        /// Gets or sets the Skeleton.
        /// </summary>
        /// <userdoc>
        /// Describes the node hierarchy that will be active at runtime.
        /// </userdoc>
        [DataMember(50)]
        public Skeleton Skeleton { get; set; }

        /// <summary>
        /// Gets or sets a boolean describing if root movement should be applied inside Skeleton (if false and a skeleton exists) or on TransformComponent (if true)
        /// </summary>
        /// <userdoc>
        /// When root motion is enabled, main motion will be applied to TransformComponent. If false, it will be applied inside the skeleton nodes.
        /// Note that if there is no skeleton, it will always apply motion to TransformComponent.
        /// </userdoc>
        [DataMember(60)]
        public bool RootMotion { get; set; }

        /// <summary>
        /// Gets or sets the preview model
        /// </summary>
        /// <userdoc>
        /// Choose a model to preview with.
        /// </userdoc>
        [DataMember(100)]
        public Model PreviewModel { get; set; }

        /// <inheritdoc/>
        public IEnumerable<IReference> EnumerateCompileTimeDependencies(PackageSession session)
        {
            var reference = AttachedReferenceManager.GetAttachedReference(Skeleton);
            if (reference != null)
            {
                yield return new AssetReference(reference.Id, reference.Url);
            }
        }
    }
}
