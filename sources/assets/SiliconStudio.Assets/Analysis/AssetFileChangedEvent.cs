// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Assets.Analysis
{
    /// <summary>
    /// An event that notifies the type of disk change for an asset.
    /// </summary>
    public class AssetFileChangedEvent : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFileChangedEvent"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="assetLocation">The asset URL.</param>
        public AssetFileChangedEvent(Package package, AssetFileChangedType changeType, UFile assetLocation)
        {
            Package = package;
            ChangeType = changeType;
            AssetLocation = assetLocation.GetDirectoryAndFileNameWithoutExtension(); // Make sure we are using the location withint the package without the extension
        }

        /// <summary>
        /// Gets the package the event is related to.
        /// </summary>
        /// <value>The package.</value>
        public Package Package { get; set; }

        /// <summary>
        /// Gets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        public AssetFileChangedType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the asset identifier.
        /// </summary>
        /// <value>The asset identifier.</value>
        public Guid AssetId { get; set; }

        /// <summary>
        /// Gets the asset location relative to the package.
        /// </summary>
        /// <value>The asset location.</value>
        public UFile AssetLocation { get; set; }

        /// <summary>
        /// Gets or sets the hash of the asset source (optional).
        /// </summary>
        /// <value>
        /// The hash of the asset source.
        /// </value>
        public ObjectId? Hash { get; set; }
    }
}