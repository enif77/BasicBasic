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

namespace BasicBasic.Direct
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using BasicBasic.Shared;
    using BasicBasic.Shared.Tokens;


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
        /// If true, a BY or QUIT commands were executed, so the program should end.
        /// </summary>
        public bool QuitRequested
        {
            get
            {
                if (_programState != null)
                {
                    return _programState.QuitRequested;
                }

                return true;
            }

            set
            {
                if (_programState != null)
                {
                    _programState.QuitRequested = value;
                }
            }
        }


        /// <summary>
        /// Initializes this interpereter instance.
        /// </summary>
        public void Initialize()
        {
            _programState = new ProgramState(_errorHandler);
            _tokenizer = new Tokenizer(_programState);

            _token = null;
        }

        /// <summary>
        /// Interprets the currently defined program.
        /// </summary>
        public void Interpret()
        {
            if (QuitRequested)
            {
                throw _programState.Error("Quit requested.");
            }

            InterpretImpl();
        }

        /// <summary>
        /// Scans the given source and interprets it.
        /// </summary>
        /// <param name="source"></param>
        public void Interpret(string source)
        {
            if (source == null) throw _programState.Error("A source expected.");

            if (QuitRequested)
            {
                throw _programState.Error("Quit requested.");
            }

            ScanSource(source);
            InterpretImpl();
        }

        /// <summary>
        /// Interprets a single program line.
        /// </summary>
        /// <param name="source">A program line source.</param>
        public bool InterpretLine(string source)
        {
            if (source == null) throw _programState.Error("A source expected.");

            if (QuitRequested)
            {
                throw _programState.Error("Quit requested.");
            }

            // Each token should be preceeded by at least a single white character.
            // So we have to add one here, if user do not inserted one.
            if (Tokenizer.IsWhite(source[0]) == false)
            {
                source = " " + source;
            }

            // We require the EOLN character...
            if (source.EndsWith("\n") == false)
            {
                source += "\n";
            }

            var programLine = new ProgramLine()
            {
                Source = source,
                Label = -1,
                Start = 0,
                End = source.Length - 1
            };

            InterpretLine(programLine);

            return QuitRequested;
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

            // We require the EOLN character...
            if (source.EndsWith("\n") == false)
            {
                source += "\n";
            }

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

        private IErrorHandler _errorHandler;
        private ProgramState _programState;


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
        private IProgramLine InterpretLine(IProgramLine programLine)
        {
            _programState.SetCurrentProgramLine(programLine);

            // The tokenizer, we are using here does not know, how to work with a "substring defined" source,
            // so we have to cut the substring here for it. It is not very effective though.
            _tokenizer.Source = ((ProgramLine)programLine).Source.Substring(((ProgramLine)programLine).Start, ((ProgramLine)programLine).Length);

            NextToken();

            if (_token.TokenCode == TokenCode.TOK_NUM)
            {
                // Eat label.
                NextToken();
            }

            // The statement.
            switch (_token.TokenCode)
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

                case TokenCode.TOK_KEY_CLS: return ClsCommand();
                case TokenCode.TOK_KEY_LIST: return ListCommand();
                case TokenCode.TOK_KEY_NEW: return NewCommand();
                case TokenCode.TOK_KEY_RUN: return RunCommand();
                case TokenCode.TOK_KEY_BY:
                case TokenCode.TOK_KEY_QUIT:
                    return QuitCommand();

                default:
                    throw _programState.UnexpectedTokenError(_token);
            }
        }


        #region interactive mode controll commands

        // CLS EOLN
        private IProgramLine ClsCommand()
        {
            if (IsInteractiveModeProgramLine() == false)
            {
                throw _programState.Error("CLS command is not supported outside of the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            Console.Clear();

            return null;
        }

        // LIST EOLN
        private IProgramLine ListCommand()
        {
            if (IsInteractiveModeProgramLine() == false)
            {
                throw _programState.Error("LIST command is not supported outside of the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            foreach (var line in ListProgramLines())
            {
                Console.WriteLine(line);
            }

            return null;
        }

        // NEW EOLN
        private IProgramLine NewCommand()
        {
            if (IsInteractiveModeProgramLine() == false)
            {
                throw _programState.Error("NEW command is not supported outside of the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            RemoveAllProgramLines();

            return null;
        }

        // RUN EOLN
        private IProgramLine RunCommand()
        {
            if (IsInteractiveModeProgramLine() == false)
            {
                throw _programState.Error("RUN command is not supported outside of the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            Interpret();

            return null;
        }

        // BY EOLN
        // QUIT EOLN
        private IProgramLine QuitCommand()
        {
            if (IsInteractiveModeProgramLine() == false)
            {
                throw _programState.Error("BY or QUIT commands are not supported outside of the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            QuitRequested = true;

            return null;
        }
        
        #endregion


        #region statements

        // DATA ...
        private IProgramLine DataStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("DATA statement is not supported in the interactive mode.");
            }

            return _programState.NextProgramLine();
        }

        // An user defined function.
        // DEF FNx = numeric-expression EOLN
        private IProgramLine DefStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("DEF statement is not supported in the interactive mode.");
            }

            EatToken(TokenCode.TOK_KEY_DEF);

            // Get the function name.
            ExpToken(TokenCode.TOK_UFN);
            var fname = _token.StrValue;

            // Do not redefine user functions.
            if (_programState.IsUserFnDefined(fname))
            {
                throw _programState.ErrorAtLine("{0} function redefinition", fname);
            }

            // Save this function definition.
            _programState.DefineUserFn(fname,  _programState.CurrentProgramLine.Label);

            return _programState.NextProgramLine();
        }

        // An array definition.
        // DIM array-declaration { ',' array-declaration } EOLN
        private IProgramLine DimStatement()
        {
            EatToken(TokenCode.TOK_KEY_DIM);

            ArrayDeclaration();

            // ','
            while (_token.TokenCode == TokenCode.TOK_LSTSEP)
            {
                EatToken(TokenCode.TOK_LSTSEP);

                ArrayDeclaration();
            }

            ExpToken(TokenCode.TOK_EOLN);

            return _programState.NextProgramLine();
        }

        // array-declaration : letter '(' integer ')' .
        private void ArrayDeclaration()
        {
            // Get the function name.
            ExpToken(TokenCode.TOK_SVARIDNT);

            var arrayName = _token.StrValue;

            // Eat array name.
            NextToken();

            EatToken(TokenCode.TOK_LBRA);
            ExpToken(TokenCode.TOK_NUM);

            var topBound = (int)_token.NumValue;

            CheckArray(arrayName, topBound, topBound, false);

            // Eat array upper bound.
            NextToken();

            EatToken(TokenCode.TOK_RBRA);
        }

        // The end of program.
        // END EOLN
        private IProgramLine EndStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("END statement is not supported in the interactive mode.");
            }

            EatToken(TokenCode.TOK_KEY_END);
            ExpToken(TokenCode.TOK_EOLN);

            var nextLine = _programState.NextProgramLine();
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
        private IProgramLine GoToStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("GO TO and GO SUB statements are not supported in the interactive mode.");
            }

            var gosub = false;

            // GO TO or GO SUB ...
            if (_token.TokenCode == TokenCode.TOK_KEY_GO)
            {
                // Eat TO.
                NextToken();

                // GO SUB?
                if (_token.TokenCode == TokenCode.TOK_KEY_SUB)
                {
                    gosub = true;
                }
                else
                {
                    ExpToken(TokenCode.TOK_KEY_TO);
                }
            }
            else if (_token.TokenCode == TokenCode.TOK_KEY_GOSUB)
            {
                gosub = true;
            }

            // Eat the statement.
            NextToken();

            // Get the label.
            var label = ExpLabel();
            NextToken();

            // EOLN.
            ExpToken(TokenCode.TOK_EOLN);

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
        private IProgramLine IfStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("IF statement is not supported in the interactive mode.");
            }

            EatToken(TokenCode.TOK_KEY_IF);

            // Do not jump.
            var jump = false;

            // String or numeric conditional jump?
            if (IsStringExpression())
            {
                var v1 = StringExpression();
                NextToken();

                var relTok = _token;
                NextToken();

                var v2 = StringExpression();
                NextToken();

                jump = _programState.StringComparison(relTok, v1, v2);
            }
            else
            {
                var v1 = NumericExpression();

                var relTok = _token;
                NextToken();

                var v2 = NumericExpression();

                jump = _programState.NumericComparison(relTok, v1, v2);
            }

            EatToken(TokenCode.TOK_KEY_THEN);

            // Get the label.
            var label = ExpLabel();

            // EOLN.
            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            return jump
                ? _programState.GetProgramLine(label) 
                : _programState.NextProgramLine();
        }

        // INPUT variable { ',' variable } EOLN
        private IProgramLine InputStatement()
        {
            // Eat INPUT.
            NextToken();

            var varsList = new List<string>();

            bool atSep = true;
            while (_token.TokenCode != TokenCode.TOK_EOLN)
            {
                switch (_token.TokenCode)
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

                        if (_token.TokenCode == TokenCode.TOK_STRIDNT || _token.TokenCode == TokenCode.TOK_SVARIDNT || _token.TokenCode == TokenCode.TOK_VARIDNT)
                        {
                            varsList.Add(_token.StrValue);

                            // Eat the variable.
                            NextToken();
                        }
                        else
                        {
                            throw _programState.UnexpectedTokenError(_token);
                        }

                        atSep = false;
                        break;
                }
            }

            ExpToken(TokenCode.TOK_EOLN);

            if (varsList.Count == 0)
            {
                throw _programState.ErrorAtLine("The INPUT statement variables list can not be empty");
            }

            _programState.ReadUserInput(varsList);

            return _programState.NextProgramLine();
        }
                
        // Sets the array bottom dimension.
        // OPTION BASE 1
        private IProgramLine OptionStatement()
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
            ExpToken(TokenCode.TOK_NUM);

            var option = (int)_token.NumValue;
            if (option < 0 || option > 1)
            {
                throw _programState.ErrorAtLine("Array base out of allowed range 0 .. 1");
            }

            _programState.ArrayBase = option;

            return _programState.NextProgramLine();
        }

        // LET var = expr EOLN
        // var :: num-var | string-var
        private IProgramLine LetStatement()
        {
            EatToken(TokenCode.TOK_KEY_LET);

            // var
            if (_token.TokenCode == TokenCode.TOK_SVARIDNT)
            {
                var varName = _token.StrValue;

                // Eat the variable identifier.
                NextToken();

                // Array subscript.
                if (_token.TokenCode == TokenCode.TOK_LBRA)
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
            else if (_token.TokenCode == TokenCode.TOK_VARIDNT)
            {
                var varName = _token.StrValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TokenCode.TOK_EQL);

                _programState.SetNVar(varName, NumericExpression());
            }
            else if (_token.TokenCode == TokenCode.TOK_STRIDNT)
            {
                var varName = _token.StrValue;

                // Eat the variable identifier.
                NextToken();

                EatToken(TokenCode.TOK_EQL);

                _programState.SetSVar(varName, StringExpression());

                // Eat the string expression.
                NextToken();
            }
            else
            {
                throw _programState.UnexpectedTokenError(_token);
            }
                        
            // EOLN
            ExpToken(TokenCode.TOK_EOLN);

            return _programState.NextProgramLine();
        }

        // ON ...
        private IProgramLine OnStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("ON statement is not supported in the interactive mode.");
            }

            // This statement does nothing in this implementation.

            return _programState.NextProgramLine();
        }

        // PRINT [ expr { print-sep expr } ] EOLN
        // print-sep :: ';' | ','
        private IProgramLine PrintStatement()
        {
            // Eat PRINT.
            NextToken();

            bool atSep = true;
            while (_token.TokenCode != TokenCode.TOK_EOLN)
            {
                switch (_token.TokenCode)
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

            ExpToken(TokenCode.TOK_EOLN);

            Console.WriteLine();

            return _programState.NextProgramLine();
        }

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private IProgramLine RandomizeStatement()
        {
            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            _programState.Randomize();

            return _programState.NextProgramLine();
        }

        // READ ...
        private IProgramLine ReadStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("READ statement is not supported in the interactive mode.");
            }

            // This statement does nothing in this implementation.

            return _programState.NextProgramLine();
        }

        // The comment.
        // REM ...
        private IProgramLine RemStatement()
        {
            return _programState.NextProgramLine();
        }

        // RESTORE ...
        private IProgramLine RestoreStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("RESTORE statement is not supported in the interactive mode.");
            }

            // This statement does nothing in this implementation.

            return _programState.NextProgramLine();
        }

        // Returns from a subroutine.
        // RETURN EOLN
        private IProgramLine ReturnStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("RETURN statement is not supported in the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

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
        private IProgramLine StopStatement()
        {
            if (IsInteractiveModeProgramLine())
            {
                throw _programState.Error("STOP statement is not supported in the interactive mode.");
            }

            NextToken();
            ExpToken(TokenCode.TOK_EOLN);

            _programState.WasEnd = true;

            return null;
        }

        #endregion


        #region expressions

        // expr:: string-expression | numeric-expression
        private bool IsStringExpression()
        {
            return _token.TokenCode == TokenCode.TOK_STR || _token.TokenCode == TokenCode.TOK_STRIDNT;
        }

        // string-expression : string-variable | string-constant .
        private string StringExpression()
        {
            switch (_token.TokenCode)
            {
                case TokenCode.TOK_STR: return _token.StrValue;
                case TokenCode.TOK_STRIDNT: return _programState.GetSVar(_token.StrValue);

                default:
                    throw _programState.UnexpectedTokenError(_token);
            }
        }

        // numeric-expression : [ sign ] term { sign term } .
        // term : number | numeric-variable .
        // sign : '+' | '-' .
        private float NumericExpression(string paramName = null, float? paramValue = null)
        {
            var negate = false;
            if (_token.TokenCode == TokenCode.TOK_PLUS)
            {
                NextToken();
            }
            else if (_token.TokenCode == TokenCode.TOK_MINUS)
            {
                negate = true;
                NextToken();
            }

            var v = Term(paramName, paramValue);

            while (true)
            {
                if (_token.TokenCode == TokenCode.TOK_PLUS)
                {
                    NextToken();

                    v += Term(paramName, paramValue);
                }
                else if (_token.TokenCode == TokenCode.TOK_MINUS)
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
                if (_token.TokenCode == TokenCode.TOK_MULT)
                {
                    NextToken();

                    v *= Factor(paramName, paramValue);
                }
                else if (_token.TokenCode == TokenCode.TOK_DIV)
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
                if (_token.TokenCode == TokenCode.TOK_POW)
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
            switch (_token.TokenCode)
            {
                case TokenCode.TOK_NUM:
                    var n = _token.NumValue;
                    NextToken();
                    return n;

                case TokenCode.TOK_SVARIDNT:
                    {
                        var varName = _token.StrValue;
                        NextToken();

                        // Array subscript.
                        if (_token.TokenCode == TokenCode.TOK_LBRA)
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
                        var v = _programState.GetNVar(_token.StrValue);
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
                        var fnName = _token.StrValue;
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
                        var fname = _token.StrValue;
                        var flabel = _programState.GetUserFnLabel(fname);
                        if (flabel == 0)
                        {
                            throw _programState.ErrorAtLine("Undefined user function {0}", fname);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNA(X)
                        var p = (float?)null;
                        if (_token.TokenCode == TokenCode.TOK_LBRA)
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
                        ExpToken(TokenCode.TOK_UFN);

                        if (fname != _token.StrValue)
                        {
                            throw _programState.ErrorAtLine("Unexpected {0} function definition", _token.StrValue);
                        }

                        // Eat the function name.
                        NextToken();

                        // FNx(X)
                        var paramName = (string)null;
                        if (_token.TokenCode == TokenCode.TOK_LBRA)
                        {
                            if (p.HasValue == false)
                            {
                                throw _programState.ErrorAtLine("The {0} function expects a parameter", fname);
                            }

                            // Eat '(';
                            NextToken();

                            // A siple variable name (A .. Z) expected.
                            ExpToken(TokenCode.TOK_SVARIDNT);

                            paramName = _token.StrValue;

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

                        ExpToken(TokenCode.TOK_EOLN);

                        // Restore the previous position.
                        _programState.SetCurrentProgramLine(cpl, false);

                        return v;
                    }
                    
                default:
                    throw _programState.UnexpectedTokenError(_token);
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

        private Tokenizer _tokenizer;

        /// <summary>
        /// The last found token.
        /// </summary>
        private IToken _token;


        /// <summary>
        /// Extracts the next token found in the current program line source.
        /// </summary>
        public void NextToken()
        {
            _token = _tokenizer.NextToken(false);
        }
        
        /// <summary>
        /// Checks, if this token is a label, if it is from the allowed range of labels
        /// and if such label/program line actually exists.
        /// </summary>
        /// <returns>The integer value representing this label.</returns>
        private int ExpLabel()
        {
            ExpToken(TokenCode.TOK_NUM);

            var label = (int)_token.NumValue;

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
        private void EatToken(TokenCode expTok)
        {
            ExpToken(expTok);
            NextToken();
        }

        /// <summary>
        /// Checks, if the given token is the one we expected.
        /// Throws the unexpected token error if not.
        /// </summary>
        /// <param name="expTok">The expected token.</param>
        private void ExpToken(TokenCode expTok)
        {
            if (_token.TokenCode != expTok)
            {
                throw _programState.UnexpectedTokenError(_token);
            }
        }

        #endregion


        #region scanner

        /// <summary>
        /// Scans the source for program lines.
        /// Exctracts labels, line starts and ends, etc.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="interactiveMode">A program line in the interactive mode can exist, 
        /// so the user can redefine it, and can be empty, so the user can delete it.</param>
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

                        if (Tokenizer.IsWhite(c) == false)
                        {
                            throw _programState.Error("Label {0} at line {1} is not separated from the statement by a white character.", label, line);
                        }

                        if (interactiveMode == false && _programState.GetProgramLine(label) != null)
                        {
                            throw _programState.Error("Label {0} redefinition at line {1}.", label, line);
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
                        throw _programState.Error("Label not found at line {0}.", line);
                    }
                }

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

        #endregion
    }
}
