using SiestaFrame.Entity;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Camera
    {
        public Transform Transform { get; }
        public Transform Target { get; set; }
        public float FOV { get; set; }
        public float Near { get; set; }
        public float Far { get; set; }
        public float Aspect { get; set; }
        public float4x4 ViewMatrix => MathHelper.LookAt(Transform.Position, Transform.Position + Transform.Forward, Transform.Up);
        public float4x4 ProjectionMatrix => MathHelper.PerspectiveFov(FOV * MathHelper.Deg2Rad, Aspect, Near, Far);

        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public Camera()
        {
            Transform = new Transform()
            {
                Position = new float3(0, 0, 3),
            };
            FOV = 45;
            Near = 0.1f;
            Far = 100;
            Aspect = 1;
            Pitch = Yaw = 0;
        }

        public void UpdateYawPaitch()
        {
            Yaw = Transform.EulerAngles.x;
            Pitch = Transform.EulerAngles.y;
        }
    }
}
