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
    using System.IO;

    using BasicBasic.Shared;


    class Program : IErrorHandler
    {
        static int Main(string[] args)
        {
            try
            {
                var p = new Program();

                // TODO: Allow user to choose an interpreter implementation.
                p.Run(args, new Direct.Interpreter(p));
                //p.Run(args, new Indirect.Interpreter(p));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return 1;
            }

            return 0;
        }


        #region private

        private void Run(string[] args, IInterpreter interpreter)
        {
            interpreter.Initialize();

            if (args.Length > 0 && args[0].StartsWith("!") == false)
            {
                interpreter.Interpret(File.ReadAllText(args[0]));

                return;
            }

            var quitRequested = false;
            while (quitRequested == false)
            {
                Console.Write("> ");
                var input = Console.ReadLine().Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                try
                {
                    if (Tokenizer.IsDigit(input[0]))
                    {
                        interpreter.AddProgramLine(input);
                    }
                    else
                    {
                        quitRequested = interpreter.InterpretLine(input);
                    }
                }
                catch (InterpreterException ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.Message);
                }
            }
        }

        #endregion


        #region IErrorHandler

        /// <summary>
        /// Reports an general error.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        public void NotifyError(string message, params object[] args)
        {
            Console.Error.WriteLine(message, args);
        }

        /// <summary>
        /// Gets a general error on a program line and returns it as a throwable exception.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        /// <returns>A general error on a program line as a throwable exception.</returns>
        public InterpreterException ErrorAtLine(string message, params object[] args)
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

        /// <summary>
        /// Gets a general error description with parameters and returns it as a throwable exception.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <param name="args">Error message arguments.</param>
        /// <returns>A general error as a throwable exception.</returns>
        public InterpreterException Error(string message, params object[] args)
        {
            return new InterpreterException(string.Format(message, args));
        }
        
        #endregion
    }
}

