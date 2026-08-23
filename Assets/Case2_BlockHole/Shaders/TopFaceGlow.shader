Shader "Custom/TopFaceGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _NormalThreshold ("Normal Eşiği", Range(0, 1)) = 0.50
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        LOD 100
        
        Blend One One
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            float4 _GlowColor;
            float _NormalThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float upDot = dot(normalize(i.worldNormal), float3(0, 1, 0));
                
                // Daha önce 0.5 olan eşiği, _NormalThreshold (0.95) yaptık.
                // Bu sayede, yumuşatılmış (smooth) kenarlar veya eğimli iç duvarlar parlamayacak.
                // SADECE ve SADECE tam anlamıyla dümdüz yukarı bakan yüzeyler parlayacak.
                if (upDot >= _NormalThreshold)
                {
                    return _GlowColor;
                }
                
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
