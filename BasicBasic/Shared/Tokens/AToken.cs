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
namespace BasicBasic.Shared.Tokens
{
    public abstract class AToken : IToken
    {
        public TokenCode TokenCode { get; protected set; }

        public float NumValue { get; protected set; }

        public string StrValue { get; protected set; }


        protected AToken()
        {
        }


        public override string ToString()
        {
            switch (TokenCode)
            {
                case TokenCode.TOK_PLSTSEP: return ";";
                case TokenCode.TOK_LSTSEP: return ",";
                case TokenCode.TOK_EQL: return "="; 
                case TokenCode.TOK_NEQL: return "<>";
                case TokenCode.TOK_LT: return "<"; 
                case TokenCode.TOK_LTE: return "<=";
                case TokenCode.TOK_GT: return ">"; 
                case TokenCode.TOK_GTE: return ">=";
                case TokenCode.TOK_PLUS: return "+";
                case TokenCode.TOK_MINUS: return "-";
                case TokenCode.TOK_MULT: return "*";
                case TokenCode.TOK_DIV: return "/";
                case TokenCode.TOK_POW: return "^";
                case TokenCode.TOK_LBRA: return "(";
                case TokenCode.TOK_RBRA: return ")";

                case TokenCode.TOK_KEY_BASE: return "BASE";
                case TokenCode.TOK_KEY_DATA: return "DATA";
                case TokenCode.TOK_KEY_DEF: return "DEF";
                case TokenCode.TOK_KEY_DIM: return "DIM";
                case TokenCode.TOK_KEY_END: return "END";
                //case TokenCode.TOK_KEY_FOR: return "FOR";
                case TokenCode.TOK_KEY_GO: return "GO";
                case TokenCode.TOK_KEY_GOSUB: return "GOSUB";
                case TokenCode.TOK_KEY_GOTO: return "GOTO";
                case TokenCode.TOK_KEY_IF: return "IF";
                case TokenCode.TOK_KEY_INPUT: return "INPUT";
                case TokenCode.TOK_KEY_LET: return "LET";
                //case TokenCode.TOK_KEY_NEXT: return "NEXT";
                case TokenCode.TOK_KEY_ON: return "ON";
                case TokenCode.TOK_KEY_OPTION: return "OPTION";
                case TokenCode.TOK_KEY_PRINT: return "PRINT";
                case TokenCode.TOK_KEY_RANDOMIZE: return "RANDOMIZE";
                case TokenCode.TOK_KEY_READ: return "READ";
                case TokenCode.TOK_KEY_REM: return "REM";
                case TokenCode.TOK_KEY_RESTORE: return "RESTORE";
                case TokenCode.TOK_KEY_RETURN: return "RETURN";
                //case TokenCode.TOK_KEY_STEP: return "STEP";
                case TokenCode.TOK_KEY_STOP: return "STOP";
                case TokenCode.TOK_KEY_SUB: return "SUB";
                case TokenCode.TOK_KEY_THEN: return "THEN";
                case TokenCode.TOK_KEY_TO: return "TO";

                case TokenCode.TOK_EOF: return "@EOF";
                case TokenCode.TOK_EOLN: return "@EOLN";
            }

            throw new InterpreterException("Unknown token " + TokenCode + ".");
        }
    }
}
