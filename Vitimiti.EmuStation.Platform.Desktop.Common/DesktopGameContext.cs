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
using System.Reflection;
using Microsoft.Extensions.Logging;
using Vitimiti.EmuStation.Common;
using Vitimiti.EmuStation.Platform.Desktop.Common.Internals;
using Vitimiti.EmuStation.Platform.Desktop.Common.Internals.SdlSafeObjects;
using static Vitimiti.EmuStation.Platform.Desktop.Common.NativeInterop.Ffi;

namespace Vitimiti.EmuStation.Platform.Desktop.Common;

public class DesktopGameContext(ILogger<DesktopGameContext> logger) : IGameContext
{
    private const string AppNameMetadataKey = "AppName";
    private const string AppVersionMetadataKey = "AppVersion";
    private const string AppIdentifierMetadataKey = "AppIdentifier";

    private SdlLog? _sdlLog;
    private bool _disposedValue;

    public void Run() => Initialize();

    #region Initialization

    private static string? GetAssemblyMetadata(Assembly assembly, string key) =>
        assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(metadata => string.Equals(metadata.Key, key, StringComparison.Ordinal))
            .Select(metadata => metadata.Value)
            .FirstOrDefault();

    [MemberNotNull(nameof(_sdlLog))]
    private void Initialize()
    {
        SetUnhandledExceptionHandler();
        InitializeSdl();
    }

    private void SetUnhandledExceptionHandler()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            using SDL_Window nullWindow = new();
            if (e.ExceptionObject is Exception ex)
            {
                Log.UnhandledException(logger, ex);
                try
                {
                    SDL_ShowSimpleMessageBox(
                        SDL_MESSAGEBOX_ERROR,
                        "Unhandled Exception",
                        $"{ex}",
                        nullWindow
                    );
                }
                catch (Exception innerEx)
                {
                    // If we fail to show the message box, we can't do much else. Just log the error and continue.
                    Log.UnhandledException(logger, innerEx);
                }
            }
            else
            {
                Log.UnhandledUnknownException(logger, e.ExceptionObject);
                try
                {
                    SDL_ShowSimpleMessageBox(
                        SDL_MESSAGEBOX_ERROR,
                        "Unhandled Exception",
                        $"An unhandled, unknown exception object was thrown: {e.ExceptionObject}",
                        nullWindow
                    );
                }
                catch (Exception innerEx)
                {
                    // If we fail to show the message box, we can't do much else. Just log the error and continue.
                    Log.UnhandledException(logger, innerEx);
                }
            }
        };
    }

    #endregion

    [MemberNotNull(nameof(_sdlLog))]
    private void InitializeSdl()
    {
        SDL_SetMainReady();
        _sdlLog = new SdlLog(logger);

        var metadataAssembly = typeof(DesktopGameContext).Assembly;
        var appName = GetAssemblyMetadata(metadataAssembly, AppNameMetadataKey);
        var appVersion = GetAssemblyMetadata(metadataAssembly, AppVersionMetadataKey);
        var appIdentifier = GetAssemblyMetadata(metadataAssembly, AppIdentifierMetadataKey);
        if (!SDL_SetAppMetadata(appName, appVersion, appIdentifier))
        {
            throw new InvalidOperationException(
                $"Failed to set application metadata: {SDL_GetError()}."
            );
        }

        if (!SDL_InitSubSystem(SDL_INIT_VIDEO | SDL_INIT_AUDIO))
        {
            throw new InvalidOperationException(
                $"Failed to initialize SDL subsystems: {SDL_GetError()}."
            );
        }
    }

    #region IDisposable Support

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _sdlLog?.Dispose();
        }

        SDL_Quit();
        _sdlLog = null;

        _disposedValue = true;
    }

    ~DesktopGameContext()
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

    #endregion // IDisposable Support
}
