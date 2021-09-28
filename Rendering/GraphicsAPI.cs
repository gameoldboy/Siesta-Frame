using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class GraphicsAPI
    {
        public static GL GL { get; set; }
        public static void GetGLError(string name)
        {
            var error = GL.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"{name}:{error}");
            }
        }
    }
}
