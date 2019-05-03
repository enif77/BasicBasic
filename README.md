# BasicBasic Interpreter

BasicBasic is a basic implementation of the ECMA-55 Minimal BASIC System. 

Can execute a script in a file or work directly as a "super smart calculator thingy". :-)

Right now it is a bit incomplete. Bigest missing parts are:

  - PRINT statement output formatting.
  - FOR loop.
  - Two dimensional arrays.
  - Some more ECMA-55 standard defined constraints.

Some of those features will be implmented, some wont. 

More interesting informations and implementations of the Minimal Basic can be found here:

  - [ECMA-55 Minimal BASIC System - bas55](https://jorgicor.niobe.org/bas55/)
  - [John's ECMA-55 Minimal BASIC Compiler](https://buraphakit.sourceforge.io/BASIC.shtml)

## Implementation

There are multiple implementations of the Minimal BASIC System interpreter. 
Each shows a different approach to how to interpret a program source.

### Direct interpreter

Does no compilation at all. It reads the program source, splits it into tokens
and interprets each token directly. This happens all the time, so it is siple, but
very "slow" way of doing it. Dont worry, our computers are fast enough to handle it. :-)

Does NOT support - DATA, READ and RESTORE commands.

### Indirect interpreter

Still no real compilation here, but atleast it translates the program source into tokens
only once (during the scanner phase). It makes the interpretation much faster and allows
more error checks to happen before the run time (aka the interpretation phase).

Supports DATA, READ and RESTORE commands.

## Usage

If the first argument of the program is a path to an existing source file, it is loaded 
and interpreted. If the provided path starts with the exclamation character ('!'), 
it is ignored. Helps with debugging.

Without any program argument, the interactive mode starts.

## Interactive Mode

The interactive mode allows an user to immediatelly execute inserted program lines 
or to define program lines for later execution.

The command prompt for a new program line is "> ".
The command prompt for the INPUT statement is "? ".

All lines are passed to the interpreter for execution.

If a program line starts with a label (integer 1 .. 9999) it defines a new program line.
A program line can be redefined by a new program line starting with the same label.
If a program line contains only a label, it deletes an existing program line with 
the same label.

### The INPUT command user input processing

If the INPUT command is executed, a user must enter all required values in the correct 
order and format/type. Any error or missing/excessive value leads to a new input read 
cycle, where user must enter all values again, till he/she successfully enters 
everything in.

An empty input line ends the user input processing and a program continues, like the 
INPUT command was never executed. No variable values are changed.

### Interactive Mode Commands

#### BY or QUIT

Ends this app.

#### NEW

Clears all defined program lines.

#### LIST

Lists all currently defined program lines.

#### RUN

Executes the entered program.

## Language Commands

### DATA

Can be used to define a list of numeric or string constants.

#### Examples

<pre>
5  REM A list of data to be read into A, B$ and C variables.
10 DATA 1, "2", 3
15 READ A, B$, C
20 PRINT A, B$, C
25 END
</pre>

The output of this program will be "1 2 3".

### DEF

Allows user to define a custom numeric function, that can be later used in a numeric 
expression. Such a function can have up to a one parameter. 

An user defined function name has the <code>FNx</code> format, where <code>x</code> 
is a <code>'A'</code> to <code>'Z'</code> character. 

An user function must be defined first, then it can be used. An user function can not 
be redefined, once it was defined.  

A paremeter of an user defined function can be a character <code>'A'</code> to 
<code>'Z'</code>. Such parameter hides a variable with the same name inside the body 
of the user defined function.

#### Examples

<pre>
10 DEF FNA = N + 5
15 LET N = 5
20 PRINT FNA
25 END
</pre>

The output of this program will be 10.

<pre>
10 DEF FNA(X) = X + 5
20 PRINT FNA(5)
25 END
</pre>

The output of this program will be also 10.

### DIM

The DIM command is used to define one dimensional arrays. Each array has a single letter 
name from range <code>'A'</code> to <code>'Z'</code>. The default lower bound (the 
lowest array index) is 0 by default. It can be changed by the <code>OPTION BASE 1</code> 
command to 1. The upper bound (the highest array index) is set by the user in the array 
definition.

Arrays hides variables with same names. If an user defines an array A, such variable is
no more accesible. The A has to be used as an array sine then.

An array can not be defined more then once.

If a normal variable is accessed as an array for the first time, an array of 11 cells 
(or 10 cells if <code>OPTION BASE 1</code> command was used) is defined automatically.

#### Examples

<pre>
10 DIM A(100), B(30)
15 LET A(5) = 5
16 LET B(4) = 44 + C(5)
20 PRINT A(5) + B(4)
25 END
</pre>

### GOTO

Unconditional jump to an label. The other way, how to write the <code>GOTO</code> 
statement is write it as two words <code>GO TO</code>.

#### Examples

<pre>
10 PRINT "HELLO!"
20 GOTO 10
25 END
</pre>

An endless loop made by GOTO!

### GOSUB

Unconditional jump to an label with storing the current program line label as a "return
address" for the <code>RETURN</code> statement. The other way, how to write the 
<code>GOSUB</code> statement is write it as two words <code>GO SUB</code>.

This is the BASIC languague way, how to create procedures/methods.

#### Examples

<pre>
10 GOSUB 70
20 PRINT "SOMETHING..."
60 STOP
70 PRINT "HELLO!"
80 RETURN
99 END
</pre>

### IF THEN

An conditional jump to a label.

An condition is evaluated and if the result is true, program execution continues on the
label defined afther the <code>THEN</code> keyword.

#### Examples

<pre>
10 LET A = 5
20 PRINT A
30 LET A = A - 1
40 IF A > 0 THEN 20
99 END
</pre>

Prints out numbers from 5 to 1.

### INPUT

Reads data from input and sets them to variables.

#### Examples

<pre>
10 INPUT A, B$
20 PRINT A, B$
99 END
</pre>

### LET

Allows an user to set a value to a variable.

#### Examples

<pre>
10 LET A = 5
20 LET A$ = "A string."
30 LET A(4) = 10
99 END
</pre>

### OPTION BASE

Set the lower bound of arrays. Can be called only once and no array should be defined
before this command is executed. The lower bound can be either 0 (the default) or 1.

#### Examples

<pre>
10 OPTION BASE 1
20 DIM A(100)
99 END
</pre>

Will create array A with indexes from 1 to 100.

### PRINT

Prints a list of values to screen. Values can be separated by characters ',' or ';'.

#### Examples

<pre>
10 PRINT "HELLO", 100, A$; B(30), 58 * C
99 END
</pre>

### RANDOMIZE

Reinitializes the internal random number genarator.

### READ

Reads data into a variable(s).

<pre>
10 DATA 1, "2", 3
15 READ A, B$, C
20 RESTORE
35 READ D
40 PRINT A, B$, C, D
25 END
</pre>

The output of this program will be "1 2 3 1".

### REMARK

A remark or comment. Does nothing, but allows an user to enter whathever he/she needs
to make the program more readable.

### RESTORE

Starts reading data from the first item again.

See the example for the READ command.

### RETURN

Returns from a subprogram called by the <code>GOSUB</code> statement.

### STOP

Stops immediatelly program's execution.

## Build in functions

The values of the build in functions, as well as the number of arguments 
required for each function, are described below. In all cases, X stands for 
a numeric expression.

### ABS(X)

The absolute value of X.

### ATN(X)

The arctangent of X in radians, i.e. the angle whose tangent is X. The range of the 
function is -(pi / 2) < ATN(X) < (pi / 2) where pi is the ratio of the circumference 
of a circle to its diameter (3.14...).

### COS(X)

The cosine of X, where X is in radians.

### EXP(X)

The exponential of X, i.e. the value of the base of natural logarithms (e = 2,71828...)
raised to the power X; <i>if EXP(X) is less than machine infinitesimal, then its value shall
be replaced by zero</i>.

### INT(X)

The largest integer not greater than X; e.g. INT(1.3) = 1 and INT(-1.3) = -2.

### LOG(X)

The natural logarithm of X; X must be greater than zero.

### RND

The next pseudo-random  number in an implemen-tation-supplied sequence of 
pseudo-random num-bers uniformly distributed in the range 0 <= RND < 1.

### SGN(X)

The sign of X: -1 if X < 0, 0 if X = 0 and +1 if X > 0.

### SIN(X)

The sine of X, where X is in radians.

### SQR(X)

The nonnegative square root of X; X must be nonnegative.

### TAN(X)

The tangent of X, where X  is in radians.

## Syntax

<pre>
#  - A comment.
{} - Repeat 0 or more.
() - Groups things together.
[] - An optional part.
|  - Or.
&  - And.
!  - Not.
:  - A definition name and its definition separator.
-  - No white spaces allowed.
"" - A specific string (keyword etc.).
'' - A specific character.
.. - A range.
.  - The end of a definition.


program : { line } end-line .

line : label statement end-of line .

label : digit | digit-digit .

end-of-line : '\n' .

end-line : label end-statement end-of-line .

end-statement : "END" .

statement :
  def-statement |
  dim-statement |
  goto-statement | 
  gosub-statement | 
  if-then-statement |
  input-statement |
  let-statement |
  option-statement |
  print-statement |
  randomize-statement |
  remark-statement |
  return-statement |
  stop statement .

def-statement : "DEF" user-function-name [ '(' parameter-name ')' ] '=' numeric-expression

user-function-name : "FNA" .. 'FNZ' .

parameter-name : 'A' .. 'Z' .

dim-statement : "DIM" array-declaration { ',' array-declaration } .

array-declaration : array-name '(' number ')' . 

array-name : 'A' .. 'Z' .

goto-statement : ( "GO" "TO" label ) | ( "GOTO" label ) .

gosub-statement : ( "GO" "SUB" label ) | ( "GOSUB" label ) .

if-then-statement : "IF" expression "THEN" label .

input-statement : "INPUT" variable { ',' variable } .

### NOTE: The INPUT statement accepts numbers, quoted strings and unquoted strings.

let-statement : "LET" variable '=' expression .

option-statement : "OPTION" "BASE" ( '0' | '1' ) .

print-statement : [ print-list ] .

print-list : { print-item print-separator } print-item .

print-item : expression .

print-separator : ',' | ';' .

randomize-statement : "RANDOMIZE" .

remark-statement : "REM" { any-character } .

return-statement : "RETURN" .

stop-statement : "STOP" .

variable : numeric-variable | string-variable .

numeric-variable : leter [ - digit ] .

string-variable : leter - '$' .

leter : 'A' .. 'Z' .

digit : '0' .. '9' .

expression : numeric-expression | string-expression .

numeric-expression : [ sign ] term { sign term } .

sign : '+' | '-' .

term : factor { multiplier factor } .

multiplier : '*' | '/' .

factor : primary { '^' primary } .

primary : number | numeric-variable | numeric-function | '(' numeric-expression ')' | user-function | array-subscription .

numeric-function : numeric-function-name '(' numeric-expression ')' .

numeric-function-name : "ABS" | "ATN" | "COS" | "EXP" | "INT" | "LOG" | "RND" | "SGN" | "SIN" | "SQR" | "TAN" .

user-function : user-function-name [ '(' numeric-function ')' ] .

number : 
    ( [ sign - ] decimal-part [ - fractional-part ] [ - exponent-part ] ) | 
    ( [ sign - ] '.' - digit { - digit } [ - exponent-part ] ) .

decimal-part : digit { - digit } .

fractional-part : '.' { - digit } .

exponent.part : 'E' [ - sign ] - digit { - digit } .

array-subscription : array-name '(' numeric-expression ')' .

string-expression : string-variable | string-constant .

string-constant : quoted-string .

quoted-string : '"' { string-character } '"' .

string-character : ! '"' & ! end-of-line .

unquoted-string-character : ' ' | plain-string-character .

plain-string-character : '+' | '-' | '.' | digit | letter .

unquoted-string : 
    plain-string-character [ { unquoted-string-character } plain-string-character ] .
</pre>

## License

### BasicBasic - (C) 2019 Premysl Fara 
 
BasicBasic is available under the **zlib license**:

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