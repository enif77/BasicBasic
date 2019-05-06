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
    using BasicBasic.Indirect.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Globalization;


    /// <summary>
    /// The basic Basic interpreter.
    /// </summary>
    public class Interpreter : IInterpreter
    {
        #region ctor

        public Interpreter(IErrorHandler errorHandler)
        {
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _errorHandler = errorHandler;
        }

        #endregion


        #region public

        /// <summary>
        /// Initializes this interpereter instance.
        /// </summary>
        public void Initialize()
        {
            _programState = new ProgramState(_errorHandler);
            _scanner = new Scanner(_programState);


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

            // Each token should be preceeded by at least a single white character.
            // So we have to add one here, if user do not inserted one.
            if (Tokenizer.IsWhite(source[0]) == false)
            {
                source = " " + source;
            }

            InterpretLine(_scanner.ScanInteractiveModeSourceLine(source));
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

            _scanner.ScanInteractiveModeSourceLine(source);
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

        private IErrorHandler _errorHandler;
        private ProgramState _programState;
        private Scanner _scanner;


        private bool IsInteractiveModeProgramLine()
        {
            return _programState.CurrentProgramLine.Label < 0;
        }


        private ProgramLine NextProgramLine()
        {
            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }
               
        /// <summary>
        /// Interprets the whole program.
        /// </summary>
        private void InterpretImpl()
        {
            _programState.ReturnStackClear();
            _programState.ClearVariables();
            _programState.ClearArrays();
            _programState.ClearUserFunctions();
            _programState.RestoreData();

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
            _programState.SetCurrentProgramLine(programLine);

            // The statement.
            var token = NextToken();
            switch (token.TokenCode)
            {
                case TokenCode.TOK_KEY_DATA: return DataStatement();
                case TokenCode.TOK_KEY_DEF: return DefStatement();
                case TokenCode.TOK_KEY_DIM: return DimStatement();
                case TokenCode.TOK_KEY_END: return EndStatement();
                case TokenCode.TOK_KEY_GO:
                case TokenCode.TOK_KEY_GOSUB:
                case TokenCode.TOK_KEY_GOTO:
                    return GoToStatement();
                case TokenCode.TOK_KEY_IF: return IfStatement();
                case TokenCode.TOK_KEY_INPUT: return InputStatement();
                case TokenCode.TOK_KEY_LET: return LetStatement();
                case TokenCode.TOK_KEY_ON: return OnStatement();
                case TokenCode.TOK_KEY_OPTION: return OptionStatement();
                case TokenCode.TOK_KEY_PRINT: return PrintStatement();
                case TokenCode.TOK_KEY_RANDOMIZE: return RandomizeStatement();
                case TokenCode.TOK_KEY_READ: return ReadStatement();
                case TokenCode.TOK_KEY_REM: return RemStatement();
                case TokenCode.TOK_KEY_RESTORE: return RestoreStatement();
                case TokenCode.TOK_KEY_RETURN: return ReturnStatement();
                case TokenCode.TOK_KEY_STOP: return StopStatement();

                default:
                    throw _programState.UnexpectedTokenError(token);
            }
        }


        #region statements

        // The data.
        // DATA ...
        private ProgramLine DataStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("DATA statement is not supported in the interactive mode.");
            }

            return NextProgramLine();
        }

        // An user defined function.
        // DEF FNx = numeric-expression EOLN
        private ProgramLine DefStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("DEF statement is not supported in the interactive mode.");
            }

            EatToken(TokenCode.TOK_KEY_DEF);

            // Get the function name.
            ExpToken(TokenCode.TOK_UFN, ThisToken());
            var fname = ThisToken().StrValue;

            // Do not redefine user functions.
            if (_programState.IsUserFnDefined(fname))
            {
                throw _programState.ErrorAtLine("{0} function redefinition", fname);
            }

            // Save this function definition.
            _programState.DefineUserFn(fname, _programState.CurrentProgramLine.Label);

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // An array definition.
        // DIM array-declaration { ',' array-declaration } EOLN
        private ProgramLine DimStatement()
        {
            EatToken(TokenCode.TOK_KEY_DIM);

            ArrayDeclaration();

            // ','
            while (ThisToken().TokenCode == TokenCode.TOK_LSTSEP)
            {
                EatToken(TokenCode.TOK_LSTSEP);

                ArrayDeclaration();
            }

            ExpToken(TokenCode.TOK_EOLN, ThisToken());

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // array-declaration : letter '(' integer ')' .
        private void ArrayDeclaration()
        {
            // Get the function name.
            ExpToken(TokenCode.TOK_SVARIDNT, ThisToken());

            var arrayName = ThisToken().StrValue;

            // Eat array name.
            NextToken();

            EatToken(TokenCode.TOK_LBRA);
            ExpToken(TokenCode.TOK_NUM, ThisToken());

            var topBound = (int)ThisToken().NumValue;

            CheckArray(arrayName, topBound, topBound, false);

            // Eat array upper bound.
            NextToken();

            EatToken(TokenCode.TOK_RBRA);
        }

        // The end of program.
        // END EOLN
        private ProgramLine EndStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("END statement is not supported in the interactive mode.");
            }

            ExpToken(TokenCode.TOK_EOLN, NextToken());

            var nextLine = NextProgramLine();
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
            if (ThisToken().TokenCode == TokenCode.TOK_KEY_GO)
            {
                // Eat GO.
                NextToken();

                // GO SUB?
                if (ThisToken().TokenCode == TokenCode.TOK_KEY_SUB)
                {
                    gosub = true;
                }
                else
                {
                    ExpToken(TokenCode.TOK_KEY_TO, ThisToken());
                }
            }
            else if (ThisToken().TokenCode == TokenCode.TOK_KEY_GOSUB)
            {
                gosub = true;
            }

            // Eat the statement.
            NextToken();

            // Get the label.
            var label = ExpLabel();
            NextToken();

            // EOLN.
            ExpToken(TokenCode.TOK_EOLN, ThisToken());

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

            EatToken(TokenCode.TOK_KEY_IF);

            // Do not jump.
            var jump = false;

            // String or numeric conditional jump?
            if (IsStringExpression(ThisToken()))
            {
                var v1 = StringExpression(ThisToken());
                NextToken();

                var relTok = ThisToken();
                NextToken();

                var v2 = StringExpression(ThisToken());
                NextToken();

                jump = StringComparison(relTok, v1, v2);
            }
            else
            {
                var v1 = NumericExpression();

                var relTok = ThisToken();
                NextToken();

                var v2 = NumericExpression();

                jump = NumericComparison(relTok, v1, v2);
            }

            EatToken(TokenCode.TOK_KEY_THEN);

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            NextToken();
            ExpToken(TokenCode.TOK_EOLN, ThisToken());

            return jump
                ? _programState.GetProgramLine(label)
                : _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }


        private bool NumericComparison(IToken relTok, float v1, float v2)
        {
            switch (relTok.TokenCode)
            {
                case TokenCode.TOK_EQL: return v1 == v2; // =
                case TokenCode.TOK_NEQL: return v1 != v2; // <>
                case TokenCode.TOK_LT: return v1 < v2; // <
                case TokenCode.TOK_LTE: return v1 <= v2; // <=
                case TokenCode.TOK_GT: return v1 > v2; // >
                case TokenCode.TOK_GTE: return v1 >= v2; // >=

                default:
                    throw _programState.UnexpectedTokenError(relTok);
            }
        }


        private bool StringComparison(IToken relTok, string v1, string v2)
        {
            switch (relTok.TokenCode)
            {
                case TokenCode.TOK_EQL: return v1 == v2; // =
                case TokenCode.TOK_NEQL: return v1 != v2; // <>

                default:
                    throw _programState.UnexpectedTokenError(relTok);
            }
        }
        

        // INPUT variable { ',' variable } EOLN
        private ProgramLine InputStatement()
        {
            // Eat INPUT.
            NextToken();

            var varsList = new List<string>();

            bool atSep = true;
            while (ThisToken().TokenCode != TokenCode.TOK_EOLN)
            {
                switch (ThisToken().TokenCode)
                {
                    // Consume these.
                    case TokenCode.TOK_LSTSEP:
                        atSep = true;
                        NextToken();
                        break;

                    default:
                        if (atSep == false)
                        {
                            throw _programState.ErrorAtLine("A list separator expected");
                        }

                        if (ThisToken().TokenCode == TokenCode.TOK_STRIDNT || ThisToken().TokenCode == TokenCode.TOK_SVARIDNT || ThisToken().TokenCode == TokenCode.TOK_VARIDNT)
                        {
                            varsList.Add(ThisToken().StrValue);

                            // Eat the variable.
                            NextToken();
                        }
                        else
                        {
                            throw _programState.UnexpectedTokenError(ThisToken());
                        }

                        atSep = false;
                        break;
                }
            }

            ExpToken(TokenCode.TOK_EOLN, ThisToken());

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
                            // TODO: '+', '-' and '.' can start the unquoted string.
                            // unquoted-string-character : space | plain-string-character
                            // plain-string-character : plus-sign | minus-sign | full-stop | digit | letter

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
                    else if (Tokenizer.IsPlainStringCharacter(input[i]))
                    {
                        // Missing separator.
                        if (atSep == false)
                        {
                            break;
                        }

                        var strValue = string.Empty;

                        var pc = (char)0;
                        var c = input[i];
                        while (c != Tokenizer.C_EOLN)
                        {
                            if (c == ',')
                            {
                                break;
                            }

                            // Not all characters are allowed.
                            // unquoted-string-character : space | plain-string-character
                            // plain-string-character : plus-sign | minus-sign | full-stop | digit | letter
                            if (Tokenizer.IsUquotedStringCharacter(c) == false)
                            {
                                throw _programState.ErrorAtLine("Unexpected plain string character '{0}'", c);
                            }

                            strValue += c;
                            pc = c;
                            c = input[++i];
                        }

                        // unquoted-string : plain-string-character [ { unquoted-string-character } plain-string-character ] .
                        if ((c == Tokenizer.C_EOLN || c == ',') && Tokenizer.IsPlainStringCharacter(pc) == false)
                        {
                            throw _programState.ErrorAtLine("Unexpected plain string character '{0}'", c);
                        }

                        valuesList.Add(string.IsNullOrWhiteSpace(strValue) ? string.Empty : ("$" + strValue.Trim())); // '$' = a string value type.

                        // Go back one character.
                        i--;

                        atSep = false;
                    }
                    else
                    {
                        // Missing separator.
                        if (atSep == false)
                        {
                            break;
                        }

                        // Skip white chars.
                        var c = input[i];
                        while (c != Tokenizer.C_EOLN)
                        {
                            if (Tokenizer.IsWhite(c) == false)
                            {
                                break;
                            }

                            c = input[++i];
                        }

                        // Go back one character.
                        i--;
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
                    _programState.NotifyError("Not enought or too much values.");

                    valuesList.Clear();

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
                                _programState.NotifyError("A string value expected for the {0} variable.", varName);

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
                                _programState.NotifyError("A numeric value expected for the {0} variable.", varName);

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
                    _programState.NotifyError("Can not assign values.");

                    valuesList.Clear();

                    continue;
                }

                break;
            }
        }

        // on-goto-statement = ON numeric-expression GO space* TO line-number ( comma line-number )*
        private ProgramLine OnStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("ON statement are not supported in the interactive mode.");
            }

            // Eat ON.
            NextToken();

            // The label position to jump to.
            var v = (int)NumericExpression();
            if (v < 1)
            {
                _programState.ErrorAtLine("GOTO label index < 1");
            }

            if (ThisToken().TokenCode == TokenCode.TOK_KEY_GO)
            {
                // Eat GO.
                NextToken();

                EatToken(TokenCode.TOK_KEY_TO);
            }
            else if (ThisToken().TokenCode == TokenCode.TOK_KEY_GOTO)
            {
                // Eat GOTO.
                NextToken();
            }
            else
            {
                _programState.UnexpectedTokenError(ThisToken());
            }

            var labelIndex = 0;
            var labels = new IToken[_programState.CurrentProgramLine.TokensCount()];

            // At least one label is expected.
            ExpToken(TokenCode.TOK_NUM, ThisToken());

            while (ThisToken().TokenCode != TokenCode.TOK_EOF)
            {
                if (ThisToken().TokenCode == TokenCode.TOK_EOLN)
                {
                    break;
                }

                if (ThisToken().TokenCode == TokenCode.TOK_NUM)
                {
                    labels[labelIndex++] = ThisToken();

                    NextToken();

                    if (ThisToken().TokenCode == TokenCode.TOK_EOLN)
                    {
                        break;
                    }

                    EatToken(TokenCode.TOK_LSTSEP);
                }
            }

            if (v > labelIndex)
            {
                throw _programState.ErrorAtLine("Not enought labels to go to");
            }

            return _programState.GetProgramLine((int)labels[v - 1].NumValue);
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
            NextToken();

            EatToken(TokenCode.TOK_KEY_BASE);

            // Array lower bound can not be changed, when an array is already defined.
            if (_programState.IsArrayDefined())
            {
                throw _programState.ErrorAtLine("An array is already defined. Can not change the arrays lower bound");
            }

            // 0 or 1.
            ExpToken(TokenCode.TOK_NUM, ThisToken());

            var option = (int)ThisToken().NumValue;
            if (option < 0 || option > 1)
            {
                throw _programState.ErrorAtLine("Array base out of allowed range 0 .. 1");
            }

            _programState.ArrayBase = option;

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // LET var = expr EOLN
        // var :: num-var | string-var | array-var
        private ProgramLine LetStatement()
        {
            EatToken(TokenCode.TOK_KEY_LET);

            // var
            if (ThisToken().TokenCode == TokenCode.TOK_SVARIDNT)
            {
                var varName = ThisToken().StrValue;

                // Eat the variable identifier.
                NextToken();

                // Array subscript.
                if (ThisToken().TokenCode == TokenCode.TOK_LBRA)
                {
                    NextToken();

                    var index = (int)NumericExpression();

                    EatToken(TokenCode.TOK_RBRA);

                    CheckArray(varName, 10, index, true);

                    EatToken(TokenCode.TOK_EQL);

                    _programState.SetArray(varName, index, NumericExpression());
                }
                else
                {
                    CheckSubsription(varName);

                    EatToken(TokenCode.TOK_EQL);

                    _programState.SetNVar(varName, NumericExpression());
                }
            }
            else if (ThisToken().TokenCode == TokenCode.TOK_VARIDNT)
            {
                var varName = ThisToken().StrValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TokenCode.TOK_EQL);

                _programState.SetNVar(varName, NumericExpression());
            }
            else if (ThisToken().TokenCode == TokenCode.TOK_STRIDNT)
            {
                var varName = ThisToken().StrValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TokenCode.TOK_EQL);

                _programState.SetSVar(varName, StringExpression(ThisToken()));

                // Eat the string expression.
                NextToken();
            }
            else
            {
                throw _programState.UnexpectedTokenError(ThisToken());
            }

            // EOLN
            ExpToken(TokenCode.TOK_EOLN, ThisToken());

            return NextProgramLine(); 
        }

        // PRINT [ expr { print-sep expr } ] EOLN
        // print-sep :: ';' | ','
        private ProgramLine PrintStatement()
        {
            // Eat PRINT.
            NextToken();

            bool atSep = true;
            while (ThisToken().TokenCode != TokenCode.TOK_EOLN)
            {
                switch (ThisToken().TokenCode)
                {
                    // Consume these.
                    case TokenCode.TOK_LSTSEP:
                    case TokenCode.TOK_PLSTSEP:
                        atSep = true;
                        NextToken();
                        break;

                    default:
                        if (atSep == false)
                        {
                            throw _programState.ErrorAtLine("A list separator expected");
                        }

                        if (IsStringExpression(ThisToken()))
                        {
                            Console.Write(StringExpression(ThisToken()));

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

            ExpToken(TokenCode.TOK_EOLN, ThisToken());

            Console.WriteLine();

            return _programState.NextProgramLine(_programState.CurrentProgramLine.Label);
        }

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private ProgramLine RandomizeStatement()
        {
            ExpToken(TokenCode.TOK_EOLN, NextToken());

            _programState.Randomize();

            return NextProgramLine();
        }

        // The READ statement.
        // READ var { ',' var } EOLN
        // var :: num-var | string-var | array-var
        private ProgramLine ReadStatement()
        {
            EatToken(TokenCode.TOK_KEY_READ);

            var wasVar = false;
            while (true)
            {
                // var
                if (ThisToken().TokenCode == TokenCode.TOK_SVARIDNT)
                {
                    var varName = ThisToken().StrValue;
                                       
                    // Eat the variable identifier.
                    NextToken();

                    // Array subscript.
                    if (ThisToken().TokenCode == TokenCode.TOK_LBRA)
                    {
                        NextToken();

                        var index = (int)NumericExpression();

                        EatToken(TokenCode.TOK_RBRA);

                        CheckArray(varName, 10, index, true);
                        
                        _programState.SetArray(varName, index, _programState.NextNumericData());
                    }
                    else
                    {
                        CheckSubsription(varName);

                        _programState.SetNVar(varName, _programState.NextNumericData());
                    }

                    wasVar = true;
                }
                else if (ThisToken().TokenCode == TokenCode.TOK_VARIDNT)
                {
                    var varName = ThisToken().StrValue;

                    // Eat the variable identifier.
                    NextToken();
                                        
                    _programState.SetNVar(varName, _programState.NextNumericData());

                    wasVar = true;
                }
                else if (ThisToken().TokenCode == TokenCode.TOK_STRIDNT)
                {
                    var varName = ThisToken().StrValue;

                    // Eat the variable identifier.
                    NextToken();

                    _programState.SetSVar(varName, _programState.NextStringData());

                    wasVar = true;
                }

                if (ThisToken().TokenCode == TokenCode.TOK_LSTSEP)
                {
                    // Eat ','.
                    NextToken();
                }
                else
                {
                    break;
                }
            }

            // EOLN
            ExpToken(TokenCode.TOK_EOLN, ThisToken());

            if (wasVar == false)
            {
                throw _programState.ErrorAtLine("At least one variable expected");
            }

            return NextProgramLine();
        }

        // The comment.
        // REM ... EOLN
        private ProgramLine RemStatement()
        {
            return NextProgramLine();
        }

        // Allows re-readind data.
        // RESTORE EOLN
        private ProgramLine RestoreStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("RESTORE statement is not supported in the interactive mode.");
            }

            ExpToken(TokenCode.TOK_EOLN, NextToken());

            _programState.RestoreData();

            return NextProgramLine();
        }

        // Returns from a subroutine.
        // RETURN EOLN
        private ProgramLine ReturnStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("RETURN statement is not supported in the interactive mode.");
            }

            ExpToken(TokenCode.TOK_EOLN, NextToken());

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

            ExpToken(TokenCode.TOK_EOLN, NextToken());

            _programState.WasEnd = true;

            return null;
        }

        #endregion


        #region expressions

        // string-expression : quoted-string | string-variable .
        private bool IsStringExpression(IToken token)
        {
            return token.TokenCode == TokenCode.TOK_QSTR || token.TokenCode == TokenCode.TOK_STRIDNT;
        }

        // string-expression : string-variable | string-constant .
        private string StringExpression(IToken token)
        {
            switch (token.TokenCode)
            {
                case TokenCode.TOK_QSTR: return token.StrValue;
                case TokenCode.TOK_STRIDNT: return _programState.GetSVar(token.StrValue);

                default:
                    throw _programState.UnexpectedTokenError(token);
            }
        }

        // numeric-expression : [ sign ] term { sign term } .
        // term : number | numeric-variable .
        // sign : '+' | '-' .
        private float NumericExpression(string paramName = null, float? paramValue = null)
        {
            var negate = false;
            if (ThisToken().TokenCode == TokenCode.TOK_PLUS)
            {
                NextToken();
            }
            else if (ThisToken().TokenCode == TokenCode.TOK_MINUS)
            {
                negate = true;
                NextToken();
            }

            var v = Term(paramName, paramValue);

            while (true)
            {
                if (ThisToken().TokenCode == TokenCode.TOK_PLUS)
                {
                    NextToken();

                    v += Term(paramName, paramValue);
                }
                else if (ThisToken().TokenCode == TokenCode.TOK_MINUS)
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
                if (ThisToken().TokenCode == TokenCode.TOK_MULT)
                {
                    NextToken();

                    v *= Factor(paramName, paramValue);
                }
                else if (ThisToken().TokenCode == TokenCode.TOK_DIV)
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
                if (ThisToken().TokenCode == TokenCode.TOK_POW)
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
            switch (ThisToken().TokenCode)
            {
                case TokenCode.TOK_NUM:
                    var n = ThisToken().NumValue;
                    NextToken();
                    return n;

                case TokenCode.TOK_SVARIDNT:
                    {
                        var varName = ThisToken().StrValue;
                        NextToken();

                        // Array subscript.
                        if (ThisToken().TokenCode == TokenCode.TOK_LBRA)
                        {
                            NextToken();

                            var index = (int)NumericExpression();

                            EatToken(TokenCode.TOK_RBRA);

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

                case TokenCode.TOK_VARIDNT:
                    {
                        var v = _programState.GetNVar(ThisToken().StrValue);
                        NextToken();
                        return v;
                    }

                case TokenCode.TOK_LBRA:
                    {
                        NextToken();
                        var v = NumericExpression();
                        EatToken(TokenCode.TOK_RBRA);
                        return v;
                    }

                case TokenCode.TOK_FN:
                    {
                        float v;
                        var fnName = ThisToken().StrValue;
                        if (fnName == "RND")
                        {
                            NextToken();

                            return (float)_programState.NextRandom();
                        }
                        else
                        {
                            NextToken();

                            EatToken(TokenCode.TOK_LBRA);

                            v = NumericExpression();

                            EatToken(TokenCode.TOK_RBRA);
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

                case TokenCode.TOK_UFN:
                    {
                        float v;
                        var fname = ThisToken().StrValue;
                        var flabel = _programState.GetUserFnLabel(fname);
                        if (flabel == 0)
                        {
                            throw _programState.ErrorAtLine("Undefined user function {0}", fname);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNA(X)
                        var p = (float?)null;
                        if (ThisToken().TokenCode == TokenCode.TOK_LBRA)
                        {
                            NextToken();
                            p = NumericExpression();
                            EatToken(TokenCode.TOK_RBRA);
                        }

                        // Remember, where we are.
                        var cpl = _programState.CurrentProgramLine;

                        // Go to the user function definition.
                        _programState.SetCurrentProgramLine(_programState.GetProgramLine(flabel));

                        // DEF
                        NextToken();
                        EatToken(TokenCode.TOK_KEY_DEF);

                        // Function name.
                        ExpToken(TokenCode.TOK_UFN, ThisToken());

                        if (fname != ThisToken().StrValue)
                        {
                            throw _programState.ErrorAtLine("Unexpected {0} function definition", ThisToken().StrValue);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNx(X)
                        var paramName = (string)null;
                        if (ThisToken().TokenCode == TokenCode.TOK_LBRA)
                        {
                            if (p.HasValue == false)
                            {
                                throw _programState.ErrorAtLine("The {0} function expects a parameter", fname);
                            }

                            // Eat '(';
                            NextToken();

                            // A siple variable name (A .. Z) expected.
                            ExpToken(TokenCode.TOK_SVARIDNT, ThisToken());

                            paramName = ThisToken().StrValue;

                            NextToken();
                            EatToken(TokenCode.TOK_RBRA);
                        }
                        else
                        {
                            if (p.HasValue)
                            {
                                throw _programState.ErrorAtLine("The {0} function does not expect a parameter", fname);
                            }
                        }

                        // '='
                        EatToken(TokenCode.TOK_EQL);

                        v = NumericExpression(paramName, p);

                        ExpToken(TokenCode.TOK_EOLN, ThisToken());

                        // Restore the previous position.
                        _programState.SetCurrentProgramLine(cpl, false);

                        return v;
                    }

                default:
                    throw _programState.UnexpectedTokenError(ThisToken());
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
        /// Returns the current token from the current program line.
        /// </summary>
        /// <returns>The current token from the current program line.</returns>
        private IToken ThisToken()
        {
            return _programState.CurrentProgramLine.ThisToken();
        }

        /// <summary>
        /// Returns the next token from the current program line.
        /// </summary>
        /// <returns>The next token from the current program line.</returns>
        private IToken NextToken()
        {
            return _programState.CurrentProgramLine.NextToken();
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not
        /// or goes tho the next one, if it is.
        /// </summary>
        /// <param name="expTokCode">The expected token code.</param>
        private void EatToken(TokenCode expTokCode)
        {
            ExpToken(expTokCode, ThisToken());
            NextToken();
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not.
        /// </summary>
        /// <param name="expTokCode">The expected token code.</param>
        /// <param name="token">The checked token.</param>
        private void ExpToken(TokenCode expTokCode, IToken token)
        {
            if (token.TokenCode != expTokCode)
            {
                throw _programState.UnexpectedTokenError(token);
            }
        }

        /// <summary>
        /// Checks, if this token is a label, if it is from the allowed range of labels
        /// and if such label/program line actually exists.
        /// </summary>
        /// <returns>The integer value representing this label.</returns>
        private int ExpLabel()
        {
            ExpToken(TokenCode.TOK_NUM, ThisToken());

            var label = (int)ThisToken().NumValue;

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

        #endregion

        #endregion
    }
}
