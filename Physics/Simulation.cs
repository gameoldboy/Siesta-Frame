using BulletSharp;
using BulletSharp.Math;
using SiestaFrame.Object;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiestaFrame.Physics
{
    public class Simulation : IDisposable
    {
        public static Simulation Instance { get; private set; }

        public CollisionConfiguration CollisionConfiguration { get; }
        public CollisionDispatcher Dispatcher { get; }
        public BroadphaseInterface Broadphase { get; }
        public DiscreteDynamicsWorld World { get; }

        public float FixedTimeStep { get; set; } = 0.0166666675F;

        bool isSimulating;
        EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        Task simulationTask;

        float deltaTime;

        public Simulation()
        {
            CollisionConfiguration = new DefaultCollisionConfiguration();
            Dispatcher = new CollisionDispatcher(CollisionConfiguration);
            Broadphase = new DbvtBroadphase();
            World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, null, CollisionConfiguration);
            var gravity = new Vector3(0, -10, 0);
            World.SetGravity(ref gravity);

            simulationTask = new Task(() => SimulationTask());
            simulationTask.Start();

            Instance = this;
        }

        void SimulationTask()
        {
            isSimulating = true;
            while (isSimulating)
            {
                eventWaitHandle.WaitOne();
                if (isSimulating)
                {
                    World.StepSimulation(deltaTime, fixedTimeStep: FixedTimeStep);
                }
            }
        }

        public void Update(float deltaTime)
        {
            this.deltaTime = deltaTime;
            eventWaitHandle.Set();
        }

        public CollisionObject AddCollision(Entity entity)
        {
            var triangleIndexVertexArray = new TriangleIndexVertexArray();
            for (int i = 0; i < entity.Meshes.Length; i++)
            {
                var mesh = entity.Meshes[i];
                var indices = new int[mesh.Indices.Length];
                for (int j = 0; j < mesh.Indices.Length; j++)
                {
                    indices[j] = (int)mesh.Indices[j];
                }
                var vertices = new Vector3[mesh.Vertices.Length];
                for (int j = 0; j < mesh.Vertices.Length; j++)
                {
                    var vertex = mesh.Vertices[j];
                    vertices[j] = new Vector3(vertex.Position.x, vertex.Position.y, vertex.Position.z);
                }
                var indexedMesh = new IndexedMesh();
                indexedMesh.Allocate(indices.Length / 3, vertices.Length);
                indexedMesh.SetData(indices, vertices);
                triangleIndexVertexArray.AddIndexedMesh(indexedMesh);
            }
            var shape = new BvhTriangleMeshShape(triangleIndexVertexArray, true);
            //shape.Margin = 0;
            return AddCollision(shape, entity); ;
        }

        public CollisionObject AddCollision(CollisionShape shape, Entity entity)
        {
            shape.LocalScaling = MathHelper.ToVector3(entity.Transform.Scale);
            var obj = new CollisionObject()
            {
                CollisionShape = shape,
                WorldTransform = MathHelper.ToMatrix(entity.Transform.RigidModelMatrix),
                UserObject = entity
            };
            World.AddCollisionObject(obj);
            return obj;
        }

        public RigidBody AddRigidBody(Entity entity, float mass = 1f)
        {
            var triangleIndexVertexArray = new TriangleIndexVertexArray();
            for (int i = 0; i < entity.Meshes.Length; i++)
            {
                var mesh = entity.Meshes[i];
                var indices = new int[mesh.Indices.Length];
                for (int j = 0; j < mesh.Indices.Length; j++)
                {
                    indices[j] = (int)mesh.Indices[j];
                }
                var vertices = new Vector3[mesh.Vertices.Length];
                for (int j = 0; j < mesh.Vertices.Length; j++)
                {
                    var vertex = mesh.Vertices[j];
                    vertices[j] = new Vector3(vertex.Position.x, vertex.Position.y, vertex.Position.z);
                }
                var indexedMesh = new IndexedMesh();
                indexedMesh.Allocate(indices.Length / 3, vertices.Length);
                indexedMesh.SetData(indices, vertices);
                triangleIndexVertexArray.AddIndexedMesh(indexedMesh);
            }
            ConvexHullShape convexShape;
            using (var tmpConvexShape = new ConvexTriangleMeshShape(triangleIndexVertexArray))
            using (var hull = new ShapeHull(tmpConvexShape))
            {
                hull.BuildHull(tmpConvexShape.Margin);
                convexShape = new ConvexHullShape(hull.Vertices);
            }
            //convexShape.Margin = 0;
            convexShape.LocalScaling = MathHelper.ToVector3(entity.Transform.Scale);
            return AddRigidBody(convexShape, entity, mass);
        }

        public RigidBody AddRigidBody(CollisionShape shape, Entity entity, float mass = 1f)
        {
            RigidBody rigidBody;
            if (mass == 0)
            {
                using (var rbinfo = new RigidBodyConstructionInfo(0, null, shape)
                {
                    StartWorldTransform = MathHelper.ToMatrix(entity.Transform.RigidModelMatrix)
                })
                {
                    rigidBody = new RigidBody(rbinfo)
                    {
                        UserObject = entity
                    };
                }
            }
            else
            {
                var motionState = new DefaultMotionState(MathHelper.ToMatrix(entity.Transform.RigidModelMatrix));
                var localInertia = shape.CalculateLocalInertia(mass);
                using (var rbinfo = new RigidBodyConstructionInfo(mass, motionState, shape, localInertia))
                {
                    rigidBody = new RigidBody(rbinfo)
                    {
                        UserObject = entity
                    };
                }
            }
            World.AddRigidBody(rigidBody);
            return rigidBody;
        }

        public void Dispose()
        {
            isSimulating = false;
            eventWaitHandle.Set();
            simulationTask.Wait();
            this.StandardCleanup();
        }
    }
}
