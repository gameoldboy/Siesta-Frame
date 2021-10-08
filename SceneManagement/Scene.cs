using SiestaFrame.Object;
using SiestaFrame.Rendering;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace SiestaFrame.SceneManagement
{
    public class Scene : IDisposable
    {
        public string Name { get; set; }

        public List<Entity> Entites { get; }

        public Camera MainCamera { get; set; }
        public Light MainLight { get; set; }

        public Scene(string name)
        {
            Name = name;
            Entites = new List<Entity>();
            MainCamera = new Camera();
            MainLight = new Light();

            transparentList = new List<DrawData>();
            instancedDictionary = new Dictionary<Mesh, int>();
            InstancedDictionaryIndex = new Dictionary<int, Mesh>();
            InstancedList = new List<List<Entity>>();
        }

        struct DrawData
        {
            public Mesh mesh;
            public Material material;
            public Transform transform;
            public int amount;

            public DrawData(Mesh mesh, Material material, Transform transform)
            {
                this.mesh = mesh;
                this.material = material;
                this.transform = transform;
                amount = 1;
            }
        }

        List<DrawData> transparentList;
        Dictionary<Mesh, int> instancedDictionary;
        public Dictionary<int, Mesh> InstancedDictionaryIndex { get; }
        public List<List<Entity>> InstancedList { get; }

        public void CollectAndUpdateInstancedData()
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                if (entity.DrawType == Mesh.DrawType.GPUInstancing)
                {
                    var diffCnt = -1;
                    for (int j = 0; j < entity.Meshes.Length; j++)
                    {
                        var mesh = entity.Meshes[j];
                        if (!instancedDictionary.ContainsKey(mesh))
                        {
                            diffCnt++;
                            if (diffCnt > InstancedList.Count - 1)
                            {
                                InstancedList.Add(new List<Entity>());
                            }
                            instancedDictionary.Add(mesh, diffCnt);
                            InstancedDictionaryIndex.Add(diffCnt, mesh);
                        }
                        var index = instancedDictionary[mesh];
                        InstancedList[index].Add(entity);
                    }
                }
            }
            // set ssbo
            for (int i = 0; i < InstancedList.Count; i++)
            {
                var mesh = InstancedDictionaryIndex[i];
                var entities = InstancedList[i];
                if (mesh.InstancedBuffer.Length != entities.Count)
                {
                    mesh.InstancedBuffer = new Mesh.InstancedData[entities.Count];
                    Mesh.UpdateInstancedData(entities, mesh.InstancedBuffer);
                }
                else
                {
                    Mesh.UpdateInstancedData(entities, mesh.InstancedBuffer);
                }
                mesh.instancedSSBO.Bind();
                mesh.instancedSSBO.BufferData(mesh.InstancedBuffer, BufferUsageARB.DynamicDraw);
                GraphicsAPI.GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
                mesh.instancedSSBO.BindBufferBase(0);
            }
        }

        public void ClearInstancedList()
        {
            // set prev model matrix
            for (int i = 0; i < InstancedList.Count; i++)
            {
                var entities = InstancedList[i];
                var mesh = InstancedDictionaryIndex[i];
                for (int j = 0; j < entities.Count; j++)
                {
                    var entity = entities[j];
                    entity.Transform.PrevModelMatrix = mesh.InstancedBuffer[j].ModelMatrix;
                }
            }
            instancedDictionary.Clear();
            InstancedDictionaryIndex.Clear();
            for (int i = 0; i < InstancedList.Count; i++)
            {
                InstancedList[i].Clear();
            }
            InstancedList.Clear();

            GraphicsAPI.GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, 0);
        }

        public void Render(ShadowMap shadowMap, TemporalAntiAliasing temporalAntiAliasing)
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                if (entity.DrawType == Mesh.DrawType.Direct)
                {
                    for (int j = 0; j < entity.Meshes.Length; j++)
                    {
                        var mesh = entity.Meshes[j];
                        var material = entity.Materials[j % entity.Materials.Length];
                        if (material.Mode == Material.BlendMode.None)
                        {
                            mesh.Draw(entity.Transform, material, MainCamera, MainLight, shadowMap, temporalAntiAliasing);
                        }
                        else
                        {
                            transparentList.Add(new DrawData(mesh, material, entity.Transform));
                        }
                    }
                }
            }

            for (int i = 0; i < InstancedList.Count; i++)
            {
                var entities = InstancedList[i];
                var mesh = InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];
                if (material.Mode == Material.BlendMode.None)
                {
                    mesh.DrawInstanced(material, MainCamera, MainLight, shadowMap, temporalAntiAliasing, entities.Count);
                }
            }

            for (int i = 0; i < transparentList.Count; i++)
            {
                var mesh = transparentList[i].mesh;
                var material = transparentList[i].material;
                var transform = transparentList[i].transform;
                mesh.Draw(transform, material, MainCamera, MainLight, shadowMap, temporalAntiAliasing);
            }

            for (int i = 0; i < InstancedList.Count; i++)
            {
                var entities = InstancedList[i];
                var mesh = InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];
                if (material.Mode != Material.BlendMode.None)
                {
                    mesh.DrawInstanced(material, MainCamera, MainLight, shadowMap, temporalAntiAliasing, entities.Count);
                }
            }

            transparentList.Clear();
        }

        public void SyncCollision()
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                entity.SyncCollision();
            }
        }

        public void SyncRigidBody()
        {
            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                entity.SyncRigidBody();
            }
        }

        public void Dispose()
        {
            foreach (var entity in Entites)
            {
                entity.Dispose();
            }
        }
    }
}
