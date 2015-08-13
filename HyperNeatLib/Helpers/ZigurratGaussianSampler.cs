using System;
using System.Diagnostics;

namespace HyperNeatLib.Helpers
{
    public class ZigguratGaussianSampler
    {
        #region Static Fields [Defaults]

        /// <summary>
        /// Number of blocks.
        /// </summary>
        const int __blockCount = 128;
        /// <summary>
        /// Right hand x coord of the base rectangle, thus also the left hand x coord of the tail 
        /// (pre-determined/computed for 128 blocks).
        /// </summary>
        const double __R = 3.442619855899;
        /// <summary>
        /// Area of each rectangle (pre-determined/computed for 128 blocks).
        /// </summary>
        const double __A = 9.91256303526217e-3;
        /// <summary>
        /// Scale factor for converting a UInt with range [0,0xffffffff] to a double with range [0,1].
        /// </summary>
        const double __UIntToU = 1.0 / (double)uint.MaxValue;

        #endregion

        #region Instance Fields

        readonly Random random;

        // _x[i] and _y[i] describe the top-right position ox rectangle i.
        readonly double[] _x;
        readonly double[] _y;

        // The proprtion of each segment that is entirely within the distribution, expressed as uint where 
        // a value of 0 indicates 0% and uint.MaxValue 100%. Expressing this as an integer allows some floating
        // points operations to be replaced with integer ones.
        readonly uint[] _xComp;

        // Useful precomputed values.
        // Area A divided by the height of B0. Note. This is *not* the same as _x[i] because the area 
        // of B0 is __A minus the area of the distribution tail.
        readonly double _A_Div_Y0;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct with a default RNG source.
        /// </summary>
        public ZigguratGaussianSampler()
            : this(new Random())
        {
        }

        /// <summary>
        /// Construct with the specified RNG seed.
        /// </summary>
        public ZigguratGaussianSampler(int seed)
            : this(new Random(seed))
        {
        }

