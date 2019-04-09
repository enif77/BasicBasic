01 REM INPUT statement test.
10 PRINT "Enter your name:"
20 INPUT a$
30 PRINT "Hello, "; a$; "!"
40 PRINT "Wanna end now? (yes/no)"
50 INPUT a$
60 IF a$ <> "yes" THEN 10
70 PRINT "By, by!"
99 END
