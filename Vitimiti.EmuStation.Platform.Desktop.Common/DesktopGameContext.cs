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

using Microsoft.Extensions.Logging;
using Vitimiti.EmuStation.Common;
using Vitimiti.EmuStation.Platform.Desktop.Common.Internals.SdlSafeObjects;
using static Vitimiti.EmuStation.Platform.Desktop.Common.NativeInterop.Ffi;

namespace Vitimiti.EmuStation.Platform.Desktop.Common;

public class DesktopGameContext(ILogger<DesktopGameContext> logger) : IGameContext
{
    private SdlLog? _sdlLog;
    private bool _disposedValue;

    public void Initialize()
    {
        SDL_SetMainReady();
        _sdlLog = new SdlLog(logger);
    }

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
}
