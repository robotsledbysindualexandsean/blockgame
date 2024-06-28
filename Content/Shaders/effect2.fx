// Lithiums shader Example
#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_2_0
 #define PS_SHADERMODEL ps_2_0
 #else
 #define VS_SHADERMODEL vs_4_0_level_9_1
 #define PS_SHADERMODEL ps_4_0_level_9_1
 #endif


 float4x4 WorldViewProjection;
 Texture2D Texture : register(t0);

 sampler TextureSampler : register(s0)
 {
     Texture = (Texture);
 };

 struct VertexShaderInput
 {
     float4 Position : POSITION0;
     float4 Color : COLOR0;
     float2 TexureCoordinate : TEXCOORD0;
 };

 struct VertexShaderOutput
 {
     float4 Position : SV_Position;
     float4 Color : COLOR0;
     float2 TexureCoordinate : TEXCOORD0;
 };

 struct PixelShaderOutput
 {
     float4 Color : COLOR0;
 };

 VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
 {
     VertexShaderOutput output;
     output.Position = mul(input.Position, WorldViewProjection);
     output.Color = input.Color;
     output.TexureCoordinate = input.TexureCoordinate;
     return output;
 }


float wave(float4 p, float angle)
{
    float2 direction = float2(cos(angle), sin(angle));
    return cos((p, direction));
}

float wrap(float x)
{
    return abs(fmod(x, 2.) - 1.);
}


 PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
 {
    PixelShaderOutput output;
    
    float4 p = (input.Position - 0.5f) * 50.;

    float brightness = 0.;
    
    for (int i = 0; i < 11; i++)
    {
        brightness += wave(p, i);
    }

    brightness = wrap(brightness);
    output.Color = input.Color + brightness;
    
     return output;
 }

 technique
 {
     pass
     {
         VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
         PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
     }
 }