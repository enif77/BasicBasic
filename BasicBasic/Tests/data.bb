1  REM Lists all items in data until the user decides
2  REM to finish this program by entering "y" (without quotes) 
3  REM on the INPUT statement prompt.
4  REM
5  REM -1 here is the end-of-data mark.
10 DATA 1, 2, 3, 4, 5, 6, -1
20 READ A
25 IF A < 0 THEN 100
30 PRINT A + I
40 GOTO 20
90 REM
95 REM Here starts the user interaction part...
99 REM
100 PRINT "End?"
110 INPUT A$
120 IF A$ = "y" THEN 999
130 RESTORE
135 LET I = I + 1
140 GOTO 20
999 END
