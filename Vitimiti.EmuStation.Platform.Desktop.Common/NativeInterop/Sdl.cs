// The MIT License (MIT)
//
// Copyright (C) 2026 Victor Matia (vitimiti)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
// the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Vitimiti.EmuStation.Platform.Desktop.Common.NativeInterop;

internal static unsafe partial class Ffi
{
    #region Marshallers

    [CustomMarshaller(
        typeof(string),
        MarshalMode.ManagedToUnmanagedOut,
        typeof(UnownedUtf8StringMarshaller)
    )]
    private static class UnownedUtf8StringMarshaller
    {
        public static string? ConvertToManaged(byte* unmanaged) =>
            Utf8StringMarshaller.ConvertToManaged(unmanaged);
    }

    #endregion

    #region SDL_error.h

    [LibraryImport(
        LibSdl3,
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(UnownedUtf8StringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetError();

    #endregion // SDL_error.h

    #region SDL_events.h

    public readonly record struct SDL_EventType(uint Value);

    public static SDL_EventType SDL_EVENT_QUIT => new(0x0000_0100U);

    [StructLayout(LayoutKind.Explicit)]
    public struct SDL_Event
    {
        [FieldOffset(0)]
        public SDL_EventType Type;

        [FieldOffset(0)]
        private fixed byte _padding[128];
    }

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_PollEvent(out SDL_Event @event);

    #endregion // SDL_events.h

    #region SDL_gpu.h

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_GPUDevice>))]
    public sealed class SDL_GPUDevice : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SDL_GPUDevice()
            : base(ownsHandle: true) => SetHandle(0);

