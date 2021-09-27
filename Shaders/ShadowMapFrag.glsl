#version 330 core

in vec4 _TexCoords;

uniform sampler2D _BaseMap;
uniform vec4 _TilingOffset;
uniform bool _AlphaTest;

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
}