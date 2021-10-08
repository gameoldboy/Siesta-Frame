using BulletSharp;
using SiestaFrame.Physics;
using SiestaFrame.Rendering;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace SiestaFrame.Object
{
    public class Entity : IDisposable
    {
        public Transform Transform { get; set; }

        public Mesh.DrawType DrawType { get; set; }

        Mesh[] meshes;
        public Mesh[] Meshes
        {
            get => meshes;
            set
            {
                foreach (var mesh in meshes)
                {
                    mesh.Dispose();
                }
                meshes = value;
            }
        }

        public Material[] Materials { get; set; }

        public CollisionObject Collision { get; set; }

        public RigidBody RigidBody { get; set; }

        public Entity()
        {
            Transform = new Transform();
            meshes = new Mesh[0];
            Materials = new Material[0];
            DrawType = Mesh.DrawType.Direct;
        }

        public void SyncCollision()
        {
            if (Collision == null)
            {
                return;
            }
            Collision.WorldTransform = MathHelper.ToMatrix(Transform.RigidModelMatrix);
            Collision.CollisionShape.LocalScaling = MathHelper.ToVector3(Transform.Scale);
        }

        public void SyncRigidBody()
        {
            if (RigidBody == null)
            {
                return;
            }
            BulletSharp.Math.Matrix matrix;
            if (RigidBody.MotionState != null)
            {
                // 获得插值矩阵
                RigidBody.MotionState.GetWorldTransform(out matrix);
            }
            else
            {
                matrix = RigidBody.WorldTransform;
            }
            var transform = new RigidTransform(MathHelper.ToFloat4x4(matrix));
            Transform.Position = transform.pos - Transform.RigidOffsetPosition;
            Transform.Rotation = transform.rot;
        }

        public void Dispose()
        {
            foreach (var mesh in meshes)
            {
                mesh.Dispose();
            }
            var shapes = new HashSet<CollisionShape>();
            if (Collision != null)
            {
                SimulationExtensions.CleanupBodiesAndShapes(Simulation.Instance.World, Collision, shapes);
            }
            if (RigidBody != null)
            {
                SimulationExtensions.CleanupBodiesAndShapes(Simulation.Instance.World, RigidBody, shapes);
            }
            foreach (var shape in shapes)
            {
                shape.Dispose();
            }
        }
    }
}
