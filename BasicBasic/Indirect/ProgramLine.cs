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

namespace BasicBasic.Indirect
{
    using System.Globalization;
    using System.Text;

    using BasicBasic.Shared;
    using BasicBasic.Shared.Tokens;
    

    /// <summary>
    /// Holds information about a single program line.
    /// </summary>
    public class ProgramLine : IProgramLine
    {
        /// <summary>
        /// The label extracted from this program line.
        /// Can be -1, if it is a program line for immediate execution from the interactive
        /// mode. Meaning - there is no label int the source at all.
        /// </summary>
        public int Label { get; set; }


        public ProgramLine()
        {
            _tokens = new TokensList(); 
        }


        #region public

        /// <summary>
        /// Returns the number of tokens in this program line.
        /// </summary>
        /// <returns>The number of tokens in this program line.</returns>
        public int TokensCount()
        {
            return _tokens.Count;
        }

        /// <summary>
        /// Adds a token to this program line.
        /// </summary>
        /// <param name="token">A token.</param>
        public void AddToken(IToken token)
        {
            _tokens.Add(token);
        }

        /// <summary>
        /// Returns the current (last returned) token from this program line.
        /// Returns the end-of-file token, before the NextToken() was ever called or, 
        /// when no (more) tokens are available.
        /// </summary>
        /// <returns>The current token from this program line.</returns>
        public IToken ThisToken()
        {
            return _tokens.Current;
        }

        /// <summary>
        /// Returns the next token from this program line.
        /// Returns the end-of-file token, when no more tokens are available.
        /// </summary>
        /// <returns>The next token from this program line.</returns>
        public IToken NextToken()
        {
            return _tokens.Next();
        }

        /// <summary>
        /// Returns to the first token in this program line.
        /// </summary>
        public IProgramLine Rewind()
        {
            _tokens.Rewind();

            return this;
        }
        
        /// <summary>
        /// Returns the string representation of this program line.
        /// </summary>
        /// <returns>The string representation of this program line.</returns>
        public override string ToString()
        {
            if (_tokens.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (Label > 0)
            {
                sb.Append(Label.ToString(CultureInfo.InvariantCulture));
                sb.Append(" ");
            }
           
            foreach (var t in _tokens.ToList())
            {
                sb.Append(t);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        #endregion


        #region private

        private TokensList _tokens;

        #endregion
    }
}
