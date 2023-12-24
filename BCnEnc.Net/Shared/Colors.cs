using System;
using System.Numerics;

namespace BCnEncoder.Shared;

/// <summary>
/// 
/// </summary>
public struct ColorRgba32 : IEquatable<ColorRgba32>
{
	/// <summary>
	/// 
	/// </summary>
	public byte r, g, b, a;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <param name="a"></param>
	public ColorRgba32(byte r, byte g, byte b, byte a)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	public ColorRgba32(byte r, byte g, byte b)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = 255;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public readonly bool Equals(ColorRgba32 other)
	{
		return r == other.r && g == other.g && b == other.b && a == other.a;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public readonly override bool Equals(object obj)
	{
		return obj is ColorRgba32 other && Equals(other);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly override int GetHashCode()
	{
		return HashCode.Combine(r, g, b, a);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(ColorRgba32 left, ColorRgba32 right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(ColorRgba32 left, ColorRgba32 right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgba32 operator +(ColorRgba32 left, ColorRgba32 right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r + right.r),
			ByteHelper.ClampToByte(left.g + right.g),
			ByteHelper.ClampToByte(left.b + right.b),
			ByteHelper.ClampToByte(left.a + right.a));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgba32 operator -(ColorRgba32 left, ColorRgba32 right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r - right.r),
			ByteHelper.ClampToByte(left.g - right.g),
			ByteHelper.ClampToByte(left.b - right.b),
			ByteHelper.ClampToByte(left.a - right.a));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgba32 operator /(ColorRgba32 left, double right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte((int)(left.r / right)),
			ByteHelper.ClampToByte((int)(left.g / right)),
			ByteHelper.ClampToByte((int)(left.b / right)),
			ByteHelper.ClampToByte((int)(left.a / right))
		);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgba32 operator *(ColorRgba32 left, double right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte((int)(left.r * right)),
			ByteHelper.ClampToByte((int)(left.g * right)),
			ByteHelper.ClampToByte((int)(left.b * right)),
			ByteHelper.ClampToByte((int)(left.a * right))
		);
	}

	/// <summary>
	/// Component-wise left shift
	/// </summary>
	public static ColorRgba32 operator <<(ColorRgba32 left, int right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r << right),
			ByteHelper.ClampToByte(left.g << right),
			ByteHelper.ClampToByte(left.b << right),
			ByteHelper.ClampToByte(left.a << right)
		);
	}

