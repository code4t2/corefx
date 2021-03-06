// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics
{
    /// <summary>
    /// A complex number z is a number of the form z = x + yi, where x and y 
    /// are real numbers, and i is the imaginary unit, with the property i2= -1.
    /// </summary>
    [Serializable]
    public struct Complex : IEquatable<Complex>, IFormattable
    {
        public static readonly Complex Zero = new Complex(0.0, 0.0);
        public static readonly Complex One = new Complex(1.0, 0.0);
        public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

        private const double InverseOfLog10 = 0.43429448190325; // 1 / Log(10)

        // This is the largest x for which (Hypot(x,x) + x) will not overflow. It is used for branching inside Sqrt.
        private static readonly double s_sqrtRescaleThreshold = double.MaxValue / (Math.Sqrt(2.0) + 1.0);

        private double _real;
        private double _imaginary;
        
        public Complex(double real, double imaginary)
        {
            _real = real;
            _imaginary = imaginary;
        }

        public double Real { get { return _real; } }
        public double Imaginary { get { return _imaginary; } }

        public double Magnitude { get { return Abs(this); } }
        public double Phase { get { return Math.Atan2(_imaginary, _real); } }

        public static Complex FromPolarCoordinates(double magnitude, double phase)
        {
            return new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
        }

        public static Complex Negate(Complex value)
        {
            return -value;
        }

        public static Complex Add(Complex left, Complex right)
        {
            return left + right;
        }

        public static Complex Subtract(Complex left, Complex right)
        {
            return left - right;
        }

        public static Complex Multiply(Complex left, Complex right)
        {
            return left * right;
        }

        public static Complex Divide(Complex dividend, Complex divisor)
        {
            return dividend / divisor;
        }
        
        public static Complex operator -(Complex value)  /* Unary negation of a complex number */
        {
            return new Complex(-value._real, -value._imaginary);
        }
        
        public static Complex operator +(Complex left, Complex right)
        {
            return new Complex(left._real + right._real, left._imaginary + right._imaginary);
        }

        public static Complex operator -(Complex left, Complex right)
        {
            return new Complex(left._real - right._real, left._imaginary - right._imaginary);
        }

        public static Complex operator *(Complex left, Complex right)
        {
            // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
            double result_Realpart = (left._real * right._real) - (left._imaginary * right._imaginary);
            double result_Imaginarypart = (left._imaginary * right._real) + (left._real * right._imaginary);
            return new Complex(result_Realpart, result_Imaginarypart);
        }

        public static Complex operator /(Complex left, Complex right)
        {
            // Division : Smith's formula.
            double a = left._real;
            double b = left._imaginary;
            double c = right._real;
            double d = right._imaginary;

            if (Math.Abs(d) < Math.Abs(c))
            {
                double doc = d / c;
                return new Complex((a + b * doc) / (c + d * doc), (b - a * doc) / (c + d * doc));
            }
            else
            {
                double cod = c / d;
                return new Complex((b + a * cod) / (d + c * cod), (-a + b * cod) / (d + c * cod));
            }
        }

        public static double Abs(Complex value)
        {
            return Hypot(value._real, value._imaginary);
        }

        private static double Hypot(double a, double b)
        {
            // Using
            //   sqrt(a^2 + b^2) = |a| * sqrt(1 + (b/a)^2)
            // we can factor out the larger component to dodge overflow even when a * a would overflow.

            a = Math.Abs(a);
            b = Math.Abs(b);

            double small, large;
            if (a < b)
            {
                small = a;
                large = b;
            }
            else
            {
                small = b;
                large = a;
            }

            if (small == 0.0)
            {
                return (large);
            }
            else if (double.IsPositiveInfinity(large) && !double.IsNaN(small))
            {
                // The NaN test is necessary so we don't return +inf when small=NaN and large=+inf.
                // NaN in any other place returns NaN without any special handling.
                return (double.PositiveInfinity);
            }
            else
            {
                double ratio = small / large;
                return (large * Math.Sqrt(1.0 + ratio * ratio));
            }

        }

        public static Complex Conjugate(Complex value)
        {
            // Conjugate of a Complex number: the conjugate of x+i*y is x-i*y
            return new Complex(value._real, -value._imaginary);
        }

        public static Complex Reciprocal(Complex value)
        {
            // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
            if (value._real == 0 && value._imaginary == 0)
            {
                return Zero;
            }
            return One / value;
        }
        
        public static bool operator ==(Complex left, Complex right)
        {
            return left._real == right._real && left._imaginary == right._imaginary;
        }

        public static bool operator !=(Complex left, Complex right)
        {
            return left._real != right._real || left._imaginary != right._imaginary;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Complex)) return false;
            return Equals((Complex)obj);
        }

        public bool Equals(Complex value)
        {
            return _real.Equals(value._real) && _imaginary.Equals(value._imaginary);
        }

        public override int GetHashCode()
        {
            int n1 = 99999997;
            int realHash = _real.GetHashCode() % n1;
            int imaginaryHash = _imaginary.GetHashCode();
            int finalHash = realHash ^ imaginaryHash;
            return finalHash;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real, _imaginary);
        }

        public string ToString(string format)
        {
            return string.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real.ToString(format, CultureInfo.CurrentCulture), _imaginary.ToString(format, CultureInfo.CurrentCulture));
        }

        public string ToString(IFormatProvider provider)
        {
            return string.Format(provider, "({0}, {1})", _real, _imaginary);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return string.Format(provider, "({0}, {1})", _real.ToString(format, provider), _imaginary.ToString(format, provider));
        }

        public static Complex Sin(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sin(a) * Math.Cosh(b), Math.Cos(a) * Math.Sinh(b));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sinh", Justification = "Sinh is the name of a mathematical function.")]
        public static Complex Sinh(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sinh(a) * Math.Cos(b), Math.Cosh(a) * Math.Sin(b));
        }

        public static Complex Asin(Complex value)
        {
            if ((value._imaginary == 0 && value._real < 0) || value._imaginary > 0)
            {
                return -Asin(-value);
            }
            return (-ImaginaryOne) * Log(ImaginaryOne * value + Sqrt(One - value * value));
        }

        public static Complex Cos(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cos(a) * Math.Cosh(b), -(Math.Sin(a) * Math.Sinh(b)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cosh", Justification = "Cosh is the name of a mathematical function.")]
        public static Complex Cosh(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cosh(a) * Math.Cos(b), Math.Sinh(a) * Math.Sin(b));
        }

        public static Complex Acos(Complex value)
        {
            if ((value._imaginary == 0 && value._real > 0) || value._imaginary < 0)
            {
                return Math.PI - Acos(-value);
            }
            return (-ImaginaryOne) * Log(value + ImaginaryOne * Sqrt(One - (value * value)));
        }

        public static Complex Tan(Complex value)
        {
            return (Sin(value) / Cos(value));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tanh", Justification = "Tanh is the name of a mathematical function.")]
        public static Complex Tanh(Complex value)
        {
            return (Sinh(value) / Cosh(value));
        }

        public static Complex Atan(Complex value)
        {
            Complex two = new Complex(2.0, 0.0);
            return (ImaginaryOne / two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        public static Complex Log(Complex value)
        {
            return new Complex(Math.Log(Abs(value)), Math.Atan2(value._imaginary, value._real));
        }

        public static Complex Log(Complex value, double baseValue)
        {
            return Log(value) / Log(baseValue);
        }

        public static Complex Log10(Complex value)
        {
            Complex tempLog = Log(value);
            return Scale(tempLog, InverseOfLog10);
        }

        public static Complex Exp(Complex value)
        {
            double expReal = Math.Exp(value._real);
            double cosImaginary = expReal * Math.Cos(value._imaginary);
            double sinImaginary = expReal * Math.Sin(value._imaginary);
            return new Complex(cosImaginary, sinImaginary);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sqrt", Justification = "Sqrt is the name of a mathematical function.")]
        public static Complex Sqrt(Complex value)
        {

            if (value._imaginary == 0.0)
            {
                // Handle the trivial case quickly.
                if (value._real < 0.0)
                {
                    return new Complex(0.0, Math.Sqrt(-value._real));
                }
                else
                {
                    return new Complex(Math.Sqrt(value._real), 0.0);
                }
            }
            else
            {

                // One way to compute Sqrt(z) is just to call Pow(z, 0.5), which coverts to polar coordinates
                // (sqrt + atan), halves the phase, and reconverts to cartesian coordinates (cos + sin).
                // Not only is this more expensive than necessary, it also fails to preserve certain expected
                // symmetries, such as that the square root of a pure negative is a pure imaginary, and that the
                // square root of a pure imaginary has exactly equal real and imaginary parts. This all goes
                // back to the fact that Math.PI is not stored with infinite precision, so taking half of Math.PI
                // does not land us on an argument with cosine exactly equal to zero.

                // To find a fast and symmetry-respecting formula for complex square root,
                // note x + i y = \sqrt{a + i b} implies x^2 + 2 i x y - y^2 = a + i b,
                // so x^2 - y^2 = a and 2 x y = b. Cross-substitute and use the quadratic formula to obtain
                //   x = \sqrt{\frac{\sqrt{a^2 + b^2} + a}{2}}  y = \pm \sqrt{\frac{\sqrt{a^2 + b^2} - a}{2}}
                // There is just one complication: depending on the sign on a, either x or y suffers from
                // cancelation when |b| << |a|. We can get aroud this by noting that our formulas imply
                // x^2 y^2 = b^2 / 4, so |x| |y| = |b| / 2. So after computing the one that doesn't suffer
                // from cancelation, we can compute the other with just a division. This is basically just
                // the right way to evaluate the quadratic formula without cancelation.

                // All this reduces our total cost to two sqrts and a few flops, and it respects the desired
                // symmetries. Much better than atan + cos + sin!

                // The signs are a matter of choice of branch cut, which is traditionally taken so x > 0 and sign(y) = sign(b).
      
                // If the components are too large, Hypot will overflow, even though the subsequent sqrt would
                // make the result representable. To avoid this, we re-scale (by exact powers of 2 for accuracy)
                // when we encounter very large components to avoid intermediate infinities.
                bool rescale = false;
                if ((Math.Abs(value._real) >= s_sqrtRescaleThreshold) || (Math.Abs(value._imaginary) >= s_sqrtRescaleThreshold))
                {
                    if (double.IsInfinity(value._imaginary) && !double.IsNaN(value._real))
                    {
                        // We need to handle infinite imaginary parts specially because otherwise
                        // our formulas below produce inf/inf = NaN. The NaN test is necessary
                        // so that we return NaN rather than (+inf,inf) for (NaN,inf).
                        return (new Complex(double.PositiveInfinity, value._imaginary));
                    }
                    else
                    {
                        value._real *= 0.25;
                        value._imaginary *= 0.25;
                        rescale = true;
                    }
                }
 
                // This is the core of the algorithm. Everything else is special case handling.
                double x, y;
                if (value._real >= 0.0)
                {
                    x = Math.Sqrt((Hypot(value._real, value._imaginary) + value._real) * 0.5);
                    y = value._imaginary / (2.0 * x);
                }
                else
                {
                    y = Math.Sqrt((Hypot(value._real, value._imaginary) - value._real) * 0.5);
                    if (value._imaginary < 0.0) y = -y;
                    x = value._imaginary / (2.0 * y);
                }

                if (rescale)
                {
                    x *= 2.0;
                    y *= 2.0;
                }

                return new Complex(x, y);

            }
            
        }

        public static Complex Pow(Complex value, Complex power)
        {
            if (power == Zero)
            {
                return One;
            }

            if (value == Zero)
            {
                return Zero;
            }

            double valueReal = value._real;
            double valueImaginary = value._imaginary;
            double powerReal = power._real;
            double powerImaginary = power._imaginary;

            double rho = Abs(value);
            double theta = Math.Atan2(valueImaginary, valueReal);
            double newRho = powerReal * theta + powerImaginary * Math.Log(rho);

            double t = Math.Pow(rho, powerReal) * Math.Pow(Math.E, -powerImaginary * theta);

            return new Complex(t * Math.Cos(newRho), t * Math.Sin(newRho));
        }

        public static Complex Pow(Complex value, double power)
        {
            return Pow(value, new Complex(power, 0));
        }
        
        private static Complex Scale(Complex value, double factor)
        {
            double realResult = factor * value._real;
            double imaginaryResuilt = factor * value._imaginary;
            return new Complex(realResult, imaginaryResuilt);
        }

        public static implicit operator Complex(short value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(int value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(long value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(ushort value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(uint value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(ulong value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(sbyte value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(byte value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(float value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(double value)
        {
            return new Complex(value, 0.0);
        }

        public static explicit operator Complex(BigInteger value)
        {
            return new Complex((double)value, 0.0);
        }

        public static explicit operator Complex(decimal value)
        {
            return new Complex((double)value, 0.0);
        }
    }
}
