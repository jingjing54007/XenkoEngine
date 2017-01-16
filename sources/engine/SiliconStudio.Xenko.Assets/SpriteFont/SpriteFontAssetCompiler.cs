﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#pragma warning disable 162 // Unreachable code detected (due to useCacheFonts)
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Xenko.Assets.SpriteFont.Compiler;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Graphics.Font;

namespace SiliconStudio.Xenko.Assets.SpriteFont
{
    public class SpriteFontAssetCompiler : AssetCompilerBase
    {
        private static readonly FontDataFactory FontDataFactory = new FontDataFactory();

        protected override void Compile(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SpriteFontAsset)assetItem.Asset;
            UFile assetAbsolutePath = assetItem.FullPath;
            var colorSpace = context.GetColorSpace();

            if (asset.FontType is SignedDistanceFieldSpriteFontType)
            {
                var fontTypeSDF = asset.FontType as SignedDistanceFieldSpriteFontType;

                // copy the asset and transform the source and character set file path to absolute paths
                var assetClone = AssetCloner.Clone(asset);
                var assetDirectory = assetAbsolutePath.GetParent();
                assetClone.FontSource = asset.FontSource;
                fontTypeSDF.CharacterSet = !string.IsNullOrEmpty(fontTypeSDF.CharacterSet) ? UPath.Combine(assetDirectory, fontTypeSDF.CharacterSet) : null;

                result.BuildSteps = new AssetBuildStep(assetItem) { new SignedDistanceFieldFontCommand(targetUrlInStorage, assetClone) };
            }
            else
                if (asset.FontType is RuntimeRasterizedSpriteFontType)
                {
                    UFile fontPathOnDisk = asset.FontSource.GetFontPath(result);
                    if (fontPathOnDisk == null)
                    {
                        result.Error($"Runtime rasterized font compilation failed. Font {asset.FontSource.GetFontName()} was not found on this machine.");
                        result.BuildSteps = new AssetBuildStep(assetItem) { new FailedFontCommand() };
                        return;
                    }

                    var fontImportLocation = FontHelper.GetFontPath(asset.FontSource.GetFontName(), asset.FontSource.Style);

                    result.BuildSteps = new AssetBuildStep(assetItem)
                    {
                        new ImportStreamCommand { SourcePath = fontPathOnDisk, Location = fontImportLocation },
                        new RuntimeRasterizedFontCommand(targetUrlInStorage, asset)
                    };  
                }
                else
                {
                    var fontTypeStatic = asset.FontType as OfflineRasterizedSpriteFontType;
                    if (fontTypeStatic == null)
                        throw new ArgumentException("Tried to compile a non-offline rasterized sprite font with the compiler for offline resterized fonts!");

                    // copy the asset and transform the source and character set file path to absolute paths
                    var assetClone = AssetCloner.Clone(asset);
                    var assetDirectory = assetAbsolutePath.GetParent();
                    assetClone.FontSource = asset.FontSource;
                    fontTypeStatic.CharacterSet = !string.IsNullOrEmpty(fontTypeStatic.CharacterSet) ? UPath.Combine(assetDirectory, fontTypeStatic.CharacterSet): null;

                    result.BuildSteps = new AssetBuildStep(assetItem) { new OfflineRasterizedFontCommand(targetUrlInStorage, assetClone, colorSpace) };
                }
        }

        internal class OfflineRasterizedFontCommand : AssetCommand<SpriteFontAsset>
        {
            private ColorSpace colorspace;

            public OfflineRasterizedFontCommand(string url, SpriteFontAsset description, ColorSpace colorspace)
                : base(url, description)
            {
                this.colorspace = colorspace;
            }

