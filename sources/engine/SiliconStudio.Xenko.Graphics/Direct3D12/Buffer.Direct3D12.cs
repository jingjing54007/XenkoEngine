﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_XENKO_GRAPHICS_API_DIRECT3D12
using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D12;

namespace SiliconStudio.Xenko.Graphics
{
    public partial class Buffer
    {
        private SharpDX.Direct3D12.ResourceDescription nativeDescription;
        internal long GPUVirtualAddress;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            nativeDescription = ConvertToNativeDescription(GraphicsDevice, Description);
            ViewFlags = viewFlags;
            InitCountAndViewFormat(out this.elementCount, ref viewFormat);
            ViewFormat = viewFormat;
            Recreate(dataPointer);

            if (GraphicsDevice != null)
            {
                GraphicsDevice.BuffersMemory += SizeInBytes/(float)0x100000;
            }

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (GraphicsDevice != null)
            {
                GraphicsDevice.BuffersMemory -= SizeInBytes/(float)0x100000;
            }

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage == GraphicsResourceUsage.Immutable
                || Description.Usage == GraphicsResourceUsage.Default)
                return false;

            Recreate(IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public void Recreate(IntPtr dataPointer)
        {
            // TODO D3D12 where should that go longer term? should it be precomputed for future use? (cost would likely be additional check on SetDescriptorSets/Draw)
            NativeResourceState = ResourceStates.Common;
            var bufferFlags = bufferDescription.BufferFlags;

            if ((bufferFlags & BufferFlags.ConstantBuffer) != 0)
                NativeResourceState |= ResourceStates.VertexAndConstantBuffer;

            if ((bufferFlags & BufferFlags.IndexBuffer) != 0)
                NativeResourceState |= ResourceStates.IndexBuffer;

            if ((bufferFlags & BufferFlags.VertexBuffer) != 0)
                NativeResourceState |= ResourceStates.VertexAndConstantBuffer;

            if ((bufferFlags & BufferFlags.ShaderResource) != 0)
                NativeResourceState |= ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;

            if ((bufferFlags & BufferFlags.UnorderedAccess) != 0)
                NativeResourceState |= ResourceStates.UnorderedAccess;

            if ((bufferFlags & BufferFlags.StructuredBuffer) != 0)
            {
                throw new NotImplementedException();
                if (bufferDescription.StructureByteStride == 0)
                    throw new ArgumentException("Element size cannot be set to 0 for structured buffer");
            }

            if ((bufferFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer)
                throw new NotImplementedException();

            if ((bufferFlags & BufferFlags.ArgumentBuffer) == BufferFlags.ArgumentBuffer)
                NativeResourceState |= ResourceStates.IndirectArgument;

            var heapType = HeapType.Default;
            if (Usage == GraphicsResourceUsage.Staging)
            {
                throw new NotImplementedException();
            }
            else if (Usage == GraphicsResourceUsage.Dynamic)
            {
                heapType = HeapType.Upload;
                NativeResourceState = ResourceStates.GenericRead;
            }

            // TODO D3D12 move that to a global allocator in bigger committed resources
            NativeDeviceChild = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(heapType), HeapFlags.None, nativeDescription, dataPointer != IntPtr.Zero ? ResourceStates.CopyDestination : NativeResourceState);
            GPUVirtualAddress = NativeResource.GPUVirtualAddress;

            if (dataPointer != IntPtr.Zero)
            {
                if (heapType == HeapType.Upload)
                {
                    var uploadMemory = NativeResource.Map(0);
                    Utilities.CopyMemory(uploadMemory, dataPointer, SizeInBytes);
                    NativeResource.Unmap(0);
                }
                else
                {
                    // Copy data in upload heap for later copy
                    // TODO D3D12 move that to a shared upload heap
                    SharpDX.Direct3D12.Resource uploadResource;
                    int uploadOffset;
                    var uploadMemory = GraphicsDevice.AllocateUploadBuffer(SizeInBytes, out uploadResource, out uploadOffset);
                    Utilities.CopyMemory(uploadMemory, dataPointer, SizeInBytes);

                    // TODO D3D12 lock NativeCopyCommandList usages
                    var commandList = GraphicsDevice.NativeCopyCommandList;
                    commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, null);
                    // Copy from upload heap to actual resource
                    commandList.CopyBufferRegion(NativeResource, 0, uploadResource, uploadOffset, SizeInBytes);

                    // Switch resource to proper read state
                    commandList.ResourceBarrierTransition(NativeResource, 0, ResourceStates.CopyDestination, NativeResourceState);

                    commandList.Close();

                    GraphicsDevice.WaitCopyQueue();
                }
            }

            this.NativeShaderResourceView = GetShaderResourceView(ViewFormat);
        }

        /// <summary>
        /// Gets a <see cref="ShaderResourceView"/> for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="viewFormat">The view format.</param>
        /// <returns>A <see cref="ShaderResourceView"/> for the particular view format.</returns>
        /// <remarks>
        /// The buffer must have been declared with <see cref="Graphics.BufferFlags.ShaderResource"/>. 
        /// The ShaderResourceView instance is kept by this buffer and will be disposed when this buffer is disposed.
        /// </remarks>
        internal CpuDescriptorHandle GetShaderResourceView(PixelFormat viewFormat)
        {
            var srv = new CpuDescriptorHandle();
            if ((ViewFlags & BufferFlags.ShaderResource) != 0)
            {
                var description = new ShaderResourceViewDescription
                {
                    Shader4ComponentMapping = 0x00001688,
                    Format = (SharpDX.DXGI.Format)viewFormat,
                    Dimension = SharpDX.Direct3D12.ShaderResourceViewDimension.Buffer,
                    Buffer =
                    {
                        ElementCount = this.ElementCount,
                        FirstElement = 0,
                        Flags = BufferShaderResourceViewFlags.None,
                        StructureByteStride = StructureByteStride,
                    }
                };

                if (((ViewFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer))
                    description.Buffer.Flags |= BufferShaderResourceViewFlags.Raw;

                srv = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
                NativeDevice.CreateShaderResourceView(NativeResource, description, srv);
            }
            return srv;
        }

        private void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
        {
            if (Description.StructureByteStride == 0)
            {
                // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                if ((ViewFlags & BufferFlags.RawBuffer) != 0)
                {
                    count = Description.SizeInBytes / sizeof(int);
                }
                else if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    count = Description.SizeInBytes / viewFormat.SizeInBytes();
                }
                else
                {
                    count = 0;
                }
            }
            else
            {
                // For structured buffer
                count = Description.SizeInBytes / Description.StructureByteStride;
                viewFormat = PixelFormat.None;
            }
        }

        private static SharpDX.Direct3D12.ResourceDescription ConvertToNativeDescription(GraphicsDevice graphicsDevice, BufferDescription bufferDescription)
        {
            var size = bufferDescription.SizeInBytes;

            // TODO D3D12 for now, ensure size is multiple of ConstantBufferDataPlacementAlignment (for cbuffer views)
            size = (size + graphicsDevice.ConstantBufferDataPlacementAlignment - 1) / graphicsDevice.ConstantBufferDataPlacementAlignment * graphicsDevice.ConstantBufferDataPlacementAlignment;

            return SharpDX.Direct3D12.ResourceDescription.Buffer(size);
        }
    }
} 
#endif 
