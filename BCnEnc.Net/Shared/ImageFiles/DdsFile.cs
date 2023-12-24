using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BCnEncoder.Shared.ImageFiles;

/// <summary>
/// 
/// </summary>
public class DdsFile
{
	/// <summary>
	/// 
	/// </summary>
	public DdsHeader header;
	/// <summary>
	/// 
	/// </summary>
	public DdsHeaderDx10 dx10Header;
	/// <summary>
	/// 
	/// </summary>
	public List<DdsFace> Faces { get; } = new();

	/// <summary>
	/// 
	/// </summary>
	public DdsFile() { }
	/// <summary>
	/// 
	/// </summary>
	/// <param name="header"></param>
	public DdsFile(DdsHeader header)
	{
		this.header = header;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="header"></param>
	/// <param name="dx10Header"></param>
	public DdsFile(DdsHeader header, DdsHeaderDx10 dx10Header)
	{
		this.header = header;
		this.dx10Header = dx10Header;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	/// <exception cref="FormatException"></exception>
	public static DdsFile Load(Stream s)
	{
		using var br = new BinaryReader(s, Encoding.UTF8, true);
		var magic = br.ReadUInt32();
		if (magic != 0x20534444U)
		{
			throw new FormatException("The file does not contain a dds file.");
		}
		var header = br.ReadStruct<DdsHeader>();
		DdsHeaderDx10 dx10Header = default;
		if (header.dwSize != 124)
		{
			throw new FormatException("The file header contains invalid dwSize.");
		}

		var dx10Format = header.ddsPixelFormat.IsDxt10Format;

		DdsFile output;

		if (dx10Format)
		{
			dx10Header = br.ReadStruct<DdsHeaderDx10>();
			output = new DdsFile(header, dx10Header);
		}
		else
		{
			output = new DdsFile(header);
		}

		var mipMapCount = (header.dwCaps & HeaderCaps.DdscapsMipmap) != 0 ? header.dwMipMapCount : 1;
		var faceCount = (header.dwCaps2 & HeaderCaps2.Ddscaps2Cubemap) != 0 ? 6u : 1u;
		var width = header.dwWidth;
		var height = header.dwHeight;

		for (var face = 0; face < faceCount; face++)
		{
			var format = dx10Format ? dx10Header.dxgiFormat : header.ddsPixelFormat.DxgiFormat;
			var sizeInBytes = GetSizeInBytes(format, width, height);

			output.Faces.Add(new DdsFace(width, height, sizeInBytes, (int)mipMapCount));

			for (var mip = 0; mip < mipMapCount; mip++)
			{
				MipMapper.CalculateMipLevelSize(
					(int)header.dwWidth,
					(int)header.dwHeight,
					mip,
					out var mipWidth,
					out var mipHeight);

				if (mip > 0) //Calculate new byteSize
				{
					sizeInBytes = GetSizeInBytes(format, (uint)mipWidth, (uint)mipHeight);
				}

				var data = new byte[sizeInBytes];
				br.Read(data);
				output.Faces[face].MipMaps[mip] = new DdsMipMap(data, (uint)mipWidth, (uint)mipHeight);
			}
		}

		return output;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="outputStream"></param>
	/// <exception cref="InvalidOperationException"></exception>
	public void Write(Stream outputStream)
	{
		if (Faces.Count < 1 || Faces[0].MipMaps.Length < 1)
		{
			throw new InvalidOperationException("The DDS structure should have at least 1 mipmap level and 1 Face before writing to file.");
		}

		header.dwFlags |= HeaderFlags.Required;

		header.dwMipMapCount = (uint)Faces[0].MipMaps.Length;
		if (header.dwMipMapCount > 1) // MipMaps
		{
			header.dwCaps |= HeaderCaps.DdscapsMipmap | HeaderCaps.DdscapsComplex;
		}
		if (Faces.Count == 6) // CubeMap
		{
			header.dwCaps |= HeaderCaps.DdscapsComplex;
			header.dwCaps2 |= HeaderCaps2.Ddscaps2Cubemap |
			                  HeaderCaps2.Ddscaps2CubemapPositivex |
			                  HeaderCaps2.Ddscaps2CubemapNegativex |
			                  HeaderCaps2.Ddscaps2CubemapPositivey |
			                  HeaderCaps2.Ddscaps2CubemapNegativey |
			                  HeaderCaps2.Ddscaps2CubemapPositivez |
			                  HeaderCaps2.Ddscaps2CubemapNegativez;
		}

		header.dwWidth = Faces[0].Width;
		header.dwHeight = Faces[0].Height;

		if (Faces.Any(t => t.Width != header.dwWidth || t.Height != header.dwHeight))
		{
			throw new InvalidOperationException("Faces with different sizes are not supported.");
		}

		var faceCount = Faces.Count;
		var mipCount = (int)header.dwMipMapCount;

		using var bw = new BinaryWriter(outputStream, Encoding.UTF8, true);
		bw.Write(0x20534444U); // magic 'DDS '

		bw.WriteStruct(header);

		if (header.ddsPixelFormat.IsDxt10Format)
		{
			bw.WriteStruct(dx10Header);
		}

		for (var face = 0; face < faceCount; face++)
		{
			for (var mip = 0; mip < mipCount; mip++)
			{
				bw.Write(Faces[face].MipMaps[mip].Data);
			}
		}
	}

	private static uint GetSizeInBytes(DxgiFormat format, uint width, uint height)
	{
		uint sizeInBytes;
		if (format.IsCompressedFormat())
		{
			sizeInBytes = (uint)ImageToBlocks.CalculateNumOfBlocks((int)width, (int)height);
			sizeInBytes *= (uint)format.GetByteSize();
		}
		else
		{
			sizeInBytes = width * height;
			sizeInBytes = (uint)(sizeInBytes * format.GetByteSize());
		}

		return sizeInBytes;
	}
}

/// <summary>
/// 
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DdsHeader
{
	/// <summary>
	/// Has to be 124
	/// </summary>
	public uint dwSize;
	/// <summary>
	/// 
	/// </summary>
	public HeaderFlags dwFlags;
	/// <summary>
	/// 
	/// </summary>
	public uint dwHeight;
	/// <summary>
	/// 
	/// </summary>
	public uint dwWidth;
	/// <summary>
	/// 
	/// </summary>
	public uint dwPitchOrLinearSize;
	/// <summary>
	/// 
	/// </summary>
	public uint dwDepth;
	/// <summary>
	/// 
	/// </summary>
	public uint dwMipMapCount;
	/// <summary>
	/// 
	/// </summary>
	public fixed uint dwReserved1[11];
	/// <summary>
	/// 
	/// </summary>
	public DdsPixelFormat ddsPixelFormat;
	/// <summary>
	/// 
	/// </summary>
	public HeaderCaps dwCaps;
	/// <summary>
	/// 
	/// </summary>
	public HeaderCaps2 dwCaps2;
	/// <summary>
	/// 
	/// </summary>
	public uint dwCaps3;
	/// <summary>
	/// 
	/// </summary>
	public uint dwCaps4;
	/// <summary>
	/// 
	/// </summary>
	public uint dwReserved2;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <param name="format"></param>
	/// <param name="preferDxt10Header"></param>
	/// <returns></returns>
	public static (DdsHeader, DdsHeaderDx10) InitializeCompressed(int width, int height, DxgiFormat format, bool preferDxt10Header)
	{
		var header = new DdsHeader();
		var dxt10Header = new DdsHeaderDx10();

		header.dwSize = 124;
		header.dwFlags = HeaderFlags.Required;
		header.dwWidth = (uint)width;
		header.dwHeight = (uint)height;
		header.dwDepth = 1;
		header.dwMipMapCount = 1;
		header.dwCaps = HeaderCaps.DdscapsTexture;

		if (preferDxt10Header)
		{
			// ATC formats cannot be written to DXT10 header due to lack of a DxgiFormat enum
			switch (format)
			{
				case DxgiFormat.DxgiFormatAtcExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atc
					};
					break;

				case DxgiFormat.DxgiFormatAtcExplicitAlphaExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atci
					};
					break;

				case DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atca
					};
					break;

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
				case DxgiFormat.DxgiFormatR8G8B8A8Unorm:
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
				case DxgiFormat.DxgiFormatR8G8Unorm:
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
				case DxgiFormat.DxgiFormatR8Unorm:
				case DxgiFormat.DxgiFormatR8Uint:
				case DxgiFormat.DxgiFormatR8Snorm:
				case DxgiFormat.DxgiFormatR8Sint:
				case DxgiFormat.DxgiFormatA8Unorm:
				case DxgiFormat.DxgiFormatR1Unorm:
				case DxgiFormat.DxgiFormatR9G9B9E5Sharedexp:
				case DxgiFormat.DxgiFormatR8G8B8G8Unorm:
				case DxgiFormat.DxgiFormatG8R8G8B8Unorm:
				case DxgiFormat.DxgiFormatBc1Typeless:
				case DxgiFormat.DxgiFormatBc1Unorm:
				case DxgiFormat.DxgiFormatBc1UnormSrgb:
				case DxgiFormat.DxgiFormatBc2Typeless:
				case DxgiFormat.DxgiFormatBc2Unorm:
				case DxgiFormat.DxgiFormatBc2UnormSrgb:
				case DxgiFormat.DxgiFormatBc3Typeless:
				case DxgiFormat.DxgiFormatBc3Unorm:
				case DxgiFormat.DxgiFormatBc3UnormSrgb:
				case DxgiFormat.DxgiFormatBc4Typeless:
				case DxgiFormat.DxgiFormatBc4Unorm:
				case DxgiFormat.DxgiFormatBc4Snorm:
				case DxgiFormat.DxgiFormatBc5Typeless:
				case DxgiFormat.DxgiFormatBc5Unorm:
				case DxgiFormat.DxgiFormatBc5Snorm:
				case DxgiFormat.DxgiFormatB5G6R5Unorm:
				case DxgiFormat.DxgiFormatB5G5R5A1Unorm:
				case DxgiFormat.DxgiFormatB8G8R8A8Unorm:
				case DxgiFormat.DxgiFormatB8G8R8X8Unorm:
				case DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm:
				case DxgiFormat.DxgiFormatB8G8R8A8Typeless:
				case DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb:
				case DxgiFormat.DxgiFormatB8G8R8X8Typeless:
				case DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb:
				case DxgiFormat.DxgiFormatBc6HTypeless:
				case DxgiFormat.DxgiFormatBc6HUf16:
				case DxgiFormat.DxgiFormatBc6HSf16:
				case DxgiFormat.DxgiFormatBc7Typeless:
				case DxgiFormat.DxgiFormatBc7Unorm:
				case DxgiFormat.DxgiFormatBc7UnormSrgb:
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
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Dx10
					};
					dxt10Header.arraySize = 1;
					dxt10Header.dxgiFormat = format;
					dxt10Header.resourceDimension = D3D10ResourceDimension.D3D10ResourceDimensionTexture2D;
					break;
			}
		}
		else
		{
			switch (format)
			{
				case DxgiFormat.DxgiFormatBc1Unorm:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Dxt1
					};
					break;

				case DxgiFormat.DxgiFormatBc2Unorm:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Dxt3
					};
					break;

