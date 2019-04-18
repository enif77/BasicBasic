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
    /// <summary>
    /// A handler for errors.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Reports an general error.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        void NotifyError(string message, params object[] args);

        ///// <summary>
        ///// Reports the unexpected token error.
        ///// </summary>
        ///// <param name="tok">The unexpected token.</param>
        ///// <returns>An unexpected token error on a program line as a throwable exception.</returns>
        //InterpreterException UnexpectedTokenError(int tok);

        ///// <summary>
        ///// Gets a general error on a program line and returns it as a throwable exception.
        ///// </summary>
        ///// <param name="message">An error message.</param>
        ///// <param name="args">Error message arguments.</param>
        ///// <returns>A general error on a program line as a throwable exception.</returns>
        //InterpreterException ErrorAtLine(string message, params object[] args);

        /// <summary>
        /// Gets a general error description with parameters and returns it as a throwable exception.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        /// <returns>A general error as a throwable exception.</returns>
        InterpreterException Error(string message, params object[] args);
    }
}
