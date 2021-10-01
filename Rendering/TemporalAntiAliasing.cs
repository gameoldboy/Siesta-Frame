using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class TemporalAntiAliasing : IDisposable
    {
        uint historyMapA;
        uint historyMapB;

        Shader shader;

        int baseMapLocation;
        int depthTextureLocation;
        int historyMapLocation;
        int motionVectorMapLocation;
        int jitterLocation;

        HaltonSequence haltonSequence;

        bool swapHistory;

        public TemporalAntiAliasing()
        {
            Alloc();

            shader = SceneManager.AddCommonShader("TemporalAntiAliasingVert.glsl", "TemporalAntiAliasingFrag.glsl");

            baseMapLocation = shader.GetUniformLocation("_BaseMap");
            depthTextureLocation = shader.GetUniformLocation("_DepthTexture");
            historyMapLocation = shader.GetUniformLocation("_HistoryMap");
            motionVectorMapLocation = shader.GetUniformLocation("_MotionVectorMap");
            jitterLocation = shader.GetUniformLocation("_Jitter");

            haltonSequence = new HaltonSequence(1024);
        }

        public unsafe void Alloc()
        {
            if (historyMapA > 0)
            {
                GraphicsAPI.GL.DeleteTexture(historyMapA);
            }
            if (historyMapB > 0)
            {
                GraphicsAPI.GL.DeleteTexture(historyMapB);
            }
            var width = (uint)App.Instance.MainWindow.Width;
            var heigth = (uint)App.Instance.MainWindow.Height;
            historyMapA = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, historyMapA);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, width, heigth, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            historyMapB = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, historyMapB);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, width, heigth, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void PreTemporalAntiAliasing(Camera mainCamera)
        {
            var matrix = mainCamera.ProjectionMatrix;
            haltonSequence.Get(out var jitterX, out var jitterY);
            matrix[2][0] += (jitterX * 2 - 1) / App.Instance.MainWindow.Width;
            matrix[2][1] += (jitterY * 2 - 1) / App.Instance.MainWindow.Height;
            mainCamera.JitterProjectionMatrix = matrix;
        }

        public unsafe void DoTemporalAntiAliasing(PostProcessing postProcessing, uint colorAttachment, uint depthAttachment, MotionVector motionVector)
        {
            uint width = (uint)App.Instance.MainWindow.Width;
            uint height = (uint)App.Instance.MainWindow.Height;
            if (swapHistory)
            {
                App.Instance.MainWindow.BindFrameBuffer(historyMapA);
            }
            else
            {
                App.Instance.MainWindow.BindFrameBuffer(historyMapB);
            }
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width, height);
            var quad = postProcessing.BindFullScreenQuad();
            shader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            shader.SetInt(baseMapLocation, 0);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, depthAttachment);
            shader.SetInt(depthTextureLocation, 1);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture2);
            if (swapHistory)
            {
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, historyMapB);
            }
            else
            {
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, historyMapA);
            }
            shader.SetInt(historyMapLocation, 2);
            motionVector.BindMotionVector(TextureUnit.Texture3);
            shader.SetInt(motionVectorMapLocation, 3);
            haltonSequence.Peek(out var jitterX, out var jitterY);
            haltonSequence.PeekPrev(out var prevJitterX, out var prevJitterY);
            shader.SetVector(jitterLocation, new float4(
                    (jitterX - 0.5f) / width,
                    (jitterY - 0.5f) / height,
                    (prevJitterX - 0.5f) / width,
                    (prevJitterY - 0.5f) / height));
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, quad, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            if (swapHistory)
            {
                postProcessing.Blit(historyMapA, colorAttachment, width, height);
            }
            else
            {
                postProcessing.Blit(historyMapB, colorAttachment, width, height);
            }
            swapHistory = !swapHistory;
        }

        struct HaltonSequence
        {
            int count;
            int index;
            float[] arrX;
            float[] arrY;
            public HaltonSequence(int count)
            {
                this.count = count;
                index = 0;
                arrX = new float[count];
                arrY = new float[count];
                for (int i = 0; i < arrX.Length; i++)
                {
                    arrX[i] = get(i, 2);
                }
                for (int i = 0; i < arrY.Length; i++)
                {
                    arrY[i] = get(i, 3);
                }
            }
            float get(int index, int @base)
            {
                float fraction = 1;
                float result = 0;
                while (index > 0)
                {
                    fraction /= @base;
                    result += fraction * (index % @base);
                    index /= @base;
                }
                return result;
            }
            public void Get(out float x, out float y)
            {
                if (++index == count) index = 1;
                x = arrX[index];
                y = arrY[index];
            }
            public void Peek(out float x, out float y)
            {
                x = arrX[index];
                y = arrY[index];
            }
            public void PeekPrev(out float x, out float y)
            {
                x = arrX[index - 1];
                y = arrY[index - 1];
            }
        }

        public float2 GetJitter2()
        {
            haltonSequence.Peek(out var jitterX, out var jitterY);
            return new float2(jitterX - 0.5f, jitterY - 0.5f);
        }
        public float4 GetJitter4()
        {
            haltonSequence.Peek(out var jitterX, out var jitterY);
            haltonSequence.PeekPrev(out var PrevJitterX, out var PrevJitterY);
            return new float4(jitterX - 0.5f, jitterY - 0.5f, PrevJitterX - 0.5f, PrevJitterY - 0.5f);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(historyMapA);
            GraphicsAPI.GL.DeleteTexture(historyMapB);
        }
    }
}
