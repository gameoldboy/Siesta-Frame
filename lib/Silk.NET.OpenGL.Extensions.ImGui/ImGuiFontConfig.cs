using System;

namespace Silk.NET.OpenGL.Extensions.ImGui
{
    public readonly struct ImGuiFontConfig
    {
        public ImGuiFontConfig(string fontPath, int fontSize)
        {
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            FontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
            FontBuffer = new byte[0];
            FontSize = fontSize;
        }

        public ImGuiFontConfig(byte[] fontBuffer, int fontSize)
        {
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            FontPath = null;
            FontBuffer = fontBuffer;
            FontSize = fontSize;
        }

        public string FontPath { get; }
        public byte[] FontBuffer { get; }
        public int FontSize { get; }
    }
}
