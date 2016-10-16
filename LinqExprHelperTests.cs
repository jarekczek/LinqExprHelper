/*
The MIT License(MIT)
Copyright(c) 2016 Jarek Czekalski

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// I used NuGet Package Manager to get: NUnit, NUnit3TestAdapter.

using System;
using System.Linq.Expressions;
using NUnit.Framework;

#pragma warning disable CS1591

namespace jarekczek
{
    public class LinqExprHelperTests
    {
        [Test]
        public static void SameNameParameterDoesNotWork()
        {
            Expression<Func<int, int>> sumExpr = x => x + 1;
            // Let's create a combined expression: x => (x+1) * 2
            var combExpr = Expression.Lambda(
                Expression.Multiply(sumExpr.Body, Expression.Constant(2)),
                Expression.Parameter(typeof(int), "x")
                );
            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    int nRes = (int)combExpr.Compile().DynamicInvoke(3);
                    // We expect nRes == 8, but
                    // the command above will not execute, it throws:
                    // System.InvalidOperationException : variable 'x' of type
                    // 'System.Int32' referenced from scope '', but it is not defined
                    // It's because the two x variables are not identical, they
                    // come from different scopes.
                }
            );
        }

        [Test]
        public static void SameNameParameterUnified()
        {
            Expression<Func<int, int>> sumExpr = x => x + 1;
            Expression<Func<int, int>> mulExpr = x => 2 * x;
            // Let's create a combined expression: x => (x+1) + (2*x)
            var combExpr = Expression.Lambda(
                Expression.Add(sumExpr.Body, mulExpr.Body),
                Expression.Parameter(typeof(int), "x")
                );
            var combExprUni = combExpr.UnifyParametersByName();
            // To succeed, we need to unify 3 different x variables,
            // coming from different scopes, into a single one.
            int nRes = (int)combExprUni.Compile().DynamicInvoke(3);
            Assert.AreEqual(10, nRes);
        }

        [Test]
        public static void CombineExprByName()
        {
            var sumExpr = LinqExprHelper.NewExpr((int x) => x + 1);
            var mulExpr = LinqExprHelper.NewExpr((int x) => 2 * x);
            var combExpr = LinqExprHelper.NewExpr((int x, int y, int z) => y + z);
            int result = (int)combExpr
                .ReplacePar("y", sumExpr.Body)
                .ReplacePar("z", mulExpr.Body)
                .Compile().DynamicInvoke(5);
            Assert.AreEqual(16, result);
        }

        [Test]
        public static void CombineExprByNameReverseParamsOrder()
        {
            var sumExpr = LinqExprHelper.NewExpr((int x) => x + 1);
            var mulExpr = LinqExprHelper.NewExpr((int x) => 2 * x);
            var combExpr = LinqExprHelper.NewExpr((int y, int z, int x) => y + z);
            int result = (int)combExpr
                .ReplacePar("y", sumExpr.Body)
                .ReplacePar("z", mulExpr.Body)
                .Compile().DynamicInvoke(5);
            Assert.AreEqual(16, result);
        }
    }
}
