﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Xenko.Assets.Textures;
using SiliconStudio.Xenko.Importer.Common;

namespace SiliconStudio.Xenko.Assets.Models
{
    public class FbxAssetImporter : ModelAssetImporter
    {
        static FbxAssetImporter()
        {
            NativeLibrary.PreloadLibrary("libfbxsdk.dll");
        }

        // Supported file extensions for this importer
        private const string FileExtensions = ".fbx";

        private static readonly Guid Uid = new Guid("a15ae42d-42c5-4a3b-9f7e-f8cd91eda595");

        public override Guid Id => Uid;

        public override string Description => "FBX importer used for creating entities, 3D Models or animations assets";

        public override string SupportedFileExtensions => FileExtensions;

        /// <inheritdoc/>
        public override EntityInfo GetEntityInfo(UFile localPath, Logger logger, AssetImporterParameters importParameters)
        {
            var meshConverter = new Importer.FBX.MeshConverter(logger);
            var entityInfo = meshConverter.ExtractEntity(localPath.FullPath, importParameters.IsTypeSelectedForOutput(typeof(TextureAsset)));
            return entityInfo;
        }

        /// <inheritdoc/>
        public override void GetAnimationDuration(UFile localPath, Logger logger, AssetImporterParameters importParameters, out TimeSpan startTime, out TimeSpan endTime)
        {
            var meshConverter = new Importer.FBX.MeshConverter(logger);
            var durationInSeconds = meshConverter.GetAnimationDuration(localPath.FullPath);

            startTime = TimeSpan.Zero;
            endTime = TimeSpan.FromSeconds(durationInSeconds);
        }
    }
}
