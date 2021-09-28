#version 330 core

in vec3 _PositionWS;
in vec4 _TexCoords;
in mat3 _TBN;
in vec4 _PositionLS;

uniform vec4 _BaseColor;
uniform sampler2D _BaseMap;
uniform vec4 _TilingOffset;
uniform float _NormalScale;
uniform sampler2D _NormalMap;
uniform float _Smoothness;
uniform float _Metallic;
uniform sampler2D _MetallicMap;
uniform vec4 _SpecularColor;
uniform sampler2D _SpecularMap;
uniform vec3 _EmissiveColor;
uniform sampler2D _EmissiveMap;
uniform float _OcclusionStrength;
uniform sampler2D _OcclusionMap;
uniform vec3 _MatCapColor;
uniform sampler2D _MatCapMap;
uniform vec3 _ViewPosWS;
uniform vec3 _MainLightDir;
uniform mat4 MatrixView;
uniform sampler2DShadow _ShadowMap;
uniform float _ShadowRange;
uniform vec2 _TemporalJitter;

vec2 _ShadowMapSize = textureSize(_ShadowMap, 0);
vec2 _ShadowMapTexelSize = 1.0 / _ShadowMapSize;

out vec4 FragColor;

vec2 poissonDisk[16] = vec2[]( 
   vec2( -0.94201624, -0.39906216 ), 
   vec2( 0.94558609, -0.76890725 ), 
   vec2( -0.094184101, -0.92938870 ), 
   vec2( 0.34495938, 0.29387760 ), 
   vec2( -0.91588581, 0.45771432 ), 
   vec2( -0.81544232, -0.87912464 ), 
   vec2( -0.38277543, 0.27676845 ), 
   vec2( 0.97484398, 0.75648379 ), 
   vec2( 0.44323325, -0.97511554 ), 
   vec2( 0.53742981, -0.47373420 ), 
   vec2( -0.26496911, -0.41893023 ), 
   vec2( 0.79197514, 0.19090188 ), 
   vec2( -0.24188840, 0.99706507 ), 
   vec2( -0.81409955, 0.91437590 ), 
   vec2( 0.19984126, 0.78641367 ), 
   vec2( 0.14383161, -0.14100790 ) 
);

float random(vec3 seed, int i){
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898,78.233,45.164,94.673));
	return fract(sin(dot_product) * 43758.5453);
}

#define PI 3.1415926

void main()
{
    vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
    vec4 baseMapColor = texture(_BaseMap, uv);
    vec3 normapMapVec = vec3(texture(_NormalMap, uv).xy, 0.0);
    normapMapVec = (normapMapVec * 2.0 - 1.0) * _NormalScale;
    normapMapVec.z = sqrt(1.0 - normapMapVec.x * normapMapVec.x - normapMapVec.y * normapMapVec.y);
    
    vec3 normalWS = normalize(_TBN * normapMapVec);

    vec3 normalVS = (MatrixView * vec4(normalWS, 0.0)).xyz * 0.5 + 0.5;
    vec3 matcap = texture(_MatCapMap, normalVS.xy).xyz;

    vec3 mainLightDir = normalize(_MainLightDir);
    float NdotL = max(dot(normalWS, mainLightDir), 0.0);

    vec3 viewDiffPosWS = _ViewPosWS - _PositionWS;
    vec3 viewDir = normalize(viewDiffPosWS);
    vec3 reflectDir = reflect(-mainLightDir, normalWS);

    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 1.0 + 100.0 * _SpecularColor.w);

    vec3 shadowMapProjCoords = _PositionLS.xyz / _PositionLS.w;
    shadowMapProjCoords = shadowMapProjCoords * 0.5 + 0.5;

    
    float bias = max(0.005 * (1.0 - NdotL), 0.0005);
    float mainLightDepth = shadowMapProjCoords.z - bias;

    float shadow = 0;
    if(shadowMapProjCoords.x < 0.0 || shadowMapProjCoords.x > 1.0 ||
       shadowMapProjCoords.y < 0.0 || shadowMapProjCoords.y > 1.0 ||
       shadowMapProjCoords.z < 0.0 || shadowMapProjCoords.z > 1.0)
    {
        shadow = 1.0;
    }
    else
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = 2.0 * PI * random(_PositionWS * 1000, i);
            float s = sin(angle);
            float c = cos(angle);
            vec2 rotatedOffset = vec2(
                poissonDisk[i].x * c + poissonDisk[i].y * s,
                poissonDisk[i].x * -s + poissonDisk[i].y * c);
            shadow += texture(_ShadowMap,
                vec3(shadowMapProjCoords.xy +
                _ShadowMapTexelSize * rotatedOffset * 4.0, mainLightDepth));
        }
        shadow *= 0.0625;
        float shadowFadeOut = smoothstep(_ShadowRange - 1.0, _ShadowRange, length(viewDiffPosWS));
        shadow = mix(shadow, 1.0, shadowFadeOut);
    }
    
    vec3 finalColor = (matcap * _MatCapColor + NdotL * shadow) *
                    baseMapColor.xyz * _BaseColor.xyz +
                    specular * _SpecularColor.xyz * shadow;

    FragColor = vec4(finalColor, baseMapColor.w * _BaseColor.w);
}