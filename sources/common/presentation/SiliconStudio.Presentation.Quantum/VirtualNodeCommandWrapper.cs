﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Threading.Tasks;
using SiliconStudio.Presentation.ViewModel;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Commands;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Presentation.Quantum
{
    public class VirtualNodeCommandWrapper : NodeCommandWrapperBase
    {
        private readonly IContentNode node;
        private readonly Index index;
        protected readonly GraphViewModelService Service;

        public VirtualNodeCommandWrapper(IViewModelServiceProvider serviceProvider, INodeCommand nodeCommand, IContentNode node, Index index)
            : base(serviceProvider)
        {
            if (nodeCommand == null) throw new ArgumentNullException(nameof(nodeCommand));
            if (node == null) throw new ArgumentNullException(nameof(node));
            this.node = node;
            this.index = index;
            NodeCommand = nodeCommand;
            Service = serviceProvider.Get<GraphViewModelService>();
        }

        public override string Name => NodeCommand.Name;

        public override CombineMode CombineMode => NodeCommand.CombineMode;

        public INodeCommand NodeCommand { get; }

        public override async Task Invoke(object parameter)
        {
            using (var transaction = ActionService.CreateTransaction())
            {
                await NodeCommand.Execute(node, index, parameter);
                ActionService.SetName(transaction, ActionName);
            }
        }
    }
}
