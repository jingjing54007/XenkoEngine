﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// A class describing the path of a node, relative to a root node. The path can cross references, array, etc.
    /// </summary>
    /// <remarks>This class is immutable.</remarks>
    public class GraphNodePath : IEnumerable<IContentNode>, IEquatable<GraphNodePath>
    {
        /// <summary>
        /// An enum that describes the type of an item of a model node path.
        /// </summary>
        public enum ElementType
        {
            /// <summary>
            /// This item is a member (child) of the previous node
            /// </summary>
            Member,
            /// <summary>
            /// This item is the target of the object reference of the previous node.
            /// </summary>
            Target,
            /// <summary>
            /// This item is the target of a enumerable reference of the previous node corresponding to the associated index.
            /// </summary>
            Index,
        }

        /// <summary>
        /// A structure that represents an element of the path.
        /// </summary>
        public struct NodePathElement : IEquatable<NodePathElement>
        {
            public readonly ElementType Type;
            public readonly object Value;

            private NodePathElement(object value, ElementType type)
            {
                Value = value;
                Type = type;
            }

            public static NodePathElement CreateMember([NotNull] string name)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                return new NodePathElement(name, ElementType.Member);
            }

            public static NodePathElement CreateTarget()
            {
                // We use a guid to allow equality test to fail between two different instances returned by CreateTarget
                return new NodePathElement(Guid.NewGuid(), ElementType.Target);
            }

            public static NodePathElement CreateIndex(Index index)
            {
                return new NodePathElement(index, ElementType.Index);
            }

            public bool EqualsInPath(NodePathElement other)
            {
                return Type == ElementType.Target && other.Type == ElementType.Target || Equals(other);
            }

            public int GetHashCodeInPath()
            {
                unchecked
                {
                    return ((int)Type * 397) ^ (Type != ElementType.Target ? Value?.GetHashCode() ?? 0 : 0);
                }
            }

            public bool Equals(NodePathElement other)
            {
                return Type == other.Type && Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is NodePathElement && Equals((NodePathElement)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Type*397) ^ (Value?.GetHashCode() ?? 0);
                }
            }

            public static bool operator ==(NodePathElement left, NodePathElement right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NodePathElement left, NodePathElement right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                switch (Type)
                {
                    case ElementType.Member:
                        return $".{Value}";
                    case ElementType.Target:
                        return "-> (Target)";
                    case ElementType.Index:
                        return $"[{Value}]";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// An enumerator for <see cref="GraphNodePath"/>
        /// </summary>
        private class GraphNodePathEnumerator : IEnumerator<IContentNode>
        {
            private readonly GraphNodePath path;
            private int index = -1;

            public GraphNodePathEnumerator(GraphNodePath path)
            {
                if (!path.IsValid) throw new InvalidOperationException("The node path is invalid.");
                this.path = path;
            }

            public void Dispose()
            {
                index = -1;
            }

            public bool MoveNext()
            {
                if (index == path.path.Count)
                    return false;

                if (index == -1)
                {
                    Current = path.RootNode;
                }
                else
                {
                    var element = path.path[index];
                    switch (element.Type)
                    {
                        case ElementType.Member:
                            Current = ((IObjectNode)Current).Members.Single(x => string.Equals(x.Name, element.Value));
                            break;
                        case ElementType.Target:
                            Current = Current.TargetReference.TargetNode;
                            break;
                        case ElementType.Index:
                            Current = Current.ItemReferences[(Index)element.Value].TargetNode;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                ++index;

                // If a node that is not the last one is null, we cannot process the path.
                if (Current == null && index < path.path.Count)
                    throw new InvalidOperationException("A node of the path is null but is not the last node.");

                return true;
            }

            public void Reset()
            {
                index = -1;
            }

            public IContentNode Current { get; private set; }

            object IEnumerator.Current => Current;
        }

        private const int DefaultCapacity = 16;
        private readonly List<NodePathElement> path;

        private GraphNodePath(IContentNode rootNode, bool isEmpty, int defaultCapacity)
        {
            RootNode = rootNode;
            IsEmpty = isEmpty;
            path = new List<NodePathElement>(defaultCapacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodePath"/> with the given root node.
        /// </summary>
        /// <param name="rootNode">The root node to represent with this instance of <see cref="GraphNodePath"/>.</param>
        public GraphNodePath(IContentNode rootNode)
            : this(rootNode, true, DefaultCapacity)
        {
        }

        /// <summary>
        /// Gets the root node of this path.
        /// </summary>
        public IContentNode RootNode { get; }

        /// <summary>
        /// Gets whether this path is a valid path.
        /// </summary>
        public bool IsValid => path.Count > 0 || IsEmpty;

        /// <summary>
        /// Gets whether this path is empty.
        /// </summary>
        /// <remarks>An empty path resolves to <see cref="RootNode"/>.</remarks>
        public bool IsEmpty { get; }

        /// <summary>
        /// Gets the number of items in this path.
        /// </summary>
        public int Count => path.Count;

        /// <summary>
        /// Gets the items composing this path.
        /// </summary>
        public IReadOnlyList<NodePathElement> Path => path;

        /// <inheritdoc/>
        public IEnumerator<IContentNode> GetEnumerator()
        {
            return new GraphNodePathEnumerator(this);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(GraphNodePath other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!Equals(RootNode, other.RootNode) || IsEmpty != other.IsEmpty || path.Count != other.path.Count)
                return false;

            for (var i = 0; i < path.Count; ++i)
            {
                if (!path[i].EqualsInPath(other.path[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((GraphNodePath)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RootNode?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ IsEmpty.GetHashCode();
                foreach (var item in path)
                {
                    hashCode = (hashCode * 397) ^ item.GetHashCodeInPath();
                }
                return hashCode;
            }
        }

        public static bool operator ==(GraphNodePath left, GraphNodePath right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GraphNodePath left, GraphNodePath right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return IsValid ? "(root)" + path.Select(x => x.ToString()).Aggregate((current, next) => current + next) : "(invalid)";
        }

        /// <summary>
        /// Retrieve the node targeted by this path.
        /// </summary>
        /// <returns></returns>
        [Pure, NotNull]
        public IContentNode GetNode() => this.Last();

        /// <summary>
        /// Retrieve the parent path.
        /// </summary>
        /// <returns>A new <see cref="GraphNodePath"/> instance representing the parent path.</returns>
        [Pure]
        public GraphNodePath GetParent()
        {
            if (IsEmpty)
                return null;

            var result = new GraphNodePath(RootNode, path.Count == 1, path.Count - 1);
            for (var i = 0; i < path.Count - 1; ++i)
                result.path.Add(path[i]);
            return result;
        }

        /// <summary>
        /// Clones this instance of <see cref="GraphNodePath"/> and remap the new instance to a new root node.
        /// </summary>
        /// <param name="newRoot">The root node for the cloned path.</param>
        /// <returns>A copy of this path with the given node as root node.</returns>
        [Pure, NotNull]
        public GraphNodePath Clone(IContentNode newRoot) => Clone(newRoot, IsEmpty);

        /// <summary>
        /// Clones this instance of <see cref="GraphNodePath"/>.
        /// </summary>
        /// <returns>A copy of this path with the same root node.</returns>
        [Pure, NotNull]
        public GraphNodePath Clone() => Clone(RootNode, IsEmpty);

        // TODO: re-implement each of the method below in an optimized way.

        /// <summary>
        /// Creates a new <see cref="GraphNodePath"/> instance accessing a member node of the node represented by this path.
        /// </summary>
        /// <param name="memberName">The name of the member node.</param>
        /// <returns>A new <see cref="GraphNodePath"/> instance accessing a member node of the node targeted by this path.</returns>
        [Pure]
        public GraphNodePath PushMember(string memberName) => PushElement(memberName, ElementType.Member);

        /// <summary>
        /// Creates a new <see cref="GraphNodePath"/> instance accessing the target of the reference contained in the node represented by this path.
        /// </summary>
        /// <returns>A new <see cref="GraphNodePath"/> instance accessing the target of the reference contained in the node represented by this path.</returns>
        [Pure, NotNull]
        public GraphNodePath PushTarget() => PushElement(null, ElementType.Target);

        /// <summary>
        /// Creates a new <see cref="GraphNodePath"/> instance accessing the target at a given index of the enumerable reference contained in the node represented by this path.
        /// </summary>
        /// <param name="index">The index of the target node.</param>
        /// <returns>A new <see cref="GraphNodePath"/> instance accessing the target at a given index of the enumerable reference contained in the node represented by this path.</returns>
        [Pure, NotNull]
        public GraphNodePath PushIndex(Index index) => PushElement(index, ElementType.Index);

        [Pure, NotNull]
        public GraphNodePath PushChildPath(GraphNodePath childPath)
        {
            if (childPath.IsEmpty)
                return Clone();

            var result = Clone(RootNode, false);
            result.path.AddRange(childPath.path);
            return result;
        }

        // TODO: Switch to tuple return as soon as we have C# 7.0
        [NotNull]
        public static GraphNodePath From(IContentNode root, MemberPath memberPath, out Index index)
        {
            var result = new GraphNodePath(root);
            index = Index.Empty;
            var memberPathItems = memberPath.Decompose();
            for (int i = 0; i < memberPathItems.Count; i++)
            {
                var memberPathItem = memberPathItems[i];
                bool lastItem = i == memberPathItems.Count - 1;
                if (memberPathItem.MemberDescriptor != null)
                {
                    result = result.PushMember(memberPathItem.MemberDescriptor.Name);
                }
                else if (memberPathItem.GetIndex() != null)
                {
                    var localIndex = new Index(memberPathItem.GetIndex());

                    if (lastItem)
                    {
                        // If last item, we directly return the index rather than add it to the path
                        index = localIndex;
                    }
                    else
                    {
                        result = result.PushIndex(localIndex);
                    }
                }

                // Don't apply Target on last item
                if (!lastItem)
                {
                    // If this is a reference, add a target element to the path
                    var node = result.GetNode();
                    var objectReference = node.TargetReference;
                    if (objectReference?.TargetNode != null)
                    {
                        result = result.PushTarget();
                    }
                }
            }

            return result;
        }

        [Pure, NotNull]
        public MemberPath ToMemberPath()
        {
            if (!IsValid)
                throw new InvalidOperationException("The node path is invalid.");

            var memberPath = new MemberPath();
            var node = RootNode;
            for (var i = 0; i < path.Count; i++)
            {
                var itemPath = path[i];
                switch (itemPath.Type)
                {
                    case ElementType.Member:
                        var name = (string)itemPath.Value;
                        node = ((IObjectNode)node).Members.Single(x => x.Name == name);
                        memberPath.Push(((MemberContent)node).MemberDescriptor);
                        break;
                    case ElementType.Target:
                        if (i != path.Count - 1)
                        {
                            var objectRefererence = node.TargetReference;
                            node = objectRefererence.TargetNode;
                        }
                        break;
                    case ElementType.Index:
                        var index = (Index)itemPath.Value;
                        var enumerableReference = node.ItemReferences;
                        var descriptor = node.Descriptor;
                        var collectionDescriptor = descriptor as CollectionDescriptor;
                        if (collectionDescriptor != null)
                            memberPath.Push(collectionDescriptor, index.Int);
                        var dictionaryDescriptor = descriptor as DictionaryDescriptor;
                        if (dictionaryDescriptor != null)
                            memberPath.Push(dictionaryDescriptor, index.Value);

                        if (i != path.Count - 1)
                        {
                            var objectRefererence = enumerableReference.Single(x => Equals(x.Index, index));
                            node = objectRefererence.TargetNode;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return memberPath;
        }

        private GraphNodePath Clone(IContentNode newRoot, bool isEmpty)
        {
            var clone = new GraphNodePath(newRoot, isEmpty, Math.Max(path.Count, DefaultCapacity));
            clone.path.AddRange(path);
            return clone;
        }

        private GraphNodePath PushElement(object elementValue, ElementType type)
        {
            var result = Clone(RootNode, false);
            switch (type)
            {
                case ElementType.Member:
                    var name = elementValue as string;
                    if (name == null)
                        throw new ArgumentException("The value must be a non-null string when type is ElementType.Member.");
                    result.path.Add(NodePathElement.CreateMember(name));
                    return result;
                case ElementType.Target:
                    if (elementValue != null)
                        throw new ArgumentException("The value must be null when type is ElementType.Target.");
                    result.path.Add(NodePathElement.CreateTarget());
                    return result;
                case ElementType.Index:
                    if (!(elementValue is Index))
                        throw new ArgumentException("The value must be an Index when type is ElementType.Index.");
                    result.path.Add(NodePathElement.CreateIndex((Index)elementValue));
                    return result;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