            protected override IEnumerable<ObjectUrl> GetInputFilesImpl()
            {
                var fontTypeStatic = Parameters.FontType as OfflineRasterizedSpriteFontType;
                if (fontTypeStatic == null)
                    throw new ArgumentException("Tried to compile a dynamic sprite font with compiler for signed distance field fonts");

                if (File.Exists(fontTypeStatic.CharacterSet))
                    yield return new ObjectUrl(UrlType.File, fontTypeStatic.CharacterSet);
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                writer.Write(colorspace);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // try to import the font from the original bitmap or ttf file
                Graphics.SpriteFont staticFont;
                try
                {
                    staticFont = OfflineRasterizedFontCompiler.Compile(FontDataFactory, Parameters, colorspace == ColorSpace.Linear);
                }
                catch (FontNotFoundException ex) 
                {
                    commandContext.Logger.Error($"Font [{ex.FontName}] was not found on this machine.", ex);
                    return Task.FromResult(ResultStatus.Failed);
                }

                // check that the font data is valid
                if (staticFont == null || staticFont.Textures.Count == 0)
                    return Task.FromResult(ResultStatus.Failed);

                // save the data into the database
                var assetManager = new ContentManager();
                assetManager.Save(Url, staticFont);

                // dispose textures allocated by the StaticFontCompiler
                foreach (var texture in staticFont.Textures)
                    texture.Dispose();

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        /// <summary>
        /// Scalable (SDF) font build step
        /// </summary>
        internal class SignedDistanceFieldFontCommand : AssetCommand<SpriteFontAsset>
        {
            public SignedDistanceFieldFontCommand(string url, SpriteFontAsset description)
                : base(url, description)
            {
            }

            protected override IEnumerable<ObjectUrl> GetInputFilesImpl()
            {
                var fontTypeSDF = Parameters.FontType as SignedDistanceFieldSpriteFontType;
                if (fontTypeSDF == null)
                    throw new ArgumentException("Tried to compile a dynamic sprite font with compiler for signed distance field fonts");

                if (File.Exists(fontTypeSDF.CharacterSet))
                    yield return new ObjectUrl(UrlType.File, fontTypeSDF.CharacterSet);
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                // TODO Add parameter hash codes here
                // writer.Write(colorspace);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // try to import the font from the original bitmap or ttf file
                Graphics.SpriteFont scalableFont;
                try
                {
                    scalableFont = SignedDistanceFieldFontCompiler.Compile(FontDataFactory, Parameters);
                }
                catch (FontNotFoundException ex)
                {
                    commandContext.Logger.Error($"Font [{ex.FontName}] was not found on this machine.", ex);
                    return Task.FromResult(ResultStatus.Failed);
                }

                // check that the font data is valid
                if (scalableFont == null || scalableFont.Textures.Count == 0)
                    return Task.FromResult(ResultStatus.Failed);

                // save the data into the database
                var assetManager = new ContentManager();
                assetManager.Save(Url, scalableFont);

                // dispose textures allocated by the StaticFontCompiler
                foreach (var texture in scalableFont.Textures)
                    texture.Dispose();

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        internal class RuntimeRasterizedFontCommand : AssetCommand<SpriteFontAsset>
        {
            public RuntimeRasterizedFontCommand(string url, SpriteFontAsset description)
                : base(url, description)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var dynamicFont = FontDataFactory.NewDynamic(
                    Parameters.FontType.Size, Parameters.FontSource.GetFontName(), Parameters.FontSource.Style, 
                    Parameters.FontType.AntiAlias, useKerning:false, extraSpacing:Parameters.Spacing, extraLineSpacing:Parameters.LineSpacing, 
                    defaultCharacter:Parameters.DefaultCharacter);

                var assetManager = new ContentManager();
                assetManager.Save(Url, dynamicFont);

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        /// <summary>
        /// Proxy command which always fails, called when font is compiled with the wrong assets
        /// </summary>
        internal class FailedFontCommand : AssetCommand<SpriteFontAsset>
        {
            public FailedFontCommand() : base(null, null) { }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                return Task.FromResult(ResultStatus.Failed);
            }
        }
    }
}
