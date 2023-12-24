using System;
using System.Diagnostics;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder.Bptc;

internal static class Bc6ModeEncoder
{

	public static Bc6Block EncodeBlock1Sub(Bc6BlockType type, RawBlock4X4RgbFloat block, ColorRgbFloat initialEndpoint0,
		ColorRgbFloat initialEndpoint1, bool signed, out bool badTransform)
	{
		var endpointBits = type.EndpointBits();
		var deltaBits = type.DeltaBits();
		var hasTransformedEndpoints = type.HasTransformedEndpoints();

		var initialPreQuantizedEp0 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint0, signed);
		var initialPreQuantizedEp1 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint1, signed);

		var initialQuantizedEp0 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp0, endpointBits, signed);
		var initialQuantizedEp1 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp1, endpointBits, signed);

		if (hasTransformedEndpoints)
		{
			// check for delta overflow before index search
			var bTransform = false;
			Bc6EncodingHelpers.CreateTransformedEndpoint(initialQuantizedEp0, initialQuantizedEp1, deltaBits, ref bTransform);
			if (bTransform)
			{
				badTransform = true;
				return default;
			}
		}

		var unquantizedEndpoint0 = Bc6Block.UnQuantize(initialQuantizedEp0, endpointBits, signed);
		var unquantizedEndpoint1 = Bc6Block.UnQuantize(initialQuantizedEp1, endpointBits, signed);

		Span<byte> indices = stackalloc byte[16];

		Bc6EncodingHelpers.FindOptimalIndicesInt1Sub(block, unquantizedEndpoint0, unquantizedEndpoint1, indices, signed);


		Bc6EncodingHelpers.SwapIndicesIfNecessary1Sub(block, ref unquantizedEndpoint0, ref unquantizedEndpoint1, indices, signed);

		var quantEp0 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint0, endpointBits, signed);
		var quantEp1 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint1, endpointBits, signed);

		badTransform = false;

		if (hasTransformedEndpoints)
		{
			quantEp1 = Bc6EncodingHelpers.CreateTransformedEndpoint(quantEp0, quantEp1, deltaBits,
				ref badTransform);
		}

		return type switch
		{
			Bc6BlockType.Type3 => Bc6Block.PackType3(quantEp0, quantEp1, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type7 => Bc6Block.PackType7(quantEp0, quantEp1, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type11 => Bc6Block.PackType11(quantEp0, quantEp1, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type15 => Bc6Block.PackType15(quantEp0, quantEp1, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type0 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type1 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type2 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type6 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type10 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type14 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type18 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type22 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type26 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type30 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Unknown => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	public static Bc6Block EncodeBlock2Sub(Bc6BlockType type, RawBlock4X4RgbFloat block, ColorRgbFloat initialEndpoint0,
		ColorRgbFloat initialEndpoint1, ColorRgbFloat initialEndpoint2,
		ColorRgbFloat initialEndpoint3, int partitionSetId, bool signed, out bool badTransform)
	{
		Debug.Assert(type.HasSubsets(), "Trying to use 2-subset method for 1-subset block type!");

		var endpointBits = type.EndpointBits();
		var deltaBits = type.DeltaBits();
		var hasTransformedEndpoints = type.HasTransformedEndpoints();

		var initialPreQuantizedEp0 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint0, signed);
		var initialPreQuantizedEp1 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint1, signed);
		var initialPreQuantizedEp2 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint2, signed);
		var initialPreQuantizedEp3 = Bc6EncodingHelpers.PreQuantizeRawEndpoint(initialEndpoint3, signed);

		var initialQuantizedEp0 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp0, endpointBits, signed);
		var initialQuantizedEp1 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp1, endpointBits, signed);
		var initialQuantizedEp2 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp2, endpointBits, signed);
		var initialQuantizedEp3 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(initialPreQuantizedEp3, endpointBits, signed);

		if (hasTransformedEndpoints)
		{
			// check for delta overflow before index search
			var bTransform = false;
			Bc6EncodingHelpers.CreateTransformedEndpoint(initialQuantizedEp0, initialQuantizedEp1, deltaBits, ref bTransform);
			Bc6EncodingHelpers.CreateTransformedEndpoint(initialQuantizedEp0, initialQuantizedEp2, deltaBits, ref bTransform);
			Bc6EncodingHelpers.CreateTransformedEndpoint(initialQuantizedEp0, initialQuantizedEp3, deltaBits, ref bTransform);
			if (bTransform)
			{
				badTransform = true;
				return default;
			}
		}


		var unquantizedEndpoint0 = Bc6Block.UnQuantize(initialQuantizedEp0, endpointBits, signed);
		var unquantizedEndpoint1 = Bc6Block.UnQuantize(initialQuantizedEp1, endpointBits, signed);
		var unquantizedEndpoint2 = Bc6Block.UnQuantize(initialQuantizedEp2, endpointBits, signed);
		var unquantizedEndpoint3 = Bc6Block.UnQuantize(initialQuantizedEp3, endpointBits, signed);


		Span<byte> indices = stackalloc byte[16];

		Bc6EncodingHelpers.FindOptimalIndicesInt2Sub(block, unquantizedEndpoint0, unquantizedEndpoint1, indices,
			partitionSetId, 0, signed);
		Bc6EncodingHelpers.FindOptimalIndicesInt2Sub(block, unquantizedEndpoint2, unquantizedEndpoint3, indices,
			partitionSetId, 1, signed);


		Bc6EncodingHelpers.SwapIndicesIfNecessary2Sub(block, ref unquantizedEndpoint0, ref unquantizedEndpoint1, indices,
			partitionSetId, 0, signed);
		Bc6EncodingHelpers.SwapIndicesIfNecessary2Sub(block, ref unquantizedEndpoint2, ref unquantizedEndpoint3, indices,
			partitionSetId, 1, signed);

		var quantEp0 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint0, endpointBits, signed);
		var quantEp1 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint1, endpointBits, signed);
		var quantEp2 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint2, endpointBits, signed);
		var quantEp3 =
			Bc6EncodingHelpers.FinishQuantizeEndpoint(unquantizedEndpoint3, endpointBits, signed);

		badTransform = false;

		if (!hasTransformedEndpoints)
			return type switch
			{
				Bc6BlockType.Type0 => Bc6Block.PackType0(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type1 => Bc6Block.PackType1(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type2 => Bc6Block.PackType2(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type6 => Bc6Block.PackType6(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type10 => Bc6Block.PackType10(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type14 => Bc6Block.PackType14(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type18 => Bc6Block.PackType18(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type22 => Bc6Block.PackType22(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type26 => Bc6Block.PackType26(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type30 => Bc6Block.PackType30(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId,
					indices, new ArgumentOutOfRangeException()),
				Bc6BlockType.Type3 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
				Bc6BlockType.Type7 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
				Bc6BlockType.Type11 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
				Bc6BlockType.Type15 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
				Bc6BlockType.Unknown => throw new ArgumentOutOfRangeException(nameof(type), type, null),
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		quantEp1 = Bc6EncodingHelpers.CreateTransformedEndpoint(quantEp0, quantEp1, deltaBits, ref badTransform);
		quantEp2 = Bc6EncodingHelpers.CreateTransformedEndpoint(quantEp0, quantEp2, deltaBits, ref badTransform);
		quantEp3 = Bc6EncodingHelpers.CreateTransformedEndpoint(quantEp0, quantEp3, deltaBits, ref badTransform);

		return type switch
		{
			Bc6BlockType.Type0 => Bc6Block.PackType0(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type1 => Bc6Block.PackType1(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type2 => Bc6Block.PackType2(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type6 => Bc6Block.PackType6(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type10 => Bc6Block.PackType10(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type14 => Bc6Block.PackType14(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type18 => Bc6Block.PackType18(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type22 => Bc6Block.PackType22(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type26 => Bc6Block.PackType26(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type30 => Bc6Block.PackType30(quantEp0, quantEp1, quantEp2, quantEp3, partitionSetId, indices, new ArgumentOutOfRangeException()),
			Bc6BlockType.Type3 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type7 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type11 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Type15 => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			Bc6BlockType.Unknown => throw new ArgumentOutOfRangeException(nameof(type), type, null),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}
