using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Core.Storage;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Instantiation of an Effect for a given <see cref="StaticEffectObjectNodeReference"/>.
    /// </summary>
    public class RenderEffect
    {
        // Request effect selector
        public readonly EffectSelector EffectSelector;

        public int LastFrameUsed { get; private set; }

        /// <summary>
        /// Describes what state the effect is in (compiling, error, etc..)
        /// </summary>
        public RenderEffectState State;

        /// <summary>
        /// Describes when to try again after a previous error (UTC).
        /// </summary>
        public DateTime RetryTime = DateTime.MaxValue;

        public bool IsReflectionUpdateRequired;

        public Effect Effect;
        public RenderEffectReflection Reflection;

        /// <summary>
        /// Compiled pipeline state.
        /// </summary>
        public PipelineState PipelineState;

        /// <summary>
        /// Validates if effect needs to be compiled or recompiled.
        /// </summary>
        public EffectValidator EffectValidator;

        /// <summary>
        /// Pending effect being compiled.
        /// </summary>
        public Task<Effect> PendingEffect;

        public EffectParameterUpdater FallbackParameterUpdater;
        public ParameterCollection FallbackParameters;

        public RenderEffect(EffectSelector effectSelector)
        {
            EffectSelector = effectSelector;
            EffectValidator.Initialize();
        }

        /// <summary>
        /// Mark effect as used during this frame.
        /// </summary>
        /// <returns>True if state changed (object was not mark as used during this frame until now), otherwise false.</returns>
        public bool MarkAsUsed(RenderSystem renderSystem)
        {
            if (LastFrameUsed == renderSystem.FrameCounter)
                return false;

            LastFrameUsed = renderSystem.FrameCounter;
            return true;
        }

        public bool IsUsedDuringThisFrame(RenderSystem renderSystem)
        {
            return LastFrameUsed == renderSystem.FrameCounter;
        }

        public void ClearFallbackParameters()
        {
            FallbackParameterUpdater = default(EffectParameterUpdater);
            FallbackParameters = null;
        }
    }


    /// <summary>
    /// Describes an effect as used by a <see cref="RenderNode"/>.
    /// </summary>
    public class RenderEffectReflection
    {
        public static readonly RenderEffectReflection Empty = new RenderEffectReflection();

        public RootSignature RootSignature;

        public FrameResourceGroupLayout PerFrameLayout;
        public ViewResourceGroupLayout PerViewLayout;
        public RenderSystemResourceGroupLayout PerDrawLayout;

        // PerFrame
        public ResourceGroup PerFrameResources;

        public ResourceGroupBufferUploader BufferUploader;

        public EffectDescriptorSetReflection DescriptorReflection;
        public ResourceGroupDescription[] ResourceGroupDescriptions;

        // Used only for fallback effect
        public EffectParameterUpdaterLayout FallbackUpdaterLayout;
        public int[] FallbackResourceGroupMapping;
    }
}