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


    public class Scanner
    {
        /// <summary>
        /// The program state instance this tokenizer works with.
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
        /// Exctracts labels, line starts and ends, etc.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="interactiveMode">A program line in the interactive mode can exist, 
        /// so the user can redefine it, an can be empty, so the user can delete it.</param>
        public void ScanSource(string source, bool interactiveMode = false)
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

                        if (label < 1 || label > ProgramState.MaxLabel)
                        {
                            throw ProgramState.Error("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, ProgramState.MaxLabel);
                        }

                        if (Tokenizer.IsWhite(c) == false)
                        {
                            throw ProgramState.Error("Label {0} at line {1} is not separated from the statement by a white character.", label, line);
                        }

                        if (interactiveMode == false && ProgramState.GetProgramLine(label) != null)
                        {
                            throw ProgramState.Error("Label {0} redefinition at line {1}.", label, line);
                        }
                                               
                        // Remember this program line.
                        programLine.Source = source;
                        programLine.Label = label;
                        programLine.Start = i;
                        programLine.SourcePosition = programLine.Start - 1;

                        // Remember this line.
                        ProgramState.SetProgramLine(programLine);

                        atLineStart = false;
                    }
                    else
                    {
                        throw ProgramState.Error("Label not found at line {0}.", line);
                    }
                }

                if (c == Tokenizer.C_EOLN)
                {
                    // The '\n' character.
                    programLine.End = i;

                    // Max program line length check.
                    if (programLine.Length > ProgramState.MaxProgramLineLength)
                    {
                        throw ProgramState.Error("The line {0} is longer than {1} characters.", line, ProgramState.MaxProgramLineLength);
                    }

                    // An empty line?
                    if (interactiveMode && string.IsNullOrWhiteSpace(programLine.Source.Substring(programLine.Start, programLine.End - programLine.Start)))
                    {
                        // Remove the existing program line.
                        ProgramState.RemoveProgramLine(programLine.Label);
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
                throw ProgramState.Error("No line end at line {0}.", line);
            }
        }
    }
}
