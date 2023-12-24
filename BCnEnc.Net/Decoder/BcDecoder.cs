using BCnEncoder.Decoder.Options;
using BCnEncoder.Shared;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BCnEncoder.Shared.ImageFiles;
using CommunityToolkit.HighPerformance;

namespace BCnEncoder.Decoder;

/// <summary>
/// Decodes compressed files into Rgba Format.
/// </summary>
public class BcDecoder
{
	/// <summary>
	/// The input options of the decoder.
	/// </summary>
	public DecoderInputOptions InputOptions { get; } = new();

	/// <summary>
	/// The options for the decoder.
	/// </summary>
	public DecoderOptions Options { get; } = new();

	/// <summary>
	/// The output options of the decoder.
	/// </summary>
	public DecoderOutputOptions OutputOptions { get; } = new();
	#region LDR
	#region Async Api

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// </summary>
	/// <param name="inputStream">The stream containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[]> DecodeRawAsync(Stream inputStream, CompressionFormat format, int pixelWidth, int pixelHeight, CancellationToken token = default)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return Task.Run(() => DecodeRawInternal(dataArray, pixelWidth, pixelHeight, format, token), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[]> DecodeRawAsync(ReadOnlyMemory<byte> input, CompressionFormat format, int pixelWidth, int pixelHeight, CancellationToken token = default)
	{
		return Task.Run(() => DecodeRawInternal(input, pixelWidth, pixelHeight, format, token), token);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[]> DecodeAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, false, token)[0], token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[][]> DecodeAllMipMapsAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, true, token), token);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[]> DecodeAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, false, token)[0], token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgba32[][]> DecodeAllMipMapsAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, true, token), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// </summary>
	/// <param name="inputStream">The stream containing the raw encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>> DecodeRaw2DAsync(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token = default)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return Task.Run(() => DecodeRawInternal(dataArray, pixelWidth, pixelHeight, format, token)
			.AsMemory().AsMemory2D(pixelHeight, pixelWidth), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>> DecodeRaw2DAsync(ReadOnlyMemory<byte> input, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token = default)
	{
		return Task.Run(() => DecodeRawInternal(input, pixelWidth, pixelHeight, format, token)
			.AsMemory().AsMemory2D(pixelHeight, pixelWidth), token);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode the main image from it.
	/// The type of file will be detected automatically.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>> Decode2DAsync(Stream inputStream, CancellationToken token = default)
	{
		return Task.Run(() => DecodeFromStreamInternal2D(inputStream, false, token)[0], token);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode all available mipmaps from it.
	/// The type of file will be detected automatically.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>[]> DecodeAllMipMaps2DAsync(Stream inputStream, CancellationToken token = default)
	{
		return Task.Run(() => DecodeFromStreamInternal2D(inputStream, false, token), token);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>> Decode2DAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, false, token)[0]
			.AsMemory().AsMemory2D((int)file.header.PixelHeight, (int)file.header.PixelWidth), token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>[]> DecodeAllMipMaps2DAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() =>
		{
			var decoded = DecodeInternal(file, true, token);
			var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
			for (var i = 0; i < decoded.Length; i++)
			{
				var mip = file.MipMaps[i];
				mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
			}
			return mem2Ds;
		}, token);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>> Decode2DAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternal(file, false, token)[0]
			.AsMemory().AsMemory2D((int)file.header.dwHeight, (int)file.header.dwWidth), token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgba32>[]> DecodeAllMipMaps2DAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() =>
		{
			var decoded = DecodeInternal(file, true, token);
			var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
			for (var i = 0; i < decoded.Length; i++)
			{
				var mip = file.Faces[0].MipMaps[i];
				mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
			}
			return mem2Ds;
		}, token);
	}

	#endregion

	#region Sync API

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// </summary>
	/// <param name="inputStream">The stream containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgba32[] DecodeRaw(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return DecodeRaw(dataArray, pixelWidth, pixelHeight, format);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// </summary>
	/// <param name="input">The byte array containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgba32[] DecodeRaw(byte[] input, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		return DecodeRawInternal(input, pixelWidth, pixelHeight, format, default);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgba32[] Decode(KtxFile file)
	{
		return DecodeInternal(file, false, default)[0];
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>An array of decoded images.</returns>
	public ColorRgba32[][] DecodeAllMipMaps(KtxFile file)
	{
		return DecodeInternal(file, true, default);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgba32[] Decode(DdsFile file)
	{
		return DecodeInternal(file, false, default)[0];
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public ColorRgba32[][] DecodeAllMipMaps(DdsFile file)
	{
		return DecodeInternal(file, true, default);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// </summary>
	/// <param name="inputStream">The stream containing the encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgba32> DecodeRaw2D(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		var decoded = DecodeRaw(dataArray, pixelWidth, pixelHeight, format);
		return decoded.AsMemory().AsMemory2D(pixelHeight, pixelWidth);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// </summary>
	/// <param name="input">The byte array containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgba32> DecodeRaw2D(byte[] input, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var decoded = DecodeRawInternal(input, pixelWidth, pixelHeight, format, default);
		return decoded.AsMemory().AsMemory2D(pixelHeight, pixelWidth);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode the main image from it.
	/// The type of file will be detected automatically.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgba32> Decode2D(Stream inputStream)
	{
		return DecodeFromStreamInternal2D(inputStream, false, default)[0];
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode all available mipmaps from it.
	/// The type of file will be detected automatically.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgba32>[] DecodeAllMipMaps2D(Stream inputStream)
	{
		return DecodeFromStreamInternal2D(inputStream, true, default);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgba32> Decode2D(KtxFile file)
	{
		return DecodeInternal(file, false, default)[0].AsMemory().AsMemory2D((int)file.header.PixelHeight, (int)file.header.PixelWidth);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgba32>[] DecodeAllMipMaps2D(KtxFile file)
	{
		var decoded = DecodeInternal(file, true, default);
		var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
		for (var i = 0; i < decoded.Length; i++)
		{
			var mip = file.MipMaps[i];
			mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
		}
		return mem2Ds;
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgba32> Decode2D(DdsFile file)
	{
		return DecodeInternal(file, false, default)[0].AsMemory().AsMemory2D((int)file.header.dwHeight, (int)file.header.dwWidth);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgba32>[] DecodeAllMipMaps2D(DdsFile file)
	{
		var decoded = DecodeInternal(file, true, default);
		var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
		for (var i = 0; i < decoded.Length; i++)
		{
			var mip = file.Faces[0].MipMaps[i];
			mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
		}
		return mem2Ds;
	}

	/// <summary>
	/// Decode a single block from raw bytes and return it as a <see cref="Memory2D{T}"/>.
	/// Input Span size needs to equal the block size.
	/// To get the block size (in bytes) of the compression format used, see <see cref="GetBlockSize(BCnEncoder.Shared.CompressionFormat)"/>.
	/// </summary>
	/// <param name="blockData">The encoded block in bytes.</param>
	/// <param name="format">The compression format used.</param>
	/// <returns>The decoded 4x4 block.</returns>
	public Memory2D<ColorRgba32> DecodeBlock(ReadOnlySpan<byte> blockData, CompressionFormat format)
	{
		var output = new ColorRgba32[4, 4];
		DecodeBlockInternal(blockData, format, output);
		return output;
	}

	/// <summary>
	/// Decode a single block from raw bytes and write it to the given output span.
	/// Output span size must be exactly 4x4 and input Span size needs to equal the block size.
	/// To get the block size (in bytes) of the compression format used, see <see cref="GetBlockSize(BCnEncoder.Shared.CompressionFormat)"/>.
	/// </summary>
	/// <param name="blockData">The encoded block in bytes.</param>
	/// <param name="format">The compression format used.</param>
	/// <param name="outputSpan">The destination span of the decoded data.</param>
	public void DecodeBlock(ReadOnlySpan<byte> blockData, CompressionFormat format, Span2D<ColorRgba32> outputSpan)
	{
		if (outputSpan.Width != 4 || outputSpan.Height != 4)
		{
			throw new ArgumentException($"Single block decoding needs an output span of exactly 4x4");
		}
		DecodeBlockInternal(blockData, format, outputSpan);
	}

	/// <summary>
	/// Decode a single block from a stream and write it to the given output span.
	/// Output span size must be exactly 4x4.
	/// </summary>
	/// <param name="inputStream">The stream to read encoded blocks from.</param>
	/// <param name="format">The compression format used.</param>
	/// <param name="outputSpan">The destination span of the decoded data.</param>
	/// <returns>The number of bytes read from the stream. Zero (0) if reached the end of stream.</returns>
	public int DecodeBlock(Stream inputStream, CompressionFormat format, Span2D<ColorRgba32> outputSpan)
	{
		if (outputSpan.Width != 4 || outputSpan.Height != 4)
		{
			throw new ArgumentException($"Single block decoding needs an output span of exactly 4x4");
		}

		Span<byte> input = stackalloc byte[16];
		input = input.Slice(0, GetBlockSize(format));

		var bytesRead = inputStream.Read(input);

		if (bytesRead == 0)
		{
			return 0; //End of stream
		}

		if (bytesRead != input.Length)
		{
			throw new Exception("Input stream does not have enough data available for a full block.");
		}

		DecodeBlockInternal(input, format, outputSpan);
		return bytesRead;
	}

	/// <summary>
	/// Check whether a file is encoded in a supported format.
	/// </summary>
	/// <param name="file">The loaded ktx file to check</param>
	/// <returns>If the format of the file is one of the supported formats.</returns>
	public bool IsSupportedFormat(KtxFile file)
	{
		return GetCompressionFormat(file.header.GlInternalFormat) != CompressionFormat.Unknown;
	}

	/// <summary>
	/// Check whether a file is encoded in a supported format.
	/// </summary>
	/// <param name="file">The loaded dds file to check</param>
	/// <returns>If the format of the file is one of the supported formats.</returns>
	public bool IsSupportedFormat(DdsFile file)
	{
		return GetCompressionFormat(file) != CompressionFormat.Unknown;
	}

	/// <summary>
	/// Gets the format of the file.
	/// </summary>
	/// <param name="file">The loaded ktx file to check</param>
	/// <returns>The <see cref="CompressionFormat"/> of the file.</returns>
	public CompressionFormat GetFormat(KtxFile file)
	{
		return GetCompressionFormat(file.header.GlInternalFormat);
	}

	/// <summary>
	/// Gets the format of the file.
	/// </summary>
	/// <param name="file">The loaded dds file to check</param>
	/// <returns>The <see cref="CompressionFormat"/> of the file.</returns>
	public CompressionFormat GetFormat(DdsFile file)
	{
		return GetCompressionFormat(file);
	}


	#endregion
	#endregion

	#region HDR
	#region Async Api

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[]> DecodeRawHdrAsync(Stream inputStream, CompressionFormat format, int pixelWidth, int pixelHeight, CancellationToken token = default)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return Task.Run(() => DecodeRawInternalHdr(dataArray, pixelWidth, pixelHeight, format, token), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[]> DecodeRawHdrAsync(ReadOnlyMemory<byte> input, CompressionFormat format, int pixelWidth, int pixelHeight, CancellationToken token = default)
	{
		return Task.Run(() => DecodeRawInternalHdr(input, pixelWidth, pixelHeight, format, token), token);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[]> DecodeHdrAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, false, token)[0], token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[][]> DecodeAllMipMapsHdrAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, true, token), token);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[]> DecodeHdrAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, false, token)[0], token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<ColorRgbFloat[][]> DecodeAllMipMapsHdrAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, true, token), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing the raw encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>> DecodeRawHdr2DAsync(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token = default)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return Task.Run(() => DecodeRawInternalHdr(dataArray, pixelWidth, pixelHeight, format, token)
			.AsMemory().AsMemory2D(pixelHeight, pixelWidth), token);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>> DecodeRawHdr2DAsync(ReadOnlyMemory<byte> input, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token = default)
	{
		return Task.Run(() => DecodeRawInternalHdr(input, pixelWidth, pixelHeight, format, token)
			.AsMemory().AsMemory2D(pixelHeight, pixelWidth), token);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode the main image from it.
	/// The type of file will be detected automatically.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>> DecodeHdr2DAsync(Stream inputStream, CancellationToken token = default)
	{
		return Task.Run(() => DecodeFromStreamInternalHdr2D(inputStream, false, token)[0], token);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode all available mipmaps from it.
	/// The type of file will be detected automatically.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>[]> DecodeAllMipMapsHdr2DAsync(Stream inputStream, CancellationToken token = default)
	{
		return Task.Run(() => DecodeFromStreamInternalHdr2D(inputStream, false, token), token);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>> DecodeHdr2DAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, false, token)[0]
			.AsMemory().AsMemory2D((int)file.header.PixelHeight, (int)file.header.PixelWidth), token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>[]> DecodeAllMipMapsHdr2DAsync(KtxFile file, CancellationToken token = default)
	{
		return Task.Run(() =>
		{
			var decoded = DecodeInternalHdr(file, true, token);
			var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
			for (var i = 0; i < decoded.Length; i++)
			{
				var mip = file.MipMaps[i];
				mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
			}
			return mem2Ds;
		}, token);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>> DecodeHdr2DAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() => DecodeInternalHdr(file, false, token)[0]
			.AsMemory().AsMemory2D((int)file.header.dwHeight, (int)file.header.dwWidth), token);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <param name="token">The cancellation token for this asynchronous operation.</param>
	/// <returns>The awaitable operation to retrieve the decoded image.</returns>
	public Task<Memory2D<ColorRgbFloat>[]> DecodeAllMipMapsHdr2DAsync(DdsFile file, CancellationToken token = default)
	{
		return Task.Run(() =>
		{
			var decoded = DecodeInternalHdr(file, true, token);
			var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
			for (var i = 0; i < decoded.Length; i++)
			{
				var mip = file.Faces[0].MipMaps[i];
				mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
			}
			return mem2Ds;
		}, token);
	}

	#endregion

	#region Sync API

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgbFloat[] DecodeRawHdr(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		return DecodeRawHdr(dataArray, pixelWidth, pixelHeight, format);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="input">The byte array containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgbFloat[] DecodeRawHdr(byte[] input, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		return DecodeRawInternalHdr(input, pixelWidth, pixelHeight, format, default);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgbFloat[] DecodeHdr(KtxFile file)
	{
		return DecodeInternalHdr(file, false, default)[0];
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>An array of decoded images.</returns>
	public ColorRgbFloat[][] DecodeAllMipMapsHdr(KtxFile file)
	{
		return DecodeInternalHdr(file, true, default);
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>The decoded image.</returns>
	public ColorRgbFloat[] DecodeHdr(DdsFile file)
	{
		return DecodeInternalHdr(file, false, default)[0];
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public ColorRgbFloat[][] DecodeAllMipMapsHdr(DdsFile file)
	{
		return DecodeInternalHdr(file, true, default);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method will read the expected amount of bytes from the given input stream and decode it.
	/// Make sure there is no file header information left in the stream before the encoded data.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing the encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgbFloat> DecodeRawHdr2D(Stream inputStream, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var dataArray = new byte[GetBufferSize(format, pixelWidth, pixelHeight)];
		inputStream.Read(dataArray, 0, dataArray.Length);

		var decoded = DecodeRawHdr(dataArray, pixelWidth, pixelHeight, format);
		return decoded.AsMemory().AsMemory2D(pixelHeight, pixelWidth);
	}

	/// <summary>
	/// Decode a single encoded image from raw bytes.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="input">The byte array containing the raw encoded data.</param>
	/// <param name="pixelWidth">The pixelWidth of the image.</param>
	/// <param name="pixelHeight">The pixelHeight of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgbFloat> DecodeRawHdr2D(byte[] input, int pixelWidth, int pixelHeight, CompressionFormat format)
	{
		var decoded = DecodeRawInternalHdr(input, pixelWidth, pixelHeight, format, default);
		return decoded.AsMemory().AsMemory2D(pixelHeight, pixelWidth);
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode the main image from it.
	/// The type of file will be detected automatically.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgbFloat> DecodeHdr2D(Stream inputStream)
	{
		return DecodeFromStreamInternalHdr2D(inputStream, false, default)[0];
	}

	/// <summary>
	/// Read a Ktx or Dds file from a stream and decode all available mipmaps from it.
	/// The type of file will be detected automatically.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream containing a Ktx or Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgbFloat>[] DecodeAllMipMapsHdr2D(Stream inputStream)
	{
		return DecodeFromStreamInternalHdr2D(inputStream, true, default);
	}

	/// <summary>
	/// Decode the main image from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgbFloat> DecodeHdr2D(KtxFile file)
	{
		return DecodeInternalHdr(file, false, default)[0].AsMemory().AsMemory2D((int)file.header.PixelHeight, (int)file.header.PixelWidth);
	}

	/// <summary>
	/// Decode all available mipmaps from a Ktx file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Ktx file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgbFloat>[] DecodeAllMipMapsHdr2D(KtxFile file)
	{
		var decoded = DecodeInternalHdr(file, true, default);
		var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
		for (var i = 0; i < decoded.Length; i++)
		{
			var mip = file.MipMaps[i];
			mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
		}
		return mem2Ds;
	}

	/// <summary>
	/// Decode the main image from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>The decoded image.</returns>
	public Memory2D<ColorRgbFloat> DecodeHdr2D(DdsFile file)
	{
		return DecodeInternalHdr(file, false, default)[0].AsMemory().AsMemory2D((int)file.header.dwHeight, (int)file.header.dwWidth);
	}

	/// <summary>
	/// Decode all available mipmaps from a Dds file.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="file">The loaded Dds file.</param>
	/// <returns>An array of decoded images.</returns>
	public Memory2D<ColorRgbFloat>[] DecodeAllMipMapsHdr2D(DdsFile file)
	{
		var decoded = DecodeInternalHdr(file, true, default);
		var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
		for (var i = 0; i < decoded.Length; i++)
		{
			var mip = file.Faces[0].MipMaps[i];
			mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
		}
		return mem2Ds;
	}

	/// <summary>
	/// Decode a single block from raw bytes and return it as a <see cref="Memory2D{T}"/>.
	/// Input Span size needs to equal the block size.
	/// To get the block size (in bytes) of the compression format used, see <see cref="GetBlockSize(BCnEncoder.Shared.CompressionFormat)"/>.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="blockData">The encoded block in bytes.</param>
	/// <param name="format">The compression format used.</param>
	/// <returns>The decoded 4x4 block.</returns>
	public Memory2D<ColorRgbFloat> DecodeBlockHdr(ReadOnlySpan<byte> blockData, CompressionFormat format)
	{
		var output = new ColorRgbFloat[4, 4];
		DecodeBlockInternalHdr(blockData, format, output);
		return output;
	}

	/// <summary>
	/// Decode a single block from raw bytes and write it to the given output span.
	/// Output span size must be exactly 4x4 and input Span size needs to equal the block size.
	/// To get the block size (in bytes) of the compression format used, see <see cref="GetBlockSize(BCnEncoder.Shared.CompressionFormat)"/>.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="blockData">The encoded block in bytes.</param>
	/// <param name="format">The compression format used.</param>
	/// <param name="outputSpan">The destination span of the decoded data.</param>
	public void DecodeBlockHdr(ReadOnlySpan<byte> blockData, CompressionFormat format, Span2D<ColorRgbFloat> outputSpan)
	{
		if (outputSpan.Width != 4 || outputSpan.Height != 4)
		{
			throw new ArgumentException($"Single block decoding needs an output span of exactly 4x4");
		}
		DecodeBlockInternalHdr(blockData, format, outputSpan);
	}

	/// <summary>
	/// Decode a single block from a stream and write it to the given output span.
	/// Output span size must be exactly 4x4.
	/// This method is only for compressed Hdr formats. Please use the non-Hdr methods for other formats.
	/// </summary>
	/// <param name="inputStream">The stream to read encoded blocks from.</param>
	/// <param name="format">The compression format used.</param>
	/// <param name="outputSpan">The destination span of the decoded data.</param>
	/// <returns>The number of bytes read from the stream. Zero (0) if reached the end of stream.</returns>
	public int DecodeBlockHdr(Stream inputStream, CompressionFormat format, Span2D<ColorRgbFloat> outputSpan)
	{
		if (outputSpan.Width != 4 || outputSpan.Height != 4)
		{
			throw new ArgumentException($"Single block decoding needs an output span of exactly 4x4");
		}

		Span<byte> input = stackalloc byte[16];
		input = input.Slice(0, GetBlockSize(format));

		var bytesRead = inputStream.Read(input);

		if (bytesRead == 0)
		{
			return 0; //End of stream
		}

		if (bytesRead != input.Length)
		{
			throw new Exception("Input stream does not have enough data available for a full block.");
		}

		DecodeBlockInternalHdr(input, format, outputSpan);
		return bytesRead;
	}

	/// <summary>
	/// Check whether a file is encoded in a supported HDR format.
	/// </summary>
	/// <param name="file">The loaded ktx file to check</param>
	/// <returns>If the format of the file is one of the supported HDR formats.</returns>
	public bool IsHdrFormat(KtxFile file)
	{
		return GetCompressionFormat(file.header.GlInternalFormat).IsHdrFormat();
	}

	/// <summary>
	/// Check whether a file is encoded in a supported HDR format.
	/// </summary>
	/// <param name="file">The loaded dds file to check</param>
	/// <returns>If the format of the file is one of the supported HDR formats.</returns>
	public bool IsHdrFormat(DdsFile file)
	{
		return GetCompressionFormat(file).IsHdrFormat();
	}

	#endregion
	#endregion
	/// <summary>
	/// Load a stream and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="stream">The stream containing the image file.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private Memory2D<ColorRgba32>[] DecodeFromStreamInternal2D(Stream stream, bool allMipMaps, CancellationToken token)
	{
		var format = ImageFile.DetermineImageFormat(stream);

		switch (format)
		{
			case ImageFileFormat.Dds:
			{
				var file = DdsFile.Load(stream);
				var decoded = DecodeInternal(file, allMipMaps, token);
				var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
				for (var i = 0; i < decoded.Length; i++)
				{
					var mip = file.Faces[0].MipMaps[i];
					mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
				}

				return mem2Ds;
			}

			case ImageFileFormat.Ktx:
			{
				var file = KtxFile.Load(stream);
				var decoded = DecodeInternal(file, allMipMaps, token);
				var mem2Ds = new Memory2D<ColorRgba32>[decoded.Length];
				for (var i = 0; i < decoded.Length; i++)
				{
					var mip = file.MipMaps[i];
					mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
				}

				return mem2Ds;
			}

			case ImageFileFormat.Unknown:
			default:
				throw new InvalidOperationException("Unknown image format.");
		}
	}

	/// <summary>
	/// Load a KTX file and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="file">The Ktx file to decode.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private ColorRgba32[][] DecodeInternal(KtxFile file, bool allMipMaps, CancellationToken token)
	{
		var mipMaps = allMipMaps ? file.MipMaps.Count : 1;
		var colors = new ColorRgba32[mipMaps][];

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(file.header.GlInternalFormat);
		var totalBlocks = file.MipMaps.Take(mipMaps).Sum(m => m.Faces[0].Data.Length / blockSize);

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		if (IsSupportedRawFormat(file.header.GlInternalFormat))
		{
			var decoder = GetRawDecoder(file.header.GlInternalFormat);

			for (var mip = 0; mip < mipMaps; mip++)
			{
				var data = file.MipMaps[mip].Faces[0].Data;

				colors[mip] = decoder.Decode(data, context);

				context.Progress.SetProcessedBlocks(file.MipMaps.Take(mip + 1).Sum(x => x.Faces[0].Data.Length / blockSize));
			}
		}
		else
		{
			var decoder = GetRgba32Decoder(file.header.GlInternalFormat);
			var format = GetCompressionFormat(file.header.GlInternalFormat);
			if (format.IsHdrFormat())
			{
				throw new NotSupportedException($"This Format is not an RGBA32 compatible format: {format}, please use the HDR versions of the decode methods.");
			}
			if (decoder == null)
			{
				throw new NotSupportedException($"This Format is not supported: {file.header.GlInternalFormat}");
			}

			for (var mip = 0; mip < mipMaps; mip++)
			{
				var data = file.MipMaps[mip].Faces[0].Data;
				var pixelWidth = file.MipMaps[mip].Width;
				var pixelHeight = file.MipMaps[mip].Height;

				var blocks = decoder.Decode(data, context);

				colors[mip] = ImageToBlocks.ColorsFromRawBlocks(blocks, (int)pixelWidth, (int)pixelHeight);

				context.Progress.SetProcessedBlocks(file.MipMaps.Take(mip + 1).Sum(x => x.Faces[0].Data.Length / blockSize));
			}
		}

		return colors;
	}

	/// <summary>
	/// Load a DDS file and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="file">The Dds file to decode.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private ColorRgba32[][] DecodeInternal(DdsFile file, bool allMipMaps, CancellationToken token)
	{
		var mipMaps = allMipMaps ? file.header.dwMipMapCount : 1;
		var colors = new ColorRgba32[mipMaps][];

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(file);
		var totalBlocks = file.Faces[0].MipMaps.Take((int)mipMaps).Sum(m => m.Data.Length / blockSize);

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		if (IsSupportedRawFormat(file))
		{
			var decoder = GetRawDecoder(file);

			for (var mip = 0; mip < mipMaps; mip++)
			{
				var data = file.Faces[0].MipMaps[mip].Data;

				colors[mip] = decoder.Decode(data, context);

				context.Progress.SetProcessedBlocks(file.Faces[0].MipMaps.Take(mip + 1).Sum(x => x.Data.Length / blockSize));
			}
		}
		else
		{
			var dxtFormat = file.header.ddsPixelFormat.IsDxt10Format
				? file.dx10Header.dxgiFormat
				: file.header.ddsPixelFormat.DxgiFormat;
			var format = GetCompressionFormat(file);
			var decoder = GetRgba32Decoder(format);

			if (format.IsHdrFormat())
			{
				throw new NotSupportedException($"This Format is not an RGBA32 compatible format: {format}, please use the HDR versions of the decode methods.");
			}
			if (decoder == null)
			{
				throw new NotSupportedException($"This Format is not supported: {dxtFormat}");
			}

			for (var mip = 0; mip < mipMaps; mip++)
			{
				var data = file.Faces[0].MipMaps[mip].Data;
				var pixelWidth = file.Faces[0].MipMaps[mip].Width;
				var pixelHeight = file.Faces[0].MipMaps[mip].Height;

				var blocks = decoder.Decode(data, context);

				var image = ImageToBlocks.ColorsFromRawBlocks(blocks, (int)pixelWidth, (int)pixelHeight);

				colors[mip] = image;

				context.Progress.SetProcessedBlocks(file.Faces[0].MipMaps.Take(mip + 1).Sum(x => x.Data.Length / blockSize));
			}
		}

		return colors;
	}

	/// <summary>
	/// Decode raw encoded image asynchronously.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="pixelWidth">The width of the image.</param>
	/// <param name="pixelHeight">The height of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="token">The cancellation token for this operation. May be default, if the operation is not asynchronous.</param>
	/// <returns>The decoded Rgba32 image.</returns>
	private ColorRgba32[] DecodeRawInternal(ReadOnlyMemory<byte> input, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token)
	{
		if (input.Length % GetBlockSize(format) != 0)
		{
			throw new ArgumentException("The size of the input buffer does not align with the compression format.");
		}

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(format);
		var totalBlocks = input.Length / blockSize;

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		var isCompressedFormat = format.IsCompressedFormat();
		if (isCompressedFormat)
		{
			// DecodeInternal as compressed data
			var decoder = GetRgba32Decoder(format);

			if (format.IsHdrFormat())
			{
				throw new NotSupportedException($"This Format is not an RGBA32 compatible format: {format}, please use the HDR versions of the decode methods.");
			}
			if (decoder == null)
			{
				throw new NotSupportedException($"This Format is not supported: {format}");
			}

			var blocks = decoder.Decode(input, context);

			return ImageToBlocks.ColorsFromRawBlocks(blocks, pixelWidth, pixelHeight); ;
		}

		// DecodeInternal as raw data
		var rawDecoder = GetRawDecoder(format);

		return rawDecoder.Decode(input, context);
	}

	private void DecodeBlockInternal(ReadOnlySpan<byte> blockData, CompressionFormat format, Span2D<ColorRgba32> outputSpan)
	{
		var decoder = GetRgba32Decoder(format);
		if (format.IsHdrFormat())
		{
			throw new NotSupportedException($"This Format is not an RGBA32 compatible format: {format}, please use the HDR versions of the decode methods.");
		}
		if (decoder == null)
		{
			throw new NotSupportedException($"This Format is not supported: {format}");
		}
		if (blockData.Length != GetBlockSize(format))
		{
			throw new ArgumentException("The size of the input buffer does not align with the compression format.");
		}

		var rawBlock = decoder.DecodeBlock(blockData);
		var pixels = rawBlock.AsSpan;

		pixels[..4].CopyTo(outputSpan.GetRowSpan(0));
		pixels.Slice(4, 4).CopyTo(outputSpan.GetRowSpan(1));
		pixels.Slice(8, 4).CopyTo(outputSpan.GetRowSpan(2));
		pixels.Slice(12, 4).CopyTo(outputSpan.GetRowSpan(3));
	}

	#region Hdr internals

	/// <summary>
	/// Load a stream and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="stream">The stream containing the image file.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private Memory2D<ColorRgbFloat>[] DecodeFromStreamInternalHdr2D(Stream stream, bool allMipMaps, CancellationToken token)
	{
		var format = ImageFile.DetermineImageFormat(stream);

		switch (format)
		{
			case ImageFileFormat.Dds:
			{
				var file = DdsFile.Load(stream);
				var decoded = DecodeInternalHdr(file, allMipMaps, token);
				var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
				for (var i = 0; i < decoded.Length; i++)
				{
					var mip = file.Faces[0].MipMaps[i];
					mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
				}

				return mem2Ds;
			}

			case ImageFileFormat.Ktx:
			{
				var file = KtxFile.Load(stream);
				var decoded = DecodeInternalHdr(file, allMipMaps, token);
				var mem2Ds = new Memory2D<ColorRgbFloat>[decoded.Length];
				for (var i = 0; i < decoded.Length; i++)
				{
					var mip = file.MipMaps[i];
					mem2Ds[i] = decoded[i].AsMemory().AsMemory2D((int)mip.Height, (int)mip.Width);
				}

				return mem2Ds;
			}

			case ImageFileFormat.Unknown:
			default:
				throw new InvalidOperationException("Unknown image format.");
		}
	}

	/// <summary>
	/// Load a KTX file and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="file">The Ktx file to decode.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private ColorRgbFloat[][] DecodeInternalHdr(KtxFile file, bool allMipMaps, CancellationToken token)
	{
		var mipMaps = allMipMaps ? file.MipMaps.Count : 1;
		var colors = new ColorRgbFloat[mipMaps][];

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(file.header.GlInternalFormat);
		var totalBlocks = file.MipMaps.Take(mipMaps).Sum(m => m.Faces[0].Data.Length / blockSize);

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		var decoder = GetRgbFloatDecoder(file.header.GlInternalFormat);
		var format = GetCompressionFormat(file.header.GlInternalFormat);
		if (!format.IsHdrFormat())
		{
			throw new NotSupportedException($"This Format is not an HDR format: {format}, please use the non-HDR versions of the decode methods.");
		}
		if (decoder == null)
		{
			throw new NotSupportedException($"This Format is not supported: {file.header.GlInternalFormat}");
		}

		for (var mip = 0; mip < mipMaps; mip++)
		{
			var data = file.MipMaps[mip].Faces[0].Data;
			var pixelWidth = file.MipMaps[mip].Width;
			var pixelHeight = file.MipMaps[mip].Height;

			var blocks = decoder.Decode(data, context);

			colors[mip] = ImageToBlocks.ColorsFromRawBlocks(blocks, (int)pixelWidth, (int)pixelHeight);

			context.Progress.SetProcessedBlocks(file.MipMaps.Take(mip + 1).Sum(x => x.Faces[0].Data.Length / blockSize));
		}

		return colors;
	}

	/// <summary>
	/// Load a DDS file and extract either the main image or all mip maps.
	/// </summary>
	/// <param name="file">The Dds file to decode.</param>
	/// <param name="allMipMaps">If all mip maps or only the main image should be decoded.</param>
	/// <param name="token">The cancellation token for this operation. Can be default, if the operation is not asynchronous.</param>
	/// <returns>An array of decoded Rgba32 images.</returns>
	private ColorRgbFloat[][] DecodeInternalHdr(DdsFile file, bool allMipMaps, CancellationToken token)
	{
		var mipMaps = allMipMaps ? file.header.dwMipMapCount : 1;
		var colors = new ColorRgbFloat[mipMaps][];

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(file);
		var totalBlocks = file.Faces[0].MipMaps.Take((int)mipMaps).Sum(m => m.Data.Length / blockSize);

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		var dxtFormat = file.header.ddsPixelFormat.IsDxt10Format
			? file.dx10Header.dxgiFormat
			: file.header.ddsPixelFormat.DxgiFormat;
		var format = GetCompressionFormat(file);
		var decoder = GetRgbFloatDecoder(format);

		if (!format.IsHdrFormat())
		{
			throw new NotSupportedException($"This Format is not an HDR format: {format}, please use the non-HDR versions of the decode methods.");
		}
		if (decoder == null)
		{
			throw new NotSupportedException($"This Format is not supported: {dxtFormat}");
		}

		for (var mip = 0; mip < mipMaps; mip++)
		{
			var data = file.Faces[0].MipMaps[mip].Data;
			var pixelWidth = file.Faces[0].MipMaps[mip].Width;
			var pixelHeight = file.Faces[0].MipMaps[mip].Height;

			var blocks = decoder.Decode(data, context);

			var image = ImageToBlocks.ColorsFromRawBlocks(blocks, (int)pixelWidth, (int)pixelHeight);

			colors[mip] = image;

			context.Progress.SetProcessedBlocks(file.Faces[0].MipMaps.Take(mip + 1).Sum(x => x.Data.Length / blockSize));
		}

		return colors;
	}

	/// <summary>
	/// Decode raw encoded image asynchronously.
	/// </summary>
	/// <param name="input">The <see cref="Memory{T}"/> containing the encoded data.</param>
	/// <param name="pixelWidth">The width of the image.</param>
	/// <param name="pixelHeight">The height of the image.</param>
	/// <param name="format">The Format the encoded data is in.</param>
	/// <param name="token">The cancellation token for this operation. May be default, if the operation is not asynchronous.</param>
	/// <returns>The decoded Rgba32 image.</returns>
	private ColorRgbFloat[] DecodeRawInternalHdr(ReadOnlyMemory<byte> input, int pixelWidth, int pixelHeight, CompressionFormat format, CancellationToken token)
	{
		if (input.Length % GetBlockSize(format) != 0)
		{
			throw new ArgumentException("The size of the input buffer does not align with the compression format.");
		}

		var context = new OperationContext
		{
			CancellationToken = token,
			IsParallel = Options.IsParallel,
			TaskCount = Options.TaskCount
		};

		// Calculate total blocks
		var blockSize = GetBlockSize(format);
		var totalBlocks = input.Length / blockSize;

		context.Progress = new OperationProgress(Options.Progress, totalBlocks);

		var decoder = GetRgbFloatDecoder(format);

		if (!format.IsHdrFormat())
		{
			throw new NotSupportedException($"This Format is not an HDR format: {format}, please use the non-HDR versions of the decode methods.");
		}
		if (decoder == null)
		{
			throw new NotSupportedException($"This Format is not supported: {format}");
		}

		var blocks = decoder.Decode(input, context);

		return ImageToBlocks.ColorsFromRawBlocks(blocks, pixelWidth, pixelHeight);
	}

	private void DecodeBlockInternalHdr(ReadOnlySpan<byte> blockData, CompressionFormat format, Span2D<ColorRgbFloat> outputSpan)
	{
		var decoder = GetRgbFloatDecoder(format);
		if (!format.IsHdrFormat())
		{
			throw new NotSupportedException($"This Format is not an HDR format: {format}, please use the non-HDR versions of the decode methods.");
		}
		if (decoder == null)
		{
			throw new NotSupportedException($"This Format is not supported: {format}");
		}
		if (blockData.Length != GetBlockSize(format))
		{
			throw new ArgumentException("The size of the input buffer does not align with the compression format.");
		}

		var rawBlock = decoder.DecodeBlock(blockData);
		var pixels = rawBlock.AsSpan;

		pixels[..4].CopyTo(outputSpan.GetRowSpan(0));
		pixels.Slice(4, 4).CopyTo(outputSpan.GetRowSpan(1));
		pixels.Slice(8, 4).CopyTo(outputSpan.GetRowSpan(2));
		pixels.Slice(12, 4).CopyTo(outputSpan.GetRowSpan(3));
	}
	#endregion

	#region Support

	#region Is supported format

	private bool IsSupportedRawFormat(GlInternalFormat format)
	{
		return IsSupportedRawFormat(GetCompressionFormat(format));
	}

	private bool IsSupportedRawFormat(DdsFile file)
	{
		return IsSupportedRawFormat(GetCompressionFormat(file));
	}

	private bool IsSupportedRawFormat(CompressionFormat format)
	{
		return format switch
		{
			CompressionFormat.R or CompressionFormat.Rg or CompressionFormat.Rgb or CompressionFormat.Rgba or CompressionFormat.Bgra => true,
			_ => false,
		};
	}

	#endregion

	#region Get decoder

	private IBcBlockDecoder<RawBlock4X4Rgba32> GetRgba32Decoder(GlInternalFormat format)
	{
		return GetRgba32Decoder(GetCompressionFormat(format));
	}

	private IBcBlockDecoder<RawBlock4X4Rgba32> GetRgba32Decoder(DdsFile file)
	{
		return GetRgba32Decoder(GetCompressionFormat(file));
	}

	private IBcBlockDecoder<RawBlock4X4Rgba32> GetRgba32Decoder(CompressionFormat format)
	{
		return format switch
		{
			CompressionFormat.Bc1 => new Bc1NoAlphaDecoder(),
			CompressionFormat.Bc1WithAlpha => new Bc1ADecoder(),
			CompressionFormat.Bc2 => new Bc2Decoder(),
			CompressionFormat.Bc3 => new Bc3Decoder(),
			CompressionFormat.Bc4 => new Bc4Decoder(OutputOptions.Bc4Component),
			CompressionFormat.Bc5 => new Bc5Decoder(OutputOptions.Bc5Component1, OutputOptions.Bc5Component2),
			CompressionFormat.Bc7 => new Bc7Decoder(),
			CompressionFormat.Atc => new AtcDecoder(),
			CompressionFormat.AtcExplicitAlpha => new AtcExplicitAlphaDecoder(),
			CompressionFormat.AtcInterpolatedAlpha => new AtcInterpolatedAlphaDecoder(),
			_ => null
		};
	}

	private IBcBlockDecoder<RawBlock4X4RgbFloat> GetRgbFloatDecoder(GlInternalFormat format)
	{
		return GetRgbFloatDecoder(GetCompressionFormat(format));
	}

	private IBcBlockDecoder<RawBlock4X4RgbFloat> GetRgbFloatDecoder(DdsFile file)
	{
		return GetRgbFloatDecoder(GetCompressionFormat(file));
	}

	private IBcBlockDecoder<RawBlock4X4RgbFloat> GetRgbFloatDecoder(CompressionFormat format)
	{
		return format switch
		{
			CompressionFormat.Bc6S => new Bc6SDecoder(),
			CompressionFormat.Bc6U => new Bc6UDecoder(),
			_ => null
		};
	}

	#endregion

	#region Get raw decoder

	private IRawDecoder GetRawDecoder(GlInternalFormat format)
	{
		return GetRawDecoder(GetCompressionFormat(format));
	}

	private IRawDecoder GetRawDecoder(DdsFile file)
	{
		return GetRawDecoder(GetCompressionFormat(file));
	}

	private IRawDecoder GetRawDecoder(CompressionFormat format)
	{
		return format switch
		{
			CompressionFormat.R => new RawRDecoder(OutputOptions.RedAsLuminance),
			CompressionFormat.Rg => new RawRgDecoder(),
			CompressionFormat.Rgb => new RawRgbDecoder(),
			CompressionFormat.Rgba => new RawRgbaDecoder(),
			CompressionFormat.Bgra => new RawBgraDecoder(),
			CompressionFormat.Bc1 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc1WithAlpha => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc2 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc3 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc4 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc5 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc6U => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc6S => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Bc7 => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.Atc => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			CompressionFormat.AtcExplicitAlpha => throw new ArgumentOutOfRangeException(nameof(format), format,
				null),
			CompressionFormat.AtcInterpolatedAlpha => throw new ArgumentOutOfRangeException(nameof(format), format,
				null),
			CompressionFormat.Unknown => throw new ArgumentOutOfRangeException(nameof(format), format, null),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	#endregion

	#region Get block size

	/// <summary>
	/// Gets the number of total blocks in an image with the given pixel width and height.
	/// </summary>
	/// <param name="pixelWidth">The pixel width of the image</param>
	/// <param name="pixelHeight">The pixel height of the image</param>
	/// <returns>The total number of blocks.</returns>
	public int GetBlockCount(int pixelWidth, int pixelHeight)
	{
		return ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight);
	}

	/// <summary>
	/// Gets the number of blocks in an image with the given pixel width and height.
	/// </summary>
	/// <param name="pixelWidth">The pixel width of the image</param>
	/// <param name="pixelHeight">The pixel height of the image</param>
	/// <param name="blocksWidth">The amount of blocks in the x-axis</param>
	/// <param name="blocksHeight">The amount of blocks in the y-axis</param>
	public void GetBlockCount(int pixelWidth, int pixelHeight, out int blocksWidth, out int blocksHeight)
	{
		ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight, out blocksWidth, out blocksHeight);
	}

	private int GetBlockSize(GlInternalFormat format)
	{
		return GetBlockSize(GetCompressionFormat(format));
	}

	private int GetBlockSize(DdsFile file)
	{
		return GetBlockSize(GetCompressionFormat(file));
	}

	/// <summary>
	/// Get the size of blocks for the given compression format in bytes.
	/// </summary>
	/// <param name="format">The compression format used.</param>
	/// <returns>The size of a single block in bytes.</returns>
	public int GetBlockSize(CompressionFormat format)
	{
		return format switch
		{
			CompressionFormat.R => 1,
			CompressionFormat.Rg => 2,
			CompressionFormat.Rgb => 3,
			CompressionFormat.Rgba => 4,
			CompressionFormat.Bgra => 4,
			CompressionFormat.Bc1 => Unsafe.SizeOf<Bc1Block>(),
			CompressionFormat.Bc1WithAlpha => Unsafe.SizeOf<Bc1Block>(),
			CompressionFormat.Bc2 => Unsafe.SizeOf<Bc2Block>(),
			CompressionFormat.Bc3 => Unsafe.SizeOf<Bc3Block>(),
			CompressionFormat.Bc4 => Unsafe.SizeOf<Bc4Block>(),
			CompressionFormat.Bc5 => Unsafe.SizeOf<Bc5Block>(),
			CompressionFormat.Bc6S => Unsafe.SizeOf<Bc6Block>(),
			CompressionFormat.Bc6U => Unsafe.SizeOf<Bc6Block>(),
			CompressionFormat.Bc7 => Unsafe.SizeOf<Bc7Block>(),
			CompressionFormat.Atc => Unsafe.SizeOf<AtcBlock>(),
			CompressionFormat.AtcExplicitAlpha => Unsafe.SizeOf<AtcExplicitAlphaBlock>(),
			CompressionFormat.AtcInterpolatedAlpha => Unsafe.SizeOf<AtcInterpolatedAlphaBlock>(),
			CompressionFormat.Unknown => 0,
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	#endregion

	private CompressionFormat GetCompressionFormat(GlInternalFormat format)
	{
		switch (format)
		{
			case GlInternalFormat.GlR8:
				return CompressionFormat.R;

			case GlInternalFormat.GlRg8:
				return CompressionFormat.Rg;

			case GlInternalFormat.GlRgb8:
				return CompressionFormat.Rgb;

			case GlInternalFormat.GlRgba8:
				return CompressionFormat.Rgba;

			// HINT: Bgra is not supported by default. The format enum is added by an extension by Apple.
			case GlInternalFormat.GlBgra8Extension:
				return CompressionFormat.Bgra;

			case GlInternalFormat.GlCompressedRgbS3TcDxt1Ext:
				return CompressionFormat.Bc1;

			case GlInternalFormat.GlCompressedRgbaS3TcDxt1Ext:
				return CompressionFormat.Bc1WithAlpha;

			case GlInternalFormat.GlCompressedRgbaS3TcDxt3Ext:
				return CompressionFormat.Bc2;

			case GlInternalFormat.GlCompressedRgbaS3TcDxt5Ext:
				return CompressionFormat.Bc3;

			case GlInternalFormat.GlCompressedRedRgtc1Ext:
				return CompressionFormat.Bc4;

			case GlInternalFormat.GlCompressedRedGreenRgtc2Ext:
				return CompressionFormat.Bc5;

			case GlInternalFormat.GlCompressedRgbBptcUnsignedFloatArb:
				return CompressionFormat.Bc6U;

			case GlInternalFormat.GlCompressedRgbBptcSignedFloatArb:
				return CompressionFormat.Bc6S;

			// TODO: Not sure what to do with SRGB input.
			case GlInternalFormat.GlCompressedRgbaBptcUnormArb:
			case GlInternalFormat.GlCompressedSrgbAlphaBptcUnormArb:
				return CompressionFormat.Bc7;

			case GlInternalFormat.GlCompressedRgbAtc:
				return CompressionFormat.Atc;

			case GlInternalFormat.GlCompressedRgbaAtcExplicitAlpha:
				return CompressionFormat.AtcExplicitAlpha;

			case GlInternalFormat.GlCompressedRgbaAtcInterpolatedAlpha:
				return CompressionFormat.AtcInterpolatedAlpha;

			case GlInternalFormat.GlRgba4:
			case GlInternalFormat.GlRgb5:
			case GlInternalFormat.GlRgb565:
			case GlInternalFormat.GlRgb5A1:
			case GlInternalFormat.GlRgba16:
			case GlInternalFormat.GlDepthComponent16:
			case GlInternalFormat.GlDepthComponent24:
			case GlInternalFormat.GlDepthComponent32F:
			case GlInternalFormat.GlStencilIndex8:
			case GlInternalFormat.GlDepth24Stencil8:
			case GlInternalFormat.GlDepth32FStencil8:
			case GlInternalFormat.GlRg16:
			case GlInternalFormat.GlR16F:
			case GlInternalFormat.GlR32F:
			case GlInternalFormat.GlRg16F:
			case GlInternalFormat.GlRg32F:
			case GlInternalFormat.GlRgba32F:
			case GlInternalFormat.GlRgba16F:
			case GlInternalFormat.GlR8Ui:
			case GlInternalFormat.GlR8I:
			case GlInternalFormat.GlR16:
			case GlInternalFormat.GlR16I:
			case GlInternalFormat.GlR16Ui:
			case GlInternalFormat.GlR32I:
			case GlInternalFormat.GlR32Ui:
			case GlInternalFormat.GlRg8I:
			case GlInternalFormat.GlRg8Ui:
			case GlInternalFormat.GlRg16I:
			case GlInternalFormat.GlRg16Ui:
			case GlInternalFormat.GlRg32I:
			case GlInternalFormat.GlRg32Ui:
			case GlInternalFormat.GlRgb8I:
			case GlInternalFormat.GlRgb8Ui:
			case GlInternalFormat.GlRgba12:
			case GlInternalFormat.GlRgba2:
			case GlInternalFormat.GlRgba8I:
			case GlInternalFormat.GlRgba8Ui:
			case GlInternalFormat.GlRgba16I:
			case GlInternalFormat.GlRgba16Ui:
			case GlInternalFormat.GlRgba32I:
			case GlInternalFormat.GlRgba32Ui:
			case GlInternalFormat.GlR8Snorm:
			case GlInternalFormat.GlRg8Snorm:
			case GlInternalFormat.GlRgb8Snorm:
			case GlInternalFormat.GlRgba8Snorm:
			case GlInternalFormat.GlR16Snorm:
			case GlInternalFormat.GlRg16Snorm:
			case GlInternalFormat.GlRgb16Snorm:
			case GlInternalFormat.GlRgba16Snorm:
			case GlInternalFormat.GlRgb10A2:
			case GlInternalFormat.GlRgb10A2Ui:
			case GlInternalFormat.GlRgb16:
			case GlInternalFormat.GlRgb16F:
			case GlInternalFormat.GlRgb16I:
			case GlInternalFormat.GlRgb16Ui:
			case GlInternalFormat.GlRgb32F:
			case GlInternalFormat.GlRgb32I:
			case GlInternalFormat.GlRgb32Ui:
			case GlInternalFormat.GlCompressedSrgbS3TcDxt1Ext:
			case GlInternalFormat.GlCompressedSrgbAlphaS3TcDxt1Ext:
			case GlInternalFormat.GlCompressedSrgbAlphaS3TcDxt3Ext:
			case GlInternalFormat.GlCompressedSrgbAlphaS3TcDxt5Ext:
			case GlInternalFormat.GlCompressedSignedRedGreenRgtc2Ext:
			case GlInternalFormat.GlCompressedSignedRedRgtc1Ext:
			case GlInternalFormat.GlEtc1Rgb8Oes:
			case GlInternalFormat.GlCompressedR11Eac:
			case GlInternalFormat.GlCompressedSignedR11Eac:
			case GlInternalFormat.GlCompressedRg11Eac:
			case GlInternalFormat.GlCompressedSignedRg11Eac:
			case GlInternalFormat.GlCompressedRgb8Etc2:
			case GlInternalFormat.GlCompressedSrgb8Etc2:
			case GlInternalFormat.GlCompressedRgb8PunchthroughAlpha1Etc2:
			case GlInternalFormat.GlCompressedSrgb8PunchthroughAlpha1Etc2:
			case GlInternalFormat.GlCompressedRgba8Etc2Eac:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Etc2Eac:
			case GlInternalFormat.GlCompressedRgbaAstc4X4Khr:
			case GlInternalFormat.GlCompressedRgbaAstc5X4Khr:
			case GlInternalFormat.GlCompressedRgbaAstc5X5Khr:
			case GlInternalFormat.GlCompressedRgbaAstc6X5Khr:
			case GlInternalFormat.GlCompressedRgbaAstc6X6Khr:
			case GlInternalFormat.GlCompressedRgbaAstc8X5Khr:
			case GlInternalFormat.GlCompressedRgbaAstc8X6Khr:
			case GlInternalFormat.GlCompressedRgbaAstc8X8Khr:
			case GlInternalFormat.GlCompressedRgbaAstc10X5Khr:
			case GlInternalFormat.GlCompressedRgbaAstc10X6Khr:
			case GlInternalFormat.GlCompressedRgbaAstc10X8Khr:
			case GlInternalFormat.GlCompressedRgbaAstc10X10Khr:
			case GlInternalFormat.GlCompressedRgbaAstc12X10Khr:
			case GlInternalFormat.GlCompressedRgbaAstc12X12Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc4X4Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc5X4Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc5X5Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc6X5Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc6X6Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc8X5Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc8X6Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc8X8Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc10X5Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc10X6Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc10X8Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc10X10Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc12X10Khr:
			case GlInternalFormat.GlCompressedSrgb8Alpha8Astc12X12Khr:
			default:
				return CompressionFormat.Unknown;
		}
	}

	private CompressionFormat GetCompressionFormat(DdsFile file)
	{
		var format = file.header.ddsPixelFormat.IsDxt10Format ?
			file.dx10Header.dxgiFormat :
			file.header.ddsPixelFormat.DxgiFormat;

		switch (format)
		{
			case DxgiFormat.DxgiFormatR8Unorm:
				return CompressionFormat.R;

			case DxgiFormat.DxgiFormatR8G8Unorm:
				return CompressionFormat.Rg;

			// HINT: R8G8B8 has no DxgiFormat to convert from

			case DxgiFormat.DxgiFormatR8G8B8A8Unorm:
				return CompressionFormat.Rgba;

			case DxgiFormat.DxgiFormatB8G8R8A8Unorm:
				return CompressionFormat.Bgra;

			case DxgiFormat.DxgiFormatBc1Unorm:
			case DxgiFormat.DxgiFormatBc1UnormSrgb:
			case DxgiFormat.DxgiFormatBc1Typeless:
				if (file.header.ddsPixelFormat.dwFlags.HasFlag(PixelFormatFlags.DdpfAlphaPixels))
					return CompressionFormat.Bc1WithAlpha;

				return InputOptions.DdsBc1ExpectAlpha ? CompressionFormat.Bc1WithAlpha : CompressionFormat.Bc1;

			case DxgiFormat.DxgiFormatBc2Unorm:
			case DxgiFormat.DxgiFormatBc2UnormSrgb:
			case DxgiFormat.DxgiFormatBc2Typeless:
				return CompressionFormat.Bc2;

			case DxgiFormat.DxgiFormatBc3Unorm:
			case DxgiFormat.DxgiFormatBc3UnormSrgb:
			case DxgiFormat.DxgiFormatBc3Typeless:
				return CompressionFormat.Bc3;

			case DxgiFormat.DxgiFormatBc4Unorm:
			case DxgiFormat.DxgiFormatBc4Snorm:
			case DxgiFormat.DxgiFormatBc4Typeless:
				return CompressionFormat.Bc4;

			case DxgiFormat.DxgiFormatBc5Unorm:
			case DxgiFormat.DxgiFormatBc5Snorm:
			case DxgiFormat.DxgiFormatBc5Typeless:
				return CompressionFormat.Bc5;

			case DxgiFormat.DxgiFormatBc6HTypeless:
			case DxgiFormat.DxgiFormatBc6HUf16:
				return CompressionFormat.Bc6U;

			case DxgiFormat.DxgiFormatBc6HSf16:
				return CompressionFormat.Bc6S;

			case DxgiFormat.DxgiFormatBc7Unorm:
			case DxgiFormat.DxgiFormatBc7UnormSrgb:
			case DxgiFormat.DxgiFormatBc7Typeless:
				return CompressionFormat.Bc7;

			case DxgiFormat.DxgiFormatAtcExt:
				return CompressionFormat.Atc;

			case DxgiFormat.DxgiFormatAtcExplicitAlphaExt:
				return CompressionFormat.AtcExplicitAlpha;

			case DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt:
				return CompressionFormat.AtcInterpolatedAlpha;

			case DxgiFormat.DxgiFormatUnknown:
			case DxgiFormat.DxgiFormatR32G32B32A32Typeless:
			case DxgiFormat.DxgiFormatR32G32B32A32Float:
			case DxgiFormat.DxgiFormatR32G32B32A32Uint:
			case DxgiFormat.DxgiFormatR32G32B32A32Sint:
			case DxgiFormat.DxgiFormatR32G32B32Typeless:
			case DxgiFormat.DxgiFormatR32G32B32Float:
			case DxgiFormat.DxgiFormatR32G32B32Uint:
			case DxgiFormat.DxgiFormatR32G32B32Sint:
			case DxgiFormat.DxgiFormatR16G16B16A16Typeless:
			case DxgiFormat.DxgiFormatR16G16B16A16Float:
			case DxgiFormat.DxgiFormatR16G16B16A16Unorm:
			case DxgiFormat.DxgiFormatR16G16B16A16Uint:
			case DxgiFormat.DxgiFormatR16G16B16A16Snorm:
			case DxgiFormat.DxgiFormatR16G16B16A16Sint:
			case DxgiFormat.DxgiFormatR32G32Typeless:
			case DxgiFormat.DxgiFormatR32G32Float:
			case DxgiFormat.DxgiFormatR32G32Uint:
			case DxgiFormat.DxgiFormatR32G32Sint:
			case DxgiFormat.DxgiFormatR32G8X24Typeless:
			case DxgiFormat.DxgiFormatD32FloatS8X24Uint:
			case DxgiFormat.DxgiFormatR32FloatX8X24Typeless:
			case DxgiFormat.DxgiFormatX32TypelessG8X24Uint:
			case DxgiFormat.DxgiFormatR10G10B10A2Typeless:
			case DxgiFormat.DxgiFormatR10G10B10A2Unorm:
			case DxgiFormat.DxgiFormatR10G10B10A2Uint:
			case DxgiFormat.DxgiFormatR11G11B10Float:
			case DxgiFormat.DxgiFormatR8G8B8A8Typeless:
			case DxgiFormat.DxgiFormatR8G8B8A8UnormSrgb:
			case DxgiFormat.DxgiFormatR8G8B8A8Uint:
			case DxgiFormat.DxgiFormatR8G8B8A8Snorm:
			case DxgiFormat.DxgiFormatR8G8B8A8Sint:
			case DxgiFormat.DxgiFormatR16G16Typeless:
			case DxgiFormat.DxgiFormatR16G16Float:
			case DxgiFormat.DxgiFormatR16G16Unorm:
			case DxgiFormat.DxgiFormatR16G16Uint:
			case DxgiFormat.DxgiFormatR16G16Snorm:
			case DxgiFormat.DxgiFormatR16G16Sint:
			case DxgiFormat.DxgiFormatR32Typeless:
			case DxgiFormat.DxgiFormatD32Float:
			case DxgiFormat.DxgiFormatR32Float:
			case DxgiFormat.DxgiFormatR32Uint:
			case DxgiFormat.DxgiFormatR32Sint:
			case DxgiFormat.DxgiFormatR24G8Typeless:
			case DxgiFormat.DxgiFormatD24UnormS8Uint:
			case DxgiFormat.DxgiFormatR24UnormX8Typeless:
			case DxgiFormat.DxgiFormatX24TypelessG8Uint:
			case DxgiFormat.DxgiFormatR8G8Typeless:
			case DxgiFormat.DxgiFormatR8G8Uint:
			case DxgiFormat.DxgiFormatR8G8Snorm:
			case DxgiFormat.DxgiFormatR8G8Sint:
			case DxgiFormat.DxgiFormatR16Typeless:
			case DxgiFormat.DxgiFormatR16Float:
			case DxgiFormat.DxgiFormatD16Unorm:
			case DxgiFormat.DxgiFormatR16Unorm:
			case DxgiFormat.DxgiFormatR16Uint:
			case DxgiFormat.DxgiFormatR16Snorm:
			case DxgiFormat.DxgiFormatR16Sint:
			case DxgiFormat.DxgiFormatR8Typeless:
			case DxgiFormat.DxgiFormatR8Uint:
			case DxgiFormat.DxgiFormatR8Snorm:
			case DxgiFormat.DxgiFormatR8Sint:
			case DxgiFormat.DxgiFormatA8Unorm:
			case DxgiFormat.DxgiFormatR1Unorm:
			case DxgiFormat.DxgiFormatR9G9B9E5Sharedexp:
			case DxgiFormat.DxgiFormatR8G8B8G8Unorm:
			case DxgiFormat.DxgiFormatG8R8G8B8Unorm:
			case DxgiFormat.DxgiFormatB5G6R5Unorm:
			case DxgiFormat.DxgiFormatB5G5R5A1Unorm:
			case DxgiFormat.DxgiFormatB8G8R8X8Unorm:
			case DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm:
			case DxgiFormat.DxgiFormatB8G8R8A8Typeless:
			case DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb:
			case DxgiFormat.DxgiFormatB8G8R8X8Typeless:
			case DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb:
			case DxgiFormat.DxgiFormatAyuv:
			case DxgiFormat.DxgiFormatY410:
			case DxgiFormat.DxgiFormatY416:
			case DxgiFormat.DxgiFormatNv12:
			case DxgiFormat.DxgiFormatP010:
			case DxgiFormat.DxgiFormatP016:
			case DxgiFormat.DxgiFormat420Opaque:
			case DxgiFormat.DxgiFormatYuy2:
			case DxgiFormat.DxgiFormatY210:
			case DxgiFormat.DxgiFormatY216:
			case DxgiFormat.DxgiFormatNv11:
			case DxgiFormat.DxgiFormatAi44:
			case DxgiFormat.DxgiFormatIa44:
			case DxgiFormat.DxgiFormatP8:
			case DxgiFormat.DxgiFormatA8P8:
			case DxgiFormat.DxgiFormatB4G4R4A4Unorm:
			case DxgiFormat.DxgiFormatP208:
			case DxgiFormat.DxgiFormatV208:
			case DxgiFormat.DxgiFormatV408:
			case DxgiFormat.DxgiFormatForceUint:
			default:
				return CompressionFormat.Unknown;
		}
	}

	private int GetBufferSize(CompressionFormat format, int pixelWidth, int pixelHeight)
	{
		return format switch
		{
			CompressionFormat.R => pixelWidth * pixelHeight,
			CompressionFormat.Rg => 2 * pixelWidth * pixelHeight,
			CompressionFormat.Rgb => 3 * pixelWidth * pixelHeight,
			CompressionFormat.Rgba => 4 * pixelWidth * pixelHeight,
			CompressionFormat.Bgra => 4 * pixelWidth * pixelHeight,
			CompressionFormat.Bc1 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc1WithAlpha => GetBlockSize(format) *
			                                  ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc2 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc3 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc4 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc5 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc6S => GetBlockSize(format) *
			                          ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc6U => GetBlockSize(format) *
			                          ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Bc7 => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Atc => GetBlockSize(format) *
			                         ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.AtcExplicitAlpha => GetBlockSize(format) *
			                                      ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.AtcInterpolatedAlpha => GetBlockSize(format) *
			                                          ImageToBlocks.CalculateNumOfBlocks(pixelWidth, pixelHeight),
			CompressionFormat.Unknown => 0,
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	#endregion
}
