using System;
using System.IO;
using System.Threading.Tasks;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.NET.ImageSharp;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;
using BCnEncTests.Support;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace BCnEncTests;

public class ProgressTests(ITestOutputHelper output)
{
	private async Task ExecuteEncodeProgressReport(BcEncoder encoder, Image<Rgba32> testImage, int expectedTotalBlocks)
	{
		var lastProgress = new ProgressElement(0, 1);

		var processedBlocks = 0;
		encoder.Options.Progress = new SynchronousProgress<ProgressElement>(element =>
		{
			output.WriteLine($"Progress = {element.CurrentBlock} / {element.TotalBlocks}");

			Assert.Equal(++processedBlocks, element.CurrentBlock);
			lastProgress = element;
		});

		await using var ms = new MemoryStream();
		await encoder.EncodeToStreamAsync(testImage, ms);

		output.WriteLine("LastProgress = " + lastProgress);

		Assert.Equal(expectedTotalBlocks, lastProgress.TotalBlocks);
		Assert.Equal(1, lastProgress.Percentage);
	}

	private async Task ExecuteDecodeProgressReport(BcDecoder decoder, KtxFile image, int expectedTotalBlocks, bool singleMip)
	{
		var lastProgress = new ProgressElement(0, 1);

		var processedBlocks = 0;
		decoder.Options.Progress = new SynchronousProgress<ProgressElement>(element =>
		{
			output.WriteLine($"Progress = {element.CurrentBlock} / {element.TotalBlocks}");

			Assert.Equal(++processedBlocks, element.CurrentBlock);
			lastProgress = element;
		});

		if (singleMip)
		{
			await decoder.DecodeAsync(image);
		}
		else
		{
			await decoder.DecodeAllMipMapsAsync(image);
		}

		output.WriteLine("LastProgress = " + lastProgress);

		Assert.Equal(expectedTotalBlocks, lastProgress.TotalBlocks);
		Assert.Equal(1, lastProgress.Percentage);
	}

	private async Task ExecuteDecodeProgressReport(BcDecoder decoder, DdsFile image, int expectedTotalBlocks, bool singleMip)
	{
		var lastProgress = new ProgressElement(0, 1);

		var processedBlocks = 0;
		decoder.Options.Progress = new SynchronousProgress<ProgressElement>(element =>
		{
			output.WriteLine($"Progress = {element.CurrentBlock} / {element.TotalBlocks}");

			Assert.Equal(++processedBlocks, element.CurrentBlock);
			lastProgress = element;
		});

		if (singleMip)
		{
			await decoder.DecodeAsync(image);
		}
		else
		{
			await decoder.DecodeAllMipMapsAsync(image);
		}

		output.WriteLine("LastProgress = " + lastProgress);

		Assert.Equal(expectedTotalBlocks, lastProgress.TotalBlocks);
		Assert.Equal(1, lastProgress.Percentage);
	}

