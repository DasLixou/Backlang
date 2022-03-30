﻿namespace Backlang.Codeanalysis.Parsing.AST.Statements.Assembler;

public enum OpCode
{
    Mov,
    Hlt,
    Add,
    Addc,
    Subtract,
    Multiply,
    Divmod,
    And,
    Or,
    Xor,
    Not,
    Lsh,
    Rsh,
    Cmp,
    Push,
    Pop,
    Call,
    Ret,
    Jmp,
    Jmpe,
    Nop,
    Gks, //Get Key State
    Poll, //Poll Time
}