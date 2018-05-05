﻿using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class SafetyTest : IPDecl
    {
        public SafetyTest(ParserRuleContext sourceNode, string testName)
        {
            SourceLocation = sourceNode;
            Name = testName;
        }

        public IPModuleExpr ModExpr { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}