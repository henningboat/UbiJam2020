Shader "Unlit/Leaf"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("_Mask", 2D) = "white" {}
        _PatchTex ("_Mask", 2D) = "white" {}
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
            float4 _MainTex_ST;
            float4 _Mask_ST;

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
                fixed4 maskCol = tex2D(_Mask, i.uv);
                
                
                float2 patchUV = ((_PatchTransformation.xy-i.positionWS)/(_PatchTransformation.z*2))+0.5;
                
                
                fixed4 patchCol = 0;
               
                if(patchUV.x>0&&patchUV.y>0&&patchUV.x<1&&patchUV.y<1){
                    patchCol = tex2D(_PatchTex, patchUV);
                }
                
                col.rgb = lerp(col, patchCol.rgb,patchCol.a);
                
                clip(maskCol.a*col.a-0.5);
                
                if(_ShowMap>0)
                    return maskCol;
                else
                    return col;
            }
            ENDCG
        }
    }
}
