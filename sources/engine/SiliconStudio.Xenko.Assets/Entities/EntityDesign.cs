using System.ComponentModel;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Assets.Entities
{
    /// <summary>
    /// Associate an <see cref="Entity"/> with design-time data.
    /// </summary>
    [DataContract("EntityDesign")]
    public class EntityDesign : IAssetPartDesign<Entity>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        public EntityDesign()
            : this(null, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        /// <param name="entity">The entity contained in this instance.</param>
        public EntityDesign(Entity entity)
            : this(entity, string.Empty)
        {
            Entity = entity;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        /// <param name="entity">The entity contained in this instance.</param>
        /// <param name="folder">The folder in which this entity is contained.</param>
        public EntityDesign(Entity entity, string folder)
        {
            Entity = entity;
            Folder = folder;
        }

        /// <summary>
        /// The folder where the entity is attached (folder is relative to parent folder). If null or empty, the entity doesn't belong to a folder.
        /// </summary>
        [DataMember(10)]
        [DefaultValue("")]
        public string Folder { get; set; }

        /// <summary>
        /// The entity.
        /// </summary>
        [DataMember(10)]
        public Entity Entity { get; set; }

        /// <inheritdoc/>
        [DataMember(20)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        /// <inheritdoc/>
        Entity IAssetPartDesign<Entity>.Part { get { return Entity; } set { Entity = value; } }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"EntityDesign [{Entity.Name}]";
        }
    }
}