				case DxgiFormat.DxgiFormatBc3Unorm:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Dxt5
					};
					break;

				case DxgiFormat.DxgiFormatBc4Unorm:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Bc4U
					};
					break;

				case DxgiFormat.DxgiFormatBc5Unorm:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Ati2
					};
					break;

				case DxgiFormat.DxgiFormatAtcExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atc
					};
					break;

				case DxgiFormat.DxgiFormatAtcExplicitAlphaExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atci
					};
					break;

				case DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt:
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Atca
					};
					break;

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
				case DxgiFormat.DxgiFormatR8G8B8A8Unorm:
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
				case DxgiFormat.DxgiFormatR8G8Unorm:
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
				case DxgiFormat.DxgiFormatR8Unorm:
				case DxgiFormat.DxgiFormatR8Uint:
				case DxgiFormat.DxgiFormatR8Snorm:
				case DxgiFormat.DxgiFormatR8Sint:
				case DxgiFormat.DxgiFormatA8Unorm:
				case DxgiFormat.DxgiFormatR1Unorm:
				case DxgiFormat.DxgiFormatR9G9B9E5Sharedexp:
				case DxgiFormat.DxgiFormatR8G8B8G8Unorm:
				case DxgiFormat.DxgiFormatG8R8G8B8Unorm:
				case DxgiFormat.DxgiFormatBc1Typeless:
				case DxgiFormat.DxgiFormatBc1UnormSrgb:
				case DxgiFormat.DxgiFormatBc2Typeless:
				case DxgiFormat.DxgiFormatBc2UnormSrgb:
				case DxgiFormat.DxgiFormatBc3Typeless:
				case DxgiFormat.DxgiFormatBc3UnormSrgb:
				case DxgiFormat.DxgiFormatBc4Typeless:
				case DxgiFormat.DxgiFormatBc4Snorm:
				case DxgiFormat.DxgiFormatBc5Typeless:
				case DxgiFormat.DxgiFormatBc5Snorm:
				case DxgiFormat.DxgiFormatB5G6R5Unorm:
				case DxgiFormat.DxgiFormatB5G5R5A1Unorm:
				case DxgiFormat.DxgiFormatB8G8R8A8Unorm:
				case DxgiFormat.DxgiFormatB8G8R8X8Unorm:
				case DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm:
				case DxgiFormat.DxgiFormatB8G8R8A8Typeless:
				case DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb:
				case DxgiFormat.DxgiFormatB8G8R8X8Typeless:
				case DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb:
				case DxgiFormat.DxgiFormatBc6HTypeless:
				case DxgiFormat.DxgiFormatBc6HUf16:
				case DxgiFormat.DxgiFormatBc6HSf16:
				case DxgiFormat.DxgiFormatBc7Typeless:
				case DxgiFormat.DxgiFormatBc7Unorm:
				case DxgiFormat.DxgiFormatBc7UnormSrgb:
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
					header.ddsPixelFormat = new DdsPixelFormat
					{
						dwSize = 32,
						dwFlags = PixelFormatFlags.DdpfFourcc,
						dwFourCc = DdsPixelFormat.Dx10
					};
					dxt10Header.arraySize = 1;
					dxt10Header.dxgiFormat = format;
					dxt10Header.resourceDimension = D3D10ResourceDimension.D3D10ResourceDimensionTexture2D;
					break;
			}
		}

		return (header, dxt10Header);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <param name="format"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public static DdsHeader InitializeUncompressed(int width, int height, DxgiFormat format)
	{
		var header = new DdsHeader
		{
			dwSize = 124,
			dwFlags = HeaderFlags.Required | HeaderFlags.DdsdPitch,
			dwWidth = (uint)width,
			dwHeight = (uint)height,
			dwDepth = 1,
			dwMipMapCount = 1,
			dwCaps = HeaderCaps.DdscapsTexture
		};

		switch (format)
		{
			case DxgiFormat.DxgiFormatR8Unorm:
				header.ddsPixelFormat = new DdsPixelFormat()
				{
					dwSize = 32,
					dwFlags = PixelFormatFlags.DdpfLuminance,
					dwRgbBitCount = 8,
					dwRBitMask = 0xFF
				};
				header.dwPitchOrLinearSize = (uint)((width * 8 + 7) / 8);
				break;
			case DxgiFormat.DxgiFormatR8G8Unorm:
				header.ddsPixelFormat = new DdsPixelFormat()
				{
					dwSize = 32,
					dwFlags = PixelFormatFlags.DdpfLuminance | PixelFormatFlags.DdpfAlphaPixels,
					dwRgbBitCount = 16,
					dwRBitMask = 0xFF,
					dwGBitMask = 0xFF00
				};
				header.dwPitchOrLinearSize = (uint)((width * 16 + 7) / 8);
				break;
			case DxgiFormat.DxgiFormatR8G8B8A8Unorm:
				header.ddsPixelFormat = new DdsPixelFormat()
				{
					dwSize = 32,
					dwFlags = PixelFormatFlags.DdpfRgb | PixelFormatFlags.DdpfAlphaPixels,
					dwRgbBitCount = 32,
					dwRBitMask = 0xFF,
					dwGBitMask = 0xFF00,
					dwBBitMask = 0xFF0000,
					dwABitMask = 0xFF000000,
				};
				header.dwPitchOrLinearSize = (uint)((width * 32 + 7) / 8);
				break;
			case DxgiFormat.DxgiFormatB8G8R8A8Unorm:
				header.ddsPixelFormat = new DdsPixelFormat()
				{
					dwSize = 32,
					dwFlags = PixelFormatFlags.DdpfRgb | PixelFormatFlags.DdpfAlphaPixels,
					dwRgbBitCount = 32,
					dwRBitMask = 0xFF0000,
					dwGBitMask = 0xFF00,
					dwBBitMask = 0xFF,
					dwABitMask = 0xFF000000,
				};
				header.dwPitchOrLinearSize = (uint)((width * 32 + 7) / 8);
				break;
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
			case DxgiFormat.DxgiFormatBc1Typeless:
			case DxgiFormat.DxgiFormatBc1Unorm:
			case DxgiFormat.DxgiFormatBc1UnormSrgb:
			case DxgiFormat.DxgiFormatBc2Typeless:
			case DxgiFormat.DxgiFormatBc2Unorm:
			case DxgiFormat.DxgiFormatBc2UnormSrgb:
			case DxgiFormat.DxgiFormatBc3Typeless:
			case DxgiFormat.DxgiFormatBc3Unorm:
			case DxgiFormat.DxgiFormatBc3UnormSrgb:
			case DxgiFormat.DxgiFormatBc4Typeless:
			case DxgiFormat.DxgiFormatBc4Unorm:
			case DxgiFormat.DxgiFormatBc4Snorm:
			case DxgiFormat.DxgiFormatBc5Typeless:
			case DxgiFormat.DxgiFormatBc5Unorm:
			case DxgiFormat.DxgiFormatBc5Snorm:
			case DxgiFormat.DxgiFormatB5G6R5Unorm:
			case DxgiFormat.DxgiFormatB5G5R5A1Unorm:
			case DxgiFormat.DxgiFormatB8G8R8X8Unorm:
			case DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm:
			case DxgiFormat.DxgiFormatB8G8R8A8Typeless:
			case DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb:
			case DxgiFormat.DxgiFormatB8G8R8X8Typeless:
			case DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb:
			case DxgiFormat.DxgiFormatBc6HTypeless:
			case DxgiFormat.DxgiFormatBc6HUf16:
			case DxgiFormat.DxgiFormatBc6HSf16:
			case DxgiFormat.DxgiFormatBc7Typeless:
			case DxgiFormat.DxgiFormatBc7Unorm:
			case DxgiFormat.DxgiFormatBc7UnormSrgb:
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
			case DxgiFormat.DxgiFormatAtcExt:
			case DxgiFormat.DxgiFormatAtcExplicitAlphaExt:
			case DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt:
			default:
				throw new NotImplementedException("This Format is not implemented in this method");
		}

		return header;
	}
}

