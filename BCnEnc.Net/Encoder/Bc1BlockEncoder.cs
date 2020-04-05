﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Accord.Math;
using BCnComp.Net.Shared;
using SixLabors.ImageSharp.PixelFormats;

namespace BCnComp.Net.Encoder
{



	internal class Bc1BlockEncoder : IBcBlockEncoder
	{


		public byte[] Encode(RawBlock4X4Rgba32[,] blocks, int blockWidth, int blockHeight, EncodingQuality quality, bool parallel)
		{
			byte[] outputData = new byte[blockWidth * blockHeight * Marshal.SizeOf<Bc1Block>()];
			Memory<RawBlock4X4Rgba32> inputBlocks = new Memory<RawBlock4X4Rgba32>(blocks.Reshape(MatrixOrder.FortranColumnMajor));
			Span<Bc1Block> outputBlocks = MemoryMarshal.Cast<byte, Bc1Block>(outputData);

			if (parallel)
			{
				Parallel.For(0, inputBlocks.Length, i =>
				{
					Span<Bc1Block> outputBlocks = MemoryMarshal.Cast<byte, Bc1Block>(outputData);
					outputBlocks[i] = EncodeBlock(inputBlocks.Span[i], quality);
				});
			}
			else
			{
				for (int i = 0; i < inputBlocks.Length; i++)
				{
					outputBlocks[i] = EncodeBlock(inputBlocks.Span[i], quality);
				}
			}

			return outputData;
		}

		private Bc1Block EncodeBlock(RawBlock4X4Rgba32 block, EncodingQuality quality)
		{
			switch (quality)
			{
				case EncodingQuality.Fast:
					return Bc1BlockEncoderFast.EncodeBlock(block);
				case EncodingQuality.Balanced:
					return Bc1BlockEncoderYcbcrBalanced.EncodeBlock(block);
				case EncodingQuality.BestQuality:
					return Bc1BlockEncoderYcbcrSlowBest.EncodeBlock(block);

				default:
					throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
			}
		}

		public GlInternalFormat GetInternalFormat()
		{
			return GlInternalFormat.GL_COMPRESSED_RGB_S3TC_DXT1_EXT;
		}

		public GLFormat GetBaseInternalFormat()
		{
			return GLFormat.GL_RGB;
		}


		private static class Bc1BlockEncoderFast
		{

			private const int blackThreshold = 10;

			private static int ChooseClosestColor(Span<ColorRgb24> colors, Rgba32 color)
			{
				int closest = 0;
				int closestError =
					Math.Abs(colors[0].r - color.R)
					+ Math.Abs(colors[0].g - color.G)
					+ Math.Abs(colors[0].b - color.B);

				for (int i = 1; i < colors.Length; i++)
				{
					int error =
						Math.Abs(colors[i].r - color.R)
						+ Math.Abs(colors[i].g - color.G)
						+ Math.Abs(colors[i].b - color.B);
					if (error < closestError)
					{
						closest = i;
						closestError = error;
					}
				}
				return closest;
			}

			internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
				Bc1Block output = new Bc1Block();

				var pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out var mean, out var principalAxis);
				PcaVectors.GetExtremePoints(pixels, mean, principalAxis, out var min, out var max);

				ColorRgb565 c0 = new ColorRgb565(min);
				ColorRgb565 c1 = new ColorRgb565(max);

				if (c0.data <= c1.data)
				{
					var c = c0;
					c0 = c1;
					c1 = c;
				}
				output.color0 = c0;
				output.color1 = c1;

				ColorRgb24 color0 = new ColorRgb24(c0);
				ColorRgb24 color1 = new ColorRgb24(c1);

				Span<ColorRgb24> colors = output.HasAlphaOrBlack ?
					stackalloc ColorRgb24[] {
					color0,
					color1,
					color0 * (1.0 / 2.0) + color1 * (1.0 / 2.0),
					new ColorRgb24(0, 0, 0)
				} : stackalloc ColorRgb24[] {
					color0,
					color1,
					color0 * (2.0 / 3.0) + color1 * (1.0 / 3.0),
					color0 * (1.0 / 3.0) + color1 * (2.0 / 3.0)
				};

