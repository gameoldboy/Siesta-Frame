using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class PostProcessing : IDisposable
    {
        Shader postProcessingShader;
        Shader blitShader;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseMapLocation;
        int depthTextureLocation;
        int motionVectorMapLocation;
        int blitBaseMapLocation;
        int tonemappingLocation;

        VertexArrayObject FullScreenQuadVAO;
        BufferObject<float> FullScreenQuadVBO;
        BufferObject<uint> FullScreenQuadEBO;
        readonly float[] FullScreenQuadVertices = { -1f, -1f, 0f, 0f, 0f, 1, -1, 0f, 1f, 0f, -1f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f };
        readonly uint[] FullScreenQuadIndices = { 0, 1, 2, 1, 3, 2 };

        public Bloom Bloom { get; }

        public bool Tonemapping { get; set; }

        public PostProcessing()
        {
            FullScreenQuadVAO = new VertexArrayObject();
            FullScreenQuadVBO = new BufferObject<float>(BufferTargetARB.ArrayBuffer);
            FullScreenQuadEBO = new BufferObject<uint>(BufferTargetARB.ElementArrayBuffer);
            FullScreenQuadVAO.Bind();
            FullScreenQuadVBO.Bind();
            FullScreenQuadVBO.BufferData(FullScreenQuadVertices);
            FullScreenQuadEBO.Bind();
            FullScreenQuadEBO.BufferData(FullScreenQuadIndices);
            FullScreenQuadVAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 5, 0);
            FullScreenQuadVAO.VertexAttributePointer<float>(1, 2, VertexAttribPointerType.Float, 5, 3);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            GraphicsAPI.GL.UseProgram(0);

            postProcessingShader = SceneManager.AddCommonShader("PostProcessingVert.glsl", "PostProcessingFrag.glsl");
            blitShader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitFrag.glsl");

            matrixModelLocation = postProcessingShader.GetUniformLocation("MatrixModel");
            matrixViewLocation = postProcessingShader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = postProcessingShader.GetUniformLocation("MatrixProjection");
            baseMapLocation = postProcessingShader.GetUniformLocation("_BaseMap");
            depthTextureLocation = postProcessingShader.GetUniformLocation("_DepthTexture");
            motionVectorMapLocation = postProcessingShader.GetUniformLocation("_MotionVectorMap");
            blitBaseMapLocation = blitShader.GetUniformLocation("_BaseMap");
            Tonemapping = false;
            tonemappingLocation = postProcessingShader.GetUniformLocation("_Tonemap");

            Bloom = new Bloom(5);
        }

        public unsafe void DoPostProcessing(uint colorAttachment, uint depthAttachment, MotionVector motionVector, TemporalAntiAliasing temporalAntiAliasing)
        {
            temporalAntiAliasing.DoTemporalAntiAliasing(this, colorAttachment, depthAttachment, motionVector);

            Bloom.DoBloom(this, colorAttachment);

            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(App.Instance.MainWindow.Window.Size);
            FullScreenQuadVAO.Bind();
            postProcessingShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            postProcessingShader.SetInt(baseMapLocation, 0);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, depthAttachment);
            postProcessingShader.SetInt(depthTextureLocation, 1);
            motionVector.BindMotionVector(TextureUnit.Texture2);
            postProcessingShader.SetInt(motionVectorMapLocation, 2);
            postProcessingShader.SetMatrix(matrixViewLocation, MathHelper.LookAt(math.forward(), float3.zero, math.up()));
            var aspect = (float)App.Instance.MainWindow.Window.Size.X / App.Instance.MainWindow.Window.Size.Y;
            if (aspect > App.Instance.MainWindow.Aspect)
            {
                postProcessingShader.SetMatrix(matrixModelLocation, float4x4.Scale(-App.Instance.MainWindow.Aspect, 1f, 1f));
                postProcessingShader.SetMatrix(matrixProjectionLocation, MathHelper.ortho(-aspect, aspect, -1f, 1f, 0.1f, 2f));
            }
            else
            {
                postProcessingShader.SetMatrix(matrixModelLocation, float4x4.Scale(-1f, 1f / App.Instance.MainWindow.Aspect, 1f));
                postProcessingShader.SetMatrix(matrixProjectionLocation, MathHelper.ortho(-1f, 1f, -1f / aspect, 1f / aspect, 0.1f, 2f));
            }
            postProcessingShader.SetBool(tonemappingLocation, Tonemapping);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)FullScreenQuadIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public uint BindFullScreenQuad()
        {
            FullScreenQuadVAO.Bind();
            return (uint)FullScreenQuadIndices.Length;
        }

        public unsafe void Blit(uint source, uint destination, uint width, uint height)
        {
            App.Instance.MainWindow.BindFrameBuffer(destination);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width, height);
            FullScreenQuadVAO.Bind();
            blitShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, source);
            blitShader.SetInt(blitBaseMapLocation, 0);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)FullScreenQuadIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public void Dispose()
        {
            FullScreenQuadVAO.Dispose();
            FullScreenQuadVBO.Dispose();
            FullScreenQuadEBO.Dispose();
            postProcessingShader.Dispose();
            blitShader.Dispose();
        }
    }
}
