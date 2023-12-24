using System;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

namespace BCnEncoder.Encoder;

internal class Bc1BlockEncoder : BaseBcBlockEncoder<Bc1Block, RawBlock4X4Rgba32>
{
	public override Bc1Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
	{
		return quality switch
		{
			CompressionQuality.Fast => Bc1BlockEncoderFast.EncodeBlock(block),
			CompressionQuality.Balanced => Bc1BlockEncoderBalanced.EncodeBlock(block),
			CompressionQuality.BestQuality => Bc1BlockEncoderSlow.EncodeBlock(block),
			_ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
		};
	}

	public override GlInternalFormat GetInternalFormat()
	{
		return GlInternalFormat.GlCompressedRgbS3TcDxt1Ext;
	}

	public override GlFormat GetBaseInternalFormat()
	{
		return GlFormat.GlRgb;
	}

	public override DxgiFormat GetDxgiFormat()
	{
		return DxgiFormat.DxgiFormatBc1Unorm;
	}

	#region Encoding private stuff

	private static Bc1Block TryColors(RawBlock4X4Rgba32 rawBlock, ColorRgb565 color0, ColorRgb565 color1,
		out float error, float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
	{
		var output = new Bc1Block();

		var pixels = rawBlock.AsSpan;

		output.color0 = color0;
		output.color1 = color1;

		var c0 = color0.ToColorRgb24();
		var c1 = color1.ToColorRgb24();

		ReadOnlySpan<ColorRgb24> colors = output.HasAlphaOrBlack ?
		[
			c0,
				c1,
				c0.InterpolateHalf(c1),
				new ColorRgb24(0, 0, 0)
		]
		: stackalloc ColorRgb24[] {
				c0,
				c1,
				c0.InterpolateThird(c1, 1),
				c0.InterpolateThird(c1, 2)
			};

		error = 0;
		for (var i = 0; i < 16; i++)
		{
			var color = pixels[i];
			output[i] = ColorChooser.ChooseClosestColor4(colors, color, rWeight, gWeight, bWeight, out var e);
			error += e;
		}

		return output;
	}


	#endregion

	#region Encoders

	private static class Bc1BlockEncoderFast
	{

		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var output = new Bc1Block();

			var pixels = rawBlock.AsSpan;

			RgbBoundingBox.Create565(pixels, out var min, out var max);

			output = TryColors(rawBlock, max, min, out var error);

			return output;
		}
	}

	private static class Bc1BlockEncoderBalanced
	{
		private const int MaxTries = 24 * 2;
		private const float ErrorThreshold = 0.05f;

		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var pixels = rawBlock.AsSpan;

			PcaVectors.Create(pixels, out var mean, out var pa);
			PcaVectors.GetMinMaxColor565(pixels, mean, pa, out var min, out var max);

			var c0 = max;
			var c1 = min;

			if (c0.data < c1.data)
			{
				(c0, c1) = (c1, c0);
			}

			var best = TryColors(rawBlock, c0, c1, out var bestError);

			for (var i = 0; i < MaxTries; i++)
			{
				var (newC0, newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

				if (newC0.data < newC1.data)
				{
					(newC0, newC1) = (newC1, newC0);
				}

				var block = TryColors(rawBlock, newC0, newC1, out var error);

				if (error < bestError)
				{
					best = block;
					bestError = error;
					c0 = newC0;
					c1 = newC1;
				}

				if (bestError < ErrorThreshold)
				{
					break;
				}
			}

			return best;
		}
	}

	private static class Bc1BlockEncoderSlow
	{
		private const int MaxTries = 9999;
		private const float ErrorThreshold = 0.01f;

		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var pixels = rawBlock.AsSpan;

			PcaVectors.Create(pixels, out var mean, out var pa);
			PcaVectors.GetMinMaxColor565(pixels, mean, pa, out var min, out var max);

			var c0 = max;
			var c1 = min;

			if (c0.data < c1.data)
			{
				(c0, c1) = (c1, c0);
			}

			var best = TryColors(rawBlock, c0, c1, out var bestError);

			var lastChanged = 0;

			for (var i = 0; i < MaxTries; i++)
			{
				var (newC0, newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

				if (newC0.data < newC1.data)
				{
					(newC0, newC1) = (newC1, newC0);
				}

				var block = TryColors(rawBlock, newC0, newC1, out var error);

				lastChanged++;

				if (error < bestError)
				{
					best = block;
					bestError = error;
					c0 = newC0;
					c1 = newC1;
					lastChanged = 0;
				}

				if (bestError < ErrorThreshold || lastChanged > ColorVariationGenerator.VarPatternCount)
				{
					break;
				}
			}

			return best;
		}
	}

	#endregion
}

internal class Bc1AlphaBlockEncoder : BaseBcBlockEncoder<Bc1Block, RawBlock4X4Rgba32>
{
	public override Bc1Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
	{
		return quality switch
		{
			CompressionQuality.Fast => Bc1AlphaBlockEncoderFast.EncodeBlock(block),
			CompressionQuality.Balanced => Bc1AlphaBlockEncoderBalanced.EncodeBlock(block),
			CompressionQuality.BestQuality => Bc1AlphaBlockEncoderSlow.EncodeBlock(block),
			_ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
		};
	}

