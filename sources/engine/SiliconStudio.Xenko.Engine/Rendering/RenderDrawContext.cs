﻿using System;
using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Rendering context used during <see cref="IGraphicsRenderer.Draw"/>.
    /// </summary>
    public sealed class RenderDrawContext : ComponentBase
    {
        // States
        private int currentStateIndex = -1;
        private readonly List<StateAndTargets> allocatedStates = new List<StateAndTargets>(10);

        private readonly Dictionary<Type, DrawEffect> sharedEffects = new Dictionary<Type, DrawEffect>();

        public RenderDrawContext(IServiceRegistry services, RenderContext renderContext, GraphicsContext graphicsContext)
        {
            if (services == null) throw new ArgumentNullException("services");

            RenderContext = renderContext;
            ResourceGroupAllocator = graphicsContext.ResourceGroupAllocator;
            GraphicsDevice = RenderContext.GraphicsDevice;
            GraphicsContext = graphicsContext;
            CommandList = graphicsContext.CommandList;
            Resolver = new ResourceResolver(this);
        }

        /// <summary>
        /// Gets the render context.
        /// </summary>
        public RenderContext RenderContext { get; }

        /// <summary>
        /// Gets the <see cref="ResourceGroup"/> allocator.
        /// </summary>
        public ResourceGroupAllocator ResourceGroupAllocator { get; }

        /// <summary>
        /// Gets the command list.
        /// </summary>
        public CommandList CommandList { get; private set; }

        public GraphicsContext GraphicsContext { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public ResourceResolver Resolver { get; private set; }

        /// <summary>
        /// Locks the command list until <see cref="IDisposable.Dispose()"/> is called on the returned value type.
        /// </summary>
        /// <returns></returns>
        /// This is necessary only during Collect(), Extract() and Prepare() phases, not during Draw().
        /// Some graphics API might not require actual locking, in which case this object might do nothing.
        public DefaultCommandListLock LockCommandList()
        {
            // TODO: Temporary, for now we use the CommandList itself as a lock
            return new DefaultCommandListLock(CommandList);
        }

        /// <summary>
        /// Pushes render targets and viewport state.
        /// </summary>
        public void PushRenderTargets()
        {
            // Check if we need to allocate a new StateAndTargets
            StateAndTargets newState;
            currentStateIndex++;
            if (currentStateIndex == allocatedStates.Count)
            {
                newState = new StateAndTargets();
                allocatedStates.Add(newState);
            }
            else
            {
                newState = allocatedStates[currentStateIndex];
            }
            newState.Capture(CommandList);
        }

        /// <summary>
        /// Restores render targets and viewport state.
        /// </summary>
        public void PopRenderTargets()
        {
            if (currentStateIndex < 0)
            {
                throw new InvalidOperationException("Cannot pop more than push");
            }

            var oldState = allocatedStates[currentStateIndex--];
            oldState.Restore(CommandList);
        }

        /// <summary>
        /// Gets or creates a shared effect.
        /// </summary>
        /// <typeparam name="T">Type of the shared effect (mush have a constructor taking a <see cref="Rendering.RenderContext"/></typeparam>
        /// <returns>A singleton instance of <typeparamref name="T"/></returns>
        public T GetSharedEffect<T>() where T : DrawEffect, new()
        {
            // TODO: Add a way to support custom constructor
            lock (sharedEffects)
            {
                DrawEffect effect;
                if (!sharedEffects.TryGetValue(typeof(T), out effect))
                {
                    effect = new T();
                    sharedEffects.Add(typeof(T), effect);
                    effect.Initialize(RenderContext);
                }

                return (T)effect;
            }
        }

        /// <summary>
        /// Holds current viewports and render targets.
        /// </summary>
        private class StateAndTargets
        {
            private const int MaxRenderTargetCount = 8;
            private const int MaxViewportAndScissorRectangleCount = 16;

            public int RenderTargetCount;
            public int ViewportCount;

            public readonly Viewport[] Viewports = new Viewport[MaxViewportAndScissorRectangleCount];
            public readonly Texture[] RenderTargets = new Texture[MaxRenderTargetCount];
            public Texture DepthStencilBuffer;

            public void Capture(CommandList commandList)
            {
                RenderTargetCount = commandList.RenderTargetCount;
                ViewportCount = commandList.ViewportCount;

                DepthStencilBuffer = commandList.DepthStencilBuffer;
                
                // TODO: Backup scissor rectangles and restore them
                
                for (int i = 0; i < RenderTargetCount; i++)
                {
                    RenderTargets[i] = commandList.RenderTargets[i];
                }

                for (int i = 0; i < ViewportCount; i++)
                {
                    Viewports[i] = commandList.Viewports[i];
                }
            }

            public void Restore(CommandList commandList)
            {
                commandList.SetRenderTargets(DepthStencilBuffer, RenderTargetCount, RenderTargets);
                commandList.SetViewports(ViewportCount, Viewports);
            }
        }
    }
}