	[Fact]
	public async void DecodeProgressReportParallel()
	{
		var testImage = KtxLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = true }
		};

		var expectedTotal = 0;

		for (var i = 0; i < testImage.header.NumberOfMipmapLevels; i++)
		{
			expectedTotal += decoder.GetBlockCount((int)testImage.MipMaps[i].Width, (int)testImage.MipMaps[i].Height);
		}

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, false);
	}

	[Fact]
	public async void DecodeProgressReportNonParallel()
	{
		var testImage = KtxLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = false }
		};

		var expectedTotal = 0;

		for (var i = 0; i < testImage.header.NumberOfMipmapLevels; i++)
		{
			expectedTotal += decoder.GetBlockCount((int)testImage.MipMaps[i].Width, (int)testImage.MipMaps[i].Height);
		}

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, false);
	}

	[Fact]
	public async void DecodeProgressReportParallelOneMip()
	{
		var testImage = KtxLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = true }
		};

		var expectedTotal = decoder.GetBlockCount((int)testImage.MipMaps[0].Width, (int)testImage.MipMaps[0].Height);

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, true);
	}

	[Fact]
	public async void DecodeProgressReportNonParallelOneMip()
	{
		var testImage = KtxLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = false }
		};

		var expectedTotal = decoder.GetBlockCount((int)testImage.MipMaps[0].Width, (int)testImage.MipMaps[0].Height);

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, true);
	}

	[Fact]
	public async void DecodeProgressReportParallelDds()
	{
		var testImage = DdsLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = true }
		};

		var expectedTotal = 0;

		for (var i = 0; i < testImage.header.dwMipMapCount; i++)
		{
			expectedTotal += decoder.GetBlockCount((int)testImage.Faces[0].MipMaps[i].Width, (int)testImage.Faces[0].MipMaps[i].Height);
		}

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, false);
	}

	[Fact]
	public async void DecodeProgressReportNonParallelDds()
	{
		var testImage = DdsLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = false }
		};

		var expectedTotal = 0;

		for (var i = 0; i < testImage.header.dwMipMapCount; i++)
		{
			expectedTotal += decoder.GetBlockCount((int)testImage.Faces[0].MipMaps[i].Width, (int)testImage.Faces[0].MipMaps[i].Height);
		}

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, false);
	}

	[Fact]
	public async void DecodeProgressReportParallelOneMipDds()
	{
		var testImage = DdsLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = true }
		};

		var expectedTotal = decoder.GetBlockCount((int)testImage.Faces[0].MipMaps[0].Width, (int)testImage.Faces[0].MipMaps[0].Height);

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, true);
	}

	[Fact]
	public async void DecodeProgressReportNonParallelOneMipDds()
	{
		var testImage = DdsLoader.TestDecompressBc1;
		var decoder = new BcDecoder
		{
			Options = { IsParallel = false }
		};

		var expectedTotal = decoder.GetBlockCount((int)testImage.Faces[0].MipMaps[0].Width, (int)testImage.Faces[0].MipMaps[0].Height);

		await ExecuteDecodeProgressReport(decoder, testImage, expectedTotal, true);
	}

	[Fact]
	public async void EncodeProgressReportParallelKtx()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = true },
			OutputOptions = { FileFormat = OutputFileFormat.Ktx }
		};

		var expectedTotal = 0;

		for (var i = 0; i < encoder.CalculateNumberOfMipLevels(testImage); i++)
		{
			encoder.CalculateMipMapSize(testImage, i, out var mW, out var mH);
			expectedTotal += encoder.GetBlockCount(mW, mH);
		}

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportNonParallelKtx()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = false },
			OutputOptions = { FileFormat = OutputFileFormat.Ktx }
		};

		var expectedTotal = 0;

		for (var i = 0; i < encoder.CalculateNumberOfMipLevels(testImage); i++)
		{
			encoder.CalculateMipMapSize(testImage, i, out var mW, out var mH);
			expectedTotal += encoder.GetBlockCount(mW, mH);
		}

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportParallelOneMipKtx()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = true },
			OutputOptions = { MaxMipMapLevel = 1, FileFormat = OutputFileFormat.Ktx }
		};

		var expectedTotal = encoder.GetBlockCount(testImage.Width, testImage.Height);

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportNonParallelOneMipKtx()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = false },
			OutputOptions = { MaxMipMapLevel = 1, FileFormat = OutputFileFormat.Ktx }
		};

		var expectedTotal = encoder.GetBlockCount(testImage.Width, testImage.Height);

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportParallelDds()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = true },
			OutputOptions = { FileFormat = OutputFileFormat.Dds }
		};

		var expectedTotal = 0;

		for (var i = 0; i < encoder.CalculateNumberOfMipLevels(testImage); i++)
		{
			encoder.CalculateMipMapSize(testImage, i, out var mW, out var mH);
			expectedTotal += encoder.GetBlockCount(mW, mH);
		}

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportNonParallelDds()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = false },
			OutputOptions = { FileFormat = OutputFileFormat.Dds }
		};

		var expectedTotal = 0;

		for (var i = 0; i < encoder.CalculateNumberOfMipLevels(testImage); i++)
		{
			encoder.CalculateMipMapSize(testImage, i, out var mW, out var mH);
			expectedTotal += encoder.GetBlockCount(mW, mH);
		}

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportParallelOneMipDds()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = true },
			OutputOptions = { MaxMipMapLevel = 1, FileFormat = OutputFileFormat.Dds }
		};

		var expectedTotal = encoder.GetBlockCount(testImage.Width, testImage.Height);

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}

	[Fact]
	public async void EncodeProgressReportNonParallelOneMipDds()
	{
		var testImage = ImageLoader.TestBlur1;
		var encoder = new BcEncoder
		{
			Options = { IsParallel = false },
			OutputOptions = { MaxMipMapLevel = 1, FileFormat = OutputFileFormat.Dds }
		};

		var expectedTotal = encoder.GetBlockCount(testImage.Width, testImage.Height);

		await ExecuteEncodeProgressReport(encoder, testImage, expectedTotal);
	}
}

internal class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
	public void Report(T value)
	{
		handler(value);
	}
}
