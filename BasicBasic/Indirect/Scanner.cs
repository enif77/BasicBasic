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

    using BasicBasic.Indirect.Tokens;
    

    public class Scanner
    {
        /// <summary>
        /// The program state instance this scanner works with.
        /// </summary>
        public ProgramState ProgramState { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public Scanner(ProgramState programState)
        {
            if (programState == null) throw new ArgumentNullException(nameof(programState));

            ProgramState = programState;
        }


        /// <summary>
        /// Scans the source for program lines.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="interactiveMode">A program line in the interactive mode can exist, 
        /// so the user can redefine it, and can be empty, so the user can delete it.</param>
        public ProgramLine ScanSource(string source, bool interactiveMode = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var tokenizer = new Tokenizer(ProgramState)
            {
                Source = source
            };

            var token = tokenizer.NextToken();
            var line = 1;
            var atLineStart = true;
            ProgramLine programLine = null;
            while (token.TokenCode != TokenCode.TOK_EOF)
            {
                if (atLineStart)
                {
                    int label;
                    if (token.TokenCode != TokenCode.TOK_NUM)
                    {
                        if (interactiveMode == false)
                        {
                            throw ProgramState.UnexpectedTokenError(token);
                        }

                        // Interactive mode program line.
                        label = -1;
                    }
                    else
                    {
                        label = (int)token.NumValue;
                        if (label < 1 || label > ProgramState.MaxLabel)
                        {
                            throw ProgramState.Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, ProgramState.MaxLabel);
                        }
                    }
                    
                    //if (Tokenizer.IsWhite(c) == false)
                    //{
                    //    throw ProgramState.Error("Label {0} at line {1} is not separated from the statement by a white character.", label, line);
                    //}

                    if (interactiveMode == false && ProgramState.GetProgramLine(label) != null)
                    {
                        throw ProgramState.Error("Label {0} redefinition at line {1}.", label, line);
                    }

                    // Create a new program line.
                    programLine = new ProgramLine()
                    {
                        Label = label
                    };

                    // An interactive mode program line?
                    if (label == -1)
                    {
                        // Remember the current token (a command).
                        programLine.AddToken(token);
                    }

                    atLineStart = false;
                }
                else
                {
                    // Save all tokens to the program line.
                    if (token.TokenCode == TokenCode.TOK_EOLN)
                    {
                        // EOLN is a part of a program line.
                        programLine.AddToken(token);

                        if (interactiveMode && programLine.Label == -1)
                        {
                            // Return the scanned program line.
                            return programLine;
                        }

                        // Remember this line.
                        ProgramState.SetProgramLine(programLine);
                        programLine = null;
                        atLineStart = true;
                        line++;
                    }
                    else if (token.TokenCode == TokenCode.TOK_KEY_REM)
                    {
                        // Skip the remark.
                        programLine.AddToken(new RemToken(tokenizer.SkipToEoln()));
                    }
                    else
                    {
                        programLine.AddToken(token);
                    }
                }

                token = tokenizer.NextToken();
            }

            if (interactiveMode && programLine != null)
            {
                if (programLine.Label == -1)
                {
                    throw ProgramState.Error("A line end expected at the interactive line.");
                }

                // Remember this line.
                ProgramState.SetProgramLine(programLine);
                programLine = null;
            }

            // The last line does not ended with the '\n' character.
            if (programLine != null)
            {
                throw ProgramState.Error("No line end at line {0}.", line);
            }

            return null;
        }
    }
}
