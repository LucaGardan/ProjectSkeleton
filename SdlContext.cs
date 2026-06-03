using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;

namespace TheAdventure;

public class SdlContext:INativeContext
{
    private readonly IntPtr _nativeLibrary;

    public SdlContext()
    {
        string dllPath=Path.Combine(AppContext.BaseDirectory,"SDL2.dll");
        _nativeLibrary=NativeLibrary.Load(dllPath);
    }

    public IntPtr GetProcAddress(string proc,int? slot=null)
    {
        return NativeLibrary.GetExport(_nativeLibrary,proc);
    }

    public bool TryGetProcAddress(string proc,[UnscopedRef] out IntPtr addr,int? slot=null)
    {
        return NativeLibrary.TryGetExport(_nativeLibrary,proc,out addr);
    }

    public void Dispose()
    {
        if(_nativeLibrary!=IntPtr.Zero)
            NativeLibrary.Free(_nativeLibrary);

        GC.SuppressFinalize(this);
    }
}