Shader "Unlit/SSOcTreeVoxelizedShadow"
{
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
    }
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 200

            Pass{
                CGPROGRAM

                #include "UnityCG.cginc"
                #pragma vertex vert_img
                #pragma fragment frag

                sampler2D  _MainTex;
                sampler2D _CameraDepthTexture;
                uniform	sampler2D _OTreeTex;
                uniform int _OTreeWidth;
                uniform int _unitsPerMeter;
                uniform half4 _wposOffset;
                uniform float4x4 _World2Light;
                uniform int _useLightSpace;
                float4 GetWorldPositionFromDepthValue(float2 uv, float linearDepth)
                {
                    float camPosZ = _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * linearDepth;

                    // unity_CameraProjection._m11 = near / t，其中t是视锥体near平面的高度的一半。
                    // 投影矩阵的推导见：http://www.songho.ca/opengl/gl_projectionmatrix.html。
                    // 这里求的height和width是坐标点所在的视锥体截面（与摄像机方向垂直）的高和宽，并且
                    // 假设相机投影区域的宽高比和屏幕一致。
                    float height = 2 * camPosZ / unity_CameraProjection._m11;
                    float width = _ScreenParams.x / _ScreenParams.y * height;

                    float camPosX = width * uv.x - width / 2;
                    float camPosY = height * uv.y - height / 2;
                    float4 camPos = float4(camPosX, camPosY, camPosZ, 1.0);
                    return mul(unity_CameraToWorld, camPos);
                }
                int4 getTreeValue(int index) {

                    return	tex2D(_OTreeTex, half2(index % _OTreeWidth+0.5, index / _OTreeWidth+0.5)
                        / (half)max(_OTreeWidth, 1));
                }
                fixed shadowValue(float3 wpos)
                {
                    //wpos += half3(1, 0, -1)*2/ _unitsPerMeter;
                    int x = wpos.x * _unitsPerMeter + 0.5;
                    int y = wpos.y * _unitsPerMeter + 0.5;
                    int z = wpos.z * _unitsPerMeter + 0.5;  

                    if (_useLightSpace > 0) {
                        wpos = mul(_World2Light, float4(wpos, 1));
                       
                        x = wpos.x * _unitsPerMeter + 0.5 + 60 * _unitsPerMeter;
                        y = wpos.y * _unitsPerMeter + 0.5 + 60 * _unitsPerMeter;
                        z = wpos.z * _unitsPerMeter + 0.5;
                    }
                    
                   
                    int index = 0;
                    int size = 128 * _unitsPerMeter;
                    int3 linkOffset = 0;
                    [unroll(11)]
                    while (1) {

                        int4 node = getTreeValue(index);
                        int flag = node.w % 10;
                        bool linkTo = flag > 1;

                        flag %= 2;
                        node.w = node.w / 10;

                       
                        if (node.w == 0)return   1 - flag;
                        if (size == 1)return  1;
                    
                        int childIndex = 0;

                        if (x > node.x+ linkOffset.x + size / 2)
                        {
                            childIndex++;
                        }
                        if (y > node.y+ linkOffset.y + size / 2)
                        {
                            childIndex += 2;
                        }
                        if (z > node.z+ linkOffset.z + size / 2)
                        {
                            childIndex += 4;
                        }
                        if (linkTo) {
                            linkOffset += node.xyz - getTreeValue(node.w).xyz;
                        }
                        index = node.w + childIndex;
                        size /= 2;

                    }

                    return 1;
                }
                float4 frag(v2f_img o) : COLOR
                {
                    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, o.uv);
                // 注意：经过投影变换之后的深度和相机空间里的z已经不是线性关系。所以要先将其转换为线性深度。
                // 见：https://developer.nvidia.com/content/depth-precision-visualized
                float linearDepth = Linear01Depth(rawDepth);
                float3 worldpos = GetWorldPositionFromDepthValue(o.uv, linearDepth);
                float atten = 1;
                float scale = 1.0 / _unitsPerMeter;

                atten = shadowValue(worldpos );
              /*  float  atten00 = shadowValue(worldpos + half3(-0.1, 0, 0.1));
                    float  atten10 =  shadowValue(worldpos + half3(scale - 0.1, 0, 0.1));
                    float  atten01 =  shadowValue(worldpos + half3(-0.1, 0, scale + 0.1));
                    float   atten11 =  shadowValue(worldpos + half3(scale - 0.1, 0, scale + 0.1));
                    float fx = (worldpos.x * _unitsPerMeter + 0.5);
                    float fz = (worldpos.z * _unitsPerMeter + 0.5);
                    fx = fx - (int)fx;
                    fz = fz - (int)fz;
                    atten = atten00 * (1 - fx) * (1 - fz) + atten10 * fx * (1 - fz) + atten01 * (1 - fx) * fz + atten11 * fx * fz;*/
                return saturate( atten);
                //return float4(worldpos.xyz / 255.0 , 1.0);  // 除以255以便显示颜色，测试用。
            }
            ENDCG
        }
          Pass{
                CGPROGRAM





                #include "UnityCG.cginc"
                #pragma vertex vert_img
                #pragma fragment frag
                sampler2D  _MainTex;
         static   float2 poisson[12] = { float2(-0.326212f, -0.40581f),
     float2(-0.840144f, -0.07358f),
     float2(-0.695914f, 0.457137f),
     float2(-0.203345f, 0.620716f),
     float2(0.96234f, -0.194983f),
     float2(0.473434f, -0.480026f),
     float2(0.519456f, 0.767022f),
     float2(0.185461f, -0.893124f),
     float2(0.507431f, 0.064425f),
     float2(0.89642f, 0.412458f),
     float2(-0.32194f, -0.932615f),
     float2(-0.791559f, -0.59771f) };

            sampler2D  _TestQTreeMaskTex;
            float4 _TestQTreeMaskTex_TexelSize;
                  float4 frag(v2f_img o) : COLOR
                {
                    float4 col = tex2D(_MainTex, o.uv);
                    float atten = tex2D(_TestQTreeMaskTex, o.uv);
                    for (int k = 0; k < 12; k++) {
                          atten += tex2D(_TestQTreeMaskTex, o.uv + poisson[k] * _TestQTreeMaskTex_TexelSize.xy*8 );
                    }
                    return col *  lerp(0.5, 1, atten / 13);// (pow(atten / 13, 3) + 0.3) / 1.3;
                }
                 ENDCG
                }
    }
}