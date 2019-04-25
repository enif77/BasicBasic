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
    using System;


    /// <summary>
    /// The global program state.
    /// </summary>
    public class ProgramState
    {
        #region constants

        public readonly int MaxLabel = 9999;
        public readonly int MaxProgramLineLength = 72;  // ECMA-55
        public readonly int ReturnStackSize = 32;

        #endregion

        /// <summary>
        /// The currently interpreted program line.
        /// </summary>
        public ProgramLine CurrentProgramLine { get; private set; }

        /// <summary>
        /// True, it this program reaced the end line.
        /// </summary>
        public bool WasEnd { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public ProgramState(IErrorHandler errorHandler)
        {
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _errorHandler = errorHandler;

            //ProgramLines = new ProgramLine[MaxLabel + 1];
            //ReturnStack = new int[ReturnStackSize];
            //ReturnStackTop = -1;
            //UserFns = new int[('Z' - 'A') + 1];
            //Arrays = new float[('Z' - 'A') + 1][];
            //ArrayBase = -1;                  // -1 = not yet user defined = 0.
            //Random = new Random(20170327);

            //NVars = new float[(('Z' - 'A') + 1) * 10]; // A or A0 .. A9;
            //SVars = new string[('Z' - 'A') + 1];      // A$ .. Z$

            WasEnd = false;
        }


        #region errors

        private IErrorHandler _errorHandler;

        /// <summary>
        /// Reports an general error.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        public void NotifyError(string message, params object[] args)
        {
            _errorHandler.NotifyError(message, args);
        }

        /// <summary>
        /// Reports the unexpected token error.
        /// </summary>
        /// <param name="tok">The unexpected token.</param>
        /// <returns>An unexpected token error on a program line as a throwable exception.</returns>
        public InterpreterException UnexpectedTokenError(int tok)
        {
            return ErrorAtLine("Unexpected token {0}", tok);
        }

        /// <summary>
        /// Gets a general error on a program line and returns it as a throwable exception.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        /// <returns>A general error on a program line as a throwable exception.</returns>
        public InterpreterException ErrorAtLine(string message, params object[] args)
        {
            // Interactive mode?
            if (CurrentProgramLine.Label < 1)
            {
                if (args == null || args.Length == 0)
                {
                    return Error(message + ".");
                }
                else
                {
                    return Error("{0}.", string.Format(message, args));
                }
            }
            else
            {
                if (args == null || args.Length == 0)
                {
                    return Error("{0} at line {1}.", message, CurrentProgramLine.Label);
                }
                else
                {
                    return Error("{0} at line {1}.", string.Format(message, args), CurrentProgramLine.Label);
                }
            }
        }

        /// <summary>
        /// Gets a general error description with parameters and returns it as a throwable exception.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        /// <returns>A general error as a throwable exception.</returns>
        public InterpreterException Error(string message, params object[] args)
        {
            return _errorHandler.Error(message, args);
        }

        #endregion
    }
}