/// <summary>
/// 
/// </summary>
public struct DdsPixelFormat
{
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dx10 = MakeFourCc('D', 'X', '1', '0');
		
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dxt1 = MakeFourCc('D', 'X', 'T', '1');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dxt2 = MakeFourCc('D', 'X', 'T', '2');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dxt3 = MakeFourCc('D', 'X', 'T', '3');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dxt4 = MakeFourCc('D', 'X', 'T', '4');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Dxt5 = MakeFourCc('D', 'X', 'T', '5');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Ati1 = MakeFourCc('A', 'T', 'I', '1');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Ati2 = MakeFourCc('A', 'T', 'I', '2');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Atc  = MakeFourCc('A', 'T', 'C', ' ');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Atci = MakeFourCc('A', 'T', 'C', 'I');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Atca = MakeFourCc('A', 'T', 'C', 'A');

	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Bc4S = MakeFourCc('B', 'C', '4', 'S');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Bc4U = MakeFourCc('B', 'C', '4', 'U');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Bc5S = MakeFourCc('B', 'C', '5', 'S');
	/// <summary>
	/// 
	/// </summary>
	public static readonly uint Bc5U = MakeFourCc('B', 'C', '5', 'U');

	private static uint MakeFourCc(char c0, char c1, char c2, char c3)
	{
		uint result = c0;
		result |= (uint)c1 << 8;
		result |= (uint)c2 << 16;
		result |= (uint)c3 << 24;
		return result;
	}

