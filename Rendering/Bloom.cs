using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Bloom : IDisposable
    {
        uint[] textures;

        Shader thresholdShader;
        Shader downShader;
        Shader upShader;
        Shader finalShader;

        uint width;
        uint height;

        int thresholdBaseMapLocation;
        int thresholdLocation;

        int downBaseMapLocation;
        int downSizeLocation;

        int upBaseMapLocation;
        int upSizeLocation;

        int finalBaseMapLocation;
        int finalMapLocation;
        int intensityLocation;

        public float Threshold { get; set; }
        public float Intensity { get; set; }

        public unsafe Bloom(int iterations)
        {
            width = (uint)(App.Instance.MainWindow.Aspect * 720);
            height = 720;

            textures = new uint[iterations];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = GraphicsAPI.GL.GenTexture();
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, textures[i]);
                GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, (uint)(width / math.pow(2, i + 1)), (uint)(height / math.pow(2, i + 1)), 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            }
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);

            thresholdShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomThresholdFrag.glsl");
            downShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomDownFrag.glsl");
            upShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomUpFrag.glsl");
            finalShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomFinalFrag.glsl");

            thresholdBaseMapLocation = thresholdShader.GetUniformLocation("_baseMap");
            thresholdLocation = thresholdShader.GetUniformLocation("_Threshold");

            downBaseMapLocation = downShader.GetUniformLocation("_baseMap");
            downSizeLocation = downShader.GetUniformLocation("_Size");

            upBaseMapLocation = upShader.GetUniformLocation("_baseMap");
            upSizeLocation = upShader.GetUniformLocation("_Size");

            finalBaseMapLocation = finalShader.GetUniformLocation("_baseMap");
            finalMapLocation = finalShader.GetUniformLocation("_FinalMap");
            intensityLocation = finalShader.GetUniformLocation("_Intensity");

            Threshold = 1f;
            Intensity = 1f;
        }

        public unsafe void DoBloom(PostProcessing postProcessing, uint colorAttachment)
        {
            var ww = (uint)App.Instance.MainWindow.Width;
            var wh = (uint)App.Instance.MainWindow.Height;
            // threshold
            App.Instance.MainWindow.BindFrameBuffer(App.Instance.MainWindow.TempColorAttachment);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, ww, wh);
            var quad = postProcessing.BindFullScreenQuad();
            thresholdShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            thresholdShader.SetInt(thresholdBaseMapLocation, 0);
            thresholdShader.SetFloat(thresholdLocation, Threshold);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            // iteration downsampling
            for (int i = 0; i < textures.Length; i++)
            {
                var w = (uint)(width / math.pow(2, i + 1));
                var h = (uint)(height / math.pow(2, i + 1));

                App.Instance.MainWindow.BindFrameBuffer(textures[i]);
                GraphicsAPI.GL.Viewport(0, 0, w, h);
                downShader.Use();
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
                if (i == 0)
                {
                    GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, App.Instance.MainWindow.TempColorAttachment);
                }
                else
                {
                    GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, textures[i - 1]);
                }
                downShader.SetInt(downBaseMapLocation, 0);
                downShader.SetVector(downSizeLocation, new float2(w, h));
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }
            // iteration upsampling
            for (int i = textures.Length - 2; i >= 0; i--)
            {
                var w = (uint)(width / math.pow(2, i + 1));
                var h = (uint)(height / math.pow(2, i + 1));

                App.Instance.MainWindow.BindFrameBuffer(textures[i]);
                GraphicsAPI.GL.Viewport(0, 0, w, h);
                upShader.Use();
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, textures[i + 1]);
                upShader.SetInt(upBaseMapLocation, 0);
                upShader.SetVector(upSizeLocation, new float2(w, h));
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }
            //final pass
            App.Instance.MainWindow.BindFrameBuffer(App.Instance.MainWindow.TempColorAttachment);
            GraphicsAPI.GL.Viewport(0, 0, ww, wh);
            finalShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            finalShader.SetInt(finalBaseMapLocation, 0);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, textures[0]);
            finalShader.SetInt(finalMapLocation, 1);
            finalShader.SetFloat(intensityLocation, Intensity);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            //blit
            postProcessing.Blit(App.Instance.MainWindow.TempColorAttachment, colorAttachment, ww, wh);
        }

        public void Dispose()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                GraphicsAPI.GL.DeleteTexture(textures[i]);
            }
            thresholdShader.Dispose();
            downShader.Dispose();
            upShader.Dispose();
            finalShader.Dispose();
        }
    }
}
