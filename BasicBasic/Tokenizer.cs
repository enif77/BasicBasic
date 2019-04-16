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
    using System;
    using System.Collections.Generic;


    public class Tokenizer
    {
        #region tokens

        /// <summary>
        /// The end of the line character.
        /// </summary>
        public const char C_EOLN = '\n';

        /// <summary>
        /// The end of the line token.
        /// </summary>
        public const int TOK_EOLN = 0;

        /// <summary>
        /// A simple ('A' .. 'Z') identifier token.
        /// used for variables, arrays and user function parameters.
        /// </summary>
        public const int TOK_SVARIDNT = 4;

        /// <summary>
        /// A numeric variable ("A0" .. "Z9") identifier token.
        /// </summary>
        public const int TOK_VARIDNT = 5;

        /// <summary>
        /// A string variable ("A$" .. "Z$") identifier token.
        /// </summary>
        public const int TOK_STRIDNT = 6;

        /// <summary>
        /// A number token.
        /// </summary>
        public const int TOK_NUM = 10;

        /// <summary>
        /// A quoted string token.
        /// </summary>
        public const int TOK_QSTR = 11;

        /// <summary>
        /// A build in function name token.
        /// </summary>
        public const int TOK_FN = 12;

        /// <summary>
        /// An user defined function name ("FN?") token.
        /// </summary>
        public const int TOK_UFN = 13;

        /// <summary>
        /// A PRINT statement values list separator (';') token.
        /// </summary>
        public const int TOK_PLSTSEP = 20;

        /// <summary>
        /// A list separator (',') token.
        /// </summary>
        public const int TOK_LSTSEP = 21;

        public const int TOK_EQL = 30;   // =
        public const int TOK_NEQL = 31;  // <>
        public const int TOK_LT = 32;    // <
        public const int TOK_LTE = 33;   // <=
        public const int TOK_GT = 34;    // >
        public const int TOK_GTE = 35;   // >=

        // + - * / ^ ( )
        public const int TOK_PLUS = 40;
        public const int TOK_MINUS = 41;
        public const int TOK_MULT = 42;
        public const int TOK_DIV = 43;
        public const int TOK_POW = 44;
        public const int TOK_LBRA = 45;
        public const int TOK_RBRA = 46;

        // Keywords tokens.

        public const int TOK_KEY_BASE = 100;
        //public const int TOK_KEY_DATA = 101;
        public const int TOK_KEY_DEF = 102;
        public const int TOK_KEY_DIM = 103;
        public const int TOK_KEY_END = 104;
        //public const int TOK_KEY_FOR = 105;
        public const int TOK_KEY_GO = 106;
        public const int TOK_KEY_GOSUB = 107;
        public const int TOK_KEY_GOTO = 108;
        public const int TOK_KEY_IF = 109;
        public const int TOK_KEY_INPUT = 110;
        public const int TOK_KEY_LET = 111;
        //public const int TOK_KEY_NEXT = 112;
        //public const int TOK_KEY_ON = 113;
        public const int TOK_KEY_OPTION = 114;
        public const int TOK_KEY_PRINT = 115;
        public const int TOK_KEY_RANDOMIZE = 116;
        //public const int TOK_KEY_READ = 117;
        public const int TOK_KEY_REM = 118;
        //public const int TOK_KEY_RESTORE = 119;
        public const int TOK_KEY_RETURN = 120;
        //public const int TOK_KEY_STEP = 121;
        public const int TOK_KEY_STOP = 122;
        public const int TOK_KEY_SUB = 123;
        public const int TOK_KEY_THEN = 124;
        public const int TOK_KEY_TO = 125;

        /// <summary>
        /// The keyword - token map.
        /// </summary>
        private readonly Dictionary<string, int> _keyWordsMap = new Dictionary<string, int>()
        {
            { "BASE", TOK_KEY_BASE },
            { "DEF", TOK_KEY_DEF },
            { "DIM", TOK_KEY_DIM },
            { "END", TOK_KEY_END },
            { "GO", TOK_KEY_GO },
            { "GOSUB", TOK_KEY_GOSUB },
            { "GOTO", TOK_KEY_GOTO },
            { "IF", TOK_KEY_IF },
            { "INPUT", TOK_KEY_INPUT },
            { "LET", TOK_KEY_LET },
            { "OPTION", TOK_KEY_OPTION },
            { "PRINT", TOK_KEY_PRINT },
            { "RANDOMIZE", TOK_KEY_RANDOMIZE },
            { "REM", TOK_KEY_REM },
            { "RETURN", TOK_KEY_RETURN },
            { "STOP", TOK_KEY_STOP },
            { "SUB", TOK_KEY_SUB },
            { "THEN", TOK_KEY_THEN },
            { "TO", TOK_KEY_TO },
        };

        #endregion


        /// <summary>
        /// The program state instance this tokenizer works with.
        /// </summary>
        public ProgramState ProgramState { get; }

        /// <summary>
        /// The last found token.
        /// </summary>
        public int Token { get; private set; }

        /// <summary>
        /// A value of the TOK_NUM.
        /// </summary>
        public float NumValue { get; private set; }

        /// <summary>
        /// A value of TOK_QSTR, TOK_SVARIDNT, TOK_VARIDNT, TOK_STRIDNT, TOK_FN and TOK_UFN tokens.
        /// </summary>
        public string StrValue { get; private set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public Tokenizer(ProgramState programState)
        {
            if (programState == null) throw new ArgumentNullException(nameof(programState));

            ProgramState = programState;
            Token = 0;
            NumValue = 0;
            StrValue = null;
        }


        /// <summary>
        /// Extracts the next token found in the current program line source.
        /// </summary>
        public void NextToken()
        {
            if (ProgramState.CurrentProgramLine.SourcePosition > ProgramState.CurrentProgramLine.End)
            {
                throw ProgramState.ErrorAtLine("Read beyond the line end");
            }

            var c = NextChar();
            while (c != C_EOLN)
            {
                //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

                // Skip white chars.
                bool wasWhite = false;
                if (IsWhite(c))
                {
                    c = NextChar();
                    wasWhite = true;
                }
                
                if (IsDigit(c) || c == '.')
                {
                    ParseNumber(c);

                    return;
                }

                if (IsLetter(c))
                {
                    ParseIdent(c, wasWhite);

                    return;
                }

                if (c == '"')
                {
                    ParseQuotedString();

                    return;
                }

                switch (c)
                {
                    case '=': Token = TOK_EQL; return;

                    case '<':
                        {
                            var cc = NextChar();
                            if (cc == '>')
                            {
                                Token = TOK_NEQL;
                            }
                            else if (cc == '=')
                            {
                                Token = TOK_LTE;
                            }
                            else
                            {
                                PreviousChar();
                                Token = TOK_LT;
                            }

                            return;
                        }

                    case '>':
                        {
                            var cc = NextChar();
                            if (cc == '=')
                            {
                                Token = TOK_GTE;
                            }
                            else
                            {
                                PreviousChar();
                                Token = TOK_GT;
                            }

                            return;
                        }

                    case '+':
                        {
                            var cc = NextChar();
                            PreviousChar();

                            if (IsDigit(cc) || cc == '.')
                            {
                                ParseNumber(c);
                            }
                            else
                            {
                                Token = TOK_PLUS;
                            }

                            return;
                        }

                    case '-':
                        {
                            var cc = NextChar();
                            PreviousChar();

                            if (IsDigit(cc) || cc == '.')
                            {
                                ParseNumber(c);
                            }
                            else
                            {
                                Token = TOK_MINUS;
                            }

                            return;
                        }

                    case '*': Token = TOK_MULT; return;
                    case '/': Token = TOK_DIV; return;
                    case '^': Token = TOK_POW; return;
                    case '(': Token = TOK_LBRA; return;
                    case ')': Token = TOK_RBRA; return;
                    case ',': Token = TOK_LSTSEP; return;
                    case ';': Token = TOK_PLSTSEP; return;
                }

                c = NextChar();
            }

            Token = TOK_EOLN;
        }

        /// <summary>
        /// Parses an identifier the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character of the parsed identifier.</param>
        private void ParseIdent(char c, bool wasWhite)
        {
            var tok = TOK_SVARIDNT;
            var strValue = c.ToString();

            c = NextChar();

            if (IsDigit(c))
            {
                // A numeric Ax variable.
                StrValue = (strValue + c).ToUpperInvariant();
                tok = TOK_VARIDNT;
            }
            else if (c == '$')
            {
                // A string A$ variable.
                tok = TOK_STRIDNT;

                StrValue = (strValue + c).ToUpperInvariant();
            }
            else if (IsLetter(c))
            {
                // A key word?
                while (IsLetter(c))
                {
                    strValue += c;

                    c = NextChar();
                }

                // Go one char back, so the next time we will read the character behind this identifier.
                PreviousChar();

                strValue = strValue.ToUpperInvariant();

                if (_keyWordsMap.ContainsKey(strValue))
                {
                    // Each keyword should be preceeded by at least a single white character.
                    if (wasWhite == false)
                    {
                        throw ProgramState.ErrorAtLine("No white character before the {0} keyword found", strValue);
                    }

                    tok = _keyWordsMap[strValue];
                }
                else
                {
                    if (strValue.Length != 3)
                    {
                        throw ProgramState.ErrorAtLine("Unknown token '{0}'", strValue);
                    }

                    tok = strValue.StartsWith("FN")
                        ? TOK_UFN
                        : TOK_FN;
                }

                StrValue = strValue;
            }
            else
            {
                // A simple variable A.
                StrValue = strValue.ToUpperInvariant();

                // Go one char back, so the next time we will read the character behind this identifier.
                PreviousChar();
            }

            Token = tok;
        }

        /// <summary>
        /// Parses the quoted string using the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character ('"') of the parsed string literal.</param>
        private void ParseQuotedString()
        {
            var strValue = string.Empty;

            var c = NextChar();
            while (c != '"' && c != C_EOLN)
            {
                strValue += c;

                c = NextChar();
            }

            if (c != '"')
            {
                throw ProgramState.ErrorAtLine("Unexpected end of quoted string");
            }

            Token = TOK_QSTR;
            StrValue = strValue;
        }

        /// <summary>
        /// Parses the number using the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character of the parsed number.</param>
        private void ParseNumber(char c)
        {
            var negate = false;
            if (c == '+')
            {
                c = NextChar();
            }
            else if (c == '-')
            {
                negate = true;
                c = NextChar();
            }

            var numValue = 0.0f;
            while (IsDigit(c))
            {
                numValue = numValue * 10.0f + (c - '0');

                c = NextChar();
            }

            if (c == '.')
            {
                var exp = 0.1f;

                c = NextChar();
                while (IsDigit(c))
                {
                    numValue += (c - '0') * exp;
                    exp *= 0.1f;

                    c = NextChar();
                }
            }

            if (c == 'E')
            {
                c = NextChar();

                var negateExp = false;
                if (c == '+')
                {
                    c = NextChar();
                }
                else if (c == '-')
                {
                    negateExp = true;
                    c = NextChar();
                }

                var exp = 0f;
                while (IsDigit(c))
                {
                    exp = exp * 10.0f + (c - '0');

                    c = NextChar();
                }

                exp = negateExp ? -exp : exp;

                numValue = (float)(numValue * Math.Pow(10.0, exp));
            }

            Token = TOK_NUM;
            NumValue = negate ? -numValue : numValue;

            // Go one char back, so the next time we will read the character right behind this number.
            PreviousChar();
        }

        /// <summary>
        /// Gets the next character from the current program line source.
        /// </summary>
        /// <returns>The next character from the current program line source.</returns>
        private char NextChar()
        {
            return ProgramState.CurrentProgramLine.NextChar();
        }

        /// <summary>
        /// Gets the next character from the current program line source.
        /// </summary>
        /// <returns>The next character from the current program line source.</returns>
        private char PreviousChar()
        {
            return ProgramState.CurrentProgramLine.PreviousChar();
        }

        /// <summary>
        /// Checks, if an character is a digit.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a digit.</returns>
        public static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Checks, if an character is a white character.
        /// hwite-character = SPACE | TAB .
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a white character.</returns>
        public static bool IsWhite(char c)
        {
            return c == ' ' || c == '\t';
        }

        /// <summary>
        /// Checks, if an character is a letter.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a letter.</returns>
        public static bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        /// <summary>
        /// Checks, if an character is a plain-string-character.
        /// plain-string-character : plus-sign | minus-sign | full-stop | digit | letter
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a plain-string-character.</returns>
        public static bool IsPlainStringCharacter(char c)
        {
            return c == '+' || c == '-' || c == '.' || IsDigit(c) || IsLetter(c);
        }

        /// <summary>
        /// Checks, if an character is a unquoted-string-character.
        /// unquoted-string-character : ' ' | plain-string-character .
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is an unquoted-string-character.</returns>
        public static bool IsUquotedStringCharacter(char c)
        {
            return c == ' ' || IsPlainStringCharacter(c);
        }
    }
}
