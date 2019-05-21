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

namespace BasicBasic.Shared
{
    using System;
    using System.Collections.Generic;

    using BasicBasic.Shared.Tokens;


    /// <summary>
    /// Represents a list of tokens.
    /// </summary>
    public class TokensList
    {
        #region properties
        
        /// <summary>
        /// Returns the number of tokens in this program line.
        /// </summary>
        public int Count
        {
            get { return _lastInsertedTokenPos + 1; }
        }
        
        /// <summary>
        /// Returns the current (last returned) token from this program line.
        /// Returns the end-of-file token, before the Next() was ever called or, 
        /// when no (more) tokens are available.
        /// </summary>
        public IToken Current
        {
            get
            {
                if (_thisTokenPos < 0 || _thisTokenPos >= _tokens.Length)
                {
                    return new SimpleToken(TokenCode.TOK_EOF);
                }

                return _tokens[_thisTokenPos];
            }
        }

        #endregion


        #region ctor

        /// <summary>
        /// Constructor.
        /// </summary>
        public TokensList()
        {
            Clear();
        }

        #endregion


        #region public

        /// <summary>
        /// Removes all tokens from this list.
        /// </summary>
        public void Clear()
        {
            _thisTokenPos = -1;
            _lastInsertedTokenPos = -1;
            _tokens = new IToken[10];
        }

        /// <summary>
        /// Adds a token to this program line.
        /// </summary>
        /// <param name="token">A token.</param>
        public void Add(IToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            var newTokPos = _lastInsertedTokenPos + 1;
            if (newTokPos >= _tokens.Length)
            {
                var newTokens = new IToken[_tokens.Length + 10];
                for (var i = 0; i < _tokens.Length; i++)
                {
                    newTokens[i] = _tokens[i];
                }

                _tokens = newTokens;
            }

            _lastInsertedTokenPos = newTokPos;
            _thisTokenPos = newTokPos;

            _tokens[_lastInsertedTokenPos] = token;
        }
        
        /// <summary>
        /// Returns the next token from this program line.
        /// Returns the end-of-file token, when no more tokens are available.
        /// </summary>
        /// <returns>The next token from this program line.</returns>
        public IToken Next()
        {
            var newTokPos = _thisTokenPos + 1;
            if (newTokPos > _lastInsertedTokenPos)
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
        /// Returns all tokens from this instance as a list.
        /// </summary>
        /// <returns>All tokens from this instance as a list.</returns>
        public IList<IToken> ToList()
        {
            var list = new List<IToken>(Count);

            for (var i = 0; i <= _lastInsertedTokenPos; i++)
            {
                if (_tokens[i] == null)
                {
                    break;
                }

                list.Add(_tokens[i]);
            }

            return list;
        }

        #endregion


        #region private

        private int _thisTokenPos;
        private int _lastInsertedTokenPos;
        private IToken[] _tokens;

        #endregion
    }
}
