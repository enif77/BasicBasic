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
    public class Interpreter
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
                //case Tokenizer.TOK_KEY_DEF: return DefStatement();
                //case Tokenizer.TOK_KEY_DIM: return DimStatement();
                case TokenCode.TOK_KEY_END: return EndStatement();
                //case Tokenizer.TOK_KEY_GO:
                //case Tokenizer.TOK_KEY_GOSUB:
                //case Tokenizer.TOK_KEY_GOTO:
                //    return GoToStatement();
                //case Tokenizer.TOK_KEY_IF: return IfStatement();
                //case Tokenizer.TOK_KEY_INPUT: return InputStatement();
                //case Tokenizer.TOK_KEY_LET: return LetStatement();
                //case Tokenizer.TOK_KEY_OPTION: return OptionStatement();
                //case Tokenizer.TOK_KEY_PRINT: return PrintStatement();
                case TokenCode.TOK_KEY_RANDOMIZE: return RandomizeStatement();
                case TokenCode.TOK_KEY_REM: return RemStatement();
                case TokenCode.TOK_KEY_RETURN: return ReturnStatement();
                case TokenCode.TOK_KEY_STOP: return StopStatement();

                default:
                    throw _programState.UnexpectedTokenError(token);
            }
        }


        #region statements

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

        // Reseeds the random number generator.
        // RANDOMIZE EOLN
        private ProgramLine RandomizeStatement()
        {
            ExpToken(TokenCode.TOK_EOLN, NextToken());

            _programState.Randomize();

            return NextProgramLine();
        }

        // The comment.
        // REM ...
        private ProgramLine RemStatement()
        {
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


        #region tokenizer

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
        /// Throws the unexpected token error if not.
        /// </summary>
        /// <param name="expTok">The expected token.</param>
        private void ExpToken(TokenCode expTokCode, IToken token)
        {
            if (token.TokenCode != expTokCode)
            {
                throw _programState.UnexpectedTokenError(token);
            }
        }

        #endregion

        #endregion
    }
}
