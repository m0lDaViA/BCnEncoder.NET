#pragma warning disable CS1587 // XML comment is not placed on a valid language element
/// ================ Half.cs ====================
/// The code is free to use for any reason without any restrictions.
/// Ladislav Lang (2009), Joannes Vermorel (2017)
#pragma warning restore CS1587 // XML comment is not placed on a valid language element

using System;
using System.Diagnostics;
using System.Globalization;

namespace BCnEncoder.Shared
{
	/// <summary>
	/// Represents a half-precision floating point number. 
	/// </summary>
	/// <remarks>
	/// Note:
	///     Half is not fast enought and precision is also very bad, 
	///     so is should not be used for mathematical computation (use Single instead).
	///     The main advantage of Half type is lower memory cost: two bytes per number. 
	///     Half is typically used in graphical applications.
	///     
	/// Note: 
	///     All functions, where is used conversion half->float/float->half, 
	///     are approx. ten times slower than float->double/double->float, i.e. ~3ns on 2GHz CPU.
	///
	/// References:
	///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
	///     - Fast Half Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
	///     - IEEE 754 revision, link: http://grouper.ieee.org/groups/754/
	/// </remarks>
	[Serializable]
	public struct Half : IComparable, IFormattable, IConvertible, IComparable<Half>, IEquatable<Half>
	{
		/// <summary>
		/// Internal representation of the half-precision floating-point number.
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal ushort value;

