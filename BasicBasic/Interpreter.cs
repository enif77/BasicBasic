﻿/* BasicBasic - (C) 2019 Premysl Fara 
 
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
    /// Represents and error, that occurred during the execution of a program.
    /// </summary>
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
        #region ctor

        public Interpreter()
        {
        }

        #endregion


        #region public

        /// <summary>
        /// Initializes this interpereter instance.
        /// </summary>
        public void Initialize()
        {
            _programState = new ProgramState();
        }

        /// <summary>
        /// Interprets the currently defined program.
        /// </summary>
        public void Interpret()
        {
            InterpretImpl();
        }

        /// <summary>
        /// Scans the given sorce and interprets it.
        /// </summary>
        /// <param name="source"></param>
        public void Interpret(string source)
        {
            if (source == null) Error("A source expected.");

            ScanSource(source);
            InterpretImpl();
        }

        /// <summary>
        /// Interprets a single program line.
        /// </summary>
        /// <param name="source">A program line source.</param>
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

        /// <summary>
        /// Returns the list of defined program lines.
        /// </summary>
        /// <returns>The list of defined program lines.</returns>
        public IEnumerable<string> ListProgramLines()
        {
            return _programState.GetProgramLines();
        }

        /// <summary>
        /// Adds a new rogram line to the current program.
        /// </summary>
        /// <param name="source">A program line source.</param>
        public void AddProgramLine(string source)
        {
            if (source == null) Error("A source expected.");

            ScanSource(source, true);
        }

        /// <summary>
        /// Removes a program line from the current program.
        /// </summary>
        /// <param name="label">A program line label to be removed.</param>
        public void RemoveProgramLine(int label)
        {
            _programState.RemoveProgramLine(label);
        }

        /// <summary>
        /// Removes all program lines from this program.
        /// </summary>
        public void RemoveAllProgramLines()
        {
            _programState.RemoveAllProgramLines();
        }

        #endregion


        #region private

        private ProgramState _programState;


        /// <summary>
        /// Interprets the whole program.
        /// </summary>
        private void InterpretImpl()
        {
            var programLine = _programState.NextProgramLine(0);
            while (programLine != null)
            {
                programLine = InterpretLine(programLine);
            }

            if (_programState.WasEnd == false)
            {
                Error("Unexpected end of program.");
            }
        }

        /// <summary>
        /// Interprets a single program line.
        /// </summary>
        /// <param name="programLine">A program line.</param>
        /// <returns>The next program line to interpret.</returns>
        private ProgramLine InterpretLine(ProgramLine programLine)
        {
            //Console.WriteLine("{0:000} -> {1}", programLine.Label, _source.Substring(programLine.Start, (programLine.End - programLine.Start) + 1));

            _programState.SetCurrentProgramLine(programLine);

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
            if (_programState.IsUserFnDefined(fname))
            {
                ErrorAtLine("{0} function redefinition", fname);
            }

            // Save this function definition.
            _programState.DefineUserFn(fname,  _programState.CurrentProgramLine.Label);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
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

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
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

            var nextLine = _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
            if (nextLine != null)
            {
                ErrorAtLine("Unexpected END statement");
            }

            _programState.WasEnd = true;

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
                try
                {
                    _programState.ReturnStackPushLabel(_programState.CurrentProgramLine.Label);
                }
                catch
                {
                    ErrorAtLine("Return stack overflow");
                }
            }

            return _programState.GetProgramLine(label);
        }
        
        // IF exp1 rel exp2 THEN line-number
        // rel-num :: = <> >= <=
        // rel-str :: = <>
        private ProgramLine IfStatement()
        {
            EatToken(TOK_KEY_IF);

            // Do not jump.
            var jump = false;

            // String or numeric conditional jump?
            if (IsStringExpression())
            {
                var v1 = StringExpression();
                NextToken();

                var relTok = _tok;
                NextToken();

                var v2 = StringExpression();
                NextToken();

                jump = StringComparison(relTok, v1, v2);
            }
            else
            {
                var v1 = NumericExpression();

                var relTok = _tok;
                NextToken();

                var v2 = NumericExpression();

                jump = NumericComparison(relTok, v1, v2);
            }

            EatToken(TOK_KEY_THEN);

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            NextToken();
            ExpToken(TOK_EOLN);

            return jump
                ? _programState.GetProgramLine(label) 
                : _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }


        private bool NumericComparison(int relTok, float v1, float v2)
        {
            switch (relTok)
            {
                case TOK_EQL: return v1 == v2; // =
                case TOK_NEQL: return v1 != v2; // <>
                case TOK_LT: return v1 < v2; // <
                case TOK_LTE: return v1 <= v2; // <=
                case TOK_GT: return v1 > v2; // >
                case TOK_GTE: return v1 >= v2; // >=

                default:
                    UnexpectedTokenError(relTok);
                    break;
            }

            return false;
        }


        private bool StringComparison(int relTok, string v1, string v2)
        {
            switch (relTok)
            {
                case TOK_EQL: return v1 == v2; // =
                case TOK_NEQL: return v1 != v2; // <>

                default:
                    UnexpectedTokenError(relTok);
                    break;
            }

            return false;
        }


        // Sets the array bottom dimension.
        // OPTION BASE 1
        private ProgramLine OptionStatement()
        {
            if (_programState.ArrayBase >= 0)
            {
                ErrorAtLine("The OPTION BASE command already executed. Can not change the arrays lower bound");
            }

            // Eat "OPTION".
            NextToken();

            EatToken(TOK_KEY_BASE);

            // Array lower bound can not be changed, when an array is already defined.
            if (_programState.IsArrayDefined())
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

            _programState.ArrayBase = option;

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // LET var = expr EOLN
        // var :: num-var | string-var
        private ProgramLine LetStatement()
        {
            EatToken(TOK_KEY_LET);

            // var
            if (_tok == TOK_SVARIDNT)
            {
                var varName = _strValue;

                // Eat the variable identifier.
                NextToken();

                // Array subscript.
                if (_tok == TOK_LBRA)
                {
                    NextToken();

                    var index = (int)NumericExpression();

                    EatToken(TOK_RBRA);

                    CheckArray(varName, 10, index, true);

                    EatToken(TOK_EQL);

                    _programState.SetArray(varName, index, NumericExpression());
                }
                else
                {
                    CheckSubsription(varName);

                    EatToken(TOK_EQL);

                    _programState.SetNVar(varName, NumericExpression());
                }
            }
            else if (_tok == TOK_VARIDNT)
            {
                var varName = _strValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TOK_EQL);

                _programState.SetNVar(varName, NumericExpression());
            }
            else if (_tok == TOK_STRIDNT)
            {
                var varName = _strValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TOK_EQL);

                _programState.SetSVar(varName, StringExpression());

                // Eat the string expression.
                NextToken();
            }
            else
            {
                UnexpectedTokenError(_tok);
            }
                        
            // EOLN
            ExpToken(TOK_EOLN);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
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

                        if (IsStringExpression())
                        {
                            Console.Write(StringExpression());

                            // Eat the string expression.
                            NextToken();
                        }
                        else
                        {
                            Console.Write(FormatNumber(NumericExpression()));
                        }
                        
                        atSep = false;
                        break;
                }
            }

            ExpToken(TOK_EOLN);

            Console.WriteLine();

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private ProgramLine RandomizeStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);

            _programState.Randomize();

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // The comment.
        // REM ...
        private ProgramLine RemStatement()
        {
            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // Returns from a subroutine.
        // RETURN EOLN
        private ProgramLine ReturnStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);

            try
            {
                return _programState.NextProgramLine(_programState.ReturnStackPopLabel());
            }
            catch
            {
                ErrorAtLine("Return stack underflow");
            }

            return null;
        }

        // The end of execution.
        // STOP EOLN
        private ProgramLine StopStatement()
        {
            NextToken();
            ExpToken(TOK_EOLN);

            _programState.WasEnd = true;

            return null;
        }

        #endregion


        #region expressions

        // expr:: string-expression | numeric-expression
        private bool IsStringExpression()
        {
            return _tok == TOK_QSTR || _tok == TOK_STRIDNT;
        }

        // string-expression : string-variable | string-constant .
        private string StringExpression()
        {
            switch (_tok)
            {
                case TOK_QSTR: return _strValue;
                case TOK_STRIDNT: return _programState.GetSVar(_strValue);

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

                        return _programState.GetNVar(varName);
                    }
                    
                case TOK_VARIDNT:
                    {
                        var v = _programState.GetNVar(_strValue);
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

                            return (float)_programState.NextRandom();
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
                        var flabel = _programState.GetUserFnLabel(fname);
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
                        var cpl = _programState.CurrentProgramLine;
                        var cplp = _programState.CurrentProgramLinePos;

                        // Go to the user function definition.
                        _programState.SetCurrentProgramLine(_programState.GetProgramLine(flabel));
                        _programState.CurrentProgramLinePos = 0;

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
                        _programState.SetCurrentProgramLine(cpl, cplp);

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
            var arrayIndex = _programState.GetArrayIndex(arrayName);

            // Do not redefine array.
            if (canExist == false && _programState.IsArrayDefined(arrayIndex))
            {
                ErrorAtLine("Array {0} redefinition", arrayName);
            }

            var bottomBound = (_programState.ArrayBase < 0) ? 0 : _programState.ArrayBase;
            if (topBound < bottomBound)
            {
                ErrorAtLine("Array top bound ({0}) is less than the defined array bottom bound ({1})", topBound, _programState.ArrayBase);
            }

            index -= bottomBound;

            // Undefined array?
            if (_programState.IsArrayDefined(arrayIndex) == false)
            {
                _programState.DefineArray(arrayIndex, topBound);
            }

            if (index < 0 || index >= _programState.GetArrayLength(arrayIndex))
            {
                ErrorAtLine("Index {0} out of array bounds", index + bottomBound);
            }

            return _programState.GetArrayValue(arrayIndex, index);
        }

        /// <summary>
        /// Checks, if an array is used as a variable.
        /// </summary>
        /// <param name="varName"></param>
        private void CheckSubsription(string varName)
        {
            if (_programState.IsArrayDefined(varName))
            {
                ErrorAtLine("Array {0} subsciption expected", varName);
            }
        }

        #endregion


        #region variables

        #endregion


        #region formatters

        /// <summary>
        /// Formats a number to a PRINT statement output format.
        /// </summary>
        /// <param name="n">A number.</param>
        /// <returns>A number formated to the PRINT statement output format.</returns>
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

        /// <summary>
        /// The end of the line character.
        /// </summary>
        private const char C_EOLN = '\n';

        /// <summary>
        /// The end of the line token.
        /// </summary>
        private const int TOK_EOLN = 0;

        /// <summary>
        /// A simple ('A' .. 'Z') identifier token.
        /// used for variables, arrays and user function parameters.
        /// </summary>
        private const int TOK_SVARIDNT = 4;

        /// <summary>
        /// A numeric variable ("A0" .. "Z9") identifier token.
        /// </summary>
        private const int TOK_VARIDNT = 5;

        /// <summary>
        /// A string variable ("A$" .. "Z$") identifier token.
        /// </summary>
        private const int TOK_STRIDNT = 6;

        /// <summary>
        /// A number token.
        /// </summary>
        private const int TOK_NUM = 10;

        /// <summary>
        /// A quoted string token.
        /// </summary>
        private const int TOK_QSTR = 11;

        /// <summary>
        /// A build in function name token.
        /// </summary>
        private const int TOK_FN = 12;

        /// <summary>
        /// An user defined function name ("FN?") token.
        /// </summary>
        private const int TOK_UFN = 13;

        /// <summary>
        /// A PRINT statement values list separator (';') token.
        /// </summary>
        private const int TOK_PLSTSEP = 20;

        /// <summary>
        /// A list separator (',') token.
        /// </summary>
        private const int TOK_LSTSEP = 21;
        
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

        // Keywords tokens.

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

        /// <summary>
        /// A value of TOK_QSTR, TOK_SVARIDNT, TOK_VARIDNT, TOK_STRIDNT, TOK_FN and TOK_UFN tokens.
        /// </summary>
        private string _strValue = null;

        /// <summary>
        /// Checks, if this token is a label, if it is from the allowed range of labels
        /// and if such label/program line actually exists.
        /// </summary>
        /// <returns>The integer value representing this label.</returns>
        private int ExpLabel()
        {
            ExpToken(TOK_NUM);

            var label = (int)_numValue;

            if (label < 1 || label > _programState.MaxLabel)
            {
                Error("The label {0} at line {1} is out of <1 ... {2}> rangle.", label, _programState.CurrentProgramLine.Label, _programState.MaxLabel);
            }

            var target = _programState.GetProgramLine(label);
            if (target == null)
            {
                ErrorAtLine("Undefined label {0}", label);
            }

            return label;
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not
        /// or goes tho the next one, if it is.
        /// </summary>
        /// <param name="expTok">The expected token.</param>
        private void EatToken(int expTok)
        {
            ExpToken(expTok);
            NextToken();
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not.
        /// </summary>
        /// <param name="expTok">The expected token.</param>
        private void ExpToken(int expTok)
        {
            if (_tok != expTok)
            {
                UnexpectedTokenError(_tok);
            }
        }

        /// <summary>
        /// Extracts the next token found in the current program line source.
        /// </summary>
        private void NextToken()
        {
            if (_programState.CurrentProgramLine.Start + _programState.CurrentProgramLinePos > _programState.CurrentProgramLine.End)
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
                                _programState.CurrentProgramLinePos--;
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
                                _programState.CurrentProgramLinePos--;
                            _tok = TOK_GT;
                        }

                        return;
                    }

                    case '+':
                    {
                        var cc = NextChar();
                            _programState.CurrentProgramLinePos--;

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
                            _programState.CurrentProgramLinePos--;

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

        /// <summary>
        /// Parses an identifier the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character of the parsed identifier.</param>
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
                _programState.CurrentProgramLinePos--;

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
                _programState.CurrentProgramLinePos--;
            }

            _tok = tok;
        }
        
        /// <summary>
        /// Parses the quoted string using the ECMA-55 rules.
        /// </summary>
        /// <param name="c">The first character ('"') of the parsed string literal.</param>
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

            _tok = TOK_NUM;
            _numValue = negate ? -numValue : numValue;

            // Go one char back, so the next time we will read the character right behind this number.
            _programState.CurrentProgramLinePos--;
        }

        /// <summary>
        /// Gets the next character from the current program line source.
        /// </summary>
        /// <returns>The next character from the current program line source.</returns>
        private char NextChar()
        {
            return _programState.CurrentProgramLine.Source[_programState.CurrentProgramLine.Start + _programState.CurrentProgramLinePos++];
        }

        /// <summary>
        /// Checks, if an character is a digit.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a digit.</returns>
        public bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Checks, if an character is a letter.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns>True, if a character is a letter.</returns>
        private bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        #endregion


        #region scanner

        /// <summary>
        /// Scans the source for program lines.
        /// Exctracts labels, line starts and ends, etc.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="interactiveMode">A program line in the interactive mode can exist, 
        /// so the user can redefine it, an can be empty, so the user can delete it.</param>
        private void ScanSource(string source, bool interactiveMode = false)
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

                        if (label < 1 || label > _programState.MaxLabel)
                        {
                            Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, _programState.MaxLabel);
                        }

                        if (interactiveMode == false && _programState.GetProgramLine(label) != null)
                        {
                            Error("Label {0} redefinition at line {1}.", label, line);
                        }

                        // Remember this program line.
                        programLine.Source = source;
                        programLine.Label = label;
                        programLine.Start = i;

                        // Remember this line.
                        _programState.SetProgramLine(programLine);

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
                    if (programLine.Length > _programState.MaxProgramLineLength)
                    {
                        Error("The line {0} is longer than {1} characters.", line, _programState.MaxProgramLineLength);
                    }

                    // An empty line?
                    if (interactiveMode && string.IsNullOrWhiteSpace(programLine.Source.Substring(programLine.Start, programLine.End - programLine.Start)))
                    {
                        // Remove the existing program line.
                        _programState.RemoveProgramLine(programLine.Label);
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

        /// <summary>
        /// Reports the unexpected token error.
        /// </summary>
        /// <param name="tok">The unexpected token.</param>
        private void UnexpectedTokenError(int tok)
        {
            ErrorAtLine("Unexpected token {0}", tok);
        }

        /// <summary>
        /// Reports an general error on a program line.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        private void ErrorAtLine(string message, params object[] args)
        {
            // Interactive mode?
            if (_programState.CurrentProgramLine.Label < 1)
            {
                if (args == null || args.Length == 0)
                {
                    Error(message + ".");
                }
                else
                {
                    Error("{0}.", string.Format(message, args));
                }
            }
            else
            {
                if (args == null || args.Length == 0)
                {
                    Error("{0} at line {1}.", message, _programState.CurrentProgramLine.Label);
                }
                else
                {
                    Error("{0} at line {1}.", string.Format(message, args), _programState.CurrentProgramLine.Label);
                }
            }
        }

        /// <summary>
        /// Reports an general error and ends the current program execution.
        /// </summary>
        /// <exception cref="InterpreterException">Thrown when an error occurs during a program execution.</exception>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        private void Error(string message, params object[] args)
        {
            throw new InterpreterException(string.Format(message, args));
        }

        #endregion
                
        #endregion
    }
}
