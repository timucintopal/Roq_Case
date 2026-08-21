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
        ZWrite Off

        Pass
        {
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
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 pos = IN.positionOS.xyz;
                
                // Normalize peel direction
                float2 dir = normalize(_PeelDirection.xy);
                
                // Calculate distance of vertex along the peel direction
                float d = dot(pos.xy, dir);
                
                // Map _PeelAmount (0 to 1) to a peel line position.
                // When _PeelAmount is 0, peel line is at _SpriteSize (far away, nothing peeled)
                // When _PeelAmount is 1, peel line is at -_SpriteSize (fully peeled)
                float peelLine = _SpriteSize - (_PeelAmount * _SpriteSize * 2.0); 
                
                float distToLine = d - peelLine;
                OUT.isBackface = 0.0;

                if (distToLine > 0.0)
                {
                    // Vertex is in the curled part
                    float theta = distToLine / _CurlRadius;
                    
                    // Limit how far it can curl (optional, but good for stickers so they don't roll infinitely)
                    // We allow it to roll up to PI (folded flat back) or slightly more.
                    if (theta > 3.14159) 
                    {
                        // Flat part after the roll
                        float flatDist = distToLine - (3.14159 * _CurlRadius);
                        pos.xy = pos.xy - dir * distToLine - dir * flatDist;
                        pos.z = -(_CurlRadius * 2.0); // Folded completely back towards camera
                        OUT.isBackface = 1.0;
                    }
                    else
                    {
                        // Curved part
                        float x_prime = sin(theta) * _CurlRadius;
                        float z_prime = (1.0 - cos(theta)) * _CurlRadius;
                        
                        // If theta > PI/2 (90 degrees), the surface normal points away, showing the back of the sticker
                        if (theta > 1.5708) {
                             OUT.isBackface = 1.0;
                        }
                        
                        // Apply back to object space
                        pos.xy = pos.xy - dir * distToLine + dir * x_prime;
                        pos.z = -z_prime; // -Z moves it towards the camera (assuming standard 2D view)
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
                
                // Only modify RGB where the sprite actually has alpha
                if (col.a > 0.05 && IN.isBackface > 0.5)
                {
                    // Apply foil effect to the back of the sticker
                    half4 foil = _FoilColor;
                    
                    // Fake shiny effect based on UVs and a sine wave
                    float shine = sin(IN.uv.x * 15.0 + IN.uv.y * 15.0) * 0.5 + 0.5;
                    foil.rgb += shine * _FoilShininess;
                    
                    col.rgb = foil.rgb;
                }
                
                return col;
            }
            ENDHLSL
        }
    }
}
