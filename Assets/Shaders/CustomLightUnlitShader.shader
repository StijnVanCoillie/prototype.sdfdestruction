Shader "Unlit/CustomLightUnlitShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
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
            };

            fixed4 _Color;

            float customLighting(half3 normal, half3 lightDir, half atten)
            {
                float l = dot(normal, lightDir) * atten;
                return lerp((l + 1) * 0.5, max(l, 0.), 0.5);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;

                float light = customLighting(i.normal, _WorldSpaceLightPos0.xyz, 1.0);
                col.rgb *= light;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
