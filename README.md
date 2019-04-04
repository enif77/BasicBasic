# BasicBasic Interpreter

BasicBasic is a basic implementation of the ECMA-55 Minimal BASIC System. It is a direct interpreter 
without any compilation phase. Can execute a script in a file or work directly as a "super smart
calculator thing". :-)

Right now it is a bit incomplete. Bigest missing parts are:

  - INPUT command.
  - DATA, READ and RESTORE commands.
  - PRINT statement output formatting.
  - FOR loop.
  - Two dimensional arrays.
  - Some more ECMA-55 standard defined constraints.

Some of those features will be implmented, some wont. 

More interesting informations and implementations of the Minimal Basic can be found here:

  - [ECMA-55 Minimal BASIC System - bas55](https://jorgicor.niobe.org/bas55/)
  - [John's ECMA-55 Minimal BASIC Compiler](https://buraphakit.sourceforge.io/BASIC.shtml)


## Usage

If the first argument of the program is a path to an existing source file, it is loaded and interpreted.
If the first argument starts with the exclamation character ('!'), it is ignored.

Without any program argument, the interactive mode starts.

## Interactive Mode

The interactive mode allows an user to immediatelly execute inserted progam lines or to define
program lines for later execution.

The command propmt for a new program line is "> ".

All lines are passed to the interpreter for execution.

If a line starts with a label (integer 1 .. 99) it defines a new program line.
A program line can be redefined by a program line starting with the same label.
If a program line contains only a label, it deletes an existing program line with the same label.

### Interactive Mode Commands

#### BY or QUIT

Ends this app.

#### NEW

Clears all defined program lines.

#### LIST

Lists all currently defined program lines.

#### RUN

Executes the entered program.

## Syntax

<pre>
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


program : { block } end-line .

block : { line | for-block } .

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