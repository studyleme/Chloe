﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Chloe.DbExpressions;
using Chloe.Utility;

namespace Chloe.Core.Visitors
{
    public class ExpressionVisitorBase : ExpressionVisitor<DbExpression>
    {
        static ExpressionVisitorBase()
        {

        }

        protected override DbExpression VisitLambda(LambdaExpression lambda)
        {
            return this.Visit(lambda.Body);
        }

        // +
        protected override DbExpression VisitBinary_Add(BinaryExpression exp)
        {
            return DbExpression.Add(exp.Type, this.Visit(exp.Left), this.Visit(exp.Right), exp.Method);
        }
        // -
        protected override DbExpression VisitBinary_Subtract(BinaryExpression exp)
        {
            return DbExpression.Subtract(this.Visit(exp.Left), this.Visit(exp.Right), exp.Type);
        }
        // *
        protected override DbExpression VisitBinary_Multiply(BinaryExpression exp)
        {
            return DbExpression.Multiply(this.Visit(exp.Left), this.Visit(exp.Right), exp.Type);
        }
        // /
        protected override DbExpression VisitBinary_Divide(BinaryExpression exp)
        {
            return DbExpression.Divide(exp.Type, this.Visit(exp.Left), this.Visit(exp.Right));
        }
        // <
        protected override DbExpression VisitBinary_LessThan(BinaryExpression exp)
        {
            return DbExpression.LessThan(this.Visit(exp.Left), this.Visit(exp.Right));
        }
        // <=
        protected override DbExpression VisitBinary_LessThanOrEqual(BinaryExpression exp)
        {
            return DbExpression.LessThanOrEqual(this.Visit(exp.Left), this.Visit(exp.Right));
        }
        // >
        protected override DbExpression VisitBinary_GreaterThan(BinaryExpression exp)
        {
            return DbExpression.GreaterThan(this.Visit(exp.Left), this.Visit(exp.Right));
        }
        // >=
        protected override DbExpression VisitBinary_GreaterThanOrEqual(BinaryExpression exp)
        {
            return DbExpression.GreaterThanOrEqual(this.Visit(exp.Left), this.Visit(exp.Right));
        }

        protected override DbExpression VisitBinary_And(BinaryExpression exp)
        {
            // true && a.ID == 1 或者 a.ID == 1 && true
            Expression left = exp.Left, right = exp.Right;
            ConstantExpression c = right as ConstantExpression;
            DbExpression dbExp = null;
            //a.ID == 1 && true
            if (c != null)
            {
                if ((bool)c.Value == true)
                {
                    // (a.ID==1)==true
                    //dbExp = new DbEqualExpression(this.Visit(exp.Left), new DbConstantExpression(true));
                    dbExp = DbExpression.Equal(this.Visit(exp.Left), UtilConstants.DbConstant_True);
                    return dbExp;
                }
                else
                {
                    //dbExp = new DbEqualExpression(new DbConstantExpression(1), new DbConstantExpression(0));
                    dbExp = UtilConstants.DbEqual_TrueEqualFalse;
                    return dbExp;
                }
            }

            c = left as ConstantExpression;
            // true && a.ID == 1  
            if (c != null)
            {
                if ((bool)c.Value == true)
                {
                    // (a.ID==1)==true
                    dbExp = DbExpression.Equal(this.Visit(exp.Right), UtilConstants.DbConstant_True);
                    return dbExp;
                }
                else
                {
                    // 直接 (1=0)
                    dbExp = UtilConstants.DbEqual_TrueEqualFalse;
                    return dbExp;
                }
            }

            /* 考虑 a.B && XX 的情况，统一将 a.B && XX 和 a.X>1 && XX ==> a.B==true && XX 和 (a.X>1)==true && XX */

            // left==true
            var newLeft = Expression.Equal(left, UtilConstants.Constant_True);
            // right==true
            var newRight = Expression.Equal(right, UtilConstants.Constant_True);

            //dbExp = new DbAndExpression(this.Visit(newLeft), this.Visit(newRight));
            dbExp = DbExpression.And(this.Visit(newLeft), this.Visit(newRight));

            return dbExp;
        }

