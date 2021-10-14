using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Material = ModelTools.Rendering.Material;
using Mesh = ModelTools.Rendering.Mesh;
using Texture = ModelTools.Rendering.Texture;

namespace ModelTools
{
    public class Model : IDisposable
    {
        public Mesh[] Meshes { get; private set; }
        Dictionary<string, Texture> textures;
        public Material[] Materials { get; private set; }

        public class Bone
        {
            public string name;
            public float4x4 matrix;
            public Bone parent;
            public Bone[] children;

            public float4x4 GetObjectSpaceMatrix()
            {
                var bone = this;
                var m = matrix;
                while (bone.parent != null)
                {
                    m = math.mul(bone.parent.matrix, m);
                    bone = bone.parent;
                }
                return m;
            }
        }

        public Bone[] Bones { get; private set; }

        public Bone SkeletonRoot { get; private set; }

        public Mesh.BoundingBox AABB { get; private set; }

        public static unsafe Model Load(string path, float scale = 1f)
        {
            var assimp = Assimp.GetApi();
            var scene = assimp.ImportFile(path, (uint)(
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.GenerateUVCoords |
                PostProcessSteps.CalculateTangentSpace |
                PostProcessSteps.JoinIdenticalVertices));
            if (scene == null || (scene->MFlags == (uint)SceneFlags.Incomplete) || scene->MRootNode == null)
            {
                Console.WriteLine($"ERROR::ASSIMP::{assimp.GetErrorStringS()}");
                assimp.FreeScene(scene);
                assimp.Dispose();
                return null;
            }

            var model = new Model();
            model.textures = new Dictionary<string, Texture>();
            var meshes = new List<Mesh>();
            var materials = new List<Material>();
            var bones = new List<Bone>();

            Console.WriteLine("MetaData--------------------");
            getMetaData(scene->MMetaData);
            Console.WriteLine("Animations--------------------");
            getAnimation(scene);
            Console.WriteLine("Meshes--------------------");
            model.processNode(scene->MRootNode, 0, null, scale, scene, assimp, path, meshes, materials, bones);

            assimp.FreeScene(scene);
            assimp.Dispose();

            model.Meshes = meshes.ToArray();
            model.Materials = materials.ToArray();
            model.Bones = bones.ToArray();
            model.CalculateAABB();

            return model;
        }

        unsafe void processNode(Node* node, int index, Bone parent, float scale, Scene* scene, Assimp assimp, string modelPath, List<Mesh> meshes, List<Material> materials, List<Bone> bones)
        {
            Bone bone = new Bone();
            bone.name = node->MName.AsString;
            //Console.WriteLine(bone.name);
            bone.matrix = MathHelper.ToFloat4x4(node->MTransformation, MathHelper.MatrixOrder.Row);
            if (scale != 1f)
            {
                bone.matrix = math.mul(float4x4.Scale(scale), bone.matrix);
            }

            if (node->MNumChildren > 0)
            {
                bone.children = new Bone[node->MNumChildren];
            }
            else
            {
                bone.children = null;
            }
            if (parent == null)
            {
                bone.parent = null;
                SkeletonRoot = bone;
            }
            else
            {
                bone.parent = parent;
                parent.children[index] = bone;
            }
            // 递归骨骼
            for (int i = 0; i < node->MNumChildren; i++)
            {
                processNode(node->MChildren[i], i, bone, 1f, scene, assimp, modelPath, meshes, materials, bones);
            }

            bones.Add(bone);

            for (int i = 0; i < node->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[node->MMeshes[i]];

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

                meshes.Add(new Mesh(vertices.ToArray(), indices.ToArray(), bone));
                Console.WriteLine($"name:{mesh->MName}, vertices:{vertices.Count}, indices:{indices.Count}");

                // 贴图
                var material = new Material();
                var aiMat = scene->MMaterials[mesh->MMaterialIndex];
                if (getTexturePath(aiMat, TextureType.TextureTypeDiffuse, assimp, modelPath, out var texturePath))
                {
                    if (!textures.ContainsKey(texturePath))
                    {
                        switch (Path.GetExtension(texturePath))
                        {
                            case ".gobt":
                                textures.Add(texturePath, new Rendering.TextureGOB(texturePath));
                                break;
                            default:
                                textures.Add(texturePath, new Rendering.TextureImage(texturePath));
                                break;
                        }
                    }
                    material.BaseMap = textures[texturePath];
                }

                materials.Add(material);
            }
        }