	/// <summary>
	/// 
	/// </summary>
	public uint dwSize;
	/// <summary>
	/// 
	/// </summary>
	public PixelFormatFlags dwFlags;
	/// <summary>
	/// 
	/// </summary>
	public uint dwFourCc;
	/// <summary>
	/// 
	/// </summary>
	public uint dwRgbBitCount;
	/// <summary>
	/// 
	/// </summary>
	public uint dwRBitMask;
	/// <summary>
	/// 
	/// </summary>
	public uint dwGBitMask;
	/// <summary>
	/// 
	/// </summary>
	public uint dwBBitMask;
	/// <summary>
	/// 
	/// </summary>
	public uint dwABitMask;

	/// <summary>
	/// 
	/// </summary>
	public readonly DxgiFormat DxgiFormat
	{
		get
		{
			if (dwFlags.HasFlag(PixelFormatFlags.DdpfFourcc))
			{
				if (dwFourCc == Dxt1) return DxgiFormat.DxgiFormatBc1Unorm;
				if (dwFourCc == Dxt2 || dwFourCc == Dxt3) return DxgiFormat.DxgiFormatBc2Unorm;
				if (dwFourCc == Dxt4 || dwFourCc == Dxt5) return DxgiFormat.DxgiFormatBc3Unorm;
				if (dwFourCc == Ati1 || dwFourCc == Bc4S || dwFourCc == Bc4U) return DxgiFormat.DxgiFormatBc4Unorm;
				if (dwFourCc == Ati2 || dwFourCc == Bc5S || dwFourCc == Bc5U) return DxgiFormat.DxgiFormatBc5Unorm;
				if (dwFourCc == Atc) return DxgiFormat.DxgiFormatAtcExt;
				if (dwFourCc == Atci) return DxgiFormat.DxgiFormatAtcExplicitAlphaExt;
				if (dwFourCc == Atca) return DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt;
			}
			else
			{
				if (dwFlags.HasFlag(PixelFormatFlags.DdpfRgb)) // RGB/A
				{
					if (dwFlags.HasFlag(PixelFormatFlags.DdpfAlphaPixels)) //RGBA
					{
						if (dwRgbBitCount != 32) return DxgiFormat.DxgiFormatUnknown;
						switch (dwRBitMask)
						{
							case 0xff when dwGBitMask == 0xff00 && dwBBitMask == 0xff0000 &&
							               dwABitMask == 0xff000000:
								return DxgiFormat.DxgiFormatR8G8B8A8Unorm;
							case 0xff0000 when dwGBitMask == 0xff00 && dwBBitMask == 0xff &&
							                   dwABitMask == 0xff000000:
								return DxgiFormat.DxgiFormatB8G8R8A8Unorm;
						}
					}
					else //RGB
					{
						if (dwRgbBitCount != 32) return DxgiFormat.DxgiFormatUnknown;
						if (dwRBitMask == 0xff0000 && dwGBitMask == 0xff00 && dwBBitMask == 0xff)
						{
							return DxgiFormat.DxgiFormatB8G8R8X8Unorm;
						}
					}
				}
				else if (dwFlags.HasFlag(PixelFormatFlags.DdpfLuminance)) // R/RG
				{
					if (dwFlags.HasFlag(PixelFormatFlags.DdpfAlphaPixels)) // RG
					{
						if (dwRgbBitCount != 16) return DxgiFormat.DxgiFormatUnknown;
						if (dwRBitMask == 0xff && dwGBitMask == 0xff00)
						{
							return DxgiFormat.DxgiFormatR8G8Unorm;
						}
					}
					else // Luminance only
					{
						if (dwRgbBitCount != 8) return DxgiFormat.DxgiFormatUnknown;
						if (dwRBitMask == 0xff)
						{
							return DxgiFormat.DxgiFormatR8Unorm;
						}
					}
				}
			}
			return DxgiFormat.DxgiFormatUnknown;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public bool IsDxt10Format => (dwFlags & PixelFormatFlags.DdpfFourcc) == PixelFormatFlags.DdpfFourcc
	                             && dwFourCc == Dx10;
}

/// <summary>
/// 
/// </summary>
public struct DdsHeaderDx10
{
	/// <summary>
	/// 
	/// </summary>
	public DxgiFormat dxgiFormat;
	/// <summary>
	/// 
	/// </summary>
	public D3D10ResourceDimension resourceDimension;
	/// <summary>
	/// 
	/// </summary>
	public uint miscFlag;
	/// <summary>
	/// 
	/// </summary>
	public uint arraySize;
	/// <summary>
	/// 
	/// </summary>
	public uint miscFlags2;
}

/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
/// <param name="width"></param>
/// <param name="height"></param>
/// <param name="sizeInBytes"></param>
/// <param name="numMipMaps"></param>
public class DdsFace(uint width, uint height, uint sizeInBytes, int numMipMaps)
{
	/// <summary>
	/// 
	/// </summary>
	public uint Width { get; set; } = width;
	/// <summary>
	/// 
	/// </summary>
	public uint Height { get; set; } = height;
	/// <summary>
	/// 
	/// </summary>
	public uint SizeInBytes { get; } = sizeInBytes;
	/// <summary>
	/// 
	/// </summary>
	public DdsMipMap[] MipMaps { get; } = new DdsMipMap[numMipMaps];
}

/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
/// <param name="data"></param>
/// <param name="width"></param>
/// <param name="height"></param>
public class DdsMipMap(byte[] data, uint width, uint height)
{
	/// <summary>
	/// 
	/// </summary>
	public uint Width { get; set; } = width;
	/// <summary>
	/// 
	/// </summary>
	public uint Height { get; set; } = height;
	/// <summary>
	/// 
	/// </summary>
	public uint SizeInBytes { get; } = (uint)data.Length;
	/// <summary>
	/// 
	/// </summary>
	public byte[] Data { get; } = data;
}

/// <summary>
/// Flags to indicate which members contain valid data.
/// </summary>
[Flags]
public enum HeaderFlags : uint
{
	/// <summary>
	/// Required in every .dds file.
	/// </summary>
	DdsdCaps = 0x1,
	/// <summary>
	/// Required in every .dds file.
	/// </summary>
	DdsdHeight = 0x2,
	/// <summary>
	/// Required in every .dds file.
	/// </summary>
	DdsdWidth = 0x4,
	/// <summary>
	/// Required when pitch is provided for an uncompressed texture.
	/// </summary>
	DdsdPitch = 0x8,
	/// <summary>
	/// Required in every .dds file.
	/// </summary>
	DdsdPixelformat = 0x1000,
	/// <summary>
	/// Required in a mipmapped texture.
	/// </summary>
	DdsdMipmapcount = 0x20000,
	/// <summary>
	/// Required when pitch is provided for a compressed texture.
	/// </summary>
	DdsdLinearsize = 0x80000,
	/// <summary>
	/// Required in a depth texture.
	/// </summary>
	DdsdDepth = 0x800000,

	/// <summary>
	/// 
	/// </summary>
	Required = DdsdCaps | DdsdHeight | DdsdWidth | DdsdPixelformat
}

/// <summary>
/// Specifies the complexity of the surfaces stored.
/// </summary>
[Flags]
public enum HeaderCaps : uint
{
	/// <summary>
	/// Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture).
	/// </summary>
	DdscapsComplex = 0x8,
	/// <summary>
	/// Optional; should be used for a mipmap.
	/// </summary>
	DdscapsMipmap = 0x400000,
	/// <summary>
	/// Required
	/// </summary>
	DdscapsTexture = 0x1000
}

/// <summary>
/// Additional detail about the surfaces stored.
/// </summary>
[Flags]
public enum HeaderCaps2 : uint
{
	/// <summary>
	/// Required for a cube map.
	/// </summary>
	Ddscaps2Cubemap = 0x200,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapPositivex = 0x400,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapNegativex = 0x800,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapPositivey = 0x1000,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapNegativey = 0x2000,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapPositivez = 0x4000,
	/// <summary>
	/// Required when these surfaces are stored in a cube map.
	/// </summary>
	Ddscaps2CubemapNegativez = 0x8000,
	/// <summary>
	/// Required for a volume texture.
	/// </summary>
	Ddscaps2Volume = 0x200000
}

/// <summary>
/// 
/// </summary>
[Flags]
public enum PixelFormatFlags : uint
{
	/// <summary>
	/// Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
	/// </summary>
	DdpfAlphaPixels = 0x1,
	/// <summary>
	/// Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
	/// </summary>
	DdpfAlpha = 0x2,
	/// <summary>
	/// Texture contains compressed RGB data; dwFourCC contains valid data.
	/// </summary>
	DdpfFourcc = 0x4,
	/// <summary>
	/// Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask) contain valid data.
	/// </summary>
	DdpfRgb = 0x40,
	/// <summary>
	/// Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
	/// </summary>
	DdpfYuv = 0x200,
	/// <summary>
	/// Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file.
	/// </summary>
	DdpfLuminance = 0x20000
}

/// <summary>
/// 
/// </summary>
public enum D3D10ResourceDimension : uint
{
	/// <summary>
	/// 
	/// </summary>
	D3D10ResourceDimensionUnknown,
	/// <summary>
	/// 
	/// </summary>
	D3D10ResourceDimensionBuffer,
	/// <summary>
	/// 
	/// </summary>
	D3D10ResourceDimensionTexture1D,
	/// <summary>
	/// 
	/// </summary>
	D3D10ResourceDimensionTexture2D,
	/// <summary>
	/// 
	/// </summary>
	D3D10ResourceDimensionTexture3D
};

/// <summary>
/// 
/// </summary>
public enum DxgiFormat : uint
{
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatUnknown,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32A32Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32A32Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32A32Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32A32Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32B32Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16B16A16Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G32Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32G8X24Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatD32FloatS8X24Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32FloatX8X24Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatX32TypelessG8X24Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR10G10B10A2Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR10G10B10A2Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR10G10B10A2Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR11G11B10Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8A8Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16G16Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatD32Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR32Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR24G8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatD24UnormS8Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR24UnormX8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatX24TypelessG8Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Float,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatD16Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR16Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8Uint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8Sint,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatA8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR1Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR9G9B9E5Sharedexp,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR8G8B8G8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatG8R8G8B8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc1Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc1Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc1UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc2Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc2Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc2UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc3Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc3Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc3UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc4Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc4Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc4Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc5Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc5Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc5Snorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB5G6R5Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB5G5R5A1Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8A8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8X8Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatR10G10B10XrBiasA2Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8A8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8A8UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8X8Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB8G8R8X8UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc6HTypeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc6HUf16,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc6HSf16,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc7Typeless,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc7Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatBc7UnormSrgb,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatAyuv,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatY410,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatY416,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatNv12,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatP010,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatP016,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormat420Opaque,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatYuy2,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatY210,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatY216,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatNv11,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatAi44,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatIa44,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatP8,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatA8P8,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatB4G4R4A4Unorm,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatP208,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatV208,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatV408,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatForceUint,

	// Added here due to lack of an official value
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatAtcExt = 300,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatAtcExplicitAlphaExt,
	/// <summary>
	/// 
	/// </summary>
	DxgiFormatAtcInterpolatedAlphaExt
};

/// <summary>
/// 
/// </summary>
public static class DxgiFormatExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="format"></param>
	/// <returns></returns>
	public static int GetByteSize(this DxgiFormat format)
	{
		return format switch
		{
			DxgiFormat.DxgiFormatUnknown => 4,
			DxgiFormat.DxgiFormatR32G32B32A32Typeless => 4 * 4,
			DxgiFormat.DxgiFormatR32G32B32A32Float => 4 * 4,
			DxgiFormat.DxgiFormatR32G32B32A32Uint => 4 * 4,
			DxgiFormat.DxgiFormatR32G32B32A32Sint => 4 * 4,
			DxgiFormat.DxgiFormatR32G32B32Typeless => 4 * 3,
			DxgiFormat.DxgiFormatR32G32B32Float => 4 * 3,
			DxgiFormat.DxgiFormatR32G32B32Uint => 4 * 3,
			DxgiFormat.DxgiFormatR32G32B32Sint => 4 * 3,
			DxgiFormat.DxgiFormatR16G16B16A16Typeless => 4 * 2,
			DxgiFormat.DxgiFormatR16G16B16A16Float => 4 * 2,
			DxgiFormat.DxgiFormatR16G16B16A16Unorm => 4 * 2,
			DxgiFormat.DxgiFormatR16G16B16A16Uint => 4 * 2,
			DxgiFormat.DxgiFormatR16G16B16A16Snorm => 4 * 2,
			DxgiFormat.DxgiFormatR16G16B16A16Sint => 4 * 2,
			DxgiFormat.DxgiFormatR32G32Typeless => 4 * 2,
			DxgiFormat.DxgiFormatR32G32Float => 4 * 2,
			DxgiFormat.DxgiFormatR32G32Uint => 4 * 2,
			DxgiFormat.DxgiFormatR32G32Sint => 4 * 2,
			DxgiFormat.DxgiFormatR32G8X24Typeless => 4 * 2,
			DxgiFormat.DxgiFormatD32FloatS8X24Uint => 4,
			DxgiFormat.DxgiFormatR32FloatX8X24Typeless => 4,
			DxgiFormat.DxgiFormatX32TypelessG8X24Uint => 4,
			DxgiFormat.DxgiFormatR10G10B10A2Typeless => 4,
			DxgiFormat.DxgiFormatR10G10B10A2Unorm => 4,
			DxgiFormat.DxgiFormatR10G10B10A2Uint => 4,
			DxgiFormat.DxgiFormatR11G11B10Float => 4,
			DxgiFormat.DxgiFormatR8G8B8A8Typeless => 4,
			DxgiFormat.DxgiFormatR8G8B8A8Unorm => 4,
			DxgiFormat.DxgiFormatR8G8B8A8UnormSrgb => 4,
			DxgiFormat.DxgiFormatR8G8B8A8Uint => 4,
			DxgiFormat.DxgiFormatR8G8B8A8Snorm => 4,
			DxgiFormat.DxgiFormatR8G8B8A8Sint => 4,
			DxgiFormat.DxgiFormatR16G16Typeless => 4,
			DxgiFormat.DxgiFormatR16G16Float => 4,
			DxgiFormat.DxgiFormatR16G16Unorm => 4,
			DxgiFormat.DxgiFormatR16G16Uint => 4,
			DxgiFormat.DxgiFormatR16G16Snorm => 4,
			DxgiFormat.DxgiFormatR16G16Sint => 4,
			DxgiFormat.DxgiFormatR32Typeless => 4,
			DxgiFormat.DxgiFormatD32Float => 4,
			DxgiFormat.DxgiFormatR32Float => 4,
			DxgiFormat.DxgiFormatR32Uint => 4,
			DxgiFormat.DxgiFormatR32Sint => 4,
			DxgiFormat.DxgiFormatR24G8Typeless => 4,
			DxgiFormat.DxgiFormatD24UnormS8Uint => 4,
			DxgiFormat.DxgiFormatR24UnormX8Typeless => 4,
			DxgiFormat.DxgiFormatX24TypelessG8Uint => 4,
			DxgiFormat.DxgiFormatR8G8Typeless => 2,
			DxgiFormat.DxgiFormatR8G8Unorm => 2,
			DxgiFormat.DxgiFormatR8G8Uint => 2,
			DxgiFormat.DxgiFormatR8G8Snorm => 2,
			DxgiFormat.DxgiFormatR8G8Sint => 2,
			DxgiFormat.DxgiFormatR16Typeless => 2,
			DxgiFormat.DxgiFormatR16Float => 2,
			DxgiFormat.DxgiFormatD16Unorm => 2,
			DxgiFormat.DxgiFormatR16Unorm => 2,
			DxgiFormat.DxgiFormatR16Uint => 2,
			DxgiFormat.DxgiFormatR16Snorm => 2,
			DxgiFormat.DxgiFormatR16Sint => 2,
			DxgiFormat.DxgiFormatR8Typeless => 1,
			DxgiFormat.DxgiFormatR8Unorm => 1,
			DxgiFormat.DxgiFormatR8Uint => 1,
			DxgiFormat.DxgiFormatR8Snorm => 1,
			DxgiFormat.DxgiFormatR8Sint => 1,
			DxgiFormat.DxgiFormatA8Unorm => 1,
			DxgiFormat.DxgiFormatR1Unorm => 1,
			DxgiFormat.DxgiFormatR9G9B9E5Sharedexp => 4,
			DxgiFormat.DxgiFormatR8G8B8G8Unorm => 4,
			DxgiFormat.DxgiFormatG8R8G8B8Unorm => 4,
			DxgiFormat.DxgiFormatBc1Typeless => 8,
			DxgiFormat.DxgiFormatBc1Unorm => 8,
			DxgiFormat.DxgiFormatBc1UnormSrgb => 8,
			DxgiFormat.DxgiFormatBc2Typeless => 16,
			DxgiFormat.DxgiFormatBc2Unorm => 16,
			DxgiFormat.DxgiFormatBc2UnormSrgb => 16,
			DxgiFormat.DxgiFormatBc3Typeless => 16,
			DxgiFormat.DxgiFormatBc3Unorm => 16,
			DxgiFormat.DxgiFormatBc3UnormSrgb => 16,
			DxgiFormat.DxgiFormatBc4Typeless => 8,
			DxgiFormat.DxgiFormatBc4Unorm => 8,
			DxgiFormat.DxgiFormatBc4Snorm => 8,
			DxgiFormat.DxgiFormatBc5Typeless => 16,
			DxgiFormat.DxgiFormatBc5Unorm => 16,
			DxgiFormat.DxgiFormatBc5Snorm => 16,
			DxgiFormat.DxgiFormatB5G6R5Unorm => 2,
			DxgiFormat.DxgiFormatB5G5R5A1Unorm => 2,
			DxgiFormat.DxgiFormatB8G8R8A8Unorm => 4,
			DxgiFormat.DxgiFormatB8G8R8X8Unorm => 4,
			DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm => 4,
			DxgiFormat.DxgiFormatB8G8R8A8Typeless => 4,
			DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb => 4,
			DxgiFormat.DxgiFormatB8G8R8X8Typeless => 4,
			DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb => 4,
			DxgiFormat.DxgiFormatBc6HTypeless => 16,
			DxgiFormat.DxgiFormatBc6HUf16 => 16,
			DxgiFormat.DxgiFormatBc6HSf16 => 16,
			DxgiFormat.DxgiFormatBc7Typeless => 16,
			DxgiFormat.DxgiFormatBc7Unorm => 16,
			DxgiFormat.DxgiFormatBc7UnormSrgb => 16,
			DxgiFormat.DxgiFormatP8 => 1,
			DxgiFormat.DxgiFormatA8P8 => 2,
			DxgiFormat.DxgiFormatB4G4R4A4Unorm => 2,
			DxgiFormat.DxgiFormatAtcExt => 8,
			DxgiFormat.DxgiFormatAtcExplicitAlphaExt => 16,
			DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt => 16,
			_ => 4
		};
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="format"></param>
	/// <returns></returns>
	public static bool IsCompressedFormat(this DxgiFormat format)
	{
		return format switch
		{
			DxgiFormat.DxgiFormatBc1Typeless => true,
			DxgiFormat.DxgiFormatBc1Unorm => true,
			DxgiFormat.DxgiFormatBc1UnormSrgb => true,
			DxgiFormat.DxgiFormatBc2Typeless => true,
			DxgiFormat.DxgiFormatBc2Unorm => true,
			DxgiFormat.DxgiFormatBc2UnormSrgb => true,
			DxgiFormat.DxgiFormatBc3Typeless => true,
			DxgiFormat.DxgiFormatBc3Unorm => true,
			DxgiFormat.DxgiFormatBc3UnormSrgb => true,
			DxgiFormat.DxgiFormatBc4Typeless => true,
			DxgiFormat.DxgiFormatBc4Unorm => true,
			DxgiFormat.DxgiFormatBc4Snorm => true,
			DxgiFormat.DxgiFormatBc5Typeless => true,
			DxgiFormat.DxgiFormatBc5Unorm => true,
			DxgiFormat.DxgiFormatBc5Snorm => true,
			DxgiFormat.DxgiFormatBc6HTypeless => true,
			DxgiFormat.DxgiFormatBc6HUf16 => true,
			DxgiFormat.DxgiFormatBc6HSf16 => true,
			DxgiFormat.DxgiFormatBc7Typeless => true,
			DxgiFormat.DxgiFormatBc7Unorm => true,
			DxgiFormat.DxgiFormatBc7UnormSrgb => true,
			DxgiFormat.DxgiFormatAtcExt => true,
			DxgiFormat.DxgiFormatAtcExplicitAlphaExt => true,
			DxgiFormat.DxgiFormatAtcInterpolatedAlphaExt => true,
			DxgiFormat.DxgiFormatUnknown => false,
			DxgiFormat.DxgiFormatR32G32B32A32Typeless => false,
			DxgiFormat.DxgiFormatR32G32B32A32Float => false,
			DxgiFormat.DxgiFormatR32G32B32A32Uint => false,
			DxgiFormat.DxgiFormatR32G32B32A32Sint => false,
			DxgiFormat.DxgiFormatR32G32B32Typeless => false,
			DxgiFormat.DxgiFormatR32G32B32Float => false,
			DxgiFormat.DxgiFormatR32G32B32Uint => false,
			DxgiFormat.DxgiFormatR32G32B32Sint => false,
			DxgiFormat.DxgiFormatR16G16B16A16Typeless => false,
			DxgiFormat.DxgiFormatR16G16B16A16Float => false,
			DxgiFormat.DxgiFormatR16G16B16A16Unorm => false,
			DxgiFormat.DxgiFormatR16G16B16A16Uint => false,
			DxgiFormat.DxgiFormatR16G16B16A16Snorm => false,
			DxgiFormat.DxgiFormatR16G16B16A16Sint => false,
			DxgiFormat.DxgiFormatR32G32Typeless => false,
			DxgiFormat.DxgiFormatR32G32Float => false,
			DxgiFormat.DxgiFormatR32G32Uint => false,
			DxgiFormat.DxgiFormatR32G32Sint => false,
			DxgiFormat.DxgiFormatR32G8X24Typeless => false,
			DxgiFormat.DxgiFormatD32FloatS8X24Uint => false,
			DxgiFormat.DxgiFormatR32FloatX8X24Typeless => false,
			DxgiFormat.DxgiFormatX32TypelessG8X24Uint => false,
			DxgiFormat.DxgiFormatR10G10B10A2Typeless => false,
			DxgiFormat.DxgiFormatR10G10B10A2Unorm => false,
			DxgiFormat.DxgiFormatR10G10B10A2Uint => false,
			DxgiFormat.DxgiFormatR11G11B10Float => false,
			DxgiFormat.DxgiFormatR8G8B8A8Typeless => false,
			DxgiFormat.DxgiFormatR8G8B8A8Unorm => false,
			DxgiFormat.DxgiFormatR8G8B8A8UnormSrgb => false,
			DxgiFormat.DxgiFormatR8G8B8A8Uint => false,
			DxgiFormat.DxgiFormatR8G8B8A8Snorm => false,
			DxgiFormat.DxgiFormatR8G8B8A8Sint => false,
			DxgiFormat.DxgiFormatR16G16Typeless => false,
			DxgiFormat.DxgiFormatR16G16Float => false,
			DxgiFormat.DxgiFormatR16G16Unorm => false,
			DxgiFormat.DxgiFormatR16G16Uint => false,
			DxgiFormat.DxgiFormatR16G16Snorm => false,
			DxgiFormat.DxgiFormatR16G16Sint => false,
			DxgiFormat.DxgiFormatR32Typeless => false,
			DxgiFormat.DxgiFormatD32Float => false,
			DxgiFormat.DxgiFormatR32Float => false,
			DxgiFormat.DxgiFormatR32Uint => false,
			DxgiFormat.DxgiFormatR32Sint => false,
			DxgiFormat.DxgiFormatR24G8Typeless => false,
			DxgiFormat.DxgiFormatD24UnormS8Uint => false,
			DxgiFormat.DxgiFormatR24UnormX8Typeless => false,
			DxgiFormat.DxgiFormatX24TypelessG8Uint => false,
			DxgiFormat.DxgiFormatR8G8Typeless => false,
			DxgiFormat.DxgiFormatR8G8Unorm => false,
			DxgiFormat.DxgiFormatR8G8Uint => false,
			DxgiFormat.DxgiFormatR8G8Snorm => false,
			DxgiFormat.DxgiFormatR8G8Sint => false,
			DxgiFormat.DxgiFormatR16Typeless => false,
			DxgiFormat.DxgiFormatR16Float => false,
			DxgiFormat.DxgiFormatD16Unorm => false,
			DxgiFormat.DxgiFormatR16Unorm => false,
			DxgiFormat.DxgiFormatR16Uint => false,
			DxgiFormat.DxgiFormatR16Snorm => false,
			DxgiFormat.DxgiFormatR16Sint => false,
			DxgiFormat.DxgiFormatR8Typeless => false,
			DxgiFormat.DxgiFormatR8Unorm => false,
			DxgiFormat.DxgiFormatR8Uint => false,
			DxgiFormat.DxgiFormatR8Snorm => false,
			DxgiFormat.DxgiFormatR8Sint => false,
			DxgiFormat.DxgiFormatA8Unorm => false,
			DxgiFormat.DxgiFormatR1Unorm => false,
			DxgiFormat.DxgiFormatR9G9B9E5Sharedexp => false,
			DxgiFormat.DxgiFormatR8G8B8G8Unorm => false,
			DxgiFormat.DxgiFormatG8R8G8B8Unorm => false,
			DxgiFormat.DxgiFormatB5G6R5Unorm => false,
			DxgiFormat.DxgiFormatB5G5R5A1Unorm => false,
			DxgiFormat.DxgiFormatB8G8R8A8Unorm => false,
			DxgiFormat.DxgiFormatB8G8R8X8Unorm => false,
			DxgiFormat.DxgiFormatR10G10B10XrBiasA2Unorm => false,
			DxgiFormat.DxgiFormatB8G8R8A8Typeless => false,
			DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb => false,
			DxgiFormat.DxgiFormatB8G8R8X8Typeless => false,
			DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb => false,
			DxgiFormat.DxgiFormatAyuv => false,
			DxgiFormat.DxgiFormatY410 => false,
			DxgiFormat.DxgiFormatY416 => false,
			DxgiFormat.DxgiFormatNv12 => false,
			DxgiFormat.DxgiFormatP010 => false,
			DxgiFormat.DxgiFormatP016 => false,
			DxgiFormat.DxgiFormat420Opaque => false,
			DxgiFormat.DxgiFormatYuy2 => false,
			DxgiFormat.DxgiFormatY210 => false,
			DxgiFormat.DxgiFormatY216 => false,
			DxgiFormat.DxgiFormatNv11 => false,
			DxgiFormat.DxgiFormatAi44 => false,
			DxgiFormat.DxgiFormatIa44 => false,
			DxgiFormat.DxgiFormatP8 => false,
			DxgiFormat.DxgiFormatA8P8 => false,
			DxgiFormat.DxgiFormatB4G4R4A4Unorm => false,
			DxgiFormat.DxgiFormatP208 => false,
			DxgiFormat.DxgiFormatV208 => false,
			DxgiFormat.DxgiFormatV408 => false,
			DxgiFormat.DxgiFormatForceUint => false,
			_ => false
		};
	}
}
