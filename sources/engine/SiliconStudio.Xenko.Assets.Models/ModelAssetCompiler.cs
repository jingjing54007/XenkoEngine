﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Data;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Assets.Models
{
    public class ModelAssetCompiler : AssetCompilerBase
    {
        protected override void Compile(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ModelAsset)assetItem.Asset;
            // Get absolute path of asset source on disk
            var assetDirectory = assetItem.FullPath.GetParent();
            var assetSource = UPath.Combine(assetDirectory, asset.Source);

            var gameSettingsAsset = context.GetGameSettingsAsset();
            var renderingSettings = gameSettingsAsset.Get<RenderingSettings>();
            var allow32BitIndex = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_9_2;
            var allowUnsignedBlendIndices = context.GetGraphicsPlatform(assetItem.Package) != GraphicsPlatform.OpenGLES;
            var extension = asset.Source.GetFileExtension();

            // Find skeleton asset, if any
            AssetItem skeleton = null;
            if (asset.Skeleton != null)
                skeleton = assetItem.Package.FindAssetFromAttachedReference(asset.Skeleton);

            var importModelCommand = ImportModelCommand.Create(extension);
            if (importModelCommand == null)
            {
                result.Error($"No importer found for model extension '{extension}. The model '{assetSource}' can't be imported.");
                return;
            }

            importModelCommand.Mode = ImportModelCommand.ExportMode.Model;
            importModelCommand.SourcePath = assetSource;
            importModelCommand.Location = targetUrlInStorage;
            importModelCommand.Allow32BitIndex = allow32BitIndex;
            importModelCommand.AllowUnsignedBlendIndices = allowUnsignedBlendIndices;
            importModelCommand.Materials = asset.Materials;
            importModelCommand.ScaleImport = asset.ScaleImport;
            importModelCommand.PivotPosition = asset.PivotPosition;
            importModelCommand.SkeletonUrl = skeleton?.Location;

            result.BuildSteps = new AssetBuildStep(assetItem) { importModelCommand };
        }
    }
}
