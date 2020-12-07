
sampler2D _PositionOffsetTex;
float _OneOverSurfaceSize;

void TransformPosition(float4 positionOS, out float4 positionWS, out float4 positionCS){            
    positionWS = mul(UNITY_MATRIX_M, positionOS); 
    
#if ApplyTransformTex
    positionWS = float4(0,0,positionWS.z,0) + tex2Dlod(_PositionOffsetTex, float4(positionWS.xy*_OneOverSurfaceSize,0,0));
#endif
    
    positionCS = mul(UNITY_MATRIX_VP, positionWS);
}