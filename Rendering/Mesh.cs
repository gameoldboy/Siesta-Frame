using Silk.NET.OpenGL;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public struct Mesh
    {
        public struct Vertex
        {
            public float3 Position;
            public float3 Normal;
            public float4 TexCoords;
            public float4 Color;
        }

        public struct Texture
        {
            public uint id;
            public string type;
        }

        public float[] Vertices;
        public uint[] Indices;
        //public Texture[] Textures;

        public Mesh(ref float[] vertices, ref uint[] indices /*, Texture[] textures*/)
        {
            Indices = indices;
            Vertices = vertices;
            //Textures = textures;
            EBO = new BufferObject<uint>(Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(VBO, EBO);

            setupMesh();
        }

        public void Draw(Shader shader)
        {

        }

        public VertexArrayObject<float, uint> VAO;
        public BufferObject<float> VBO;
        public BufferObject<uint> EBO;

        unsafe void setupMesh()
        {
            // 顶点坐标
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 14, 0);
            // 顶点法线
            VAO.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 14, 3);
            // 顶点UV
            VAO.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 14, 6);
            // 顶点颜色
            VAO.VertexAttributePointer(3, 4, VertexAttribPointerType.Float, 14, 10);

            Graphics.GL.BindVertexArray(0);
        }
    }
}
