using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.NET.ImageSharp;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BCnEncTests;

public class Examples
{
		
	public void EncodeImageSharp()
	{
		using var image = Image.Load<Rgba32>("example.png");

		var encoder = new BcEncoder
		{
			OutputOptions =
			{
				GenerateMipMaps = true,
				Quality = CompressionQuality.Balanced,
				Format = CompressionFormat.Bc1,
				FileFormat = OutputFileFormat.Ktx //Change to Dds for a dds file.
			}
		};

		using var fs = File.OpenWrite("example.ktx");
		encoder.EncodeToStream(image, fs);
	}
		
	public void DecodeImageSharp()
	{
		using var fs = File.OpenRead("compressed_bc1.ktx");

		var decoder = new BcDecoder();
		using var image = decoder.DecodeToImageRgba32(fs);
			
		using var outFs = File.OpenWrite("decoding_test_bc1.png");
		image.SaveAsPng(outFs);
	}

	public void EncodeHdr()
	{
		var image = HdrImage.Read("example.hdr");
			

		var encoder = new BcEncoder
		{
			OutputOptions =
			{
				GenerateMipMaps = true,
				Quality = CompressionQuality.Balanced,
				Format = CompressionFormat.Bc6U,
				FileFormat = OutputFileFormat.Ktx //Change to Dds for a dds file.
			}
		};

		using var fs = File.OpenWrite("example.ktx");
		encoder.EncodeToStreamHdr(image.PixelMemory, fs);
	}

	public void DecodeHdr()
	{
		using var fs = File.OpenRead("compressed_bc6.ktx");

		var decoder = new BcDecoder();
		var pixels = decoder.DecodeHdr2D(fs);

		var image = new HdrImage(pixels.Span);

		using var outFs = File.OpenWrite("decoded.hdr");
		image.Write(outFs);
	}
}
