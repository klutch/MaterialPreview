//////////////////////////////////////////
// Create a normal map from a texture
//////////////////////////////////////////
float4 getNormal(sampler textureSampler, float2 texCoords, float normalStrength)
{
	float4 source = tex2D(textureSampler, texCoords);
	float base = (source.r + source.g + source.b) / 3;
	float3 normal = float3(0.5, 0.5, 1) * 2 - 1;

	////////////////////////////////////////////
	// x00 - x10 - x20
	//  |     |     |
	// x01 - x11 - x21     x11 = base
	//  |     |     |
	// x02 - x12 - x22
	////////////////////////////////////////////

	// First sampling
	float x01 = (tex2D(textureSampler, texCoords + float2(1 / renderSize.x, 0)).rgb * 2 - 1);
	float x10 = (tex2D(textureSampler, texCoords + float2(0, 1 / renderSize.y)).rgb * 2 - 1);
	float x21 = (tex2D(textureSampler, texCoords + float2(-1 / renderSize.x, 0)).rgb * 2 - 1);
	float x12 = (tex2D(textureSampler, texCoords + float2(0, -1 / renderSize.y)).rgb * 2 - 1);

	// Second sampling
	x01 += (tex2D(textureSampler, texCoords + float2(2 / renderSize.x, 0)).rgb * 2 - 1) / 2;
	x10 += (tex2D(textureSampler, texCoords + float2(0, 2 / renderSize.y)).rgb * 2 - 1) / 2;
	x21 += (tex2D(textureSampler, texCoords + float2(-2 / renderSize.x, 0)).rgb * 2 - 1) / 2;
	x12 += (tex2D(textureSampler, texCoords + float2(0, -2 / renderSize.y)).rgb * 2 - 1) / 2;

	// Third sampling
	x01 += (tex2D(textureSampler, texCoords + float2(3 / renderSize.x, 0)).rgb * 2 - 1) / 4;
	x10 += (tex2D(textureSampler, texCoords + float2(0, 3 / renderSize.y)).rgb * 2 - 1) / 4;
	x21 += (tex2D(textureSampler, texCoords + float2(-3 / renderSize.x, 0)).rgb * 2 - 1) / 4;
	x12 += (tex2D(textureSampler, texCoords + float2(0, -3 / renderSize.y)).rgb * 2 - 1) / 4;

	// Fourth sampling
	x01 += (tex2D(textureSampler, texCoords + float2(4 / renderSize.x, 0)).rgb * 2 - 1) / 4;
	x10 += (tex2D(textureSampler, texCoords + float2(0, 4 / renderSize.y)).rgb * 2 - 1) / 4;
	x21 += (tex2D(textureSampler, texCoords + float2(-4 / renderSize.x, 0)).rgb * 2 - 1) / 4;
	x12 += (tex2D(textureSampler, texCoords + float2(0, -4 / renderSize.y)).rgb * 2 - 1) / 4;

	float u = (x01 - base) - (x21 - base);
	float v = (x10 - base) - (x12 - base);
	normal.r += u * normalStrength;
	normal.g += v * normalStrength;
	normal.rgb = (normal.rgb + 1) / 2;
	
	return float4(normal, 1);
}