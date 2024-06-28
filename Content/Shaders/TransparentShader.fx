// Shader that is used to remove transparent alpha pixels on textures when rendering
#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_2_0
 #define PS_SHADERMODEL ps_2_0
 #else
 #define VS_SHADERMODEL vs_4_0_level_9_1
 #define PS_SHADERMODEL ps_4_0_level_9_1
 #endif


 float4x4 WorldViewProjection; //worldviewprojection matrix
 Texture2D Texture : register(t0); //texture atlas
 sampler TextureSampler : register(s0) //sampler used in calculations
 {
     Texture = (Texture);
 };

 struct VertexShaderInput //Shader gets position of vertex, color of vertex, and coordinate of its UV
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

 struct PixelShaderOutput //returns color of pixel
 {
     float4 Color : COLOR0;
 };

//Does nothing to verticies themselves
 VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
 {
     VertexShaderOutput output;
     output.Position = mul(input.Position, WorldViewProjection);
     output.Color = input.Color;
     output.TexureCoordinate = input.TexureCoordinate;
     return output;
 }

//Shader that calculates color for each individual pixel
 PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
 {
    PixelShaderOutput output; //make output var
    float4 outCol = tex2D(TextureSampler, input.TexureCoordinate) * input.Color; //Get current default color
    clip((outCol.a - 0.9) * 1); //clip anything that is above alpha 0 i thinkw
    output.Color = outCol; //set that as its color
    
     return output; //return the output
 }

 technique
 {
     pass
     {
         VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
         PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
     }
 }