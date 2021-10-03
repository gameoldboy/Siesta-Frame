using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Bloom : IDisposable
    {
        uint[] downTextures;
        uint[] upTextures;

        Shader thresholdShader;
        Shader downShader;
        Shader upShader;
        Shader finalShader;

        int thresholdBaseMapLocation;
        int thresholdLocation;

        int downBaseMapLocation;
        int downSizeLocation;

        int upBaseMapLocation;
        int upPrevMapLocation;
        int upSizeLocation;

        int finalBaseMapLocation;
        int finalMapLocation;
        int intensityLocation;

        public float Threshold { get; set; }
        public float Intensity { get; set; }

        public Bloom()
        {
            Alloc();

            thresholdShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomThresholdFrag.glsl");
            downShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomDownFrag.glsl");
            upShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomUpFrag.glsl");
            finalShader = SceneManager.AddCommonShader("BloomVert.glsl", "BloomFinalFrag.glsl");

            thresholdBaseMapLocation = thresholdShader.GetUniformLocation("_baseMap");
            thresholdLocation = thresholdShader.GetUniformLocation("_Threshold");

            downBaseMapLocation = downShader.GetUniformLocation("_baseMap");
            downSizeLocation = downShader.GetUniformLocation("_Size");

            upBaseMapLocation = upShader.GetUniformLocation("_baseMap");
            upPrevMapLocation = upShader.GetUniformLocation("_PrevMap");
            upSizeLocation = upShader.GetUniformLocation("_Size");

            finalBaseMapLocation = finalShader.GetUniformLocation("_baseMap");
            finalMapLocation = finalShader.GetUniformLocation("_FinalMap");
            intensityLocation = finalShader.GetUniformLocation("_Intensity");

            Threshold = 1f;
            Intensity = 1f;
        }

        public unsafe void Alloc()
        {
            if (downTextures != null)
            {
                for (int i = 0; i < downTextures.Length; i++)
                {
                    GraphicsAPI.GL.DeleteTexture(downTextures[i]);
                }
            }
            if (upTextures != null)
            {
                for (int i = 0; i < upTextures.Length; i++)
                {
                    GraphicsAPI.GL.DeleteTexture(upTextures[i]);
                }
            }
            var width = App.Instance.MainWindow.Width;
            var height = App.Instance.MainWindow.Height;
            var maxIterations = 0;
            for (int i = 0; i < 1000; i++)
            {
                var w = (int)(width / math.pow(2, i + 1));
                var h = (int)(height / math.pow(2, i + 1));
                if (w < 10 || h < 10)
                {
                    break;
                }
                maxIterations++;
            }
            Console.WriteLine($"Bloom max iterations:{maxIterations}");
            downTextures = new uint[maxIterations];
            upTextures = new uint[maxIterations - 1];
            for (int i = 0; i < downTextures.Length; i++)
            {
                downTextures[i] = GraphicsAPI.GL.GenTexture();
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, downTextures[i]);
                GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, (uint)(width / math.pow(2, i + 1)), (uint)(height / math.pow(2, i + 1)), 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            }
            for (int i = 0; i < upTextures.Length; i++)
            {
                upTextures[i] = GraphicsAPI.GL.GenTexture();
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, upTextures[i]);
                GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, (uint)(width / math.pow(2, i + 1)), (uint)(height / math.pow(2, i + 1)), 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
                GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            }
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public unsafe void DoBloom(PostProcessing postProcessing, uint colorAttachment)
        {
            var width = App.Instance.MainWindow.Width;
            var height = App.Instance.MainWindow.Height;
            //threshold pass
            App.Instance.MainWindow.BindFrameBuffer(downTextures[0]);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width / 2, height / 2);
            var quad = postProcessing.BindFullScreenQuad();
            thresholdShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            thresholdShader.SetInt(thresholdBaseMapLocation, 0);
            thresholdShader.SetFloat(thresholdLocation, Threshold);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            //iteration downsampling
            for (int i = 1; i < downTextures.Length; i++)
            {
                var w = (uint)(width / math.pow(2, i + 1));
                var h = (uint)(height / math.pow(2, i + 1));

                App.Instance.MainWindow.BindFrameBuffer(downTextures[i]);
                GraphicsAPI.GL.Viewport(0, 0, w, h);
                downShader.Use();
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, downTextures[i - 1]);
                downShader.SetInt(downBaseMapLocation, 0);
                downShader.SetVector(downSizeLocation, new float2(w, h));
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }
            //iteration upsampling
            for (int i = upTextures.Length - 1; i >= 0; i--)
            {
                var w = (uint)(width / math.pow(2, i + 1));
                var h = (uint)(height / math.pow(2, i + 1));

                App.Instance.MainWindow.BindFrameBuffer(upTextures[i]);
                GraphicsAPI.GL.Viewport(0, 0, w, h);
                upShader.Use();
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
                if (i == upTextures.Length - 1)
                {
                    GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, downTextures[i + 1]);
                }
                else
                {
                    GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, upTextures[i + 1]);
                }
                upShader.SetInt(upBaseMapLocation, 0);
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, downTextures[i]);
                upShader.SetInt(upPrevMapLocation, 1);
                upShader.SetVector(upSizeLocation, new float2(w, h));
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }
            //final pass
            App.Instance.MainWindow.BindFrameBuffer(App.Instance.MainWindow.TempColorAttachment);
            GraphicsAPI.GL.Viewport(0, 0, width, height);
            finalShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            finalShader.SetInt(finalBaseMapLocation, 0);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, upTextures[0]);
            finalShader.SetInt(finalMapLocation, 1);
            finalShader.SetFloat(intensityLocation, Intensity);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            //blit
            postProcessing.Blit(App.Instance.MainWindow.TempColorAttachment, colorAttachment, width, height);
        }

        public void Dispose()
        {
            for (int i = 0; i < downTextures.Length; i++)
            {
                GraphicsAPI.GL.DeleteTexture(downTextures[i]);
            }
            for (int i = 0; i < upTextures.Length; i++)
            {
                GraphicsAPI.GL.DeleteTexture(upTextures[i]);
            }
            thresholdShader.Dispose();
            downShader.Dispose();
            upShader.Dispose();
            finalShader.Dispose();
        }
    }
}
