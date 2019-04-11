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
            _tokenizer = new Tokenizer(_programState);
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
            if (source == null) throw _programState.Error("A source expected.");

            ScanSource(source);
            InterpretImpl();
        }

        /// <summary>
        /// Interprets a single program line.
        /// </summary>
        /// <param name="source">A program line source.</param>
        public void InterpretLine(string source)
        {
            if (source == null) throw _programState.Error("A source expected.");

            var programLine = new ProgramLine()
            {
                Source = source,
                Label = -1,
                Start = 0,
                SourcePosition = -1,  // Start - 1.
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
            if (source == null) throw _programState.Error("A program line source expected.");

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
        private Tokenizer _tokenizer;


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
                throw _programState.Error("Unexpected end of program.");
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

            _tokenizer.NextToken();

            // The statement.
            switch (_tokenizer.Token)
            {
                case Tokenizer.TOK_KEY_DEF: return DefStatement();
                case Tokenizer.TOK_KEY_DIM: return DimStatement();
                case Tokenizer.TOK_KEY_END: return EndStatement();
                case Tokenizer.TOK_KEY_GO:
                case Tokenizer.TOK_KEY_GOSUB:
                case Tokenizer.TOK_KEY_GOTO:
                    return GoToStatement();
                case Tokenizer.TOK_KEY_IF: return IfStatement();
                case Tokenizer.TOK_KEY_INPUT: return InputStatement();
                case Tokenizer.TOK_KEY_LET: return LetStatement();
                case Tokenizer.TOK_KEY_OPTION: return OptionStatement();
                case Tokenizer.TOK_KEY_PRINT: return PrintStatement();
                case Tokenizer.TOK_KEY_RANDOMIZE: return RandomizeStatement();
                case Tokenizer.TOK_KEY_REM: return RemStatement();
                case Tokenizer.TOK_KEY_RETURN: return ReturnStatement();
                case Tokenizer.TOK_KEY_STOP: return StopStatement();

                default:
                    throw _programState.UnexpectedTokenError(_tokenizer.Token);
            }
        }


        #region statements

        // An user defined function.
        // DEF FNx = numeric-expression EOLN
        private ProgramLine DefStatement()
        {
            EatToken(Tokenizer.TOK_KEY_DEF);

            // Get the function name.
            ExpToken(Tokenizer.TOK_UFN);
            var fname = _tokenizer.StrValue;

            // Do not redefine user functions.
            if (_programState.IsUserFnDefined(fname))
            {
                throw _programState.ErrorAtLine("{0} function redefinition", fname);
            }

            // Save this function definition.
            _programState.DefineUserFn(fname,  _programState.CurrentProgramLine.Label);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // An array definition.
        // DIM array-declaration { ',' array-declaration } EOLN
        private ProgramLine DimStatement()
        {
            EatToken(Tokenizer.TOK_KEY_DIM);

            ArrayDeclaration();

            // ','
            while (_tokenizer.Token == Tokenizer.TOK_LSTSEP)
            {
                EatToken(Tokenizer.TOK_LSTSEP);

                ArrayDeclaration();
            }

            ExpToken(Tokenizer.TOK_EOLN);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // array-declaration : letter '(' integer ')' .
        private void ArrayDeclaration()
        {
            // Get the function name.
            ExpToken(Tokenizer.TOK_SVARIDNT);

            var arrayName = _tokenizer.StrValue;

            // Eat array name.
            _tokenizer.NextToken();

            EatToken(Tokenizer.TOK_LBRA);
            ExpToken(Tokenizer.TOK_NUM);

            var topBound = (int)_tokenizer.NumValue;

            CheckArray(arrayName, topBound, topBound, false);

            // Eat array upper bound.
            _tokenizer.NextToken();

            EatToken(Tokenizer.TOK_RBRA);
        }

        // The end of program.
        // END EOLN
        private ProgramLine EndStatement()
        {
            EatToken(Tokenizer.TOK_KEY_END);
            ExpToken(Tokenizer.TOK_EOLN);

            var nextLine = _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
            if (nextLine != null)
            {
                throw _programState.ErrorAtLine("Unexpected END statement");
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
            if (_tokenizer.Token == Tokenizer.TOK_KEY_GO)
            {
                // Eat TO.
                _tokenizer.NextToken();

                // GO SUB?
                if (_tokenizer.Token == Tokenizer.TOK_KEY_SUB)
                {
                    gosub = true;
                }
                else
                {
                    ExpToken(Tokenizer.TOK_KEY_TO);
                }
            }
            else if (_tokenizer.Token == Tokenizer.TOK_KEY_GOSUB)
            {
                gosub = true;
            }

            // Eat the statement.
            _tokenizer.NextToken();

            // Get the label.
            var label = ExpLabel();
            _tokenizer.NextToken();

            // EOLN.
            ExpToken(Tokenizer.TOK_EOLN);

            if (gosub)
            {
                try
                {
                    _programState.ReturnStackPushLabel(_programState.CurrentProgramLine.Label);
                }
                catch
                {
                    throw _programState.ErrorAtLine("Return stack overflow");
                }
            }

            return _programState.GetProgramLine(label);
        }
        
        // IF exp1 rel exp2 THEN line-number
        // rel-num :: = <> >= <=
        // rel-str :: = <>
        private ProgramLine IfStatement()
        {
            EatToken(Tokenizer.TOK_KEY_IF);

            // Do not jump.
            var jump = false;

            // String or numeric conditional jump?
            if (IsStringExpression())
            {
                var v1 = StringExpression();
                _tokenizer.NextToken();

                var relTok = _tokenizer.Token;
                _tokenizer.NextToken();

                var v2 = StringExpression();
                _tokenizer.NextToken();

                jump = StringComparison(relTok, v1, v2);
            }
            else
            {
                var v1 = NumericExpression();

                var relTok = _tokenizer.Token;
                _tokenizer.NextToken();

                var v2 = NumericExpression();

                jump = NumericComparison(relTok, v1, v2);
            }

            EatToken(Tokenizer.TOK_KEY_THEN);

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            _tokenizer.NextToken();
            ExpToken(Tokenizer.TOK_EOLN);

            return jump
                ? _programState.GetProgramLine(label) 
                : _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }


        private bool NumericComparison(int relTok, float v1, float v2)
        {
            switch (relTok)
            {
                case Tokenizer.TOK_EQL: return v1 == v2; // =
                case Tokenizer.TOK_NEQL: return v1 != v2; // <>
                case Tokenizer.TOK_LT: return v1 < v2; // <
                case Tokenizer.TOK_LTE: return v1 <= v2; // <=
                case Tokenizer.TOK_GT: return v1 > v2; // >
                case Tokenizer.TOK_GTE: return v1 >= v2; // >=

                default:
                    throw _programState.UnexpectedTokenError(relTok);
            }
        }


        private bool StringComparison(int relTok, string v1, string v2)
        {
            switch (relTok)
            {
                case Tokenizer.TOK_EQL: return v1 == v2; // =
                case Tokenizer.TOK_NEQL: return v1 != v2; // <>

                default:
                    throw _programState.UnexpectedTokenError(relTok);
            }
        }


        // INPUT variable { ',' variable } EOLN
        private ProgramLine InputStatement()
        {
            // Eat INPUT.
            _tokenizer.NextToken();

            var varsList = new List<string>();

            bool atSep = true;
            while (_tokenizer.Token != Tokenizer.TOK_EOLN)
            {
                switch (_tokenizer.Token)
                {
                    // Consume these.
                    case Tokenizer.TOK_LSTSEP:
                        atSep = true;
                        _tokenizer.NextToken();
                        break;

                    default:
                        if (atSep == false)
                        {
                            throw _programState.ErrorAtLine("A list separator expected");
                        }

                        if (_tokenizer.Token == Tokenizer.TOK_STRIDNT || _tokenizer.Token == Tokenizer.TOK_SVARIDNT || _tokenizer.Token == Tokenizer.TOK_VARIDNT)
                        {
                            varsList.Add(_tokenizer.StrValue);

                            // Eat the variable.
                            _tokenizer.NextToken();
                        }
                        else
                        {
                            throw _programState.UnexpectedTokenError(_tokenizer.Token);
                        }

                        atSep = false;
                        break;
                }
            }

            ExpToken(Tokenizer.TOK_EOLN);

            if (varsList.Count == 0)
            {
                throw _programState.ErrorAtLine("The INPUT statement variables list can not be empty");
            }

            ReadUserInput(varsList);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        /// <summary>
        /// Reads the user's inputs and assigns it to selected variables.
        /// </summary>
        /// <param name="varsList">Variables, for which we need values.</param>
        private void ReadUserInput(List<string> varsList)
        {
            var valuesList = new List<string>();

            while (true)
            {
                Console.Write("? ");

                var input = Console.ReadLine() + Tokenizer.C_EOLN;
                var inputParsed = false;

                // Remove this!
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                // Parse th input.
                // input : data { ',' data } .
                // data : number | quoted-string
                var i = 0;
                bool atSep = true;
                while (true)
                {
                    if (i >= input.Length || input[i] == Tokenizer.C_EOLN)
                    {
                        inputParsed = true;

                        break;
                    }

                    // A quoted string.
                    if (input[i] == '\"')
                    {
                        // Missing separator.
                        if (atSep == false)
                        {
                            break;
                        }

                        var strValue = "$"; // '$' = a string value type.

                        // Eat '"'.
                        var c = input[++i];
                        while (c != Tokenizer.C_EOLN)
                        {
                            if (c == '\"')
                            {
                                break;
                            }

                            strValue += c;
                            c = input[++i];
                        }

                        if (c != '"')
                        {
                            // Unfinished quoted string.
                            break;
                        }

                        valuesList.Add(c == 0 ? string.Empty : strValue);

                        atSep = false;
                    }
                    // A number.
                    else if (input[i] == '+' || input[i] == '-' || Tokenizer.IsDigit(input[i]))
                    {
                        // Missing separator.
                        if (atSep == false)
                        {
                            break;
                        }

                        var sign = '+';
                        var numValue = (string)null;

                        if (input[i] == '+')
                        {
                            i++;
                        }
                        else if (input[i] == '-')
                        {
                            sign = '-';
                            i++;
                        }

                        // TODO: '+', '-' and '.' can start the unquoted string.
                        // unquoted -string-character : space | plain-string-character
                        // plain-string-character : plus-sign | minus-sign | full-stop | digit | letter

                        var c = input[i];
                        while (Tokenizer.IsDigit(c))
                        {
                            if (numValue == null)
                            {
                                numValue = sign.ToString();
                            }

                            numValue += c;
                            c = input[++i];
                        }

                        if (c == '.')
                        {
                            if (numValue == null)
                            {
                                numValue = sign.ToString();
                            }

                            numValue += c;

                            c = input[++i];
                            while (Tokenizer.IsDigit(c))
                            {
                                numValue += c;
                                c = input[++i];
                            }
                        }

                        if (c == 'E')
                        {
                            if (numValue == null)
                            {
                                // A number should not start with the exponent.
                                break;
                            }

                            numValue += c;

                            c = input[++i];
                            if (c == '+' || c == '-')
                            {
                                numValue += c;
                                c = input[++i];
                            }

                            while (Tokenizer.IsDigit(c))
                            {
                                numValue += c;

                                c = input[++i];
                            }
                        }

                        // Not a number?
                        if (numValue == null)
                        {
                            break;
                        }

                        valuesList.Add(numValue);

                        // Go back one character.
                        i--;

                        atSep = false;
                    }
                    else if (input[i] == ',')
                    {
                        // Missing value.
                        if (atSep)
                        {
                            break;
                        }

                        atSep = true;
                    }
                    else
                    {
                        // Missing separator.
                        if (atSep == false)
                        {
                            break;
                        }

                        var strValue = string.Empty;

                        var c = input[i];
                        while (c != Tokenizer.C_EOLN)
                        {
                            // TODO: Not all characters are allowed.
                            // unquoted -string-character : space | plain-string-character
                            // plain-string-character : plus-sign | minus-sign | full-stop | digit | letter

                            if (c == ',')
                            {
                                break;
                            }

                            strValue += c;
                            c = input[++i];
                        }

                        valuesList.Add(string.IsNullOrWhiteSpace(strValue) ? string.Empty : ("$" + strValue.Trim())); // '$' = a string value type.

                        // Go back one character.
                        i--;

                        atSep = false;
                    }

                    i++;
                }

                // Something wrong?
                if (inputParsed == false || atSep)
                {
                    valuesList.Clear();

                    continue;
                }

                // Assign values.
                if (varsList.Count != valuesList.Count)
                {
                    valuesList.Clear();

                    // Not enought or too much values.
                    continue;
                }

                var valuesAssigned = true;
                for (i = 0; i < varsList.Count; i++)
                {
                    var varName = varsList[i];
                    var value = valuesList[i];

                    // A string variable?
                    if (varName.EndsWith("$"))
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            _programState.SetSVar(varName, string.Empty);
                        }
                        else
                        {
                            // Not a string value?
                            if (value.StartsWith("$") == false)
                            {
                                valuesAssigned = false;

                                break;
                            }

                            _programState.SetSVar(varName, value.Substring(1));
                        }
                    }
                    // A numeric variable.
                    else
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            _programState.SetNVar(varName, 0);
                        }
                        else
                        {
                            // Not a numeric value?
                            if (value.StartsWith("$"))
                            {
                                valuesAssigned = false;

                                break;
                            }

                            _programState.SetNVar(varName, float.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture));
                        }
                    }
                }

                // Values assignment failed.
                if (valuesAssigned == false)
                {
                    valuesList.Clear();

                    continue;
                }

                break;
            }
        }


        // Sets the array bottom dimension.
        // OPTION BASE 1
        private ProgramLine OptionStatement()
        {
            if (_programState.ArrayBase >= 0)
            {
                throw _programState.ErrorAtLine("The OPTION BASE command already executed. Can not change the arrays lower bound");
            }

            // Eat "OPTION".
            _tokenizer.NextToken();

            EatToken(Tokenizer.TOK_KEY_BASE);

            // Array lower bound can not be changed, when an array is already defined.
            if (_programState.IsArrayDefined())
            {
                throw _programState.ErrorAtLine("An array is already defined. Can not change the arrays lower bound");
            }

            // 0 or 1.
            ExpToken(Tokenizer.TOK_NUM);

            var option = (int)_tokenizer.NumValue;
            if (option < 0 || option > 1)
            {
                throw _programState.ErrorAtLine("Array base out of allowed range 0 .. 1");
            }

            _programState.ArrayBase = option;

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // LET var = expr EOLN
        // var :: num-var | string-var
        private ProgramLine LetStatement()
        {
            EatToken(Tokenizer.TOK_KEY_LET);

            // var
            if (_tokenizer.Token == Tokenizer.TOK_SVARIDNT)
            {
                var varName = _tokenizer.StrValue;

                // Eat the variable identifier.
                _tokenizer.NextToken();

                // Array subscript.
                if (_tokenizer.Token == Tokenizer.TOK_LBRA)
                {
                    _tokenizer.NextToken();

                    var index = (int)NumericExpression();

                    EatToken(Tokenizer.TOK_RBRA);

                    CheckArray(varName, 10, index, true);

                    EatToken(Tokenizer.TOK_EQL);

                    _programState.SetArray(varName, index, NumericExpression());
                }
                else
                {
                    CheckSubsription(varName);

                    EatToken(Tokenizer.TOK_EQL);

                    _programState.SetNVar(varName, NumericExpression());
                }
            }
            else if (_tokenizer.Token == Tokenizer.TOK_VARIDNT)
            {
                var varName = _tokenizer.StrValue;

                // Eat the variable identifier.
                _tokenizer.NextToken();

                EatToken(Tokenizer.TOK_EQL);

                _programState.SetNVar(varName, NumericExpression());
            }
            else if (_tokenizer.Token == Tokenizer.TOK_STRIDNT)
            {
                var varName = _tokenizer.StrValue;

                // Eat the variable identifier.
                _tokenizer.NextToken();

                EatToken(Tokenizer.TOK_EQL);

                _programState.SetSVar(varName, StringExpression());

                // Eat the string expression.
                _tokenizer.NextToken();
            }
            else
            {
                throw _programState.UnexpectedTokenError(_tokenizer.Token);
            }
                        
            // EOLN
            ExpToken(Tokenizer.TOK_EOLN);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }
        
        // PRINT [ expr { print-sep expr } ] EOLN
        // print-sep :: ';' | ','
        private ProgramLine PrintStatement()
        {
            // Eat PRINT.
            _tokenizer.NextToken();

            bool atSep = true;
            while (_tokenizer.Token != Tokenizer.TOK_EOLN)
            {
                switch (_tokenizer.Token)
                {
                    // Consume these.
                    case Tokenizer.TOK_LSTSEP:
                    case Tokenizer.TOK_PLSTSEP:
                        atSep = true;
                        _tokenizer.NextToken();
                        break;

                    default:
                        if (atSep == false)
                        {
                            throw _programState.ErrorAtLine("A list separator expected");
                        }

                        if (IsStringExpression())
                        {
                            Console.Write(StringExpression());

                            // Eat the string expression.
                            _tokenizer.NextToken();
                        }
                        else
                        {
                            Console.Write(FormatNumber(NumericExpression()));
                        }
                        
                        atSep = false;
                        break;
                }
            }

            ExpToken(Tokenizer.TOK_EOLN);

            Console.WriteLine();

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private ProgramLine RandomizeStatement()
        {
            _tokenizer.NextToken();
            ExpToken(Tokenizer.TOK_EOLN);

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
            _tokenizer.NextToken();
            ExpToken(Tokenizer.TOK_EOLN);

            try
            {
                return _programState.NextProgramLine(_programState.ReturnStackPopLabel());
            }
            catch
            {
                throw _programState.ErrorAtLine("Return stack underflow");
            }
        }

        // The end of execution.
        // STOP EOLN
        private ProgramLine StopStatement()
        {
            _tokenizer.NextToken();
            ExpToken(Tokenizer.TOK_EOLN);

            _programState.WasEnd = true;

            return null;
        }

        #endregion


        #region expressions

        // expr:: string-expression | numeric-expression
        private bool IsStringExpression()
        {
            return _tokenizer.Token == Tokenizer.TOK_QSTR || _tokenizer.Token == Tokenizer.TOK_STRIDNT;
        }

        // string-expression : string-variable | string-constant .
        private string StringExpression()
        {
            switch (_tokenizer.Token)
            {
                case Tokenizer.TOK_QSTR: return _tokenizer.StrValue;
                case Tokenizer.TOK_STRIDNT: return _programState.GetSVar(_tokenizer.StrValue);

                default:
                    throw _programState.UnexpectedTokenError(_tokenizer.Token);
            }
        }

        // numeric-expression : [ sign ] term { sign term } .
        // term : number | numeric-variable .
        // sign : '+' | '-' .
        private float NumericExpression(string paramName = null, float? paramValue = null)
        {
            var negate = false;
            if (_tokenizer.Token == Tokenizer.TOK_PLUS)
            {
                _tokenizer.NextToken();
            }
            else if (_tokenizer.Token == Tokenizer.TOK_MINUS)
            {
                negate = true;
                _tokenizer.NextToken();
            }

            var v = Term(paramName, paramValue);

            while (true)
            {
                if (_tokenizer.Token == Tokenizer.TOK_PLUS)
                {
                    _tokenizer.NextToken();

                    v += Term(paramName, paramValue);
                }
                else if (_tokenizer.Token == Tokenizer.TOK_MINUS)
                {
                    _tokenizer.NextToken();

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
                if (_tokenizer.Token == Tokenizer.TOK_MULT)
                {
                    _tokenizer.NextToken();

                    v *= Factor(paramName, paramValue);
                }
                else if (_tokenizer.Token == Tokenizer.TOK_DIV)
                {
                    _tokenizer.NextToken();

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
                if (_tokenizer.Token == Tokenizer.TOK_POW)
                {
                    _tokenizer.NextToken();

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
            switch (_tokenizer.Token)
            {
                case Tokenizer.TOK_NUM:
                    var n = _tokenizer.NumValue;
                    _tokenizer.NextToken();
                    return n;

                case Tokenizer.TOK_SVARIDNT:
                    {
                        var varName = _tokenizer.StrValue;
                        _tokenizer.NextToken();

                        // Array subscript.
                        if (_tokenizer.Token == Tokenizer.TOK_LBRA)
                        {
                            _tokenizer.NextToken();

                            var index = (int)NumericExpression();

                            EatToken(Tokenizer.TOK_RBRA);

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
                    
                case Tokenizer.TOK_VARIDNT:
                    {
                        var v = _programState.GetNVar(_tokenizer.StrValue);
                        _tokenizer.NextToken();
                        return v;
                    }
                    
                case Tokenizer.TOK_LBRA:
                    {
                        _tokenizer.NextToken();
                        var v = NumericExpression();
                        EatToken(Tokenizer.TOK_RBRA);
                        return v;
                    }
                    
                case Tokenizer.TOK_FN:
                    {
                        float v;
                        var fnName = _tokenizer.StrValue;
                        if (fnName == "RND")
                        {
                            _tokenizer.NextToken();

                            return (float)_programState.NextRandom();
                        }
                        else
                        {
                            _tokenizer.NextToken();

                            EatToken(Tokenizer.TOK_LBRA);

                            v = NumericExpression();

                            EatToken(Tokenizer.TOK_RBRA);
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
                                throw _programState.ErrorAtLine("Unknown function '{0}'", fnName);
                        }

                        return v;
                    }

                case Tokenizer.TOK_UFN:
                    {
                        float v;
                        var fname = _tokenizer.StrValue;
                        var flabel = _programState.GetUserFnLabel(fname);
                        if (flabel == 0)
                        {
                            throw _programState.ErrorAtLine("Undefined user function {0}", fname);
                        }

                        // Eat the function name.
                        _tokenizer.NextToken();

                        // FNA(X)
                        var p = (float?)null;
                        if (_tokenizer.Token == Tokenizer.TOK_LBRA)
                        {
                            _tokenizer.NextToken();
                            p = NumericExpression();
                            EatToken(Tokenizer.TOK_RBRA);
                        }

                        // Remember, where we are.
                        var cpl = _programState.CurrentProgramLine;

                        // Go to the user function definition.
                        _programState.SetCurrentProgramLine(_programState.GetProgramLine(flabel));

                        // DEF
                        _tokenizer.NextToken();
                        EatToken(Tokenizer.TOK_KEY_DEF);

                        // Function name.
                        ExpToken(Tokenizer.TOK_UFN);

                        if (fname != _tokenizer.StrValue)
                        {
                            throw _programState.ErrorAtLine("Unexpected {0} function definition", _tokenizer.StrValue);
                        }

                        // Eat the function name.
                        _tokenizer.NextToken();

                        // FNx(X)
                        var paramName = (string)null;
                        if (_tokenizer.Token == Tokenizer.TOK_LBRA)
                        {
                            if (p.HasValue == false)
                            {
                                throw _programState.ErrorAtLine("The {0} function expects a parameter", fname);
                            }

                            // Eat '(';
                            _tokenizer.NextToken();

                            // A siple variable name (A .. Z) expected.
                            ExpToken(Tokenizer.TOK_SVARIDNT);

                            paramName = _tokenizer.StrValue;

                            _tokenizer.NextToken();
                            EatToken(Tokenizer.TOK_RBRA);
                        }
                        else
                        {
                            if (p.HasValue)
                            {
                                throw _programState.ErrorAtLine("The {0} function does not expect a parameter", fname);
                            }
                        }
                                               
                        // '='
                        EatToken(Tokenizer.TOK_EQL);

                        v = NumericExpression(paramName, p);

                        ExpToken(Tokenizer.TOK_EOLN);

                        // Restore the previous position.
                        _programState.SetCurrentProgramLine(cpl, false);

                        return v;
                    }
                    
                default:
                    throw _programState.UnexpectedTokenError(_tokenizer.Token);
            }
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
                throw _programState.ErrorAtLine("Array {0} redefinition", arrayName);
            }

            var bottomBound = (_programState.ArrayBase < 0) ? 0 : _programState.ArrayBase;
            if (topBound < bottomBound)
            {
                throw _programState.ErrorAtLine("Array top bound ({0}) is less than the defined array bottom bound ({1})", topBound, _programState.ArrayBase);
            }

            index -= bottomBound;

            // Undefined array?
            if (_programState.IsArrayDefined(arrayIndex) == false)
            {
                _programState.DefineArray(arrayIndex, topBound);
            }

            if (index < 0 || index >= _programState.GetArrayLength(arrayIndex))
            {
                throw _programState.ErrorAtLine("Index {0} out of array bounds", index + bottomBound);
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
                throw _programState.ErrorAtLine("Array {0} subsciption expected", varName);
            }
        }

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

        //#region tokens

        ///// <summary>
        ///// The end of the line character.
        ///// </summary>
        //private const char C_EOLN = '\n';

        ///// <summary>
        ///// The end of the line token.
        ///// </summary>
        //private const int TOK_EOLN = 0;

        ///// <summary>
        ///// A simple ('A' .. 'Z') identifier token.
        ///// used for variables, arrays and user function parameters.
        ///// </summary>
        //private const int TOK_SVARIDNT = 4;

        ///// <summary>
        ///// A numeric variable ("A0" .. "Z9") identifier token.
        ///// </summary>
        //private const int TOK_VARIDNT = 5;

        ///// <summary>
        ///// A string variable ("A$" .. "Z$") identifier token.
        ///// </summary>
        //private const int TOK_STRIDNT = 6;

        ///// <summary>
        ///// A number token.
        ///// </summary>
        //private const int TOK_NUM = 10;

        ///// <summary>
        ///// A quoted string token.
        ///// </summary>
        //private const int TOK_QSTR = 11;

        ///// <summary>
        ///// A build in function name token.
        ///// </summary>
        //private const int TOK_FN = 12;

        ///// <summary>
        ///// An user defined function name ("FN?") token.
        ///// </summary>
        //private const int TOK_UFN = 13;

        ///// <summary>
        ///// A PRINT statement values list separator (';') token.
        ///// </summary>
        //private const int TOK_PLSTSEP = 20;

        ///// <summary>
        ///// A list separator (',') token.
        ///// </summary>
        //private const int TOK_LSTSEP = 21;

        //private const int TOK_EQL = 30;   // =
        //private const int TOK_NEQL = 31;  // <>
        //private const int TOK_LT = 32;    // <
        //private const int TOK_LTE = 33;   // <=
        //private const int TOK_GT = 34;    // >
        //private const int TOK_GTE = 35;   // >=

        //// + - * / ^ ( )
        //private const int TOK_PLUS = 40;
        //private const int TOK_MINUS = 41;
        //private const int TOK_MULT = 42;
        //private const int TOK_DIV = 43;
        //private const int TOK_POW = 44;
        //private const int TOK_LBRA = 45;
        //private const int TOK_RBRA = 46;

        //// Keywords tokens.

        //private const int TOK_KEY_BASE = 100;
        ////private const int TOK_KEY_DATA = 101;
        //private const int TOK_KEY_DEF = 102;
        //private const int TOK_KEY_DIM = 103;
        //private const int TOK_KEY_END = 104;
        ////private const int TOK_KEY_FOR = 105;
        //private const int TOK_KEY_GO = 106;
        //private const int TOK_KEY_GOSUB = 107;
        //private const int TOK_KEY_GOTO = 108;
        //private const int TOK_KEY_IF = 109;
        //private const int TOK_KEY_INPUT = 110;
        //private const int TOK_KEY_LET = 111;
        ////private const int TOK_KEY_NEXT = 112;
        ////private const int TOK_KEY_ON = 113;
        //private const int TOK_KEY_OPTION = 114;
        //private const int TOK_KEY_PRINT = 115;
        //private const int TOK_KEY_RANDOMIZE = 116;
        ////private const int TOK_KEY_READ = 117;
        //private const int TOK_KEY_REM = 118;
        ////private const int TOK_KEY_RESTORE = 119;
        //private const int TOK_KEY_RETURN = 120;
        ////private const int TOK_KEY_STEP = 121;
        //private const int TOK_KEY_STOP = 122;
        //private const int TOK_KEY_SUB = 123;
        //private const int TOK_KEY_THEN = 124;
        //private const int TOK_KEY_TO = 125;

        ///// <summary>
        ///// The keyword - token map.
        ///// </summary>
        //private readonly Dictionary<string, int> _keyWordsMap = new Dictionary<string, int>()
        //{
        //    { "BASE", TOK_KEY_BASE },
        //    { "DEF", TOK_KEY_DEF },
        //    { "DIM", TOK_KEY_DIM },
        //    { "END", TOK_KEY_END },
        //    { "GO", TOK_KEY_GO },
        //    { "GOSUB", TOK_KEY_GOSUB },
        //    { "GOTO", TOK_KEY_GOTO },
        //    { "IF", TOK_KEY_IF },
        //    { "INPUT", TOK_KEY_INPUT },
        //    { "LET", TOK_KEY_LET },
        //    { "OPTION", TOK_KEY_OPTION },
        //    { "PRINT", TOK_KEY_PRINT },
        //    { "RANDOMIZE", TOK_KEY_RANDOMIZE },
        //    { "REM", TOK_KEY_REM },
        //    { "RETURN", TOK_KEY_RETURN },
        //    { "STOP", TOK_KEY_STOP },
        //    { "SUB", TOK_KEY_SUB },
        //    { "THEN", TOK_KEY_THEN },
        //    { "TO", TOK_KEY_TO },
        //};

        //#endregion


        ///// <summary>
        ///// The last found token.
        ///// </summary>
        //private int _tok = 0;

        ///// <summary>
        ///// A value of the TOK_NUM.
        ///// </summary>
        //private float _numValue = 0.0f;

        ///// <summary>
        ///// A value of TOK_QSTR, TOK_SVARIDNT, TOK_VARIDNT, TOK_STRIDNT, TOK_FN and TOK_UFN tokens.
        ///// </summary>
        //private string _strValue = null;

        /// <summary>
        /// Checks, if this token is a label, if it is from the allowed range of labels
        /// and if such label/program line actually exists.
        /// </summary>
        /// <returns>The integer value representing this label.</returns>
        private int ExpLabel()
        {
            ExpToken(Tokenizer.TOK_NUM);

            var label = (int)_tokenizer.NumValue;

            if (label < 1 || label > _programState.MaxLabel)
            {
                throw _programState.Error("The label {0} at line {1} is out of <1 ... {2}> rangle.", label, _programState.CurrentProgramLine.Label, _programState.MaxLabel);
            }

            var target = _programState.GetProgramLine(label);
            if (target == null)
            {
                throw _programState.ErrorAtLine("Undefined label {0}", label);
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
            _tokenizer.NextToken();
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not.
        /// </summary>
        /// <param name="expTok">The expected token.</param>
        private void ExpToken(int expTok)
        {
            if (_tokenizer.Token != expTok)
            {
                throw _programState.UnexpectedTokenError(_tokenizer.Token);
            }
        }

        ///// <summary>
        ///// Extracts the next token found in the current program line source.
        ///// </summary>
        //private void NextToken()
        //{
        //    if (_programState.CurrentProgramLine.SourcePosition > _programState.CurrentProgramLine.End)
        //    {
        //        throw ErrorAtLine("Read beyond the line end");
        //    }

        //    var c = NextChar();
        //    while (c != C_EOLN)
        //    {
        //        //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

        //        //// Skip white chars.
        //        //if (c <= ' ' && c != '\n')
        //        //{
        //        //    c = NextChar();
        //        //}

        //        if (IsDigit(c) || c == '.')
        //        {
        //            ParseNumber(c);

        //            return;
        //        }

        //        if (IsLetter(c))
        //        {
        //            ParseIdent(c);

        //            return;
        //        }

        //        if (c == '"')
        //        {
        //            ParseQuotedString(c);

        //            return;
        //        }

        //        switch (c)
        //        {
        //            case ';': _tok = TOK_PLSTSEP; return;
        //            case ',': _tok = TOK_LSTSEP; return;
        //            case '=': _tok = TOK_EQL; return;

        //            case '<':
        //            {
        //                var cc = NextChar();
        //                if (cc == '>')
        //                {
        //                    _tok = TOK_NEQL;
        //                }
        //                else if (cc == '=')
        //                {
        //                    _tok = TOK_LTE;
        //                }
        //                else
        //                {
        //                    _programState.CurrentProgramLine.PreviousChar();
        //                    _tok = TOK_LT;
        //                }

        //                return;
        //            }

        //            case '>':
        //            {
        //                var cc = NextChar();
        //                if (cc == '=')
        //                {
        //                    _tok = TOK_GTE;
        //                }
        //                else
        //                {
        //                    _programState.CurrentProgramLine.PreviousChar();
        //                    _tok = TOK_GT;
        //                }

        //                return;
        //            }

        //            case '+':
        //            {
        //                var cc = NextChar();
        //                _programState.CurrentProgramLine.PreviousChar();

        //                if (IsDigit(cc) || cc == '.')
        //                {
        //                    ParseNumber(c);
        //                }
        //                else
        //                {
        //                    _tok = TOK_PLUS;
        //                }

        //                return;
        //            }

        //            case '-':
        //            {
        //                var cc = NextChar();
        //                _programState.CurrentProgramLine.PreviousChar();

        //                if (IsDigit(cc) || cc == '.')
        //                {
        //                    ParseNumber(c);
        //                }
        //                else
        //                {
        //                    _tok = TOK_MINUS;
        //                }

        //                return;
        //            }

        //            case '*': _tok = TOK_MULT; return;
        //            case '/': _tok = TOK_DIV; return;
        //            case '^': _tok = TOK_POW; return;
        //            case '(': _tok = TOK_LBRA; return;
        //            case ')': _tok = TOK_RBRA; return;
        //        }

        //        c = NextChar();
        //    }

        //    _tok = TOK_EOLN;
        //}

        ///// <summary>
        ///// Parses an identifier the ECMA-55 rules.
        ///// </summary>
        ///// <param name="c">The first character of the parsed identifier.</param>
        //private void ParseIdent(char c)
        //{
        //    var tok = TOK_SVARIDNT;
        //    var strValue = c.ToString();

        //    c = NextChar();

        //    if (IsDigit(c))
        //    {
        //        // A numeric Ax variable.
        //        _strValue = (strValue + c).ToUpperInvariant();
        //        tok = TOK_VARIDNT;
        //    }
        //    else if (c == '$')
        //    {
        //        // A string A$ variable.
        //        tok = TOK_STRIDNT;

        //        _strValue = (strValue + c).ToUpperInvariant();
        //    }
        //    else if (IsLetter(c))
        //    {
        //        // A key word?
        //        while (IsLetter(c))
        //        {
        //            strValue += c;

        //            c = NextChar();
        //        }

        //        // Go one char back, so the next time we will read the character behind this identifier.
        //        _programState.CurrentProgramLine.PreviousChar();

        //        strValue = strValue.ToUpperInvariant();

        //        if (_keyWordsMap.ContainsKey(strValue))
        //        {
        //            tok = _keyWordsMap[strValue];
        //        }
        //        else
        //        {
        //            if (strValue.Length != 3)
        //            {
        //                throw ErrorAtLine("Unknown token '{0}'", strValue);
        //            }

        //            tok = strValue.StartsWith("FN") 
        //                ? TOK_UFN 
        //                : TOK_FN;
        //        }

        //        _strValue = strValue;
        //    }
        //    else
        //    {
        //        // A simple variable A.
        //        _strValue = strValue.ToUpperInvariant();

        //        // Go one char back, so the next time we will read the character behind this identifier.
        //        _programState.CurrentProgramLine.PreviousChar();
        //    }

        //    _tok = tok;
        //}

        ///// <summary>
        ///// Parses the quoted string using the ECMA-55 rules.
        ///// </summary>
        ///// <param name="c">The first character ('"') of the parsed string literal.</param>
        //private void ParseQuotedString(char c)
        //{
        //    var strValue = string.Empty;

        //    c = NextChar();
        //    while (c != '"' && c != C_EOLN)
        //    {
        //        strValue += c;

        //        c = NextChar();
        //    }

        //    if (c != '"')
        //    {
        //        throw ErrorAtLine("Unexpected end of quoted string");
        //    }

        //    _tok = TOK_QSTR;
        //    _strValue = strValue;
        //}

        ///// <summary>
        ///// Parses the number using the ECMA-55 rules.
        ///// </summary>
        ///// <param name="c">The first character of the parsed number.</param>
        //private void ParseNumber(char c)
        //{
        //    var negate = false;
        //    if (c == '+')
        //    {
        //        c = NextChar();
        //    }
        //    else if (c == '-')
        //    {
        //        negate = true;
        //        c = NextChar();
        //    }

        //    var numValue = 0.0f;
        //    while (IsDigit(c))
        //    {
        //        numValue = numValue * 10.0f + (c - '0');

        //        c = NextChar();
        //    }

        //    if (c == '.')
        //    {
        //        var exp = 0.1f;

        //        c = NextChar();
        //        while (IsDigit(c))
        //        {
        //            numValue += (c - '0') * exp;
        //            exp *= 0.1f;

        //            c = NextChar();
        //        }
        //    }

        //    if (c == 'E')
        //    {
        //        c = NextChar();

        //        var negateExp = false;
        //        if (c == '+')
        //        {
        //            c = NextChar();
        //        }
        //        else if (c == '-')
        //        {
        //            negateExp = true;
        //            c = NextChar();
        //        }

        //        var exp = 0f;
        //        while (IsDigit(c))
        //        {
        //            exp = exp * 10.0f + (c - '0');

        //            c = NextChar();
        //        }

        //        exp = negateExp ? -exp : exp;

        //        numValue = (float)(numValue * Math.Pow(10.0, exp));
        //    }

        //    _tok = TOK_NUM;
        //    _numValue = negate ? -numValue : numValue;

        //    // Go one char back, so the next time we will read the character right behind this number.
        //    _programState.CurrentProgramLine.PreviousChar();
        //}

        ///// <summary>
        ///// Gets the next character from the current program line source.
        ///// </summary>
        ///// <returns>The next character from the current program line source.</returns>
        //private char NextChar()
        //{
        //    return _programState.CurrentProgramLine.NextChar();
        //}

        ///// <summary>
        ///// Checks, if an character is a digit.
        ///// </summary>
        ///// <param name="c">A character.</param>
        ///// <returns>True, if a character is a digit.</returns>
        //public bool IsDigit(char c)
        //{
        //    return c >= '0' && c <= '9';
        //}

        ///// <summary>
        ///// Checks, if an character is a letter.
        ///// </summary>
        ///// <param name="c">A character.</param>
        ///// <returns>True, if a character is a letter.</returns>
        //private bool IsLetter(char c)
        //{
        //    return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        //}

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
                    if (Tokenizer.IsDigit(c))
                    {
                        var label = 0;
                        while (Tokenizer.IsDigit(c))
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
                            throw _programState.Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, _programState.MaxLabel);
                        }

                        if (interactiveMode == false && _programState.GetProgramLine(label) != null)
                        {
                            throw _programState.Error("Label {0} redefinition at line {1}.", label, line);
                        }

                        // Remember this program line.
                        programLine.Source = source;
                        programLine.Label = label;
                        programLine.Start = i;
                        programLine.SourcePosition = programLine.Start - 1;

                        // Remember this line.
                        _programState.SetProgramLine(programLine);

                        atLineStart = false;
                    }
                    else
                    {
                        throw _programState.Error("Label not found at line {0}.", line);
                    }
                }

                //// Skip white chars.
                //if (c <= ' ' && c != '\n')
                //{
                //    continue;
                //}

                if (c == Tokenizer.C_EOLN)
                {
                    // The '\n' character.
                    programLine.End = i;

                    // Max program line length check.
                    if (programLine.Length > _programState.MaxProgramLineLength)
                    {
                        throw _programState.Error("The line {0} is longer than {1} characters.", line, _programState.MaxProgramLineLength);
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
                throw _programState.Error("No line end at line {0}.", line);
            }
        }

        #endregion


        //#region errors

        ///// <summary>
        ///// Reports the unexpected token error.
        ///// </summary>
        ///// <param name="tok">The unexpected token.</param>
        //private InterpreterException UnexpectedTokenError(int tok)
        //{
        //    return ErrorAtLine("Unexpected token {0}", tok);
        //}

        ///// <summary>
        ///// Reports an general error on a program line.
        ///// </summary>
        ///// <param name="message">An error message.</param>
        ///// <param name="args">Error message arguments.</param>
        //private InterpreterException ErrorAtLine(string message, params object[] args)
        //{
        //    // Interactive mode?
        //    if (_programState.CurrentProgramLine.Label < 1)
        //    {
        //        if (args == null || args.Length == 0)
        //        {
        //            return Error(message + ".");
        //        }
        //        else
        //        {
        //            return Error("{0}.", string.Format(message, args));
        //        }
        //    }
        //    else
        //    {
        //        if (args == null || args.Length == 0)
        //        {
        //            return Error("{0} at line {1}.", message, _programState.CurrentProgramLine.Label);
        //        }
        //        else
        //        {
        //            return Error("{0} at line {1}.", string.Format(message, args), _programState.CurrentProgramLine.Label);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Reports an general error and ends the current program execution.
        ///// </summary>
        ///// <exception cref="InterpreterException">Thrown when an error occurs during a program execution.</exception>
        ///// <param name="message">An error message.</param>
        ///// <param name="args">Error message arguments.</param>
        //private InterpreterException Error(string message, params object[] args)
        //{
        //    return new InterpreterException(string.Format(message, args));
        //}

        //#endregion
                
        #endregion
    }
}