	public override GlInternalFormat GetInternalFormat()
	{
		return GlInternalFormat.GlCompressedRgbaS3TcDxt1Ext;
	}

	public override GlFormat GetBaseInternalFormat()
	{
		return GlFormat.GlRgba;
	}

	public override DxgiFormat GetDxgiFormat()
	{
		return DxgiFormat.DxgiFormatBc1Unorm;
	}

	#region Encoding private stuff

	private static Bc1Block TryColors(RawBlock4X4Rgba32 rawBlock, ColorRgb565 color0, ColorRgb565 color1,
		out float error, float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
	{
		var output = new Bc1Block();

		var pixels = rawBlock.AsSpan;

		output.color0 = color0;
		output.color1 = color1;

		var c0 = color0.ToColorRgb24();
		var c1 = color1.ToColorRgb24();

		var hasAlpha = output.HasAlphaOrBlack;

		ReadOnlySpan<ColorRgb24> colors = hasAlpha ?
		[
			c0,
				c1,
				c0.InterpolateHalf(c1),
				new ColorRgb24(0, 0, 0)
		]
		: stackalloc ColorRgb24[] {
				c0,
				c1,
				c0.InterpolateThird(c1, 1),
				c0.InterpolateThird(c1, 2)
			};

		error = 0;
		for (var i = 0; i < 16; i++)
		{
			var color = pixels[i];
			output[i] = ColorChooser.ChooseClosestColor4AlphaCutoff(colors, color, rWeight, gWeight, bWeight,
				128, hasAlpha, out var e);
			error += e;
		}

		return output;
	}

	#endregion

	#region Encoders

	private static class Bc1AlphaBlockEncoderFast
	{

		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var output = new Bc1Block();

			var pixels = rawBlock.AsSpan;

			var hasAlpha = rawBlock.HasTransparentPixels();

			RgbBoundingBox.Create565AlphaCutoff(pixels, out var min, out var max);

			var c0 = max;
			var c1 = min;

			if (hasAlpha && c0.data > c1.data)
			{
				(c0, c1) = (c1, c0);
			}

			output = TryColors(rawBlock, c0, c1, out var error);

			return output;
		}
	}

	private static class Bc1AlphaBlockEncoderBalanced
	{
		private const int MaxTries = 24 * 2;
		private const float ErrorThreshold = 0.05f;


		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var pixels = rawBlock.AsSpan;

			var hasAlpha = rawBlock.HasTransparentPixels();

			PcaVectors.Create(pixels, out var mean, out var pa);
			PcaVectors.GetMinMaxColor565(pixels, mean, pa, out var min, out var max);

			var c0 = max;
			var c1 = min;

			(c0, c1) = hasAlpha switch
			{
				false when c0.data < c1.data => (c1, c0),
				true when c1.data < c0.data => (c1, c0),
				_ => (c0, c1)
			};

			var best = TryColors(rawBlock, c0, c1, out var bestError);

			for (var i = 0; i < MaxTries; i++)
			{
				var (newC0, newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

				(newC0, newC1) = hasAlpha switch
				{
					false when newC0.data < newC1.data => (newC1, newC0),
					true when newC1.data < newC0.data => (newC1, newC0),
					_ => (newC0, newC1)
				};

				var block = TryColors(rawBlock, newC0, newC1, out var error);

				if (error < bestError)
				{
					best = block;
					bestError = error;
					c0 = newC0;
					c1 = newC1;
				}

				if (bestError < ErrorThreshold)
				{
					break;
				}
			}

			return best;
		}
	}

	private static class Bc1AlphaBlockEncoderSlow
	{
		private const int MaxTries = 9999;
		private const float ErrorThreshold = 0.05f;

		internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var pixels = rawBlock.AsSpan;

			var hasAlpha = rawBlock.HasTransparentPixels();

			PcaVectors.Create(pixels, out var mean, out var pa);
			PcaVectors.GetMinMaxColor565(pixels, mean, pa, out var min, out var max);

			var c0 = max;
			var c1 = min;

			(c0, c1) = hasAlpha switch
			{
				false when c0.data < c1.data => (c1, c0),
				true when c1.data < c0.data => (c1, c0),
				_ => (c0, c1)
			};

			var best = TryColors(rawBlock, c0, c1, out var bestError);

			var lastChanged = 0;
			for (var i = 0; i < MaxTries; i++)
			{
				var (newC0, newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

				(newC0, newC1) = hasAlpha switch
				{
					false when newC0.data < newC1.data => (newC1, newC0),
					true when newC1.data < newC0.data => (newC1, newC0),
					_ => (newC0, newC1)
				};

				var block = TryColors(rawBlock, newC0, newC1, out var error);

				lastChanged++;

				if (error < bestError)
				{
					best = block;
					bestError = error;
					c0 = newC0;
					c1 = newC1;
					lastChanged = 0;
				}

				if (bestError < ErrorThreshold || lastChanged > ColorVariationGenerator.VarPatternCount)
				{
					break;
				}
			}

			return best;
		}
	}

	#endregion

}
