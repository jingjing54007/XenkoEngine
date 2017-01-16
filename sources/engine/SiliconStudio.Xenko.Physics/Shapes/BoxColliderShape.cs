﻿// Copyright (c) 2014-2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Extensions;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Graphics.GeometricPrimitives;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Physics
{
    public class BoxColliderShape : ColliderShape
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoxColliderShape"/> class.
        /// </summary>
        /// <param name="is2D">If this cube is a 2D quad</param>
        /// <param name="size">The size of the cube</param>
        public BoxColliderShape(bool is2D, Vector3 size)
        {
            Type = ColliderShapeTypes.Box;
            Is2D = is2D;

            //Box is not working properly when in a convex2dshape, Z cannot be 0

            CachedScaling = Is2D ? new Vector3(1, 1, 0.001f) : Vector3.One;

            if (is2D) size.Z = 0.001f;

            var shape = new BulletSharp.BoxShape(size/2)
            {
                LocalScaling = CachedScaling
            };

            if (Is2D)
            {
                InternalShape = new BulletSharp.Convex2DShape(shape) { LocalScaling = CachedScaling };
            }
            else
            {
                InternalShape = shape;
            }

            DebugPrimitiveMatrix = Matrix.Scaling(size * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cube.New(device).ToMeshDraw();
        }
    }
}
