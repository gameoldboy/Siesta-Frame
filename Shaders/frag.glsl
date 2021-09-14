#version 330 core

in vec2 uv;
in vec3 normalWS;
in vec3 normalVS;
in vec3 worldPos;

uniform sampler2D uTexture0;
uniform vec3 viewPos;

out vec4 FragColor;

void main()
{
    vec3 lightDir = normalize(vec3(1, 2, 1));

    vec3 matcapNrm = normalize(normalVS) * 0.5 + 0.5;
    vec4 matcap = texture(uTexture0, matcapNrm.xy);

    vec3 normal = normalize(normalWS);
    float NdotL = max(dot(normal, lightDir), 0);

    vec3 viewDir = normalize(viewPos - worldPos);
    vec3 reflectDir = reflect(-lightDir, normal);

    float specular = pow(max(dot(viewDir, reflectDir), 0.0), 100);

    float NdotV = dot(normal, viewDir);
    FragColor = matcap * 0.5 + NdotL * 0.5 + specular;
}