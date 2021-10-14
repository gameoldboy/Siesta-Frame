using Unity.Mathematics;

namespace ModelTools.Rendering
{
    public struct RenderingData
    {
        public float4x4 viewMatrix;
        public float4x4 projectionMatrix;
        public float3 cameraPosition;
        public float3 mainLightDirection;
    }
}
