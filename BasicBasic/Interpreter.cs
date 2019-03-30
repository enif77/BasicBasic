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
        public readonly int ReturnStackSize = 32;

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
        private int[] _returnStack;
        private int _returnStackTop;
        private int[] _userFns;
        private Random _random;


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
            _returnStack = new int[ReturnStackSize];
            _returnStackTop = -1;
            _userFns = new int['Z' - 'A'];
            _random = new Random(20170327);
            

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

            NextToken();

            // The statement.
            switch (_tok)
            {
                case TOK_KEY_DEF: return DefStatement();
                case TOK_KEY_END: return EndStatement();
                case TOK_KEY_GO:
                case TOK_KEY_GOSUB:
                case TOK_KEY_GOTO:
                    return GoToStatement();
                case TOK_KEY_IF: return IfStatement();
                case TOK_KEY_LET: return LetStatement();
                case TOK_KEY_PRINT: return PrintStatement();
                case TOK_KEY_RANDOMIZE: return RandomizeStatement();
                case TOK_KEY_REM: return RemStatement();
                case TOK_KEY_RETURN: return ReturnStatement();
                case TOK_KEY_STOP: return StopStatement();

                default:
                    UnexpectedTokenError(_tok);
                    break;
            }

            return null;
        }


        #region statements

        // An user defined function.
        // DEF FNx = numeric-expression EOLN
        private ProgramLine DefStatement()
        {
            EatToken(TOK_KEY_DEF);

            // Get the function name.
            ExpToken(TOK_UFN);
            var fname = _strValue;

            // Do not redefine user functions.
            if (_userFns[fname[2] - 'A'] != 0)
            {
                Error("{0} function redefinition at line {1}.", fname, _currentProgramLine.Label);
            }

            // Save this function definition.
            _userFns[fname[2] - 'A'] = _currentProgramLine.Label;

            return NextProgramLine(_currentProgramLine.Label);
        }

        // The end of program.
        // END EOLN
        private ProgramLine EndStatement()
        {
            EatToken(TOK_KEY_END);
            ExpToken(TOK_EOLN);

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
        // GO SUB line-number EOLN
        // GOSUB line-number EOLN
        private ProgramLine GoToStatement()
        {
            var gosub = false;

            // GO TO or GO SUB ...
            if (_tok == TOK_KEY_GO)
            {
                // Eat TO.
                NextToken();

                // GO SUB?
                if (_tok == TOK_KEY_SUB)
                {
                    gosub = true;
                }
                else
                {
                    ExpToken(TOK_KEY_TO);
                }
            }
            else if (_tok == TOK_KEY_GOSUB)
            {
                gosub = true;
            }

            // Eat the statement.
            NextToken();

            // Get the label.
            var label = ExpLabel();
            NextToken();

            // EOLN.
            ExpToken(TOK_EOLN);

            if (gosub)
            {
                _returnStackTop++;
                if (_returnStackTop >= _returnStack.Length)
                {
                    Error("Return stack overflow.");
                }

                _returnStack[_returnStackTop] = _currentProgramLine.Label;
            }

            return _programLines[label - 1];
        }
        
        // IF exp1 rel exp2 THEN line-number
        // rel-num :: = <> >= <=
        // rel-str :: = <>
        private ProgramLine IfStatement()
        {
            EatToken(TOK_KEY_IF);
            
            var v1 = Expression();

            var relTok = _tok;
            NextToken();

            var v2 = Expression();

            EatToken(TOK_KEY_THEN);

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            NextToken();
            ExpToken(TOK_EOLN);

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
            EatToken(TOK_KEY_LET);

            // var
            string varName = null;
            if (_tok == TOK_VARIDNT || _tok == TOK_STRIDNT)
            {
                varName = _strValue;
            }
            else
            {
                UnexpectedTokenError(_tok);
            }

            // Eat the variable identifier.
            NextToken();

            EatToken(TOK_EQL);

            // expr
            SetVar(varName, Expression());
            
            // EOLN
            ExpToken(TOK_EOLN);

            return NextProgramLine(_currentProgramLine.Label);
        }
        
        // PRINT [ expr { print-sep expr } ] EOLN
        // print-sep :: ';' | ','
        private ProgramLine PrintStatement()
        {
            // Eat PRINT.
            NextToken();

            bool atSep = true;
            while (_tok != TOK_EOLN)
            {
                switch (_tok)
                {
                    // Consume these.
                    case TOK_LSTSEP:
                    case TOK_PLSTSEP:
                        atSep = true;
                        NextToken();
                        break;

                    default:
                        if (atSep == false)
                        {
                            Error("A list separator expected at line {0}.", _currentProgramLine.Label);
                        }
                        Console.Write(Expression());
                        atSep = false;
                        break;
                }
            }

            ExpToken(TOK_EOLN);

            Console.WriteLine();

            return NextProgramLine(_currentProgramLine.Label);
        }

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private ProgramLine RandomizeStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);
            
            _random = new Random((int)DateTime.Now.Ticks);

            return NextProgramLine(_currentProgramLine.Label);
        }

        // The comment.
        // REM ...
        private ProgramLine RemStatement()
        {
            return NextProgramLine(_currentProgramLine.Label);
        }

        // Returns from a subroutine.
        // RETURN EOLN
        private ProgramLine ReturnStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);

            if (_returnStackTop < 0)
            {
                Error("Return stack underflow.");
            }

            return NextProgramLine(_returnStack[_returnStackTop--]);
        }

        // The end of execution.
        // STOP EOLN
        private ProgramLine StopStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);

            _wasEnd = true;

            return null;
        }

        #endregion


        #region expressions

        // expr :: string-expression | numeric-expression
        private Value Expression()
        {
            if (_tok == TOK_QSTR || _tok == TOK_STRIDNT)
            {
                var s = StringExpression();

                NextToken();

                return Value.String(s);
            }

            return Value.Numeric(NumericExpression());
        }

        // string-expression : string-variable | string-constant .
        private string StringExpression()
        {
            switch (_tok)
            {
                case TOK_QSTR: return _strValue;
                case TOK_STRIDNT: return GetSVar(_strValue);

                default:
                    UnexpectedTokenError(_tok);
                    break;
            }

            return null;
        }

        // numeric-expression : [ sign ] term { sign term } .
        // term : number | numeric-variable .
        // sign : '+' | '-' .
        private float NumericExpression(string paramName = null, float? paramValue = null)
        {
            var negate = false;
            if (_tok == TOK_PLUS)
            {
                NextToken();
            }
            else if (_tok == TOK_MINUS)
            {
                negate = true;
                NextToken();
            }

            var v = Term(paramName, paramValue);

            while (true)
            {
                if (_tok == TOK_PLUS)
                {
                    NextToken();

                    v += Term(paramName, paramValue);
                }
                else if (_tok == TOK_MINUS)
                {
                    NextToken();

                    v -= Term(paramName, paramValue);
                }
                else
                {
                    break;
                }
            }
                     
            return (negate) ? -v : v;
        }

        // term : factor { multiplier factor } .
        // multiplier : '*' | '/' .
        private float Term(string paramName = null, float? paramValue = null)
        {
            var v = Factor(paramName, paramValue);

            while (true)
            {
                if (_tok == TOK_MULT)
                {
                    NextToken();

                    v *= Factor(paramName, paramValue);
                }
                else if (_tok == TOK_DIV)
                {
                    NextToken();

                    var n = Factor(paramName, paramValue);

                    // TODO: Division by zero, if n = 0.

                    v /= n;
                }
                else
                {
                    break;
                }
            }

            return v;
        }

        // factor : primary { '^' primary } .
        private float Factor(string paramName = null, float? paramValue = null)
        {
            var v = Primary(paramName, paramValue);

            while (true)
            {
                if (_tok == TOK_POW)
                {
                    NextToken();

                    v = (float)Math.Pow(v, Primary(paramName, paramValue));
                }
                else
                {
                    break;
                }
            }

            return v;
        }

        // primary : number | numeric-variable | numeric-function | '(' numeric-expression ')' | user-function .
        private float Primary(string pName = null, float? pValue = null)
        {
            switch (_tok)
            {
                case TOK_NUM:
                    var s = _numValue;
                    NextToken();
                    return s;

                case TOK_VARIDNT:
                    if (_strValue == pName)
                    {
                        NextToken();
                        return pValue.Value;
                    }
                    var v = GetNVar(_strValue);
                    NextToken();
                    return v;

                case TOK_LBRA:
                    NextToken();
                    v = NumericExpression();
                    EatToken(TOK_RBRA);
                    return v;

                case TOK_FN:
                    {
                        var fnName = _strValue;
                        if (fnName == "RND")
                        {
                            NextToken();

                            return (float)_random.NextDouble();
                        }
                        else
                        {
                            NextToken();

                            EatToken(TOK_LBRA);

                            v = NumericExpression();

                            EatToken(TOK_RBRA);
                        }
                        
                        switch (fnName)
                        {
                            case "ABS":
                                v = Math.Abs(v);
                                break;

                            case "ATN":
                                v = (float)Math.Atan(v);
                                break;

                            case "COS":
                                v = (float)Math.Cos(v);
                                break;

                            case "EXP":
                                v = (float)Math.Exp(v);
                                break;

                            case "INT":
                                v = (float)Math.Floor(v);
                                break;

                            case "LOG":
                                // TODO: X must be greater than zero.
                                v = (float)Math.Log(v);
                                break;

                            case "SGN":
                                v = Math.Sign(v);
                                break;

                            case "SIN":
                                v = (float)Math.Sin(v);
                                break;

                            case "SQR":
                                // TODO: X must be nonnegative.
                                v = (float)Math.Sqrt(v);
                                break;

                            case "TAN":
                                v = (float)Math.Tan(v);
                                break;

                            default:
                                Error("Unknown function nabe '{0}'.", fnName);
                                break;
                        }

                        return v;
                    }

                case TOK_UFN:
                    {
                        var fname = _strValue;
                        var flabel = _userFns[fname[2] - 'A'];
                        if (flabel == 0)
                        {
                            Error("Undefined user function {0}.", fname);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNA(X)
                        var p = (float?)null;
                        if (_tok == TOK_LBRA)
                        {
                            NextToken();
                            p = NumericExpression();
                            EatToken(TOK_RBRA);
                        }

                        // Remember, where we are.
                        var cpl = _currentProgramLine;
                        var cplp = _currentProgramLinePos;

                        // Go to the user function definition.
                        _currentProgramLine = _programLines[flabel - 1];
                        _currentProgramLinePos = 0;

                        // DEF
                        NextToken();
                        EatToken(TOK_KEY_DEF);

                        // Function name.
                        ExpToken(TOK_UFN);

                        if (fname != _strValue)
                        {
                            Error("Unexpected function definition ({0}) at line {1}.", _strValue, _currentProgramLine.Label);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNx(X)
                        var paramName = (string)null;
                        if (_tok == TOK_LBRA)
                        {
                            if (p.HasValue == false)
                            {
                                Error("The {0} function expects a parameter.", fname);
                            }

                            // Eat '(';
                            NextToken();

                            // A siple variable name (A .. Z) expected.
                            if (_tok != TOK_VARIDNT || _strValue.Length > 1)
                            {
                                Error("A siple variable name (A .. Z) expected.");
                            }

                            paramName = _strValue;

                            NextToken();
                            EatToken(TOK_RBRA);
                        }
                        else
                        {
                            if (p.HasValue)
                            {
                                Error("The {0} function does not expect a parameter.", fname);
                            }
                        }
                                               
                        // '='
                        EatToken(TOK_EQL);

                        v = NumericExpression(paramName, p);

                        ExpToken(TOK_EOLN);

                        // Restore the previous position.
                        _currentProgramLine = cpl;
                        _currentProgramLinePos = cplp;

                        return v;
                    }
                    
                default:
                    UnexpectedTokenError(_tok);
                    break;
            }

            return float.NaN;
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
        private const int TOK_FN = 12;
        private const int TOK_UFN = 13;

        private const int TOK_PLSTSEP = 20;  // ;
        private const int TOK_LSTSEP = 21;  // ,
        
        private const int TOK_EQL = 30;   // =
        private const int TOK_NEQL = 31;  // <>
        private const int TOK_LT = 32;    // <
        private const int TOK_LTE = 33;   // <=
        private const int TOK_GT = 34;    // >
        private const int TOK_GTE = 35;   // >=

        // + - * / ^ ( )
        private const int TOK_PLUS = 40;
        private const int TOK_MINUS = 41;
        private const int TOK_MULT = 42;
        private const int TOK_DIV = 43;
        private const int TOK_POW = 44;
        private const int TOK_LBRA = 45;
        private const int TOK_RBRA = 46;

        //private const int TOK_KEY_BASE = 100;
        //private const int TOK_KEY_DATA = 101;
        private const int TOK_KEY_DEF = 102;
        //private const int TOK_KEY_DIM = 103;
        private const int TOK_KEY_END = 104;
        //private const int TOK_KEY_FOR = 105;
        private const int TOK_KEY_GO = 106;
        private const int TOK_KEY_GOSUB = 107;
        private const int TOK_KEY_GOTO = 108;
        private const int TOK_KEY_IF = 109;
        //private const int TOK_KEY_INPUT = 110;
        private const int TOK_KEY_LET = 111;
        //private const int TOK_KEY_NEXT = 112;
        //private const int TOK_KEY_ON = 113;
        //private const int TOK_KEY_OPTION = 114;
        private const int TOK_KEY_PRINT = 115;
        private const int TOK_KEY_RANDOMIZE = 116;
        //private const int TOK_KEY_READ = 117;
        private const int TOK_KEY_REM = 118;
        //private const int TOK_KEY_RESTORE = 119;
        private const int TOK_KEY_RETURN = 120;
        //private const int TOK_KEY_STEP = 121;
        private const int TOK_KEY_STOP = 122;
        private const int TOK_KEY_SUB = 123;
        private const int TOK_KEY_THEN = 124;
        private const int TOK_KEY_TO = 125;

        private readonly Dictionary<string, int> _keyWordsMap = new Dictionary<string, int>()
        {
            { "DEF", TOK_KEY_DEF },
            { "END", TOK_KEY_END },
            { "GO", TOK_KEY_GO },
            { "GOSUB", TOK_KEY_GOSUB },
            { "GOTO", TOK_KEY_GOTO },
            { "IF", TOK_KEY_IF },
            { "LET", TOK_KEY_LET },
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
        /// The last found token.
        /// </summary>
        private int _tok = 0;

        /// <summary>
        /// A value of the TOK_NUM.
        /// </summary>
        private float _numValue = 0.0f;

        // A value of TOK_QSTR, TOK_STRIDNT, TOK_FN and TOK_UFN tokens.
        private string _strValue = null;


        private int ExpLabel()
        {
            ExpToken(TOK_NUM);

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


        private void EatToken(int expTok)
        {
            ExpToken(expTok);
            NextToken();
        }


        private void ExpToken(int expTok)
        {
            if (_tok != expTok)
            {
                UnexpectedTokenError(_tok);
            }
        }


        private void NextToken()
        {
            if (_currentProgramLine.Start + _currentProgramLinePos > _currentProgramLine.End)
            {
                Error("Read beyond the line end at line {0}.", _currentProgramLine.Label);
                //_tok = TOK_EOLN;

                //return;
            }

            var c = NextChar();
            while (c != C_EOLN)
            {
                //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

                //// Skip white chars.
                //if (c <= ' ' && c != '\n')
                //{
                //    c = NextChar();
                //}

                if (IsDigit(c) || c == '.')
                {
                    ParseNumber(c);

                    return;
                }

                if (IsLetter(c))
                {
                    ParseIdent(c);

                    return;
                }

                if (c == '"')
                {
                    ParseQuotedString(c);

                    return;
                }

                switch (c)
                {
                    case ';': _tok = TOK_PLSTSEP; return;
                    case ',': _tok = TOK_LSTSEP; return;
                    case '=': _tok = TOK_EQL; return;

                    case '<':
                    {
                        var cc = NextChar();
                        if (cc == '>')
                        {
                            _tok = TOK_NEQL;
                        }
                        else if (cc == '=')
                        {
                            _tok = TOK_LTE;
                        }
                        else
                        {
                            _currentProgramLinePos--;
                            _tok = TOK_LT;
                        }
                        
                        return;
                    }

                    case '>':
                    {
                        var cc = NextChar();
                        if (cc == '=')
                        {
                            _tok = TOK_GTE;
                        }
                        else
                        {
                            _currentProgramLinePos--;
                            _tok = TOK_GT;
                        }

                        return;
                    }

                    case '+':
                    {
                        var cc = NextChar();
                        _currentProgramLinePos--;

                        if (IsDigit(cc) || cc == '.')
                        {
                            ParseNumber(c);
                        }
                        else
                        {
                            _tok = TOK_PLUS;
                        }

                        return;
                    }

                    case '-':
                    {
                        var cc = NextChar();
                        _currentProgramLinePos--;

                        if (IsDigit(cc) || cc == '.')
                        {
                            ParseNumber(c);
                        }
                        else
                        {
                            _tok = TOK_MINUS;
                        }
                            
                        return;
                    }

                    case '*': _tok = TOK_MULT; return;
                    case '/': _tok = TOK_DIV; return;
                    case '^': _tok = TOK_POW; return;
                    case '(': _tok = TOK_LBRA; return;
                    case ')': _tok = TOK_RBRA; return;
                }

                c = NextChar();
            }

            _tok = TOK_EOLN;
        }

        // A or A5 -> numeric var name
        // A$ -> string var name
        // A(...) -> array var name
        // PRINT -> a key word
        // ABS, ATN, COS, EXP, INT, LOG, RND, SGN, SIN, SQR, TAN -> TOK_FN
        // FNx -> TOK_UFN
        private void ParseIdent(char c)
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

                // Go one char back, so the next time we will read the character behind this identifier.
                _currentProgramLinePos--;

                strValue = strValue.ToUpperInvariant();
                               
                if (_keyWordsMap.ContainsKey(strValue))
                {
                    tok = _keyWordsMap[strValue];
                }
                else
                {
                    if (strValue.Length != 3)
                    {
                        Error("Unknown token '{0}' at line {1}.", strValue, _currentProgramLine.Label);
                    }

                    tok = strValue.StartsWith("FN") 
                        ? TOK_UFN 
                        : TOK_FN;
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

            _tok = tok;
        }


        // '"' ... '"'
        private void ParseQuotedString(char c)
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

            _tok = TOK_QSTR;
            _strValue = strValue;
        }


        //number : 
        //    ( [ sign - ] decimal-part [ - fractional-part ] [ - exponent-part ] ) | 
        //    ( [ sign - ] '.' - digit { - digit } [ - exponent-part ] ) .
        //
        //decimal-part : digit { - digit } .
        //
        //fractional-part : '.' { - digit } .
        //
        //exponent.part : 'E' [ - sign ] - digit { - digit } .
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

            _tok = TOK_NUM;
            _numValue = negate ? -numValue : numValue;

            // Go one char back, so the next time we will read the character behind this number.
            _currentProgramLinePos--;
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
  def-statement |
  goto-statement | 
  gosub-statement | 
  if-then-statement |
  let-statement |
  print-statement |
  randomize-statement |
  remark-statement |
  return-statement |
  stop statement .

def-statement : "DEF" user-function-name '=' numeric-expression EOLN

user-function-name : "FNA" .. 'FNZ' .

goto-statement : ( GO TO label ) | ( GOTO label ) .

gosub-statement : ( GO SUB label ) | ( GOSUB label ) .

if-then-statement : "IF" expression "THEN" label .

let-statement : "LET" variable '=' expression .

print-statement : [ print-list ] .

print-list : { print-item print-separator } print-item .

print-item : expression .

print-separator : ',' | ';' .

randomize-statement : "RANDOMIZE" .

remark-statement : "REM" { any-character } .

return-statement : "RETURN" .

stop-statement : "STOP" .

variable : numeric-variable | string-variable .

numeric-variable : leter [ - digit ] .

string-variable : leter - '$' .

leter : 'A' .. 'Z' .

digit : '0' .. '9' .

expression : numeric-expression | string-expression .

numeric-expression : [ sign ] term { sign term } .

sign : '+' | '-' .

term : factor { multiplier factor } .

multiplier : '*' | '/' .

factor : primary { '^' primary } .

primary : number | numeric-variable | numeric-function | '(' numeric-expression ')' | user-function .

numeric-function : numeric-function-name '(' numeric-expression ')' .

numeric-function-name : "ABS" | "ATN" | "COS" | "EXP" | "INT" | "LOG" | "RND" | "SGN" | "SIN" | "SQR" | "TAN" .

user-function : user-function-name .

number : 
    ( [ sign - ] decimal-part [ - fractional-part ] [ - exponent-part ] ) | 
    ( [ sign - ] '.' - digit { - digit } [ - exponent-part ] ) .

decimal-part : digit { - digit } .

fractional-part : '.' { - digit } .

exponent.part : 'E' [ - sign ] - digit { - digit } .

string-expression : string-variable | string-constant .

string-constant : quoted-string .

quoted-string : '"' { string-character } '"' .

string-character : ! '"' & ! end-of-line .

---

10.  _U_S_E_R_ _D_E_F_I_N_E_D_ _F_U_N_C_T_I_O_N_S

 10.1  _G_e_n_e_r_a_l_ _D_e_s_c_r_i_p_t_i_o_n

       In addition to the implementation supplied functions provided
       for the convenience of the programmer (see 9), BASIC allows
       the programmer to define new functions within a program.

       The general form of statements for defining functions is

                    DEF FNx = expression
       or           DEF FNx (parameter) = expression

       where x is a single letter and a parameter is a simple numeric-
       variable.

 10.2  _S_y_n_t_a_x

       1. def-statement      = DEF numeric-defined-function
                               parameter-list? equals-sign
                               numeric-expression
       2. numeric-defined-
          function           = FN letter
       3. parameter-list     = left-parenthesis parameter
                               right-parenthesis
       4. parameter          = simple-numeric-variable

 10.3  _E_x_a_m_p_l_e_s

       DEF FNF(X) = X^4 - 1    DEF FNP = 3.14159
       DEF FNA(X) = A*X + B

 10.4  _S_e_m_a_n_t_i_c_s

       A function definition specifies the means of evaluation the
       function in terms of the value of an expression involving the
       parameter appearing in the parameter-list and possibly other
       variables or constants. When the function is referenced, i.e.
       when an expression involving the function is evaluated, then
       the expression in the argument list for the function reference,
       if any, is evaluated and its value is assigned to the parameter
       in the parameter-list for the function definition (the number
       of arguments shall correspond exactly to the number of para-
       meters). The expression in the function definition is then eva-
       luated, and this value is assigned as the value of the function.

                                 - 14 -

       The parameter appearing in the parameter-list of a function
       definition is local to that definition, i.e. it is distinct
       from any variable with the same name outside of the function
       definition. Variables which do not appear in the parameter-
       list are the variables of the same name outside the function
       definition.

       A function definition shall occur in a lower numbered line
       than that of the first reference to the function. The expres-
       sion in a def-statement is not evaluated unless the defined
       function is referenced.

       If the execution of a program reaches a line containing a
       def-statement, then it shall proceed to the next line with no
       other effect.

       A function definition may refer to other defined functions,
       but not to the function being defined. A function shall be de-
       fined at most once in a program.

*/
