﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum.Commands;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// The default <see cref="INodeBuilder"/> implementation that construct a model from a data object.
    /// </summary>
    internal class DefaultNodeBuilder : DataVisitorBase, INodeBuilder
    {
        private readonly Stack<IInitializingGraphNode> contextStack = new Stack<IInitializingGraphNode>();
        private readonly HashSet<IContentNode> referenceContents = new HashSet<IContentNode>();
        private static readonly Type[] InternalPrimitiveTypes = { typeof(decimal), typeof(string), typeof(Guid) };
        private IInitializingObjectNode rootNode;
        private Guid rootGuid;

        public DefaultNodeBuilder(NodeContainer nodeContainer)
        {
            NodeContainer = nodeContainer;
            primitiveTypes.AddRange(InternalPrimitiveTypes);
        }

        /// <inheritdoc/>
        public NodeContainer NodeContainer { get; }
        
        /// <inheritdoc/>
        private readonly List<Type> primitiveTypes = new List<Type>();

        /// <inheritdoc/>
        public ICollection<INodeCommand> AvailableCommands { get; } = new List<INodeCommand>();

        /// <inheritdoc/>
        public IContentFactory ContentFactory { get; set; } = new DefaultContentFactory();

        public bool DiscardUnbrowsable { get; set; } = true;

        /// <summary>
        /// Reset the visitor in order to use it to generate another model.
        /// </summary>
        public override void Reset()
        {
            rootNode = null;
            rootGuid = Guid.Empty;
            contextStack.Clear();
            referenceContents.Clear();
            base.Reset();
        }

        public void RegisterPrimitiveType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || primitiveTypes.Contains(type))
                return;

            primitiveTypes.Add(type);
        }

        public void UnregisterPrimitiveType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || InternalPrimitiveTypes.Contains(type))
                throw new InvalidOperationException("The given type cannot be unregistered from the list of primitive types");

            primitiveTypes.Remove(type);
        }

        public bool IsPrimitiveType(Type type)
        {
            if (type == null)
                return false;

            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            return type.IsPrimitive || type.IsEnum || primitiveTypes.Any(x => x.IsAssignableFrom(type));
        }

        /// <inheritdoc/>
        public IObjectNode Build(object obj, Guid guid)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Reset();
            rootGuid = guid;
            var typeDescriptor = TypeDescriptorFactory.Find(obj.GetType());
            VisitObject(obj, typeDescriptor as ObjectDescriptor, true);
            return rootNode;
        }

        /// <inheritdoc/>
        public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
        {
            ITypeDescriptor currentDescriptor = descriptor;

            bool isRootNode = contextStack.Count == 0;
            if (isRootNode)
            {
                // If we're visiting a value type as "object" we need to use a special "boxed" node.
                var content = descriptor.Type.IsValueType ? ContentFactory.CreateBoxedContent(this, rootGuid, obj, descriptor, IsPrimitiveType(descriptor.Type))
                    : ContentFactory.CreateObjectContent(this, rootGuid, obj, descriptor, IsPrimitiveType(descriptor.Type));

                currentDescriptor = content.Descriptor;
                rootNode = (IInitializingObjectNode)content;
                if (content.IsReference && currentDescriptor.Type.IsStruct())
                    throw new QuantumConsistencyException("A collection type", "A structure type", rootNode);

                if (content.IsReference)
                    referenceContents.Add(content);

                AvailableCommands.Where(x => x.CanAttach(currentDescriptor, null)).ForEach(rootNode.AddCommand);

                if (obj == null)
                {
                    rootNode.Seal();
                    return;
                }
                PushContextNode(rootNode);
            }

            if (!IsPrimitiveType(currentDescriptor.Type))
            {
                base.VisitObject(obj, descriptor, true);
            }

            if (isRootNode)
            {
                PopContextNode();
                rootNode.Seal();
            }
        }

        /// <inheritdoc/>
        public override void VisitCollection(IEnumerable collection, CollectionDescriptor descriptor)
        {
            if (!descriptor.HasIndexerAccessors)
                throw new NotSupportedException("Collections that do not have indexer accessors are not supported in Quantum.");

            // Don't visit items unless they are primitive or enumerable (collections within collections)
            if (IsCollection(descriptor.ElementType))
            {
                base.VisitCollection(collection, descriptor);
            }
        }

        /// <inheritdoc/>
        public override void VisitDictionary(object dictionary, DictionaryDescriptor descriptor)
        {
            if (!IsPrimitiveType(descriptor.KeyType))
                throw new InvalidOperationException("The type of dictionary key must be a primary type.");

            // Don't visit items unless they are primitive or enumerable (collections within collections)
            if (IsCollection(descriptor.ValueType))
            {
                base.VisitDictionary(dictionary, descriptor);
            }
        }

        /// <inheritdoc/>
        public override void VisitObjectMember(object container, ObjectDescriptor containerDescriptor, IMemberDescriptor member, object value)
        {
            // If this member should contains a reference, create it now.
            var containerNode = (IInitializingObjectNode)GetContextNode();
            var guid = Guid.NewGuid();
            var content = (MemberContent)ContentFactory.CreateMemberContent(this, guid, containerNode, member, IsPrimitiveType(member.Type), value);
            containerNode.AddMember(content);

            if (content.IsReference)
                referenceContents.Add(content);

            PushContextNode(content);
            if (content.TargetReference == null)
            {
                // For enumerable references, we visit the member to allow VisitCollection or VisitDictionary to enrich correctly the node.
                Visit(content.Value);
            }
            PopContextNode();

            AvailableCommands.Where(x => x.CanAttach(content.Descriptor, (MemberDescriptorBase)member)).ForEach(content.AddCommand);

            content.Seal();
        }

        public IReference CreateReferenceForNode(Type type, object value)
        {
            // We don't create references for primitive types
            if (IsPrimitiveType(type))
                return null;

            // At this point it is either a struct, a reference type or a collection
            var descriptor = TypeDescriptorFactory.Find(value?.GetType());
            var valueType = GetElementValueType(descriptor);

            // We don't create references for collection of primitive types
            if (IsPrimitiveType(valueType))
                return null;

            // In any other case, we create a reference
            return Reference.CreateReference(value, type, Index.Empty);
        }

        private void PushContextNode(IInitializingGraphNode node)
        {
            contextStack.Push(node);
        }

        private void PopContextNode()
        {
            contextStack.Pop();
        }

        private IInitializingGraphNode GetContextNode()
        {
            return contextStack.Peek();
        }

        private static bool IsCollection(Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type);
        }

        private static Type GetElementValueType(ITypeDescriptor descriptor)
        {
            var dictionaryDescriptor = descriptor as DictionaryDescriptor;
            var collectionDescriptor = descriptor as CollectionDescriptor;
            return dictionaryDescriptor != null ? dictionaryDescriptor.ValueType : collectionDescriptor?.ElementType;
        }
    }
}
