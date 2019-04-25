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
    /// <summary>
    /// Holds information about a single program line.
    /// </summary>
    public class ProgramLine
    {
        /// <summary>
        /// The source of this program line.
        /// Can be the same for all program lines, if we are running a script,
        /// or each program line can have its own, if we are defining a program 
        /// in the interactive mode.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The current source position (from where was the last character).
        /// </summary>
        public int SourcePosition { get; set; }

        /// <summary>
        /// The label extracted from this program line.
        /// Can be -1, if it is a program line for immediate execution from the interactive
        /// mode. Meaning - there is no label int the source at all.
        /// </summary>
        public int Label { get; set; }

        /// <summary>
        /// The start index of this program line.
        /// Usually the first character after the label.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The end index of this program line.
        /// Points on the end-of-line ('\n') character.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The length of this program line in character.
        /// </summary>
        public int Length { get { return (End - Start) + 1; } }


        /// <summary>
        /// Returns the next character and advances the SourcePosition.
        /// </summary>
        /// <returns>The next character or 0 at the end of the Source.</returns>
        public char NextChar()
        {
            var p = SourcePosition + 1;
            if (p >= Start && p <= End)
            {
                SourcePosition = p;

                return Source[SourcePosition];
            }
            else
            {
                SourcePosition = End + 1;

                return (char)0;
            }
        }

        /// <summary>
        /// Returns the next character, but does not advance the SourcePosition.
        /// </summary>
        /// <returns>The next character or 0 at the end of the Source.</returns>
        public char PeekNextChar()
        {
            var p = SourcePosition + 1;
            if (p >= Start && p <= End)
            {
                return Source[SourcePosition];
            }
            else
            {
                return (char)0;
            }
        }

        /// <summary>
        /// Returns the previous character and advances the SourcePosition.
        /// </summary>
        /// <returns>The previous character or 0 at the beginning of the Source.</returns>
        public char PreviousChar()
        {
            var p = SourcePosition;
            if (p >= Start && p <= End)
            {
                SourcePosition--;

                return Source[p];
            }
            else if (SourcePosition > End)
            {
                SourcePosition = End;

                return PreviousChar();
            }
            else
            {
                SourcePosition = Start - 1;

                return (char)0;
            }
        }

        /// <summary>
        /// Returns the character at the current SourcePosition.
        /// Returns 0 untill the NextChar() was called atleast once.
        /// </summary>
        /// <returns>The the character at the current SourcePosition or 0.</returns>
        public char CurrentChar()
        {
            var p = SourcePosition;
            if (p >= Start && p <= End)
            {
                return Source[SourcePosition];
            }
            else
            {
                return (char)0;
            }
        }

        /// <summary>
        /// Returns to the first character.
        /// Call NextChar() to read the first character and let the CurrentChar() to return it.
        /// </summary>
        public void Rewind()
        {
            SourcePosition = Start - 1;
        }


        /// <summary>
        /// The string representation of this program line.
        /// </summary>
        /// <returns>The string representation of this program line.</returns>
        public override string ToString()
        {
            //return string.Format("{0}: {1} - {2}", Label, Start, End);
            return string.Format("{0}{1}",
                Label,
                Source.Substring(Start, End - Start));
        }
    }
}
