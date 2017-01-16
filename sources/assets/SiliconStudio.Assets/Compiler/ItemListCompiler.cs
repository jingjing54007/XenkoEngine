﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.IO;
using SiliconStudio.Assets.Diagnostics;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Assets.Compiler
{
    /// <summary>
    /// The base class to compile a series of <see cref="AssetItem"/>s using associated <see cref="IAssetCompiler"/>s.
    /// An item list compiler only creates the build steps required to creates some output items.
    /// The result of a compilation has then to be executed by the build engine to effectively create the outputs items.
    /// </summary>
    public abstract class ItemListCompiler
    {
        private readonly ICompilerRegistry<IAssetCompiler> compilerRegistry;
        private int latestPriority;

        /// <summary>
        /// Raised when a single asset has been compiled.
        /// </summary>
        public EventHandler<AssetCompiledArgs> AssetCompiled;

        /// <summary>
        /// Create an instance of <see cref="ItemListCompiler"/> using the provided compiler registry.
        /// </summary>
        /// <param name="compilerRegistry">The registry that contains the compiler to use for each asset type</param>
        protected ItemListCompiler(ICompilerRegistry<IAssetCompiler> compilerRegistry)
        {
            if (compilerRegistry == null) throw new ArgumentNullException(nameof(compilerRegistry));
            this.compilerRegistry = compilerRegistry;
        }

        /// <summary>
        /// Compile the required build steps necessary to produce the desired outputs items.
        /// </summary>
        /// <param name="context">The context source.</param>
        /// <param name="assetItems">The list of items to compile</param>
        /// <param name="compilationResult">The current compilation result, containing the build steps and the logging</param>
        protected void Compile(CompilerContext context, IEnumerable<AssetItem> assetItems,
            AssetCompilerResult compilationResult)
        {
            foreach (var assetItem in assetItems)
            {
                var itemBuildStep = CompileItem(context, compilationResult, assetItem);
                if (itemBuildStep != null)
                    compilationResult.BuildSteps.Add(itemBuildStep);
            }
        }

        /// <summary>
        /// Compile the required build step necessary to produce the desired output item.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="compilationResult">The compilation result.</param>
        /// <param name="assetItem">The asset item.</param>
        protected ListBuildStep CompileItem(CompilerContext context, AssetCompilerResult compilationResult, AssetItem assetItem)
        {
            // First try to find an asset compiler for this particular asset.
            IAssetCompiler compiler;
            try
            {
                compiler = compilerRegistry.GetCompiler(assetItem.Asset.GetType());
            }
            catch (Exception ex)
            {
                compilationResult.Error($"Cannot find a compiler for asset [{assetItem.Id}] from path [{assetItem.Location}]", ex);
                return null;
            }

            if (compiler == null)
            {
                return null;
            }

            // Second we are compiling the asset (generating a build step)
            try
            {
                var resultPerAssetType = compiler.Compile(context, assetItem);

                // Raise the AssetCompiled event.
                AssetCompiled?.Invoke(this, new AssetCompiledArgs(assetItem, resultPerAssetType));

                // TODO: See if this can be unified with PackageBuilder.BuildStepProcessed
                var assetFullPath = assetItem.FullPath.ToWindowsPath();
                foreach (var message in resultPerAssetType.Messages)
                {
                    var assetMessage = AssetLogMessage.From(null, assetItem.ToReference(), message, assetFullPath);
                    // Forward log messages to compilationResult
                    compilationResult.Log(assetMessage);

                    // Forward log messages to build step logger
                    resultPerAssetType.BuildSteps.Logger.Log(assetMessage);
                }

                // Make the build step fail if there was an error during compiling (only when we are compiling the build steps of an asset)
                if (resultPerAssetType.BuildSteps is AssetBuildStep && resultPerAssetType.BuildSteps.Logger.HasErrors)
                    resultPerAssetType.BuildSteps.Add(new CommandBuildStep(new FailedCommand(assetItem.Location)));

                // TODO: Big review of the log infrastructure of CompilerApp & BuildEngine!
                // Assign module string to all command build steps
                SetAssetLogger(resultPerAssetType.BuildSteps, assetItem.Package, assetItem.ToReference(), assetItem.FullPath.ToWindowsPath());

                // Add a wait command to the build steps if required by the item build
                if (resultPerAssetType.ShouldWaitForPreviousBuilds)
                    compilationResult.BuildSteps.Add(new WaitBuildStep());

                foreach (var buildStep in resultPerAssetType.BuildSteps)
                {
                    buildStep.Priority = latestPriority++;
                }

                // Add the item result build steps the item list result build steps 
                return resultPerAssetType.BuildSteps;
            }
            catch (Exception ex)
            {
                compilationResult.Error($"Unexpected exception while compiling asset [{assetItem.Id}] from path [{assetItem.Location}]", ex);
                return null;
            }
        }

        /// <summary>
        /// Sets recursively the <see cref="BuildStep.Module"/>.
        /// </summary>
        /// <param name="buildStep">The build step.</param>
        /// <param name="module">The module.</param>
        private void SetAssetLogger(BuildStep buildStep, Package package, IReference assetReference, string assetFullPath)
        {
            if (buildStep.TransformExecuteContextLogger == null)
                buildStep.TransformExecuteContextLogger = (ref Logger logger) => logger = new AssetLogger(package, assetReference, assetFullPath, logger);

            var enumerableBuildStep = buildStep as EnumerableBuildStep;
            if (enumerableBuildStep != null && enumerableBuildStep.Steps != null)
            {
                foreach (var child in enumerableBuildStep.Steps)
                {
                    SetAssetLogger(child, package, assetReference, assetFullPath);
                }
            }
        }
    }
}
