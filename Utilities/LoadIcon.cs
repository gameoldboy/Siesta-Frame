using Silk.NET.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.InteropServices;

namespace SiestaFrame
{
    public partial class Utilities
    {
        public static unsafe RawImage LoadIcon(byte[] buffer)
        {
            var image = Image.Load<Rgba32>(buffer);
            image.Mutate(x => x.Resize(48, 48, KnownResamplers.Lanczos3));
            var memoryGroup = image.GetPixelMemoryGroup();
            Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
            var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
            foreach (var memory in memoryGroup)
            {
                memory.Span.CopyTo(block);
                block = block.Slice(memory.Length);
            }
            return new RawImage(image.Width, image.Height, array);
        }
    }
}
