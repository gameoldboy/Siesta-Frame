#version 330 core

in vec4 _TexCoords;
in vec4 _PositionCS;
in vec4 _PrevPosCS;

uniform sampler2D _BaseMap;
uniform vec4 _TilingOffset;
uniform bool _AlphaTest;

out vec2 FragColor;

void main()
{
    if(_AlphaTest)
    {
        vec2 uv = _TexCoords.xy * _TilingOffset.xy + _TilingOffset.zw;
        float baseMapAlpha = texture(_BaseMap, uv).w;
        if(baseMapAlpha < 0.1)
        {
            discard;
        }
    }

    vec2 screenPos = _PositionCS.xy / _PositionCS.w * 0.5;
    vec2 prevScreenPos = _PrevPosCS.xy / _PrevPosCS.w * 0.5;

    FragColor = screenPos - prevScreenPos;
}