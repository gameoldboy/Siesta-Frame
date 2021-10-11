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
        Shader blitMRT2Shader;
        Shader blitMRT3Shader;
        Shader blitMRT4Shader;
        Shader sightShader;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseMapLocation;
        int depthTextureLocation;
        int shadowMapLocation;
        int normalMapLocation;
        int motionVectorMapLocation;
        int bloomMapLocation;
        int bloomIntensityLocation;
        int exposureLocation;
        int tonemappingLocation;
        int colorGradingLocation;
        int temporalJitterLocation;

        int blitBaseMapLocation;
        int blitMRT2BaseMapLocation;
        int blitMRT3BaseMapLocation;
        int blitMRT4BaseMapLocation;

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

        public float Exposure { get; set; }

        public struct ColorGrading
        {
            public float Contrast;
            public float Saturation;
            public float Temperature;
            public float Tint;

            public float4 GetVector()
            {
                return new float4(Contrast, Saturation, Temperature, Tint);
            }
        }

        ColorGrading colorAdjustments;
        public ref ColorGrading ColorAdjustments => ref colorAdjustments;

        public bool Tonemapping { get; set; }

        public PostProcessing()
        {
            fullScreenQuadVAO = new VertexArrayObject();
            fullScreenQuadVAO.Bind();
            fullScreenQuadVBO = new BufferObject<float>(BufferTargetARB.ArrayBuffer);
            fullScreenQuadVBO.Bind();
            fullScreenQuadVBO.BufferData(fullScreenQuadVertices);
            fullScreenQuadEBO = new BufferObject<uint>(BufferTargetARB.ElementArrayBuffer);
            fullScreenQuadEBO.Bind();
            fullScreenQuadEBO.BufferData(fullScreenQuadIndices);
            fullScreenQuadVAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 20, 0);
            fullScreenQuadVAO.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 20, 12);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            sightVAO = new VertexArrayObject();
            sightVAO.Bind();
            sightVBO = new BufferObject<float>(BufferTargetARB.ArrayBuffer);
            sightVBO.Bind();
            sightVBO.BufferData(sightVertices);
            sightVAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 12, 0);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            postProcessingShader = SceneManager.AddCommonShader("PostProcessingVert.glsl", "PostProcessingFrag.glsl");
            blitShader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitFrag.glsl");
            blitMRT2Shader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitMRT2Frag.glsl");
            blitMRT3Shader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitMRT3Frag.glsl");
            blitMRT4Shader = SceneManager.AddCommonShader("BlitVert.glsl", "BlitMRT4Frag.glsl");
            sightShader = SceneManager.AddCommonShader("LinesVert.glsl", "LinesFrag.glsl");

            matrixModelLocation = postProcessingShader.GetUniformLocation("MatrixModel");
            matrixViewLocation = postProcessingShader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = postProcessingShader.GetUniformLocation("MatrixProjection");
            baseMapLocation = postProcessingShader.GetUniformLocation("_BaseMap");
            depthTextureLocation = postProcessingShader.GetUniformLocation("_DepthTexture");
            shadowMapLocation = postProcessingShader.GetUniformLocation("_ShadowMap");
            normalMapLocation = postProcessingShader.GetUniformLocation("_NormalTexture");
            motionVectorMapLocation = postProcessingShader.GetUniformLocation("_MotionVectors");
            bloomMapLocation = postProcessingShader.GetUniformLocation("_BloomMap");
            bloomIntensityLocation = postProcessingShader.GetUniformLocation("_BloomIntensity");
            Exposure = 0;
            exposureLocation = postProcessingShader.GetUniformLocation("_Exposure");
            Tonemapping = true;
            tonemappingLocation = postProcessingShader.GetUniformLocation("_Tonemap");
            ColorGrading colorAdjustments;
            colorAdjustments.Contrast = 0.9f;
            colorAdjustments.Saturation = 1.2f;
            colorAdjustments.Temperature = 0;
            colorAdjustments.Tint = 0;
            this.colorAdjustments = colorAdjustments;
            colorGradingLocation = postProcessingShader.GetUniformLocation("_ColorGrading");
            temporalJitterLocation = postProcessingShader.GetUniformLocation("_Jitter");
            blitBaseMapLocation = blitShader.GetUniformLocation("_BaseMap");
            blitMRT2BaseMapLocation = blitMRT2Shader.GetUniformLocation("_BaseMap");
            blitMRT3BaseMapLocation = blitMRT3Shader.GetUniformLocation("_BaseMap");
            blitMRT4BaseMapLocation = blitMRT4Shader.GetUniformLocation("_BaseMap");
            sightMatrixModelLocation = sightShader.GetUniformLocation("MatrixModel");
            sightMatrixViewLocation = sightShader.GetUniformLocation("MatrixView");
            sightMatrixProjectionLocation = sightShader.GetUniformLocation("MatrixProjection");
            sightColorLocation = sightShader.GetUniformLocation("_Color");

            Bloom = new Bloom();
        }

        public unsafe void DoPostProcessing(uint colorAttachment, uint depthAttachment, ShadowMap shadowMap, GBuffer gBuffer, TemporalAntiAliasing temporalAntiAliasing)
        {
            temporalAntiAliasing.DoTemporalAntiAliasing(this, colorAttachment, depthAttachment, gBuffer);

            Bloom.DoBloom(this, colorAttachment);

            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            shadowMap.BindShadowMap(TextureUnit.Texture2);
            postProcessingShader.SetInt(shadowMapLocation, 2);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture3);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, gBuffer.NormalTexture);
            postProcessingShader.SetInt(normalMapLocation, 3);
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture4);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, gBuffer.MotionVectors);
            postProcessingShader.SetInt(motionVectorMapLocation, 4);
            Bloom.BindBloomMap(TextureUnit.Texture5);
            postProcessingShader.SetInt(bloomMapLocation, 5);
            postProcessingShader.SetFloat(bloomIntensityLocation, Bloom.Intensity);
            postProcessingShader.SetFloat(exposureLocation, Exposure);
            postProcessingShader.SetBool(tonemappingLocation, Tonemapping);
            postProcessingShader.SetVector(colorGradingLocation, ColorAdjustments.GetVector());
            postProcessingShader.SetVector(temporalJitterLocation, temporalAntiAliasing.GetJitter2());
            float4x4 matrixModel;
            var matrixView = MathHelper.LookAt(math.forward(), float3.zero, math.up());
            float4x4 matrixProjection;
            postProcessingShader.SetMatrix(matrixViewLocation, matrixView);
            var aspect = (float)App.Instance.MainWindow.Window.Size.X / App.Instance.MainWindow.Window.Size.Y;
            if (aspect > App.Instance.MainWindow.Aspect)
            {
                matrixModel = float4x4.Scale(-App.Instance.MainWindow.Aspect, 1f, 1f);
                matrixProjection = MathHelper.Ortho(-aspect, aspect, -1f, 1f, 0.1f, 2f);
            }
            else
            {
                matrixModel = float4x4.Scale(-1f, 1f / App.Instance.MainWindow.Aspect, 1f);
                matrixProjection = MathHelper.Ortho(-1f, 1f, -1f / aspect, 1f / aspect, 0.1f, 2f);
            }
            postProcessingShader.SetMatrix(matrixModelLocation, matrixModel);
            postProcessingShader.SetMatrix(matrixProjectionLocation, matrixProjection);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)fullScreenQuadIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            sightVAO.Bind();
            sightShader.Use();
            matrixModel = float4x4.Scale(16f, 16f, 1f);
            matrixProjection = MathHelper.Ortho(
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

        public unsafe void Blit(uint source, uint[] destinations, uint width, uint height)
        {
            if (destinations.Length < 2)
            {
                throw new NotSupportedException("destinations.Length < 2");
            }
            if (destinations.Length > 4)
            {
                throw new NotImplementedException("destinations.Length > 4");
            }
            App.Instance.MainWindow.BindFrameBuffer(destinations);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width, height);
            fullScreenQuadVAO.Bind();
            switch (destinations.Length)
            {
                case 2:
                    blitMRT2Shader.Use();
                    blitMRT2Shader.SetInt(blitMRT2BaseMapLocation, 0);
                    break;
                case 3:
                    blitMRT3Shader.Use();
                    blitMRT3Shader.SetInt(blitMRT3BaseMapLocation, 0);
                    break;
                case 4:
                    blitMRT4Shader.Use();
                    blitMRT4Shader.SetInt(blitMRT4BaseMapLocation, 0);
                    break;
            }
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, source);
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
            blitMRT2Shader.Dispose();
            blitMRT3Shader.Dispose();
            blitMRT4Shader.Dispose();
            sightShader.Dispose();
        }
    }
}
