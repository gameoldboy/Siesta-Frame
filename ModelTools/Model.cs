using Assimp;
using ModelTools.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Bone = ModelTools.Animation.Bone;
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

        public Bone[] Bones { get; private set; }

        public BoneRoot SkeletonRoot { get; private set; }

        public Mesh.BoundingBox AABB { get; private set; }

        public static unsafe Model Load(string path, float scale = 1f)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            var stream = new FileStream(path, FileMode.Open);
            var assimp = new AssimpContext();
            Scene scene = null;
            try
            {
                scene = assimp.ImportFileFromStream(stream,
                   PostProcessSteps.Triangulate |
                   PostProcessSteps.GenerateNormals |
                   PostProcessSteps.GenerateUVCoords |
                   PostProcessSteps.CalculateTangentSpace |
                   PostProcessSteps.JoinIdenticalVertices |
                   PostProcessSteps.ValidateDataStructure);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR::ASSIMP::{e.Message}");
            }
            if (scene == null || (scene.SceneFlags == SceneFlags.Incomplete) || scene.RootNode == null)
            {
                Console.WriteLine("Scene empty or incomplete");
                assimp.Dispose();
                return null;
            }

            var model = new Model();
            model.textures = new Dictionary<string, Texture>();
            var meshes = new List<Mesh>();
            var materials = new List<Material>();
            var bones = new List<Bone>();

            Console.WriteLine("MetaData--------------------");
            ShowMetaData(scene.Metadata);

            Console.WriteLine("Animations--------------------");
            if (scene.HasAnimations)
            {
                getAnimation(scene.Animations);
            }
            Console.WriteLine("Meshes--------------------");
            var bone = new BoneRoot();
            model.SkeletonRoot = bone;
            bone.UnitScaleFactor = scale;
            if (scene.Metadata.TryGetValue("UnitScaleFactor", out var entry))
            {
                bone.UnitScaleFactor *= Convert.ToSingle(entry.Data);
            }
            bone.name = scene.RootNode.Name;
            bone.matrix = MathHelper.ToFloat4x4(scene.RootNode.Transform, MathHelper.MatrixOrder.Row);
            if (scene.RootNode.HasChildren)
            {
                bone.children = new Bone[scene.RootNode.ChildCount];
            }
            else
            {
                bone.children = null;
            }
            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                model.processNode(scene.RootNode.Children[i], i, bone, scene, path, meshes, materials, bones);
            }

            assimp.Dispose();
            stream.Dispose();

            model.Meshes = meshes.ToArray();
            model.Materials = materials.ToArray();
            model.Bones = bones.ToArray();
            model.CalculateAABB();

            return model;
        }

        unsafe void processNode(Node node, int index, Bone parent, Scene scene, string modelPath, List<Mesh> meshes, List<Material> materials, List<Bone> bones)
        {
            var bone = new Bone();
            bone.name = node.Name;
            //Console.WriteLine(bone.name);
            bone.matrix = MathHelper.ToFloat4x4(node.Transform, MathHelper.MatrixOrder.Row);
            bone.parent = parent;
            parent.children[index] = bone;
            if (node.HasChildren)
            {
                bone.children = new Bone[node.ChildCount];
            }
            else
            {
                bone.children = null;
            }
            // 递归骨骼
            for (int i = 0; i < node.ChildCount; i++)
            {
                processNode(node.Children[i], i, bone, scene, modelPath, meshes, materials, bones);
            }

            bones.Add(bone);

            for (int i = 0; i < node.MeshCount; i++)
            {
                var mesh = scene.Meshes[node.MeshIndices[i]];

                List<Mesh.Vertex> vertices = new List<Mesh.Vertex>();
                List<uint> indices = new List<uint>();

                // 顶点
                for (int j = 0; j < mesh.VertexCount; j++)
                {
                    Mesh.Vertex vertex;
                    vertex.position = MathHelper.ToFloat3(mesh.Vertices[j]);
                    vertex.normal = MathHelper.ToFloat3(mesh.Normals[j]);
                    var tangent = MathHelper.ToFloat3(mesh.Tangents[j]);
                    var bitangent = MathHelper.ToFloat3(mesh.BiTangents[j]);
                    var sign = math.sign(math.dot(math.cross(vertex.normal, tangent), bitangent));
                    sign = sign == 0 ? 1f : sign;
                    vertex.tangent = new float4(tangent, sign);
                    var uv0 = float2.zero;
                    var uv1 = float2.zero;
                    if (mesh.HasTextureCoords(0))
                    {
                        uv0 = MathHelper.ToFloat3(mesh.TextureCoordinateChannels[0][j]).xy;
                    }
                    if (mesh.HasTextureCoords(1))
                    {
                        uv1 = MathHelper.ToFloat3(mesh.TextureCoordinateChannels[1][j]).xy;
                    }
                    vertex.texCoords = new float4(uv0, uv1);
                    var color = float4.zero;
                    if (mesh.HasVertexColors(0))
                    {
                        color = MathHelper.ToFloat4(mesh.VertexColorChannels[0][j]);
                    }
                    vertex.color = color;
                    vertex.boneIds = uint4.zero;
                    vertex.weights = float4.zero;
                    vertices.Add(vertex);
                }
                // 面索引
                for (int j = 0; j < mesh.FaceCount; j++)
                {
                    var face = mesh.Faces[j];
                    for (int k = 0; k < face.IndexCount; k++)
                    {
                        indices.Add((uint)face.Indices[k]);
                    }
                }

                meshes.Add(new Mesh(vertices.ToArray(), indices.ToArray(), bone));
                Console.WriteLine($"name:{mesh.Name}, vertices:{vertices.Count}, indices:{indices.Count}");

                // 贴图
                var material = new Material();
                var aiMat = scene.Materials[mesh.MaterialIndex];
                if (getTexturePath(aiMat, TextureType.Diffuse, modelPath, out var texturePath))
                {
                    if (!textures.ContainsKey(texturePath))
                    {
                        switch (Path.GetExtension(texturePath).ToLower())
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

        static unsafe void ShowMetaData(Metadata metaData)
        {
            if (metaData == null)
            {
                return;
            }
            foreach (var key in metaData.Keys)
            {
                var entry = metaData[key];
                switch (entry.DataType)
                {
                    case MetaDataType.Bool:
                        Console.WriteLine($"{key}:{entry.DataType}:{(bool)entry.Data}");
                        break;
                    case MetaDataType.Int32:
                        Console.WriteLine($"{key}:{entry.DataType}:{(int)entry.Data}");
                        break;
                    case MetaDataType.UInt64:
                        Console.WriteLine($"{key}:{entry.DataType}:{(ulong)entry.Data}");
                        break;
                    case MetaDataType.Float:
                        Console.WriteLine($"{key}:{entry.DataType}:{(float)entry.Data}");
                        break;
                    case MetaDataType.Double:
                        Console.WriteLine($"{key}:{entry.DataType}:{(double)entry.Data}");
                        break;
                    case MetaDataType.String:
                        Console.WriteLine($"{key}:{entry.DataType}:{(string)entry.Data}");
                        break;
                    case MetaDataType.Vector3D:
                        Console.WriteLine($"{key}:{entry.DataType}:{(Vector3D)entry.Data}");
                        break;
                }
            }
        }

        static unsafe void getAnimation(List<Assimp.Animation> animations)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                var animation = animations[i];
                Console.WriteLine(animation.Name);

                for (int j = 0; j < animation.NodeAnimationChannelCount; j++)
                {
                    var channel = animation.NodeAnimationChannels[i];
                    Console.WriteLine(channel.NodeName);
                }
            }
        }

        static unsafe bool getTexturePath(Assimp.Material material, TextureType textureType, string modelPath, out string texturePath)
        {
            if (material.GetMaterialTexture(textureType, 0, out var textureSlot))
            {
                if (Path.IsPathFullyQualified(textureSlot.FilePath))
                {
                    texturePath = textureSlot.FilePath;
                }
                else
                {
                    texturePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(modelPath)), textureSlot.FilePath);
                }
                Console.WriteLine($"texture:{texturePath}");
                return File.Exists(texturePath);
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
                var pos = math.mul(Bones[i].CalculateObjectSpaceMatrix(), new float4(0, 0, 0, 1f));
                aabb.right = math.max(aabb.right, pos.x);
                aabb.left = math.min(aabb.left, pos.x);
                aabb.top = math.max(aabb.top, pos.y);
                aabb.bottom = math.min(aabb.bottom, pos.y);
                aabb.front = math.max(aabb.front, pos.z);
                aabb.back = math.min(aabb.back, pos.z);
            }
            for (int i = 0; i < Meshes.Length; i++)
            {
                Meshes[i].CalculateAABB();
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

            Console.WriteLine($"AABB:{AABB}");
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
