[![Nuget](https://img.shields.io/nuget/v/BCnEncoder.Net)](https://www.nuget.org/packages/BCnEncoder.Net/)
![Tests](https://github.com/Nominom/BCnEncoder.NET/workflows/Tests/badge.svg)

# BCnEncoder.NET
A Cross-platform BCn / DXT encoding libary for .NET

# What is it?
BCnEncoder.NET is a library for compressing rgba images to different block-compressed formats. It has no native dependencies ~~and is .NET Standard 2.1 compatible~~.
Removed .NET Framkework and .NET 2.1 support and added .NET 8 support.

Supported formats are:
 - Raw unsigned byte R, RG, RGB and RGBA formats
 - BC1 (S3TC DXT1)
 - BC2 (S3TC DXT3)
 - BC3 (S3TC DXT5)
 - BC4 (RGTC1)
 - BC5 (RGTC2)
 - BC6 (BPTC_FLOAT)
 - BC7 (BPTC)

# Current state
The current state of this library is in development but quite usable. I'm planning on implementing support for more codecs and 
different algorithms. The current version is capable of encoding and decoding BC1-BC7 images in both KTX or DDS formats.

Please note, that the API might change between versions.
# Dependencies
Current dependencies are:
* [Microsoft.Toolkit.HighPerformance](https://github.com/windows-toolkit/WindowsCommunityToolkit) licensed under the [MIT](https://opensource.org/licenses/MIT) license for Span2D and Memory2D types.

# Image library extensions
This library has extension packages available for the following image libraries:

[ImageSharp](https://www.nuget.org/packages/BCnEncoder.Net.ImageSharp/)

The extension packages provide extension methods for ease of use with the image library.

# Upgrading to 2.0

If you're upgrading from 1.X.X to version 2, expect some of your exsting code to be broken. ImageSharp was removed as a core dependency in version 2.0, so the code will no longer work with ImageSharp's Image types by default. You can install the extension package for ImageSharp to continue using this library easily with ImageSharp apis.

# API
The below examples are using the ImageSharp extension package. For more detailed usage examples, you can go look at the unit tests.

Remember add the following usings to the top of the file:
```CSharp
using BCnEncoder.Encoder;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
```

Here's an example on how to encode a png image to BC1 without alpha, and save it to a file.
```CSharp
using Image<Rgba32> image = Image.Load<Rgba32>("example.png");

BcEncoder encoder = new BcEncoder();

encoder.OutputOptions.GenerateMipMaps = true;
encoder.OutputOptions.Quality = CompressionQuality.Balanced;
encoder.OutputOptions.Format = CompressionFormat.Bc1;
encoder.OutputOptions.FileFormat = OutputFileFormat.Ktx; //Change to Dds for a dds file.

using FileStream fs = File.OpenWrite("example.ktx");
encoder.EncodeToStream(image, fs);
```

And how to decode a compressed image from a KTX file and save it to png format.
```CSharp
using FileStream fs = File.OpenRead("compressed_bc1.ktx");

BcDecoder decoder = new BcDecoder();
using Image<Rgba32> image = decoder.DecodeToImageRgba32(fs);

using FileStream outFs = File.OpenWrite("decoding_test_bc1.png");
image.SaveAsPng(outFs);
```

How to encode an HDR image with BC6H. 
(HdrImage class reads and writes Radiance HDR files. This class is experimental and subject to be removed)
```CSharp
HdrImage image = HdrImage.Read("example.hdr");
			
BcEncoder encoder = new BcEncoder();

encoder.OutputOptions.GenerateMipMaps = true;
encoder.OutputOptions.Quality = CompressionQuality.Balanced;


