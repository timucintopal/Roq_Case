Shader "Custom/StickerPeel"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FoilColor ("Foil Color (Back)", Color) = (0.8, 0.8, 0.85, 1)
        
        _PeelAmount ("Peel Amount", Range(0, 1)) = 0
        _PeelDirection ("Peel Direction (XY)", Vector) = (-1, 1, 0, 0)
        _CurlRadius ("Curl Radius", Range(0.01, 1)) = 0.3
        _FoilShininess ("Foil Shininess", Range(0, 1)) = 0.5
        _SpriteSize ("Sprite Size Bound", Float) = 5.0
        
        _ShineLocation ("Shine Location", Range(-1, 3)) = -1
        _ShineWidth ("Shine Width", Float) = 0.15
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        // ÇÖZÜM: Derinlik tamponunu açıyoruz (ZWrite On)
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "StickerPeelPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float isBackface    : TEXCOORD1;
            };

            sampler2D _MainTex;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _FoilColor;
                float _PeelAmount;
                float4 _PeelDirection;
                float _CurlRadius;
                float _FoilShininess;
                float _SpriteSize;
                float _ShineLocation;
                float _ShineWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 pos = IN.positionOS.xyz;
                float2 dir = normalize(_PeelDirection.xy);
                float d = dot(pos.xy, dir);
                
                float peelLine = _SpriteSize - (_PeelAmount * _SpriteSize * 2.0); 
                float distToLine = d - peelLine;
                OUT.isBackface = 0.0;

                if (distToLine > 0.0)
                {
                    float theta = distToLine / _CurlRadius;
                    
                    if (theta > 3.14159) 
                    {
                        float flatDist = distToLine - (3.14159 * _CurlRadius);
                        pos.xy = pos.xy - dir * distToLine - dir * flatDist;
                        pos.z = -(_CurlRadius * 2.0); // Kameraya doğru yaklaştır
                        OUT.isBackface = 1.0;
                    }
                    else
                    {
                        float x_prime = sin(theta) * _CurlRadius;
                        float z_prime = (1.0 - cos(theta)) * _CurlRadius;
                        
                        if (theta > 1.5708) {
                             OUT.isBackface = 1.0;
                        }
                        
                        pos.xy = pos.xy - dir * distToLine + dir * x_prime;
                        pos.z = -z_prime; // Kameraya doğru yaklaştır (-Z)
                    }
                }

                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = tex2D(_MainTex, IN.uv) * IN.color;
                
                // ÇÖZÜM: Şeffaf olan pikselleri (Sprite'ın dışındaki boşlukları) tamamen iptal et.
                // Bu sayede ZWrite açıkken bile arkadaki diğer objeler görünmeye devam eder.
                clip(col.a - 0.05);
                
                if (IN.isBackface > 0.5)
                {
                    half4 foil = _FoilColor;
                    float shine = sin(IN.uv.x * 15.0 + IN.uv.y * 15.0) * 0.5 + 0.5;
                    foil.rgb += shine * _FoilShininess;
                    
                    col.rgb = foil.rgb;
                }
                
                // --- Beyaz Hare / Parlama (Glint) Efekti ---
                if (_ShineLocation > -0.5)
                {
                    // Çapraz bir çizgi elde etmek için x ve y koordinatlarını topluyoruz (0 ile 2 arası bir değer)
                    float diagonal = IN.uv.x + IN.uv.y; 
                    
                    // Çizginin kenarlarını yumuşatmak için smoothstep kullanıyoruz
                    float shineBand = smoothstep(_ShineLocation - _ShineWidth, _ShineLocation, diagonal) 
                                    - smoothstep(_ShineLocation, _ShineLocation + _ShineWidth, diagonal);
                    
                    // Orijinal renge beyaz parlaklık olarak ekle
                    col.rgb += shineBand * col.a * 0.6; // Saydam yerleri parlatma, şiddeti 0.6
                }
                
                return col;
            }
            ENDHLSL
        }
    }
}
