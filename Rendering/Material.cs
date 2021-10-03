using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Material
    {
        public enum BlendMode
        {
            None,
            Add,
            Alpha,
            AlphaHashed,
            AlphaDither
        }

        public float4 BaseColor { get; set; }
        public Texture BaseMap { get; set; }
        public float4 TilingOffset { get; set; }
        public float NormalScale { get; set; }
        public Texture NormalMap { get; set; }
        public float Smoothness { get; set; }
        public float Metallic { get; set; }
        public Texture MetallicMap { get; set; }
        public float4 SpecularColor { get; set; }
        public Texture SpecularMap { get; set; }
        public float3 EmissiveColor { get; set; }
        public Texture EmissiveMap { get; set; }
        public float OcclusionStrength { get; set; }
        public Texture OcclusionMap { get; set; }
        public float3 MatCapColor { get; set; }
        public Texture MatCapMap { get; set; }

        public Shader Shader { get; set; }

        public int MatrixModelLocation { get; }
        public int MatrixViewLocation { get; }
        public int MatrixProjectionLocation { get; }
        public int BaseColorLocation { get; }
        public int BaseMapLocation { get; }
        public int TilingOffsetLocation { get; }
        public int NormalScaleLocation { get; }
        public int NormalMapLocation { get; }
        public int SmoothnessLocation { get; }
        public int MetallicLocation { get; }
        public int MetallicMapLocation { get; }
        public int SpecularColorlLocation { get; }
        public int SpecularMaplLocation { get; }
        public int EmissiveColorLocation { get; }
        public int EmissiveMapLocation { get; }
        public int OcclusionStrengthLocation { get; }
        public int OcclusionMapLocation { get; }
        public int MatCapColorlLocation { get; }
        public int MatCapMapLocation { get; }
        public int ViewPosWSLocation { get; }
        public int MainLightDirLocation { get; }
        public int ShadowMapLocation { get; }
        public int MatrixMainLightViewLocation { get; }
        public int MatrixMainLightProjectionLocation { get; }
        public int MainLightShadowRangeLocation { get; }
        public int TemporalJitterLocation { get; }
        public int AlphaHashedLocation { get; }
        public int AlphaDitherLocation { get; }
        public int ScreenSizeLocation { get; }

        public BlendMode Mode { get; set; }

        public Material()
        {
            BaseColor = new float4(0.5f, 0.5f, 0.5f, 1f);
            BaseMap = Texture.White;
            TilingOffset = new float4(1f, 1f, 0f, 0f);
            NormalScale = 1f;
            NormalMap = Texture.Normal;
            Smoothness = 0.5f;
            Metallic = 0.5f;
            MetallicMap = Texture.White;
            SpecularColor = new float4(1f, 1f, 1f, 1f);
            SpecularMap = Texture.White;
            EmissiveColor = new float3(0f, 0f, 0f);
            EmissiveMap = Texture.Black;
            OcclusionStrength = 1f;
            OcclusionMap = Texture.White;
            MatCapColor = new float3(0.5f, 0.5f, 0.5f);
            MatCapMap = Texture.White;

            Shader = Shader.Default;

            MatrixModelLocation = Shader.GetUniformLocation("MatrixModel");
            MatrixViewLocation = Shader.GetUniformLocation("MatrixView");
            MatrixProjectionLocation = Shader.GetUniformLocation("MatrixProjection");
            BaseColorLocation = Shader.GetUniformLocation("_BaseColor");
            BaseMapLocation = Shader.GetUniformLocation("_BaseMap");
            TilingOffsetLocation = Shader.GetUniformLocation("_TilingOffset");
            NormalScaleLocation = Shader.GetUniformLocation("_NormalScale");
            NormalMapLocation = Shader.GetUniformLocation("_NormalMap");
            SmoothnessLocation = Shader.GetUniformLocation("_Smoothness");
            MetallicLocation = Shader.GetUniformLocation("_Metallic");
            MetallicMapLocation = Shader.GetUniformLocation("_MetallicMap");
            SpecularColorlLocation = Shader.GetUniformLocation("_SpecularColor");
            SpecularMaplLocation = Shader.GetUniformLocation("_SpecularMap");
            EmissiveColorLocation = Shader.GetUniformLocation("_EmissiveColor");
            EmissiveMapLocation = Shader.GetUniformLocation("_EmissiveMap");
            OcclusionStrengthLocation = Shader.GetUniformLocation("_OcclusionStrength");
            OcclusionMapLocation = Shader.GetUniformLocation("_OcclusionMap");
            MatCapColorlLocation = Shader.GetUniformLocation("_MatCapColor");
            MatCapMapLocation = Shader.GetUniformLocation("_MatCapMap");
            ViewPosWSLocation = Shader.GetUniformLocation("_ViewPosWS");
            MainLightDirLocation = Shader.GetUniformLocation("_MainLightDir");
            ShadowMapLocation = Shader.GetUniformLocation("_ShadowMap");
            MatrixMainLightViewLocation = Shader.GetUniformLocation("MatrixMainLightView");
            MatrixMainLightProjectionLocation = Shader.GetUniformLocation("MatrixMainLightProjection");
            MainLightShadowRangeLocation = Shader.GetUniformLocation("_ShadowRange");
            TemporalJitterLocation = Shader.GetUniformLocation("_Jitter");
            AlphaHashedLocation = Shader.GetUniformLocation("_AlphaHashed");
            AlphaDitherLocation = Shader.GetUniformLocation("_AlphaDither");
            ScreenSizeLocation = Shader.GetUniformLocation("_ScreenSize");
        }
    }
}