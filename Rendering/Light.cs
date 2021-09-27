using SiestaFrame.Object;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Light
    {
        public enum LightType
        {
            Directional,
            Spot,
            Point
        }

        public LightType Type { get; set; }

        public Transform Transform { get; set; }
        Transform shadowFitTransform;

        public float Intensity { get; set; }
        public float3 Color { get; set; }
        public float ShadowRange { get; set; }

        public float4x4 ViewMatrix => MathHelper.LookAt(shadowFitTransform.Position, shadowFitTransform.Position + Transform.Forward, Transform.Up);
        public float4x4 ProjectionMatrix =>
             MathHelper.ortho(-ShadowRange, ShadowRange, -ShadowRange, ShadowRange, 0.1f, ShadowRange * 2);

        public Light()
        {
            Type = LightType.Directional;
            Transform = new Transform()
            {
                EulerAngles = new float3(45, 130, 0),
            };
            shadowFitTransform = new Transform();
            Intensity = 1f;
            Color = new float3(1f, 1f, 1f);
            ShadowRange = 10f;
        }

        public void ShadowFitToCamera(Camera camera)
        {
            shadowFitTransform.Position = math.floor(camera.Transform.Position - Transform.Forward * ShadowRange);
        }
    }
}