        static unsafe void getMetaData(Metadata* metaData)
        {
            if (metaData == null)
            {
                return;
            }
            for (int i = 0; i < metaData->MNumProperties; i++)
            {
                var metadataEntry = metaData->MValues[i];
                switch (metadataEntry.MType)
                {
                    case MetadataType.Bool:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 1))
                        using (var reader = new BinaryReader(stream))
                        {
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{reader.ReadBoolean()}");
                        }
                        break;
                    case MetadataType.Int32:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 4))
                        using (var reader = new BinaryReader(stream))
                        {
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{reader.ReadInt32()}");
                        }
                        break;
                    case MetadataType.Uint64:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 8))
                        using (var reader = new BinaryReader(stream))
                        {
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{reader.ReadUInt64()}");
                        }
                        break;
                    case MetadataType.Float:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 4))
                        using (var reader = new BinaryReader(stream))
                        {
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{reader.ReadSingle()}");
                        }
                        break;
                    case MetadataType.Double:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 8))
                        using (var reader = new BinaryReader(stream))
                        {
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{reader.ReadDouble()}");
                        }
                        break;
                    case MetadataType.Aistring:
                        Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{Marshal.PtrToStringUTF8((IntPtr)metadataEntry.MData)}");
                        break;
                    case MetadataType.Aivector3D:
                        using (var stream = new UnmanagedMemoryStream((byte*)metadataEntry.MData, 12))
                        using (var reader = new BinaryReader(stream))
                        {
                            var x = reader.ReadSingle();
                            var y = reader.ReadSingle();
                            var z = reader.ReadSingle();
                            Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:{x},{y},{z}");
                        }
                        break;
                    case MetadataType.MetaMax:
                        Console.WriteLine($"{metaData->MKeys[i].AsString}:{metadataEntry.MType}:not support");
                        break;
                }
            }
        }

        static unsafe void getAnimation(Scene* scene)
        {
            for (int i = 0; i < scene->MNumAnimations; i++)
            {
                var animation = scene->MAnimations[i];
                Console.WriteLine(animation->MName.AsString);

                for (int j = 0; j < animation->MNumChannels; j++)
                {
                    var channel = animation->MChannels[i];
                    //Console.WriteLine(channel->MNodeName.AsString);
                }
            }
        }

        static unsafe bool getTexturePath(Silk.NET.Assimp.Material* material, TextureType textureType, Assimp assimp, string modelPath, out string texturePath)
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
                return System.IO.File.Exists(texturePath);
            }
            texturePath = null;
            return false;
        }

        public void CalculateAABB()
        {
            Mesh.BoundingBox aabb;
            aabb.right = float.MinValue;
            aabb.left = float.MaxValue;
            aabb.top = float.MinValue;
            aabb.bottom = float.MaxValue;
            aabb.front = float.MinValue;
            aabb.back = float.MaxValue;
            for (int i = 0; i < Bones.Length; i++)
            {
                var pos = math.mul(Bones[i].GetObjectSpaceMatrix(), new float4(0, 0, 0, 1f));
                aabb.right = math.max(aabb.right, pos.x);
                aabb.left = math.min(aabb.left, pos.x);
                aabb.top = math.max(aabb.top, pos.y);
                aabb.bottom = math.min(aabb.bottom, pos.y);
                aabb.front = math.max(aabb.front, pos.z);
                aabb.back = math.min(aabb.back, pos.z);
            }
            for (int i = 0; i < Meshes.Length; i++)
            {
                var meshAABB = Meshes[i].AABB;
                aabb.right = math.max(aabb.right, meshAABB.right);
                aabb.left = math.min(aabb.left, meshAABB.left);
                aabb.top = math.max(aabb.top, meshAABB.top);
                aabb.bottom = math.min(aabb.bottom, meshAABB.bottom);
                aabb.front = math.max(aabb.front, meshAABB.front);
                aabb.back = math.min(aabb.back, meshAABB.back);
            }
            aabb.center = new float3(
                aabb.left + (aabb.right - aabb.left) / 2f,
                aabb.bottom + (aabb.top - aabb.bottom) / 2f,
                aabb.back + (aabb.front - aabb.back) / 2f);
            AABB = aabb;
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }
            foreach (var texture in textures.Values)
            {
                texture.Dispose();
            }
        }
    }
}
