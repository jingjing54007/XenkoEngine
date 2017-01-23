using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Presentation.Quantum
{
    /// <summary>
    /// An interface representing an view model that can provide properties to build an <see cref="GraphViewModel"/>.
    /// </summary>
    public interface IPropertiesProviderViewModel
    {
        /// <summary>
        /// Gets whether this view model is currently able to provide properties.
        /// </summary>
        bool CanProvidePropertiesViewModel { get; }

        /// <summary>
        /// Retrieves the root <see cref="IContentNode"/> to use to generate properties.
        /// </summary>
        /// <returns>The root <see cref="IContentNode"/> to use to generate properties.</returns>
        IContentNode GetRootNode();

        /// <summary>
        /// Indicates whether the members of the given reference should be constructed for the view model.
        /// </summary>
        /// <param name="member">The member content containing the reference.</param>
        /// <param name="reference">The reference to a <see cref="GraphNode"/> contained in a parent node.</param>
        /// <returns><see cref="ExpandReferencePolicy.Full"/> or <see cref="ExpandReferencePolicy.Partial"/> if respectively all or some of the members of the referenced node should be constructed, <see cref="ExpandReferencePolicy.None"/> otherwise.</returns>
        // TODO: in some case of "boxing" the reference can actually be contained in an ObjectContent. Might need to update the signature of this method for proper support
        ExpandReferencePolicy ShouldExpandReference(MemberContent member, ObjectReference reference);

        /// <summary>
        /// Indicates whether the member corresponding to the given content should be constructed for the view model.
        /// </summary>
        /// <param name="content">The content of the member to evaluate.</param>
        /// <param name="expandReferencePolicy">The result from previous call to <see cref="ShouldExpandReference"/>.</param>
        /// <returns><c>True</c> if the member node should be constructed, <c>False</c> otherwise.</returns>
        bool ShouldConstructMember(MemberContent content, ExpandReferencePolicy expandReferencePolicy);
    }
}
