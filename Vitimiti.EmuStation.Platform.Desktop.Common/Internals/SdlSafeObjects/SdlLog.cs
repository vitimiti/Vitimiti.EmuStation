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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Extensions.Logging;
using static Vitimiti.EmuStation.Platform.Desktop.Common.NativeInterop.Ffi;

namespace Vitimiti.EmuStation.Platform.Desktop.Common.Internals.SdlSafeObjects;

internal sealed partial class SdlLog : IDisposable
{
    private delegate void LogFunction(
        SDL_LogCategory category,
        SDL_LogPriority priority,
        string message
    );

    private GCHandle _handle;
    private bool _disposedValue;

    public SdlLog(ILogger logger)
    {
        SDL_SetLogPriorities(SDL_LogPriority.FromLogger(logger));
        _handle = GCHandle.Alloc(
            new LogFunction(
                (category, priority, message) =>
                {
                    var level = priority.ToLogLevel();
                    LogSdlMessage(logger, level, category, message);
                }
            )
        );

        unsafe
        {
            SDL_SetLogOutputFunction(&LogCallback, (void*)GCHandle.ToIntPtr(_handle));
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            // Nothing to do here, but if there were managed resources, they would be disposed of here.
        }

        if (_handle.IsAllocated)
        {
            _handle.Free();
        }

        _disposedValue = true;
    }

    ~SdlLog()
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void LogCallback(
        void* userData,
        SDL_LogCategory category,
        SDL_LogPriority priority,
        byte* message
    )
    {
        if (userData is null)
        {
            return;
        }

        var handle = GCHandle.FromIntPtr((nint)userData);
        if (!handle.IsAllocated || handle.Target is not LogFunction callback)
        {
            return;
        }

        var messageString = Utf8StringMarshaller.ConvertToManaged(message) ?? string.Empty;
        callback(category, priority, messageString);
    }

    [LoggerMessage(EventId = 9000, Message = "[{Category}] {Message}")]
    private static partial void LogSdlMessage(
        ILogger logger,
        LogLevel logLevel,
        SDL_LogCategory category,
        string message
    );
}
