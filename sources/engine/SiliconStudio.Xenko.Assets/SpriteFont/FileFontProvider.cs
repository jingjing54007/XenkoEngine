﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Xenko.Assets.SpriteFont.Compiler;
using SiliconStudio.Xenko.Graphics.Font;

namespace SiliconStudio.Xenko.Assets.SpriteFont
{
    [DataContract("FileFontProvider")]
    [Display("Font from File")]
    public class FileFontProvider : FontProviderBase
    {
        /// <summary>
        /// Gets or sets the source file containing the font data. This can be a TTF file or a bitmap file.
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The path to the file containing the font data to use.
        /// </userdoc>
        [DataMember(10)]
        [Display("Source")]
        public UFile Source { get; set; } = new UFile("");

        /// <inheritdoc/>
        public override FontFace GetFontFace()
        {
            if (!File.Exists(Source))
            {
                // Font does not exist
                throw new FontNotFoundException(Source);
            }

            var factory = new Factory();

            using (var fontFile = new FontFile(factory, Source))
            {
                FontSimulations fontSimulations;
                switch (Style)
                {
                    case Xenko.Graphics.Font.FontStyle.Regular:
                        fontSimulations = FontSimulations.None;
                        break;
                    case Xenko.Graphics.Font.FontStyle.Bold:
                        fontSimulations = FontSimulations.Bold;
                        break;
                    case Xenko.Graphics.Font.FontStyle.Italic:
                        fontSimulations = FontSimulations.Oblique;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RawBool isSupported;
                FontFileType fontType;
                FontFaceType faceType;
                int numberFaces;

                fontFile.Analyze(out isSupported, out fontType, out faceType, out numberFaces);

                return new FontFace(factory, faceType, new[] { fontFile }, 0, fontSimulations);
            }
        }

        /// <inheritdoc/>
        public override string GetFontPath(AssetCompilerResult result = null)
        {
            if (!File.Exists(Source))
            {
                result?.Error($"Cannot find font file '{Source}'. Make sure it exists and is referenced correctly.");
            }
            return Source;
        }

        /// <inheritdoc/>
        public override string GetFontName()
        {
            return Source.GetFileName();
        }
    }
}
