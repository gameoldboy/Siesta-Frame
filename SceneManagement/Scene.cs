using SiestaFrame.Object;
using SiestaFrame.Rendering;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace SiestaFrame.SceneManagement
{
    public class Scene : IDisposable
    {
        public string Name { get; set; }

        public List<Entity> Entites { get; }

        public Camera MainCamera { get; set; }
        public Light MainLight { get; set; }
        public SkyBox SkyBox { get; set; }

        public Scene(string name)
        {
            Name = name;
            Entites = new List<Entity>();
            MainCamera = new Camera();
            MainLight = new Light();
            //SkyBox.SkyBoxFaces faces;
            //faces.right = "right.jpg";
            //faces.left = "left.jpg";
            //faces.top = "top.jpg";
            //faces.bottom = "bottom.jpg";
            //faces.front = "front.jpg";
            //faces.back = "back.jpg";
            SkyBox = new SkyBox();
            SkyBox.Load("birchwood_4k.gobt");

            transparentList = new List<Mesh.DrawData>();
            instancedDictionary = new Dictionary<Mesh, int>();
            InstancedDictionaryIndex = new Dictionary<int, Mesh>();
            InstancedList = new List<List<Entity>>();
        }

        List<Mesh.DrawData> transparentList;
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
            RenderingData renderingData;
            renderingData.cameraPosition = MainCamera.Transform.Position;
            renderingData.viewMatrix = MainCamera.ViewMatrix;
            renderingData.projectionMatrix = MainCamera.JitterProjectionMatrix;
            renderingData.prevViewMatrix = MainCamera.PrevViewMatrix;
            renderingData.prevProjectionMatrix = MainCamera.PrevProjectionMatrix;
            renderingData.mainLightDirection = -MainLight.Transform.Forward;
            renderingData.mainLightViewMatrix = MainLight.ViewMatrix;
            renderingData.mainLightProjectionMatrix = MainLight.ProjectionMatrix;
            renderingData.mainLightShadowRange = MainLight.ShadowRange;
            renderingData.lights = new RenderingData.Light[0];

            for (int i = 0; i < Entites.Count; i++)
            {
                var entity = Entites[i];
                if (entity.DrawType == Mesh.DrawType.Direct)
                {
                    var modelMatrix = entity.Transform.ModelMatrix;
                    var prevModelMatrix = entity.Transform.PrevModelMatrix;
                    for (int j = 0; j < entity.Meshes.Length; j++)
                    {
                        var mesh = entity.Meshes[j];
                        var material = entity.Materials[j % entity.Materials.Length];
                        Mesh.DrawData drawData;
                        drawData.mesh = mesh;
                        drawData.material = material;
                        drawData.clockwise = Mesh.DrawData.CalculateClockwise(entity.Transform);
                        drawData.modelMatrix = modelMatrix;
                        drawData.prevModelMatrix = prevModelMatrix;
                        if (material.Mode == Material.BlendMode.None)
                        {
                            mesh.Draw(drawData, renderingData, shadowMap, temporalAntiAliasing);
                        }
                        else
                        {
                            transparentList.Add(drawData);
                        }
                    }
                    entity.Transform.PrevModelMatrix = modelMatrix;
                }
            }

            for (int i = 0; i < InstancedList.Count; i++)
            {
                var entities = InstancedList[i];
                var mesh = InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];
                if (material.Mode == Material.BlendMode.None)
                {
                    Mesh.DrawData drawData;
                    drawData.mesh = mesh;
                    drawData.material = material;
                    drawData.clockwise = false;
                    drawData.modelMatrix = default;
                    drawData.prevModelMatrix = default;

                    mesh.DrawInstanced(drawData, renderingData, shadowMap, temporalAntiAliasing, entities.Count);
                }
            }

            SkyBox.Draw(renderingData, new float3(1f), 90f);

            for (int i = 0; i < transparentList.Count; i++)
            {
                var mesh = transparentList[i].mesh;
                mesh.Draw(transparentList[i], renderingData, shadowMap, temporalAntiAliasing);
            }

            for (int i = 0; i < InstancedList.Count; i++)
            {
                var entities = InstancedList[i];
                var mesh = InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];
                if (material.Mode != Material.BlendMode.None)
                {
                    Mesh.DrawData drawData;
                    drawData.mesh = mesh;
                    drawData.material = material;
                    drawData.clockwise = false;
                    drawData.modelMatrix = default;
                    drawData.prevModelMatrix = default;

                    mesh.DrawInstanced(drawData, renderingData, shadowMap, temporalAntiAliasing, entities.Count);
                }

                for (int j = 0; j < entities.Count; j++)
                {
                    var entity = entities[j];
                    entity.Transform.PrevModelMatrix = mesh.InstancedBuffer[j].ModelMatrix;
                }
            }

            MainCamera.PrevViewMatrix = renderingData.viewMatrix;
            MainCamera.PrevProjectionMatrix = renderingData.projectionMatrix;

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
            SkyBox.Dispose();
            foreach (var entity in Entites)
            {
                entity.Dispose();
            }
        }
    }
}
