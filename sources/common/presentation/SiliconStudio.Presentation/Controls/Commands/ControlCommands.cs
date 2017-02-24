﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Extensions;

namespace SiliconStudio.Presentation.Controls.Commands
{
    /// <summary>
    /// This class provides an instance of all commands in the namespace <see cref="Commands"/>.
    /// These instances can be used in XAML with the <see cref="System.Windows.Markup.StaticExtension"/> markup extension.
    /// </summary>
    public static class ControlCommands
    {
        /// <summary>
        /// Initialize the static properties of the <see cref="ControlCommands"/> class.
        /// </summary>
        static ControlCommands()
        {
            ClearSelectionCommand = new RoutedCommand(nameof(ClearSelectionCommand), typeof(Selector));
            CommandManager.RegisterClassCommandBinding(typeof(Selector), new CommandBinding(ClearSelectionCommand, OnClearSelectionCommand));
            SetAllVectorComponentsCommand = new RoutedCommand(nameof(SetAllVectorComponentsCommand), typeof(VectorEditorBase));
            CommandManager.RegisterClassCommandBinding(typeof(VectorEditorBase), new CommandBinding(SetAllVectorComponentsCommand, OnSetAllVectorComponents));
            ResetValueCommand = new RoutedCommand(nameof(ResetValueCommand), typeof(VectorEditorBase));
            CommandManager.RegisterClassCommandBinding(typeof(VectorEditorBase), new CommandBinding(ResetValueCommand, OnResetValue));
        }

        /// <summary>
        /// Clears the current selection of a text box.
        /// </summary>
        [NotNull]
        public static RoutedCommand ClearSelectionCommand { get; }

        /// <summary>
        /// Sets all the components of a <see cref="VectorEditorBase"/> to the value given as parameter.
        /// </summary>
        [NotNull]
        public static RoutedCommand SetAllVectorComponentsCommand { get; }

        /// <summary>
        /// Resets the current value of a vector editor to the value set in the <see cref="VectorEditorBase{T}.DefaultValue"/> property.
        /// </summary>
        [NotNull]
        public static RoutedCommand ResetValueCommand { get; }
        
        private static void OnClearSelectionCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var selector = sender as Selector;
            if (selector != null)
            {
                selector.SelectedItem = null;
            }
        }

        private static void OnSetAllVectorComponents(object sender, ExecutedRoutedEventArgs e)
        {
            var vectorEditor = sender as VectorEditorBase;
            if (vectorEditor != null)
            {
                try
                {
                    var value = Convert.ToSingle(e.Parameter);
                    vectorEditor.SetVectorFromValue(value);
                }
                catch (Exception ex)
                {
                    ex.Ignore();
                }
            }
        }

        private static void OnResetValue(object sender, ExecutedRoutedEventArgs e)
        {
            var vectorEditor = sender as VectorEditorBase;
            vectorEditor?.ResetValue();
        }
    }
}
