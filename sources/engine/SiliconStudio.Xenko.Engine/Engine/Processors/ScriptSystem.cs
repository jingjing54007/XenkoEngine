﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Xenko.Games;

namespace SiliconStudio.Xenko.Engine.Processors
{
    /// <summary>
    /// The script system handles scripts scheduling in a game.
    /// </summary>
    public sealed class ScriptSystem : GameSystemBase
    {
        internal readonly static Logger Log = GlobalLogger.GetLogger("ScriptSystem");

        /// <summary>
        /// Contains all currently executed scripts
        /// </summary>
        private readonly HashSet<ScriptComponent> registeredScripts = new HashSet<ScriptComponent>();
        private readonly HashSet<ScriptComponent> scriptsToStart = new HashSet<ScriptComponent>();
        private readonly HashSet<SyncScript> syncScripts = new HashSet<SyncScript>();
        private readonly List<ScriptComponent> scriptsToStartCopy = new List<ScriptComponent>();
        private readonly List<SyncScript> syncScriptsCopy = new List<SyncScript>();

        /// <summary>
        /// Gets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        public Scheduler Scheduler { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameSystemBase" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <remarks>The GameSystem is expecting the following services to be registered: <see cref="IGame" /> and <see cref="ContentManager" />.</remarks>
        public ScriptSystem(IServiceRegistry registry)
            : base(registry)
        {
            Enabled = true;
            Scheduler = new Scheduler();
            Scheduler.ActionException += Scheduler_ActionException;
            Services.AddService(typeof(ScriptSystem), this);
        }

        protected override void Destroy()
        {
            Scheduler.ActionException -= Scheduler_ActionException;
            Scheduler = null;

            Services.RemoveService(typeof(ScriptSystem));

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            // Copy scripts to process (so that scripts added during this frame don't affect us)
            // TODO: How to handle scripts that we want to start during current frame?
            scriptsToStartCopy.AddRange(scriptsToStart);
            scriptsToStart.Clear();
            syncScriptsCopy.AddRange(syncScripts);

            // Schedule new scripts: StartupScript.Start() and AsyncScript.Execute()
            foreach (var script in scriptsToStartCopy)
            {
                // Start the script
                var startupScript = script as StartupScript;
                if (startupScript != null)
                {
                    Scheduler.Add(startupScript.Start, startupScript.Priority);
                }
                else
                {
                    // Start a microthread with execute method if it's an async script
                    var asyncScript = script as AsyncScript;
                    if (asyncScript != null)
                    {
                        asyncScript.MicroThread = AddTask(asyncScript.Execute, asyncScript.Priority);
                    }
                }
            }

            // Schedule existing scripts: SyncScript.Update()
            foreach (var syncScript in syncScriptsCopy)
            {
                // Update priority
                var updateSchedulerNode = syncScript.UpdateSchedulerNode;
                updateSchedulerNode.Value.Priority = syncScript.Priority;

                // Schedule
                Scheduler.Schedule(updateSchedulerNode, ScheduleMode.Last);
            }

            // Run current micro threads
            Scheduler.Run();

            // Flag scripts as not being live reloaded after starting/executing them for the first time
            foreach (var script in scriptsToStartCopy)
            {
                if (script.IsLiveReloading)
                    script.IsLiveReloading = false;
            }

            scriptsToStartCopy.Clear();
            syncScriptsCopy.Clear();
        }

        /// <summary>
        /// Allows to wait for next frame.
        /// </summary>
        /// <returns>ChannelMicroThreadAwaiter&lt;System.Int32&gt;.</returns>
        public ChannelMicroThreadAwaiter<int> NextFrame()
        {
            return Scheduler.NextFrame();
        }

        /// <summary>
        /// Adds the specified micro thread function.
        /// </summary>
        /// <param name="microThreadFunction">The micro thread function.</param>
        /// <returns>MicroThread.</returns>
        public MicroThread AddTask(Func<Task> microThreadFunction, int priority = 0)
        {
            var microThread = Scheduler.Create();
            microThread.Priority = priority;
            microThread.Start(microThreadFunction);
            return microThread;
        }

        /// <summary>
        /// Waits all micro thread finished their task completion.
        /// </summary>
        /// <param name="microThreads">The micro threads.</param>
        /// <returns>Task.</returns>
        public async Task WhenAll(params MicroThread[] microThreads)
        {
            await Scheduler.WhenAll(microThreads);
        }

        /// <summary>
        /// Add the provided script to the script system.
        /// </summary>
        /// <param name="script">The script to add</param>
        public void Add(ScriptComponent script)
        {
            script.Initialize(Services);
            registeredScripts.Add(script);

            // Register script for Start() and possibly async Execute()
            scriptsToStart.Add(script);

            // If it's a synchronous script, add it to the list as well
            var syncScript = script as SyncScript;
            if (syncScript != null)
            {
                syncScript.UpdateSchedulerNode = Scheduler.Create(syncScript.Update, syncScript.Priority);
                syncScript.UpdateSchedulerNode.Value.Token = syncScript;
                syncScripts.Add(syncScript);
            }
        }

        /// <summary>
        /// Remove the provided script from the script system.
        /// </summary>
        /// <param name="script">The script to remove</param>
        public void Remove(ScriptComponent script)
        {
            // Make sure it's not registered in any pending list
            var startWasPending = scriptsToStart.Remove(script);
            var wasRegistered = registeredScripts.Remove(script);

            if (!startWasPending && wasRegistered)
            {
                // Cancel scripts that were already started
                try
                {
                    script.Cancel();
                }
                catch (Exception e)
                {
                    HandleSynchronousException(script, e);
                }

                var asyncScript = script as AsyncScript;
                asyncScript?.MicroThread.Cancel();
            }

            var syncScript = script as SyncScript;
            if (syncScript != null)
            {
                syncScripts.Remove(syncScript);
                syncScript.UpdateSchedulerNode = null;
            }
        }

        /// <summary>
        /// Called by a live scripting debugger to notify the ScriptSystem about reloaded scripts.
        /// </summary>
        /// <param name="oldScript">The old script</param>
        /// <param name="newScript">The new script</param>
        public void LiveReload(ScriptComponent oldScript, ScriptComponent newScript)
        {
            // Set live reloading mode for the rest of it's lifetime
            oldScript.IsLiveReloading = true;

            // Set live reloading mode until after being started
            newScript.IsLiveReloading = true;
        }

        private void Scheduler_ActionException(Scheduler scheduler, SchedulerEntry schedulerEntry, Exception e)
        {
            HandleSynchronousException((ScriptComponent)schedulerEntry.Token, e);
        }

        private void HandleSynchronousException(ScriptComponent script, Exception e)
        {
            Log.Error("Unexpected exception while executing a script.", e);

            // Only crash if live scripting debugger is not listening
            if (Scheduler.PropagateExceptions)
                ExceptionDispatchInfo.Capture(e).Throw();

            // Remove script from all lists
            var syncScript = script as SyncScript;
            if (syncScript != null)
            {
                syncScripts.Remove(syncScript);
            }

            registeredScripts.Remove(script);
        }

        class PriorityScriptComparer : IComparer<ScriptComponent>
        {
            public static readonly PriorityScriptComparer Default = new PriorityScriptComparer();

            public int Compare(ScriptComponent x, ScriptComponent y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }
}
