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
namespace BasicBasic.Indirect.Tokens
{
    public abstract class AToken : IToken
    {
        public int TokenCode { get; protected set; }

        public float NumValue { get; protected set; }

        public string StrValue { get; protected set; }


        protected AToken()
        {
        }


        public override string ToString()
        {
            switch (TokenCode)
            {
                case Tokenizer.TOK_SVARIDNT:
                case Tokenizer.TOK_VARIDNT:
                case Tokenizer.TOK_STRIDNT:
                case Tokenizer.TOK_FN:
                case Tokenizer.TOK_UFN:
                    return StrValue;

                case Tokenizer.TOK_PLSTSEP: return ";";
                case Tokenizer.TOK_LSTSEP: return ",";
                case Tokenizer.TOK_EQL: return "="; 
                case Tokenizer.TOK_NEQL: return "<>";
                case Tokenizer.TOK_LT: return "<"; 
                case Tokenizer.TOK_LTE: return "<=";
                case Tokenizer.TOK_GT: return ">"; 
                case Tokenizer.TOK_GTE: return ">=";
                case Tokenizer.TOK_PLUS: return "+";
                case Tokenizer.TOK_MINUS: return "-";
                case Tokenizer.TOK_MULT: return "*";
                case Tokenizer.TOK_DIV: return "/";
                case Tokenizer.TOK_POW: return "^";
                case Tokenizer.TOK_LBRA: return "(";
                case Tokenizer.TOK_RBRA: return ")";

                case Tokenizer.TOK_KEY_BASE: return "BASE";
                //case Tokenizer.TOK_KEY_DATA: return "DATA";
                case Tokenizer.TOK_KEY_DEF: return "DEF";
                case Tokenizer.TOK_KEY_DIM: return "DIM";
                case Tokenizer.TOK_KEY_END: return "END";
                //case Tokenizer.TOK_KEY_FOR: return "FOR";
                case Tokenizer.TOK_KEY_GO: return "GO";
                case Tokenizer.TOK_KEY_GOSUB: return "GOSUB";
                case Tokenizer.TOK_KEY_GOTO: return "GOTO";
                case Tokenizer.TOK_KEY_IF: return "IF";
                case Tokenizer.TOK_KEY_INPUT: return "INPUT";
                case Tokenizer.TOK_KEY_LET: return "LET";
                //case Tokenizer.TOK_KEY_NEXT: return "NEXT";
                //case Tokenizer.TOK_KEY_ON: return "ON";
                case Tokenizer.TOK_KEY_OPTION: return "OPTION";
                case Tokenizer.TOK_KEY_PRINT: return "PRINT";
                case Tokenizer.TOK_KEY_RANDOMIZE: return "RANDOMIZE";
                //case Tokenizer.TOK_KEY_READ: return "READ";
                case Tokenizer.TOK_KEY_REM: return "REM";
                //case Tokenizer.TOK_KEY_RESTORE: return "RESTORE";
                case Tokenizer.TOK_KEY_RETURN: return "RETURN";
                //case Tokenizer.TOK_KEY_STEP: return "STEP";
                case Tokenizer.TOK_KEY_STOP: return "STOP";
                case Tokenizer.TOK_KEY_SUB: return "SUB";
                case Tokenizer.TOK_KEY_THEN: return "THEN";
                case Tokenizer.TOK_KEY_TO: return "TO";

                case Tokenizer.TOK_EOF: return "@EOF";
                case Tokenizer.TOK_EOLN: return "@EOLN";
            }

            throw new InterpreterException("Unknown token " + TokenCode + ".");
        }
    }
}
