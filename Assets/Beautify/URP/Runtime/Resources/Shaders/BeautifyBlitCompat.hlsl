#ifndef BEAUTIFY_BLIT_COMPAT_INCLUDED
#define BEAUTIFY_BLIT_COMPAT_INCLUDED
// URP 17 uses a procedural full-screen triangle. These legacy Beautify
// passes still receive the mesh supplied by CommandBuffer.Blit.
#define Attributes BeautifyURPAttributes
#define Varyings BeautifyURPVaryings
#define Vert BeautifyURPVert
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#undef Attributes
#undef Varyings
#undef Vert

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};
Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}
#endif
