using System;
using System.Linq;
using System.Runtime.InteropServices;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder.Bptc;

internal static class BptcEncodingHelpers
{
	private static readonly byte[] colorInterpolationWeights2 = [ 0, 21, 43, 64 ];
	private static readonly byte[] colorInterpolationWeights3 = [0, 9, 18, 27, 37, 46, 55, 64];
	private static readonly byte[] colorInterpolationWeights4 = [0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64
	];


	public static int InterpolateInt(int e0, int e1, int index, int indexPrecision)
	{
		if (indexPrecision == 0) return e0;
		var aWeights2 = colorInterpolationWeights2;
		var aWeights3 = colorInterpolationWeights3;
		var aWeights4 = colorInterpolationWeights4;

		return indexPrecision switch
		{
			2 => (((64 - aWeights2[index]) * e0 + aWeights2[index] * e1 + 32) >> 6),
			3 => ((64 - aWeights3[index]) * e0 + aWeights3[index] * e1 + 32) >> 6,
			_ => ((64 - aWeights4[index]) * e0 + aWeights4[index] * e1 + 32) >> 6
		};
	}

	public static byte InterpolateByte(byte e0, byte e1, int index, int indexPrecision)
	{
		if (indexPrecision == 0) return e0;
		var aWeights2 = colorInterpolationWeights2;
		var aWeights3 = colorInterpolationWeights3;
		var aWeights4 = colorInterpolationWeights4;

		return indexPrecision switch
		{
			2 => (byte)(((64 - aWeights2[index]) * e0 + aWeights2[index] * e1 + 32) >> 6),
			3 => (byte)(((64 - aWeights3[index]) * e0 + aWeights3[index] * e1 + 32) >> 6),
			_ => (byte)(((64 - aWeights4[index]) * e0 + aWeights4[index] * e1 + 32) >> 6)
		};
	}



	public static int[] Rank2SubsetPartitions(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters, bool smallIndex = false)
	{
		var output = Enumerable.Range(0, smallIndex ? 32 : 64).ToArray();


		output = [.. output.OrderBy(CalculatePartitionError)];

		return output;

		int CalculatePartitionError(int partitionIndex)
		{ 
			var error = 0;
			ReadOnlySpan<int> partitionTable = Bc7Block.Subsets2PartitionTable[partitionIndex];
			Span<int> subset0 = stackalloc int[numDistinctClusters];
			Span<int> subset1 = stackalloc int[numDistinctClusters];
			var max0Idx = 0;
			var max1Idx = 0;

			//Calculate largest cluster index for each subset 
			for (var i = 0; i < 16; i++)
			{
				if (partitionTable[i] == 0)
				{
					var r = reducedIndicesBlock[i];
					subset0[r]++;
					var count = subset0[r];
					if (count > subset0[max0Idx])
					{
						max0Idx = r;
					}
				}
				else
				{
					var r = reducedIndicesBlock[i];
					subset1[r]++;
					var count = subset1[r];
					if (count > subset1[max1Idx])
					{
						max1Idx = r;
					}
				}
			}

			// Calculate error by counting as error everything that does not match the largest cluster
			for (var i = 0; i < 16; i++)
			{
				if (partitionTable[i] == 0)
				{
					if (reducedIndicesBlock[i] != max0Idx) error++;
				}
				else
				{
					if (reducedIndicesBlock[i] != max1Idx) error++;
				}
			}

			return error;
		}
	}

