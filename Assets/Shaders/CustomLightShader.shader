Shader "Stijn/CustomLightShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Custom lighting model, Lambertian inspired
        #pragma surface surf CustomLighting fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float _useless;
        };

        fixed4 _Color;

        float4 LightingCustomLighting(SurfaceOutput s, half3 lightDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = _Color;
            o.Albedo = c.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
