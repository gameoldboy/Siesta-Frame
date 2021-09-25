using SiestaFrame.Object;
using SiestaFrame.SceneManagement;
using Silk.NET.Assimp;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Unity.Mathematics;
using Material = SiestaFrame.Rendering.Material;
using Mesh = SiestaFrame.Rendering.Mesh;

namespace SiestaFrame
{
    public partial class Utilities
    {
        public static unsafe Entity LoadModel(string path)
        {
            var assimp = Assimp.GetApi();

            string modelPath;
            if (Path.IsPathFullyQualified(path))
            {
                modelPath = path;
            }
            else
            {
                modelPath = Path.Combine("Assets", "Models", path);
            }
            var scene = assimp.ImportFile(modelPath, (uint)(PostProcessSteps.CalculateTangentSpace | PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals));

            var Meshes = new List<Mesh>();
            var Materials = new List<Material>();

            for (int i = 0; i < scene->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[i];

                List<Mesh.Vertex> vertices = new List<Mesh.Vertex>();
                List<uint> indices = new List<uint>();

                // 顶点
                for (int j = 0; j < mesh->MNumVertices; j++)
                {
                    Mesh.Vertex vertex;
                    vertex.Position = MathHelper.ToFloat3(mesh->MVertices[j]);
                    vertex.Normal = MathHelper.ToFloat3(mesh->MNormals[j]);
                    vertex.Tangent = MathHelper.ToFloat3(mesh->MTangents[j]);
                    vertex.Bitangent = MathHelper.ToFloat3(mesh->MBitangents[j]);
                    var uv0 = Vector3.Zero;
                    var uv1 = Vector3.Zero;
                    if (mesh->MTextureCoords[0] != null)
                    {
                        uv0 = mesh->MTextureCoords[0][j];
                    }
                    if (mesh->MTextureCoords[1] != null)
                    {
                        uv1 = mesh->MTextureCoords[1][j];
                    }
                    vertex.TexCoords = new float4(uv0.X, uv0.Y, uv1.X, uv1.Y);
                    var color = Vector4.Zero;
                    if (mesh->MColors[0] != null)
                    {
                        color = mesh->MColors[0][j];
                    }
                    vertex.Color = MathHelper.ToFloat4(color);
                    vertex.BoneIds = uint4.zero;
                    vertex.Weights = float4.zero;
                    vertices.Add(vertex);
                }
                // 面索引
                for (int j = 0; j < mesh->MNumFaces; j++)
                {
                    var face = mesh->MFaces[j];
                    for (int k = 0; k < face.MNumIndices; k++)
                    {
                        indices.Add(face.MIndices[k]);
                    }
                }

                Meshes.Add(new Mesh(vertices.ToArray(), indices.ToArray()));
                Debug.WriteLine($"name:{mesh->MName}, vertices:{vertices.Count}, indices:{indices.Count}");

                // 贴图
                var sceneManager = SceneManager.Instance;
                var material = new Material();
                var aiMat = *scene->MMaterials[mesh->MMaterialIndex];
                var texturePath = GetTexturePath(assimp, aiMat, TextureType.TextureTypeDiffuse, modelPath);
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    material.BaseMap = sceneManager.AddTexture(texturePath);
                }
                texturePath = GetTexturePath(assimp, aiMat, TextureType.TextureTypeHeight, modelPath);
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    material.NormalMap = sceneManager.AddTexture(texturePath);
                }
                texturePath = GetTexturePath(assimp, aiMat, TextureType.TextureTypeSpecular, modelPath);
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    material.SpecularMap = sceneManager.AddTexture(texturePath);
                }
                texturePath = GetTexturePath(assimp, aiMat, TextureType.TextureTypeEmissive, modelPath);
                if (!string.IsNullOrWhiteSpace(texturePath))
                {
                    material.EmissiveMap = sceneManager.AddTexture(texturePath);
                }

                Materials.Add(material);
            }

            return new Entity()
            {
                Meshes = Meshes.ToArray(),
                Materials = Materials.ToArray()
            };
        }

        static unsafe string GetTexturePath(Assimp assimp, Silk.NET.Assimp.Material material, TextureType textureType, string modelPath)
        {
            AssimpString assimpString = new AssimpString();
            var textureCount = assimp.GetMaterialTextureCount(material, textureType);
            if (textureCount > 0)
            {
                assimp.GetMaterialTexture(in material, textureType, 0, ref assimpString, null, null, null, null, null, null);
                string texturePath;
                if (Path.IsPathFullyQualified(assimpString.AsString))
                {
                    texturePath = assimpString.AsString;
                }
                else
                {
                    texturePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(modelPath)), assimpString.AsString);
                }
                Debug.WriteLine($"texture:{texturePath}");
                return texturePath;
            }
            return null;
        }
    }
}
