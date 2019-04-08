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

    using BasicBasic;


    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var interpreter = new Interpreter();

                interpreter.Initialize();

                if (args.Length > 0 && args[0].StartsWith("!") == false)
                {
                    interpreter.Interpret(File.ReadAllText(args[0]));
                }
                else
                {
                    while (true)
                    {
                        Console.Write("> ");
                        var input = Console.ReadLine().Trim();

                        if (string.IsNullOrWhiteSpace(input))
                        {
                            continue;
                        }

                        if (input.StartsWith("BY", StringComparison.InvariantCultureIgnoreCase) ||
                            input.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase))
                        {
                            break;
                        }
                        else if (input.StartsWith("RUN", StringComparison.InvariantCultureIgnoreCase))
                        {
                            try
                            {
                                interpreter.Interpret();
                            }
                            catch (InterpreterException ex)
                            {
                                Console.WriteLine();
                                Console.WriteLine(ex.Message);
                            }
                        }
                        else if (input.StartsWith("NEW", StringComparison.InvariantCultureIgnoreCase))
                        {
                            interpreter.RemoveAllProgramLines();
                        }
                        else if (input.StartsWith("LIST", StringComparison.InvariantCultureIgnoreCase))
                        {
                            foreach (var line in interpreter.ListProgramLines())
                            {
                                Console.WriteLine(line);
                            }
                        }
                        else
                        {
                            try
                            {
                                if (interpreter.IsDigit(input[0]))
                                {
                                    interpreter.AddProgramLine(input + "\n");
                                }
                                else
                                {
                                    interpreter.InterpretLine(input + "\n");
                                }
                            }
                            catch (InterpreterException ex)
                            {
                                Console.WriteLine();
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return 1;
            }

            return 0;
        }
    }
}

