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
                Console.Error.WriteLine(ex.Message);

                return 1;
            }

            return 0;
        }

        //static string _source = @"1
        //05 FOR I = 1. TO I = 10.01234 STEP .25
        //10 PRINT ""Hello""
        //15
        //20 NEXT I
        //25 asfdfgd dfg dfkg dsgjuldjg lsdkjgusldfjg duslfkgj sudlgkj
        //30
        //99 END
        //";

        //static string _source = @"1
        //10 PRINT a1
        //11 print b$
        //12 PRINT ""Hello, world!""
        //15
        //99 END
        //";

        //        static string _source = @"1 rem Various tests.
        //10 PRINT 11
        //4 let d = +5.3E-4 * -2
        //5 let c2 = 10 ^ -2
        //7 REM A and A0 are the same variables.
        //8 let a = 5
        //9 let a0 = 6
        //6 let z9 = 666 / 6 * 3
        //11 LET A3 = 66
        //12 let b = -a3
        //13 let s$ = ""str""
        //14 let s1 = ""123""
        //15 let s2 = ""a123""
        //19 if a = 5 then 26
        //20 PRINT
        //23 print a0 + 100 - b, "" ""; a, "" "", a3, "" ""; b; "" "", s$, "" "", 78.25 
        //24 print ""s1 = ""; s1
        //25 print ""s2 = ""; s2
        //26 print ""z9 = ""; z9
        //27 print ""c = ""; c
        //30 go to 34
        //31 stop
        //34 PRINT ""Hello, world!""
        //40 let c = 5 * (3 + 2)
        //41 print ""c = ""; c
        //42 print ""d = ""; abs(d * sin(100))
        //43 print ""c2 = ""; c2
        //50 end
        //";

        //static string _source = @"1 REM While loop test.
        //10 let a = 5
        //11 if a < 1 then 99
        //12 PRINT ""Hello, world!""
        //15 let a = a - 1
        //20 goto 11
        //99 END
        //";

        //static string _source = @"1 REM GO SUB test.
        //10 let a = 5
        //11 go sub 60
        //12 if a < 0 then 50
        //13 go to 11
        //50 STOP
        //60 print a
        //61 let a = a - 1
        //62 return
        //99 END
        //";

        static string _source4 = @"1 REM User functions test.
08 let x = 3
09 let a = 10
10 def fna(x) = 5 + x
11 def fnb = a + fna(55)
12 PRINT ""fnb = "", fnb
13 PRINT ""x = "", x
99 END
";

        static string _source5 = @"1 REM RND test.
10 PRINT RND, "" "", RND
20 RANDOMIZE
30 PRINT RND, "" "", RND
35 print abs(-1)
36 let a = 123 + 5
37 print a
40 END
";

        static string _source = @"1 PRINT ""Arrays test""
10 option base 1
98 print ""OK""
99 END
";


    }
}