        protected override DbExpression VisitBinary_Or(BinaryExpression exp)
        {
            // true && a.ID == 1 或者 a.ID == 1 && true
            Expression left = exp.Left, right = exp.Right;
            ConstantExpression c = right as ConstantExpression;
            DbExpression dbExp = null;
            //a.ID == 1 || true
            if (c != null)
            {
                if ((bool)c.Value == false)
                {
                    // (a.ID==1)==true
                    dbExp = DbExpression.Equal(this.Visit(exp.Left), UtilConstants.DbConstant_True);
                    return dbExp;
                }
                else
                {
                    dbExp = UtilConstants.DbEqual_TrueEqualFalse;
                    return dbExp;
                }
            }

            c = left as ConstantExpression;
            // true && a.ID == 1  
            if (c != null)
            {
                if ((bool)c.Value == false)
                {
                    // (a.ID==1)==true
                    dbExp = DbExpression.Equal(this.Visit(exp.Right), UtilConstants.DbConstant_True);
                    return dbExp;
                }
                else
                {
                    // 直接 (1=0)
                    dbExp = UtilConstants.DbEqual_TrueEqualFalse;
                    return dbExp;
                }
            }

            /* 考虑 a.B || XX 的情况，统一将 a.B || XX 和 a.X>1 || XX 转成 a.B==true || XX 和 (a.X>1)==true || XX */

            // left==true
            var newLeft = Expression.Equal(left, UtilConstants.Constant_True);
            // right==true
            var newRight = Expression.Equal(right, UtilConstants.Constant_True);

            dbExp = DbExpression.Or(this.Visit(newLeft), this.Visit(newRight));
            return dbExp;
        }

        protected override DbExpression VisitConstant(ConstantExpression exp)
        {
            return DbExpression.Constant(exp.Value, exp.Type);
        }

        protected override DbExpression VisitUnary_Not(UnaryExpression exp)
        {
            DbNotExpression e = DbExpression.Not(this.Visit(exp.Operand));
            return e;
        }

        protected override DbExpression VisitUnary_Convert(UnaryExpression u)
        {
            return DbExpression.Convert(u.Type, this.Visit(u.Operand));
        }

        protected override DbExpression VisitMemberAccess(MemberExpression exp)
        {
            DbExpression dbExpression = this.Visit(exp.Expression);
            return DbExpression.MemberAccess(exp.Member, dbExpression);
        }

        // a??b
        protected override DbExpression VisitBinary_Coalesce(BinaryExpression exp)
        {
            DbExpression whenExp = null;
            DbExpression thenExp = null;
            DbExpression elseExp = null;

            // case when left is null then rigth else left end
            thenExp = this.Visit(exp.Right);
            whenExp = elseExp = this.Visit(exp.Left);

            List<DbCaseWhenExpression.WhenThenExpressionPair> whenThenExps = new List<DbCaseWhenExpression.WhenThenExpressionPair>(1);
            whenThenExps.Add(new DbCaseWhenExpression.WhenThenExpressionPair(DbExpression.Equal(whenExp, DbExpression.Constant(null, exp.Type)), thenExp));

            DbExpression dbExp = DbExpression.CaseWhen(whenThenExps, elseExp, exp.Type);

            return dbExp;
        }

        // true?a:b;
        //假如 case when 的 then 部分又是返回 bool 类型 并且诸如 a>1 a=2 等 dbbool 的情况的(因为 a>1 等只能数据库识别)，则继续再够建 case when ...也就是必须确保 then 部分不能有 诸如 a>1，a=2 等的表达式
        protected override DbExpression VisitConditional(ConditionalExpression exp)
        {
            // a.B>0 ? <Expression1> : <Expression2>    a.B ? <Expression1> : <Expression2> 
            // 统一转成 (a.B>0)==true ? <Expression1> : <Expression2>
            // 然后翻译成 case when (a.B>0)==true then <Expression1> when (a.B>0)==false then <Expression2> else null end   
            // case when a.B==true then <Expression1> when a.B==false then <Expression2> else 1=0 end

            Expression test = exp.Test, ifTrue = exp.IfTrue, ifFalse = exp.IfFalse;
            Expression whenTrueTest = null, whenFalseTest = null, thenIfTrue = null, thenfFalse = null;

            // test==true
            whenTrueTest = Expression.Equal(test, UtilConstants.Constant_True);
            whenFalseTest = Expression.Equal(test, UtilConstants.Constant_False);

            // ifTrue==true
            thenIfTrue = ifTrue;
            // ifFalse==true
            thenfFalse = ifFalse;


            List<DbCaseWhenExpression.WhenThenExpressionPair> whenThenExps = new List<DbCaseWhenExpression.WhenThenExpressionPair>(2);

            whenThenExps.Add(new DbCaseWhenExpression.WhenThenExpressionPair(this.Visit(whenTrueTest), this.Visit(thenIfTrue)));
            whenThenExps.Add(new DbCaseWhenExpression.WhenThenExpressionPair(this.Visit(whenFalseTest), this.Visit(thenfFalse)));

            DbExpression elseExp = DbExpression.Constant(null, exp.Type);
            DbExpression dbExp = DbExpression.CaseWhen(whenThenExps, elseExp, exp.Type);

            //如果是 返回 bool 类型，则变成 (true?a:b)==true ，为了方便 select 字段返回 bool 类型的情况，交给 DbExpressionVisitor 去做
            return dbExp;
        }

