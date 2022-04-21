fixed4 _Color;
float _SizeMaximum;
sampler3D _SDF;
sampler3D _SDF_Destruction;

float map(float s, float a1, float a2, float b1, float b2)
{
    return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
}

float opSubtraction(float d1, float d2) { return max(-d1, d2); } // d2 is the object you want to cut things out of

float sdBox(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float distanceFunction(float3 pos)
{
    // Early test
    //float s = 1.0 * 0.5;
    //float d1 = sdBox(pos - float3(0.5,0.5,0.5), float3(s, s, s));

    float d1 = tex3D(_SDF, pos).r;
    float d2 = tex3D(_SDF_Destruction, pos).r;
    return opSubtraction(d2, d1);
}

float3 calcNormalOld(float3 p)
{
    const float eps = 0.0001; // or some other value
    const float2 h = float2(eps, 0);
    return normalize(float3(tex3D(_SDF_Destruction, p + h.xyy).r - tex3D(_SDF_Destruction, p - h.xyy).r,
        tex3D(_SDF_Destruction, p + h.yxy).r - tex3D(_SDF_Destruction, p - h.yxy).r,
        tex3D(_SDF_Destruction, p + h.yyx).r - tex3D(_SDF_Destruction, p - h.yyx).r));
}

float3 calcNormal(float3 pos)
{
    float3 n = float3(0, 0, 0);
    for (int i = 0; i < 4; i++)
    {
        float3 e = 0.5773 * (2.0 * float3((((i + 3) >> 1) & 1), ((i >> 1) & 1), (i & 1)) - 1.0);
        //n += e * distanceFunction(pos + 0.0002 * e);
        n += e * tex3D(_SDF_Destruction, pos + 0.2 * e).r;//0.02 0.002
        if (n.x + n.y + n.z > 100.0) break;
    }
    return normalize(n);
}

float calcSoftshadow(float3 ro, float3 rd, float mint, float maxt)
{
    float res = 1.0;
    float ph = 1e20;
    for (int i = 0; i < 16; ++i)
    {
        float h = distanceFunction(ro + rd * mint);
        if (h < 0.001)
        {
            return 0.0;
        }
        float y = h * h / (2.0 * ph);
        float d = sqrt(h * h - y * y);
        res = min(res, 10.0 * d / max(0.0, mint - y));
        ph = h;
        mint += h;
    }
    return res;
}

float calcShadow(float3 ro, float3 rd, float mint, float maxt)
{
    for (int i = 0; i < 16; ++i)
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
}

float customLighting(half3 normal, half3 lightDir, half atten)
{
    //return dot(normal, lightDir) * atten;
    float l = dot(normal, lightDir) * atten;
    return lerp((l + 1) * 0.5, max(l, 0.), 0.5);
}

float4 RayMarchSimple(float3 ro, float3 rd)
{
    float4 value = float4(1, 0, 0, -1);

    const int maxstep = 128;//128
    float t = 0;

    //[unroll(128)]
    for (int i = 0; i < maxstep; ++i)
    {
        float3 p = ro + rd * t;

        float dist = distanceFunction(p);

        if (dist < 0.001)
        {
            //value.rgb = _Color.rgb;
            value.xyz = p;
            value.a = 1;

            break;
        }

        t += dist;
    }

    return value;
}

float4 RayMarchComplex(float3 ro, float3 rd)
{
    float4 value = float4(1, 0, 0, -1);

    const int maxstep = 256;//128
    float t = 0;
    float tmax = _SizeMaximum * 0.5;
    tmax = 1.0;

    //[unroll(1024)]
    for (int i = 0; i < maxstep && t < tmax; ++i)
    {
        float3 p = ro + rd * t; 

        float dist = distanceFunction(p);

        if (dist < 0.0005*t)
        {
            //value.rgb = _Color.rgb;
            value.xyz = p;
            value.a = 1;

            break;
        }

        t += dist * 0.5;

        //if (t > 10) break;
    }

    return value;
}