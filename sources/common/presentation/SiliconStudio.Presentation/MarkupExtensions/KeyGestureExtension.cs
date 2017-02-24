﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows.Input;
using System.Windows.Markup;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Presentation.MarkupExtensions
{
    /// <summary>
    /// This markup extension allows to create a <see cref="KeyGesture"/> instance from a string representing the gesture.
    /// </summary>
    public class KeyGestureExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the key gesture.
        /// </summary>
        public KeyGesture Gesture { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGestureExtension"/> class with a string representing the gesture.
        /// </summary>
        /// <param name="gesture">A string representing the gesture.</param>
        public KeyGestureExtension([NotNull] string gesture)
        {
            var modifiers = ModifierKeys.None;
            var tokens = gesture.Split('+');
            for (int i = 0; i < tokens.Length - 1; ++i)
            {
                var token = tokens[i].Replace("Ctrl", "Control");
                var modifier = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), token, true);
                modifiers |= modifier;
            }
            var key = (Key)Enum.Parse(typeof(Key), tokens[tokens.Length - 1], true);
            Gesture = new KeyGesture(key, modifiers);
        }

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Gesture;
        }
    }
}