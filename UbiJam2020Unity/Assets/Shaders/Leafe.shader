Shader "Unlit/Leaf"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("_Mask", 2D) = "white" {}
        _LocalMask ("LocalMask", 2D) = "white" {}
        _PatchTex ("_Mask", 2D) = "white" {}
        _FogColor ("_FogColor", Color) = (0,0,0,0)
        [Toggle]_ShowMap ("ShowMask", Float) = 0
        _PatchTransformation ("_PatchTransformation", Vector) = (0,0,0,0)
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Mask;
            sampler2D _PatchTex;
            sampler2D _LocalMask;
            float4 _MainTex_ST;
            float4 _Mask_ST;
            float4 _FogColor;

            float _ShowMap;
            float3 _PatchTransformation;
    
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.positionWS = mul(UNITY_MATRIX_M,v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed maskCol = tex2D(_Mask, i.uv);
                fixed localMaskCol = tex2D(_LocalMask, i.uv);
                
                
                float2 patchUV = ((_PatchTransformation.xy-i.positionWS)/(_PatchTransformation.z*2))+0.5;
                
                
                fixed4 patchCol = 0;
               
                if(patchUV.x>0&&patchUV.y>0&&patchUV.x<1&&patchUV.y<1){
                    patchCol = tex2D(_PatchTex, patchUV);
                }
                
                col.rgb = lerp(col, patchCol.rgb,patchCol.a);
                
                col.rgb=lerp(col.rgb, _FogColor.rgb,min(0.8,_FogColor.a*i.positionWS.z));
                
                clip(maskCol*col.a-0.5);
                
                if(_ShowMap>0){
                    if(localMaskCol<0.50&&maskCol>0.5){
                        return float4(1,0,0,1);
                    }else{
                        return maskCol;
                    }
                }else{
                    return float4(col);
                }
            }
            ENDCG
        }
    }
}
