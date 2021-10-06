using BulletSharp;
using BulletSharp.Math;
using SiestaFrame.Object;
using System;

namespace SiestaFrame.Physics
{
    public class Simulation : IDisposable
    {
        public static Simulation Instance { get; private set; }

        public CollisionConfiguration CollisionConfiguration { get; }
        public CollisionDispatcher Dispatcher { get; }
        public BroadphaseInterface Broadphase { get; }
        public DiscreteDynamicsWorld World { get; }

        public Simulation()
        {
            CollisionConfiguration = new DefaultCollisionConfiguration();
            Dispatcher = new CollisionDispatcher(CollisionConfiguration);
            Broadphase = new DbvtBroadphase();
            World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, null, CollisionConfiguration);
            var gravity = new Vector3(0, -10, 0);
            World.SetGravity(ref gravity);

            Instance = this;
        }

        public void Update(float deltaTime)
        {
            World.StepSimulation(deltaTime);
        }

        public CollisionObject[] AddCollision(Entity entity)
        {
            var objects = new CollisionObject[entity.Meshes.Length];
            for (int i = 0; i < entity.Meshes.Length; i++)
            {
                var mesh = entity.Meshes[i];
                var vertices = new Vector3[mesh.Vertices.Length];
                for (int j = 0; j < mesh.Vertices.Length; j++)
                {
                    var vertex = mesh.Vertices[j];
                    vertices[j] = new Vector3(vertex.Position.x, vertex.Position.y, vertex.Position.z);
                }
                var indices = new int[mesh.Indices.Length];
                for (int j = 0; j < mesh.Indices.Length; j++)
                {
                    indices[j] = (int)mesh.Indices[j];
                }
                var triangleIndexVertexArray = new TriangleIndexVertexArray(indices, vertices);
                var shape = new GImpactMeshShape(triangleIndexVertexArray);
                shape.Margin = 0;
                shape.UpdateBound();
                var obj = new CollisionObject()
                {
                    CollisionShape = shape,
                    WorldTransform = MathHelper.ToMatrix(entity.Transform.RigidModelMatrix),
                    UserObject = entity
                };
                World.AddCollisionObject(obj);
                objects[i] = obj;
            }
            return objects;
        }

        public void Dispose()
        {
            this.StandardCleanup();
        }
    }
}
