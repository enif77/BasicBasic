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
    using System;
    using System.Globalization;
    using System.Text;

    using BasicBasic.Indirect.Tokens;
    

    /// <summary>
    /// Holds information about a single program line.
    /// </summary>
    public class ProgramLine
    {
        /// <summary>
        /// The label extracted from this program line.
        /// Can be -1, if it is a program line for immediate execution from the interactive
        /// mode. Meaning - there is no label int the source at all.
        /// </summary>
        public int Label { get; set; }


        public ProgramLine()
        {
            _thisTokenPos = -1;
            _tokens = new IToken[10]; 
        }


        #region public

        /// <summary>
        /// Returns the number of tokens in this program line.
        /// </summary>
        /// <returns>The number of tokens in this program line.</returns>
        public int TokensCount()
        {
            return _thisTokenPos + 1;
        }

        /// <summary>
        /// Adds a token to this program line.
        /// </summary>
        /// <param name="token">A token.</param>
        public void AddToken(IToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            var newTokPos = _thisTokenPos + 1;
            if (newTokPos >= _tokens.Length)
            {
                var newTokens = new IToken[_tokens.Length + 10];
                for (var i = 0; i < _tokens.Length; i++)
                {
                    newTokens[i] = _tokens[i];
                }

                _tokens = newTokens;
            }

            _thisTokenPos = newTokPos;

            _tokens[_thisTokenPos] = token;          
        }

        /// <summary>
        /// Returns the current (last returned) token from this program line.
        /// Returns the end-of-file token, before the NextToken() was ever called or, 
        /// when no (more) tokens are available.
        /// </summary>
        /// <returns>The current token from this program line.</returns>
        public IToken ThisToken()
        {
            if (_thisTokenPos < 0 || _thisTokenPos >= _tokens.Length)
            {
                return new SimpleToken(TokenCode.TOK_EOF);
            }

            return _tokens[_thisTokenPos];
        }

        /// <summary>
        /// Returns the next token from this program line.
        /// Returns the end-of-file token, when no more tokens are available.
        /// </summary>
        /// <returns>The next token from this program line.</returns>
        public IToken NextToken()
        {
            var newTokPos = _thisTokenPos + 1;
            if (newTokPos >= _tokens.Length || _tokens[newTokPos] == null)
            {
                return new SimpleToken(TokenCode.TOK_EOF);
            }

            _thisTokenPos = newTokPos;

            return _tokens[_thisTokenPos];
        }

        /// <summary>
        /// Returns to the first token in this program line.
        /// </summary>
        public void Rewind()
        {
            _thisTokenPos = -1;
        }
        
        /// <summary>
        /// Returns the string representation of this program line.
        /// </summary>
        /// <returns>The string representation of this program line.</returns>
        public override string ToString()
        {
            if (_thisTokenPos < 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (Label > 0)
            {
                sb.Append(Label.ToString(CultureInfo.InvariantCulture));
                sb.Append(" ");
            }
           
            for (var i = 0; i < _tokens.Length; i++)
            {
                if (_tokens[i] == null)
                {
                    break;
                }

                sb.Append(_tokens[i]);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        #endregion


        #region private

        private int _thisTokenPos;
        private IToken[] _tokens;

        #endregion
    }
}
