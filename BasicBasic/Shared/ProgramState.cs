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

namespace BasicBasic.Shared
{
    using System;
    using System.Collections.Generic;

    using BasicBasic.Shared.Tokens;


    /// <summary>
    /// The global program state.
    /// </summary>
    public class ProgramState : IErrorHandler
    {
        #region constants

        public readonly int MaxLabel = 9999;
        public readonly int MaxProgramLineLength = 72;  // ECMA-55
        public readonly int ReturnStackSize = 32;

        #endregion

        /// <summary>
        /// The currently interpreted program line.
        /// </summary>
        public IProgramLine CurrentProgramLine { get; private set; }

        /// <summary>
        /// True, it this program reaced the end line.
        /// </summary>
        public bool WasEnd { get; set; }

        /// <summary>
        /// If true, a BY or QUIT commands were executed, so the program should end.
        /// </summary>
        public bool QuitRequested { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public ProgramState(IErrorHandler errorHandler)
        {
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _errorHandler = errorHandler;

            ProgramLines = new IProgramLine[MaxLabel + 1];
            ReturnStack = new int[ReturnStackSize];
            ReturnStackTop = -1;
            UserFns = new int[('Z' - 'A') + 1];
            Arrays = new float[('Z' - 'A') + 1][];
            ArrayBase = -1;                  // -1 = not yet user defined = 0.
            Data = new TokensList();
            Random = new Random(20170327);

            NVars = new float[(('Z' - 'A') + 1) * 10]; // A or A0 .. A9;
            SVars = new string[('Z' - 'A') + 1];      // A$ .. Z$

            WasEnd = false;
            QuitRequested = false;
        }


        #region program lines

        private IProgramLine[] ProgramLines { get; }


        /// <summary>
        /// Checks, if a label is from allowed range.
        /// </summary>
        /// <param name="label">A label.</param>
        /// <param name="line">At which source line.</param>
        public void CheckLabel(int label, int line = -1)
        {
            if (label < 1 || label > MaxLabel)
            {
                if (line > 0)
                {
                    throw Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, MaxLabel);
                }
                else
                {
                    throw Error("The label {0} is out of <1 ... {2}> rangle.", label, MaxLabel);
                }
            }
        }
        
        /// <summary>
        /// Returns a program line for a specific label.
        /// </summary>
        /// <param name="label">A label.</param>
        /// <returns>A program line for a specific label.</returns>
        public IProgramLine GetProgramLine(int label)
        {
            return ProgramLines[label - 1];
        }

        /// <summary>
        /// Stores a program line for a certain label.
        /// </summary>
        /// <param name="programLine">A program line.</param>
        public void SetProgramLine(IProgramLine programLine)
        {
            ProgramLines[programLine.Label - 1] = programLine;
        }

        /// <summary>
        /// Returns the list of defined program lines.
        /// </summary>
        /// <returns>The list of defined program lines.</returns>
        public IEnumerable<string> GetProgramLines()
        {
            var list = new List<string>();

            for (var i = 0; i < ProgramLines.Length; i++)
            {
                if (ProgramLines[i] == null)
                {
                    continue;
                }

                list.Add(ProgramLines[i].ToString());
            }

            return list;
        }

        /// <summary>
        /// Removes a program line from the current program.
        /// </summary>
        /// <param name="label">A program line label to be removed.</param>
        public void RemoveProgramLine(int label)
        {
            ProgramLines[label - 1] = null;
        }

        /// <summary>
        /// Removes all program lines from this program.
        /// </summary>
        public void RemoveAllProgramLines()
        {
            for (var i = 0; i < ProgramLines.Length; i++)
            {
                ProgramLines[i] = null;
            }
        }

        /// <summary>
        /// Sets the current program line.
        /// </summary>
        /// <param name="programLine">A program line.</param>
        /// <param name="rewind">If true, call Rewind() on the program line to start its proccesing from its beginning.</param>
        public void SetCurrentProgramLine(IProgramLine programLine, bool rewind = true)
        {
            CurrentProgramLine = programLine;

            if (rewind)
            {
                CurrentProgramLine.Rewind();
            }
        }

        /// <summary>
        /// Gets the next defined program line.
        /// </summary>
        /// <param name="fromLabel">From which label should we start to look for the next program line.</param>
        /// <returns>The next defined program line or null.</returns>
        public IProgramLine NextProgramLine(int fromLabel)
        {
            // Interactive mode line.
            if (fromLabel < 0)
            {
                return null;
            }

            // Skip program lines without code.
            // NOTE: Because labels are 1 to N, but program lines are 0 to N,
            // by using the fromLabel value directly, we are effectivelly starting
            // one line behind the from-label program line.
            for (var label = fromLabel; label < ProgramLines.Length; label++)
            {
                if (ProgramLines[label] != null)
                {
                    return ProgramLines[label];
                }
            }

            return null;
        }

