using System;
using System.Collections.Generic;
using System.Diagnostics;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

namespace BCnEncoder.Encoder;

internal class Bc4BlockEncoder(ColorComponent component) : BaseBcBlockEncoder<Bc4Block, RawBlock4X4Rgba32>
{
	private readonly Bc4ComponentBlockEncoder bc4Encoder = new(component);

	public override Bc4Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
	{
		var output = new Bc4Block
		{
			componentBlock = bc4Encoder.EncodeBlock(block, quality)
		};

		return output;
	}

	public override GlInternalFormat GetInternalFormat()
	{
		return GlInternalFormat.GlCompressedRedRgtc1Ext;
	}

	public override GlFormat GetBaseInternalFormat()
	{
		return GlFormat.GlRed;
	}

	public override DxgiFormat GetDxgiFormat()
	{
		return DxgiFormat.DxgiFormatBc4Unorm;
	}
}

internal class Bc4ComponentBlockEncoder(ColorComponent component)
{
	public Bc4ComponentBlock EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
	{
		var output = new Bc4ComponentBlock();

		var pixels = block.AsSpan;
		var colors = new byte[pixels.Length];

		for (var i = 0; i < pixels.Length; i++)
			colors[i] = ComponentHelper.ColorToComponent(pixels[i], component);

		return quality switch
		{
			CompressionQuality.Fast => FindComponentValues(output, colors, 3),
			CompressionQuality.Balanced => FindComponentValues(output, colors, 4),
			CompressionQuality.BestQuality => FindComponentValues(output, colors, 8),
			_ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
		};
	}

	#region Encoding private stuff

	private static Bc4ComponentBlock FindComponentValues(Bc4ComponentBlock colorBlock, IReadOnlyList<byte> pixels, int variations)
	{

		//Find min and max alpha
		byte min = 255;
		byte max = 0;
		var hasExtremeValues = false;
		foreach (var t in pixels)
		{
			if (t is < 255 and > 0)
			{
				if (t < min) min = t;
				if (t > max) max = t;
			}
			else
			{
				hasExtremeValues = true;
			}
		}


		//everything is either fully black or fully red
		if (hasExtremeValues && min == 255 && max == 0)
		{
			colorBlock.Endpoint0 = 0;
			colorBlock.Endpoint1 = 255;
			var error = SelectIndices(ref colorBlock);
			Debug.Assert(0 == error);
			return colorBlock;
		}

		var best = colorBlock;
		best.Endpoint0 = max;
		best.Endpoint1 = min;
		var bestError = SelectIndices(ref best);
		if (bestError == 0)
		{
			return best;
		}

		for (var i = (byte)variations; i > 0; i--)
		{
			{
				var c0 = ByteHelper.ClampToByte(max - i);
				var c1 = ByteHelper.ClampToByte(min + i);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}
			{
				var c0 = ByteHelper.ClampToByte(max + i);
				var c1 = ByteHelper.ClampToByte(min - i);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}
			{
				var c0 = ByteHelper.ClampToByte(max);
				var c1 = ByteHelper.ClampToByte(min - i);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}
			{
				var c0 = ByteHelper.ClampToByte(max + i);
				var c1 = ByteHelper.ClampToByte(min);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}
			{
				var c0 = ByteHelper.ClampToByte(max);
				var c1 = ByteHelper.ClampToByte(min + i);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}
			{
				var c0 = ByteHelper.ClampToByte(max - i);
				var c1 = ByteHelper.ClampToByte(min);
				var block = colorBlock;
				block.Endpoint0 = hasExtremeValues ? c1 : c0;
				block.Endpoint1 = hasExtremeValues ? c0 : c1;
				var error = SelectIndices(ref block);
				if (error < bestError)
				{
					best = block;
					bestError = error;
					max = c0;
					min = c1;
				}
			}

			if (bestError < 5)
			{
				break;
			}
		}

		return best;

		int SelectIndices(ref Bc4ComponentBlock block)
		{
			var cumulativeError = 0;
			var c0 = block.Endpoint0;
			var c1 = block.Endpoint1;
			var colors = c0 > c1 ?
			[
				c0,
				c1,
				c0.InterpolateSeventh(c1, 1),
				c0.InterpolateSeventh(c1, 2),
				c0.InterpolateSeventh(c1, 3),
				c0.InterpolateSeventh(c1, 4),
				c0.InterpolateSeventh(c1, 5),
				c0.InterpolateSeventh(c1, 6)
			]
			: stackalloc byte[] {
				c0,
				c1,
				c0.InterpolateFifth(c1, 1),
				c0.InterpolateFifth(c1, 2),
				c0.InterpolateFifth(c1, 3),
				c0.InterpolateFifth(c1, 4),
				0,
				255
			};
			for (var i = 0; i < pixels.Count; i++)
			{
				byte bestIndex = 0;
				var abs = Math.Abs(pixels[i] - colors[0]);
				for (byte j = 1; j < colors.Length; j++)
				{
					var error = Math.Abs(pixels[i] - colors[j]);
					if (error < abs)
					{
						bestIndex = j;
						abs = error;
					}

					if (abs == 0) break;
				}

				block.SetComponentIndex(i, bestIndex);
				cumulativeError += abs * abs;
			}

			return cumulativeError;
		}
	}

	#endregion
}