	/// <summary>
	/// Component-wise right shift
	/// </summary>
	public static ColorRgba32 operator >>(ColorRgba32 left, int right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r >> right),
			ByteHelper.ClampToByte(left.g >> right),
			ByteHelper.ClampToByte(left.b >> right),
			ByteHelper.ClampToByte(left.a >> right)
		);
	}

	/// <summary>
	/// Component-wise bitwise OR operation
	/// </summary>
	public static ColorRgba32 operator |(ColorRgba32 left, ColorRgba32 right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r | right.r),
			ByteHelper.ClampToByte(left.g | right.g),
			ByteHelper.ClampToByte(left.b | right.b),
			ByteHelper.ClampToByte(left.a | right.a)
		);
	}

	/// <summary>
	/// Component-wise bitwise OR operation
	/// </summary>
	public static ColorRgba32 operator |(ColorRgba32 left, int right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r | right),
			ByteHelper.ClampToByte(left.g | right),
			ByteHelper.ClampToByte(left.b | right),
			ByteHelper.ClampToByte(left.a | right)
		);
	}

	/// <summary>
	/// Component-wise bitwise AND operation
	/// </summary>
	public static ColorRgba32 operator &(ColorRgba32 left, ColorRgba32 right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r & right.r),
			ByteHelper.ClampToByte(left.g & right.g),
			ByteHelper.ClampToByte(left.b & right.b),
			ByteHelper.ClampToByte(left.a & right.a)
		);
	}

	/// <summary>
	/// Component-wise bitwise AND operation
	/// </summary>
	public static ColorRgba32 operator &(ColorRgba32 left, int right)
	{
		return new ColorRgba32(
			ByteHelper.ClampToByte(left.r & right),
			ByteHelper.ClampToByte(left.g & right),
			ByteHelper.ClampToByte(left.b & right),
			ByteHelper.ClampToByte(left.a & right)
		);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly override string ToString()
	{
		return $"r : {r} g : {g} b : {b} a : {a}";
	}

	internal readonly ColorRgbaFloat ToFloat()
	{
		return new ColorRgbaFloat(this);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly ColorRgbFloat ToRgbFloat()
	{
		return new ColorRgbFloat(this);
	}
}

internal struct ColorRgbaFloat(float r, float g, float b, float a)
	: IEquatable<ColorRgbaFloat>
{
	public float r = r, g = g, b = b, a = a;

	public ColorRgbaFloat(ColorRgba32 other) : this(other.r / 255f, other.g / 255f, other.b / 255f, other.a / 255f)
	{
	}

	public ColorRgbaFloat(float r, float g, float b) : this(r, g, b, 1)
	{
	}

	public readonly bool Equals(ColorRgbaFloat other)
	{
		const double epsilon = 0.00001;

		return Math.Abs(r - other.r) < epsilon &&
		       Math.Abs(g - other.g) < epsilon &&
		       Math.Abs(b - other.b) < epsilon &&
		       Math.Abs(a - other.a) < epsilon;
	}

	public readonly override bool Equals(object obj) => obj is ColorRgbaFloat other && Equals(other);

	public readonly override int GetHashCode() => HashCode.Combine(r, g, b, a);

	public static bool operator ==(ColorRgbaFloat left, ColorRgbaFloat right) => left.Equals(right);

	public static bool operator !=(ColorRgbaFloat left, ColorRgbaFloat right) => !left.Equals(right);

	public static ColorRgbaFloat operator +(ColorRgbaFloat left, ColorRgbaFloat right) =>
		new(
			left.r + right.r,
			left.g + right.g,
			left.b + right.b,
			left.a + right.a);

	public static ColorRgbaFloat operator -(ColorRgbaFloat left, ColorRgbaFloat right) =>
		new(
			left.r - right.r,
			left.g - right.g,
			left.b - right.b,
			left.a - right.a);

	public static ColorRgbaFloat operator /(ColorRgbaFloat left, float right) =>
		new(
			left.r / right,
			left.g / right,
			left.b / right,
			left.a / right
		);

	public static ColorRgbaFloat operator *(ColorRgbaFloat left, float right) =>
		new(
			left.r * right,
			left.g * right,
			left.b * right,
			left.a * right
		);

	public static ColorRgbaFloat operator *(float left, ColorRgbaFloat right) =>
		new(
			right.r * left,
			right.g * left,
			right.b * left,
			right.a * left
		);

	public readonly override string ToString() => $"r : {r:0.00} g : {g:0.00} b : {b:0.00} a : {a:0.00}";

	public readonly ColorRgba32 ToRgba32() =>
		new(
			ByteHelper.ClampToByte(r * 255),
			ByteHelper.ClampToByte(g * 255),
			ByteHelper.ClampToByte(b * 255),
			ByteHelper.ClampToByte(a * 255)
		);
}
	
/// <summary>
/// 
/// </summary>
public struct ColorRgbFloat(float r, float g, float b) : IEquatable<ColorRgbFloat>
{
	/// <summary>
	/// 
	/// </summary>
	public float r = r, g = g, b = b;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="other"></param>
	public ColorRgbFloat(ColorRgba32 other) : this(other.r / 255f, other.g / 255f, other.b / 255f)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="vector"></param>
	public ColorRgbFloat(Vector3 vector) : this(vector.X, vector.Y, vector.Z)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public readonly bool Equals(ColorRgbFloat other)
	{
		const double epsilon = 0.00001;

		return Math.Abs(r - other.r) < epsilon &&
		       Math.Abs(g - other.g) < epsilon &&
		       Math.Abs(b - other.b) < epsilon;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public readonly override bool Equals(object obj) => obj is ColorRgbFloat other && Equals(other);


	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly override int GetHashCode() => HashCode.Combine(r, g, b);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(ColorRgbFloat left, ColorRgbFloat right) => left.Equals(right);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(ColorRgbFloat left, ColorRgbFloat right) => !left.Equals(right);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgbFloat operator +(ColorRgbFloat left, ColorRgbFloat right) =>
		new(
			left.r + right.r,
			left.g + right.g,
			left.b + right.b);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgbFloat operator -(ColorRgbFloat left, ColorRgbFloat right) =>
		new(
			left.r - right.r,
			left.g - right.g,
			left.b - right.b);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgbFloat operator /(ColorRgbFloat left, float right) =>
		new(
			left.r / right,
			left.g / right,
			left.b / right
		);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgbFloat operator *(ColorRgbFloat left, float right) =>
		new(
			left.r * right,
			left.g * right,
			left.b * right
		);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static ColorRgbFloat operator *(float left, ColorRgbFloat right) =>
		new(
			right.r * left,
			right.g * left,
			right.b * left
		);

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly override string ToString() => $"r : {r:0.00} g : {g:0.00} b : {b:0.00}";

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly ColorRgba32 ToRgba32() =>
		new(
			ByteHelper.ClampToByte(r * 255),
			ByteHelper.ClampToByte(g * 255),
			ByteHelper.ClampToByte(b * 255),
			255
		);

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public readonly Vector3 ToVector3() => new(r, g, b);

	internal readonly float CalcLogDist(ColorRgbFloat other)
	{
		var dr = Math.Sign(other.r) * MathF.Log(1 + MathF.Abs(other.r)) - Math.Sign(r) * MathF.Log(1 + MathF.Abs(r));
		var dg = Math.Sign(other.g) * MathF.Log(1 + MathF.Abs(other.g)) - Math.Sign(g) * MathF.Log(1 + MathF.Abs(g));
		var db = Math.Sign(other.b) * MathF.Log(1 + MathF.Abs(other.b)) - Math.Sign(b) * MathF.Log(1 + MathF.Abs(b));
		return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db));
	}

	internal readonly float CalcDist(ColorRgbFloat other)
	{
		var dr = other.r - r;
		var dg = other.g - g;
		var db = other.b - b;
		return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db));
	}

	internal void ClampToPositive()
	{
		if (r < 0) r = 0;
		if (g < 0) g = 0;
		if (b < 0) b = 0;
	}

	internal void ClampToHalf()
	{
		r = Math.Max(Math.Min(r, Half.MaxValue), Half.MinValue);
		g = Math.Max(Math.Min(g, Half.MaxValue), Half.MinValue);
		b = Math.Max(Math.Min(b, Half.MaxValue), Half.MinValue);
	}
}

