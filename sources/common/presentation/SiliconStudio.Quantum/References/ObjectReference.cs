﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Quantum.References
{
    /// <summary>
    /// A class representing a reference to another object that has a different model.
    /// </summary>
    public sealed class ObjectReference : IReferenceInternal
    {
        private readonly Type type;
        private object orphanObject;

        /// <summary>
        /// Initialize a new instance of the <see cref="ObjectReference"/> class using a data object.
        /// </summary>
        /// <remarks>This constructor should be used when the given <see cref="objectValue"/> has no mode node yet existing.</remarks>
        /// <param name="objectValue">A data object to reference. Can be null.</param>
        /// <param name="objectType">The type of data object to reference.</param>
        /// <param name="index">The index of this reference in its parent reference, if it is a <see cref="ReferenceEnumerable"/>.</param>
        internal ObjectReference(object objectValue, Type objectType, Index index)
        {
            Reference.CheckReferenceCreationSafeGuard();
            if (objectType == null) throw new ArgumentNullException(nameof(objectType));
            if (objectValue != null && !objectType.IsInstanceOfType(objectValue)) throw new ArgumentException(@"The given type does not match the given object.", nameof(objectValue));
            orphanObject = objectValue;
            type = objectType;
            Index = index;
        }

        /// <summary>
        /// Gets the model node targeted by this reference, if available.
        /// </summary>
        public IObjectNode TargetNode { get; private set; }

        /// <inheritdoc/>
        public object ObjectValue => TargetNode != null ? TargetNode.Value : orphanObject;

        /// <summary>
        /// Gets the index of this reference in its parent collection. If the reference is not in a collection, this will return <see cref="Quantum.Index.Empty"/>.
        /// </summary>
        public Index Index { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> of the model node targeted by this reference, if available.
        /// </summary>
        public Guid TargetGuid { get; private set; }

        /// <inheritdoc/>
        public bool HasIndex(Index index)
        {
            return index.IsEmpty;
        }

        public void Refresh(IContentNode ownerNode, NodeContainer nodeContainer)
        {
            Refresh(ownerNode, nodeContainer, Index.Empty);
        }

        internal void Refresh(IContentNode ownerNode, NodeContainer nodeContainer, Index index)
        {
            var objectValue = ownerNode.Retrieve(index);

            var boxedTarget = TargetNode as BoxedContent;
            if (boxedTarget != null && objectValue?.GetType() == TargetNode.Type)
            {
                // If we are boxing a struct, and the targeted type didn't change, we reuse the same nodes and just overwrite the struct value.
                boxedTarget.UpdateFromOwner(objectValue);
                // But we still need to refresh inner references!
                foreach (var member in TargetNode.Members.Where(x => x.IsReference))
                {
                    nodeContainer?.UpdateReferences(member);
                }
            }
            else if (TargetNode?.Value != objectValue)
            {
                // This call will recursively update the references.
                var target = SetTarget(objectValue, nodeContainer);
                if (target != null)
                {
                    var boxedContent = target as BoxedContent;
                    boxedContent?.SetOwnerContent(ownerNode, index);
                }
            }
            // This reference is not orphan anymore.
            orphanObject = null;
        }

        /// <inheritdoc/>
        public IEnumerable<ObjectReference> Enumerate()
        {
            yield return this;
        }

        /// <inheritdoc/>
        public bool Equals(IReference other)
        {
            var otherReference = other as ObjectReference;
            if (otherReference == null)
                return false;

            return TargetGuid == otherReference.TargetGuid && TargetNode == otherReference.TargetNode;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{{(Index != Index.Empty? $"[{Index.Value}]" : "")} -> {TargetNode}}}";
        }

        /// <summary>
        /// Set the <see cref="TargetNode"/> and <see cref="TargetGuid"/> of the targeted object by retrieving it from or creating it to the given <see cref="NodeContainer"/>.
        /// </summary>
        /// <param name="objectValue">The value for which to set the target node.</param>
        /// <param name="nodeContainer">The <see cref="NodeContainer"/> used to retrieve or create the target node.</param>
        internal IContentNode SetTarget(object objectValue, NodeContainer nodeContainer)
        {
            if (nodeContainer == null) throw new ArgumentNullException(nameof(nodeContainer));
            var targetNode = nodeContainer.GetOrCreateNodeInternal(objectValue);
            SetTarget(targetNode);
            return targetNode;
        }

        /// <summary>
        /// Set the <see cref="TargetNode"/> and <see cref="TargetGuid"/> of the targeted object by retrieving it from or creating it to the given <see cref="NodeContainer"/>.
        /// </summary>
        /// <param name="targetNode">The <see cref="NodeContainer"/> used to retrieve or create the target node.</param>
        internal IObjectNode SetTarget(IObjectNode targetNode)
        {
            if (targetNode != null)
            {
                if (targetNode.Value != null && !type.IsInstanceOfType(targetNode.Value))
                    throw new InvalidOperationException(@"The type of the retrieved node content does not match the type of this reference");

                if (targetNode.Value != null && !type.IsInstanceOfType(targetNode.Value))
                    throw new InvalidOperationException("TargetNode type does not match the reference type.");

                TargetNode = targetNode;
                TargetGuid = targetNode.Guid;
            }
            else
            {
                TargetNode = null;
                TargetGuid = Guid.Empty;
            }

            return targetNode;
        }
    }
}
