﻿using System;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;
using SiliconStudio.Core.Yaml.Events;
using SiliconStudio.Core.Yaml.Serialization;
using SiliconStudio.Core.Yaml.Serialization.Serializers;

namespace SiliconStudio.Assets.Serializers
{
    /// <summary>
    /// Serializer that works with scalar, but could still read older ObjectSerializer format.
    /// </summary>
    public abstract class ScalarOrObjectSerializer : IYamlSerializableFactory, IYamlSerializable, IDataCustomVisitor
    {
        private static readonly ObjectSerializer ObjectSerializer = new ObjectSerializer();
        private readonly YamlRedirectSerializer scalarRedirectSerializer;

        protected ScalarOrObjectSerializer()
        {
            scalarRedirectSerializer = new YamlRedirectSerializer(this);
        }

        public IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return CanVisit(typeDescriptor.Type) ? this : null;
        }

        public abstract bool CanVisit(Type type);

        public void Visit(ref VisitorContext context)
        {
            // For a scalar object, we don't visit its members
            // But we do still visit the instance (either struct or class)
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }

        public object ReadYaml(ref ObjectContext objectContext)
        {
            if (!objectContext.Reader.Accept<Scalar>())
            {
                // Old format, fallback to ObjectSerializer
                return ObjectSerializer.ReadYaml(ref objectContext);
            }

            // If it's a scalar (new format), redirect to our scalar serializer
            return scalarRedirectSerializer.ReadYaml(ref objectContext);
        }

        public void WriteYaml(ref ObjectContext objectContext)
        {
            // Always write in the new format
            scalarRedirectSerializer.WriteYaml(ref objectContext);
        }

        public abstract object ConvertFrom(ref ObjectContext context, Scalar fromScalar);

        public abstract string ConvertTo(ref ObjectContext objectContext);

        protected virtual void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
        {
            // Emit the scalar
            objectContext.SerializerContext.Writer.Emit(scalar);
        }

        internal class YamlRedirectSerializer : AssetScalarSerializerBase
        {
            private readonly ScalarOrObjectSerializer realScalarSerializer;

            public YamlRedirectSerializer(ScalarOrObjectSerializer realScalarSerializer)
            {
                this.realScalarSerializer = realScalarSerializer;
            }

            public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
            {
                return realScalarSerializer.ConvertFrom(ref context, fromScalar);
            }

            public override string ConvertTo(ref ObjectContext objectContext)
            {
                return realScalarSerializer.ConvertTo(ref objectContext);
            }

            protected override void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
            {
                realScalarSerializer.WriteScalar(ref objectContext, scalar);
            }

            public override bool CanVisit(Type type)
            {
                return realScalarSerializer.CanVisit(type);
            }
        }
    }
}