	public static int[] Rank3SubsetPartitions(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters)
	{
		var output = Enumerable.Range(0, 64).ToArray();

		output = [.. output.OrderBy(CalculatePartitionError)];

		return output;

		int CalculatePartitionError(int partitionIndex)
		{
			var error = 0;
			ReadOnlySpan<int> partitionTable = Bc7Block.Subsets3PartitionTable[partitionIndex];

			Span<int> subset0 = stackalloc int[numDistinctClusters];
			Span<int> subset1 = stackalloc int[numDistinctClusters];
			Span<int> subset2 = stackalloc int[numDistinctClusters];
			var max0Idx = 0;
			var max1Idx = 0;
			var max2Idx = 0;

			//Calculate largest cluster index for each subset 
			for (var i = 0; i < 16; i++)
			{
				switch (partitionTable[i])
				{
					case 0:
					{
						var r = reducedIndicesBlock[i];
						subset0[r]++;
						var count = subset0[r];
						if (count > subset0[max0Idx])
						{
							max0Idx = r;
						}

						break;
					}
					case 1:
					{
						var r = reducedIndicesBlock[i];
						subset1[r]++;
						var count = subset1[r];
						if (count > subset1[max1Idx])
						{
							max1Idx = r;
						}

						break;
					}
					default:
					{
						var r = reducedIndicesBlock[i];
						subset2[r]++;
						var count = subset2[r];
						if (count > subset2[max2Idx])
						{
							max2Idx = r;
						}

						break;
					}
				}
			}

			// Calculate error by counting as error everything that does not match the largest cluster
			for (var i = 0; i < 16; i++)
			{
				switch (partitionTable[i])
				{
					case 0:
					{
						if (reducedIndicesBlock[i] != max0Idx) error++;
						break;
					}
					case 1:
					{
						if (reducedIndicesBlock[i] != max1Idx) error++;
						break;
					}
					default:
					{
						if (reducedIndicesBlock[i] != max2Idx) error++;
						break;
					}
				}
			}

			return error;
		}
	}
}

internal struct ClusterIndices4X4
{
	public int i00, i10, i20, i30;
	public int i01, i11, i21, i31;
	public int i02, i12, i22, i32;
	public int i03, i13, i23, i33;

	public Span<int> AsSpan => MemoryMarshal.CreateSpan(ref i00, 16);

	public int this[int x, int y]
	{
		get => AsSpan[x + y * 4];
		set => AsSpan[x + y * 4] = value;
	}

	public int this[int index]
	{
		get => AsSpan[index];
		set => AsSpan[index] = value;
	}

	public int NumClusters
	{
		get
		{
			var t = AsSpan;
			Span<int> clusters = stackalloc int[16];
			var distinct = 0;
			for (var i = 0; i < 16; i++)
			{
				var cluster = t[i];
				var found = false;
				for (var j = 0; j < distinct; j++)
				{
					if (clusters[j] != cluster) continue;
					found = true;
					break;
				}

				if (found) continue;
				clusters[distinct] = cluster;
				++distinct;
			}
			return distinct;
		}
	}

	/// <summary>
	/// Reduces block down to adjacent cluster indices. For example,
	/// block that contains clusters 5, 16 and 77 will become a block that contains clusters 0, 1 and 2
	/// </summary>
	public ClusterIndices4X4 Reduce(out int numClusters)
	{
		var result = new ClusterIndices4X4();
		numClusters = NumClusters;
		Span<int> mapKey = stackalloc int[numClusters];
		var indices = AsSpan;
		var outIndices = result.AsSpan;
		var next = 0;
		for (var i = 0; i < 16; i++)
		{
			var cluster = indices[i];
			var found = false;
			for (var j = 0; j < next; j++)
			{
				if (mapKey[j] != cluster) continue;
				found = true;
				outIndices[i] = j;
				break;
			}

			if (found) continue;
			outIndices[i] = next;
			mapKey[next] = cluster;
			++next;
		}

		return result;
	}
}


internal struct IndexBlock4X4
{
	public byte i00, i10, i20, i30;
	public byte i01, i11, i21, i31;
	public byte i02, i12, i22, i32;
	public byte i03, i13, i23, i33;

	public Span<byte> AsSpan => MemoryMarshal.CreateSpan(ref i00, 16);

	public byte this[int x, int y]
	{
		get => AsSpan[x + y * 4];
		set => AsSpan[x + y * 4] = value;
	}

	public byte this[int index]
	{
		get => AsSpan[index];
		set => AsSpan[index] = value;
	}
}
