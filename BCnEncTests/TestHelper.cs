﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BCnEnc.Net.Decoder;
using BCnEnc.Net.Encoder;
using BCnEnc.Net.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Xunit.Abstractions;

namespace BCnEncTests
{
	public static class TestHelper
	{
		public static float DecodeCheckPSNR(string filename, Image<Rgba32> original) {
			using FileStream fs = File.OpenRead(filename);
			var ktx = KtxFile.Load(fs);
			var decoder = new BcDecoder();
			using var img = decoder.Decode(ktx);
			var pixels = original.GetPixelSpan();
			var pixels2 = img.GetPixelSpan();

			return ImageQuality.PeakSignalToNoiseRatio(pixels, pixels2, false);
		}

		public static void ExecuteEncodingTest(Image<Rgba32> image, CompressionFormat format, EncodingQuality quality, string filename, ITestOutputHelper output) {
			BcEncoder encoder = new BcEncoder();
			encoder.OutputOptions.quality = quality;
			encoder.OutputOptions.generateMipMaps = true;
			encoder.OutputOptions.format = format;

			using FileStream fs = File.OpenWrite(filename);
			encoder.Encode(image, fs);
			fs.Close();
			var psnr = TestHelper.DecodeCheckPSNR(filename, image);
			output.WriteLine("PSNR: " + psnr + "db");
		}
	}
}