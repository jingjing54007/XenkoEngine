﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;

namespace SiliconStudio.Core.Reflection
{
    public class LoadedAssembly
    {
        public string Path { get; private set; }
        public Assembly Assembly { get; private set; }

        public LoadedAssembly(string path, Assembly assembly)
        {
            Path = path;
            Assembly = assembly;
        }
    }
    public class AssemblyContainer
    {
        private readonly List<LoadedAssembly> loadedAssemblies = new List<LoadedAssembly>();
        private readonly Dictionary<string, LoadedAssembly> loadedAssembliesByName = new Dictionary<string, LoadedAssembly>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly string[] KnownAssemblyExtensions = { ".dll", ".exe" };
        [ThreadStatic]
        private static AssemblyContainer loadingInstance;

        [ThreadStatic]
        private static LoggerResult log;

        [ThreadStatic]
        private static List<string> searchDirectoryList;

        /// <summary>
        /// The default assembly container loader.
        /// </summary>
        public static readonly AssemblyContainer Default = new AssemblyContainer();

        static AssemblyContainer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Gets a copy of the list of loaded assemblies.
        /// </summary>
        /// <value>
        /// The loaded assemblies.
        /// </value>
        public IList<LoadedAssembly> LoadedAssemblies
        {
            get
            {
                lock (loadedAssemblies)
                {
                    return loadedAssemblies.ToList();
                }
            }
        }

        public Assembly LoadAssemblyFromPath(string assemblyFullPath, ILogger outputLog = null, List<string> lookupDirectoryList = null)
        {
            if (assemblyFullPath == null) throw new ArgumentNullException("assemblyFullPath");

            log = new LoggerResult();

            lookupDirectoryList = lookupDirectoryList ?? new List<string>();
            assemblyFullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, assemblyFullPath));
            var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath);

            if (assemblyDirectory == null || !Directory.Exists(assemblyDirectory))
            {
                throw new ArgumentException("Invalid assembly path. Doesn't contain directory information");
            }

            if (!lookupDirectoryList.Contains(assemblyDirectory, StringComparer.InvariantCultureIgnoreCase))
            {
                lookupDirectoryList.Add(assemblyDirectory);
            }

            var previousLookupList = searchDirectoryList;
            try
            {
                loadingInstance = this;
                searchDirectoryList = lookupDirectoryList;

                return LoadAssemblyFromPathInternal(assemblyFullPath);
            }
            finally
            {
                loadingInstance = null;
                searchDirectoryList = previousLookupList;

                if (outputLog != null)
                {
                    log.CopyTo(outputLog);
                }
            }
        }

        public bool UnloadAssembly(Assembly assembly)
        {
            lock (loadedAssemblies)
            {
                var loadedAssembly = loadedAssemblies.FirstOrDefault(x => x.Assembly == assembly);
                if (loadedAssembly == null)
                    return false;

                loadedAssemblies.Remove(loadedAssembly);
                loadedAssembliesByName.Remove(loadedAssembly.Path);
                return true;
            }
        }

        private Assembly LoadAssemblyByName(string assemblyName)
        {
            if (assemblyName == null) throw new ArgumentNullException("assemblyName");

            var assemblyPartialPathList = new List<string>();
            assemblyPartialPathList.AddRange(KnownAssemblyExtensions.Select(knownExtension => assemblyName + knownExtension));

            foreach (var directoryPath in searchDirectoryList)
            {
                foreach (var assemblyPartialPath in assemblyPartialPathList)
                {
                    var assemblyFullPath = Path.Combine(directoryPath, assemblyPartialPath);
                    if (File.Exists(assemblyFullPath))
                    {
                        return LoadAssemblyFromPathInternal(assemblyFullPath);
                    }
                }
            }
            return null;
        }

        private Assembly LoadAssemblyFromPathInternal(string assemblyFullPath)
        {
            if (assemblyFullPath == null) throw new ArgumentNullException("assemblyFullPath");

            assemblyFullPath = Path.GetFullPath(assemblyFullPath);

            try
            {
                lock (loadedAssemblies)
                {
                    LoadedAssembly loadedAssembly;
                    if (loadedAssembliesByName.TryGetValue(assemblyFullPath, out loadedAssembly))
                    {
                        return loadedAssembly.Assembly;
                    }

                    if (!File.Exists(assemblyFullPath))
                        return null;

                    // Find pdb (if it exists)
                    var pdbFullPath = Path.ChangeExtension(assemblyFullPath, ".pdb");
                    if (!File.Exists(pdbFullPath))
                        pdbFullPath = null;

                    // PreLoad the assembly into memory without locking it
                    var assemblyBytes = File.ReadAllBytes(assemblyFullPath);
                    var pdbBytes = pdbFullPath != null ? File.ReadAllBytes(pdbFullPath) : null;

                    // Load the assembly into the current AppDomain
                    Assembly assembly;
                    if (new UDirectory(AppDomain.CurrentDomain.BaseDirectory) == new UFile(assemblyFullPath).GetFullDirectory())
                    {
                        // If loading from base directory, don't even try to load through byte array, as Assembly.Load will notice there is a "safer" version to load
                        // This happens usually when opening Xenko assemblies themselves
                        assembly = Assembly.LoadFrom(assemblyFullPath);
                    }
                    else
                    {
                        // TODO: Is using AppDomain would provide more opportunities for unloading?
                        assembly = pdbBytes != null ? Assembly.Load(assemblyBytes, pdbBytes) : Assembly.Load(assemblyBytes);
                        loadedAssembly = new LoadedAssembly(assemblyFullPath, assembly);
                        loadedAssemblies.Add(loadedAssembly);
                        loadedAssembliesByName.Add(assemblyFullPath, loadedAssembly);

                        // Force assembly resolve with proper name
                        // (doing it here, because if done later, loadingInstance will be set to null and it won't work)
                        Assembly.Load(assembly.FullName);
                    }

                    // Make sure that all referenced assemblies are loaded here
                    foreach (var assemblyRef in assembly.GetReferencedAssemblies())
                    {
                        Assembly.Load(assemblyRef);
                    }

                    // Make sure that Module initializer are called
                    if (assembly.GetTypes().Length > 0)
                    {
                        foreach (var module in assembly.Modules)
                        {
                            RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
                        }
                    }
                    return assembly;
                }
            }
            catch (Exception exception)
            {
                log.Error($"Error while loading assembly reference [{assemblyFullPath}]", exception);
                var loaderException = exception as ReflectionTypeLoadException;
                if (loaderException != null)
                {
                    foreach (var exceptionForType in loaderException.LoaderExceptions)
                    {
                        log.Error("Unable to load type. See exception.", exceptionForType);
                    }
                }
            }
            return null;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // If it is handled by current thread, then we can handle it here.
            var container = loadingInstance;
            if (container != null)
            {
                var assemblyName = new AssemblyName(args.Name);
                return container.LoadAssemblyByName(assemblyName.Name);
            }
            return null;
        }
    }
}
