using Unity.Mathematics;

namespace SiestaFrame.Entity
{
    public class Transform
    {
        public float3 Position { get; set; } = float3.zero;

        public float3 Scale { get; set; } = new float3(1f, 1f, 1f);

        quaternion rotation = quaternion.identity;
        public quaternion Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                eulerAngles = MathHelper.ToEulerAngles(Rotation) * MathHelper.Rad2Deg;
            }
        }

        float3 eulerAngles = float3.zero;
        public float3 EulerAngles
        {
            get => eulerAngles;
            set
            {
                eulerAngles = value;
                Rotation = MathHelper.FromEulerAngles(eulerAngles * MathHelper.Deg2Rad);
            }
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

        public float4x4 ViewMatrix => float4x4.TRS(Position, Rotation, Scale);
    }
}