				for (int i = 0; i < 16; i++)
				{
					var color = pixels[i];
					output[i] = ChooseClosestColor(colors, color);
				}

				return output;
			}
		}
		private static class Bc1BlockEncoderYcbcrSlowBest
		{
			private const int variations = 2;

			private static int ChooseClosestColor(Span<YCbCr> colors, YCbCr color)
			{
				int closest = 0;
				float closestError = 0;
				bool first = true;

				for (int i = 0; i < colors.Length; i++)
				{
					float error = MathF.Abs(colors[i].y - color.y) * 4
								  + MathF.Abs(colors[i].cb - color.cb)
								  + MathF.Abs(colors[i].cr - color.cr);
					if (first)
					{
						closestError = error;
						first = false;
					}
					else if (error < closestError)
					{
						closest = i;
						closestError = error;
					}
				}
				return closest;
			}

			private static Bc1Block TryColors(RawBlock4X4Ycbcr rawBlock, ColorRgb565 color0, ColorRgb565 color1)
			{
				Bc1Block output = new Bc1Block();

				var pixels = rawBlock.AsSpan;

				output.color0 = color0;
				output.color1 = color1;

				var c0 = color0.ToColorRgb24();
				var c1 = color1.ToColorRgb24();

				Span<YCbCr> colors = output.HasAlphaOrBlack ?
					stackalloc YCbCr[] {
					new YCbCr(c0),
					new YCbCr(c1),
					new YCbCr(c0 * (1.0 / 2.0) + c1 * (1.0 / 2.0)),
					new YCbCr(0, 0, 0)
				} : stackalloc YCbCr[] {
					new YCbCr(c0),
					new YCbCr(c1),
					new YCbCr(c0 * (2.0 / 3.0) + c1 * (1.0 / 3.0)),
					new YCbCr(c0 * (1.0 / 3.0) + c1 * (2.0 / 3.0))
				};

				for (int i = 0; i < 16; i++)
				{
					var color = pixels[i];
					output[i] = ChooseClosestColor(colors, color);
				}

				return output;
			}

			private static void GenerateVariations(YCbCr min, YCbCr max, List<ColorRgb565> colors)
			{

				for (int i = 0; i < variations; i++)
				{
					max.y -= 0.05f;
					min.y += 0.05f;

					var ma = max.ToColorRgb565();
					var mi = min.ToColorRgb565();
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					if (!colors.Contains(mi))
					{
						colors.Add(mi);
					}

					//variate reds in max
					ma.RawR += 1;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}
					ma.RawR -= 2;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					//variate blues in max
					ma.RawR += 1;
					ma.RawB += 1;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}
					ma.RawB -= 2;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					//variate reds in min
					mi.RawR += 1;
					if (!colors.Contains(mi))
					{
						colors.Add(mi);
					}
					ma.RawR -= 2;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					//variate blues in min
					mi.RawR += 1;
					mi.RawB += 1;
					if (!colors.Contains(mi))
					{
						colors.Add(mi);
					}
					mi.RawB -= 2;
					if (!colors.Contains(mi))
					{
						colors.Add(mi);
					}

				}
			}

			internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
				var rawBlockYcbcr = rawBlock.ToRawBlockYcbcr();
				var pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out var mean, out var principalAxis);
				PcaVectors.GetExtremePoints(pixels, mean, principalAxis, out var min, out var max);

				var minYcbcr = new YCbCr(min);
				var maxYcbcr = new YCbCr(max);
				List<ColorRgb565> uniqueColors = new List<ColorRgb565>();
				uniqueColors.Add(minYcbcr.ToColorRgb565());
				uniqueColors.Add(maxYcbcr.ToColorRgb565());
				GenerateVariations(minYcbcr, maxYcbcr, uniqueColors);


				Bc1Block best = new Bc1Block();
				float bestError = 0;
				bool first = true;
				for (int i = 0; i < uniqueColors.Count; i++)
				{
					for (int j = 0; j < uniqueColors.Count; j++)
					{
						var color0 = uniqueColors[i];
						var color1 = uniqueColors[j];
						var encoded = TryColors(rawBlockYcbcr, color0, color1);
						var decoded = encoded.Decode(false);
						var error = rawBlockYcbcr.CalculateError(decoded);

						if (first)
						{
							best = encoded;
							bestError = error;
							first = false;
						}
						else if (error < bestError)
						{
							best = encoded;
							bestError = error;
						}
					}
				}

				return best;
			}
		}
		private static class Bc1BlockEncoderYcbcrBalanced
		{
			private const int variations = 2;

			private static int ChooseClosestColor(Span<YCbCr> colors, YCbCr color)
			{
				int closest = 0;
				float closestError = 0;
				bool first = true;

				for (int i = 0; i < colors.Length; i++)
				{
					float error = MathF.Abs(colors[i].y - color.y) * 4
								  + MathF.Abs(colors[i].cb - color.cb)
								  + MathF.Abs(colors[i].cr - color.cr);
					if (first)
					{
						closestError = error;
						first = false;
					}
					else if (error < closestError)
					{
						closest = i;
						closestError = error;
					}
				}
				return closest;
			}

			private static Bc1Block TryColors(RawBlock4X4Ycbcr rawBlock, ColorRgb565 color0, ColorRgb565 color1)
			{
				Bc1Block output = new Bc1Block();

				var pixels = rawBlock.AsSpan;

				output.color0 = color0;
				output.color1 = color1;

				var c0 = color0.ToColorRgb24();
				var c1 = color1.ToColorRgb24();

				Span<YCbCr> colors = output.HasAlphaOrBlack ?
					stackalloc YCbCr[] {
					new YCbCr(c0),
					new YCbCr(c1),
					new YCbCr(c0 * (1.0 / 2.0) + c1 * (1.0 / 2.0)),
					new YCbCr(0, 0, 0)
				} : stackalloc YCbCr[] {
					new YCbCr(c0),
					new YCbCr(c1),
					new YCbCr(c0 * (2.0 / 3.0) + c1 * (1.0 / 3.0)),
					new YCbCr(c0 * (1.0 / 3.0) + c1 * (2.0 / 3.0))
				};

				for (int i = 0; i < 16; i++)
				{
					var color = pixels[i];
					output[i] = ChooseClosestColor(colors, color);
				}

				return output;
			}

			private static void GenerateVariations(YCbCr min, YCbCr max, List<ColorRgb565> colors)
			{

				for (int i = 0; i < variations; i++)
				{
					max.y -= 0.05f;
					min.y += 0.05f;

					var ma = max.ToColorRgb565();
					var mi = min.ToColorRgb565();
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					if (!colors.Contains(mi))
					{
						colors.Add(mi);
					}

					//variate reds in max
					ma.RawR += 1;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}
					ma.RawR -= 2;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

					//variate blues in max
					ma.RawR += 1;
					ma.RawB += 1;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}
					ma.RawB -= 2;
					if (!colors.Contains(ma))
					{
						colors.Add(ma);
					}

				}
			}

			internal static Bc1Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
				var rawBlockYcbcr = rawBlock.ToRawBlockYcbcr();
				var pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out var mean, out var principalAxis);
				PcaVectors.GetExtremePoints(pixels, mean, principalAxis, out var min, out var max);

				var minYcbcr = new YCbCr(min);
				var maxYcbcr = new YCbCr(max);
				List<ColorRgb565> uniqueColors = new List<ColorRgb565>();
				uniqueColors.Add(minYcbcr.ToColorRgb565());
				uniqueColors.Add(maxYcbcr.ToColorRgb565());
				GenerateVariations(minYcbcr, maxYcbcr, uniqueColors);


				Bc1Block best = new Bc1Block();
				float bestError = 0;
				bool first = true;
				for (int i = 0; i < uniqueColors.Count; i++)
				{
					for (int j = 0; j < uniqueColors.Count; j++)
					{
						var color0 = uniqueColors[i];
						var color1 = uniqueColors[j];
						var encoded = TryColors(rawBlockYcbcr, color0, color1);
						var decoded = encoded.Decode(false);
						var error = rawBlockYcbcr.CalculateError(decoded);

						if (first)
						{
							best = encoded;
							bestError = error;
							first = false;
						}
						else if (error < bestError)
						{
							best = encoded;
							bestError = error;
						}
					}
				}

				return best;
			}
		}
	}
}