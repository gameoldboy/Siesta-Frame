using SiestaFrame.Object;
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
        public float4x4 ViewMatrix => MathHelper.LookAt(Transform.Position, Transform.Position + Transform.Forward, Transform.Up);
        public float4x4 ProjectionMatrix =>
            MathHelper.PerspectiveFov(FOV * MathHelper.Deg2Rad, App.Instance.MainWindow.Width, App.Instance.MainWindow.Height, Near, Far);
        public float4x4 PrevViewMatrix { get; set; }
        public float4x4 PrevProjectionMatrix { get; set; }
        public float4x4 JitterProjectionMatrix { get; set; }
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
            Pitch = Yaw = 0;
            PrevViewMatrix = ViewMatrix;
            PrevProjectionMatrix = ProjectionMatrix;
        }

        public void ApplyYawPitch()
        {
            Transform.EulerAngles = new float3(Pitch, Yaw, 0);
        }

        public void UpdateYawPitch()
        {
            Pitch = Transform.EulerAngles.x;
            Yaw = Transform.EulerAngles.y;
        }
    }
}
