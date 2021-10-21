using Assimp;
using Assimp.Configs;
using ModelTools.Animation;
using ModelTools.Rendering;
using Silk.NET.OpenGL;
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

        public Animation.Animation[] Animations { get; private set; }

        public Mesh.BoundingBox AABB { get; private set; }

        public static Model Load(string path, string format, float scale = 1f)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            var assimp = new AssimpContext();
            Scene scene = null;
            try
            {
                var vertexBoneWeightLimitConfig = new VertexBoneWeightLimitConfig(4);
                assimp.SetConfig(vertexBoneWeightLimitConfig);
                var postProcessSteps =
                    PostProcessSteps.Triangulate |
                    PostProcessSteps.GenerateNormals |
                    PostProcessSteps.CalculateTangentSpace |
                    PostProcessSteps.LimitBoneWeights |
                    PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.ValidateDataStructure;
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    scene = assimp.ImportFileFromStream(stream, postProcessSteps, format);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR::ASSIMP::{e.Message}");
                return null;
            }
            Console.WriteLine((uint)scene.SceneFlags);
            if (scene == null || scene.RootNode == null)
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

            Console.WriteLine("Meshes--------------------");
            model.SkeletonRoot = new BoneRoot();
            model.SkeletonRoot.UnitScaleFactor = scale;
            if (scene.Metadata.TryGetValue("UnitScaleFactor", out var entry))
            {
                model.SkeletonRoot.UnitScaleFactor *= Convert.ToSingle(entry.Data);
            }
            model.SkeletonRoot.name = scene.RootNode.Name;
            model.SkeletonRoot.offset = float4x4.identity;
            model.SkeletonRoot.matrix = MathHelper.ToFloat4x4(scene.RootNode.Transform, MathHelper.MatrixOrder.Row);
            model.SkeletonRoot.children = new Bone[scene.RootNode.ChildCount];
            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                model.processNode(scene.RootNode.Children[i], i, model.SkeletonRoot, scene, path, meshes, materials, bones);
            }
            model.Meshes = meshes.ToArray();
            model.Materials = materials.ToArray();
            model.Bones = bones.ToArray();
            model.CalculateAABB();

            Console.WriteLine("Animations--------------------");
            model.getVertexWeights(scene);
            model.getAnimation(scene);

            assimp.Dispose();

            return model;
        }

        void processNode(Node node, int index, Bone parent, Scene scene, string modelPath, List<Mesh> meshes, List<Material> materials, List<Bone> bones)
        {
            var bone = new Bone();
            bone.name = node.Name;
            //Console.WriteLine(bone.name);
            bone.offset = float4x4.identity;
            bone.matrix = MathHelper.ToFloat4x4(node.Transform, MathHelper.MatrixOrder.Row);
            bone.parent = parent;
            parent.children[index] = bone;
            bone.children = new Bone[node.ChildCount];
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
                    var uv0 = MathHelper.ToFloat3(mesh.TextureCoordinateChannels[0][j]).xy;
                    var uv1 = float2.zero;
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

        static bool getTexturePath(Assimp.Material material, TextureType textureType, string modelPath, out string texturePath)
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

        static void ShowMetaData(Metadata metaData)
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

        void getVertexWeights(Scene scene)
        {
            for (int i = 0; i < scene.MeshCount; i++)
            {
                var mesh = scene.Meshes[i];
                for (int j = 0; j < mesh.BoneCount; j++)
                {
                    var aiBone = mesh.Bones[j];
                    for (uint k = 0; k < Bones.Length; k++)
                    {
                        if (Bones[k].name == aiBone.Name)
                        {
                            Bones[k].offset = MathHelper.ToFloat4x4(aiBone.OffsetMatrix, MathHelper.MatrixOrder.Row);
                            for (int l = 0; l < aiBone.VertexWeightCount; l++)
                            {
                                var weight = aiBone.VertexWeights[l];
                                var vertex = Meshes[i].Vertices[weight.VertexID];
                                var available = false;
                                for (int m = 0; m < 4; m++)
                                {
                                    if (vertex.weights[m] == 0)
                                    {
                                        vertex.boneIds[m] = k;
                                        vertex.weights[m] = weight.Weight;
                                        available = true;
                                        break;
                                    }
                                }
                                if (available)
                                {
                                    Meshes[i].Vertices[weight.VertexID] = vertex;
                                }
                                else
                                {
                                    throw new NotSupportedException($"{mesh.Name}=>{aiBone.Name}:Vertex weights > 4");
                                }
                            }
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < Meshes.Length; i++)
            {
                var mesh = Meshes[i];
                uint boneId = 0;
                for (uint j = 0; j < Bones.Length; j++)
                {
                    if (Bones[j] == mesh.LinkedBone)
                    {
                        boneId = j;
                        break;
                    }
                }
                // 权重归一化
                for (int j = 0; j < mesh.Vertices.Length; j++)
                {
                    var vertex = mesh.Vertices[j];
                    var sum = vertex.weights.x + vertex.weights.y + vertex.weights.z + vertex.weights.w;
                    if (sum == 0)
                    {
                        vertex.boneIds.x = boneId;
                        vertex.weights.x = 1f;
                    }
                    else
                    {
                        vertex.weights = vertex.weights / sum;
                    }
                    mesh.Vertices[j] = vertex;
                }

                mesh.Setup();
            }
        }

        void getAnimation(Scene scene)
        {
            var animations = new Animation.Animation[scene.AnimationCount];
            for (int i = 0; i < scene.AnimationCount; i++)
            {
                var aiAnimation = scene.Animations[i];
                Console.WriteLine($"name:{aiAnimation.Name}, fps:{aiAnimation.TicksPerSecond}, framesCount:{aiAnimation.DurationInTicks}, channelCount:{aiAnimation.NodeAnimationChannelCount}");

                animations[i] = new Animation.Animation(
                    aiAnimation.Name, aiAnimation.TicksPerSecond,
                    aiAnimation.DurationInTicks, aiAnimation.NodeAnimationChannelCount);
                for (int j = 0; j < aiAnimation.NodeAnimationChannelCount; j++)
                {
                    var channel = aiAnimation.NodeAnimationChannels[j];
                    //Console.WriteLine(channel.NodeName);
                    // 映射骨骼
                    for (int k = 0; k < Bones.Length; k++)
                    {
                        var bone = Bones[k];
                        if (channel.NodeName == bone.name)
                        {
                            Track track;
                            track.bone = bone;

                            track.positionKeys = new PositionKey[channel.PositionKeyCount];
                            for (int l = 0; l < channel.PositionKeyCount; l++)
                            {
                                var key = channel.PositionKeys[l];
                                PositionKey positionKey;
                                positionKey.time = key.Time;
                                positionKey.position = MathHelper.ToFloat3(key.Value);
                                track.positionKeys[l] = positionKey;
                            }

                            track.rotationKeys = new RotationKey[channel.RotationKeyCount];
                            for (int l = 0; l < channel.RotationKeyCount; l++)
                            {
                                var key = channel.RotationKeys[l];
                                RotationKey rotationKey;
                                rotationKey.time = key.Time;
                                rotationKey.rotation = MathHelper.ToQuaternion(key.Value);
                                track.rotationKeys[l] = rotationKey;
                            }

                            track.scalingKeys = new ScalingKey[channel.ScalingKeyCount];
                            for (int l = 0; l < channel.ScalingKeyCount; l++)
                            {
                                var key = channel.ScalingKeys[l];
                                ScalingKey scalingKey;
                                scalingKey.time = key.Time;
                                scalingKey.scale = MathHelper.ToFloat3(key.Value);
                                track.scalingKeys[l] = scalingKey;
                            }

                            animations[i].Tracks[j] = track;
                            break;
                        }
                    }
                }
            }

            Animations = animations;
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
