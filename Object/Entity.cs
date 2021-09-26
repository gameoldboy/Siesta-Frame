using SiestaFrame.Rendering;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;
using Boolean = Silk.NET.OpenGL.Boolean;
using Texture = SiestaFrame.Rendering.Texture;

namespace SiestaFrame.Object
{
    public class Entity : IDisposable
    {
        public Transform Transform { get; set; }
        Mesh[] meshes;
        public Mesh[] Meshes
        {
            get => meshes;
            set
            {
                foreach (var mesh in meshes)
                {
                    mesh.Dispose();
                }
                meshes = value;
            }
        }
        public Material[] Materials { get; set; }

        public Entity()
        {
            Transform = new Transform();
            meshes = new Mesh[0];
            Materials = new Material[0];
        }

        public unsafe void DrawShadowMap(Camera camera, Transform mainLight)
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                var material = Materials[i % Materials.Length];

                mesh.VAO.Bind();
                material.ShaderShadowMap.Use();

                const float shadowRange = 10f;
                material.ShaderShadowMap.SetMatrix(material.ShadowMapMatrixModelLocation, Transform.ModelMatrix);
                material.ShaderShadowMap.SetMatrix(material.ShadowMapMatrixViewLocation, MathHelper.LookAt(mainLight.Position, mainLight.Position + mainLight.Forward, mainLight.Up));
                material.ShaderShadowMap.SetMatrix(material.ShadowMapMatrixProjectionLocation, MathHelper.ortho(-shadowRange, shadowRange, -shadowRange, shadowRange, 0.1f, shadowRange));
                material.BaseMap.Bind(TextureUnit.Texture0);
                material.ShaderShadowMap.SetInt(material.ShadowMapBaseMapLocation, 0);
                material.ShaderShadowMap.SetVector(material.ShadowMapTilingOffsetLocation, material.TilingOffset);
                material.ShaderShadowMap.SetBool(material.ShadowMapAlphaTest, material.Mode != Material.BlendMode.None ? true : false);
                if (math.sign(Transform.Scale.x) * math.sign(Transform.Scale.y) * math.sign(Transform.Scale.z) < 0)
                {
                    GraphicsAPI.GL.FrontFace(FrontFaceDirection.CW);
                }
                else
                {
                    GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
                }
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindVertexArray(0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }
        }

        public unsafe void Draw(Camera camera, Transform mainLight, uint shadowMap)
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                var material = Materials[i % Materials.Length];

                mesh.VAO.Bind();
                material.Shader.Use();
                material.Shader.SetMatrix(material.MatrixModelLocation, Transform.ModelMatrix);
                material.Shader.SetMatrix(material.MatrixViewLocation, camera.ViewMatrix);
                material.Shader.SetMatrix(material.MatrixProjectionLocation, camera.ProjectionMatrix);
                const float shadowRange = 10f;
                material.Shader.SetMatrix(material.MatrixMainLightViewLocation, MathHelper.LookAt(mainLight.Position, mainLight.Position + mainLight.Forward, mainLight.Up));
                material.Shader.SetMatrix(material.MatrixMainLightProjectionLocation, MathHelper.ortho(-shadowRange, shadowRange, -shadowRange, shadowRange, 0.1f, shadowRange));
                material.BaseMap.Bind(TextureUnit.Texture0);
                material.NormalMap.Bind(TextureUnit.Texture1);
                material.MetallicMap.Bind(TextureUnit.Texture2);
                material.SpecularMap.Bind(TextureUnit.Texture3);
                material.EmissiveMap.Bind(TextureUnit.Texture4);
                material.OcclusionMap.Bind(TextureUnit.Texture5);
                material.MatCapMap.Bind(TextureUnit.Texture6);
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture7);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, shadowMap);
                material.Shader.SetVector(material.BaseColorLocation, material.BaseColor);
                material.Shader.SetInt(material.BaseMapLocation, 0);
                material.Shader.SetVector(material.TilingOffsetLocation, material.TilingOffset);
                material.Shader.SetFloat(material.NormalScaleLocation, material.NormalScale);
                material.Shader.SetInt(material.NormalMapLocation, 1);
                material.Shader.SetFloat(material.SmoothnessLocation, material.Smoothness);
                material.Shader.SetFloat(material.MetallicLocation, material.Metallic);
                material.Shader.SetInt(material.MetallicMapLocation, 2);
                material.Shader.SetVector(material.SpecularColorlLocation, material.SpecularColor);
                material.Shader.SetInt(material.SpecularMaplLocation, 3);
                material.Shader.SetVector(material.EmissiveColorLocation, material.EmissiveColor);
                material.Shader.SetInt(material.EmissiveMapLocation, 4);
                material.Shader.SetFloat(material.OcclusionStrengthLocation, material.OcclusionStrength);
                material.Shader.SetInt(material.OcclusionMapLocation, 5);
                material.Shader.SetVector(material.MatCapColorlLocation, material.MatCapColor);
                material.Shader.SetInt(material.MatCapMapLocation, 6);
                material.Shader.SetVector(material.ViewPosWSLocation, camera.Transform.Position);
                material.Shader.SetVector(material.MainLightDirLocation, -mainLight.Forward);
                material.Shader.SetInt(material.ShadowMapLocation, 7);
                if (math.sign(Transform.Scale.x) * math.sign(Transform.Scale.y) * math.sign(Transform.Scale.z) < 0)
                {
                    GraphicsAPI.GL.FrontFace(FrontFaceDirection.CW);
                }
                else
                {
                    GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
                }
                switch (material.Mode)
                {
                    case Material.BlendMode.Alpha:
                        GraphicsAPI.GL.Enable(EnableCap.Blend);
                        GraphicsAPI.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        GraphicsAPI.GL.DepthMask(Boolean.False);
                        break;
                    case Material.BlendMode.Add:
                        GraphicsAPI.GL.Enable(EnableCap.Blend);
                        GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                        GraphicsAPI.GL.DepthMask(Boolean.False);
                        break;
                }
                GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
                GraphicsAPI.GL.BindVertexArray(0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
                GraphicsAPI.GL.Disable(EnableCap.Blend);
                GraphicsAPI.GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
                GraphicsAPI.GL.DepthMask(Boolean.True);
            }
        }

        public void Dispose()
        {
            foreach (var mesh in meshes)
            {
                mesh.Dispose();
            }
        }
    }
}
