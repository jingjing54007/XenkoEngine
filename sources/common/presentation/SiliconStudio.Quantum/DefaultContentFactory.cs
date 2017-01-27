// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// This class is an implementation of the <see cref="IContentFactory"/> interface that can construct <see cref="ObjectContent"/>, <see cref="BoxedContent"/>
    /// and <see cref="MemberContent"/> instances.
    /// </summary>
    public class DefaultContentFactory : IContentFactory
    {
        /// <inheritdoc/>
        public virtual IContentNode CreateObjectContent(INodeBuilder nodeBuilder, Guid guid, object obj, ITypeDescriptor descriptor, bool isPrimitive)
        {
            var reference = nodeBuilder.CreateReferenceForNode(descriptor.Type, obj) as ReferenceEnumerable;
            return new ObjectContent(obj, guid, descriptor, isPrimitive, reference);
        }

        /// <inheritdoc/>
        public virtual IContentNode CreateBoxedContent(INodeBuilder nodeBuilder, Guid guid, object structure, ITypeDescriptor descriptor, bool isPrimitive)
        {
            return new BoxedContent(structure, guid, descriptor, isPrimitive);
        }

        /// <inheritdoc/>
        public virtual IContentNode CreateMemberContent(INodeBuilder nodeBuilder, Guid guid, IObjectNode parent, IMemberDescriptor member, bool isPrimitive, object value)
        {
            var reference = nodeBuilder.CreateReferenceForNode(member.Type, value);
            return new MemberContent(nodeBuilder, guid, parent, member, isPrimitive, reference);
        }
    }
}
