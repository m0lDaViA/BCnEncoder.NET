using System;
using System.Numerics;
using BCnEncoder.Shared;
using Half = BCnEncoder.Shared.Half;

namespace BCnEncoder.Encoder;

/// <summary>
/// Least squares optimization from https://github.com/knarkowicz/GPURealTimeBC6H
/// Code is public domain
/// </summary>
internal static class LeastSquares
{

	private static int ComputeIndex4(float texelPos, float endPoint0Pos, float endPoint1Pos)
	{
		var r = (texelPos - endPoint0Pos) / (endPoint1Pos - endPoint0Pos);
		return (int)Math.Clamp(r * 15f /*14.93333f + 0.03333f + 0.5f*/, 0.0f, 15.0f);
	}

	private static int ComputeIndex3(float texelPos, float endPoint0Pos, float endPoint1Pos)
	{
		var r = (texelPos - endPoint0Pos) / (endPoint1Pos - endPoint0Pos);
		return (int)Math.Clamp(r * 6.98182f + 0.00909f + 0.5f, 0.0f, 7.0f);
	}

	private static uint F32ToF16(float f32) => Half.GetBits(new Half(f32));

	private static Vector3 F32ToF16(Vector3 f32) =>
		new(
			F32ToF16(f32.X),
			F32ToF16(f32.Y),
			F32ToF16(f32.Z)
		);

	private static float F16ToF32(uint f16) => Half.ToHalf((ushort)f16);

	private static Vector3 F16ToF32(Vector3 f16) =>
		new(
			F16ToF32((uint)f16.X),
			F16ToF32((uint)f16.Y),
			F16ToF32((uint)f16.Z)
		);

	public static void OptimizeEndpoints1Sub(RawBlock4X4RgbFloat block, ref ColorRgbFloat ep0, ref ColorRgbFloat ep1)
	{
		var ep0V = ep0.ToVector3();
		var ep1V = ep1.ToVector3();

		var pixels = block.AsSpan;

		var blockDir = ep1V - ep0V;
		blockDir /= (blockDir.X + blockDir.Y + blockDir.Z);

		var endPoint0Pos = (float)F32ToF16(Vector3.Dot(ep0V, blockDir));
		var endPoint1Pos = (float)F32ToF16(Vector3.Dot(ep1V, blockDir));

		var alphaTexelSum = new Vector3();
		var betaTexelSum = new Vector3();
		var alphaBetaSum = 0.0f;
		var alphaSqSum = 0.0f;
		var betaSqSum = 0.0f;

		for (var i = 0; i < 16; i++)
		{
			var texelPos = (float)F32ToF16(Vector3.Dot(pixels[i].ToVector3(), blockDir));
			var texelIndex = ComputeIndex4(texelPos, endPoint0Pos, endPoint1Pos);

			var beta = Math.Clamp(texelIndex / 15.0f, 0f, 1f);
			var alpha = 1.0f - beta;

			var texelF16 = F32ToF16(pixels[i].ToVector3());
			alphaTexelSum += alpha * texelF16;
			betaTexelSum += beta * texelF16;

			alphaBetaSum += alpha * beta;

			alphaSqSum += alpha * alpha;
			betaSqSum += beta * beta;
		}

		var det = alphaSqSum * betaSqSum - alphaBetaSum * alphaBetaSum;

		if (!(MathF.Abs(det) > 0.00001f)) return;
		var detRcp = 1f / (det);
		var ep0F16 = detRcp * (alphaTexelSum * betaSqSum - betaTexelSum * alphaBetaSum);
		var ep1F16 = detRcp * (betaTexelSum * alphaSqSum - alphaTexelSum * alphaBetaSum);
		ep0F16 = Vector3.Clamp(ep0F16, Vector3.Zero, new Vector3(Half.MaxValue.value));
		ep1F16 = Vector3.Clamp(ep1F16, Vector3.Zero, new Vector3(Half.MaxValue.value));
		ep0 = new ColorRgbFloat(F16ToF32(ep0F16));
		ep1 = new ColorRgbFloat(F16ToF32(ep1F16));
	}

	public static void OptimizeEndpoints2Sub(RawBlock4X4RgbFloat block, ref ColorRgbFloat ep0, ref ColorRgbFloat ep1,
		int partitionSetId, int subsetIndex)
	{
		var ep0V = ep0.ToVector3();
		var ep1V = ep1.ToVector3();

		var pixels = block.AsSpan;

		var blockDir = ep1V - ep0V;
		blockDir /= blockDir / (blockDir.X + blockDir.Y + blockDir.Z);

		var endPoint0Pos = (float)F32ToF16(Vector3.Dot(ep0V, blockDir));
		var endPoint1Pos = (float)F32ToF16(Vector3.Dot(ep1V, blockDir));

		var alphaTexelSum = new Vector3();
		var betaTexelSum = new Vector3();
		var alphaBetaSum = 0.0f;
		var alphaSqSum = 0.0f;
		var betaSqSum = 0.0f;

		for (var i = 0; i < 16; i++)
		{
			if (Bc6Block.Subsets2PartitionTable[partitionSetId][i] != subsetIndex) continue;
			var texelPos = (float)F32ToF16(Vector3.Dot(pixels[i].ToVector3(), blockDir));
			var texelIndex = ComputeIndex3(texelPos, endPoint0Pos, endPoint1Pos);

			var beta = Math.Clamp(texelIndex / 7.0f, 0f, 1f);
			var alpha = 1.0f - beta;

			var texelF16 = F32ToF16(pixels[i].ToVector3());
			alphaTexelSum += alpha * texelF16;
			betaTexelSum += beta * texelF16;

			alphaBetaSum += alpha * beta;

			alphaSqSum += alpha * alpha;
			betaSqSum += beta * beta;
		}

		var det = alphaSqSum * betaSqSum - alphaBetaSum * alphaBetaSum;

		if (!(MathF.Abs(det) > 0.00001f)) return;
		var detRcp = 1f / (det);
		var ep0F16 = detRcp * (alphaTexelSum * betaSqSum - betaTexelSum * alphaBetaSum);
		var ep1F16 = detRcp * (betaTexelSum * alphaSqSum - alphaTexelSum * alphaBetaSum);
		ep0F16 = Vector3.Clamp(ep0F16, Vector3.Zero, new Vector3(Half.MaxValue.value));
		ep1F16 = Vector3.Clamp(ep1F16, Vector3.Zero, new Vector3(Half.MaxValue.value));
		ep0 = new ColorRgbFloat(F16ToF32(ep0F16));
		ep1 = new ColorRgbFloat(F16ToF32(ep1F16));
	}
}