        #endregion


        #region return stack

        private int[] ReturnStack { get; set; }
        private int ReturnStackTop { get; set; }


        /// <summary>
        /// Pushes a label to the return stack.
        /// </summary>
        /// <param name="label">A label.</param>
        public void ReturnStackPushLabel(int label)
        {
            ReturnStack[++ReturnStackTop] = CurrentProgramLine.Label;
        }

        /// <summary>
        /// Returns a label from the top of the return stack.
        /// </summary>
        /// <returns>A label from the top of the return stack.</returns>
        public int ReturnStackPopLabel()
        {
            return ReturnStack[ReturnStackTop--];
        }

        /// <summary>
        /// Removes all labels from the the return stack.
        /// </summary>
        public void ReturnStackClear()
        {
            ReturnStackTop = -1;
        }

        #endregion


        #region user defined functions

        private int[] UserFns { get; set; }


        /// <summary>
        /// Checks, if an user defined function exists.
        /// </summary>
        /// <param name="fname">An user function name (FNx).</param>
        /// <returns>True, if such a user defined function is already defined.</returns>
        public bool IsUserFnDefined(string fname)
        {
            return GetUserFnLabel(fname) != 0;
        }

        /// <summary>
        /// Defines an user defined function.
        /// </summary>
        /// <param name="fname">An user function name (FNx).</param>
        /// <param name="label">A program line label, where this user defined function is defined.</param>
        public void DefineUserFn(string fname, int label)
        {
            UserFns[fname[2] - 'A'] = label;
        }

        /// <summary>
        /// Returns a program line label, where a user defined function is defined.
        /// </summary>
        /// <param name="fname">An user function name (FNx).</param>
        /// <returns>A program line label, where a user defined function is defined.</returns>
        public int GetUserFnLabel(string fname)
        {
            return UserFns[fname[2] - 'A'];
        }

        /// <summary>
        /// Undefines all user defined functions.
        /// </summary>
        public void ClearUserFunctions()
        {
            UserFns = new int[('Z' - 'A') + 1];
        }


        #endregion


        #region random

        /// <summary>
        /// The random number generator.
        /// </summary>
        private Random Random { get; set; }


        /// <summary>
        /// Reseeds the random numbers generator.
        /// </summary>
        public void Randomize()
        {
            Random = new Random((int)DateTime.Now.Ticks);
        }

        /// <summary>
        /// Returns the next random number from the random numbers generator.
        /// </summary>
        /// <returns></returns>
        public float NextRandom()
        {
            return (float)Random.NextDouble();
        }

        #endregion


        #region variables

        /// <summary>
        /// Numeric variables values.
        /// </summary>
        private float[] NVars { get; set; }

        /// <summary>
        /// String variables values.
        /// </summary>
        private string[] SVars { get; set; }


        /// <summary>
        /// Gets a value of a numeric variable.
        /// </summary>
        /// <param name="varName">A variable name.</param>
        /// <returns>A value of a numeric variable.</returns>
        public float GetNVar(string varName)
        {
            var n = varName[0] - 'A';
            var x = 0;
            if (varName.Length == 2)
            {
                x = varName[1] - '0';
            }

            return NVars[n * 10 + x];
        }

        /// <summary>
        /// Sets a value to a numeric variable.
        /// </summary>
        /// <param name="varName">A variable name.</param>
        /// <param name="v">A value.</param>
        public void SetNVar(string varName, float v)
        {
            var n = varName[0] - 'A';
            var x = 0;
            if (varName.Length == 2)
            {
                x = varName[1] - '0';
            }

            NVars[n * 10 + x] = v;
        }

        /// <summary>
        /// Gets a value of a string variable.
        /// </summary>
        /// <param name="varName">A variable name.</param>
        /// <returns>A value of a string variable.</returns>
        public string GetSVar(string varName)
        {
            return SVars[varName[0] - 'A'] ?? string.Empty;
        }

        /// <summary>
        /// Sets a value to a variable.
        /// </summary>
        /// <param name="varName">A variable name.</param>
        /// <param name="v">A value.</param>
        public void SetSVar(string varName, string v)
        {
            SVars[varName[0] - 'A'] = v;
        }

