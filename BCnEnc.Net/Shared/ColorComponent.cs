namespace BCnEncoder.Shared;

/// <summary>
/// The component to take from colors for BC4 and BC5.
/// </summary>
public enum ColorComponent
{
	/// <summary>
	/// The red component of an Rgba32 color.
	/// </summary>
	R,

	/// <summary>
	/// The green component of an Rgba32 color.
	/// </summary>
	G,

	/// <summary>
	/// The blue component of an Rgba32 color.
	/// </summary>
	B,

	/// <summary>
	/// The alpha component of an Rgba32 color.
	/// </summary>
	A,

	/// <summary>
	/// Use the color's luminance value as the component.
	/// </summary>
	Luminance
}
