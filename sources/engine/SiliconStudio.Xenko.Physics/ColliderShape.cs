﻿// Copyright (c) 2014-2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Physics
{
    public class ColliderShape : IDisposable
    {
        protected const float DebugScaling = 1.0f;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (InternalShape == null) return;
            InternalShape.Dispose();
            InternalShape = null;
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public ColliderShapeTypes Type { get; protected set; }

        /// <summary>
        /// The local offset
        /// </summary>
        public Vector3 LocalOffset;

        /// <summary>
        /// The local rotation
        /// </summary>
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <summary>
        /// Updates the local transformations, required if you change LocalOffset and/or LocalRotation.
        /// </summary>
        public virtual void UpdateLocalTransformations()
        {
            //cache matrices used to translate the position from and to physics engine / gfx engine
            PositiveCenterMatrix = Matrix.RotationQuaternion(LocalRotation) * (Parent == null ? Matrix.Translation(LocalOffset * CachedScaling) : Matrix.Translation(LocalOffset));
            NegativeCenterMatrix = PositiveCenterMatrix;
            NegativeCenterMatrix.Invert();
        }

        /// <summary>
        /// Gets the positive center matrix.
        /// </summary>
        /// <value>
        /// The positive center matrix.
        /// </value>
        public Matrix PositiveCenterMatrix;

        /// <summary>
        /// Gets the negative center matrix.
        /// </summary>
        /// <value>
        /// The negative center matrix.
        /// </value>
        public Matrix NegativeCenterMatrix;

        protected Vector3 CachedScaling;

        /// <summary>
        /// Gets or sets the scaling.
        /// Make sure that you manually created and assigned an exclusive ColliderShape to the Collider otherwise since the engine shares shapes among many Colliders, all the colliders will be scaled.
        /// Please note that this scaling has no relation to the TransformComponent scaling.
        /// </summary>
        /// <value>
        /// The scaling.
        /// </value>
        public virtual Vector3 Scaling
        {
            get
            {
                return CachedScaling;
            }
            set
            {
                var oldScale = CachedScaling;

                CachedScaling = value;
                if (Is2D && Type == ColliderShapeTypes.Box) CachedScaling.Z = 0.001f; //Box is not working properly when in a convex2dshape, Z cannot be 0
                else if(Is2D) CachedScaling.Z = 0.0f;

                if (Parent == null)
                {
                    InternalShape.LocalScaling = CachedScaling;
                }

                UpdateLocalTransformations();

                //If we have a debug entity apply correct scaling to it as well
                if (DebugEntity == null) return;

                var invertedScale = Matrix.Scaling(oldScale);
                invertedScale.Invert();
                var unscaledMatrix = DebugEntity.Transform.LocalMatrix*invertedScale;
                var newScale = Matrix.Scaling(CachedScaling);
                DebugEntity.Transform.LocalMatrix = unscaledMatrix*newScale;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collider shape is 2D.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is2 d]; otherwise, <c>false</c>.
        /// </value>
        public bool Is2D { get; internal set; }

        public IColliderShapeDesc Description { get; internal set; }

        internal BulletSharp.CollisionShape InternalShape;

        internal CompoundColliderShape Parent;

        public virtual MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return null;
        }

        public Matrix DebugPrimitiveMatrix;

        internal bool NeedsCustomCollisionCallback;

        internal bool IsPartOfAsset = false;

        internal Entity DebugEntity;
    }
}
