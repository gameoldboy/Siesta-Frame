using SiestaFrame.Object;
using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Mesh : IDisposable
    {
        public enum DrawType
        {
            Direct,
            GPUInstancing
        }

        public struct Vertex
        {
            public float3 Position;
            public float3 Normal;
            public float4 Tangent;
            public float4 TexCoords;
            public float4 Color;
            public uint4 BoneIds;
            public float4 Weights;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InstancedData
        {
            [FieldOffset(0)] public float4x4 ModelMatrix;
            [FieldOffset(64)] public float4 BaseColor;
            [FieldOffset(80)] public float4 TilingOffset;
            [FieldOffset(96)] public float NormalScale;
            [FieldOffset(100)] public float Smoothness;
            [FieldOffset(104)] public float Metallic;
            [FieldOffset(108)] public float OcclusionStrength;
            [FieldOffset(112)] public float4 SpecularColor;
            [FieldOffset(128)] public float3 SelectedColor;
            [FieldOffset(144)] public float3 MatCapColor;
            [FieldOffset(160)] public float3 EmissiveColor;
            [FieldOffset(176)] public float4x4 PrevModelMatrix;
        }

        public Vertex[] Vertices { get; set; }
        public uint[] Indices { get; set; }

        public InstancedData[] InstancedBuffer { get; set; }

        public Mesh(Vertex[] vertices, uint[] indices)
        {
            Indices = indices;
            Vertices = vertices;
            InstancedBuffer = new InstancedData[0];

            Setup();
        }

        public VertexArrayObject VAO { get; private set; }
        public BufferObject<Vertex> VBO { get; private set; }
        public BufferObject<uint> EBO { get; private set; }

        public BufferObject<InstancedData> instancedSSBO { get; private set; }

        public unsafe void Setup()
        {
            VBO?.Dispose();
            EBO?.Dispose();
            VAO?.Dispose();
            VAO = new VertexArrayObject();
            VAO.Bind();
            VBO = new BufferObject<Vertex>(BufferTargetARB.ArrayBuffer);
            VBO.Bind();
            VBO.BufferData(Vertices);
            EBO = new BufferObject<uint>(BufferTargetARB.ElementArrayBuffer);
            EBO.Bind();
            EBO.BufferData(Indices);

            uint vertexSize;
            vertexSize = (uint)sizeof(Vertex);
            var offset = 0;
            // 顶点坐标
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, vertexSize, offset);
            // 顶点法线
            VAO.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, vertexSize, offset += 3 * sizeof(float));
            // 顶点切线
            VAO.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, vertexSize, offset += 3 * sizeof(float));
            // 顶点UV
            VAO.VertexAttributePointer(3, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(float));
            // 顶点颜色
            VAO.VertexAttributePointer(4, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(float));
            // 骨骼索引
            VAO.VertexAttributePointer(5, 4, VertexAttribPointerType.UnsignedInt, vertexSize, offset += 4 * sizeof(float));
            // 骨骼权重
            VAO.VertexAttributePointer(6, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(uint));

            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GraphicsAPI.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

            instancedSSBO?.Dispose();
            instancedSSBO = new BufferObject<InstancedData>(BufferTargetARB.ShaderStorageBuffer);
        }

        public struct DrawData
        {
            public Mesh mesh;
            public Material material;
            public bool clockwise;
            public float4x4 modelMatrix;
            public float4x4 prevModelMatrix;

            public static bool CalculateClockwise(Transform transform)
            {
                return math.sign(transform.Scale.x) * math.sign(transform.Scale.y) * math.sign(transform.Scale.z) < 0;
            }
        }

        void setShader(DrawData drawData, Scene.RenderingData renderingData,
            ShadowMap shadowMap, TemporalAntiAliasing temporalAntiAliasing, bool alphaHashed, bool alphaDither)
        {
            var material = drawData.material;
            material.Shader.SetMatrix(material.MatrixModelLocation, drawData.modelMatrix);
            material.Shader.SetMatrix(material.MatrixViewLocation, renderingData.viewMatrix);
            material.Shader.SetMatrix(material.MatrixProjectionLocation, renderingData.projectionMatrix);
            material.Shader.SetMatrix(material.MatrixMainLightViewLocation, renderingData.mainLightViewMatrix);
            material.Shader.SetMatrix(material.MatrixMainLightProjectionLocation, renderingData.mainLightProjectionMatrix);
            material.Shader.SetVector(material.BaseColorLocation, material.BaseColor);
            material.BaseMap.Bind(TextureUnit.Texture0);
            material.Shader.SetInt(material.BaseMapLocation, 0);
            material.Shader.SetVector(material.TilingOffsetLocation, material.TilingOffset);
            material.Shader.SetFloat(material.NormalScaleLocation, material.NormalScale);
            material.NormalMap.Bind(TextureUnit.Texture1);
            material.Shader.SetInt(material.NormalMapLocation, 1);
            material.Shader.SetFloat(material.SmoothnessLocation, material.Smoothness);
            material.Shader.SetFloat(material.MetallicLocation, material.Metallic);
            material.MetallicMap.Bind(TextureUnit.Texture2);
            material.Shader.SetInt(material.MetallicMapLocation, 2);
            material.Shader.SetVector(material.SpecularColorlLocation, material.SpecularColor);
            material.SpecularMap.Bind(TextureUnit.Texture3);
            material.Shader.SetInt(material.SpecularMaplLocation, 3);
            material.Shader.SetVector(material.EmissiveColorLocation, material.EmissiveColor);
            material.EmissiveMap.Bind(TextureUnit.Texture4);
            material.Shader.SetInt(material.EmissiveMapLocation, 4);
            material.Shader.SetVector(material.SelectedColorLocation, material.SelectedColor);
            material.Shader.SetFloat(material.OcclusionStrengthLocation, material.OcclusionStrength);
            material.OcclusionMap.Bind(TextureUnit.Texture5);
            material.Shader.SetInt(material.OcclusionMapLocation, 5);
            material.Shader.SetVector(material.MatCapColorlLocation, material.MatCapColor);
            material.MatCapMap.Bind(TextureUnit.Texture6);
            material.Shader.SetInt(material.MatCapMapLocation, 6);
            material.Shader.SetVector(material.ViewPosWSLocation, renderingData.cameraPosition);
            material.Shader.SetVector(material.MainLightDirLocation, renderingData.mainLightDirection);
            shadowMap.BindShadowMap(TextureUnit.Texture7);
            material.Shader.SetInt(material.ShadowMapLocation, 7);
            material.Shader.SetFloat(material.MainLightShadowRangeLocation, renderingData.mainLightShadowRange);
            material.Shader.SetVector(material.TemporalJitterLocation, temporalAntiAliasing.GetJitter2());
            material.Shader.SetVector(material.ScreenSizeLocation, new float2(App.Instance.MainWindow.Width, App.Instance.MainWindow.Height));
            material.Shader.SetBool(material.AlphaHashedLocation, alphaHashed);
            material.Shader.SetBool(material.AlphaDitherLocation, alphaDither);
            material.Shader.SetMatrix(material.PrevMatrixModelLocation, drawData.prevModelMatrix);
            material.Shader.SetMatrix(material.PrevMatrixViewLocation, renderingData.prevViewMatrix);
            material.Shader.SetMatrix(material.PrevMatrixProjectionLocation, renderingData.prevProjectionMatrix);
        }

        public unsafe void Draw(DrawData drawData, Scene.RenderingData renderingData,
            ShadowMap shadowMap, TemporalAntiAliasing temporalAntiAliasing)
        {
            if (drawData.clockwise)
            {
                GraphicsAPI.GL.FrontFace(FrontFaceDirection.CW);
            }
            else
            {
                GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
            }
            var alphaHashed = false;
            var alphaDither = false;
            switch (drawData.material.Mode)
            {
                case Material.BlendMode.Add:
                    GraphicsAPI.GL.Enable(EnableCap.Blend);
                    GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                    GraphicsAPI.GL.DepthMask(false);
                    break;
                case Material.BlendMode.Alpha:
                    GraphicsAPI.GL.Enable(EnableCap.Blend);
                    GraphicsAPI.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    GraphicsAPI.GL.DepthMask(false);
                    break;
                case Material.BlendMode.AlphaHashed:
                    alphaHashed = true;
                    break;
                case Material.BlendMode.AlphaDither:
                    alphaDither = true;
                    break;
            }

            VAO.Bind();
            drawData.material.Shader.Use();
            setShader(drawData, renderingData, shadowMap, temporalAntiAliasing, alphaHashed, alphaDither);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            GraphicsAPI.GL.Disable(EnableCap.Blend);
            GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GraphicsAPI.GL.DepthMask(true);
            GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
        }

        public unsafe void DrawInstanced(DrawData drawData, Scene.RenderingData renderingData,
            ShadowMap shadowMap, TemporalAntiAliasing temporalAntiAliasing, int amount)
        {
            var alphaHashed = false;
            var alphaDither = false;
            switch (drawData.material.Mode)
            {
                case Material.BlendMode.Add:
                    GraphicsAPI.GL.Enable(EnableCap.Blend);
                    GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                    GraphicsAPI.GL.DepthMask(false);
                    break;
                case Material.BlendMode.Alpha:
                    GraphicsAPI.GL.Enable(EnableCap.Blend);
                    GraphicsAPI.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    GraphicsAPI.GL.DepthMask(false);
                    break;
                case Material.BlendMode.AlphaHashed:
                    alphaHashed = true;
                    break;
                case Material.BlendMode.AlphaDither:
                    alphaDither = true;
                    break;
            }

            VAO.Bind();
            drawData.material.Shader.Use();
            setShader(drawData, renderingData, shadowMap, temporalAntiAliasing, alphaHashed, alphaDither);
            GraphicsAPI.GL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null, (uint)amount);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
            GraphicsAPI.GL.Disable(EnableCap.Blend);
            GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GraphicsAPI.GL.DepthMask(true);
        }

        public static void UpdateInstancedData(List<Entity> entities, InstancedData[] vertices)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                vertices[i].ModelMatrix = entity.Transform.ModelMatrix;
                vertices[i].BaseColor = entity.Materials[0].BaseColor;
                vertices[i].TilingOffset = entity.Materials[0].TilingOffset;
                vertices[i].NormalScale = entity.Materials[0].NormalScale;
                vertices[i].Smoothness = entity.Materials[0].Smoothness;
                vertices[i].Metallic = entity.Materials[0].Metallic;
                vertices[i].OcclusionStrength = entity.Materials[0].OcclusionStrength;
                vertices[i].SpecularColor = entity.Materials[0].SpecularColor;
                vertices[i].SelectedColor = entity.Materials[0].SelectedColor;
                vertices[i].MatCapColor = entity.Materials[0].MatCapColor;
                vertices[i].EmissiveColor = entity.Materials[0].EmissiveColor;
                vertices[i].PrevModelMatrix = entity.Transform.PrevModelMatrix;
            }
        }

        public void Dispose()
        {
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
            instancedSSBO.Dispose();
        }
    }
}
