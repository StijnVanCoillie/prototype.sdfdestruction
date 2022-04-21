Shader "Stijn/DestructionShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)

        _SizeMaximum("Maximum Size", FLOAT) = 1.0
        _SDF("SDF Asset", 3D) = "" {}
        _SDF_Destruction("SDF Destruction", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Custom lighting model, Lambertian inspired
        #pragma surface surf CustomLighting vertex:vert fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #pragma exclude_renderers d3d11_9x
        #pragma exclude_renderers d3d9

        struct Input
        {
            float3 posSDF;
            float3 dirSDF;
        };

        struct SurfaceOutputCustom
        {
            fixed3 Albedo;
            fixed3 Normal;
            fixed3 Emission;
            half Specular;
            fixed Gloss;
            fixed Alpha;
            fixed Lit;
        };

        fixed4 _Color;

        float _SizeMaximum;
        sampler3D _SDF;
        sampler3D _SDF_Destruction;

        float4 LightingCustomLighting(SurfaceOutputCustom s, half3 lightDir, half atten)
        {
            if (s.Lit > 0.5) // Surface
            {
                half NdotL = dot(s.Normal, lightDir);
                half4 c;
                c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
                c.a = s.Alpha;
                return c;
            }

            return float4(s.Albedo, s.Alpha);
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            data.posSDF = (v.vertex / _SizeMaximum + 1.) * 0.5;
            
            // Calculate direction
            float3 c = (mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)) / _SizeMaximum + 1.) * 0.5;
            data.dirSDF = data.posSDF - c;
        }

        float opSubtraction(float d1, float d2) { return max(-d1, d2); } // d2 is the object you want to cut things out of

        float distanceFunction(float3 pos)
        {
            float d1 = tex3D(_SDF, pos).r; // RFloat textureformat
            float d2 = tex3D(_SDF_Destruction, pos).r; // RFloat textureformat
            return opSubtraction(d2, d1);
        }

        float3 calcNormal(float3 pos)
        {
            float3 n = float3(0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float3 e = 0.5773 * (2.0 * float3((((i + 3) >> 1) & 1), ((i >> 1) & 1), (i & 1)) - 1.0);
                //n += e * distanceFunction(pos + 0.0002 * e);
                n += e * tex3D(_SDF_Destruction, pos + 0.0001 * e).r;
                if (n.x + n.y + n.z > 100.0) break;
            }
            return normalize(n);
        }
        
        float calcAO(float3 pos, float nor)
        {
            float ao = 0.0;
            float totao = 0.0;
            float sca = 10.0;
            for (int aoi = 0; aoi < 5; aoi++)
            {
                float hr = 0.01 + 0.015 * float(aoi * aoi);
                float3 aopos = pos + hr * nor;
                float dd = distanceFunction(aopos);
                ao = -(dd - hr);
                totao += ao * sca;
                sca *= 0.5;
                //if( totao>1000.0+sin(iTime) ) break;
            }
            return 1.0 - clamp(totao, 0.0, 1.0);
        }

        float calcSoftshadow(in float3 ro, in float3 rd, float mint, float maxt)
        {
            float res = 1.0; 
            float ph = 1e20;
            [unroll(16)]
            for (float t = mint; t < maxt; t += ph)
            {
                float h = distanceFunction(ro + rd * t);
                if (h < 0.001)
                    return 0.0;
                float y = h * h / (2.0 * ph);
                float d = sqrt(h * h - y * y);
                res = min(res, 10.0 * d / max(0.0, t - y));
                ph = h;
                t += h;
            }
            return res;
        }

        float calcShadow(float3 ro, float3 rd, float mint, float maxt)
        {
            for (int i = 0; i < 64; ++i)
            {
                float h = distanceFunction(ro + rd * mint);
                if (h < 0.001)
                {
                    return 0.0;
                }
                if (mint > maxt)
                {
                    break;
                }

                mint += h;
            }
            return 1.0;
            /*for (float t = mint; t < maxt;)
            {
                float h = distanceFunction(ro + rd * t);
                if (h < 0.001)
                {
                    return 0.0;
                }
                t += h;
            
            return 1.0;*/
        }

        float4 RayMarchSimple( float3 ro, float3 rd)
        {
            float4 value = float4(0,0,0,-1);

            const int maxstep = 128;
            float t = 0;

            [unroll(128)]
            for (int i = 0; i < maxstep; ++i)
            {
                float3 p = ro + rd * t;

                float dist = distanceFunction(p);

                if (dist < 0.001)
                {
                    value.rgb = float3(1, 0, 0);
                    //value.rgb = _Color.rgb;

                    value.a = 1;

                    //value.rgb = calcNormal(p);

                    // Shading, basic implementation, for now...
                    float3 n = -calcNormal(p);
                    //float3 n = float3(1, 0, 0);

                    float3 lightDir = _WorldSpaceLightPos0.xyz;

                    float diff = dot(n, lightDir);
                    diff = lerp((diff + 1) * 0.5, max(diff, 0.), 0.5);
                    value.xyz *= diff;

                    //float softShadow = calcSoftshadow(p, lightDir, 0.1, 6.0);//0.01, 3.0
                    //float ao = calcAO(p, n);

                    //value.rgb *= ao;

                    //float shadow = calcShadow(p, lightDir, 0.1, 6.0);

                    break;
                }

                t += dist;
            }

            /*if (value.w > 0)
            {
                float3 p = ro + rd * t;
                float3 n = tex3D(_SDF_Destruction, p).rgb;
                //value.rgb = n;

                float3 lightDir = _WorldSpaceLightPos0.xyz;

                float diff = dot(n, lightDir);
                diff = lerp((diff + 1) * 0.5, max(diff, 0.), 0.5);
                value.xyz *= diff;
            }*/

            return value;
        }

        void surf( Input IN, inout SurfaceOutputCustom o)
        {
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Lit = 1;

            float a = tex3D(_SDF_Destruction, IN.posSDF).r; // RFloat textureformat
            if( a < 0.)
            {
                //raymarching the shit out of it...
                float4 raymarch = RayMarchSimple(IN.posSDF, normalize(IN.dirSDF));
                o.Albedo = raymarch.xyz;
                o.Emission = o.Albedo;

                o.Lit = 0;
                clip(raymarch.w);
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
