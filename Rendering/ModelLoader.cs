using Silk.NET.Assimp;
using System.Numerics;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public static class ModelLoader
    {
        public static unsafe Mesh Load(string path)
        {
            var assimp = Assimp.GetApi();
            var scene = *assimp.ImportFile(path, (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals));

            var mesh = *scene.MMeshes[0];

            float[] vertices = new float[mesh.MNumVertices * 14];
            uint[] indices = new uint[mesh.MNumFaces * 3];

            for (int i = 0; i < mesh.MNumVertices; i++)
            {
                var pos = MathHelper.ToFloat3(mesh.MVertices[i]);
                var nrm = MathHelper.ToFloat3(mesh.MNormals[i]);
                var uv0 = Vector3.Zero;
                var uv1 = Vector3.Zero;
                if (mesh.MTextureCoords[0] != null)
                {
                    uv0 = mesh.MTextureCoords[0][i];
                }
                if (mesh.MTextureCoords[1] != null)
                {
                    uv1 = mesh.MTextureCoords[1][i];
                }
                var uv = new float4(uv0.X, uv0.Y, uv1.X, uv1.Y);
                var color = Vector4.Zero;
                if (mesh.MColors[0] != null)
                {
                    color = mesh.MColors[0][i];
                }
                var col = MathHelper.ToFloat4(color);
                vertices[i * 14] = pos.x;
                vertices[i * 14 + 1] = pos.y;
                vertices[i * 14 + 2] = pos.z;
                vertices[i * 14 + 3] = nrm.x;
                vertices[i * 14 + 4] = nrm.y;
                vertices[i * 14 + 5] = nrm.z;
                vertices[i * 14 + 6] = uv.x;
                vertices[i * 14 + 7] = uv.y;
                vertices[i * 14 + 8] = uv.z;
                vertices[i * 14 + 9] = uv.w;
                vertices[i * 14 + 10] = col.x;
                vertices[i * 14 + 11] = col.y;
                vertices[i * 14 + 12] = col.z;
                vertices[i * 14 + 13] = col.w;
            }
            for (int i = 0; i < mesh.MNumFaces; i++)
            {
                var face = mesh.MFaces[i];
                for (int j = 0; j < 3; j++)
                {
                    var index = face.MIndices[j];
                    indices[i * 3 + j] = index;
                }
            }

            System.Diagnostics.Debug.WriteLine($"vertices:{vertices.Length}, indices:{indices.Length}");

            return new Mesh(ref vertices, ref indices);
        }
    }
}