internal struct ColorYCbCr
{
	public float y;
	public float cb;
	public float cr;

	public ColorYCbCr(float y, float cb, float cr)
	{
		this.y = y;
		this.cb = cb;
		this.cr = cr;
	}

	internal ColorYCbCr(ColorRgb24 rgb)
	{
		var fr = (float)rgb.r / 255;
		var fg = (float)rgb.g / 255;
		var fb = (float)rgb.b / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	internal ColorYCbCr(ColorRgbaFloat rgb)
	{
		var fr = rgb.r;
		var fg = rgb.g;
		var fb = rgb.b;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	internal ColorYCbCr(ColorRgbFloat rgb)
	{
		var fr = rgb.r;
		var fg = rgb.g;
		var fb = rgb.b;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	internal ColorYCbCr(ColorRgb565 rgb)
	{
		var fr = (float)rgb.R / 255;
		var fg = (float)rgb.G / 255;
		var fb = (float)rgb.B / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	public ColorYCbCr(ColorRgba32 rgba)
	{
		var fr = (float)rgba.r / 255;
		var fg = (float)rgba.g / 255;
		var fb = (float)rgba.b / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	public ColorYCbCr(Vector3 vec)
	{
		var fr = vec.X;
		var fg = vec.Y;
		var fb = vec.Z;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
	}

	public readonly ColorRgb565 ToColorRgb565()
	{
		var r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
		var g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
		var b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

		return new ColorRgb565((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
	}

	public readonly ColorRgba32 ToColorRgba32()
	{
		var r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
		var g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
		var b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

		return new ColorRgba32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
	}

	public readonly override string ToString()
	{
		var r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
		var g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
		var b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

		return $"r : {r * 255} g : {g * 255} b : {b * 255}";
	}

	public readonly float CalcDistWeighted(ColorYCbCr other, float yWeight = 4)
	{
		var dy = (y - other.y) * (y - other.y) * yWeight;
		var dcb = (cb - other.cb) * (cb - other.cb);
		var dcr = (cr - other.cr) * (cr - other.cr);

		return MathF.Sqrt(dy + dcb + dcr);
	}

	public static ColorYCbCr operator +(ColorYCbCr left, ColorYCbCr right) =>
		new(
			left.y + right.y,
			left.cb + right.cb,
			left.cr + right.cr);

	public static ColorYCbCr operator /(ColorYCbCr left, float right) =>
		new(
			left.y / right,
			left.cb / right,
			left.cr / right);
}

internal struct ColorRgb555 : IEquatable<ColorRgb555>
{
	public readonly bool Equals(ColorRgb555 other) => data == other.data;

	public readonly override bool Equals(object obj) => obj is ColorRgb555 other && Equals(other);

	public readonly override int GetHashCode() => data.GetHashCode();

	public static bool operator ==(ColorRgb555 left, ColorRgb555 right) => left.Equals(right);

	public static bool operator !=(ColorRgb555 left, ColorRgb555 right) => !left.Equals(right);

	private const ushort ModeMask = 0b1_00000_00000_00000;
	private const int ModeShift = 15;
	private const ushort RedMask = 0b0_11111_00000_00000;
	private const int RedShift = 10;
	private const ushort GreenMask = 0b0_00000_11111_00000;
	private const int GreenShift = 5;
	private const ushort BlueMask = 0b0_00000_00000_11111;

	public ushort data;

	public byte Mode
	{
		readonly get
		{
			var mode = (data & ModeMask) >> ModeShift;
			return (byte)mode;
		}
		set
		{
			data = (ushort)(data & ~ModeMask);
			data = (ushort)(data | (value << ModeShift));
		}
	}

	public byte R
	{
		readonly get
		{
			var r5 = (data & RedMask) >> RedShift;
			return (byte)((r5 << 3) | (r5 >> 2));
		}
		set
		{
			var r5 = value >> 3;
			data = (ushort)(data & ~RedMask);
			data = (ushort)(data | (r5 << RedShift));
		}
	}

	public byte G
	{
		readonly get
		{
			var g5 = (data & GreenMask) >> GreenShift;
			return (byte)((g5 << 3) | (g5 >> 2));
		}
		set
		{
			var g5 = value >> 3;
			data = (ushort)(data & ~GreenMask);
			data = (ushort)(data | (g5 << GreenShift));
		}
	}

	public byte B
	{
		readonly get
		{
			var b5 = data & BlueMask;
			return (byte)((b5 << 3) | (b5 >> 2));
		}
		set
		{
			var b5 = value >> 3;
			data = (ushort)(data & ~BlueMask);
			data = (ushort)(data | b5);
		}
	}

	public int RawR
	{
		readonly get => (data & RedMask) >> RedShift;
		set
		{
			if (value > 31) value = 31;
			if (value < 0) value = 0;
			data = (ushort)(data & ~RedMask);
			data = (ushort)(data | (value << RedShift));
		}
	}

	public int RawG
	{
		readonly get => (data & GreenMask) >> GreenShift;
		set
		{
			if (value > 31) value = 31;
			if (value < 0) value = 0;
			data = (ushort)(data & ~GreenMask);
			data = (ushort)(data | (value << GreenShift));
		}
	}

	public int RawB
	{
		readonly get => data & BlueMask;
		set
		{
			if (value > 31) value = 31;
			if (value < 0) value = 0;
			data = (ushort)(data & ~BlueMask);
			data = (ushort)(data | value);
		}
	}

	public ColorRgb555(byte r, byte g, byte b)
	{
		data = 0;
		R = r;
		G = g;
		B = b;
	}

	public ColorRgb555(Vector3 colorVector)
	{
		data = 0;
		R = ByteHelper.ClampToByte(colorVector.X * 255);
		G = ByteHelper.ClampToByte(colorVector.Y * 255);
		B = ByteHelper.ClampToByte(colorVector.Z * 255);
	}

	public ColorRgb555(ColorRgb24 color)
	{
		data = 0;
		R = color.r;
		G = color.g;
		B = color.b;
	}

	public readonly ColorRgb24 ToColorRgb24() => new(R, G, B);

	public readonly override string ToString() => $"r : {R} g : {G} b : {B}";

	public ColorRgba32 ToColorRgba32() => new(R, G, B, 255);
}

internal struct ColorRgb565 : IEquatable<ColorRgb565>
{
	public readonly bool Equals(ColorRgb565 other) => data == other.data;

	public readonly override bool Equals(object obj) => obj is ColorRgb565 other && Equals(other);

	public readonly override int GetHashCode() => data.GetHashCode();

	public static bool operator ==(ColorRgb565 left, ColorRgb565 right) => left.Equals(right);

	public static bool operator !=(ColorRgb565 left, ColorRgb565 right) => !left.Equals(right);

	private const ushort RedMask = 0b11111_000000_00000;
	private const int RedShift = 11;
	private const ushort GreenMask = 0b00000_111111_00000;
	private const int GreenShift = 5;
	private const ushort BlueMask = 0b00000_000000_11111;

	public ushort data;

	public byte R
	{
		readonly get
		{
			var r5 = (data & RedMask) >> RedShift;
			return (byte)((r5 << 3) | (r5 >> 2));
		}
		set
		{
			var r5 = value >> 3;
			data = (ushort)(data & ~RedMask);
			data = (ushort)(data | (r5 << RedShift));
		}
	}

	public byte G
	{
		readonly get
		{
			var g6 = (data & GreenMask) >> GreenShift;
			return (byte)((g6 << 2) | (g6 >> 4));
		}
		set
		{
			var g6 = value >> 2;
			data = (ushort)(data & ~GreenMask);
			data = (ushort)(data | (g6 << GreenShift));
		}
	}

	public byte B
	{
		readonly get
		{
			var b5 = data & BlueMask;
			return (byte)((b5 << 3) | (b5 >> 2));
		}
		set
		{
			var b5 = value >> 3;
			data = (ushort)(data & ~BlueMask);
			data = (ushort)(data | b5);
		}
	}

	public int RawR
	{
		readonly get => (data & RedMask) >> RedShift;
		set
		{
			if (value > 31) value = 31;
			if (value < 0) value = 0;
			data = (ushort)(data & ~RedMask);
			data = (ushort)(data | (value << RedShift));
		}
	}

	public int RawG
	{
		readonly get => (data & GreenMask) >> GreenShift;
		set
		{
			if (value > 63) value = 63;
			if (value < 0) value = 0;
			data = (ushort)(data & ~GreenMask);
			data = (ushort)(data | (value << GreenShift));
		}
	}

	public int RawB
	{
		readonly get => data & BlueMask;
		set
		{
			if (value > 31) value = 31;
			if (value < 0) value = 0;
			data = (ushort)(data & ~BlueMask);
			data = (ushort)(data | value);
		}
	}

	public ColorRgb565(byte r, byte g, byte b)
	{
		data = 0;
		R = r;
		G = g;
		B = b;
	}

	public ColorRgb565(Vector3 colorVector)
	{
		data = 0;
		R = ByteHelper.ClampToByte(colorVector.X * 255);
		G = ByteHelper.ClampToByte(colorVector.Y * 255);
		B = ByteHelper.ClampToByte(colorVector.Z * 255);
	}

	public ColorRgb565(ColorRgb24 color)
	{
		data = 0;
		R = color.r;
		G = color.g;
		B = color.b;
	}

	public readonly ColorRgb24 ToColorRgb24() => new(R, G, B);

	public readonly override string ToString() => $"r : {R} g : {G} b : {B}";

	public readonly ColorRgba32 ToColorRgba32() => new(R, G, B, 255);
}

internal struct ColorRgb24(byte r, byte g, byte b) : IEquatable<ColorRgb24>
{
	public byte r = r, g = g, b = b;

	public ColorRgb24(ColorRgb565 color) : this(color.R, color.G, color.B)
	{
	}

	public ColorRgb24(ColorRgba32 color) : this(color.r, color.g, color.b)
	{
	}

	public readonly bool Equals(ColorRgb24 other) => r == other.r && g == other.g && b == other.b;

	public readonly override bool Equals(object obj) => obj is ColorRgb24 other && Equals(other);

	public readonly override int GetHashCode() => HashCode.Combine(r, g, b);

	public static bool operator ==(ColorRgb24 left, ColorRgb24 right) => left.Equals(right);

	public static bool operator !=(ColorRgb24 left, ColorRgb24 right) => !left.Equals(right);

	public static ColorRgb24 operator +(ColorRgb24 left, ColorRgb24 right) =>
		new(
			ByteHelper.ClampToByte(left.r + right.r),
			ByteHelper.ClampToByte(left.g + right.g),
			ByteHelper.ClampToByte(left.b + right.b));

	public static ColorRgb24 operator -(ColorRgb24 left, ColorRgb24 right) =>
		new(
			ByteHelper.ClampToByte(left.r - right.r),
			ByteHelper.ClampToByte(left.g - right.g),
			ByteHelper.ClampToByte(left.b - right.b));

	public static ColorRgb24 operator /(ColorRgb24 left, double right) =>
		new(
			ByteHelper.ClampToByte((int)(left.r / right)),
			ByteHelper.ClampToByte((int)(left.g / right)),
			ByteHelper.ClampToByte((int)(left.b / right))
		);

	public static ColorRgb24 operator *(ColorRgb24 left, double right) =>
		new(
			ByteHelper.ClampToByte((int)(left.r * right)),
			ByteHelper.ClampToByte((int)(left.g * right)),
			ByteHelper.ClampToByte((int)(left.b * right))
		);

	public readonly override string ToString() => $"r : {r} g : {g} b : {b}";
}

internal struct ColorYCbCrAlpha
{
	public float y;
	public float cb;
	public float cr;
	public float alpha;

	public ColorYCbCrAlpha(float y, float cb, float cr, float alpha)
	{
		this.y = y;
		this.cb = cb;
		this.cr = cr;
		this.alpha = alpha;
	}

	public ColorYCbCrAlpha(ColorRgb24 rgb)
	{
		var fr = (float)rgb.r / 255;
		var fg = (float)rgb.g / 255;
		var fb = (float)rgb.b / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
		alpha = 1;
	}

	public ColorYCbCrAlpha(ColorRgb565 rgb)
	{
		var fr = (float)rgb.R / 255;
		var fg = (float)rgb.G / 255;
		var fb = (float)rgb.B / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
		alpha = 1;
	}

	public ColorYCbCrAlpha(ColorRgba32 rgba)
	{
		var fr = (float)rgba.r / 255;
		var fg = (float)rgba.g / 255;
		var fb = (float)rgba.b / 255;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
		alpha = rgba.a / 255f;
	}

	public ColorYCbCrAlpha(ColorRgbaFloat rgba)
	{
		var fr = rgba.r;
		var fg = rgba.g;
		var fb = rgba.b;

		y = 0.2989f * fr + 0.5866f * fg + 0.1145f * fb;
		cb = -0.1687f * fr - 0.3313f * fg + 0.5000f * fb;
		cr = 0.5000f * fr - 0.4184f * fg - 0.0816f * fb;
		alpha = rgba.a;
	}


	public readonly ColorRgb565 ToColorRgb565()
	{
		var r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
		var g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
		var b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

		return new ColorRgb565((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
	}

	public readonly override string ToString()
	{
		var r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
		var g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
		var b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

		return $"r : {r * 255} g : {g * 255} b : {b * 255}";
	}

	public readonly float CalcDistWeighted(ColorYCbCrAlpha other, float yWeight = 4, float aWeight = 1)
	{
		var dy = (y - other.y) * (y - other.y) * yWeight;
		var dcb = (cb - other.cb) * (cb - other.cb);
		var dcr = (cr - other.cr) * (cr - other.cr);
		var da = (alpha - other.alpha) * (alpha - other.alpha) * aWeight;

		return MathF.Sqrt(dy + dcb + dcr + da);
	}

	public static ColorYCbCrAlpha operator +(ColorYCbCrAlpha left, ColorYCbCrAlpha right) =>
		new(
			left.y + right.y,
			left.cb + right.cb,
			left.cr + right.cr,
			left.alpha + right.alpha);

	public static ColorYCbCrAlpha operator /(ColorYCbCrAlpha left, float right) =>
		new(
			left.y / right,
			left.cb / right,
			left.cr / right,
			left.alpha / right);
}

internal struct ColorXyz
{
	public float x;
	public float y;
	public float z;

	public ColorXyz(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public ColorXyz(ColorRgb24 color)
	{
		this = ColorToXyz(color);
	}

	public ColorXyz(ColorRgbFloat color)
	{
		this = ColorToXyz(color);
	}

	public readonly ColorRgbFloat ToColorRgbFloat() =>
		new(
			3.2404542f * x - 1.5371385f * y - 0.4985314f * z,
			-0.9692660f * x + 1.8760108f * y + 0.0415560f * z,
			0.0556434f * x - 0.2040259f * y + 1.0572252f * z
		);

	public static ColorXyz ColorToXyz(ColorRgb24 color)
	{
		var r = PivotRgb(color.r / 255.0f);
		var g = PivotRgb(color.g / 255.0f);
		var b = PivotRgb(color.b / 255.0f);

		// Observer. = 2°, Illuminant = D65
		return new ColorXyz(r * 0.4124f + g * 0.3576f + b * 0.1805f, r * 0.2126f + g * 0.7152f + b * 0.0722f,
			r * 0.0193f + g * 0.1192f + b * 0.9505f);
	}

	public static ColorXyz ColorToXyz(ColorRgbFloat color)
	{
		var r = PivotRgb(color.r);
		var g = PivotRgb(color.g);
		var b = PivotRgb(color.b);

		// Observer. = 2°, Illuminant = D65
		return new ColorXyz(r * 0.4124f + g * 0.3576f + b * 0.1805f, r * 0.2126f + g * 0.7152f + b * 0.0722f,
			r * 0.0193f + g * 0.1192f + b * 0.9505f);
	}

	private static float PivotRgb(float n) => (n > 0.04045f ? MathF.Pow((n + 0.055f) / 1.055f, 2.4f) : n / 12.92f) * 100;
}

internal struct ColorLab
{
	public float l;
	public float a;
	public float b;

	public ColorLab(float l, float a, float b)
	{
		this.l = l;
		this.a = a;
		this.b = b;
	}

	public ColorLab(ColorRgb24 color)
	{
		this = ColorToLab(color);
	}

	public ColorLab(ColorRgba32 color)
	{
		this = ColorToLab(new ColorRgb24(color.r, color.g, color.b));
	}

	public ColorLab(ColorRgbFloat color)
	{
		this = XyzToLab(new ColorXyz(color));
	}

	public static ColorLab ColorToLab(ColorRgb24 color)
	{
		var xyz = new ColorXyz(color);
		return XyzToLab(xyz);
	}


	public static ColorLab XyzToLab(ColorXyz xyz)
	{
		const float refX = 95.047f; // Observer= 2°, Illuminant= D65
		const float refY = 100.000f;
		const float refZ = 108.883f;

		var x = PivotXyz(xyz.x / refX);
		var y = PivotXyz(xyz.y / refY);
		var z = PivotXyz(xyz.z / refZ);

		return new ColorLab(116 * y - 16, 500 * (x - y), 200 * (y - z));
	}

	private static float PivotXyz(float n)
	{
		var i = MathF.Cbrt(n);
		return n > 0.008856f ? i : 7.787f * n + 16 / 116f;
	}
}

internal struct ColorRgbe : IEquatable<ColorRgbe>
{
	public byte r;
	public byte g;
	public byte b;
	public byte e;

	public ColorRgbe(byte r, byte g, byte b, byte e)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.e = e;
	}

	public ColorRgbe(ColorRgbFloat color)
	{
		var max = MathF.Max(color.b, MathF.Max(color.g, color.r));
		if (max <= 1e-32f)
		{
			r = g = b = e = 0;
		}
		else
		{
			MathHelper.FrExp(max, out var exponent);
			var scale = MathHelper.LdExp(1f, -exponent + 8);
			r = (byte)(scale * color.r);
			g = (byte)(scale * color.g);
			b = (byte)(scale * color.b);
			e = (byte)(exponent + 128);
		}
	}

	public readonly ColorRgbFloat ToColorRgbFloat(float exposure = 1.0f)
	{
		if (e == 0)
		{
			return new ColorRgbFloat(0, 0, 0);
		}
		{
			var fexp = MathHelper.LdExp(1f, e - (128 + 8)) / exposure;

			return new ColorRgbFloat(
				(r + 0.5f) * fexp,
				(g + 0.5f) * fexp,
				(b + 0.5f) * fexp
			);
		}
	}


	public readonly bool Equals(ColorRgbe other) => r == other.r && g == other.g && b == other.b && e == other.e;

	public readonly override bool Equals(object obj) => obj is ColorRgbe other && Equals(other);

	public readonly override int GetHashCode() => HashCode.Combine(r, g, b, e);

	public static bool operator ==(ColorRgbe left, ColorRgbe right) => left.Equals(right);

	public static bool operator !=(ColorRgbe left, ColorRgbe right) => !left.Equals(right);

	public readonly override string ToString() => $"{nameof(r)}: {r}, {nameof(g)}: {g}, {nameof(b)}: {b}, {nameof(e)}: {e}";
}
