﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Xenko.Shaders.Compiler;

namespace SiliconStudio.Xenko.Assets.Effect
{
    /// <summary>
    /// This command is responsible to compile a single permutation of an effect (xkfx or xksl)
    /// </summary>
    internal sealed class EffectCompileCommand : IndexFileCommand
    {
        private static readonly PropertyKey<EffectCompilerBase> CompilerKey = new PropertyKey<EffectCompilerBase>("CompilerKey", typeof(EffectCompileCommand));

        private readonly AssetCompilerContext context;
        private readonly UDirectory baseUrl;
        private string effectName;
        private CompilerParameters compilerParameters;
        private readonly Package package;
        private static Dictionary<string, int> PermutationCount = new Dictionary<string, int>();

        public EffectCompileCommand(AssetCompilerContext context, UDirectory baseUrl, string effectName, CompilerParameters compilerParameters, Package package)
        {
            this.context = context;
            this.baseUrl = baseUrl;
            this.effectName = effectName;
            this.compilerParameters = compilerParameters;
            this.package = package;
        }

        public override string Title => $"EffectCompile [{effectName}]";

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            uint effectbyteCodeMagicNumber = EffectBytecode.MagicHeader;
            writer.Serialize(ref effectbyteCodeMagicNumber, ArchiveMode.Serialize);
            writer.Serialize(ref effectName, ArchiveMode.Serialize);
            writer.Serialize(ref compilerParameters, ArchiveMode.Serialize);
            writer.Serialize(ref compilerParameters.EffectParameters, ArchiveMode.Serialize);
        }

        protected override void ComputeAssemblyHash(BinarySerializationWriter writer)
        {
            writer.Write(DataSerializer.BinaryFormatVersion);
            writer.Write(EffectBytecode.MagicHeader);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var compiler = GetOrCreateEffectCompiler(context);

            // Get main effect name (before the first dot)
            var isXkfx = ShaderMixinManager.Contains(effectName);
            var source = isXkfx ? new ShaderMixinGeneratorSource(effectName) : (ShaderSource)new ShaderClassSource(effectName);

            int permutationCount;
            lock (PermutationCount)
            {
                PermutationCount.TryGetValue(effectName, out permutationCount);
                permutationCount++;
                PermutationCount[effectName] = permutationCount;
            }
            commandContext.Logger.Verbose($"Trying permutation #{permutationCount} for effect [{effectName}]: \n{compilerParameters.ToStringPermutationsDetailed()}");

            var compilerResults = compiler.Compile(source, compilerParameters);

            // Copy logs and if there are errors, exit directly
            compilerResults.CopyTo(commandContext.Logger);
            if (compilerResults.HasErrors)
            {
                return Task.FromResult(ResultStatus.Failed);
            }

            // wait for result an check compilation status
            var completedTask = compilerResults.Bytecode.WaitForResult();
            completedTask.CompilationLog.CopyTo(commandContext.Logger);
            if (completedTask.CompilationLog.HasErrors)
            {
                return Task.FromResult(ResultStatus.Failed);
            }

            // Register all dependencies
            var allSources = new HashSet<string>(completedTask.Bytecode.HashSources.Select(keyPair => keyPair.Key));
            foreach (var className in allSources)
            {
                commandContext.RegisterInputDependency(new ObjectUrl(UrlType.ContentLink, EffectCompilerBase.GetStoragePathFromShaderType(className)));
            }

            // Generate sourcecode if configured
            if (compilerParameters.ContainsKey(EffectSourceCodeKeys.Enable))
            {
                var outputDirectory = UPath.Combine(package.RootDirectory, baseUrl);

                var fieldName = compilerParameters.Get(EffectSourceCodeKeys.FieldName);
                if (fieldName.StartsWith("binary"))
                {
                    fieldName = fieldName.Substring("binary".Length);
                    if (char.IsUpper(fieldName[0]))
                    {
                        fieldName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
                    }
                }

                var outputClassFile = effectName + "." + fieldName + "." + compilerParameters.EffectParameters.Platform + "." + compilerParameters.EffectParameters.Profile + ".cs";
                var fullOutputClassFile = Path.Combine(outputDirectory.ToWindowsPath(), outputClassFile);

                commandContext.Logger.Verbose($"Writing shader bytecode to .cs source [{fullOutputClassFile}]");
                using (var stream = new FileStream(fullOutputClassFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                    EffectByteCodeToSourceCodeWriter.Write(effectName, compilerParameters, compilerResults.Bytecode.WaitForResult().Bytecode, new StreamWriter(stream, System.Text.Encoding.UTF8));
            }

            return Task.FromResult(ResultStatus.Successful);
        }

        public override string ToString()
        {
            return Title;
        }

        private static EffectCompilerBase GetOrCreateEffectCompiler(AssetCompilerContext context)
        {
            lock (context)
            {
                var compiler = context.Properties.Get(CompilerKey);
                if (compiler == null)
                {
                    // Create compiler
                    var effectCompiler = new Shaders.Compiler.EffectCompiler();
                    effectCompiler.SourceDirectories.Add(EffectCompilerBase.DefaultSourceShaderFolder);
                    compiler = new EffectCompilerCache(effectCompiler) { CurrentCache = EffectBytecodeCacheLoadSource.StartupCache };
                    context.Properties.Set(CompilerKey, compiler);

                    var shaderLocations = context.Properties.Get(EffectShaderAssetCompiler.ShaderLocationsKey);

                    // Temp copy URL to absolute file path to inform the compiler the absolute file location
                    // of all xksl files.
                    if (shaderLocations != null)
                    {
                        foreach (var shaderLocation in shaderLocations)
                        {
                            effectCompiler.UrlToFilePath[shaderLocation.Key] = shaderLocation.Value;
                        }
                    }
                }

                return compiler;
            }
        }

        public static BuildStep FromRequest(AssetCompilerContext context, Package package, UDirectory urlRoot, EffectCompileRequest effectCompileRequest)
        {
            var compilerParameters = new CompilerParameters(effectCompileRequest.UsedParameters);
            compilerParameters.EffectParameters.Platform = context.GetGraphicsPlatform(package);
            compilerParameters.EffectParameters.Profile = context.GetGameSettingsAsset().Get<RenderingSettings>(context.Platform).DefaultGraphicsProfile;
            compilerParameters.EffectParameters.ApplyCompilationMode(context.GetCompilationMode());
            return new CommandBuildStep(new EffectCompileCommand(context, urlRoot, effectCompileRequest.EffectName, compilerParameters, package));
        }
    }
}
