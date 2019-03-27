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
    using System.Globalization;
    

    /// <summary>
    /// The basic Basic interpreter.
    /// </summary>
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

        private string _source;
        private int _currentProgramLinePos;
        private ProgramLine _currentProgramLine;
        private ProgramLine[] _programLines;
        private bool _wasEnd = false;


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


        private void InterpretImpl()
        {
            _wasEnd = false;

            for (var i = 0; i < _nvars.Length; i++)
            {
                _nvars[i] = 0;
            }

            for (var i = 0; i < _svars.Length; i++)
            {
                _svars[i] = null;
            }
            
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
            //Console.WriteLine("{0:000} -> {1}", programLine.Label, _source.Substring(programLine.Start, (programLine.End - programLine.Start) + 1));

            _currentProgramLine = programLine;
            _currentProgramLinePos = 0;

            var tok = NextToken();

            // The statement.
            switch (tok)
            {
                case TOK_KEY_END: return EndStatement();
                case TOK_KEY_GO:
                case TOK_KEY_GOTO:
                    return GoToStatement(tok);
                case TOK_KEY_IF: return IfStatement();
                case TOK_KEY_LET: return LetStatement();
                case TOK_KEY_PRINT: return PrintStatement();
                case TOK_KEY_REM: return RemStatement();
                case TOK_KEY_STOP: return StopStatement();

                default:
                    UnexpectedTokenError(tok);
                    break;
            }

            return null;
        }
               

        #region statements

        // The end of program.
        // END EOLN
        private ProgramLine EndStatement()
        {
            ExpToken(NextToken(), TOK_EOLN);

            var thisLine = _currentProgramLine;
            var nextLine = NextProgramLine(_currentProgramLine.Label);
            if (nextLine != null)
            {
                Error("Unexpected END statement at line {0}.", thisLine.Label);
            }

            _wasEnd = true;

            return null;
        }
               
        // GO TO line-number EOLN
        // GOTO line-number EOLN
        private ProgramLine GoToStatement(int tok)
        {
            // GO TO ...
            if (tok == TOK_KEY_GO)
            {
                // Eat TO.
                ExpToken(NextToken(), TOK_KEY_TO);
            }

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            ExpToken(NextToken(), TOK_EOLN);

            return _programLines[label - 1];
        }
        
        // IF exp1 rel exp2 THEN line-number
        // rel-num :: = <> >= <=
        // rel-str :: = <>
        private ProgramLine IfStatement()
        {
            var v1 = Expression(NextToken());

            var relTok = NextToken();

            var v2 = Expression(NextToken());

            ExpToken(NextToken(), TOK_KEY_THEN);
            
            // Get the label.
            var label = ExpLabel();

            // EOLN.
            ExpToken(NextToken(), TOK_EOLN);

            // Do not jump.
            var jump = false;

            // Numeric.
            if (v1.Type == 0 && v2.Type == 0)
            {
                switch (relTok)
                {
                    case TOK_EQL:   // =
                        jump = v1.NumValue == v2.NumValue;
                        break;

                    case TOK_NEQL:  // <>
                        jump = v1.NumValue != v2.NumValue;
                        break;

                    case TOK_LT:    // <
                        jump = v1.NumValue < v2.NumValue;
                        break;

                    case TOK_LTE:   // <=
                        jump = v1.NumValue <= v2.NumValue;
                        break;

                    case TOK_GT:    // >
                        jump = v1.NumValue > v2.NumValue;
                        break;

                    case TOK_GTE:   // >=
                        jump = v1.NumValue >= v2.NumValue;
                        break;

                    default:
                        UnexpectedTokenError(relTok);
                        break;
                }
            }
            else if (v1.Type == 0 && v2.Type == 0)
            {
                switch (relTok)
                {
                    case TOK_EQL:   // =
                        jump = v1.StrValue == v2.StrValue;
                        break;

                    case TOK_NEQL:  // <>
                        jump = v1.StrValue != v2.StrValue;
                        break;

                    default:
                        UnexpectedTokenError(relTok);
                        break;
                }
            }
            else
            {
                Error("Incompatible types in comparison at line {0}.", _currentProgramLine.Label);
            }

            return jump
                ? _programLines[label - 1] 
                : NextProgramLine(_currentProgramLine.Label);
        }

        // LET var = expr EOLN
        // var :: num-var | string-var
        private ProgramLine LetStatement()
        {
            string varName = null;

            var tok = NextToken();

            // var
            if (tok == TOK_VARIDNT || tok == TOK_STRIDNT)
            {
                varName = _strValue;
            }
            else
            {
                UnexpectedTokenError(tok);
            }

            // '=' 
            ExpToken(NextToken(), TOK_EQL);

            // expr
            SetVar(varName, Expression(NextToken()));

            // EOLN
            ExpToken(NextToken(), TOK_EOLN);

            return NextProgramLine(_currentProgramLine.Label);
        }
        
        // PRINT [ expr { print-sep expr } ] EOLN
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

            ExpToken(tok, TOK_EOLN);

            Console.WriteLine();

            return NextProgramLine(_currentProgramLine.Label);
        }
        
        // The comment.
        // REM ...
        private ProgramLine RemStatement()
        {
            return NextProgramLine(_currentProgramLine.Label);
        }
        
        // The end of execution.
        // STOP EOLN
        private ProgramLine StopStatement()
        {
            ExpToken(NextToken(), TOK_EOLN);

            _wasEnd = true;

            return null;
        }
        

        // expr :: "string" | number | var-ident
        private Value Expression(int tok)
        {
            switch (tok)
            {
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


        #region variables

        private float[] _nvars = new float[(('Z' - 'A') + 1) * 10]; // A or A0 .. A9
        private string[] _svars = new string[('Z' - 'A') + 1];      // A$ .. Z$


        // A or A5.
        // A = A0
        private float GetNVar(string varName)
        {
            var n = varName[0] - 'A';
            var x = 0;
            if (varName.Length == 2)
            {
                x = varName[1] - '0';
            }

            return _nvars[n * 10 + x];
        }


        // A$.
        private string GetSVar(string varName)
        {
            return _svars[varName[0] - 'A'] ?? string.Empty;
        }


        private void SetVar(string varName, Value v)
        {
            if (varName.EndsWith("$"))
            {
                _svars[varName[0] - 'A'] = v.ToString();
            }
            else
            {
                var n = varName[0] - 'A';
                var x = 0;
                if (varName.Length == 2)
                {
                    x = varName[1] - '0';
                }

                _nvars[n * 10 + x] = v.ToNumber();
            }
        }

        #endregion


        #region tokenizer

        #region tokens

        private const char C_EOLN = '\n';

        private const int TOK_EOLN = 0;
        private const int TOK_VARIDNT = 5;
        private const int TOK_STRIDNT = 6;
        private const int TOK_NUM = 10;
        private const int TOK_QSTR = 11;

        private const int TOK_PLSTSEP = 20;  // ;
        private const int TOK_LSTSEP = 21;  // ,

        private const int TOK_EQL = 30;   // =
        private const int TOK_NEQL = 31;  // <>
        private const int TOK_LT = 32;    // <
        private const int TOK_LTE = 33;   // <=
        private const int TOK_GT = 34;    // >
        private const int TOK_GTE = 35;   // >=

        //private const int TOK_KEY_BASE = 100;
        //private const int TOK_KEY_DATA = 101;
        //private const int TOK_KEY_DEF = 102;
        //private const int TOK_KEY_DIM = 103;
        private const int TOK_KEY_END = 104;
        //private const int TOK_KEY_FOR = 105;
        private const int TOK_KEY_GO = 106;
        //private const int TOK_KEY_GOSUB = 107;
        private const int TOK_KEY_GOTO = 108;
        private const int TOK_KEY_IF = 109;
        //private const int TOK_KEY_INPUT = 110;
        private const int TOK_KEY_LET = 111;
        //private const int TOK_KEY_NEXT = 112;
        //private const int TOK_KEY_ON = 113;
        //private const int TOK_KEY_OPTION = 114;
        private const int TOK_KEY_PRINT = 115;
        //private const int TOK_KEY_RANDOMIZE = 116;
        //private const int TOK_KEY_READ = 117;
        private const int TOK_KEY_REM = 118;
        //private const int TOK_KEY_RESTORE = 119;
        //private const int TOK_KEY_RETURN = 120;
        //private const int TOK_KEY_STEP = 121;
        private const int TOK_KEY_STOP = 122;
        //private const int TOK_KEY_SUB = 123;
        private const int TOK_KEY_THEN = 124;
        private const int TOK_KEY_TO = 125;

        private readonly Dictionary<string, int> _keyWordsMap = new Dictionary<string, int>()
        {
            { "END", TOK_KEY_END },
            { "GO", TOK_KEY_GO },
            { "GOTO", TOK_KEY_GOTO },
            { "IF", TOK_KEY_IF },
            { "LET", TOK_KEY_LET },
            { "PRINT", TOK_KEY_PRINT },
            { "REM", TOK_KEY_REM },
            { "STOP", TOK_KEY_STOP },
            { "THEN", TOK_KEY_THEN },
            { "TO", TOK_KEY_TO },
        };

        #endregion


        /// <summary>
        /// A value of the TOK_NUM.
        /// </summary>
        private float _numValue = 0.0f;

        // A value of TOK_VARIDNT, TOK_STRIDNT or TOK_KEYW tokens.
        private string _strValue = null;


        private int ExpLabel()
        {
            ExpToken(NextToken(), TOK_NUM);
            var label = (int)_numValue;

            if (label < 1 || label > MaxLabel)
            {
                throw new InterpreterException(string.Format("The label {0} at line {1} out of <1 ... {2}> rangle.", label, _currentProgramLine.Label, MaxLabel));
            }

            var target = _programLines[label - 1];
            if (target == null)
            {
                throw new InterpreterException(string.Format("Undefined label {0} at line {1}.", label, _currentProgramLine.Label));
            }

            return label;
        }


        private int ExpToken(int tok, int expTok)
        {
            if (tok != expTok)
            {
                UnexpectedTokenError(tok);
            }

            return tok;
        }


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

                if (c == '<')
                {
                    var cc = NextChar();
                    if (cc == '>')
                    {
                        return TOK_NEQL;
                    }
                    else if (cc == '=')
                    {
                        return TOK_LTE;
                    }

                    _currentProgramLinePos--;

                    return TOK_LT;
                }

                if (c == '>')
                {
                    var cc = NextChar();
                    if (cc == '=')
                    {
                        return TOK_GTE;
                    }

                    _currentProgramLinePos--;

                    return TOK_GT;
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

                _strValue = (strValue + c).ToUpperInvariant();
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

            _numValue = numValue;

            // Go one char back, so the next time we will read the character behind this number.
            _currentProgramLinePos--;

            return TOK_NUM;
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


        #region classes

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

        #endregion
                
        #endregion
    }
}

/*

Syntax
-------

{} - Repeat 0 or more.
() - Groups things together.
[] - An optional part.
|  - Or.
&  - And.
!  - Not.
:  - Definition name and definition separator.
-  - No white spaces allowed.
"" - A specific string (keyword etc.).
'' - A specific character.
.. - A range.
.  - The end of a definition.


program : { block } end-line .

block : { line | for-block } .

line : label statement end-of line .

label : digit | digit-digit .

end-of-line : '\n' .

end-line : label end-statement end-of-line .

end-statement : "END" .

statement :
  goto-statement | 
  if-then-statement |
  let-statement |
  print-statement |
  remark-statement |
  stop statement .

goto-statement : ( GO TO label ) | ( GOTO label ) .

if-then-statement : "IF" expression "THEN" label .

let-statement : "LET" variable '=' expression .

print-statement : [ print-list ] .

print-list : { print-item print-separator } print-item .

print-item : expression .

print-separator : ',' | ';' .

remark-statement : "REM" { any-character } .

stop-statement : "STOP" .

variable : numeric-variable | string-variable .

numeric-variable : leter [ - digit ] .

string-variable : leter - '$' .

leter : 'A' .. 'Z' .

digit : '0' .. '9' .

expression : number | quoted-string .

number : ( decimal-part [ - fractional-part ] ) | ( '.' - digit { - digit } ) .
    
decimal-part : digit { - digit } .

fractional-part : '.' { - digit } .

quoted-string : '"' { string-character } '"' .

string-character : ! '"' & ! end-of-line .

---

expression : numeric-expression | string-expression .

numeric-expression : [ sign ] term { sign term } .

term : factor { multiplier factor } .

factor : primary { '^' primary } .

sign : '+' | '-' .

multiplier : '*' | '/' .

primary :
  numeric-variable | 
  number | 
  '(' numeric-expression ')' .

string-expression : string-variable | string-constant .

string-constant : quoted-string .


*/
