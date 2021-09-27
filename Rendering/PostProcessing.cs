using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class PostProcessing : IDisposable
    {
        Shader postPocessingShader;
        int postPocessingMapLocation;
        int postPocessingMatrixModelLocation;
        int postPocessingMatrixViewLocation;
        int postPocessingMatrixProjectionLocation;
        BufferObject<float> postPocessingVBO;
        BufferObject<uint> postPocessingEBO;
        VertexArrayObject<float, uint> postPocessingVAO;
        readonly float[] postPocessingVertices = { -1f, -1f, 0f, 0f, 0f, 1, -1, 0f, 1f, 0f, -1f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f };
        readonly uint[] postPocessingIndices = { 0u, 1u, 2u, 1u, 3u, 2u };

        public PostProcessing()
        {
            postPocessingShader = SceneManager.AddCommonShader("PostProcessingVert.glsl", "PostProcessingFrag.glsl");
            postPocessingMapLocation = postPocessingShader.GetUniformLocation("_BaseMap");
            postPocessingMatrixModelLocation = postPocessingShader.GetUniformLocation("MatrixModel");
            postPocessingMatrixViewLocation = postPocessingShader.GetUniformLocation("MatrixView");
            postPocessingMatrixProjectionLocation = postPocessingShader.GetUniformLocation("MatrixProjection");
            postPocessingVBO = new BufferObject<float>(postPocessingVertices, BufferTargetARB.ArrayBuffer);
            postPocessingEBO = new BufferObject<uint>(postPocessingIndices, BufferTargetARB.ElementArrayBuffer);
            postPocessingVAO = new VertexArrayObject<float, uint>(postPocessingVBO, postPocessingEBO);
            postPocessingVAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 5, 0);
            postPocessingVAO.VertexAttributePointer<float>(1, 2, VertexAttribPointerType.Float, 5, 3);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public unsafe void DoPostProcessing(uint colorTexture, uint depthTexture)
        {
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(App.Instance.MainWindow.Window.Size);
            postPocessingVAO.Bind();
            postPocessingShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorTexture);
            postPocessingShader.SetInt(postPocessingMapLocation, 0);
            postPocessingShader.SetMatrix(postPocessingMatrixViewLocation, MathHelper.LookAt(math.forward(), float3.zero, math.up()));
            var aspect = (float)App.Instance.MainWindow.Window.Size.X / App.Instance.MainWindow.Window.Size.Y;
            if (aspect > App.Instance.MainWindow.Aspect)
            {
                postPocessingShader.SetMatrix(postPocessingMatrixModelLocation, float4x4.Scale(-App.Instance.MainWindow.Aspect, 1f, 1f));
                postPocessingShader.SetMatrix(postPocessingMatrixProjectionLocation, MathHelper.ortho(-aspect, aspect, -1f, 1f, 0.1f, 2f));
            }
            else
            {
                postPocessingShader.SetMatrix(postPocessingMatrixModelLocation, float4x4.Scale(-1f, 1f / App.Instance.MainWindow.Aspect, 1f));
                postPocessingShader.SetMatrix(postPocessingMatrixProjectionLocation, MathHelper.ortho(-1f, 1f, -1f / aspect, 1f / aspect, 0.1f, 2f));
            }
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)postPocessingIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
        }

        public void Dispose()
        {
            postPocessingVBO.Dispose();
            postPocessingEBO.Dispose();
            postPocessingVAO.Dispose();
        }
    }
}
