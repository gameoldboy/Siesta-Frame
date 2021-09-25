using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Mesh : IDisposable
    {
        public struct Vertex
        {
            public float3 Position;
            public float3 Normal;
            public float3 Tangent;
            public float3 Bitangent;
            public float4 TexCoords;
            public float4 Color;
            public uint4 BoneIds;
            public float4 Weights;
        }

        public Vertex[] Vertices { get; set; }
        public uint[] Indices { get; set; }

        public Mesh(Vertex[] vertices, uint[] indices)
        {
            Indices = indices;
            Vertices = vertices;

            SetupMesh();
        }

        public VertexArrayObject<Vertex, uint> VAO { get; private set; }
        public BufferObject<Vertex> VBO { get; private set; }
        public BufferObject<uint> EBO { get; private set; }

        public unsafe void SetupMesh()
        {
            VBO?.Dispose();
            EBO?.Dispose();
            VAO?.Dispose();
            EBO = new BufferObject<uint>(Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<Vertex>(Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<Vertex, uint>(VBO, EBO);

            // 顶点坐标
            VAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 28, 0);
            // 顶点法线
            VAO.VertexAttributePointer<float>(1, 3, VertexAttribPointerType.Float, 28, 3);
            // 顶点切线
            VAO.VertexAttributePointer<float>(2, 3, VertexAttribPointerType.Float, 28, 6);
            // 顶点副切线
            VAO.VertexAttributePointer<float>(3, 3, VertexAttribPointerType.Float, 28, 9);
            // 顶点UV
            VAO.VertexAttributePointer<float>(4, 4, VertexAttribPointerType.Float, 28, 12);
            // 顶点颜色
            VAO.VertexAttributePointer<float>(5, 4, VertexAttribPointerType.Float, 28, 16);
            // 骨骼索引
            VAO.VertexAttributePointer<uint>(6, 4, VertexAttribPointerType.UnsignedInt, 28, 20);
            // 骨骼权重
            VAO.VertexAttributePointer<float>(7, 4, VertexAttribPointerType.Float, 28, 24);

            GraphicsAPI.GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            VBO.Dispose();
            EBO.Dispose();
            VAO.Dispose();
        }
    }
}
