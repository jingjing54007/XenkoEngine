﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Windows;
using SiliconStudio.Core.Annotations;

// http://www.thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/

namespace SiliconStudio.Presentation.Core
{
    /// <summary>
    /// A class that serves as a proxy for data binding. As a freezable, its <see cref="Data"/> dependency property can inherit data context from a container <see cref="DependencyObject"/>.
    /// </summary>
    public class BindingProxy : Freezable
    {
        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy));

        /// <summary>
        /// Gets or sets the data contained in this <see cref="BindingProxy"/>.
        /// </summary>
        public object Data { get { return GetValue(DataProperty); } set { SetValue(DataProperty, value); } }

        /// <inheritdoc/>
        [NotNull]
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}