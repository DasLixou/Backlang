﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1.AST.Expressions;

[TestClass]
public class DefaultExprTests : ParserTestBase
{
    [TestMethod]
    public void Default_With_Type_Should_Pass()
    {
        var src = "default(i32);";
        var tree = ParseAndGetNodesInFunction(src);
    }

    [TestMethod]
    public void Default_Without_Type_Should_Pass()
    {
        var src = "default;";
        var tree = ParseAndGetNodesInFunction(src);
    }
}