using Unity.Mathematics;

namespace SiestaFrame.Object
{
    public class Transform
    {
        public float3 Position { get; set; }

        public float3 Scale { get; set; }

        quaternion rotation;
        public quaternion Rotation
        {
            get => rotation;
            set
            {
                new float3x3(Rotation);
                rotation = value;
                eulerAngles = MathHelper.ToEuler(Rotation) * MathHelper.Rad2Deg;
            }
        }

        float3 eulerAngles;
        public float3 EulerAngles
        {
            get => eulerAngles;
            set
            {
                eulerAngles = value;
                Rotation = MathHelper.FromEuler(eulerAngles * MathHelper.Deg2Rad);
            }
        }

        public Transform()
        {
            Position = float3.zero;
            Scale = new float3(1f, 1f, 1f);
            rotation = quaternion.identity;
            eulerAngles = float3.zero;
        }

        public float3 Right
        {
            get => math.rotate(Rotation, math.right());
            set { rotation = MathHelper.FromToRotation(math.right(), value); }
        }

        public float3 Up
        {
            get => math.rotate(Rotation, math.up());
            set { rotation = MathHelper.FromToRotation(math.up(), value); }
        }

        public float3 Forward
        {
            get => math.rotate(Rotation, math.forward());
            set => rotation = MathHelper.LookRotation(value);
        }

        public float4x4 ModelMatrix => MathHelper.TRS(Position, rotation, Scale);
        public float4x4 PrevModelMatrix { get; set; }
    }
}
