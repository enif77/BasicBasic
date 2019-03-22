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

        #endregion


        #region ctor

        public Interpreter()
        {
            // label :: integer <1 .. 99>
            _programLines = new ProgramLine[MaxLabel + 1];

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

            ScanForLabels(source);
            InterpretImpl(source);
            
            return 0;
        }


        #endregion


        #region private
        
        private void InterpretImpl(string source)
        {
            var programLine = NextProgramLine(0);
            while (programLine != null)
            {
                programLine = InterpretLine(source, programLine);
            }
        }


        private ProgramLine InterpretLine(string source, ProgramLine programLine)
        {
            Console.WriteLine("{0:000} -> {1}", programLine.Label, source.Substring(programLine.Start, (programLine.End - programLine.Start) + 1));

            return NextProgramLine(programLine.Label);
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


        private void ScanForLabels(string source)
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

                    // Skip white chars.
                    if (c <= ' ' && c != '\n')
                    {
                        continue;
                    }

                    // Label.
                    if (char.IsDigit(c))
                    {
                        programLine.Start = i;
                        var label = 0;
                        while (char.IsDigit(c))
                        {
                            label = label * 10 + (c - '0');

                            i++;
                            if (i >= source.Length)
                            {
                                break;
                            }

                            c = source[i];
                        }

                        if (label < 1 || label > MaxLabel)
                        {
                            throw new InterpreterException(string.Format("Label {0} at line {1} out of <1 ... {2}> rangle.", label, line, MaxLabel));
                        }

                        if (_programLines[label - 1] != null)
                        {
                            throw new InterpreterException(string.Format("Label {0} redefinition at line {1}.", label, line));
                        }

                        // Remember this line's label.
                        programLine.Label = label;

                        // Remember this line.
                        _programLines[label - 1] = programLine;

                        // Re read the char behind the label.
                        i--;

                        atLineStart = false;
                    }
                }

                if (c == '\n')
                {
                    // The character before '\n'.
                    programLine.End = i - 1;

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
                programLine.End = source.Length - 1;
                if (programLine.End < 0)
                {
                    programLine.End = 0;
                }
            }
        }


        private class ProgramLine
        {
            public int Label { get; set; }
            public int Start { get; set; }
            public int End { get; set; }


            public override string ToString()
            {
                return string.Format("{0}: {1} - {2}", Label, Start, End);
            }
        }


        private ProgramLine[] _programLines;
        private float[] _vars;

        #endregion
    }
}
