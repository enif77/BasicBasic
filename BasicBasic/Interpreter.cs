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
            _scanner = new Scanner(_programState);
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

            _scanner.ScanSource(source);
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

            _scanner.ScanSource(source, true);
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
        private Scanner _scanner;
        private Tokenizer _tokenizer;


        private bool IsInteractiveModeProgramLine()
        {
            return _programState.CurrentProgramLine.Label < 0;
        }


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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("DEF statement is not supported in the interactive mode.");
            }

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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("END statement is not supported in the interactive mode.");
            }

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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("GO TO and GO SUB statements are not supported in the interactive mode.");
            }

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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("IF statement is not supported in the interactive mode.");
            }

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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("RETURN statement is not supported in the interactive mode.");
            }

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
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("STOP statement is not supported in the interactive mode.");
            }

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
        
        #endregion

        #endregion
    }
}