        protected override DbExpression VisitBinary_Equal(BinaryExpression exp)
        {
            var left = exp.Left;
            var right = exp.Right;

            // 对于应用于 bool 类型的相等情况
            if (left.Type == UtilConstants.TypeOfBoolean)
            {
                return this.VisitBinary_Equal_Boolean(exp);
            }

            // 对于应用于 Nullable<bool> 类型的相等情况
            if (left.Type == UtilConstants.TypeOfBoolean_Nullable)
            {
                return this.VisitBinary_Equal_NullableBoolean(exp);
            }

            DbEqualExpression dbExp = DbExpression.Equal(this.Visit(left), this.Visit(right));
            return dbExp;
        }

        protected override DbExpression VisitBinary_NotEqual(BinaryExpression exp)
        {
            DbNotExpression e = DbExpression.Not(this.VisitBinary_Equal(exp));
            return e;
        }

        protected override DbExpression VisitMethodCall(MethodCallExpression exp)
        {
            DbExpression obj = null;
            List<DbExpression> argList = new List<DbExpression>(exp.Arguments.Count);
            DbExpression dbExp = null;
            if (exp.Object != null)
                obj = this.Visit(exp.Object);
            foreach (var item in exp.Arguments)
            {
                argList.Add(this.Visit(item));
            }

            dbExp = DbExpression.MethodCall(obj, exp.Method, argList);

            return dbExp;
        }


        // 处理 a.XX==XXX 其中 a.XX.Type 为 bool
        DbExpression VisitBinary_Equal_Boolean(BinaryExpression exp)
        {
            var left = exp.Left;
            var right = exp.Right;
            ConstantExpression c = right as ConstantExpression;
            if (c != null) //只处理 XXX==true 或 XXX==false XXX.Type 为 Bool
            {
                return VisitBinary_Specific(left, (bool)c.Value);
            }

            else if ((c = left as ConstantExpression) != null)  //只处理 true==XXX 或 false==XXX   其中XXX.Type 为 Bool
            {
                return VisitBinary_Specific(left, (bool)c.Value);
            }

            else if (((StripConvert(left)).NodeType == ExpressionType.MemberAccess && (StripConvert(right)).NodeType == ExpressionType.MemberAccess))
            {
                //left right 都为 MemberExpression
                return DbExpression.Equal(this.Visit(left), this.Visit(right));
            }

            else
            {
                // 将 left == right 转 (left==true && right==true) || (left==false && right==false) 
                return BuildExpForWhenBooleanEqual(left, right, false);
            }

        }

        //处理 a.XX==XXX 其中 a.XX.Type 为 Nullable<bool>
        DbExpression VisitBinary_Equal_NullableBoolean(BinaryExpression exp)
        {
            var left = exp.Left;
            var right = exp.Right;

            if (((StripConvert(left)).NodeType == ExpressionType.MemberAccess && (StripConvert(right)).NodeType == ExpressionType.MemberAccess))
            {
                //left right 都为 MemberExpression
                return DbExpression.Equal(this.Visit(left), this.Visit(right));
            }

            ConstantExpression cLeft = left as ConstantExpression;
            ConstantExpression cRight = right as ConstantExpression;
            ConstantExpression c = null;
            // left right 其中一边为常量，并且为null
            if ((cLeft != null && cLeft.Value == null) || (cRight != null && cRight.Value == null))
            {
                // 限定只能是 a.XX == null 其中 a.XX 是 Nullable<bool> 类型，如 (bool?)(a.X>0) == null 的情况则抛出异常
                if ((StripConvert(left)).NodeType != ExpressionType.MemberAccess && (StripConvert(right)).NodeType != ExpressionType.MemberAccess)
                {
                    throw new Exception("无效的表达式：" + exp.ToString());
                }

                return DbExpression.Equal(this.Visit(left), this.Visit(right));
            }


            if (right.NodeType == ExpressionType.Convert)//只处理隐士转换情况 XXX==Convert(true) 或 XXX==Convert(false) XXX.Type 为 Nullable
            {
                c = ((UnaryExpression)right).Operand as ConstantExpression;
                if (c != null)
                {
                    return VisitBinary_Specific(left, (bool)c.Value);
                }
            }

            if (left.NodeType == ExpressionType.Convert)//只处理隐士转换情况 XXX==Convert(true) 或 XXX==Convert(false) XXX.Type 为 Nullable
            {
                c = ((UnaryExpression)left).Operand as ConstantExpression;
                if (c != null)
                {
                    return VisitBinary_Specific(right, (bool)c.Value);
                }
            }

            // a.BoolField == (a.ID > 0) 类写法
            // 将 left == right 转 (left==true && right==true) || (left==false && right==false) 
            // 如有需要将转成 (left==true && right==true) || (left==false && right==false) || (left is null && right is null)
            return BuildExpForWhenBooleanEqual(left, right, true);
        }

