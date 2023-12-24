using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.NET.ImageSharp;
using BCnEncoder.Shared;
using BCnEncTests.Support;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace BCnEncTests;

public class EncodingAsyncTest
{
	private readonly BcEncoder encoder = new();
	private readonly BcDecoder decoder = new();
	private readonly Image<Rgba32> originalImage = ImageLoader.TestGradient1;
	private readonly Image<Rgba32>[] originalCubeMap = ImageLoader.TestCubemap;

	[Fact]
	public async void EncodeToDdsAsync()
	{
		var file = await encoder.EncodeToDdsAsync(originalImage);
		var image = await decoder.DecodeToImageRgba32Async(file);

		TestHelper.AssertImagesEqual(originalImage, image, encoder.OutputOptions.Quality);
		image.Dispose();
	}

	[Fact]
	public async void EncodeToKtxAsync()
	{
		var file = await encoder.EncodeToKtxAsync(originalImage);
		var image = await decoder.DecodeToImageRgba32Async(file);

		TestHelper.AssertImagesEqual(originalImage, image, encoder.OutputOptions.Quality);
		image.Dispose();
	}

	[Fact]
	public async void EncodeCubemapToDdsAsync()
	{
		var file = await encoder.EncodeCubeMapToDdsAsync(originalCubeMap[0], originalCubeMap[1], originalCubeMap[2],
			originalCubeMap[3], originalCubeMap[4], originalCubeMap[5]);

		for (var i = 0; i < 6; i++)
		{
			var image = await decoder.DecodeRawToImageRgba32Async(file.Faces[i].MipMaps[0].Data,
				(int)file.Faces[i].Width, (int)file.Faces[i].Height, CompressionFormat.Bc1);

			TestHelper.AssertImagesEqual(originalCubeMap[i], image, encoder.OutputOptions.Quality);
			image.Dispose();
		}
	}

	[Fact]
	public async void EncodeCubemapToKtxAsync()
	{
		var file = await encoder.EncodeCubeMapToKtxAsync(originalCubeMap[0], originalCubeMap[1], originalCubeMap[2],
			originalCubeMap[3], originalCubeMap[4], originalCubeMap[5]);

		for (var i = 0; i < 6; i++)
		{
			var image = await decoder.DecodeRawToImageRgba32Async(file.MipMaps[0].Faces[i].Data,
				(int)file.MipMaps[0].Faces[i].Width, (int)file.MipMaps[0].Faces[i].Height, CompressionFormat.Bc1);

			TestHelper.AssertImagesEqual(originalCubeMap[i], image, encoder.OutputOptions.Quality);
			image.Dispose();
		}
	}

	[Fact]
	public async void EncodeToRawBytesAsync()
	{
		var data = await encoder.EncodeToRawBytesAsync(originalImage);
		var image = await decoder.DecodeRawToImageRgba32Async(data[0], originalImage.Width, originalImage.Height,
			CompressionFormat.Bc1);

		TestHelper.AssertImagesEqual(originalImage, image, encoder.OutputOptions.Quality);
		image.Dispose();
	}
}