        /// <summary>
        /// Removes values from all variables.
        /// </summary>
        public void ClearVariables()
        {
            NVars = new float[(('Z' - 'A') + 1) * 10]; // A or A0 .. A9;
            SVars = new string[('Z' - 'A') + 1];      // A$ .. Z$
        }

        #endregion


        #region arrays

        /// <summary>
        /// User defined arrays.
        /// Overrides numeric variables. 
        /// If an array N is defined, the N numeric variable can not be used as an numeric variable anymore.
        /// </summary>
        private float[][] Arrays { get; set; }

        /// <summary>
        /// The lover bound array index.
        /// Can be -1, which is "not yet defined", so it can be changed by the OPTION statement.
        /// If its -1, it is like if it was 0.
        /// </summary>
        public int ArrayBase { get; set; }

        /// <summary>
        /// Return the internal array index of an arrray.
        /// </summary>
        /// <param name="arrayName"></param>
        /// <returns></returns>
        public int GetArrayIndex(string arrayName)
        {
            return arrayName[0] - 'A';
        }

        /// <summary>
        /// Checks, if an array is already defined.
        /// </summary>
        /// <param name="arrayName">An array name.</param>
        /// <returns>True, if an array is already defined.</returns>
        public bool IsArrayDefined(string arrayName)
        {
            return IsArrayDefined(GetArrayIndex(arrayName));
        }

        /// <summary>
        /// Checks, if an array is already defined.
        /// </summary>
        /// <param name="arrayIndex">An array index. If -1 then it checks, if any array is defined.</param>
        /// <returns>True, if an array is already defined.</returns>
        public bool IsArrayDefined(int arrayIndex = -1)
        {
            if (arrayIndex < 0)
            {
                for (var i = 0; i < Arrays.Length; i++)
                {
                    if (Arrays[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            return Arrays[arrayIndex] != null;
        }

        /// <summary>
        /// Creates a new array.
        /// </summary>
        /// <param name="arrayIndex">An array index.</param>
        /// <param name="topBound">The top array index bound.</param>
        public void DefineArray(int arrayIndex, int topBound)
        {
            var bottomBound = (ArrayBase < 0) ? 0 : ArrayBase;
            Arrays[arrayIndex] = new float[topBound - bottomBound + 1];
        }

        /// <summary>
        /// Returns the length of a defined array.
        /// </summary>
        /// <param name="arrayIndex">An array index.</param>
        /// <returns></returns>
        public int GetArrayLength(int arrayIndex)
        {
            return Arrays[arrayIndex].Length;
        }

        /// <summary>
        /// Returns a value from an array.
        /// </summary>
        /// <param name="arrayIndex">An array index.</param>
        /// <param name="index">A value index.</param>
        /// <returns>A value.</returns>
        public float GetArrayValue(int arrayIndex, int index)
        {
            return Arrays[arrayIndex][index];
        }

        /// <summary>
        /// Sets a value to a specific cell in an array.
        /// </summary>
        /// <param name="arrayName">An array name.</param>
        /// <param name="index">An index.</param>
        /// <param name="v">A value.</param>
        public void SetArray(string arrayName, int index, float v)
        {
            var bottomBound = (ArrayBase < 0) ? 0 : ArrayBase;

            Arrays[arrayName[0] - 'A'][index - bottomBound] = v;
        }

        /// <summary>
        /// Undefines all arrays.
        /// </summary>
        public void ClearArrays()
        {
            Arrays = new float[('Z' - 'A') + 1][];
            ArrayBase = -1;                  // -1 = not yet user defined = 0.
        }

        #endregion


        #region data

        private TokensList Data { get; set; }


        public void AddData(IToken token)
        {
            Data.Add(token);
        }


        public float NextNumericData()
        {
            var tok = Data.Next();
            if (tok.TokenCode == TokenCode.TOK_NUM)
            {
                return tok.NumValue;
            }

            throw ErrorAtLine("A numeric value in data expected");
        }


        public string NextStringData()
        {
            var tok = Data.Next();
            if (tok.TokenCode == TokenCode.TOK_STR)
            {
                return tok.StrValue;
            }

            throw ErrorAtLine("A string value in data expected");
        }


        public void ClearData()
        {
            Data.Clear();
        }


        public void RestoreData()
        {
            Data.Rewind();
        }

        #endregion


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
        public InterpreterException UnexpectedTokenError(IToken tok)
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
            if (CurrentProgramLine == null || CurrentProgramLine.Label < 1)
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
