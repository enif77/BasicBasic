1 REM GO SUB test.
10 let a = 5
11 go sub 60
12 if a < 0 then 50
13 go to 11
50 STOP
60 print a
61 let a = a - 1
62 return
99 END