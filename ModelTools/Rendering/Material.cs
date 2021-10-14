using Unity.Mathematics;

namespace ModelTools.Rendering
{
    public class Material
    {
        public enum BlendMode
        {
            None,
            AlphaDither
        }

        public float4 BaseColor { get; set; }
        public Texture BaseMap { get; set; }

        public Shader Shader { get; set; }

        public int MatrixModelLocation { get; private set; }
        public int MatrixViewLocation { get; private set; }
        public int MatrixProjectionLocation { get; private set; }
        public int BaseColorLocation { get; private set; }
        public int BaseMapLocation { get; private set; }
        public int ViewPosWSLocation { get; private set; }
        public int MainLightDirLocation { get; private set; }
        public int AlphaDitherLocation { get; private set; }
        public int ScreenSizeLocation { get; private set; }

        public BlendMode Mode { get; set; }

        public Material()
        {
            BaseColor = new float4(1f);
            BaseMap = Texture.White;

            Shader = Shader.Default;

            UpdateShaderLocation();
        }

        public void UpdateShaderLocation()
        {
            MatrixModelLocation = Shader.GetUniformLocation("MatrixModel");
            MatrixViewLocation = Shader.GetUniformLocation("MatrixView");
            MatrixProjectionLocation = Shader.GetUniformLocation("MatrixProjection");
            BaseColorLocation = Shader.GetUniformLocation("_BaseColor");
            BaseMapLocation = Shader.GetUniformLocation("_BaseMap");
            ViewPosWSLocation = Shader.GetUniformLocation("_ViewPosWS");
            MainLightDirLocation = Shader.GetUniformLocation("_MainLightDir");
            AlphaDitherLocation = Shader.GetUniformLocation("_AlphaDither");
            ScreenSizeLocation = Shader.GetUniformLocation("_ScreenSize");
        }
    }
}