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
    using System.Collections.Generic;
    using System.Text;
    using BasicBasic.Indirect.Tokens;
    

    /// <summary>
    /// Breaks a program source into tokens.
    /// </summary>
    public class Tokenizer
    {
        #region tokens

        /// <summary>
        /// The end of the source character.
        /// </summary>
        public const char C_EOF = (char)0;

        /// <summary>
        /// The end of the line character.
        /// </summary>
        public const char C_EOLN = '\n';

        /// <summary>
        /// The keyword - token map.
        /// </summary>
        private readonly Dictionary<string, TokenCode> _keyWordsMap = new Dictionary<string, TokenCode>()
        {
            { "BASE", TokenCode.TOK_KEY_BASE },
            { "DATA", TokenCode.TOK_KEY_DATA },
            { "DEF", TokenCode.TOK_KEY_DEF },
            { "DIM", TokenCode.TOK_KEY_DIM },
            { "END", TokenCode.TOK_KEY_END },
            { "GO", TokenCode.TOK_KEY_GO },
            { "GOSUB", TokenCode.TOK_KEY_GOSUB },
            { "GOTO", TokenCode.TOK_KEY_GOTO },
            { "IF", TokenCode.TOK_KEY_IF },
            { "INPUT", TokenCode.TOK_KEY_INPUT },
            { "LET", TokenCode.TOK_KEY_LET },
            { "ON", TokenCode.TOK_KEY_ON },
            { "OPTION", TokenCode.TOK_KEY_OPTION },
            { "PRINT", TokenCode.TOK_KEY_PRINT },
            { "RANDOMIZE", TokenCode.TOK_KEY_RANDOMIZE },
            { "READ", TokenCode.TOK_KEY_READ },
            { "REM", TokenCode.TOK_KEY_REM },
            { "RESTORE", TokenCode.TOK_KEY_RESTORE },
            { "RETURN", TokenCode.TOK_KEY_RETURN },
            { "STOP", TokenCode.TOK_KEY_STOP },
            { "SUB", TokenCode.TOK_KEY_SUB },
            { "THEN", TokenCode.TOK_KEY_THEN },
            { "TO", TokenCode.TOK_KEY_TO },

            // Controll commands of the interactive mode.
            { "BY", TokenCode.TOK_KEY_BY },
            { "QUIT", TokenCode.TOK_KEY_QUIT },
            { "RUN", TokenCode.TOK_KEY_RUN },
            { "NEW", TokenCode.TOK_KEY_NEW },
            { "LIST", TokenCode.TOK_KEY_LIST },
            { "CLS", TokenCode.TOK_KEY_CLS }
        };

        #endregion


        /// <summary>
        /// The program state instance this tokenizer works with.
        /// </summary>
        public ProgramState ProgramState { get; }


        private string _source;

        /// <summary>
        /// The currentlly parsed source.
        /// </summary>
        public string Source
        {
            get
            {
                return _source;
            }

            set
            {
                _source = value ?? string.Empty;

                SourcePosition = -1;
            }
        }

        /// <summary>
        /// The current source position (from where was the last character).
        /// </summary>
        public int SourcePosition  { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Tokenizer(ProgramState programState)
        {
            if (programState == null) throw new ArgumentNullException(nameof(programState));

            ProgramState = programState;
        }


        /// <summary>
        /// Skips everything till the next end of line or end of file.
        /// </summary>
        /// <returns>All skipped characters as a string.</returns>
        public string SkipToEoln()
        {
            if (SourcePosition >= Source.Length)
            {
                throw ProgramState.ErrorAtLine("Read beyond the Source end");
            }

            var sb = new StringBuilder();
            var c = NextChar();
            while (c != C_EOF)
            {
                if (c == C_EOLN)
                {
                    PreviousChar();

                    break;
                }

                // Ignore the CR character.
                if (c != '\r')
                {
                    sb.Append(c);
                }
                
                c = NextChar();
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Extracts the next token found in the program source.
        /// </summary>
        public IToken NextToken(bool withUnquotedStrings)
        {
            if (SourcePosition >= Source.Length)
            {
                throw ProgramState.ErrorAtLine("Read beyond the Source end");
            }

            var c = NextChar();
            while (c != C_EOF)
            {
                //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

                // Skip white chars.
                bool wasWhite = false;
                while (IsWhite(c))
                {
                    c = NextChar();
                    wasWhite = true;
                }

                if (IsDigit(c) || c == '.')
                {
                    return ParseNumber(c);
                }

                if (IsLetter(c))
                {
                    return ParseIdent(c, wasWhite);
                }

                if (c == '"')
                {
                    return ParseQuotedString();
                }

                switch (c)
                {
                    case '=': return new SimpleToken(TokenCode.TOK_EQL);

                    case '<':
                        {
                            var cc = NextChar();
                            if (cc == '>')
                            {
                                return new SimpleToken(TokenCode.TOK_NEQL);
                            }
                            else if (cc == '=')
                            {
                                return new SimpleToken(TokenCode.TOK_LTE);
                            }
                            else
                            {
                                PreviousChar();
                                return new SimpleToken(TokenCode.TOK_LT);
                            }
                        }

                    case '>':
                        {
                            var cc = NextChar();
                            if (cc == '=')
                            {
                                return new SimpleToken(TokenCode.TOK_GTE);
                            }
                            else
                            {
                                PreviousChar();
                                return new SimpleToken(TokenCode.TOK_GT);
                            }
                        }

                    case '+':
                        {
                            var cc = NextChar();
                            PreviousChar();

                            if (IsDigit(cc) || cc == '.')
                            {
                                return ParseNumber(c);
                            }
                            else
                            {
                                return new SimpleToken(TokenCode.TOK_PLUS);
                            }
                        }

                    case '-':
                        {
                            var cc = NextChar();
                            PreviousChar();

                            if (IsDigit(cc) || cc == '.')
                            {
                                return ParseNumber(c);
                            }
                            else
                            {
                                return new SimpleToken(TokenCode.TOK_MINUS);
                            }
                        }

                    case '*': return new SimpleToken(TokenCode.TOK_MULT);
                    case '/': return new SimpleToken(TokenCode.TOK_DIV);
                    case '^': return new SimpleToken(TokenCode.TOK_POW);
                    case '(': return new SimpleToken(TokenCode.TOK_LBRA);
                    case ')': return new SimpleToken(TokenCode.TOK_RBRA);
                    case ',': return new SimpleToken(TokenCode.TOK_LSTSEP);
                    case ';': return new SimpleToken(TokenCode.TOK_PLSTSEP);
                    case C_EOLN: return new SimpleToken(TokenCode.TOK_EOLN);
                }

                // TODO: Extend support for unquoted strings (for the DATA and INPUT statements).
                if (withUnquotedStrings && IsPlainStringCharacter(c))
                {
                    return ParseUnquotedString(c);
                }

                c = NextChar();
            }

            return new SimpleToken(TokenCode.TOK_EOF);
        }

        /// <summary>
        /// Parses an identifier the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character of the parsed identifier.</param>
        private IToken ParseIdent(char c, bool wasWhite)
        {
            var strValue = c.ToString();

            c = NextChar();
            if (IsDigit(c))
            {
                // A numeric Ax variable.
                return new IdentifierToken(TokenCode.TOK_VARIDNT, (strValue + c).ToUpperInvariant());
            }
            else if (c == '$')
            {
                // A string A$ variable.
                return new IdentifierToken(TokenCode.TOK_STRIDNT, (strValue + c).ToUpperInvariant());
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

                    return new SimpleToken(_keyWordsMap[strValue]);
                }
                else
                {
                    if (strValue.Length == 3)
                    {
                        return strValue.StartsWith("FN")
                            ? new IdentifierToken(TokenCode.TOK_UFN, strValue)
                            : new IdentifierToken(TokenCode.TOK_FN, strValue);
                    }

                    return new StringToken(TokenCode.TOK_UQSTR, strValue);
                }
            }
            else
            {
                // Go one char back, so the next time we will read the character behind this identifier.
                PreviousChar();

                return new IdentifierToken(TokenCode.TOK_SVARIDNT, strValue.ToUpperInvariant());
            }
        }

        /// <summary>
        /// Parses the quoted string using the ECMA-55 rules.
        /// </summary>
        private IToken ParseQuotedString()
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

            return new StringToken(TokenCode.TOK_QSTR, strValue);
        }

        /// <summary>
        /// Parses the unquoted string using the ECMA-55 rules.
        /// </summary>
        private IToken ParseUnquotedString(char c)
        {
            var strValue = c.ToString();

            c = NextChar();
            while (c != C_EOF)
            {
                // Not all characters are allowed here.
                if (IsUnquotedStringCharacter(c) == false)
                {
                    break;
                }

                strValue += c;
                c = NextChar();
            }

            // plain-string-character : plus-sign | minus-sign | full-stop | digit | letter
            // unquoted-string-character : space | plain-string-character
            // unquoted-string : plain-string-character [ { unquoted-string-character } plain-string-character ] .
            return new StringToken(TokenCode.TOK_UQSTR, strValue);
        }

        /// <summary>
        /// Parses the number using the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character of the parsed number.</param>
        private IToken ParseNumber(char c)
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

            // Go one char back, so the next time we will read the character right behind this number.
            PreviousChar();

            return new NumberToken(TokenCode.TOK_NUM, negate ? -numValue : numValue);
        }

        /// <summary>
        /// Gets the next character from the current program line source.
        /// </summary>
        /// <returns>The next character from the current program line source.</returns>
        private char NextChar()
        {
            var p = SourcePosition + 1;
            if (p >= 0 && p < Source.Length)
            {
                SourcePosition = p;

                return Source[SourcePosition];
            }
            else
            {
                SourcePosition = Source.Length;

                return C_EOF;
            }
        }

        /// <summary>
        /// Gets the next character from the current program line source.
        /// </summary>
        /// <returns>The next character from the current program line source.</returns>
        private char PreviousChar()
        {
            if (SourcePosition > 0 && SourcePosition <= Source.Length)
            {
                SourcePosition--;

                return Source[SourcePosition];
            }
            else if (SourcePosition > Source.Length)
            {
                SourcePosition = Source.Length;

                return C_EOF;
            }
            else
            {
                SourcePosition = -1;

                return C_EOF;
            }
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
        public static bool IsUnquotedStringCharacter(char c)
        {
            return c == ' ' || IsPlainStringCharacter(c);
        }
    }
}
