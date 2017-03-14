// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Xenko.Graphics;
using System.Reflection;
using System.Threading;
using SiliconStudio.Core.Threading;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Facility to perform rendering: extract rendering data from scene, determine effects and GPU states, compute and prepare data (i.e. matrices, buffers, etc...) and finally draw it.
    /// </summary>
    public class RenderSystem : ComponentBase
    {
        [Obsolete("This field is provisional and will be replaced by a proper mechanisms in the future")]
        public readonly List<Func<RenderView, RenderObject, bool>> ViewObjectFilters = new List<Func<RenderView, RenderObject, bool>>();

        private readonly ThreadLocal<ExtractThreadLocals> extractThreadLocals = new ThreadLocal<ExtractThreadLocals>(() => new ExtractThreadLocals());
        private readonly ConcurrentPool<PrepareThreadLocals> prepareThreadLocals = new ConcurrentPool<PrepareThreadLocals>(() => new PrepareThreadLocals());
        private CompiledCommandList[] commandLists;
        private Texture[] renderTargets;

        private readonly Dictionary<Type, RootRenderFeature> renderFeaturesByType = new Dictionary<Type, RootRenderFeature>();
        private readonly HashSet<Type> renderObjectsDefaultPipelinePlugins = new HashSet<Type>();
        private IServiceRegistry registry;


        // TODO GRAPHICS REFACTOR should probably be controlled by graphics compositor?
        /// <summary>
        /// List of render stages.
        /// </summary>
        public FastTrackingCollection<RenderStage> RenderStages { get; } = new FastTrackingCollection<RenderStage>();

        /// <summary>
        /// Frame counter, mostly for internal use.
        /// </summary>
        public int FrameCounter { get; private set; } = 1;

        /// <summary>
        /// List of render features
        /// </summary>
        public FastTrackingCollection<RootRenderFeature> RenderFeatures { get; } = new FastTrackingCollection<RootRenderFeature>();

        /// <summary>
        /// The graphics device, used to create graphics resources.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// The effect system, used to compile effects.
        /// </summary>
        public EffectSystem EffectSystem { get; private set; }

        /// <summary>
        /// List of views.
        /// </summary>
        public FastTrackingCollection<RenderView> Views { get; } = new FastTrackingCollection<RenderView>();

        public RenderContext RenderContextOld { get; private set; }

        public event Action RenderStageSelectorsChanged;

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services => registry;

        public PipelinePluginManager PipelinePlugins { get; }

        public RenderSystem()
        {
            PipelinePlugins = new PipelinePluginManager(this);
            RenderStages.CollectionChanged += RenderStages_CollectionChanged;
            RenderFeatures.CollectionChanged += RenderFeatures_CollectionChanged;
        }

        /// <summary>
        /// Performs pipeline initialization, enumerates views and populates visibility groups.
        /// </summary>
        /// <param name="context"></param>
        public void Collect(RenderDrawContext context)
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Collect();
            }
        }

        /// <summary>
        /// Extract data from entities, should be as fast as possible to not block simulation loop. It should be mostly copies, and the actual processing should be part of Prepare().
        /// </summary>
        public void Extract(RenderDrawContext context)
        {
            // Prepare views
            for (int index = 0; index < Views.Count; index++)
            {
                // Update indices
                var view = Views[index];
                view.Index = index;

                // Create missing RenderViewFeature
                while (view.Features.Count < RenderFeatures.Count)
                {
                    view.Features.Add(new RenderViewFeature());
                }

                for (int i = 0; i < RenderFeatures.Count; i++)
                {
                    var renderViewFeature = view.Features[i];
                    renderViewFeature.RootFeature = RenderFeatures[i];
                }
            }

            // Create nodes for objects to render
            Dispatcher.ForEach(Views, view =>
            {
                // Sort per render feature (used for later sorting)
                // We'll be able to process data more efficiently for the next steps
                Dispatcher.Sort(view.RenderObjects, RenderObjectFeatureComparer.Default);

                Dispatcher.ForEach(view.RenderObjects, () => extractThreadLocals.Value, (renderObject, batch) =>
                {
                    var renderFeature = renderObject.RenderFeature;
                    var viewFeature = view.Features[renderFeature.Index];

                    // Create object node
                    renderFeature.GetOrCreateObjectNode(renderObject);

                    // Let's create the view object node
                    var renderViewNode = renderFeature.CreateViewObjectNode(view, renderObject);
                    viewFeature.ViewObjectNodes.Add(renderViewNode, batch.ViewFeatureObjectNodeCache);

                    // Collect object
                    // TODO: Check which stage it belongs to (and skip everything if it doesn't belong to any stage)
                    // TODO: For now, we build list and then copy. Another way would be to count and then fill (might be worse, need to check)
                    var activeRenderStages = renderObject.ActiveRenderStages;
                    foreach (var renderViewStage in view.RenderStages)
                    {
                        // Check if this RenderObject wants to be rendered for this render stage
                        var renderStageIndex = renderViewStage.RenderStage.Index;
                        if (!activeRenderStages[renderStageIndex].Active)
                            continue;

                        var renderNode = renderFeature.CreateRenderNode(renderObject, view, renderViewNode, renderViewStage.RenderStage);

                        // Note: Used mostly during updating
                        viewFeature.RenderNodes.Add(renderNode, batch.ViewFeatureRenderNodeCache);

                        // Note: Used mostly during rendering
                        renderViewStage.RenderNodes.Add(new RenderNodeFeatureReference(renderFeature, renderNode, renderObject), batch.ViewStageRenderNodeCache);
                    }
                }, batch => batch.Flush());

                // Finish collectin of view feature nodes
                foreach (var viewFeature in view.Features)
                {
                    viewFeature.ViewObjectNodes.Close();
                    viewFeature.RenderNodes.Close();
                }

                // Also sort view|stage per render feature
                foreach (var renderViewStage in view.RenderStages)
                {
                    renderViewStage.RenderNodes.Close();

                    Dispatcher.Sort(renderViewStage.RenderNodes, RenderNodeFeatureReferenceComparer.Default);
                }
            });

            // Finish collection of render feature nodes
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.CloseNodeCollectors();
            }

            // Ensure size of data arrays per objects
            PrepareDataArrays();

            // Generate and execute extract jobs
            foreach (var renderFeature in RenderFeatures)
            // We might be able to parallelize too as long as we resepect render feature dependency graph (probably very few dependencies in practice)
            {
                // Divide into task chunks for parallelism
                renderFeature.Extract();
            }

            // Ensure size of all other data arrays
            PrepareDataArrays();
        }

        /// <summary>
        /// Finalizes the render features work and releases temporary resources if necessary.
        /// </summary>
        /// <param name="context"></param>
        public void Flush(RenderDrawContext context)
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Flush(context);
            }
        }

        /// <summary>
        /// Performs most of the work (computation and resource preparation). Later game simulation might be running during that step.
        /// </summary>
        /// <param name="context"></param>
        public unsafe void Prepare(RenderDrawContext context)
        {
            // Sync point: after extract, before prepare (game simulation could resume now)

            // Generate and execute prepare effect jobs
            foreach (var renderFeature in RenderFeatures)
            // We might be able to parallelize too as long as we resepect render feature dependency graph (probably very few dependencies in practice)
            {
                // Divide into task chunks for parallelism
                renderFeature.PrepareEffectPermutations(context);
            }

            // Generate and execute prepare jobs
            foreach (var renderFeature in RenderFeatures)
            // We might be able to parallelize too as long as we resepect render feature dependency graph (probably very few dependencies in practice)
            {
                // Divide into task chunks for parallelism
                renderFeature.Prepare(context);
            }

            // Sort
            Dispatcher.ForEach(Views, view =>
            {
                Dispatcher.For(0, view.RenderStages.Count, () => prepareThreadLocals.Acquire(), (index, local) =>
                {
                    var renderViewStage = view.RenderStages[index];

                    var renderNodes = renderViewStage.RenderNodes;
                    if (renderNodes.Count == 0)
                        return;

                    var renderStage = renderViewStage.RenderStage;

                    // Allocate sorted render nodes
                    if (renderViewStage.SortedRenderNodes == null || renderViewStage.SortedRenderNodes.Length < renderNodes.Count)
                        Array.Resize(ref renderViewStage.SortedRenderNodes, renderNodes.Count);
                    var sortedRenderNodes = renderViewStage.SortedRenderNodes;

                    if (renderStage.SortMode != null)
                    {
                        // Make sure sortKeys is big enough
                        if (local.SortKeys == null || local.SortKeys.Length < renderNodes.Count)
                            Array.Resize(ref local.SortKeys, renderNodes.Count);

                        // renderNodes[start..end] belongs to the same render feature
                        fixed (SortKey* sortKeysPtr = local.SortKeys)
                            renderStage.SortMode.GenerateSortKey(view, renderViewStage, sortKeysPtr);

                        Dispatcher.Sort(local.SortKeys, 0, renderNodes.Count, Comparer<SortKey>.Default);

                        // Reorder list
                        for (int i = 0; i < renderNodes.Count; ++i)
                        {
                            sortedRenderNodes[i] = renderNodes[local.SortKeys[i].Index];
                        }
                    }
                    else
                    {
                        // No sorting, copy as is
                        for (int i = 0; i < renderNodes.Count; ++i)
                        {
                            sortedRenderNodes[i] = renderNodes[i];
                        }
                    }
                }, state => prepareThreadLocals.Release(state));
            });

            // Flush the resources uploaded during Prepare
            context.ResourceGroupAllocator.Flush();
            context.RenderContext.Flush();
        }

        public void Draw(RenderDrawContext renderDrawContext, RenderView renderView, RenderStage renderStage)
        {
            // Sync point: draw (from now, we should execute with a graphics device context to perform rendering)

            // Look for the RenderViewStage corresponding to this RenderView | RenderStage combination
            RenderViewStage renderViewStage = null;
            foreach (var currentRenderViewStage in renderView.RenderStages)
            {
                if (currentRenderViewStage.RenderStage == renderStage)
                {
                    renderViewStage = currentRenderViewStage;
                    break;
                }
            }

            if (renderViewStage == null)
            {
                throw new InvalidOperationException("Requested RenderView|RenderStage combination doesn't exist. Please add it to RenderView.RenderStages.");
            }

            // Generate and execute draw jobs
            var renderNodes = renderViewStage.SortedRenderNodes;
            var renderNodeCount = renderViewStage.RenderNodes.Count;

            if (renderNodeCount == 0)
                return;

            if (!GraphicsDevice.IsDeferred)
            {
                int currentStart, currentEnd;
                for (currentStart = 0; currentStart < renderNodeCount; currentStart = currentEnd)
                {
                    var currentRenderFeature = renderNodes[currentStart].RootRenderFeature;
                    currentEnd = currentStart + 1;
                    while (currentEnd < renderNodeCount && renderNodes[currentEnd].RootRenderFeature == currentRenderFeature)
                    {
                        currentEnd++;
                    }

                    // Divide into task chunks for parallelism
                    currentRenderFeature.Draw(renderDrawContext, renderView, renderViewStage, currentStart, currentEnd);
                }
            }
            else
            {
                // Create at most one batch per processor
                int batchCount = Math.Min(Environment.ProcessorCount, renderNodeCount);
                int batchSize = (renderNodeCount + (batchCount - 1)) / batchCount;
                batchCount = (renderNodeCount + (batchSize - 1)) / batchSize;

                // Remember state
                var depthStencilBuffer = renderDrawContext.CommandList.DepthStencilBuffer;
                int renderTargetCount = renderDrawContext.CommandList.RenderTargetCount;
                if (renderTargets == null)
                    renderTargets = new Texture[renderDrawContext.CommandList.RenderTargets.Length];
                for (int i = 0; i < renderTargetCount; ++i)
                    renderTargets[i] = renderDrawContext.CommandList.RenderTargets[i];
                var viewport = renderDrawContext.CommandList.Viewport;
                var scissor = renderDrawContext.CommandList.Scissor;

                // Collect one command list per batch and the main one up to this point
                if (commandLists == null || commandLists.Length < batchCount + 1)
                {
                    Array.Resize(ref commandLists, batchCount + 1);
                }
                commandLists[0] = renderDrawContext.CommandList.Close();

                Dispatcher.For(0, batchCount, () => renderDrawContext.RenderContext.GetThreadContext(), (batchIndex, threadContext) =>
                {
                    threadContext.CommandList.Reset();
                    threadContext.CommandList.ClearState();

                    // Transfer state to all command lists
                    threadContext.CommandList.SetRenderTargets(depthStencilBuffer, renderTargetCount, renderTargets);
                    threadContext.CommandList.SetViewport(viewport);
                    threadContext.CommandList.SetScissorRectangle(scissor);

                    var currentStart = batchSize * batchIndex;
                    int currentEnd;

                    var endExclusive = Math.Min(renderNodeCount, currentStart + batchSize);

                    if (endExclusive <= currentStart)
                        return;

                    for (; currentStart < endExclusive; currentStart = currentEnd)
                    {
                        var currentRenderFeature = renderNodes[currentStart].RootRenderFeature;
                        currentEnd = currentStart + 1;
                        while (currentEnd < endExclusive && renderNodes[currentEnd].RootRenderFeature == currentRenderFeature)
                        {
                            currentEnd++;
                        }

                        // Divide into task chunks for parallelism
                        currentRenderFeature.Draw(threadContext, renderView, renderViewStage, currentStart, currentEnd);
                    }

                    commandLists[batchIndex + 1] = threadContext.CommandList.Close();
                });

                GraphicsDevice.ExecuteCommandLists(batchCount + 1, commandLists);

                renderDrawContext.CommandList.Reset();
                renderDrawContext.CommandList.ClearState();

                // Reapply previous state
                renderDrawContext.CommandList.SetRenderTargets(depthStencilBuffer, renderTargetCount, renderTargets);
                renderDrawContext.CommandList.SetViewport(viewport);
                renderDrawContext.CommandList.SetScissorRectangle(scissor);
            }
        }

        /// <summary>
        /// Initializes the render system.
        /// </summary>
        /// <param name="effectSystem">The effect system.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        public void Initialize(RenderContext context)
        {
            registry = context.Services;

            // Get graphics device service
            var graphicsDeviceService = Services.GetSafeServiceAs<IGraphicsDeviceService>();

            EffectSystem = Services.GetSafeServiceAs<EffectSystem>();

            // Be notified when a RenderObject is added or removed
            Views.CollectionChanged += Views_CollectionChanged;

            GraphicsDevice = graphicsDeviceService.GraphicsDevice;
            RenderContextOld = context;

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Initialize(RenderContextOld);
            }
        }

        protected override void Destroy()
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Dispose();
            }

            base.Destroy();
        }

        /// <summary>
        /// Reset render objects and features. Should be called at beginning of Extract phase.
        /// </summary>
        public void Reset()
        {
            FrameCounter++;

            // Clear render features node lists
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Reset();
            }

            // Clear views
            foreach (var view in Views)
            {
                // Clear nodes
                view.RenderObjects.Clear(false);

                foreach (var renderViewFeature in view.Features)
                {
                    renderViewFeature.RenderNodes.Clear(true);
                    renderViewFeature.ViewObjectNodes.Clear(true);
                    renderViewFeature.Layouts.Clear(false);
                }

                foreach (var renderViewStage in view.RenderStages)
                {
                    //Cannot use Clear(true), many structs have refs to objects
                    renderViewStage.RenderNodes.Clear(false);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="RenderObject"/> to the rendering.
        /// </summary>
        /// An appropriate <see cref="RootRenderFeature"/> will be found and the object will be initialized with it.
        /// If nothing could be found, <see cref="RenderObject.RenderFeature"/> will be null.
        /// <param name="renderObject"></param>
        public void AddRenderObject(RenderObject renderObject)
        {
            RootRenderFeature renderFeature;

            if (renderFeaturesByType.TryGetValue(renderObject.GetType(), out renderFeature))
            {
                // Found it
                renderFeature.AddRenderObject(renderObject);
            }
            else
            {
                // New type without render feature, let's do auto pipeline setup
                if (InstantiateDefaultPipelinePlugin(renderObject.GetType()))
                {
                    // Try again, after pipeline plugin setup
                    if (renderFeaturesByType.TryGetValue(renderObject.GetType(), out renderFeature))
                    {
                        renderFeature.AddRenderObject(renderObject);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a <see cref="RenderObject"/> from the rendering.
        /// </summary>
        /// <param name="renderObject"></param>
        public void RemoveRenderObject(RenderObject renderObject)
        {
            var renderFeature = renderObject.RenderFeature;
            renderFeature?.RemoveRenderObject(renderObject);
        }

        private void PrepareDataArrays()
        {
            // Also do it for each render feature
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.PrepareDataArrays();
            }
        }

        private void RenderStages_CollectionChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ((RenderStage)e.Item).Index = e.Index;
                    break;
            }
        }

        private void RenderFeatures_CollectionChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            var renderFeature = (RootRenderFeature)e.Item;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    renderFeature.Index = e.Index;
                    renderFeature.RenderSystem = this;

                    if (RenderContextOld != null)
                        renderFeature.Initialize(RenderContextOld);

                    renderFeature.RenderStageSelectors.CollectionChanged += RenderStageSelectors_CollectionChanged;

                    renderFeaturesByType.Add(renderFeature.SupportedRenderObjectType, renderFeature);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    renderFeature.RenderStageSelectors.CollectionChanged -= RenderStageSelectors_CollectionChanged;
                    renderFeaturesByType.Remove(renderFeature.SupportedRenderObjectType);
                    renderFeature.Unload();
                    break;
            }
        }

        private void RenderStageSelectors_CollectionChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            RenderStageSelectorsChanged?.Invoke();
        }

        private void Views_CollectionChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ((RenderView)e.Item).Index = e.Index;
                    break;
            }
        }

        private bool InstantiateDefaultPipelinePlugin(Type renderObjectType)
        {
            // Already processed
            if (!renderObjectsDefaultPipelinePlugins.Add(renderObjectType))
                return false;

            var autoPipelineAttribute = renderObjectType.GetTypeInfo().GetCustomAttribute<DefaultPipelinePluginAttribute>();
            if (autoPipelineAttribute != null)
            {
                PipelinePlugins.InstantiatePlugin(autoPipelineAttribute.PipelinePluginType);
                return true;
            }

            return false;
        }

        private class ExtractThreadLocals
        {
            public readonly ConcurrentCollectorCache<ViewObjectNodeReference> ViewFeatureObjectNodeCache = new ConcurrentCollectorCache<ViewObjectNodeReference>(16);
            public readonly ConcurrentCollectorCache<RenderNodeReference> ViewFeatureRenderNodeCache = new ConcurrentCollectorCache<RenderNodeReference>(16);
            public readonly ConcurrentCollectorCache<RenderNodeFeatureReference> ViewStageRenderNodeCache = new ConcurrentCollectorCache<RenderNodeFeatureReference>(16);

            public void Flush()
            {
                ViewFeatureObjectNodeCache.Flush();
                ViewFeatureRenderNodeCache.Flush();
                ViewStageRenderNodeCache.Flush();
            }
        }

        private class PrepareThreadLocals
        {
            public SortKey[] SortKeys;
        }

        private class RenderNodeFeatureReferenceComparer : IComparer<RenderNodeFeatureReference>
        {
            public static readonly RenderNodeFeatureReferenceComparer Default = new RenderNodeFeatureReferenceComparer();

            public int Compare(RenderNodeFeatureReference x, RenderNodeFeatureReference y)
            {
                return x.RootRenderFeature.Index - y.RootRenderFeature.Index;
            }
        }

        private class RenderObjectFeatureComparer : IComparer<RenderObject>
        {
            public static readonly RenderObjectFeatureComparer Default = new RenderObjectFeatureComparer();

            public int Compare(RenderObject x, RenderObject y)
            {
                return x.RenderFeature.Index - y.RenderFeature.Index;
            }
        }
    }
}
