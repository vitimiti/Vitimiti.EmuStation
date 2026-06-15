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
using static Vitimiti.EmuStation.Platform.Desktop.Common.NativeInterop.Ffi;

namespace Vitimiti.EmuStation.Platform.Desktop.Common.Internals.SdlSafeObjects;

internal sealed class SdlScreenDevice : IDisposable
{
    private bool _disposedValue;

    private SDL_GPUDevice? _gpuDevice;
    private SDL_Window? _window;

    [MemberNotNull(nameof(_window), nameof(_gpuDevice))]
    public void Initialize(string? appName)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
#if DEBUG || INTERNAL
        const bool debugMode = true;
#else
        const bool debugMode = false;
#endif
        _gpuDevice = SDL_CreateGPUDevice(
            SDL_GPU_SHADERFORMAT_SPIRV
                | SDL_GPU_SHADERFORMAT_DXBC
                | SDL_GPU_SHADERFORMAT_DXIL
                | SDL_GPU_SHADERFORMAT_MSL
                | SDL_GPU_SHADERFORMAT_METALLIB,
            debugMode,
            name: null
        );
        if (_gpuDevice.IsInvalid)
        {
            throw new InvalidOperationException(
                $"Failed to create SDL GPU device: {SDL_GetError()}."
            );
        }

        _window = SDL_CreateWindow(
            appName ?? "Vitimiti.EmuStation",
            1280,
            720,
            SDL_WINDOW_FULLSCREEN
        );
        if (_window.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create SDL window: {SDL_GetError()}.");
        }

        if (!SDL_ClaimWindowForGPUDevice(_gpuDevice, _window))
        {
            throw new InvalidOperationException(
                $"Failed to claim SDL window for GPU device: {SDL_GetError()}."
            );
        }
    }

    public void Render()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (_gpuDevice is null)
        {
            throw new InvalidOperationException("GPU device is not initialized.");
        }
        if (_window is null)
        {
            throw new InvalidOperationException("Window is not initialized.");
        }

        var commandBuffer = SDL_AcquireGPUCommandBuffer(_gpuDevice);
        if (commandBuffer.IsInvalid)
        {
            throw new InvalidOperationException(
                $"Failed to acquire GPU command buffer: {SDL_GetError()}."
            );
        }

        if (
            !SDL_WaitAndAcquireGPUSwapchainTexture(
                commandBuffer,
                _window,
                out var swapChainTexture,
                out _,
                out _
            )
        )
        {
            return; // This is not always an error, it can happen if the window is minimized or not visible.
        }

        SDL_GPUColorTargetInfo colorTargetInfo = new()
        {
            Texture = swapChainTexture,
            ClearColor = new SDL_FColor()
            {
                R = 45 / 255F,
                G = 40 / 255F,
                B = 58 / 255F,
                A = 1F,
            },
            LoadOp = SDL_GPU_LOADOP_CLEAR,
            StoreOp = SDL_GPU_STOREOP_STORE,
        };

        SDL_GPURenderPass renderPass;
        unsafe
        {
            renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);
        }
        SDL_EndGPURenderPass(renderPass);

        if (!SDL_SubmitGPUCommandBuffer(commandBuffer))
        {
            throw new InvalidOperationException(
                $"Failed to submit GPU command buffer: {SDL_GetError()}."
            );
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (_gpuDevice is not null && _window is not null)
        {
            SDL_WaitForGPUIdle(_gpuDevice);
            SDL_ReleaseWindowFromGPUDevice(_gpuDevice, _window);
        }

        if (disposing)
        {
            _window?.Dispose();
            _gpuDevice?.Dispose();
        }

        _window = null;
        _gpuDevice = null;

        _disposedValue = true;
    }

    ~SdlScreenDevice()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