        /// <summary>
        /// Construct with the provided RNG source.
        /// </summary>
        public ZigguratGaussianSampler(Random random)
        {
            this.random = random;

            // Initialise rectangle position data. 
            // _x[i] and _y[i] describe the top-right position ox Box i.

            // Allocate storage. We add one to the length of _x so that we have an entry at _x[_blockCount], this avoids having 
            // to do a special case test when sampling from the top box.
            _x = new double[__blockCount + 1];
            _y = new double[__blockCount];

            // Determine top right position of the base rectangle/box (the rectangle with the Gaussian tale attached). 
            // We call this Box 0 or B0 for short.
            // Note. x[0] also describes the right-hand edge of B1. (See diagram).
            _x[0] = __R;
            _y[0] = GaussianPdfDenorm(__R);

            // The next box (B1) has a right hand X edge the same as B0. 
            // Note. B1's height is the box area divided by its width, hence B1 has a smaller height than B0 because
            // B0's total area includes the attached distribution tail.
            _x[1] = __R;
            _y[1] = _y[0] + (__A / _x[1]);

            // Calc positions of all remaining rectangles.
            for (int i = 2; i < __blockCount; i++)
            {
                _x[i] = GaussianPdfDenormInv(_y[i - 1]);
                _y[i] = _y[i - 1] + (__A / _x[i]);
            }

            // For completeness we define the right-hand edge of a notional box 6 as being zero (a box with no area).
            _x[__blockCount] = 0.0;

            // Useful precomputed values.
            _A_Div_Y0 = __A / _y[0];
            _xComp = new uint[__blockCount];

            // Special case for base box. _xComp[0] stores the area of B0 as a proportion of __R 
            // (recalling that all segments have area __A, but that the base segment is the combination of B0 and the distribution tail).
            // Thus -xComp[0[ is the probability that a sample point is within the box part of the segment.
            _xComp[0] = (uint)(((__R * _y[0]) / __A) * (double)uint.MaxValue);

            for (int i = 1; i < __blockCount - 1; i++)
            {
                _xComp[i] = (uint)((_x[i + 1] / _x[i]) * (double)uint.MaxValue);
            }
            _xComp[__blockCount - 1] = 0;  // Shown for completeness.

            // Sanity check. Test that the top edge of the topmost rectangle is at y=1.0.
            // Note. We expect there to be a tiny drift away from 1.0 due to the inexactness of floating
            // point arithmetic.
            Debug.Assert(Math.Abs(1.0 - _y[__blockCount - 1]) < 1e-10);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the next sample value from the gaussian distribution.
        /// </summary>
        public double NextSample()
        {
            for (;;)
            {
                // Select box at random.
                var buffer = new byte[1];
                this.random.NextBytes(buffer);

                var u = buffer[0];

                int i = (int)(u & 0x7F);
                double sign = ((u & 0x80) == 0) ? -1.0 : 1.0;

                // Generate uniform random value with range [0,0xffffffff].
                uint u2 = (uint)(random.Next(int.MinValue, int.MaxValue));

                // Special case for the base segment.
                if (0 == i)
                {
                    if (u2 < _xComp[0])
                    {   // Generated x is within R0.
                        return u2 * __UIntToU * _A_Div_Y0 * sign;
                    }
                    // Generated x is in the tail of the distribution.
                    return SampleTail() * sign;
                }

                // All other segments.
                if (u2 < _xComp[i])
                {   // Generated x is within the rectangle.
                    return u2 * __UIntToU * _x[i] * sign;
                }

                // Generated x is outside of the rectangle.
                // Generate a random y coordinate and test if our (x,y) is within the distribution curve.
                // This execution path is relatively slow/expensive (makes a call to Math.Exp()) but relatively rarely executed,
                // although more often than the 'tail' path (above).
                double x = u2 * __UIntToU * _x[i];
                if (_y[i - 1] + ((_y[i] - _y[i - 1]) * this.random.NextDouble()) < GaussianPdfDenorm(x))
                {
                    return x * sign;
                }
            }
        }

        /// <summary>
        /// Get the next sample value from the gaussian distribution.
        /// </summary>
        /// <param name="mu">The distribution's mean.</param>
        /// <param name="mu">The distribution's standard deviation.</param>
        public double NextSample(double mu, double sigma)
        {
            return mu + (NextSample() * sigma);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sample from the distribution tail (defined as having x >= __R).
        /// </summary>
        /// <returns></returns>
        private double SampleTail()
        {
            double x, y;
            do
            {
                // Note. we use NextDoubleNonZero() because Log(0) returns NaN and will also tend to be a very slow execution path (when it occurs, which is rarely).

                var val = 0.0;

                while (val == 0.0)
                {
                    val = random.NextDouble();
                }



                x = -Math.Log(val) / __R;

                val = 0.0;

                while (val == 0.0)
                {
                    val = random.NextDouble();
                }


                y = -Math.Log(val);
            }
            while (y + y < x * x);
            return __R + x;
        }

        /// <summary>
        /// Gaussian probability density function, denormailised, that is, y = e^-(x^2/2).
        /// </summary>
        private double GaussianPdfDenorm(double x)
        {
            return Math.Exp(-(x * x / 2.0));
        }

        /// <summary>
        /// Inverse function of GaussianPdfDenorm(x)
        /// </summary>
        private double GaussianPdfDenormInv(double y)
        {
            // Operates over the y range (0,1], which happens to be the y range of the pdf, 
            // with the exception that it does not include y=0, but we would never call with 
            // y=0 so it doesn't matter. Remember that a Gaussian effectively has a tail going
            // off into x == infinity, hence asking what is x when y=0 is an invalid question
            // in the context of this class.
            return Math.Sqrt(-2.0 * Math.Log(y));
        }

        #endregion
    }
}