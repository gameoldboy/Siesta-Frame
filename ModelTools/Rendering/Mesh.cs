using ModelTools.Animation;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace ModelTools.Rendering
{
    public class Mesh : IDisposable
    {
        public struct Vertex
        {
            public float3 position;
            public float3 normal;
            public float4 tangent;
            public float4 texCoords;
            public float4 color;
            public uint4 boneIds;
            public float4 weights;
        }

        public struct BoundingBox
        {
            public float right;
            public float left;
            public float top;
            public float bottom;
            public float front;
            public float back;
            public float3 center;

            public BoundingBox(float3 extents)
            {
                var halfExtents = extents * 0.5f;
                right = halfExtents.x;
                left = -halfExtents.x;
                top = halfExtents.y;
                bottom = -halfExtents.y;
                front = halfExtents.z;
                back = -halfExtents.z;
                center = float3.zero;
            }

            public override string ToString()
            {
                return $"right:{right}, left:{left}, " +
                       $"top:{top}, bottom:{bottom}, " +
                       $"front:{front}, back:{back}, " +
                       $"center:{center}";
            }
        }

        public Vertex[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }
        public Bone LinkedBone { get; private set; }

        public BoundingBox AABB { get; private set; }

        public Mesh(Vertex[] vertices, uint[] indices, Bone bone)
        {
            Indices = indices;
            Vertices = vertices;
            LinkedBone = bone;
        }

        public VertexArrayObject VAO { get; private set; }
        public BufferObject<Vertex> VBO { get; private set; }
        public BufferObject<uint> EBO { get; private set; }

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
            VAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, vertexSize, offset);
            // 顶点法线
            VAO.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, vertexSize, offset += 3 * sizeof(float));
            // 顶点切线
            VAO.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, vertexSize, offset += 3 * sizeof(float));
            // 顶点UV
            VAO.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(float));
            // 顶点颜色
            VAO.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(float));
            // 骨骼索引
            VAO.VertexAttribIPointer(5, 4, VertexAttribIType.UnsignedInt, vertexSize, offset += 4 * sizeof(float));
            // 骨骼权重
            VAO.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, vertexSize, offset += 4 * sizeof(uint));

            Program.GL.BindVertexArray(0);
            Program.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            Program.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        public void CalculateAABB()
        {
            BoundingBox aabb;
            aabb.right = float.MinValue;
            aabb.left = float.MaxValue;
            aabb.top = float.MinValue;
            aabb.bottom = float.MaxValue;
            aabb.front = float.MinValue;
            aabb.back = float.MaxValue;
            for (int i = 0; i < Vertices.Length; i++)
            {
                var pos = math.mul(LinkedBone.CalculateObjectSpaceMatrix(), new float4(Vertices[i].position, 1f));
                aabb.right = math.max(aabb.right, pos.x);
                aabb.left = math.min(aabb.left, pos.x);
                aabb.top = math.max(aabb.top, pos.y);
                aabb.bottom = math.min(aabb.bottom, pos.y);
                aabb.front = math.max(aabb.front, pos.z);
                aabb.back = math.min(aabb.back, pos.z);
            }
            aabb.center = new float3(
                aabb.left + (aabb.right - aabb.left) / 2f,
                aabb.bottom + (aabb.top - aabb.bottom) / 2f,
                aabb.back + (aabb.front - aabb.back) / 2f);
            AABB = aabb;
        }

        public struct DrawData
        {
            public Material material;
            public float4x4 modelMatrix;
        }

        void setShader(DrawData drawData, RenderingData renderingData, bool alphaDither)
        {
            var material = drawData.material;
            material.Shader.SetMatrix(material.MatrixModelLocation, drawData.modelMatrix);
            material.Shader.SetMatrix(material.MatrixViewLocation, renderingData.viewMatrix);
            material.Shader.SetMatrix(material.MatrixProjectionLocation, renderingData.projectionMatrix);
            material.Shader.SetVector(material.BaseColorLocation, material.BaseColor);
            material.BaseMap.Bind(TextureUnit.Texture0);
            material.Shader.SetInt(material.BaseMapLocation, 0);
            material.Shader.SetVector(material.ViewPosWSLocation, renderingData.cameraPosition);
            material.Shader.SetVector(material.MainLightDirLocation, renderingData.mainLightDirection);
            material.Shader.SetVector(material.ScreenSizeLocation, new float2(Program.window.Size.X, Program.window.Size.Y));
            material.Shader.SetBool(material.AlphaDitherLocation, alphaDither);
        }

        public unsafe void Draw(DrawData drawData, RenderingData renderingData)
        {
            var alphaDither = false;
            switch (drawData.material.Mode)
            {
                case Material.BlendMode.AlphaDither:
                    alphaDither = true;
                    break;
            }

            VAO.Bind();
            drawData.material.Shader.Use();
            setShader(drawData, renderingData, alphaDither);
            Program.GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            Program.GL.BindVertexArray(0);
            Program.GL.BindTexture(TextureTarget.Texture2D, 0);
            Program.GL.UseProgram(0);
        }

        public void Dispose()
        {
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }
    }
}