        DbExpression VisitBinary_Specific(Expression exp, bool trueOrFalse)
        {
            // a.B == true      a.B=1
            // !a.B==true        not (a.b=1)
            // !(a.x>0)==true    not (case when a.x>0 then 1 else 0 end = 1)
            // Convert(true)==true  

            if (exp.NodeType == ExpressionType.Not)
            {
                // 将 !a 转成 !(a==true)，!!a 则成为 !(!a==true)
                var operand = ((UnaryExpression)exp).Operand;
                Expression c = null;
                if (trueOrFalse)
                {
                    if (operand.Type == UtilConstants.TypeOfBoolean)
                    {
                        c = UtilConstants.Constant_True;
                    }
                    else
                        c = UtilConstants.Constant_True;
                }
                else
                {
                    if (operand.Type == UtilConstants.TypeOfBoolean)
                    {
                        c = UtilConstants.Constant_False;
                    }
                    else
                        c = UtilConstants.Convert_FalseToNullable;
                }

                // a==true
                var newOperand = Expression.Equal(operand, c);
                // !(a==true)
                return DbExpression.Not(this.Visit(newOperand));
            }

            else if (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
            {
                return VisitBinary_Specific(this.StripConvert(exp), trueOrFalse);
            }

            else
            {
                // 如果是 MemberAccess 则转成 case <MemberExpression> when 1 then 1 else 0 end = 1 ==>  等价于 a.B=1 ?
                // 可能出现 true==true 情况 如 a.B>1 ? true : false 三元表达式会转成 a.B>1 ? true==true : false==true 又如 Convert(true)==true
                if (exp.NodeType == ExpressionType.MemberAccess || exp.NodeType == ExpressionType.Constant)
                {
                    var nExp = this.Visit(exp);
                    return DbExpression.Equal(nExp, DbExpression.Constant(trueOrFalse));
                }


                // 如果是类似 a.XX>XXX 的表达式则转成 case when <Expression> then 1 else 0 end = 1  ==>  直接 Visit a.XX>XXX
                // a.XX>XXX == true  a.XX>XXX == false
                if (!trueOrFalse)
                {
                    // !(a.XX>XXX)
                    return DbExpression.Not(this.Visit(exp));
                }
                return this.Visit(exp);
            }

        }

        // 将 left == right 转 (left==true && right==true) || (left==false && right==false)  
        // 如有必要将转成 (left==true && right==true) || (left==false && right==false) || (left is null && right is null)
        // left 和 right 的 Type 必须为 bool 或者 Nullable<bool>
        // isNullable 
        DbExpression BuildExpForWhenBooleanEqual(Expression left, Expression right, bool isNullable)
        {
            Expression resultExp = null;

            Expression rightTrueExp = null, rightFalseExp = null;
            if (isNullable)
            {
                rightTrueExp = UtilConstants.Constant_True;
                rightFalseExp = UtilConstants.Convert_FalseToNullable;
            }
            else
            {
                rightTrueExp = UtilConstants.Constant_True;
                rightFalseExp = UtilConstants.Constant_False;
            }


            // left==true
            var nLeft_True = Expression.Equal(left, rightTrueExp);
            // right==true
            var nRigth_True = Expression.Equal(right, rightTrueExp);
            // left==true && right==true
            var nLeft_Or = Expression.AndAlso(nLeft_True, nRigth_True);

            // left==false
            var nLeft_False = Expression.Equal(left, rightFalseExp);
            // right==false
            var nRigth_False = Expression.Equal(right, rightFalseExp);
            // left==false && right==false
            var nRigth_Or = Expression.AndAlso(nLeft_False, nRigth_False);

            // left==true && right==true) || (left==false && right==false
            resultExp = Expression.Or(nLeft_Or, nRigth_Or);

            /* 构建 left==Convert(true); left.Type 为Nullable  并且只有 left right 两边都为 MemberAccess 的时候才会构造 =null*/
            if (isNullable && (StripConvert(left)).NodeType == ExpressionType.MemberAccess && (StripConvert(right)).NodeType == ExpressionType.MemberAccess)
            {
                /* 构建 left==null && right==null */
                var l = Expression.Equal(left, Expression.Constant(null));
                var r = Expression.Equal(right, Expression.Constant(null));

                // left==null && right==null
                var nullBinary = Expression.AndAlso(l, r);

                //(left==true && right==true) || (left==false && right==false) || (left is null && right is null)
                resultExp = Expression.Or(resultExp, nullBinary);
            }

            return this.Visit(resultExp);
        }

        Expression StripConvert(Expression exp)
        {
            Expression operand = exp;
            while (operand.NodeType == ExpressionType.Convert || operand.NodeType == ExpressionType.ConvertChecked)
            {
                operand = ((UnaryExpression)operand).Operand;
            }
            return operand;
        }
    }
}