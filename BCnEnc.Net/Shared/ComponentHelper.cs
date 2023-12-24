using System;

namespace BCnEncoder.Shared;

internal static class ComponentHelper
{
	public static ColorRgba32 ComponentToColor(ColorComponent component, byte componentValue)
	{
		return component switch
		{
			ColorComponent.R => new ColorRgba32(componentValue, 0, 0, 255),
			ColorComponent.G => new ColorRgba32(0, componentValue, 0, 255),
			ColorComponent.B => new ColorRgba32(0, 0, componentValue, 255),
			ColorComponent.A => new ColorRgba32(0, 0, 0, componentValue),
			ColorComponent.Luminance => new ColorRgba32(componentValue, componentValue, componentValue, 255),
			_ => throw new InvalidOperationException("Unsupported component.")
		};
	}

	public static ColorRgba32 ComponentToColor(ColorRgba32 existingColor, ColorComponent component, byte componentValue)
	{
		switch (component)
		{
			case ColorComponent.R:
				existingColor.r = componentValue;
				break;

			case ColorComponent.G:
				existingColor.g = componentValue;
				break;

			case ColorComponent.B:
				existingColor.b = componentValue;
				break;

			case ColorComponent.A:
				existingColor.a = componentValue;
				break;

			case ColorComponent.Luminance:
				existingColor.r = existingColor.g = existingColor.b = componentValue;
				break;

			default:
				throw new InvalidOperationException("Unsupported component.");
		}

		return existingColor;
	}

	public static byte ColorToComponent(ColorRgba32 color, ColorComponent component)
	{
		return component switch
		{
			ColorComponent.R => color.r,
			ColorComponent.G => color.g,
			ColorComponent.B => color.b,
			ColorComponent.A => color.a,
			ColorComponent.Luminance => (byte)(new ColorYCbCr(color).y * 255),
			_ => throw new InvalidOperationException("Unsupported component.")
		};
	}
}
