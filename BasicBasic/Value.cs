/* BasicBasic - (C) 2019 Premysl Fara 
 
BasicBasic is available under the zlib license:

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
 
 */

namespace BasicBasic
{
    using System.Globalization;


    /// <summary>
    /// A value of an expression.
    /// </summary>
    public class Value
    {
        /// <summary>
        /// The type of this value.
        /// 0 = number, 1 = string.
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// The numeric value.
        /// </summary>
        public float NumValue { get; private set; }

        /// <summary>
        /// The string value.
        /// </summary>
        public string StrValue { get; private set; }


        /// <summary>
        /// A value can not be constructed by an user.
        /// </summary>
        private Value()
        {
        }


        /// <summary>
        /// Converts this value to a string.
        /// </summary>
        /// <returns>The string representation of this value.</returns>
        public override string ToString()
        {
            if (Type == 0)
            {
                // TODO: Format the number using the ECMA-55 rules.

                return NumValue.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return StrValue;
            }
        }

        /// <summary>
        /// Converts this value to a number.
        /// </summary>
        /// <returns>The numeric representation of this value.</returns>
        public float ToNumber()
        {
            if (Type == 0)
            {
                return NumValue;
            }
            else
            {
                // TODO: Parse this number using the ParseNumber() method.

                if (float.TryParse(StrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                {
                    return n;
                }

                return 0;
            }
        }


        /// <summary>
        /// Creates a new numeric value.
        /// </summary>
        /// <param name="n">A value.</param>
        /// <returns>A new Value instance.</returns>
        public static Value Numeric(float n)
        {
            return new Value() { Type = 0, NumValue = n };
        }

        /// <summary>
        /// Creates a new string value.
        /// </summary>
        /// <param name="s">A value.</param>
        /// <returns>A new Value instance.</returns>
        public static Value String(string s)
        {
            return new Value() { Type = 1, StrValue = s };
        }
    }
}
