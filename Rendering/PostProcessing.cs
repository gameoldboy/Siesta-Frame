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
        Shader sightShader;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseMapLocation;
        int depthTextureLocation;
        int motionVectorMapLocation;
        int blitBaseMapLocation;
        int tonemappingLocation;
        int sightMatrixModelLocation;
        int sightMatrixViewLocation;
        int sightMatrixProjectionLocation;
        int sightColorLocation;

        VertexArrayObject fullScreenQuadVAO;
        BufferObject<float> fullScreenQuadVBO;
        BufferObject<uint> fullScreenQuadEBO;
        readonly float[] fullScreenQuadVertices = { -1f, -1f, 0f, 0f, 0f, 1f, -1f, 0f, 1f, 0f, -1f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f };
        readonly uint[] fullScreenQuadIndices = { 0, 1, 2, 1, 3, 2 };

        VertexArrayObject sightVAO;
        BufferObject<float> sightVBO;
        readonly float[] sightVertices = { -1f, 0f, 0f, 1f, 0f, 0f, 0f, -1f, 0f, 0f, 1f, 0f };

        public Bloom Bloom { get; }

        public bool Tonemapping { get; set; }

        public PostProcessing()
        {
            fullScreenQuadVAO = new VertexArrayObject();
            fullScreenQuadVBO = new BufferObject<float>(BufferTargetARB.ArrayBuffer);
            fullScreenQuadEBO = new BufferObject<uint>(BufferTargetARB.ElementArrayBuffer);
            fullScreenQuadVAO.Bind();
            fullScreenQuadVBO.Bind();
            fullScreenQuadVBO.BufferData(fullScreenQuadVertices);
            fullScreenQuadEBO.Bind();
            fullScreenQuadEBO.BufferData(fullScreenQuadIndices);
            fullScreenQuadVAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 5, 0);
            fullScreenQuadVAO.VertexAttributePointer<float>(1, 2, VertexAttribPointerType.Float, 5, 3);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            GraphicsAPI.GL.DisableVertexAttribArray(0);
            GraphicsAPI.GL.DisableVertexAttribArray(1);
            sightVAO = new VertexArrayObject();
            sightVBO = new BufferObject<float>(BufferTargetARB.ArrayBuffer);
            sightVAO.Bind();
            sightVBO.Bind();
            sightVBO.BufferData(sightVertices);
            sightVAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 3, 0);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GraphicsAPI.GL.DisableVertexAttribArray(0);

            postProcessingShader = SceneManager.AddCommonShader("PostProcessingVert.glsl", "PostProcessingFrag.glsl");
            blitShader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitFrag.glsl");
            sightShader = SceneManager.AddCommonShader("LinesVert.glsl", "LinesFrag.glsl");

            matrixModelLocation = postProcessingShader.GetUniformLocation("MatrixModel");
            matrixViewLocation = postProcessingShader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = postProcessingShader.GetUniformLocation("MatrixProjection");
            baseMapLocation = postProcessingShader.GetUniformLocation("_BaseMap");
            depthTextureLocation = postProcessingShader.GetUniformLocation("_DepthTexture");
            motionVectorMapLocation = postProcessingShader.GetUniformLocation("_MotionVectorMap");
            blitBaseMapLocation = blitShader.GetUniformLocation("_BaseMap");
            Tonemapping = false;
            tonemappingLocation = postProcessingShader.GetUniformLocation("_Tonemap");
            sightMatrixModelLocation = sightShader.GetUniformLocation("MatrixModel");
            sightMatrixViewLocation = sightShader.GetUniformLocation("MatrixView");
            sightMatrixProjectionLocation = sightShader.GetUniformLocation("MatrixProjection");
            sightColorLocation = sightShader.GetUniformLocation("_Color");

            Bloom = new Bloom();
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
            fullScreenQuadVAO.Bind();
            postProcessingShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            postProcessingShader.SetInt(baseMapLocation, 0);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture1);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, depthAttachment);
            postProcessingShader.SetInt(depthTextureLocation, 1);
            motionVector.BindMotionVector(TextureUnit.Texture2);
            postProcessingShader.SetInt(motionVectorMapLocation, 2);
            float4x4 matrixModel;
            var matrixView = MathHelper.LookAt(math.forward(), float3.zero, math.up());
            float4x4 matrixProjection;
            postProcessingShader.SetMatrix(matrixViewLocation, matrixView);
            var aspect = (float)App.Instance.MainWindow.Window.Size.X / App.Instance.MainWindow.Window.Size.Y;
            if (aspect > App.Instance.MainWindow.Aspect)
            {
                matrixModel = float4x4.Scale(-App.Instance.MainWindow.Aspect, 1f, 1f);
                matrixProjection = MathHelper.ortho(-aspect, aspect, -1f, 1f, 0.1f, 2f);
            }
            else
            {
                matrixModel = float4x4.Scale(-1f, 1f / App.Instance.MainWindow.Aspect, 1f);
                matrixProjection = MathHelper.ortho(-1f, 1f, -1f / aspect, 1f / aspect, 0.1f, 2f);
            }
            postProcessingShader.SetMatrix(matrixModelLocation, matrixModel);
            postProcessingShader.SetMatrix(matrixProjectionLocation, matrixProjection);
            postProcessingShader.SetBool(tonemappingLocation, Tonemapping);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)fullScreenQuadIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            sightVAO.Bind();
            sightShader.Use();
            matrixModel = float4x4.Scale(16f, 16f, 1f);
            matrixProjection = MathHelper.ortho(
                -App.Instance.MainWindow.Window.Size.X / 2f,
                App.Instance.MainWindow.Window.Size.X / 2f,
                -App.Instance.MainWindow.Window.Size.Y / 2f,
                App.Instance.MainWindow.Window.Size.Y / 2f, 0.1f, 2f);
            sightShader.SetMatrix(sightMatrixModelLocation, matrixModel);
            sightShader.SetMatrix(sightMatrixViewLocation, matrixView);
            sightShader.SetMatrix(sightMatrixProjectionLocation, matrixProjection);
            sightShader.SetVector(sightColorLocation, new float4(0, 1f, 0, 0.5f));
            GraphicsAPI.GL.Disable(EnableCap.FramebufferSrgb);
            GraphicsAPI.GL.Enable(EnableCap.Blend);
            GraphicsAPI.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GraphicsAPI.GL.DrawArrays(PrimitiveType.Lines, 0, 4);
            GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GraphicsAPI.GL.Disable(EnableCap.Blend);
            GraphicsAPI.GL.Enable(EnableCap.FramebufferSrgb);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public uint BindFullScreenQuad()
        {
            fullScreenQuadVAO.Bind();
            return (uint)fullScreenQuadIndices.Length;
        }

        public unsafe void Blit(uint source, uint destination, uint width, uint height)
        {
            App.Instance.MainWindow.BindFrameBuffer(destination);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width, height);
            fullScreenQuadVAO.Bind();
            blitShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, source);
            blitShader.SetInt(blitBaseMapLocation, 0);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)fullScreenQuadIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public void Dispose()
        {
            fullScreenQuadVAO.Dispose();
            fullScreenQuadVBO.Dispose();
            fullScreenQuadEBO.Dispose();
            sightVAO.Dispose();
            sightVBO.Dispose();
            postProcessingShader.Dispose();
            blitShader.Dispose();
            sightShader.Dispose();
        }
    }
}
