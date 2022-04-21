Shader "Unlit/DestructionUnlitShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)

        _SizeMaximum("Maximum Size", FLOAT) = 1.0
        _SDF("SDF Asset", 3D) = "" {}
        _SDF_Destruction("SDF Destruction", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "RaymarchUtils.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 posSDF : TEXCOORD1;
                float3 dirSDF : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.posSDF = (v.vertex / _SizeMaximum + 1.) * 0.5;
                // Calculate direction
                //float3 c = (mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)) / _SizeMaximum + 1.) * 0.5;
                //o.dirSDF = o.posSDF - c;
                o.dirSDF = (mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)) / _SizeMaximum + 1.) * 0.5;

                // Default
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color;

                float a = tex3D(_SDF_Destruction, i.posSDF).r; // RFloat textureformat
                if (a < 0)//0.
                {
                    //raymarching the shit out of it...
                    //float4 raymarch = RayMarchSimple(i.posSDF, normalize(i.dirSDF));
                    //float4 raymarch = RayMarchSimple(i.posSDF, normalize(i.posSDF - i.dirSDF));
                    float4 raymarch = RayMarchComplex(i.posSDF, normalize(i.posSDF - i.dirSDF));
                    if (raymarch.a > 0)
                    {
                        float3 normal = -calcNormal(raymarch.xyz);
                        float light = customLighting(normal, _WorldSpaceLightPos0.xyz, 1.0);

                        //col.rgb = customLighting(normal, _WorldSpaceLightPos0.xyz, _LightColor0.rgb, 1.0);

                        //float s = calcShadow(raymarch.xyz, _WorldSpaceLightPos0.xyz, 0.01, 1.0);
                        //float s = calcSoftshadow(raymarch.xyz, _WorldSpaceLightPos0.xyz, 0.001, 1.0);
                        //s = map(s, 0, 1, 0.5, 1);

                        float z = raymarch.z;

                        //col.rgb *= s * light * z;
                        //col.rgb = normal;
                        col.rgb *= light * z;
                    }

                    clip(raymarch.w);
                }
                else
                {
                    float light = customLighting(i.normal, _WorldSpaceLightPos0.xyz, 1.0);

                    col.rgb *= light;
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
