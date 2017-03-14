﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Xenko.Graphics.Font
{
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<SignedDistanceFieldSpriteFont>), Profile = "Content")]
    [ContentSerializer(typeof(SignedDistanceFieldSpriteFontContentSerializer))]
    [DataSerializer(typeof(SignedDistanceFieldSpriteFontSerializer))]
    internal class SignedDistanceFieldSpriteFont : SpriteFont
    {
        internal Dictionary<char, Glyph> CharacterToGlyph;

        internal List<Texture> StaticTextures;

        internal SignedDistanceFieldSpriteFont()
        {
            FontType = SpriteFontType.SDF;
        }

        internal SignedDistanceFieldSpriteFont(float size, IList<Glyph> glyphs, IEnumerable<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings, float extraSpacing, float extraLineSpacing, char defaultCharacter)
        {
            FontType = SpriteFontType.SDF;

            Size = size;
            StaticTextures = new List<Texture>();
            CharacterToGlyph = new Dictionary<char, Glyph>(glyphs.Count);
            KerningMap = new Dictionary<int, float>();
            BaseOffsetY = baseOffset;
            DefaultLineSpacing = defaultLineSpacing;
            ExtraSpacing = extraSpacing;
            ExtraLineSpacing = extraLineSpacing;
            DefaultCharacter = defaultCharacter;

            // build the character map
            foreach (var glyph in glyphs)
            {
                var character = (char)glyph.Character;
                CharacterToGlyph[character] = glyph;
            }

            // Prepare kernings if they are available.
            if (kernings != null)
            {
                for (int i = 0; i < kernings.Count; i++)
                {
                    int key = (kernings[i].First << 16) | kernings[i].Second;
                    KerningMap.Add(key, kernings[i].Offset);
                }
            }

            // add the textures if any
            if (textures != null)
                StaticTextures.AddRange(textures);
        }

        public override IReadOnlyList<Texture> Textures
        {
            get { return StaticTextures; }
        }

        public override float GetExtraSpacing(float fontSize)
        {
            return ExtraSpacing;
        }

        public override float GetExtraLineSpacing(float fontSize)
        {
            return ExtraLineSpacing;
        }

        public override float GetFontDefaultLineSpacing(float fontSize)
        {
            return DefaultLineSpacing;
        }

        protected override float GetBaseOffsetY(float fontSize)
        {
            return BaseOffsetY;
        }

        public override bool IsCharPresent(char c)
        {
            return CharacterToGlyph.ContainsKey(c);
        }

        protected override Glyph GetGlyph(CommandList commandList, char character, ref Vector2 fontSize, bool dumb, out Vector2 fixScaling)
        {
            Glyph glyph = null;
            fixScaling = new Vector2(1, 1);

            if (!CharacterToGlyph.ContainsKey(character))
                Logger.Warning($"Character '{character}' is not available in the static font character map");
            else
                glyph = CharacterToGlyph[character];

            return glyph;
        }
    }
}
