﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings.ExtractCondition
{
    internal abstract class ExtractConditionRefactoring<TStatement> where TStatement : StatementSyntax
    {
        public abstract SyntaxKind StatementKind { get; }

        public abstract string Title { get; }

        public abstract StatementSyntax GetStatement(TStatement statement);

        public abstract TStatement SetStatement(TStatement statement, StatementSyntax newStatement);

        private StatementContainer GetStatementContainer(BinaryExpressionSyntax binaryExpression)
        {
            SyntaxNode node = binaryExpression.Parent.Parent;

            if (node != null)
            {
                StatementContainer container;

                if (StatementContainer.TryCreate(node, out container))
                    return container;
            }

            return null;
        }

        protected TStatement RemoveExpressionFromCondition(
            TStatement statement,
            BinaryExpressionSyntax condition,
            ExpressionSyntax expression)
        {
            return statement.ReplaceNode(
                expression.Parent,
                GetNewCondition(condition, expression));
        }

        private static ExpressionSyntax GetNewCondition(
            ExpressionSyntax condition,
            ExpressionSyntax expression)
        {
            var binaryExpression = (BinaryExpressionSyntax)expression.Parent;
            ExpressionSyntax left = binaryExpression.Left;

            if (expression == left)
            {
                return binaryExpression.Right;
            }
            else
            {
                return (binaryExpression == condition)
                    ? left.TrimTrailingTrivia()
                    : left;
            }
        }

        protected TStatement RemoveExpressionsFromCondition(
            TStatement statement,
            BinaryExpressionSyntax condition,
            SelectedExpressions selectedExpressions)
        {
            var binaryExpression = (BinaryExpressionSyntax)selectedExpressions.Expressions.First().Parent;

            return statement.ReplaceNode(
                condition,
                binaryExpression.Left.TrimTrailingTrivia());
        }

        protected TStatement AddNestedIf(
            TStatement statement,
            SelectedExpressions selectedExpressions)
        {
            ExpressionSyntax expression = ParseExpression(selectedExpressions.ExpressionsText);

            return AddNestedIf(statement, expression);
        }

        protected TStatement AddNestedIf(
            TStatement statement,
            ExpressionSyntax expression)
        {
            StatementSyntax childStatement = GetStatement(statement);

            if (childStatement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)childStatement;

                IfStatementSyntax nestedIf = IfStatement(
                    expression.WithoutTrivia(),
                    Block(block.Statements));

                return statement.ReplaceNode(
                    block,
                    block.WithStatements(SingletonList<StatementSyntax>(nestedIf)));
            }
            else
            {
                IfStatementSyntax nestedIf = IfStatement(
                    expression.WithoutTrivia(),
                    childStatement.WithoutTrivia());

                BlockSyntax block = Block(nestedIf).WithTriviaFrom(childStatement);

                return SetStatement(statement, block);
            }
        }
    }
}
