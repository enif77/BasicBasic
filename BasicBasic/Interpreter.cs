using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBasic
{
    public class Interpreter
    {
        #region constants

        public readonly int MaxLabel = 99;
        public readonly int MaxProgramLineLength = 72;  // ECMA-55

        #endregion


        #region ctor

        public Interpreter()
        {
        }

        #endregion


        #region public

        public class InterpreterException : Exception
        {
            public InterpreterException() : base()
            {
            }

            public InterpreterException(string message) : base(message)
            {
            }

            public InterpreterException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }


        public int Interpret(string source)
        {
            if (source == null) Error("A source expected.");

            _source = source;
            _programLines = new ProgramLine[MaxLabel + 1];

            ScanSource();
            InterpretImpl();
            
            return 0;
        }


        #endregion


        #region private
        
        private void InterpretImpl()
        {
            _wasEnd = false;
            _nvars = new Dictionary<string, float>();
            _svars = new Dictionary<string, string>();

            var programLine = NextProgramLine(0);
            while (programLine != null)
            {
                programLine = InterpretLine(programLine);
            }

            if (_wasEnd == false)
            {
                Error("Unexpected end of program.");
            }
        }


        private ProgramLine InterpretLine(ProgramLine programLine)
        {
            Console.WriteLine("{0:000} -> {1}", programLine.Label, _source.Substring(programLine.Start, (programLine.End - programLine.Start) + 1));

            _currentProgramLine = programLine;
            _currentProgramLinePos = 0;

            var tok = NextToken();

            // The statement.
            switch (tok)
            {
                case TOK_KEY_END: return EndStatement();
                case TOK_KEY_LET: return LetStatement();
                case TOK_KEY_PRINT: return PrintStatement();
                    
                default:
                    UnexpectedTokenError(tok);
                    break;
            }

            return null;


            //while (tok != TOK_EOLN)
            //{
            //    // Do something.
            //    if (tok == TOK_INT)
            //    {
            //        Console.WriteLine("INT " + _intValue);
            //    }
            //    else if (tok == TOK_NUM)
            //    {
            //        Console.WriteLine("NUM " + _numValue);
            //    }

            //    else if (tok == TOK_VARIDNT)
            //    {
            //        Console.WriteLine("NUMVAR " + _strValue);
            //    }
            //    else if (tok == TOK_STRIDNT)
            //    {
            //        Console.WriteLine("STRVAR " + _strValue);
            //    }
            //    else if (tok == TOK_QSTR)
            //    {
            //        Console.WriteLine("QSTR '" + _strValue + "'");
            //    }
            //    else if (tok >= TOK_FIRST_KEY && tok <= TOK_LAST_KEY)
            //    {
            //        Console.WriteLine("KEY " + _strValue);
            //    }

            //    tok = NextToken();
            //}

            //return NextProgramLine(programLine.Label);
        }

        #region statements

        // The end of execution.
        // END EOLN
        private ProgramLine EndStatement()
        {
            if (NextToken() != TOK_EOLN)
            {
                Error("Unexpected extra token at the end of the program at line {0}.", _currentProgramLine.Label);
            }

            var thisLine = _currentProgramLine;
            var nextLine = NextProgramLine(_currentProgramLine.Label);
            if (nextLine != null)
            {
                Error("Unexpected END statement at line {0}.", thisLine.Label);
            }

            _wasEnd = true;

            return null;
        }


        // LET var = expr EOLN
        // var :: num-var | string-var
        private ProgramLine LetStatement()
        {
            string varName = null;
            bool isStringVar = false;

            var tok = NextToken();

            // var
            if (tok == TOK_VARIDNT)
            {
                varName = _strValue;
            }
            else if (tok == TOK_STRIDNT)
            {
                varName = _strValue;
                isStringVar = true;
            }
            else
            {
                UnexpectedTokenError(tok);
            }

            // '=' 
            tok = NextToken();
            if (tok != TOK_EQL)
            {
                UnexpectedTokenError(tok);
            }

            // expr
            SetVar(varName, isStringVar, Expression(NextToken()));

            tok = NextToken();
            if (tok != TOK_EOLN)
            {
                UnexpectedTokenError(tok);
            }

            return NextProgramLine(_currentProgramLine.Label);
        }
                 

        // PRINT [ expr [ print-sep expr ] ] EOLN
        // print-sep :: ';' | ','
        private ProgramLine PrintStatement()
        {
            bool atSep = true;
            var tok = NextToken();
            while (tok != TOK_EOLN)
            {
                switch (tok)
                {
                    // Consume these.
                    case TOK_LSTSEP:
                    case TOK_PLSTSEP:
                        atSep = true;
                        break;

                    default:
                        if (atSep == false)
                        {
                            Error("A list separator expected at line {0}.", _currentProgramLine.Label);
                        }
                        Console.Write(Expression(tok));
                        atSep = false;
                        break;
                }

                tok = NextToken();
            }

            if (tok != TOK_EOLN)
            {
                UnexpectedTokenError(tok);
            }

            Console.WriteLine();

            return NextProgramLine(_currentProgramLine.Label);
        }


        // expr :: "string" | number | var-ident
        private Value Expression(int tok)
        {
            switch (tok)
            {
                case TOK_INT: return Value.Numeric(_intValue);
                case TOK_NUM: return Value.Numeric(_numValue);
                case TOK_QSTR: return Value.String(_strValue);
                case TOK_VARIDNT: return Value.Numeric(GetNVar(_strValue));
                case TOK_STRIDNT: return Value.String(GetSVar(_strValue));

                default:
                    UnexpectedTokenError(tok);
                    break;
            }

            return null;
        }

        #endregion


        private float GetNVar(string varName)
        {
            return _nvars.ContainsKey(varName) ? _nvars[varName] : 0;
        }


        private string GetSVar(string varName)
        {
            return _svars.ContainsKey(varName) ? _svars[varName] : string.Empty;
        }


        private void SetVar(string varName, bool isStringVar, Value v)
        {
            if (_nvars.ContainsKey(varName))
            {
                _nvars.Remove(varName);
            }

            if (isStringVar)
            {
                _svars.Add(varName, v.ToString());
            }
            else
            {
                _nvars.Add(varName, v.ToNumber());
            }
        }


        #region tokenizer

        private int NextToken()
        {
            if (_currentProgramLine.Start + _currentProgramLinePos > _currentProgramLine.End)
            {
                Error("Read beyond the line end at line {0}.", _currentProgramLine.Label);
            }

            var c = NextChar();
            while (c != C_EOLN)
            {
                //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

                if (IsDigit(c) || c == '.')
                {
                    return ParseNumber(c);
                }

                if (IsLetter(c))
                {
                    return ParseIdent(c);
                }

                if (c == '"')
                {
                    return ParseQuotedString(c);
                }

                if (c == ';')
                {
                    return TOK_PLSTSEP;
                }

                if (c == ',')
                {
                    return TOK_LSTSEP;
                }

                if (c == '=')
                {
                    return TOK_EQL;
                }

                c = NextChar();
            }

            return TOK_EOLN;
        }

        // A or A5 -> numeric var name
        // A$ -> string var name
        // A(...) -> array var name
        // PRINT -> a key word
        private int ParseIdent(char c)
        {
            var tok = TOK_VARIDNT;
            var strValue = c.ToString();

            c = NextChar();

            if (IsDigit(c))
            {
                // A numeric Ax variable.
                _strValue = (strValue + c).ToUpperInvariant();
            }
            else if (c == '$')
            {
                // A string A$ variable.
                tok = TOK_STRIDNT;

                _strValue = strValue.ToUpperInvariant();
            }
            else if (IsLetter(c))
            {
                // A key word?
                while (IsLetter(c))
                {
                    strValue += c;

                    c = NextChar();
                }

                strValue = strValue.ToUpperInvariant();

                if (_keyWordsMap.ContainsKey(strValue))
                {
                    tok = _keyWordsMap[strValue];

                    // Go one char back, so the next time we will read the character behind this identifier.
                    _currentProgramLinePos--;
                }
                else
                {
                    Error("unknown token '{0}' at line {1}.", strValue, _currentProgramLine.Label);
                }

                _strValue = strValue;
            }
            else
            {
                // A simple variable A.
                _strValue = strValue.ToUpperInvariant();

                // Go one char back, so the next time we will read the character behind this identifier.
                _currentProgramLinePos--;
            }

            return tok;
        }


        // '"' ... '"'
        private int ParseQuotedString(char c)
        {
            var strValue = string.Empty;

            c = NextChar();
            while (c != '"' && c != C_EOLN)
            {
                strValue += c;

                c = NextChar();
            }

            if (c != '"')
            {
                Error("Unexpected end of quoted string at line {0}.", _currentProgramLine.Label);
            }

            _strValue = strValue;

            return TOK_QSTR;
        }


        // 123 -> integer
        // 12.3 -> number
        // 12. -> number
        // .12 -> number
        private int ParseNumber(char c)
        {
            var tok = TOK_INT;
            var intValue = 0;
            while (IsDigit(c))
            {
                intValue = intValue * 10 + (c - '0');

                c = NextChar();
            }

            if (c == '.')
            {
                var numValue = (float)intValue;
                var exp = 0.1f;

                c = NextChar();
                while (IsDigit(c))
                {
                    numValue += (c - '0') * exp;
                    exp *= 0.1f;

                    c = NextChar();
                }

                _numValue = numValue;
                tok = TOK_NUM;
            }
            else
            {
                _intValue = intValue;
            }
            
            // Go one char back, so the next time we will read the character behind this number.
            _currentProgramLinePos--;

            return tok;
        }


        private char NextChar()
        {
            return _source[_currentProgramLine.Start + _currentProgramLinePos++];
        }


        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }


        private bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        #endregion


        private ProgramLine NextProgramLine(int fromLabel)
        {
            // Skip program lines without code.
            for (var label = fromLabel; label < _programLines.Length; label++)
            {
                if (_programLines[label] != null)
                {
                    return _programLines[label];
                }
            }

            return null;
        }


        private void ScanSource()
        {
            ProgramLine programLine = null;
            var atLineStart = true;
            var line = 1;
            var i = 0;
            for (; i < _source.Length; i++)
            {
                var c = _source[i];

                if (atLineStart)
                {
                    programLine = new ProgramLine();

                    // Label.
                    if (IsDigit(c))
                    {
                        var label = 0;
                        while (IsDigit(c))
                        {
                            label = label * 10 + (c - '0');

                            i++;
                            if (i >= _source.Length)
                            {
                                break;
                            }

                            c = _source[i];
                        }

                        if (label < 1 || label > MaxLabel)
                        {
                            throw new InterpreterException(string.Format("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, MaxLabel));
                        }

                        if (_programLines[label - 1] != null)
                        {
                            throw new InterpreterException(string.Format("Label {0} redefinition at line {1}.", label, line));
                        }

                        // Remember this program line.
                        programLine.Label = label;
                        programLine.Start = i;

                        // Remember this line.
                        _programLines[label - 1] = programLine;

                        // Re read the char behind the label.
                        i--;

                        atLineStart = false;
                    }
                    else
                    {
                        throw new InterpreterException(string.Format("Label not found at line {0}.", line));
                    }
                }

                //// Skip white chars.
                //if (c <= ' ' && c != '\n')
                //{
                //    continue;
                //}

                if (c == C_EOLN)
                {
                    // The character before '\n'.
                    programLine.End = i - 1;

                    // Max program line length check.
                    if (programLine.Length > MaxProgramLineLength)
                    {
                        throw new InterpreterException(string.Format("The line {0} is longer than {1} characters.", line, MaxProgramLineLength));
                    }

                    // We are done with this line.
                    programLine = null;

                    // Starting the next line.
                    line++;
                    atLineStart = true;
                }
            }

            // The last line does not ended with the '\n' character.
            if (programLine != null)
            {
                throw new InterpreterException(string.Format("No line end at line {0}.", line));
            }
        }


        private void UnexpectedTokenError(int tok)
        {
            Error("Unexpected token {0} at line {1}.", tok, _currentProgramLine.Label);
        }


        private void Error(string message, params object[] args)
        {
            throw new InterpreterException(string.Format(message, args));
        }


        private class ProgramLine
        {
            public int Label { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public int Length { get { return (End - Start) + 1; } }


            public override string ToString()
            {
                return string.Format("{0}: {1} - {2}", Label, Start, End);
            }
        }


        private class Value
        {
            // 0 = number, 1 = string
            public int Type { get; private set; }

            public float NumValue { get; private set; }
            public string StrValue { get; private set; }


            private Value()
            {
            }


            public override string ToString()
            {
                if (Type == 0)
                {
                    return NumValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    return StrValue;
                }
            }


            public float ToNumber()
            {
                if (Type == 0)
                {
                    return NumValue;
                }
                else
                {
                    if (float.TryParse(StrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                    {
                        return n;
                    }

                    return 0;
                }
            }


            public static Value Numeric(float n)
            {
                return new Value() { Type = 0, NumValue = n };
            }


            public static Value String(string s)
            {
                return new Value() { Type = 1, StrValue = s };
            }
        }


        private string _source;
        private int _currentProgramLinePos;
        private ProgramLine _currentProgramLine;
        private ProgramLine[] _programLines;

        private bool _wasEnd = false;
        private Dictionary<string, float> _nvars;
        private Dictionary<string, string> _svars;

        private const char C_EOLN = '\n';

        private const int TOK_EOLN = 0;
        private const int TOK_VARIDNT = 5;
        private const int TOK_STRIDNT = 6;
        private const int TOK_INT = 10;
        private const int TOK_NUM = 11;
        private const int TOK_QSTR = 12;

        private const int TOK_PLSTSEP = 20;  // ;
        private const int TOK_LSTSEP = 21;  // ,

        private const int TOK_EQL = 30;  // =

        private const int TOK_FIRST_KEY = 100;
        //private const int TOK_KEY_BASE = 100;
        //private const int TOK_KEY_DATA = 101;
        //private const int TOK_KEY_DEF = 102;
        //private const int TOK_KEY_DIM = 103;
        private const int TOK_KEY_END = 104;
        //private const int TOK_KEY_FOR = 105;
        //private const int TOK_KEY_GO = 106;
        //private const int TOK_KEY_GOSUB = 107;
        //private const int TOK_KEY_GOTO = 108;
        //private const int TOK_KEY_IF = 109;
        //private const int TOK_KEY_INPUT = 110;
        private const int TOK_KEY_LET = 111;
        //private const int TOK_KEY_NEXT = 112;
        //private const int TOK_KEY_ON = 113;
        //private const int TOK_KEY_OPTION = 114;
        private const int TOK_KEY_PRINT = 115;
        //private const int TOK_KEY_RANDOMIZE = 116;
        //private const int TOK_KEY_READ = 117;
        //private const int TOK_KEY_REM = 118;
        //private const int TOK_KEY_RESTORE = 119;
        //private const int TOK_KEY_RETURN = 120;
        //private const int TOK_KEY_STEP = 121;
        //private const int TOK_KEY_STOP = 122;
        //private const int TOK_KEY_SUB = 123;
        //private const int TOK_KEY_THEN = 124;
        private const int TOK_LAST_KEY = 124;

        private readonly Dictionary<string, int> _keyWordsMap = new Dictionary<string, int>()
        {
            { "END", TOK_KEY_END },
            { "LET", TOK_KEY_LET },
            { "PRINT", TOK_KEY_PRINT },
        };

        /// <summary>
        /// A value of the TOK_INT.
        /// </summary>
        private int _intValue = 0;

        /// <summary>
        /// A value of the TOK_NUM.
        /// </summary>
        private float _numValue = 0.0f;

        // A value of TOK_VARIDNT, TOK_STRIDNT or TOK_KEYW tokens.
        private string _strValue = null;

        #endregion
    }
}

/*

5.2   _S_y_n_t_a_x

1. program          = block* end-line
2. block            = (line/for-block)*
3. line             = line-number statement end-of-line
4. line-number      = digit digit? digit? digit?
5. end-of-line      = [implementation defined]
6. end-line         = line-number end-statement end-of-line
7. end-statement    = END
8. statement        = data-statement / def-statement /
                        dimension -statement / gosub-statement /
                        goto-statement / if-then-statement /
                        input-statement / let-statement /
                        on-goto-statement / option-statement /
                        print-statement / randomize-statement /
                        read-statement / remark-statement /
                        restore-statement / return-statement /
                        stop statement 
     
*/
