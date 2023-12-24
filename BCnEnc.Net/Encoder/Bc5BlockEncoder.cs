using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

namespace BCnEncoder.Encoder;

internal class Bc5BlockEncoder(ColorComponent component1, ColorComponent component2)
	: BaseBcBlockEncoder<Bc5Block, RawBlock4X4Rgba32>
{
	private readonly Bc4ComponentBlockEncoder redBlockEncoder = new(component1);
	private readonly Bc4ComponentBlockEncoder greenBlockEncoder = new(component2);

	public override Bc5Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
	{
		return new Bc5Block
		{
			redBlock = redBlockEncoder.EncodeBlock(block, quality),
			greenBlock = greenBlockEncoder.EncodeBlock(block, quality)
		};
	}

	public override GlInternalFormat GetInternalFormat()
	{
		return GlInternalFormat.GlCompressedRedGreenRgtc2Ext;
	}

	public override GlFormat GetBaseInternalFormat()
	{
		return GlFormat.GlRg;
	}

	public override DxgiFormat GetDxgiFormat()
	{
		return DxgiFormat.DxgiFormatBc5Unorm;
	}
}
