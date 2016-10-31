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
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

#pragma warning disable CS1591

namespace jarekczek
{
    public class Rec
    {
        public int x;
        public int y;

        /*
        public static override bool operator ==(Rec r1, Rec r2)
        {
            return r1.x == r2.x && r1.y == r2.y;
        }

        public static override bool operator !=(Rec r1, Rec r2)
        {
            return !(r1 == r2);
        }
        */

        public override bool Equals(object o2)
        {
            var r2 = o2 as Rec;
            if (r2 != null)
                return x == r2.x && y == r2.y;
            else
                return base.Equals(o2);
        }
    }


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

        [Test]
        public static void UseCombiningInLinqWhere()
        {
            var aRec = new Rec[]
            {
                new Rec { x = 0, y = 1 },
                new Rec { x = 1, y = 2 },
                new Rec { x = 2, y = 3 }
            };
            var exprFun = LinqExprHelper.NewExpr( (int z) => z * z + 3);
            // Now in dynamic way make the function operate on r.x and r.y,
            // without rewriting its body.
            // NewExpr helper allows to skip Expression<Func<?>> part.
            var exprFunX = exprFun.ReplacePar("z",
                LinqExprHelper.NewExpr((Rec r) => r.x).Body);
            // An alternative way to obtain the same expression:
            var exprFunY = exprFun.ReplacePar("z",
                ((Expression<Func<Rec, int>>)(r => r.y)).Body);
            var exprWhere = LinqExprHelper.NewExpr(
                (Rec r, int funX, int funY) => funX == 4 || funY == 4 );
            var exprWhereFinal = exprWhere
                .ReplacePar("funX", exprFunX.Body)
                .ReplacePar("funY", exprFunY.Body);
            // To use the query in a select, it must be strongly typed.
            var exprWhereTyped = (Func<Rec, bool>)exprWhereFinal.Compile();
            var aRes = aRec.Where(exprWhereTyped).ToList();
            Assert.AreEqual(2, aRes.Count);
            Assert.AreEqual(aRes[0], new Rec { x = 0, y = 1 });
            Assert.AreEqual(aRes[1], new Rec { x = 1, y = 2 });
        }

        [Test]
        public static void UseCombiningInLinqProjection()
        {
            var aRec = new Rec[]
            {
                new Rec { x = 0, y = 1 },
                new Rec { x = 1, y = 2 },
                new Rec { x = 2, y = 3 }
            };
            var exprFun = LinqExprHelper.NewExpr((int z) => z * z - z);
            // Now in dynamic way make the function operate on r.x and r.y,
            // without rewriting its body.
            var exprFunX = exprFun.ReplacePar("z",
                LinqExprHelper.NewExpr((Rec r) => r.x).Body);
            // An alternative way to obtain the same expression:
            var exprFunY = exprFun.ReplacePar("z",
                ((Expression<Func<Rec, int>>) (r => r.y)).Body);
            // Prepare a projection query, using parameters fun?, which
            // will be later in next statement.
            var exprQuery = LinqExprHelper.NewExpr(
                (Rec r, int funX, int funY) => new Rec { x = funX, y = funY });
            var exprQueryFinal = exprQuery
                .ReplacePar("funX", exprFunX.Body)
                .ReplacePar("funY", exprFunY.Body);
            // To use the query in a select, it must be strongly typed.
            var exprQueryTyped = (Func<Rec, Rec>)exprQueryFinal.Compile();
            var aRes = aRec.Select(exprQueryTyped).ToList();
            Assert.AreEqual(3, aRes.Count);
            Assert.AreEqual(aRes[0], new Rec { x = 0, y = 0 }, "0");
            Assert.AreEqual(aRes[1], new Rec { x = 0, y = 2 }, "1");
            Assert.AreEqual(aRes[2], new Rec { x = 2, y = 6 }, "2");
        }

    }
}