		#region Constants
		/// <summary>
		/// Represents the smallest positive System.Half value greater than zero. This field is constant.
		/// </summary>
		public static readonly Half Epsilon = ToHalf(0x0001);
		/// <summary>
		/// Represents the largest possible value of System.Half. This field is constant.
		/// </summary>
		public static readonly Half MaxValue = ToHalf(0x7bff);
		/// <summary>
		/// Represents the smallest possible value of System.Half. This field is constant.
		/// </summary>
		public static readonly Half MinValue = ToHalf(0xfbff);
		/// <summary>
		/// Represents not a number (NaN). This field is constant.
		/// </summary>
		public static readonly Half NaN = ToHalf(0xfe00);
		/// <summary>
		/// Represents negative infinity. This field is constant.
		/// </summary>
		public static readonly Half NegativeInfinity = ToHalf(0xfc00);
		/// <summary>
		/// Represents positive infinity. This field is constant.
		/// </summary>
		public static readonly Half PositiveInfinity = ToHalf(0x7c00);
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified single-precision floating-point number.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(float value) { this = HalfHelper.SingleToHalf(value); }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified 32-bit signed integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(int value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified 64-bit signed integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(long value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified double-precision floating-point number.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(double value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified decimal number.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(decimal value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified 32-bit unsigned integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(uint value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.Half to the value of the specified 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.Half.</param>
		public Half(ulong value) : this((float)value) { }
		#endregion

		#region Numeric operators

		/// <summary>
		/// Returns the result of multiplying the specified System.Half value by negative one.
		/// </summary>
		/// <param name="half">A System.Half.</param>
		/// <returns>A System.Half with the value of half, but the opposite sign. -or- Zero, if half is zero.</returns>
		public static Half Negate(Half half) => -half;

		/// <summary>
		/// Adds two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>A System.Half value that is the sum of half1 and half2.</returns>
		public static Half Add(Half half1, Half half2) => half1 + half2;

		/// <summary>
		/// Subtracts one specified System.Half value from another.
		/// </summary>
		/// <param name="half1">A System.Half (the minuend).</param>
		/// <param name="half2">A System.Half (the subtrahend).</param>
		/// <returns>The System.Half result of subtracting half2 from half1.</returns>
		public static Half Subtract(Half half1, Half half2) => half1 - half2;
		/// <summary>
		/// Multiplies two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half (the multiplicand).</param>
		/// <param name="half2">A System.Half (the multiplier).</param>
		/// <returns>A System.Half that is the result of multiplying half1 and half2.</returns>
		public static Half Multiply(Half half1, Half half2) => half1 * half2;
		/// <summary>
		/// Divides two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half (the dividend).</param>
		/// <param name="half2">A System.Half (the divisor).</param>
		/// <returns>The System.Half that is the result of dividing half1 by half2.</returns>
		/// <exception cref="System.DivideByZeroException">half2 is zero.</exception>
		public static Half Divide(Half half1, Half half2) => half1 / half2;

		/// <summary>
		/// Returns the value of the System.Half operand (the sign of the operand is unchanged).
		/// </summary>
		/// <param name="half">The System.Half operand.</param>
		/// <returns>The value of the operand, half.</returns>
		public static Half operator +(Half half) => half;

		/// <summary>
		/// Negates the value of the specified System.Half operand.
		/// </summary>
		/// <param name="half">The System.Half operand.</param>
		/// <returns>The result of half multiplied by negative one (-1).</returns>
		public static Half operator -(Half half) => HalfHelper.Negate(half);

		/// <summary>
		/// Increments the System.Half operand by 1.
		/// </summary>
		/// <param name="half">The System.Half operand.</param>
		/// <returns>The value of half incremented by 1.</returns>
		public static Half operator ++(Half half) => (Half)(half + 1f);

		/// <summary>
		/// Decrements the System.Half operand by one.
		/// </summary>
		/// <param name="half">The System.Half operand.</param>
		/// <returns>The value of half decremented by 1.</returns>
		public static Half operator --(Half half) => (Half)(half - 1f);

		/// <summary>
		/// Adds two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>The System.Half result of adding half1 and half2.</returns>
		public static Half operator +(Half half1, Half half2) => (Half)(half1 + (float)half2);

		/// <summary>
		/// Subtracts two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>The System.Half result of subtracting half1 and half2.</returns>        
		public static Half operator -(Half half1, Half half2) => (Half)(half1 - (float)half2);

		/// <summary>
		/// Multiplies two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>The System.Half result of multiplying half1 by half2.</returns>
		public static Half operator *(Half half1, Half half2) => (Half)(half1 * (float)half2);

		/// <summary>
		/// Divides two specified System.Half values.
		/// </summary>
		/// <param name="half1">A System.Half (the dividend).</param>
		/// <param name="half2">A System.Half (the divisor).</param>
		/// <returns>The System.Half result of half1 by half2.</returns>
		public static Half operator /(Half half1, Half half2) => (Half)(half1 / (float)half2);

		/// <summary>
		/// Returns a value indicating whether two instances of System.Half are equal.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 and half2 are equal; otherwise, false.</returns>
		public static bool operator ==(Half half1, Half half2) => (!IsNaN(half1) && (half1.value == half2.value));

		/// <summary>
		/// Returns a value indicating whether two instances of System.Half are not equal.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 and half2 are not equal; otherwise, false.</returns>
		public static bool operator !=(Half half1, Half half2) => half1.value != half2.value;

		/// <summary>
		/// Returns a value indicating whether a specified System.Half is less than another specified System.Half.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 is less than half1; otherwise, false.</returns>
		public static bool operator <(Half half1, Half half2) => half1 < (float)half2;

		/// <summary>
		/// Returns a value indicating whether a specified System.Half is greater than another specified System.Half.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 is greater than half2; otherwise, false.</returns>
		public static bool operator >(Half half1, Half half2) => half1 > (float)half2;

		/// <summary>
		/// Returns a value indicating whether a specified System.Half is less than or equal to another specified System.Half.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 is less than or equal to half2; otherwise, false.</returns>
		public static bool operator <=(Half half1, Half half2) => (half1 == half2) || (half1 < half2);

		/// <summary>
		/// Returns a value indicating whether a specified System.Half is greater than or equal to another specified System.Half.
		/// </summary>
		/// <param name="half1">A System.Half.</param>
		/// <param name="half2">A System.Half.</param>
		/// <returns>true if half1 is greater than or equal to half2; otherwise, false.</returns>
		public static bool operator >=(Half half1, Half half2) => (half1 == half2) || (half1 > half2);

		#endregion

		#region Type casting operators
		/// <summary>
		/// Converts an 8-bit unsigned integer to a System.Half.
		/// </summary>
		/// <param name="value">An 8-bit unsigned integer.</param>
		/// <returns>A System.Half that represents the converted 8-bit unsigned integer.</returns>
		public static implicit operator Half(byte value) => new((float)value);

		/// <summary>
		/// Converts a 16-bit signed integer to a System.Half.
		/// </summary>
		/// <param name="value">A 16-bit signed integer.</param>
		/// <returns>A System.Half that represents the converted 16-bit signed integer.</returns>
		public static implicit operator Half(short value) => new((float)value);

		/// <summary>
		/// Converts a Unicode character to a System.Half.
		/// </summary>
		/// <param name="value">A Unicode character.</param>
		/// <returns>A System.Half that represents the converted Unicode character.</returns>
		public static implicit operator Half(char value) => new((float)value);

		/// <summary>
		/// Converts a 32-bit signed integer to a System.Half.
		/// </summary>
		/// <param name="value">A 32-bit signed integer.</param>
		/// <returns>A System.Half that represents the converted 32-bit signed integer.</returns>
		public static implicit operator Half(int value) => new((float)value);

		/// <summary>
		/// Converts a 64-bit signed integer to a System.Half.
		/// </summary>
		/// <param name="value">A 64-bit signed integer.</param>
		/// <returns>A System.Half that represents the converted 64-bit signed integer.</returns>
		public static implicit operator Half(long value) => new((float)value);

		/// <summary>
		/// Converts a single-precision floating-point number to a System.Half.
		/// </summary>
		/// <param name="value">A single-precision floating-point number.</param>
		/// <returns>A System.Half that represents the converted single-precision floating point number.</returns>
		public static explicit operator Half(float value) => new(value);

		/// <summary>
		/// Converts a double-precision floating-point number to a System.Half.
		/// </summary>
		/// <param name="value">A double-precision floating-point number.</param>
		/// <returns>A System.Half that represents the converted double-precision floating point number.</returns>
		public static explicit operator Half(double value) => new((float)value);

		/// <summary>
		/// Converts a decimal number to a System.Half.
		/// </summary>
		/// <param name="value">decimal number</param>
		/// <returns>A System.Half that represents the converted decimal number.</returns>
		public static explicit operator Half(decimal value) => new((float)value);

		/// <summary>
		/// Converts a System.Half to an 8-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>An 8-bit unsigned integer that represents the converted System.Half.</returns>
		public static explicit operator byte(Half value) => (byte)(float)value;
		/// <summary>
		/// Converts a System.Half to a Unicode character.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A Unicode character that represents the converted System.Half.</returns>
		public static explicit operator char(Half value) => (char)(float)value;
		/// <summary>
		/// Converts a System.Half to a 16-bit signed integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 16-bit signed integer that represents the converted System.Half.</returns>
		public static explicit operator short(Half value) => (short)(float)value;
		/// <summary>
		/// Converts a System.Half to a 32-bit signed integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 32-bit signed integer that represents the converted System.Half.</returns>
		public static explicit operator int(Half value) => (int)(float)value;
		/// <summary>
		/// Converts a System.Half to a 64-bit signed integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 64-bit signed integer that represents the converted System.Half.</returns>
		public static explicit operator long(Half value) => (long)(float)value;
		/// <summary>
		/// Converts a System.Half to a single-precision floating-point number.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A single-precision floating-point number that represents the converted System.Half.</returns>
		public static implicit operator float(Half value) => HalfHelper.HalfToSingle(value);

		/// <summary>
		/// Converts a System.Half to a double-precision floating-point number.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A double-precision floating-point number that represents the converted System.Half.</returns>
		public static implicit operator double(Half value) => (float)value;

		/// <summary>
		/// Converts a System.Half to a decimal number.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A decimal number that represents the converted System.Half.</returns>
		public static explicit operator decimal(Half value) => (decimal)(float)value;
		/// <summary>
		/// Converts an 8-bit signed integer to a System.Half.
		/// </summary>
		/// <param name="value">An 8-bit signed integer.</param>
		/// <returns>A System.Half that represents the converted 8-bit signed integer.</returns>
		public static implicit operator Half(sbyte value) => new((float)value);

		/// <summary>
		/// Converts a 16-bit unsigned integer to a System.Half.
		/// </summary>
		/// <param name="value">A 16-bit unsigned integer.</param>
		/// <returns>A System.Half that represents the converted 16-bit unsigned integer.</returns>
		public static implicit operator Half(ushort value) => new((float)value);

		/// <summary>
		/// Converts a 32-bit unsigned integer to a System.Half.
		/// </summary>
		/// <param name="value">A 32-bit unsigned integer.</param>
		/// <returns>A System.Half that represents the converted 32-bit unsigned integer.</returns>
		public static implicit operator Half(uint value) => new((float)value);

		/// <summary>
		/// Converts a 64-bit unsigned integer to a System.Half.
		/// </summary>
		/// <param name="value">A 64-bit unsigned integer.</param>
		/// <returns>A System.Half that represents the converted 64-bit unsigned integer.</returns>
		public static implicit operator Half(ulong value) => new((float)value);

		/// <summary>
		/// Converts a System.Half to an 8-bit signed integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>An 8-bit signed integer that represents the converted System.Half.</returns>
		public static explicit operator sbyte(Half value) => (sbyte)(float)value;
		/// <summary>
		/// Converts a System.Half to a 16-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 16-bit unsigned integer that represents the converted System.Half.</returns>
		public static explicit operator ushort(Half value) => (ushort)(float)value;
		/// <summary>
		/// Converts a System.Half to a 32-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 32-bit unsigned integer that represents the converted System.Half.</returns>
		public static explicit operator uint(Half value) => (uint)(float)value;

		/// <summary>
		/// Converts a System.Half to a 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.Half to convert.</param>
		/// <returns>A 64-bit unsigned integer that represents the converted System.Half.</returns>
		public static explicit operator ulong(Half value) => (ulong)(float)value;

		#endregion

		/// <summary>
		/// Compares this instance to a specified System.Half object.
		/// </summary>
		/// <param name="other">A System.Half object.</param>
		/// <returns>
		/// A signed number indicating the relative values of this instance and value.
		/// Return Value Meaning Less than zero This instance is less than value. Zero
		/// This instance is equal to value. Greater than zero This instance is greater than value.
		/// </returns>
		public readonly int CompareTo(Half other)
		{
			var result = 0;
			if (this < other)
			{
				result = -1;
			}
			else if (this > other)
			{
				result = 1;
			}
			else if (this != other)
			{
				if (!IsNaN(this))
				{
					result = 1;
				}
				else if (!IsNaN(other))
				{
					result = -1;
				}
			}

			return result;
		}
		/// <summary>
		/// Compares this instance to a specified System.Object.
		/// </summary>
		/// <param name="obj">An System.Object or null.</param>
		/// <returns>
		/// A signed number indicating the relative values of this instance and value.
		/// Return Value Meaning Less than zero This instance is less than value. Zero
		/// This instance is equal to value. Greater than zero This instance is greater
		/// than value. -or- value is null.
		/// </returns>
		/// <exception cref="System.ArgumentException">value is not a System.Half</exception>
		public readonly int CompareTo(object obj)
		{
			var result = obj switch
			{
				null => 1,
				Half half => CompareTo(half),
				_ => throw new ArgumentException("Object must be of type Half.")
			};

			return result;
		}
		/// <summary>
		/// Returns a value indicating whether this instance and a specified System.Half object represent the same value.
		/// </summary>
		/// <param name="other">A System.Half object to compare to this instance.</param>
		/// <returns>true if value is equal to this instance; otherwise, false.</returns>
		public readonly bool Equals(Half other) => ((other == this) || (IsNaN(other) && IsNaN(this)));

		/// <summary>
		/// Returns a value indicating whether this instance and a specified System.Object
		/// represent the same type and value.
		/// </summary>
		/// <param name="obj">An System.Object.</param>
		/// <returns>true if value is a System.Half and equal to this instance; otherwise, false.</returns>
		public readonly override bool Equals(object obj)
		{
			var result = false;
			if (obj is not Half half) return false;
			if ((half == this) || (IsNaN(half) && IsNaN(this)))
			{
				result = true;
			}
			return result;
		}
		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public readonly override int GetHashCode() => value.GetHashCode();

		/// <summary>
		/// Returns the System.TypeCode for value type System.Half.
		/// </summary>
		/// <returns>The enumerated constant (TypeCode)255.</returns>
		public readonly TypeCode GetTypeCode() => (TypeCode)255;

		#region BitConverter & Math methods for Half
		/// <summary>
		/// Returns the specified half-precision floating point value as an array of bytes.
		/// </summary>
		/// <param name="value">The number to convert.</param>
		/// <returns>An array of bytes with length 2.</returns>
		public static byte[] GetBytes(Half value) => BitConverter.GetBytes(value.value);

		/// <summary>
		/// Converts the value of a specified instance of System.Half to its equivalent binary representation.
		/// </summary>
		/// <param name="value">A System.Half value.</param>
		/// <returns>A 16-bit unsigned integer that contain the binary representation of value.</returns>        
		public static ushort GetBits(Half value) => value.value;

		/// <summary>
		/// Returns a half-precision floating point number converted from two bytes
		/// at a specified position in a byte array.
		/// </summary>
		/// <param name="value">An array of bytes.</param>
		/// <param name="startIndex">The starting position within value.</param>
		/// <returns>A half-precision floating point number formed by two bytes beginning at startIndex.</returns>
		/// <exception cref="System.ArgumentException">
		/// startIndex is greater than or equal to the length of value minus 1, and is
		/// less than or equal to the length of value minus 1.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">startIndex is less than zero or greater than the length of value minus 1.</exception>
		public static Half ToHalf(byte[] value, int startIndex) => ToHalf((ushort)BitConverter.ToInt16(value, startIndex));

		/// <summary>
		/// Returns a half-precision floating point number converted from its binary representation.
		/// </summary>
		/// <param name="bits">Binary representation of System.Half value</param>
		/// <returns>A half-precision floating point number formed by its binary representation.</returns>
		public static Half ToHalf(ushort bits) => new() { value = bits };

		/// <summary>
		/// Returns a value indicating the sign of a half-precision floating-point number.
		/// </summary>
		/// <param name="value">A signed number.</param>
		/// <returns>
		/// A number indicating the sign of value. Number Description -1 value is less
		/// than zero. 0 value is equal to zero. 1 value is greater than zero.
		/// </returns>
		/// <exception cref="System.ArithmeticException">value is equal to System.Half.NaN.</exception>
		public static int Sign(Half value)
		{
			if (IsNaN(value))
			{
				throw new ArithmeticException("Function does not accept floating point Not-a-Number values.");
			}
			return Math.Sign(value);
		}
		/// <summary>
		/// Returns the absolute value of a half-precision floating-point number.
		/// </summary>
		/// <param name="value">A number in the range System.Half.MinValue ≤ value ≤ System.Half.MaxValue.</param>
		/// <returns>A half-precision floating-point number, x, such that 0 ≤ x ≤System.Half.MaxValue.</returns>
		public static Half Abs(Half value) => HalfHelper.Abs(value);

		/// <summary>
		/// Returns the larger of two half-precision floating-point numbers.
		/// </summary>
		/// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
		/// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
		/// <returns>
		/// Parameter value1 or value2, whichever is larger. If value1, or value2, or both val1
		/// and value2 are equal to System.Half.NaN, System.Half.NaN is returned.
		/// </returns>
		public static Half Max(Half value1, Half value2) => (value1 < value2) ? value2 : value1;

		/// <summary>
		/// Returns the smaller of two half-precision floating-point numbers.
		/// </summary>
		/// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
		/// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
		/// <returns>
		/// Parameter value1 or value2, whichever is smaller. If value1, or value2, or both val1
		/// and value2 are equal to System.Half.NaN, System.Half.NaN is returned.
		/// </returns>
		public static Half Min(Half value1, Half value2) => (value1 < value2) ? value1 : value2;

		#endregion

		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to not a number (System.Half.NaN).
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if value evaluates to not a number (System.Half.NaN); otherwise, false.</returns>
		public static bool IsNaN(Half half) => HalfHelper.IsNaN(half);

		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to negative or positive infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.Half.PositiveInfinity or System.Half.NegativeInfinity; otherwise, false.</returns>
		public static bool IsInfinity(Half half) => HalfHelper.IsInfinity(half);

		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to negative infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.Half.NegativeInfinity; otherwise, false.</returns>
		public static bool IsNegativeInfinity(Half half) => HalfHelper.IsNegativeInfinity(half);

		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to positive infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.Half.PositiveInfinity; otherwise, false.</returns>
		public static bool IsPositiveInfinity(Half half) => HalfHelper.IsPositiveInfinity(half);

		#region String operations (Parse and ToString)
		/// <summary>
		/// Converts the string representation of a number to its System.Half equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <returns>The System.Half number equivalent to the number contained in value.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
		public static Half Parse(string value) => (Half)float.Parse(value, CultureInfo.InvariantCulture);

		/// <summary>
		/// Converts the string representation of a number to its System.Half equivalent 
		/// using the specified culture-specific format information.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="provider">An System.IFormatProvider that supplies culture-specific parsing information about value.</param>
		/// <returns>The System.Half number equivalent to the number contained in s as specified by provider.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
		public static Half Parse(string value, IFormatProvider provider) => (Half)float.Parse(value, provider);

		/// <summary>
		/// Converts the string representation of a number in a specified style to its System.Half equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the style elements that can be present in value. A typical value to specify is
		/// System.Globalization.NumberStyles.Number.
		/// </param>
		/// <returns>The System.Half number equivalent to the number contained in s as specified by style.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style is the
		/// System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
		public static Half Parse(string value, NumberStyles style) => (Half)float.Parse(value, style, CultureInfo.InvariantCulture);

		/// <summary>
		/// Converts the string representation of a number to its System.Half equivalent 
		/// using the specified style and culture-specific format.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the style elements that can be present in value. A typical value to specify is 
		/// System.Globalization.NumberStyles.Number.
		/// </param>
		/// <param name="provider">An System.IFormatProvider object that supplies culture-specific information about the format of value.</param>
		/// <returns>The System.Half number equivalent to the number contained in s as specified by style and provider.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style is the
		/// System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.Half.MinValue or greater than System.Half.MaxValue.</exception>
		public static Half Parse(string value, NumberStyles style, IFormatProvider provider) => (Half)float.Parse(value, style, provider);

		/// <summary>
		/// Converts the string representation of a number to its System.Half equivalent.
		/// A return value indicates whether the conversion succeeded or failed.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="result">
		/// When this method returns, contains the System.Half number that is equivalent
		/// to the numeric value contained in value, if the conversion succeeded, or is zero
		/// if the conversion failed. The conversion fails if the s parameter is null,
		/// is not a number in a valid format, or represents a number less than System.Half.MinValue
		/// or greater than System.Half.MaxValue. This parameter is passed uninitialized.
		/// </param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryParse(string value, out Half result)
		{
			if (float.TryParse(value, out var f))
			{
				result = (Half)f;
				return true;
			}
			result = new Half();
			return false;
		}
		/// <summary>
		/// Converts the string representation of a number to its System.Half equivalent
		/// using the specified style and culture-specific format. A return value indicates
		/// whether the conversion succeeded or failed.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the permitted format of value. A typical value to specify is System.Globalization.NumberStyles.Number.
		/// </param>
		/// <param name="provider">An System.IFormatProvider object that supplies culture-specific parsing information about value.</param>
		/// <param name="result">
		/// When this method returns, contains the System.Half number that is equivalent
		/// to the numeric value contained in value, if the conversion succeeded, or is zero
		/// if the conversion failed. The conversion fails if the s parameter is null,
		/// is not in a format compliant with style, or represents a number less than
		/// System.Half.MinValue or greater than System.Half.MaxValue. This parameter is passed uninitialized.
		/// </param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style 
		/// is the System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out Half result)
		{
			var parseResult = false;
			if (float.TryParse(value, style, provider, out var f))
			{
				result = (Half)f;
				parseResult = true;
			}
			else
			{
				result = new Half();
			}
			return parseResult;
		}
		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation.
		/// </summary>
		/// <returns>A string that represents the value of this instance.</returns>
		public readonly override string ToString() => ((float)this).ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation
		/// using the specified culture-specific format information.
		/// </summary>
		/// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
		/// <returns>The string representation of the value of this instance as specified by provider.</returns>
		public readonly string ToString(IFormatProvider formatProvider) => ((float)this).ToString(formatProvider);

		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
		/// </summary>
		/// <param name="format">A numeric format string.</param>
		/// <returns>The string representation of the value of this instance as specified by format.</returns>
		public readonly string ToString(string format) => ((float)this).ToString(format, CultureInfo.InvariantCulture);

		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation 
		/// using the specified format and culture-specific format information.
		/// </summary>
		/// <param name="format">A numeric format string.</param>
		/// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
		/// <returns>The string representation of the value of this instance as specified by format and provider.</returns>
		/// <exception cref="System.FormatException">format is invalid.</exception>
		public readonly string ToString(string format, IFormatProvider formatProvider) => ((float)this).ToString(format, formatProvider);

		#endregion

		#region IConvertible Members
		readonly float IConvertible.ToSingle(IFormatProvider provider) => this;

		readonly TypeCode IConvertible.GetTypeCode() => GetTypeCode();

		readonly bool IConvertible.ToBoolean(IFormatProvider provider) => Convert.ToBoolean(this);

		readonly byte IConvertible.ToByte(IFormatProvider provider) => Convert.ToByte(this);

		readonly char IConvertible.ToChar(IFormatProvider provider) => throw new InvalidCastException(
			string.Format(CultureInfo.CurrentCulture, "Invalid cast from '{0}' to '{1}'.", "Half", "Char"));

		DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new InvalidCastException(
			string.Format(CultureInfo.CurrentCulture, "Invalid cast from '{0}' to '{1}'.", "Half", "DateTime"));

		readonly decimal IConvertible.ToDecimal(IFormatProvider provider) => Convert.ToDecimal(this);

		readonly double IConvertible.ToDouble(IFormatProvider provider) => Convert.ToDouble(this);

		readonly short IConvertible.ToInt16(IFormatProvider provider) => Convert.ToInt16(this);

		readonly int IConvertible.ToInt32(IFormatProvider provider) => Convert.ToInt32(this);

		readonly long IConvertible.ToInt64(IFormatProvider provider) => Convert.ToInt64(this);

		readonly sbyte IConvertible.ToSByte(IFormatProvider provider) => Convert.ToSByte(this);

		readonly string IConvertible.ToString(IFormatProvider provider) =>
			Convert.ToString(this, CultureInfo.InvariantCulture);

		readonly object IConvertible.ToType(Type conversionType, IFormatProvider provider) =>
			(((float)this) as IConvertible).ToType(conversionType, provider);

		readonly ushort IConvertible.ToUInt16(IFormatProvider provider) => Convert.ToUInt16(this);

		readonly uint IConvertible.ToUInt32(IFormatProvider provider) => Convert.ToUInt32(this);

		readonly ulong IConvertible.ToUInt64(IFormatProvider provider) => Convert.ToUInt64(this);

		#endregion
	}
}

namespace BCnEncoder.Shared
{
	/// <summary>
	/// Helper class for Half conversions and some low level operations.
	/// This class is internally used in the Half class.
	/// </summary>
	/// <remarks>
	/// References:
	///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
	///     - Fast Half Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
	/// </remarks>
	internal static class HalfHelper
	{
		private static readonly uint[] mantissaTable = GenerateMantissaTable();
		private static readonly uint[] exponentTable = GenerateExponentTable();
		private static readonly ushort[] offsetTable = GenerateOffsetTable();
		private static readonly ushort[] baseTable = GenerateBaseTable();
		private static readonly sbyte[] shiftTable = GenerateShiftTable();

		// Transforms the subnormal representation to a normalized one. 
		private static uint ConvertMantissa(int i)
		{
			var m = (uint)(i << 13); // Zero pad mantissa bits
			uint e = 0; // Zero exponent

			// While not normalized
			while ((m & 0x00800000) == 0)
			{
				e -= 0x00800000; // Decrement exponent (1<<23)
				m <<= 1; // Shift mantissa                
			}
			m &= unchecked((uint)~0x00800000); // Clear leading 1 bit
			e += 0x38800000; // Adjust bias ((127-14)<<23)
			return m | e; // Return combined number
		}

		private static uint[] GenerateMantissaTable()
		{
			var generateMantissaTable = new uint[2048];
			generateMantissaTable[0] = 0;
			for (var i = 1; i < 1024; i++)
			{
				generateMantissaTable[i] = ConvertMantissa(i);
			}
			for (var i = 1024; i < 2048; i++)
			{
				generateMantissaTable[i] = (uint)(0x38000000 + ((i - 1024) << 13));
			}

			return generateMantissaTable;
		}
		private static uint[] GenerateExponentTable()
		{
			var generateExponentTable = new uint[64];
			generateExponentTable[0] = 0;
			for (var i = 1; i < 31; i++)
			{
				generateExponentTable[i] = (uint)(i << 23);
			}
			generateExponentTable[31] = 0x47800000;
			generateExponentTable[32] = 0x80000000;
			for (var i = 33; i < 63; i++)
			{
				generateExponentTable[i] = (uint)(0x80000000 + ((i - 32) << 23));
			}
			generateExponentTable[63] = 0xc7800000;

			return generateExponentTable;
		}
		private static ushort[] GenerateOffsetTable()
		{
			var generateOffsetTable = new ushort[64];
			generateOffsetTable[0] = 0;
			for (var i = 1; i < 32; i++)
			{
				generateOffsetTable[i] = 1024;
			}
			generateOffsetTable[32] = 0;
			for (var i = 33; i < 64; i++)
			{
				generateOffsetTable[i] = 1024;
			}

			return generateOffsetTable;
		}
		private static ushort[] GenerateBaseTable()
		{
			var generateBaseTable = new ushort[512];
			for (var i = 0; i < 256; ++i)
			{
				var e = (sbyte)(127 - i);
				switch (e)
				{
					case > 24: // Very small numbers map to zero
						generateBaseTable[i | 0x000] = 0x0000;
						generateBaseTable[i | 0x100] = 0x8000;
						break;
					case > 14: // Small numbers map to denorms
						generateBaseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
						generateBaseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
						break;
					case >= -15: // Normal numbers just lose precision
						generateBaseTable[i | 0x000] = (ushort)((15 - e) << 10);
						generateBaseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
						break;
					case > -128: // Large numbers map to Infinity
						generateBaseTable[i | 0x000] = 0x7c00;
						generateBaseTable[i | 0x100] = 0xfc00;
						break;
					default: // Infinity and NaN's stay Infinity and NaN's
						generateBaseTable[i | 0x000] = 0x7c00;
						generateBaseTable[i | 0x100] = 0xfc00;
						break;
				}
			}

			return generateBaseTable;
		}
		private static sbyte[] GenerateShiftTable()
		{
			var generateShiftTable = new sbyte[512];
			for (var i = 0; i < 256; ++i)
			{
				var e = (sbyte)(127 - i);
				switch (e)
				{
					case > 24: // Very small numbers map to zero
						generateShiftTable[i | 0x000] = 24;
						generateShiftTable[i | 0x100] = 24;
						break;
					case > 14: // Small numbers map to denorms
						generateShiftTable[i | 0x000] = (sbyte)(e - 1);
						generateShiftTable[i | 0x100] = (sbyte)(e - 1);
						break;
					case >= -15: // Normal numbers just lose precision
						generateShiftTable[i | 0x000] = 13;
						generateShiftTable[i | 0x100] = 13;
						break;
					case > -128: // Large numbers map to Infinity
						generateShiftTable[i | 0x000] = 24;
						generateShiftTable[i | 0x100] = 24;
						break;
					default: // Infinity and NaN's stay Infinity and NaN's
						generateShiftTable[i | 0x000] = 13;
						generateShiftTable[i | 0x100] = 13;
						break;
				}
			}

			return generateShiftTable;
		}

		public static unsafe float HalfToSingle(Half half)
		{
			var result = mantissaTable[offsetTable[half.value >> 10] + (half.value & 0x3ff)] + exponentTable[half.value >> 10];
			return *(float*)&result;
		}
		public static unsafe Half SingleToHalf(float single)
		{
			var value = *(uint*)&single;

			var result = (ushort)(baseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> shiftTable[value >> 23]));
			return Half.ToHalf(result);
		}

		public static Half Negate(Half half) => Half.ToHalf((ushort)(half.value ^ 0x8000));

		public static Half Abs(Half half) => Half.ToHalf((ushort)(half.value & 0x7fff));

		public static bool IsNaN(Half half) => (half.value & 0x7fff) > 0x7c00;

		public static bool IsInfinity(Half half) => (half.value & 0x7fff) == 0x7c00;

		public static bool IsPositiveInfinity(Half half) => half.value == 0x7c00;

		public static bool IsNegativeInfinity(Half half) => half.value == 0xfc00;
	}
}
