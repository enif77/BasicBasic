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