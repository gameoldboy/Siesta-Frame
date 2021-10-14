using SiestaFrame.Object;
using SiestaFrame.SceneManagement;
using Silk.NET.Assimp;
using System.Collections.Generic;
using System;
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
            var scene = assimp.ImportFile(modelPath, (uint)(
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.GenerateUVCoords |
                PostProcessSteps.CalculateTangentSpace |
                PostProcessSteps.JoinIdenticalVertices));
            if (scene == null || (scene->MFlags == (uint)SceneFlags.Incomplete) || scene->MRootNode == null)
            {
                throw new Exception($"ERROR::ASSIMP::{assimp.GetErrorStringS()}");
            }

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
                    vertex.position = MathHelper.ToFloat3(mesh->MVertices[j]);
                    vertex.normal = MathHelper.ToFloat3(mesh->MNormals[j]);
                    var tangent = MathHelper.ToFloat3(mesh->MTangents[j]);
                    var bitangent = MathHelper.ToFloat3(mesh->MBitangents[j]);
                    var sign = math.sign(math.dot(math.cross(vertex.normal, tangent), bitangent));
                    sign = sign == 0 ? 1f : sign;
                    vertex.tangent = new float4(tangent, sign);
                    var uv0 = float2.zero;
                    var uv1 = float2.zero;
                    if (mesh->MTextureCoords[0] != null)
                    {
                        uv0 = MathHelper.ToFloat3(mesh->MTextureCoords[0][j]).xy;
                    }
                    if (mesh->MTextureCoords[1] != null)
                    {
                        uv1 = MathHelper.ToFloat3(mesh->MTextureCoords[1][j]).xy;
                    }
                    vertex.texCoords = new float4(uv0, uv1);
                    var color = float4.zero;
                    if (mesh->MColors[0] != null)
                    {
                        color = MathHelper.ToFloat4(mesh->MColors[0][j]);
                    }
                    vertex.color = color;
                    vertex.boneIds = uint4.zero;
                    vertex.weights = float4.zero;
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
                Console.WriteLine($"name:{mesh->MName}, vertices:{vertices.Count}, indices:{indices.Count}");

                // 贴图
                var sceneManager = SceneManager.Instance;
                var material = new Material();
                var aiMat = scene->MMaterials[mesh->MMaterialIndex];
                string texturePath;
                if (GetTexturePath(aiMat, TextureType.TextureTypeDiffuse, assimp, modelPath, out texturePath))
                {
                    material.BaseMap = sceneManager.AddTexture(texturePath);
                }
                if (GetTexturePath(aiMat, TextureType.TextureTypeHeight, assimp, modelPath, out texturePath))
                {
                    material.NormalMap = sceneManager.AddTexture(texturePath);
                }
                if (GetTexturePath(aiMat, TextureType.TextureTypeSpecular, assimp, modelPath, out texturePath))
                {
                    material.SpecularMap = sceneManager.AddTexture(texturePath);
                }
                if (GetTexturePath(aiMat, TextureType.TextureTypeEmissive, assimp, modelPath, out texturePath))
                {
                    material.EmissiveMap = sceneManager.AddTexture(texturePath);
                }

                Materials.Add(material);
            }

            assimp.FreeScene(scene);
            assimp.Dispose();

            return new Entity()
            {
                Meshes = Meshes.ToArray(),
                Materials = Materials.ToArray()
            };
        }

        static unsafe bool GetTexturePath(Silk.NET.Assimp.Material* material, TextureType textureType, Assimp assimp, string modelPath, out string texturePath)
        {
            AssimpString assimpString = new AssimpString();
            var textureCount = assimp.GetMaterialTextureCount(material, textureType);
            if (textureCount > 0)
            {
                assimp.GetMaterialTexture(material, textureType, 0, ref assimpString, null, null, null, null, null, null);
                if (Path.IsPathFullyQualified(assimpString.AsString))
                {
                    texturePath = assimpString.AsString;
                }
                else
                {
                    texturePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(modelPath)), assimpString.AsString);
                }
                Console.WriteLine($"texture:{texturePath}");
                return true;
            }
            texturePath = null;
            return false;
        }
    }
}
