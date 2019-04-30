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

namespace BasicBasic.Indirect.Tokens
{
    public enum TokenCode
    {
        /// <summary>
        /// The end of the file token.
        /// </summary>
        TOK_EOF = 0,

        /// <summary>
        /// The end of the line token.
        /// </summary>
        TOK_EOLN = 1,

        /// <summary>
        /// A simple ('A' .. 'Z') identifier token.
        /// used for variables, arrays and user function parameters.
        /// </summary>
        TOK_SVARIDNT = 4,

        /// <summary>
        /// A numeric variable ("A0" .. "Z9") identifier token.
        /// </summary>
        TOK_VARIDNT = 5,

        /// <summary>
        /// A string variable ("A$" .. "Z$") identifier token.
        /// </summary>
        TOK_STRIDNT = 6,

        /// <summary>
        /// A number token.
        /// </summary>
        TOK_NUM = 10,

        /// <summary>
        /// A quoted string token.
        /// </summary>
        TOK_QSTR = 11,

        /// <summary>
        /// A build in function name token.
        /// </summary>
        TOK_FN = 12,

        /// <summary>
        /// An user defined function name ("FN?") token.
        /// </summary>
        TOK_UFN = 13,

        /// <summary>
        /// A PRINT statement values list separator (';') token.
        /// </summary>
        TOK_PLSTSEP = 20,

        /// <summary>
        /// A list separator (',') token.
        /// </summary>
        TOK_LSTSEP = 21,

        TOK_EQL = 30,   // =
        TOK_NEQL = 31,  // <>
        TOK_LT = 32,    // <
        TOK_LTE = 33,   // <=
        TOK_GT = 34,    // >
        TOK_GTE = 35,   // >=

        // + - * / ^ ( )
        TOK_PLUS = 40,
        TOK_MINUS = 41,
        TOK_MULT = 42,
        TOK_DIV = 43,
        TOK_POW = 44,
        TOK_LBRA = 45,
        TOK_RBRA = 46,

        // Keywords tokens.

        TOK_KEY_BASE = 100,
        TOK_KEY_DATA = 101,
        TOK_KEY_DEF = 102,
        TOK_KEY_DIM = 103,
        TOK_KEY_END = 104,
        //TOK_KEY_FOR = 105,
        TOK_KEY_GO = 106,
        TOK_KEY_GOSUB = 107,
        TOK_KEY_GOTO = 108,
        TOK_KEY_IF = 109,
        TOK_KEY_INPUT = 110,
        TOK_KEY_LET = 111,
        //TOK_KEY_NEXT = 112,
        TOK_KEY_ON = 113,
        TOK_KEY_OPTION = 114,
        TOK_KEY_PRINT = 115,
        TOK_KEY_RANDOMIZE = 116,
        TOK_KEY_READ = 117,
        TOK_KEY_REM = 118,
        TOK_KEY_RESTORE = 119,
        TOK_KEY_RETURN = 120,
        //TOK_KEY_STEP = 121,
        TOK_KEY_STOP = 122,
        TOK_KEY_SUB = 123,
        TOK_KEY_THEN = 124,
        TOK_KEY_TO = 125
    }


    public interface IToken
    {
        TokenCode TokenCode { get; }
        float NumValue { get; }
        string StrValue { get; }
    }
}