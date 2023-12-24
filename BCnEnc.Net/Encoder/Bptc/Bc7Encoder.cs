using System;
using System.Collections.Generic;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

namespace BCnEncoder.Encoder.Bptc;

internal class Bc7Encoder : BaseBcBlockEncoder<Bc7Block, RawBlock4X4Rgba32>
{

	public override Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock, CompressionQuality quality)
	{
		return quality switch
		{
			CompressionQuality.Fast => Bc7EncoderFast.EncodeBlock(rawBlock),
			CompressionQuality.Balanced => Bc7EncoderBalanced.EncodeBlock(rawBlock),
			CompressionQuality.BestQuality => Bc7EncoderBestQuality.EncodeBlock(rawBlock),
			_ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
		};
	}

	public override GlInternalFormat GetInternalFormat()
	{
		return GlInternalFormat.GlCompressedRgbaBptcUnormArb;
	}

	public override GlFormat GetBaseInternalFormat()
	{
		return GlFormat.GlRgba;
	}

	public override DxgiFormat GetDxgiFormat() {
		return DxgiFormat.DxgiFormatBc7Unorm;
	}

	private static ClusterIndices4X4 CreateClusterIndexBlock(RawBlock4X4Rgba32 raw, out int outputNumClusters, 
		int numClusters = 3)
	{

		var indexBlock = new ClusterIndices4X4();

		var indices = LinearClustering.ClusterPixels(raw.AsSpan, 4, 4,
			numClusters, 1, 10, false);

		var output = indexBlock.AsSpan;
		for (var i = 0; i < output.Length; i++)
		{
			output[i] = indices[i];
		}

		var nClusters = indexBlock.NumClusters;
		if (nClusters < numClusters)
		{
			indexBlock = indexBlock.Reduce(out nClusters);
		}

		outputNumClusters = nClusters;
		return indexBlock;
	}

	private static class Bc7EncoderFast
	{
		private const float ErrorThreshold = 0.005f;
		private const int MaxTries = 5;

		private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock,
			IReadOnlyList<int> best2SubsetPartitions, IReadOnlyList<int> best3SubsetPartitions, bool alpha)
		{
			if (alpha)
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 5, new ArgumentException());
				yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 3, new ArgumentException());
			}
			else
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6, new ArgumentException());
				for (var i = 0; i < 64; i++) {
					if(best3SubsetPartitions[i] < 16) {
						yield return Bc7Mode0Encoder.EncodeBlock(rawBlock, 3, best3SubsetPartitions[i]);
					}
						
					yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i], new ArgumentException());
						
				}
			}
		}

		public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var hasAlpha = rawBlock.HasTransparentPixels();

			var indexBlock2 = CreateClusterIndexBlock(rawBlock, out var clusters2, 2);
			var indexBlock3 = CreateClusterIndexBlock(rawBlock, out var clusters3, 3);

			if (clusters2 < 2) {
				clusters2 = clusters3;
				indexBlock2 = indexBlock3;
			}

			var best2SubsetPartitions = BptcEncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
			var best3SubsetPartitions = BptcEncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);

			float bestError = 99999;
			var best = new Bc7Block();
			var tries = 0;
			foreach (var block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha)) {
				var decoded = block.Decode();
				var error = rawBlock.CalculateYCbCrAlphaError(decoded);
				tries++;

				if(error < bestError) {
					best = block;
					bestError = error;
				}

				if (error < ErrorThreshold || tries > MaxTries) {
					break;
				}

			}

			return best;
		}
	}

	private static class Bc7EncoderBalanced
	{
		private const float ErrorThreshold = 0.005f;
		private const int MaxTries = 25;

		private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock,
			IReadOnlyList<int> best2SubsetPartitions, IReadOnlyList<int> best3SubsetPartitions, bool alpha)
		{
			if (alpha)
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6, new ArgumentException());
				yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 4, new ArgumentException());
				yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 4, new ArgumentException());
				for (var i = 0; i < 64; i++)
				{
					yield return Bc7Mode7Encoder.EncodeBlock(rawBlock, 3, best2SubsetPartitions[i], new ArgumentException());
				}
			}
			else
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6, new ArgumentException());
				yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 4, new ArgumentException());
				yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 4, new ArgumentException());
				for (var i = 0; i < 64; i++) {
					if(best3SubsetPartitions[i] < 16) {
						yield return Bc7Mode0Encoder.EncodeBlock(rawBlock, 3, best3SubsetPartitions[i]);
					}
					else {
						yield return Bc7Mode2Encoder.EncodeBlock(rawBlock, 5, best3SubsetPartitions[i], new ArgumentException());
					}

					yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i], new ArgumentException());
				}
			}
		}

		public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var hasAlpha = rawBlock.HasTransparentPixels();

			var indexBlock2 = CreateClusterIndexBlock(rawBlock, out var clusters2, 2);
			var indexBlock3 = CreateClusterIndexBlock(rawBlock, out var clusters3, 3);

			if (clusters2 < 2) {
				clusters2 = clusters3;
				indexBlock2 = indexBlock3;
			}

			var best2SubsetPartitions = BptcEncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
			var best3SubsetPartitions = BptcEncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);

			float bestError = 99999;
			var best = new Bc7Block();
			var tries = 0;
			foreach (var block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha)) {
				var decoded = block.Decode();
				var error = rawBlock.CalculateYCbCrAlphaError(decoded);
				tries++;

				if(error < bestError) {
					best = block;
					bestError = error;
				}

				if (error < ErrorThreshold || tries > MaxTries) {
					break;
				}

			}

			return best;
		}
	}

	private static class Bc7EncoderBestQuality
	{

		private const float ErrorThreshold = 0.001f;
		private const int MaxTries = 40;

		private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock,
			IReadOnlyList<int> best2SubsetPartitions, IReadOnlyList<int> best3SubsetPartitions, bool alpha)
		{
			if (alpha)
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 8, new ArgumentException());
				yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 5, new ArgumentException());
				yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 5, new ArgumentException());
				for (var i = 0; i < 64; i++)
				{
					yield return Bc7Mode7Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i], new ArgumentException());

				}
			}
			else
			{
				yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 8, new ArgumentException());
				yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 5, new ArgumentException());
				yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 5, new ArgumentException());
				for (var i = 0; i < 64; i++) {
					if(best3SubsetPartitions[i] < 16) {
						yield return Bc7Mode0Encoder.EncodeBlock(rawBlock, 4, best3SubsetPartitions[i]);
					}
					yield return Bc7Mode2Encoder.EncodeBlock(rawBlock, 5, best3SubsetPartitions[i], new ArgumentException());

					yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i], new ArgumentException());
					yield return Bc7Mode3Encoder.EncodeBlock(rawBlock, 5, best2SubsetPartitions[i], new ArgumentException());

				}
			}
		}

		public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
		{
			var hasAlpha = rawBlock.HasTransparentPixels();

			var indexBlock2 = CreateClusterIndexBlock(rawBlock, out var clusters2, 2);
			var indexBlock3 = CreateClusterIndexBlock(rawBlock, out var clusters3, 3);

			if (clusters2 < 2) {
				clusters2 = clusters3;
				indexBlock2 = indexBlock3;
			}

			var best2SubsetPartitions = BptcEncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
			var best3SubsetPartitions = BptcEncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);


			float bestError = 99999;
			var best = new Bc7Block();
			var tries = 0;
			foreach (var block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha)) {
				var decoded = block.Decode();
				var error = rawBlock.CalculateYCbCrAlphaError(decoded);
				tries++;

				if(error < bestError) {
					best = block;
					bestError = error;
				}

				if (error < ErrorThreshold || tries > MaxTries) {
					break;
				}

			}

			return best;
		}
	}
}
