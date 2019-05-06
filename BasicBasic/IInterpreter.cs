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
    using System.Collections.Generic;


    /// <summary>
    /// Defines an interpreter interface.
    /// </summary>
    public interface IInterpreter
    {
        /// <summary>
        /// Initializes this interpereter instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Interprets the currently defined program.
        /// </summary>
        void Interpret();

        /// <summary>
        /// Scans the given sorce and interprets it.
        /// </summary>
        /// <param name="source"></param>
        void Interpret(string source);

        /// <summary>
        /// Interprets a single program line.
        /// </summary>
        /// <param name="source">A program line source.</param>
        /// <returns>True, if program exit was requested. (BY/QUIT commands executed.)</returns>
        bool InterpretLine(string source);

        /// <summary>
        /// Returns the list of defined program lines.
        /// </summary>
        /// <returns>The list of defined program lines.</returns>
        IEnumerable<string> ListProgramLines();

        /// <summary>
        /// Adds a new rogram line to the current program.
        /// </summary>
        /// <param name="source">A program line source.</param>
        void AddProgramLine(string source);

        /// <summary>
        /// Removes a program line from the current program.
        /// </summary>
        /// <param name="label">A program line label to be removed.</param>
        void RemoveProgramLine(int label);

        /// <summary>
        /// Removes all program lines from this program.
        /// </summary>
        void RemoveAllProgramLines();
    }
}
