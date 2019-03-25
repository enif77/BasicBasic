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

namespace BasicBasicApp
{
    using BasicBasic;
    using System;


    static class Program
    {
        static int Main(string[] args)
        {
            //if (args.Length < 1)
            //{
            //    Console.WriteLine("Usage: app.exe [ ... ] input.bas");

            //    return 1;
            //}

            try
            {
                var interpreter = new Interpreter();
                interpreter.Interpret(_source);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return 1;
            }

            return 0;
        }

        static string _source = @"1
05 FOR I = 1 TO I = 10 DO
10 PRINT ""Hello""
15
20 NEXT I
25 asfdfgd dfg dfkg dsgjuldjg lsdkjgusldfjg duslfkgj sudlgkj
30
99 END";
    }
}
