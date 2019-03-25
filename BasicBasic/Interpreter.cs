using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBasic
{
    public class Interpreter
    {
        #region constants

        public readonly int MaxLabel = 99;
        public readonly int MaxProgramLineLength = 72;  // ECMA-55

        #endregion


        #region ctor

        public Interpreter()
        {
            // label :: integer <1 .. 99>
            

            // variable = float <'A' .. 'Z'>
            _vars = new float['Z' - 'A'];
        }

        #endregion


        #region public

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


        public int Interpret(string source)
        {
            if (source == null) throw new InterpreterException("A source expected.");

            _source = source;
            _programLines = new ProgramLine[MaxLabel + 1];

            ScanForLabels();
            InterpretImpl();
            
            return 0;
        }


        #endregion


        #region private
        
        private void InterpretImpl()
        {
            var programLine = NextProgramLine(0);
            while (programLine != null)
            {
                programLine = InterpretLine(programLine);
            }
        }


        private ProgramLine InterpretLine(ProgramLine programLine)
        {
            Console.WriteLine("{0:000} -> {1}", programLine.Label, _source.Substring(programLine.Start, (programLine.End - programLine.Start) + 1));

            _currentProgramLine = programLine;
            _currentProgramLinePos = 0;

            var tok = NextToken();
            while (tok != TOK_EOLN)
            {
                // Do something.

                tok = NextToken();
            }

            return NextProgramLine(programLine.Label);
        }


        private int NextToken()
        {
            if (_currentProgramLine.Start + _currentProgramLinePos > _currentProgramLine.End)
            {
                throw new InterpreterException(string.Format("Read beyond the line end at line {0}.", _currentProgramLine.Label));
            }

            var c = NextChar();
            while (c != C_EOLN)
            {
                // Tokenize.

                //Console.WriteLine("C[{0:00}]: {1}", _currentProgramLinePos, c);

                c = NextChar();
            }

            return TOK_EOLN;
        }


        private char NextChar()
        {
            return _source[_currentProgramLine.Start + _currentProgramLinePos++];
        }

        
        private ProgramLine NextProgramLine(int fromLabel)
        {
            // Skip program lines without code.
            for (var label = fromLabel; label < _programLines.Length; label++)
            {
                if (_programLines[label] != null)
                {
                    return _programLines[label];
                }
            }

            return null;
        }


        private void ScanForLabels()
        {
            ProgramLine programLine = null;
            var atLineStart = true;
            var line = 1;
            var i = 0;
            for (; i < _source.Length; i++)
            {
                var c = _source[i];

                if (atLineStart)
                {
                    programLine = new ProgramLine();

                    // Label.
                    if (char.IsDigit(c))
                    {
                        var programLineStart = i;
                        var label = 0;
                        while (char.IsDigit(c))
                        {
                            label = label * 10 + (c - '0');

                            i++;
                            if (i >= _source.Length)
                            {
                                break;
                            }

                            c = _source[i];
                        }

                        if (label < 1 || label > MaxLabel)
                        {
                            throw new InterpreterException(string.Format("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, MaxLabel));
                        }

                        if (_programLines[label - 1] != null)
                        {
                            throw new InterpreterException(string.Format("Label {0} redefinition at line {1}.", label, line));
                        }

                        // Remember this program line.
                        programLine.Label = label;
                        programLine.Start = programLineStart;

                        // Remember this line.
                        _programLines[label - 1] = programLine;

                        // Re read the char behind the label.
                        i--;

                        atLineStart = false;
                    }
                    else
                    {
                        throw new InterpreterException(string.Format("Label not found at line {0}.", line));
                    }
                }

                //// Skip white chars.
                //if (c <= ' ' && c != '\n')
                //{
                //    continue;
                //}

                if (c == C_EOLN)
                {
                    // The character before '\n'.
                    programLine.End = i - 1;

                    // Max program line length check.
                    if (programLine.Length > MaxProgramLineLength)
                    {
                        throw new InterpreterException(string.Format("The line {0} is longer than {1} characters.", line, MaxProgramLineLength));
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
                throw new InterpreterException(string.Format("No line end at line {0}.", line));
            }
        }


        private class ProgramLine
        {
            public int Label { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public int Length { get { return (End - Start) + 1; } }


            public override string ToString()
            {
                return string.Format("{0}: {1} - {2}", Label, Start, End);
            }
        }


        private string _source;
        private int _currentProgramLinePos;
        private ProgramLine _currentProgramLine;
        private ProgramLine[] _programLines;

        private const char C_EOLN = '\n';

        private const int TOK_EOLN = 0;

        private float[] _vars;

        #endregion
    }
}

/*

5.2   _S_y_n_t_a_x

1. program          = block* end-line
2. block            = (line/for-block)*
3. line             = line-number statement end-of-line
4. line-number      = digit digit? digit? digit?
5. end-of-line      = [implementation defined]
6. end-line         = line-number end-statement end-of-line
7. end-statement    = END
8. statement        = data-statement / def-statement /
                        dimension -statement / gosub-statement /
                        goto-statement / if-then-statement /
                        input-statement / let-statement /
                        on-goto-statement / option-statement /
                        print-statement / randomize-statement /
                        read-statement / remark-statement /
                        restore-statement / return-statement /
                        stop statement 
     
*/
