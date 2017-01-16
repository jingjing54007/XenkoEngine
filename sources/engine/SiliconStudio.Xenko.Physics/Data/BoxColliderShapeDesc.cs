﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<BoxColliderShapeDesc>))]
    [DataContract("BoxColliderShapeDesc")]
    [Display(50, "Box")]
    public class BoxColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Select this if this shape will represent a Circle 2D shape
        /// </userdoc>
        [DataMember(5)]
        public bool Is2D;

        /// <userdoc>
        /// The size of one edge of the box.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Size = Vector3.One;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public int CompareTo(object obj)
        {
            var other = obj as BoxColliderShapeDesc;
            if (other == null) return -1;
            if (other.Is2D == Is2D && other.Size == Size && other.LocalOffset == LocalOffset && other.LocalRotation == LocalRotation) return 0;
            return 1;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Is2D.GetHashCode();
                hashCode = (hashCode*397) ^ Size.GetHashCode();
                hashCode = (hashCode*397) ^ LocalOffset.GetHashCode();
                hashCode = (hashCode*397) ^ LocalRotation.GetHashCode();
                return hashCode;
            }
        }
    }
}