        protected override bool ReleaseHandle()
        {
            try
            {
                SDL_DestroyGPUDevice(handle);
            }
            catch
            {
                // If we fail to destroy the GPU device something went really wrong.
                // Returning false will throw an exception, which is what we want in this case.
                return false;
            }

            return true;
        }
    }

    public readonly record struct SDL_GPUCommandBuffer(nint Handle)
    {
        public bool IsInvalid => Handle == 0;
    }

    public readonly record struct SDL_GPUTexture(nint Handle)
    {
        public bool IsInvalid => Handle == 0;
    }

    public readonly record struct SDL_GPURenderPass(nint Handle)
    {
        public bool IsInvalid => Handle == 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_GPUColorTargetInfo
    {
        public SDL_GPUTexture Texture;
        public uint MipLevel;
        public uint LayerOrDepthPlane;
        public SDL_FColor ClearColor;
        public SDL_GPULoadOp LoadOp;
        public SDL_GPUStoreOp StoreOp;
        public SDL_GPUTexture ResolveTexture;
        public uint ResolveMipLevel;
        public uint ResolveLayer;

        [MarshalAs(UnmanagedType.I1)]
        public bool Cycle;

        [MarshalAs(UnmanagedType.I1)]
        public bool CycleResolveTexture;

        private readonly byte _padding1;
        private readonly byte _padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_GPUDepthStencilTargetInfo
    {
        public SDL_GPUTexture Texture;
        public float ClearDepth;
        public SDL_GPULoadOp LoadOp;
        public SDL_GPUStoreOp StoreOp;
        public SDL_GPULoadOp StencilLoadOp;
        public SDL_GPUStoreOp StencilStoreOp;

        [MarshalAs(UnmanagedType.I1)]
        public bool Cycle;

        public byte ClearStencil;
        public byte MipLevel;
        public byte Layer;
    }

    public readonly record struct SDL_GPUShaderFormat(uint Value)
    {
        public static SDL_GPUShaderFormat operator |(
            SDL_GPUShaderFormat left,
            SDL_GPUShaderFormat right
        ) => new(left.Value | right.Value);
    }

    public static SDL_GPUShaderFormat SDL_GPU_SHADERFORMAT_SPIRV => new(1U << 1);
    public static SDL_GPUShaderFormat SDL_GPU_SHADERFORMAT_DXBC => new(1U << 2);
    public static SDL_GPUShaderFormat SDL_GPU_SHADERFORMAT_DXIL => new(1U << 3);
    public static SDL_GPUShaderFormat SDL_GPU_SHADERFORMAT_MSL => new(1U << 4);
    public static SDL_GPUShaderFormat SDL_GPU_SHADERFORMAT_METALLIB => new(1U << 5);

    public readonly record struct SDL_GPULoadOp(int Value);

    public static SDL_GPULoadOp SDL_GPU_LOADOP_CLEAR => new(1);

    public readonly record struct SDL_GPUStoreOp(int Value);

    public static SDL_GPUStoreOp SDL_GPU_STOREOP_STORE => new(0);

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_GPUDevice SDL_CreateGPUDevice(
        SDL_GPUShaderFormat formatFlags,
        [MarshalAs(UnmanagedType.I1)] bool debugMode,
        string? name
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_ClaimWindowForGPUDevice(SDL_GPUDevice device, SDL_Window window);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_GPUCommandBuffer SDL_AcquireGPUCommandBuffer(SDL_GPUDevice device);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_WaitAndAcquireGPUSwapchainTexture(
        SDL_GPUCommandBuffer commandBuffer,
        SDL_Window window,
        out SDL_GPUTexture texture,
        out uint swapchainTextureWidth,
        out uint swapchainTextureHeight
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_GPURenderPass SDL_BeginGPURenderPass(
        SDL_GPUCommandBuffer commandBuffer,
        SDL_GPUColorTargetInfo* colorTargetInfos,
        uint numColorTargets,
        SDL_GPUDepthStencilTargetInfo* depthStencilTargetInfo
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_EndGPURenderPass(SDL_GPURenderPass renderPass);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SubmitGPUCommandBuffer(SDL_GPUCommandBuffer commandBuffer);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_WaitForGPUIdle(SDL_GPUDevice device);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_ReleaseWindowFromGPUDevice(
        SDL_GPUDevice device,
        SDL_Window window
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_DestroyGPUDevice(nint device);

    #endregion // SDL_gpu.h

    #region SDL_init.h

    public readonly record struct SDL_InitFlags(uint Value)
    {
        public static SDL_InitFlags operator |(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value | right.Value);
    }

    public static SDL_InitFlags SDL_INIT_VIDEO => new(0x0000_0010U);
    public static SDL_InitFlags SDL_INIT_AUDIO => new(0x0000_0020U);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_InitSubSystem(SDL_InitFlags flags);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_Quit();

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_SetAppMetadata(
        string? appName,
        string? appVersion,
        string? appIdentifier
    );

    #endregion // SDL_init.h

    #region SDL_log.h

    public readonly record struct SDL_LogCategory(int Value)
    {
        [SuppressMessage(
            "Style",
            "IDE0046:Convert to conditional expression",
            Justification = "Consecutive ternary operators are less readable than a series of if statements."
        )]
        public override string ToString()
        {
            if (this == SDL_LOG_CATEGORY_APPLICATION)
            {
                return nameof(SDL_LOG_CATEGORY_APPLICATION);
            }
            if (this == SDL_LOG_CATEGORY_ERROR)
            {
                return nameof(SDL_LOG_CATEGORY_ERROR);
            }
            if (this == SDL_LOG_CATEGORY_ASSERT)
            {
                return nameof(SDL_LOG_CATEGORY_ASSERT);
            }
            if (this == SDL_LOG_CATEGORY_SYSTEM)
            {
                return nameof(SDL_LOG_CATEGORY_SYSTEM);
            }
            if (this == SDL_LOG_CATEGORY_AUDIO)
            {
                return nameof(SDL_LOG_CATEGORY_AUDIO);
            }
            if (this == SDL_LOG_CATEGORY_VIDEO)
            {
                return nameof(SDL_LOG_CATEGORY_VIDEO);
            }
            if (this == SDL_LOG_CATEGORY_RENDER)
            {
                return nameof(SDL_LOG_CATEGORY_RENDER);
            }
            if (this == SDL_LOG_CATEGORY_INPUT)
            {
                return nameof(SDL_LOG_CATEGORY_INPUT);
            }
            if (this == SDL_LOG_CATEGORY_TEST)
            {
                return nameof(SDL_LOG_CATEGORY_TEST);
            }
            if (this == SDL_LOG_CATEGORY_GPU)
            {
                return nameof(SDL_LOG_CATEGORY_GPU);
            }
            return $"SDL_LogCategory({Value})";
        }
    }

    public static SDL_LogCategory SDL_LOG_CATEGORY_APPLICATION => new(0);
    public static SDL_LogCategory SDL_LOG_CATEGORY_ERROR => new(1);
    public static SDL_LogCategory SDL_LOG_CATEGORY_ASSERT => new(2);
    public static SDL_LogCategory SDL_LOG_CATEGORY_SYSTEM => new(3);
    public static SDL_LogCategory SDL_LOG_CATEGORY_AUDIO => new(4);
    public static SDL_LogCategory SDL_LOG_CATEGORY_VIDEO => new(5);
    public static SDL_LogCategory SDL_LOG_CATEGORY_RENDER => new(6);
    public static SDL_LogCategory SDL_LOG_CATEGORY_INPUT => new(7);
    public static SDL_LogCategory SDL_LOG_CATEGORY_TEST => new(8);
    public static SDL_LogCategory SDL_LOG_CATEGORY_GPU => new(9);

    public readonly record struct SDL_LogPriority(int Value)
    {
        [SuppressMessage(
            "Style",
            "IDE0046:Convert to conditional expression",
            Justification = "Consecutive ternary operators are less readable than a series of if statements."
        )]
        public static SDL_LogPriority FromLogger(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                return SDL_LOG_PRIORITY_TRACE;
            }
            if (logger.IsEnabled(LogLevel.Debug))
            {
                return SDL_LOG_PRIORITY_DEBUG;
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                return SDL_LOG_PRIORITY_INFO;
            }
            if (logger.IsEnabled(LogLevel.Warning))
            {
                return SDL_LOG_PRIORITY_WARN;
            }
            if (logger.IsEnabled(LogLevel.Error))
            {
                return SDL_LOG_PRIORITY_ERROR;
            }
            if (logger.IsEnabled(LogLevel.Critical))
            {
                return SDL_LOG_PRIORITY_CRITICAL;
            }
            return SDL_LOG_PRIORITY_INVALID;
        }

        [SuppressMessage(
            "Style",
            "IDE0046:Convert to conditional expression",
            Justification = "Consecutive ternary operators are less readable than a series of if statements."
        )]
        public readonly LogLevel ToLogLevel()
        {
            if (this == SDL_LOG_PRIORITY_TRACE || this == SDL_LOG_PRIORITY_VERBOSE)
            {
                return LogLevel.Trace;
            }
            if (this == SDL_LOG_PRIORITY_DEBUG)
            {
                return LogLevel.Debug;
            }
            if (this == SDL_LOG_PRIORITY_INFO)
            {
                return LogLevel.Information;
            }
            if (this == SDL_LOG_PRIORITY_WARN)
            {
                return LogLevel.Warning;
            }
            if (this == SDL_LOG_PRIORITY_ERROR)
            {
                return LogLevel.Error;
            }
            if (this == SDL_LOG_PRIORITY_CRITICAL)
            {
                return LogLevel.Critical;
            }
            return LogLevel.None;
        }
    }

    public static SDL_LogPriority SDL_LOG_PRIORITY_INVALID => new(0);
    public static SDL_LogPriority SDL_LOG_PRIORITY_TRACE => new(1);
    public static SDL_LogPriority SDL_LOG_PRIORITY_VERBOSE => new(2);
    public static SDL_LogPriority SDL_LOG_PRIORITY_DEBUG => new(3);
    public static SDL_LogPriority SDL_LOG_PRIORITY_INFO => new(4);
    public static SDL_LogPriority SDL_LOG_PRIORITY_WARN => new(5);
    public static SDL_LogPriority SDL_LOG_PRIORITY_ERROR => new(6);
    public static SDL_LogPriority SDL_LOG_PRIORITY_CRITICAL => new(7);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogPriorities(SDL_LogPriority prirority);

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogOutputFunction(
        delegate* unmanaged[Cdecl]<void*, SDL_LogCategory, SDL_LogPriority, byte*, void> callback,
        void* userData
    );

    #endregion // SDL_log.h

    #region SDL_main.h

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetMainReady();

    #endregion // SDL_main.h

    #region SDL_messagebox.h

    public readonly record struct SDL_MessageBoxFlags(uint Value);

    public static SDL_MessageBoxFlags SDL_MESSAGEBOX_ERROR => new(0x0000_0010U);

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SDL_ShowSimpleMessageBox(
        SDL_MessageBoxFlags flags,
        string title,
        string message,
        SDL_Window window
    );

    #endregion // SDL_messagebox.h

    #region SDL_pixels.h

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FColor
    {
        public float R;
        public float G;
        public float B;
        public float A;
    }

    #endregion // SDL_pixels.h

    #region SDL_video.h

    public readonly record struct SDL_WindowFlags(ulong Value);

    public static SDL_WindowFlags SDL_WINDOW_FULLSCREEN => new(0x0000_0000_0000_0001UL);

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_Window>))]
    public sealed class SDL_Window : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SDL_Window()
            : base(ownsHandle: true) => SetHandle(0);

        protected override bool ReleaseHandle()
        {
            try
            {
                SDL_DestroyWindow(handle);
            }
            catch
            {
                // If we fail to destroy the window something went really wrong.
                // Returning false will throw an exception, which is what we want in this case.
                return false;
            }

            return true;
        }
    }

    [LibraryImport(LibSdl3, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_Window SDL_CreateWindow(
        string title,
        int w,
        int h,
        SDL_WindowFlags flags
    );

    [LibraryImport(LibSdl3)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_DestroyWindow(nint window);

    #endregion // SDL_video.h
}
