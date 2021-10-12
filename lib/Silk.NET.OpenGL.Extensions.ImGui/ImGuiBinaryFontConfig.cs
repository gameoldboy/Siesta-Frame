using System;
using System.Runtime.InteropServices;

namespace Silk.NET.OpenGL.Extensions.ImGui
{
    public readonly struct ImGuiBinaryFontConfig
    {
        public ImGuiBinaryFontConfig(Span<byte> buffer, int fontSize)
        {
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            unsafe
            {
                fixed (byte* b = &buffer[0])
                {
                    FontBuffer = (IntPtr)b;
                    FontBufferSize = buffer.Length;
                }
            }
            FontSize = fontSize;
        }

        public IntPtr FontBuffer { get; }
        public int FontBufferSize { get; }
        public int FontSize { get; }
    }
}
