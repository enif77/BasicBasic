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

        public void Initialize()
        {
            _programLines = new ProgramLine[MaxLabel + 1];
            _wasEnd = false;
            _returnStack = new int[ReturnStackSize];
            _returnStackTop = -1;
            _userFns = new int['Z' - 'A'];
            _arrays = new float['Z' - 'A'][];
            _arrayBase = -1;                  // -1 = not yet user defined = 0.
            _random = new Random(20170327);
        }


        public void Interpret()
        {
            InterpretImpl();
        }


        public void Interpret(string source)
        {
            if (source == null) Error("A source expected.");

            ScanSource(source);
            InterpretImpl();
        }


        public void InterpretLine(string source)
        {
            if (source == null) Error("A source expected.");

            var programLine = new ProgramLine()
            {
                Source = source,
                Label = -1,
                Start = 0,
                End = source.Length - 1
            };

            InterpretLine(programLine);
        }


        public IEnumerable<string> ListProgramLines()
        {
            var list = new List<string>();

            for (var i = 0; i < _programLines.Length; i++)
            {
                if (_programLines[i] == null)
                {
                    continue;
                }

                var pl = _programLines[i];

                list.Add(string.Format("{0} {1}",
                    pl.Label,
                    pl.Source.Substring(pl.Start, pl.End - pl.Start)));
            }

            return list;
        }


        public void AddProgramLine(string source)
        {
            if (source == null) Error("A source expected.");

            ScanSource(source, true, true);
        }


        public void RemoveProgramLine(int label)
        {
            _programLines[label - 1] = null;
        }


        public void RemoveAllProgramLines()
        {
            for (var i = 0; i < _programLines.Length; i++)
            {
                _programLines[i] = null;
            }
        }

        #endregion


        #region private

        private int _currentProgramLinePos;
        private ProgramLine _currentProgramLine;
        private ProgramLine[] _programLines;
        private bool _wasEnd = false;
        private int[] _returnStack;
        private int _returnStackTop;
        private int[] _userFns;
        private Random _random;
               

        private void InterpretImpl()
        {
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
                case TOK_KEY_DIM: return DimStatement();
                case TOK_KEY_END: return EndStatement();
                case TOK_KEY_GO:
                case TOK_KEY_GOSUB:
                case TOK_KEY_GOTO:
                    return GoToStatement();
                case TOK_KEY_IF: return IfStatement();
                case TOK_KEY_LET: return LetStatement();
                case TOK_KEY_OPTION: return OptionStatement();
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


        private ProgramLine NextProgramLine(int fromLabel)
        {
            // Interactive mode line.
            if (fromLabel < 0)
            {
                return null;
            }

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
                ErrorAtLine("{0} function redefinition", fname);
            }

            // Save this function definition.
            _userFns[fname[2] - 'A'] = _currentProgramLine.Label;

            return NextProgramLine(_currentProgramLine.Label);
        }

        // An array definition.
        // DIM array-declaration { ',' array-declaration } EOLN
        private ProgramLine DimStatement()
        {
            EatToken(TOK_KEY_DIM);

            ArrayDeclaration();

            // ','
            while (_tok == TOK_LSTSEP)
            {
                EatToken(TOK_LSTSEP);

                ArrayDeclaration();
            }

            ExpToken(TOK_EOLN);

            return NextProgramLine(_currentProgramLine.Label);
        }

        // array-declaration : letter '(' integer ')' .
        private void ArrayDeclaration()
        {
            // Get the function name.
            ExpToken(TOK_SVARIDNT);

            var arrayName = _strValue;

            // Eat array name.
            NextToken();

            EatToken(TOK_LBRA);
            ExpToken(TOK_NUM);

            var topBound = (int)_numValue;

            CheckArray(arrayName, topBound, topBound, false);

            // Eat array upper bound.
            NextToken();

            EatToken(TOK_RBRA);
        }

        // The end of program.
        // END EOLN
        private ProgramLine EndStatement()
        {
            EatToken(TOK_KEY_END);
            ExpToken(TOK_EOLN);

            var nextLine = NextProgramLine(_currentProgramLine.Label);
            if (nextLine != null)
            {
                ErrorAtLine("Unexpected END statement");
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
                    ErrorAtLine("Return stack overflow");
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
                ErrorAtLine("Incompatible types in comparison");
            }

            return jump
                ? _programLines[label - 1] 
                : NextProgramLine(_currentProgramLine.Label);
        }

        // Sets the array bottom dimension.
        // OPTION BASE 1
        private ProgramLine OptionStatement()
        {
            if (_arrayBase >= 0)
            {
                ErrorAtLine("The OPTION BASE command already executed. Can not change the arrays lower bound");
            }

            // Eat "OPTION".
            NextToken();

            EatToken(TOK_KEY_BASE);

            // Array lower bound can not be changed, when an array is already defined.
            var arrayDefined = false;
            for (var i = 0; i < _arrays.Length; i++)
            {
                if (_arrays[i] != null)
                {
                    arrayDefined = true;

                    break;
                }
            }

            if (arrayDefined)
            {
                ErrorAtLine("An array is already defined. Can not change the arrays lower bound");
            }

            // 0 or 1.
            ExpToken(TOK_NUM);

            var option = (int)_numValue;
            if (option < 0 || option > 1)
            {
                ErrorAtLine("Array base out of allowed range 0 .. 1");
            }
            
            _arrayBase = option;

            return NextProgramLine(_currentProgramLine.Label);
        }

        // LET var = expr EOLN
        // var :: num-var | string-var
        private ProgramLine LetStatement()
        {
            EatToken(TOK_KEY_LET);

            // var
            var isSubscription = false;
            var index = 0;
            string varName = null;
            if (_tok == TOK_SVARIDNT)
            {
                varName = _strValue;

                // Eat the variable identifier.
                NextToken();

                // Array subscript.
                if (_tok == TOK_LBRA)
                {
                    NextToken();

                    index = (int)NumericExpression();

                    EatToken(TOK_RBRA);

                    CheckArray(varName, 10, index, true);

                    isSubscription = true;
                }
                else
                {
                    CheckSubsription(varName);
                }
            }
            else if (_tok == TOK_VARIDNT || _tok == TOK_STRIDNT)
            {
                varName = _strValue;

                // Eat the variable identifier.
                NextToken();
            }
            else
            {
                UnexpectedTokenError(_tok);
            }
            
            EatToken(TOK_EQL);

            // expr
            var v = Expression();

            if (isSubscription)
            {
                SetArray(varName, index, v.NumValue);
            }
            else
            {
                SetVar(varName, v);
            }
                        
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
                            ErrorAtLine("A list separator expected");
                        }
                        Console.Write(FormatValue(Expression()));
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
                ErrorAtLine("Return stack underflow");
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

                case TOK_SVARIDNT:
                    {
                        var varName = _strValue;
                        NextToken();

                        // Array subscript.
                        if (_tok == TOK_LBRA)
                        {
                            NextToken();

                            var index = (int)NumericExpression();

                            EatToken(TOK_RBRA);

                            return CheckArray(varName, 10, index, true);
                        }
                        else if (varName == pName)
                        {
                            return pValue.Value;
                        }

                        // Variable used as an array?
                        CheckSubsription(varName);

                        return GetNVar(varName);
                    }
                    
                case TOK_VARIDNT:
                    {
                        var v = GetNVar(_strValue);
                        NextToken();
                        return v;
                    }
                    
                case TOK_LBRA:
                    {
                        NextToken();
                        var v = NumericExpression();
                        EatToken(TOK_RBRA);
                        return v;
                    }
                    
                case TOK_FN:
                    {
                        float v;
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
                                ErrorAtLine("Unknown function '{0}'", fnName);
                                break;
                        }

                        return v;
                    }

                case TOK_UFN:
                    {
                        float v;
                        var fname = _strValue;
                        var flabel = _userFns[fname[2] - 'A'];
                        if (flabel == 0)
                        {
                            ErrorAtLine("Undefined user function {0}", fname);
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
                            ErrorAtLine("Unexpected {0} function definition", _strValue);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNx(X)
                        var paramName = (string)null;
                        if (_tok == TOK_LBRA)
                        {
                            if (p.HasValue == false)
                            {
                                ErrorAtLine("The {0} function expects a parameter", fname);
                            }

                            // Eat '(';
                            NextToken();

                            // A siple variable name (A .. Z) expected.
                            ExpToken(TOK_SVARIDNT);

                            paramName = _strValue;

                            NextToken();
                            EatToken(TOK_RBRA);
                        }
                        else
                        {
                            if (p.HasValue)
                            {
                                ErrorAtLine("The {0} function does not expect a parameter", fname);
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


        #region arrays

        private float[][] _arrays;
        private int _arrayBase;


        /// <summary>
        /// Checks the current state of an array and validate the acces to it.
        /// Can create a new array on demand.
        /// </summary>
        /// <param name="arrayName">An array name.</param>
        /// <param name="topBound">The top alowed array index.</param>
        /// <param name="index">The index of a value stored in the array we are interested in.</param>
        /// <param name="canExist">If true, the checked array can actually exist.</param>
        /// <returns>A value found in the array at the specific index.</returns>
        private float CheckArray(string arrayName, int topBound, int index, bool canExist)
        {
            var arrayIndex = arrayName[0] - 'A';

            // Do not redefine array.
            if (canExist == false && _arrays[arrayIndex] != null)
            {
                ErrorAtLine("Array {0} redefinition", arrayName);
            }

            var bottomBound = (_arrayBase < 0) ? 0 : _arrayBase;
            if (topBound < bottomBound)
            {
                ErrorAtLine("Array top bound ({0}) is less than the defined array bottom bound ({1})", topBound, _arrayBase);
            }

            index -= bottomBound;

            // Undefined array?
            if (_arrays[arrayIndex] == null)
            {
                _arrays[arrayIndex] = new float[topBound - bottomBound + 1];
            }

            if (index < 0 || index >= _arrays[arrayIndex].Length)
            {
                ErrorAtLine("Index {0} out of array bounds", index + bottomBound);
            }

            return _arrays[arrayIndex][index];
        }

        /// <summary>
        /// Checks, if an array is used as a variable.
        /// </summary>
        /// <param name="varName"></param>
        private void CheckSubsription(string varName)
        {
            if (_arrays[varName[0] - 'A'] != null)
            {
                ErrorAtLine("Array {0} subsciption expected", varName);
            }
        }

        /// <summary>
        /// Sets a value to a specific cell in an array.
        /// </summary>
        /// <param name="arrayName">An array name.</param>
        /// <param name="index">An index.</param>
        /// <param name="v">A value.</param>
        private void SetArray(string arrayName, int index, float v)
        {
            var bottomBound = (_arrayBase < 0) ? 0 : _arrayBase;

            _arrays[arrayName[0] - 'A'][index - bottomBound] = v;
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


        #region formatters

        private string FormatValue(Value v)
        {
            if (v.Type == 0)
            {
                return FormatNumber(v.NumValue);
            }

            return v.StrValue;
        }


        private string FormatNumber(float n)
        {
            var ns = n.ToString(CultureInfo.InvariantCulture);

            if (n < 0)
            {
                return string.Format("{0} ", ns);
            }

            return string.Format(" {0} ", ns);
        }

        #endregion


        #region tokenizer

        #region tokens

        private const char C_EOLN = '\n';

        private const int TOK_EOLN = 0;
        private const int TOK_SVARIDNT = 4;  // 'A' .. 'Z'
        private const int TOK_VARIDNT = 5; // "A0" .. "Z9"
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

        private const int TOK_KEY_BASE = 100;
        //private const int TOK_KEY_DATA = 101;
        private const int TOK_KEY_DEF = 102;
        private const int TOK_KEY_DIM = 103;
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
        private const int TOK_KEY_OPTION = 114;
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
            { "BASE", TOK_KEY_BASE },
            { "DEF", TOK_KEY_DEF },
            { "DIM", TOK_KEY_DIM },
            { "END", TOK_KEY_END },
            { "GO", TOK_KEY_GO },
            { "GOSUB", TOK_KEY_GOSUB },
            { "GOTO", TOK_KEY_GOTO },
            { "IF", TOK_KEY_IF },
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
                Error("The label {0} at line {1} is out of <1 ... {2}> rangle.", label, _currentProgramLine.Label, MaxLabel);
            }

            var target = _programLines[label - 1];
            if (target == null)
            {
                ErrorAtLine("Undefined label {0}", label);
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
                ErrorAtLine("Read beyond the line end");
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
            var tok = TOK_SVARIDNT;
            var strValue = c.ToString();

            c = NextChar();

            if (IsDigit(c))
            {
                // A numeric Ax variable.
                _strValue = (strValue + c).ToUpperInvariant();
                tok = TOK_VARIDNT;
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
                        ErrorAtLine("Unknown token '{0}'", strValue);
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
                ErrorAtLine("Unexpected end of quoted string");
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
            return _currentProgramLine.Source[_currentProgramLine.Start + _currentProgramLinePos++];
        }


        public bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }


        private bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        #endregion


        #region scanner

        private void ScanSource(string source, bool canExist = false, bool interactiveMode = false)
        {
            ProgramLine programLine = null;
            var atLineStart = true;
            var line = 1;
            var i = 0;
            for (; i < source.Length; i++)
            {
                var c = source[i];

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
                            if (i >= source.Length)
                            {
                                break;
                            }

                            c = source[i];
                        }

                        if (label < 1 || label > MaxLabel)
                        {
                            Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, MaxLabel);
                        }

                        if (canExist == false && _programLines[label - 1] != null)
                        {
                            Error("Label {0} redefinition at line {1}.", label, line);
                        }

                        // Remember this program line.
                        programLine.Source = source;
                        programLine.Label = label;
                        programLine.Start = i;

                        // Remember this line.
                        _programLines[label - 1] = programLine;

                        atLineStart = false;
                    }
                    else
                    {
                        Error("Label not found at line {0}.", line);
                    }
                }

                //// Skip white chars.
                //if (c <= ' ' && c != '\n')
                //{
                //    continue;
                //}

                if (c == C_EOLN)
                {
                    // The '\n' character.
                    programLine.End = i;

                    // Max program line length check.
                    if (programLine.Length > MaxProgramLineLength)
                    {
                        Error("The line {0} is longer than {1} characters.", line, MaxProgramLineLength);
                    }

                    // An empty line?
                    if (interactiveMode && string.IsNullOrWhiteSpace(programLine.Source.Substring(programLine.Start, programLine.End - programLine.Start)))
                    {
                        // Remove the existing program line.
                        _programLines[programLine.Label - 1] = null;
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
                Error("No line end at line {0}.", line);
            }
        }

        #endregion


        #region errors

        private void UnexpectedTokenError(int tok)
        {
            ErrorAtLine("Unexpected token {0}", tok);
        }


        private void ErrorAtLine(string message, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Error("{0} at line {1}.", message, _currentProgramLine.Label);
            }
            else
            {
                Error("{0} at line {1}.", string.Format(message, args), _currentProgramLine.Label);
            }
        }


        private void Error(string message, params object[] args)
        {
            throw new InterpreterException(string.Format(message, args));
        }

        #endregion


        #region classes

        private class ProgramLine
        {
            public string Source { get; set; }
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
  dim-statement |
  goto-statement | 
  gosub-statement | 
  if-then-statement |
  let-statement |
  option-statement |
  print-statement |
  randomize-statement |
  remark-statement |
  return-statement |
  stop statement .

def-statement : "DEF" user-function-name [ '(' parameter-name ')' ] '=' numeric-expression

user-function-name : "FNA" .. 'FNZ' .

parameter-name : 'A' .. 'Z' .

dim-statement : "DIM" array-declaration { ',' array-declaration } .

array-declaration : array-name '(' number ')' . 

array-name : 'A' .. 'Z' .

goto-statement : ( GO TO label ) | ( GOTO label ) .

gosub-statement : ( GO SUB label ) | ( GOSUB label ) .

if-then-statement : "IF" expression "THEN" label .

let-statement : "LET" variable '=' expression .

option-statement : "OPTION" "BASE" ( '0' | '1' ) .

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

primary : number | numeric-variable | numeric-function | '(' numeric-expression ')' | user-function | array-subscription .

numeric-function : numeric-function-name '(' numeric-expression ')' .

numeric-function-name : "ABS" | "ATN" | "COS" | "EXP" | "INT" | "LOG" | "RND" | "SGN" | "SIN" | "SQR" | "TAN" .

user-function : user-function-name [ '(' numeric-function ')' ] .

number : 
    ( [ sign - ] decimal-part [ - fractional-part ] [ - exponent-part ] ) | 
    ( [ sign - ] '.' - digit { - digit } [ - exponent-part ] ) .

decimal-part : digit { - digit } .

fractional-part : '.' { - digit } .

exponent.part : 'E' [ - sign ] - digit { - digit } .

array-subscription : array-name '(' numeric-expression ')' .

string-expression : string-variable | string-constant .

string-constant : quoted-string .

quoted-string : '"' { string-character } '"' .

string-character : ! '"' & ! end-of-line .

*/
