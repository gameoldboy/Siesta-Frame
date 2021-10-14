using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public struct RenderingData
    {
        public struct Light
        {
            public float3 direction;
            public float4x4 viewMatrix;
            public float4x4 projectionMatrix;
        }

        public float3 cameraPosition;
        public float4x4 viewMatrix;
        public float4x4 projectionMatrix;
        public float4x4 prevViewMatrix;
        public float4x4 prevProjectionMatrix;
        public float3 mainLightDirection;
        public float4x4 mainLightViewMatrix;
        public float4x4 mainLightProjectionMatrix;
        public float mainLightShadowRange;
        public Light[] lights;
    }
}
