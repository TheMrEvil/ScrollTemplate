﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Compiler
{
	// Token: 0x020002BC RID: 700
	internal sealed class LambdaCompiler : ILocalCache
	{
		// Token: 0x060014D0 RID: 5328 RVA: 0x00041381 File Offset: 0x0003F581
		private void EmitAddress(Expression node, Type type)
		{
			this.EmitAddress(node, type, LambdaCompiler.CompilationFlags.EmitExpressionStart);
		}

		// Token: 0x060014D1 RID: 5329 RVA: 0x0004138C File Offset: 0x0003F58C
		private void EmitAddress(Expression node, Type type, LambdaCompiler.CompilationFlags flags)
		{
			bool flag = (flags & LambdaCompiler.CompilationFlags.EmitExpressionStartMask) == LambdaCompiler.CompilationFlags.EmitExpressionStart;
			LambdaCompiler.CompilationFlags flags2 = flag ? this.EmitExpressionStart(node) : LambdaCompiler.CompilationFlags.EmitNoExpressionStart;
			ExpressionType nodeType = node.NodeType;
			if (nodeType <= ExpressionType.MemberAccess)
			{
				if (nodeType == ExpressionType.ArrayIndex)
				{
					this.AddressOf((BinaryExpression)node, type);
					goto IL_A2;
				}
				if (nodeType == ExpressionType.Call)
				{
					this.AddressOf((MethodCallExpression)node, type);
					goto IL_A2;
				}
				if (nodeType == ExpressionType.MemberAccess)
				{
					this.AddressOf((MemberExpression)node, type);
					goto IL_A2;
				}
			}
			else
			{
				if (nodeType == ExpressionType.Parameter)
				{
					this.AddressOf((ParameterExpression)node, type);
					goto IL_A2;
				}
				if (nodeType == ExpressionType.Index)
				{
					this.AddressOf((IndexExpression)node, type);
					goto IL_A2;
				}
				if (nodeType == ExpressionType.Unbox)
				{
					this.AddressOf((UnaryExpression)node, type);
					goto IL_A2;
				}
			}
			this.EmitExpressionAddress(node, type);
			IL_A2:
			if (flag)
			{
				this.EmitExpressionEnd(flags2);
			}
		}

		// Token: 0x060014D2 RID: 5330 RVA: 0x00041448 File Offset: 0x0003F648
		private void AddressOf(BinaryExpression node, Type type)
		{
			if (TypeUtils.AreEquivalent(type, node.Type))
			{
				this.EmitExpression(node.Left);
				this.EmitExpression(node.Right);
				this._ilg.Emit(OpCodes.Ldelema, node.Type);
				return;
			}
			this.EmitExpressionAddress(node, type);
		}

		// Token: 0x060014D3 RID: 5331 RVA: 0x0004149C File Offset: 0x0003F69C
		private void AddressOf(ParameterExpression node, Type type)
		{
			if (TypeUtils.AreEquivalent(type, node.Type))
			{
				if (node.IsByRef)
				{
					this._scope.EmitGet(node);
					return;
				}
				this._scope.EmitAddressOf(node);
				return;
			}
			else
			{
				if (node.Type.IsByRef && node.Type.GetElementType() == type)
				{
					this.EmitExpression(node);
					return;
				}
				this.EmitExpressionAddress(node, type);
				return;
			}
		}

		// Token: 0x060014D4 RID: 5332 RVA: 0x0004150C File Offset: 0x0003F70C
		private void AddressOf(MemberExpression node, Type type)
		{
			if (TypeUtils.AreEquivalent(type, node.Type))
			{
				Type objectType = null;
				if (node.Expression != null)
				{
					this.EmitInstance(node.Expression, out objectType);
				}
				this.EmitMemberAddress(node.Member, objectType);
				return;
			}
			this.EmitExpressionAddress(node, type);
		}

		// Token: 0x060014D5 RID: 5333 RVA: 0x00041558 File Offset: 0x0003F758
		private void EmitMemberAddress(MemberInfo member, Type objectType)
		{
			FieldInfo fieldInfo = member as FieldInfo;
			if (fieldInfo != null && !fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
			{
				this._ilg.EmitFieldAddress(fieldInfo);
				return;
			}
			this.EmitMemberGet(member, objectType);
			LocalBuilder local = this.GetLocal(LambdaCompiler.GetMemberType(member));
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
		}

		// Token: 0x060014D6 RID: 5334 RVA: 0x000415C4 File Offset: 0x0003F7C4
		private void AddressOf(MethodCallExpression node, Type type)
		{
			if (!node.Method.IsStatic && node.Object.Type.IsArray && node.Method == node.Object.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public))
			{
				MethodInfo method = node.Object.Type.GetMethod("Address", BindingFlags.Instance | BindingFlags.Public);
				this.EmitMethodCall(node.Object, method, node);
				return;
			}
			this.EmitExpressionAddress(node, type);
		}

		// Token: 0x060014D7 RID: 5335 RVA: 0x00041644 File Offset: 0x0003F844
		private void AddressOf(IndexExpression node, Type type)
		{
			if (!TypeUtils.AreEquivalent(type, node.Type) || node.Indexer != null)
			{
				this.EmitExpressionAddress(node, type);
				return;
			}
			if (node.ArgumentCount == 1)
			{
				this.EmitExpression(node.Object);
				this.EmitExpression(node.GetArgument(0));
				this._ilg.Emit(OpCodes.Ldelema, node.Type);
				return;
			}
			MethodInfo method = node.Object.Type.GetMethod("Address", BindingFlags.Instance | BindingFlags.Public);
			this.EmitMethodCall(node.Object, method, node);
		}

		// Token: 0x060014D8 RID: 5336 RVA: 0x000416D5 File Offset: 0x0003F8D5
		private void AddressOf(UnaryExpression node, Type type)
		{
			this.EmitExpression(node.Operand);
			this._ilg.Emit(OpCodes.Unbox, type);
		}

		// Token: 0x060014D9 RID: 5337 RVA: 0x000416F4 File Offset: 0x0003F8F4
		private void EmitExpressionAddress(Expression node, Type type)
		{
			this.EmitExpression(node, LambdaCompiler.CompilationFlags.EmitNoExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
			LocalBuilder local = this.GetLocal(type);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
		}

		// Token: 0x060014DA RID: 5338 RVA: 0x00041738 File Offset: 0x0003F938
		private LambdaCompiler.WriteBack EmitAddressWriteBack(Expression node, Type type)
		{
			LambdaCompiler.CompilationFlags flags = this.EmitExpressionStart(node);
			LambdaCompiler.WriteBack writeBack = null;
			if (TypeUtils.AreEquivalent(type, node.Type))
			{
				ExpressionType nodeType = node.NodeType;
				if (nodeType != ExpressionType.MemberAccess)
				{
					if (nodeType == ExpressionType.Index)
					{
						writeBack = this.AddressOfWriteBack((IndexExpression)node);
					}
				}
				else
				{
					writeBack = this.AddressOfWriteBack((MemberExpression)node);
				}
			}
			if (writeBack == null)
			{
				this.EmitAddress(node, type, LambdaCompiler.CompilationFlags.EmitNoExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
			}
			this.EmitExpressionEnd(flags);
			return writeBack;
		}

		// Token: 0x060014DB RID: 5339 RVA: 0x000417A4 File Offset: 0x0003F9A4
		private LambdaCompiler.WriteBack AddressOfWriteBack(MemberExpression node)
		{
			PropertyInfo propertyInfo = node.Member as PropertyInfo;
			if (propertyInfo == null || !propertyInfo.CanWrite)
			{
				return null;
			}
			return this.AddressOfWriteBackCore(node);
		}

		// Token: 0x060014DC RID: 5340 RVA: 0x000417D4 File Offset: 0x0003F9D4
		private LambdaCompiler.WriteBack AddressOfWriteBackCore(MemberExpression node)
		{
			LocalBuilder instanceLocal = null;
			Type type = null;
			if (node.Expression != null)
			{
				this.EmitInstance(node.Expression, out type);
				this._ilg.Emit(OpCodes.Dup);
				this._ilg.Emit(OpCodes.Stloc, instanceLocal = this.GetInstanceLocal(type));
			}
			PropertyInfo pi = (PropertyInfo)node.Member;
			this.EmitCall(type, pi.GetGetMethod(true));
			LocalBuilder valueLocal = this.GetLocal(node.Type);
			this._ilg.Emit(OpCodes.Stloc, valueLocal);
			this._ilg.Emit(OpCodes.Ldloca, valueLocal);
			return delegate(LambdaCompiler @this)
			{
				LocalBuilder instanceLocal;
				if (instanceLocal != null)
				{
					@this._ilg.Emit(OpCodes.Ldloc, instanceLocal);
					@this.FreeLocal(instanceLocal);
				}
				@this._ilg.Emit(OpCodes.Ldloc, valueLocal);
				@this.FreeLocal(valueLocal);
				instanceLocal = instanceLocal;
				@this.EmitCall((instanceLocal != null) ? instanceLocal.LocalType : null, pi.GetSetMethod(true));
			};
		}

		// Token: 0x060014DD RID: 5341 RVA: 0x000418A4 File Offset: 0x0003FAA4
		private LambdaCompiler.WriteBack AddressOfWriteBack(IndexExpression node)
		{
			if (node.Indexer == null || !node.Indexer.CanWrite)
			{
				return null;
			}
			return this.AddressOfWriteBackCore(node);
		}

		// Token: 0x060014DE RID: 5342 RVA: 0x000418CC File Offset: 0x0003FACC
		private LambdaCompiler.WriteBack AddressOfWriteBackCore(IndexExpression node)
		{
			LocalBuilder instanceLocal = null;
			Type type = null;
			if (node.Object != null)
			{
				this.EmitInstance(node.Object, out type);
				this._ilg.Emit(OpCodes.Dup);
				this._ilg.Emit(OpCodes.Stloc, instanceLocal = this.GetInstanceLocal(type));
			}
			int argumentCount = node.ArgumentCount;
			LocalBuilder[] args = new LocalBuilder[argumentCount];
			for (int i = 0; i < argumentCount; i++)
			{
				Expression argument = node.GetArgument(i);
				this.EmitExpression(argument);
				LocalBuilder local = this.GetLocal(argument.Type);
				this._ilg.Emit(OpCodes.Dup);
				this._ilg.Emit(OpCodes.Stloc, local);
				args[i] = local;
			}
			this.EmitGetIndexCall(node, type);
			LocalBuilder valueLocal = this.GetLocal(node.Type);
			this._ilg.Emit(OpCodes.Stloc, valueLocal);
			this._ilg.Emit(OpCodes.Ldloca, valueLocal);
			return delegate(LambdaCompiler @this)
			{
				LocalBuilder instanceLocal;
				if (instanceLocal != null)
				{
					@this._ilg.Emit(OpCodes.Ldloc, instanceLocal);
					@this.FreeLocal(instanceLocal);
				}
				LocalBuilder[] args;
				foreach (LocalBuilder local2 in args)
				{
					@this._ilg.Emit(OpCodes.Ldloc, local2);
					@this.FreeLocal(local2);
				}
				@this._ilg.Emit(OpCodes.Ldloc, valueLocal);
				@this.FreeLocal(valueLocal);
				IndexExpression node2 = node;
				instanceLocal = instanceLocal;
				@this.EmitSetIndexCall(node2, (instanceLocal != null) ? instanceLocal.LocalType : null);
			};
		}

		// Token: 0x060014DF RID: 5343 RVA: 0x00041A18 File Offset: 0x0003FC18
		private LocalBuilder GetInstanceLocal(Type type)
		{
			Type type2 = type.IsValueType ? type.MakeByRefType() : type;
			return this.GetLocal(type2);
		}

		// Token: 0x060014E0 RID: 5344 RVA: 0x00041A3E File Offset: 0x0003FC3E
		private void EmitBinaryExpression(Expression expr)
		{
			this.EmitBinaryExpression(expr, LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x060014E1 RID: 5345 RVA: 0x00041A4C File Offset: 0x0003FC4C
		private void EmitBinaryExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			BinaryExpression binaryExpression = (BinaryExpression)expr;
			if (binaryExpression.Method != null)
			{
				this.EmitBinaryMethod(binaryExpression, flags);
				return;
			}
			if ((binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.NotEqual) && (binaryExpression.Type == typeof(bool) || binaryExpression.Type == typeof(bool?)))
			{
				if (ConstantCheck.IsNull(binaryExpression.Left) && !ConstantCheck.IsNull(binaryExpression.Right) && binaryExpression.Right.Type.IsNullableType())
				{
					this.EmitNullEquality(binaryExpression.NodeType, binaryExpression.Right, binaryExpression.IsLiftedToNull);
					return;
				}
				if (ConstantCheck.IsNull(binaryExpression.Right) && !ConstantCheck.IsNull(binaryExpression.Left) && binaryExpression.Left.Type.IsNullableType())
				{
					this.EmitNullEquality(binaryExpression.NodeType, binaryExpression.Left, binaryExpression.IsLiftedToNull);
					return;
				}
				this.EmitExpression(LambdaCompiler.GetEqualityOperand(binaryExpression.Left));
				this.EmitExpression(LambdaCompiler.GetEqualityOperand(binaryExpression.Right));
			}
			else
			{
				this.EmitExpression(binaryExpression.Left);
				this.EmitExpression(binaryExpression.Right);
			}
			this.EmitBinaryOperator(binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type, binaryExpression.Type, binaryExpression.IsLiftedToNull);
		}

		// Token: 0x060014E2 RID: 5346 RVA: 0x00041BB4 File Offset: 0x0003FDB4
		private void EmitNullEquality(ExpressionType op, Expression e, bool isLiftedToNull)
		{
			if (isLiftedToNull)
			{
				this.EmitExpressionAsVoid(e);
				this._ilg.EmitDefault(typeof(bool?), this);
				return;
			}
			this.EmitAddress(e, e.Type);
			this._ilg.EmitHasValue(e.Type);
			if (op == ExpressionType.Equal)
			{
				this._ilg.Emit(OpCodes.Ldc_I4_0);
				this._ilg.Emit(OpCodes.Ceq);
			}
		}

		// Token: 0x060014E3 RID: 5347 RVA: 0x00041C28 File Offset: 0x0003FE28
		private void EmitBinaryMethod(BinaryExpression b, LambdaCompiler.CompilationFlags flags)
		{
			if (b.IsLifted)
			{
				ParameterExpression parameterExpression = Expression.Variable(b.Left.Type.GetNonNullableType(), null);
				ParameterExpression parameterExpression2 = Expression.Variable(b.Right.Type.GetNonNullableType(), null);
				MethodCallExpression methodCallExpression = Expression.Call(null, b.Method, parameterExpression, parameterExpression2);
				Type resultType;
				if (b.IsLiftedToNull)
				{
					resultType = methodCallExpression.Type.GetNullableType();
				}
				else
				{
					resultType = typeof(bool);
				}
				this.EmitLift(b.NodeType, resultType, methodCallExpression, new ParameterExpression[]
				{
					parameterExpression,
					parameterExpression2
				}, new Expression[]
				{
					b.Left,
					b.Right
				});
				return;
			}
			this.EmitMethodCallExpression(Expression.Call(null, b.Method, b.Left, b.Right), flags);
		}

		// Token: 0x060014E4 RID: 5348 RVA: 0x00041CF2 File Offset: 0x0003FEF2
		private void EmitBinaryOperator(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
		{
			if (op == ExpressionType.ArrayIndex)
			{
				this.EmitGetArrayElement(leftType);
				return;
			}
			if (leftType.IsNullableType() || rightType.IsNullableType())
			{
				this.EmitLiftedBinaryOp(op, leftType, rightType, resultType, liftedToNull);
				return;
			}
			this.EmitUnliftedBinaryOp(op, leftType, rightType);
		}

		// Token: 0x060014E5 RID: 5349 RVA: 0x00041D28 File Offset: 0x0003FF28
		private void EmitUnliftedBinaryOp(ExpressionType op, Type leftType, Type rightType)
		{
			switch (op)
			{
			case ExpressionType.Add:
				this._ilg.Emit(OpCodes.Add);
				goto IL_34E;
			case ExpressionType.AddChecked:
				this._ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Add : (leftType.IsUnsigned() ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf));
				goto IL_34E;
			case ExpressionType.And:
			case ExpressionType.AndAlso:
				this._ilg.Emit(OpCodes.And);
				return;
			case ExpressionType.ArrayLength:
			case ExpressionType.ArrayIndex:
			case ExpressionType.Call:
			case ExpressionType.Coalesce:
			case ExpressionType.Conditional:
			case ExpressionType.Constant:
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.Invoke:
			case ExpressionType.Lambda:
			case ExpressionType.ListInit:
			case ExpressionType.MemberAccess:
			case ExpressionType.MemberInit:
				goto IL_34E;
			case ExpressionType.Divide:
				this._ilg.Emit(leftType.IsUnsigned() ? OpCodes.Div_Un : OpCodes.Div);
				goto IL_34E;
			case ExpressionType.Equal:
				break;
			case ExpressionType.ExclusiveOr:
				goto IL_2FD;
			case ExpressionType.GreaterThan:
				this._ilg.Emit(leftType.IsUnsigned() ? OpCodes.Cgt_Un : OpCodes.Cgt);
				return;
			case ExpressionType.GreaterThanOrEqual:
				this._ilg.Emit((leftType.IsUnsigned() || leftType.IsFloatingPoint()) ? OpCodes.Clt_Un : OpCodes.Clt);
				this._ilg.Emit(OpCodes.Ldc_I4_0);
				this._ilg.Emit(OpCodes.Ceq);
				return;
			case ExpressionType.LeftShift:
				this.EmitShiftMask(leftType);
				this._ilg.Emit(OpCodes.Shl);
				goto IL_34E;
			case ExpressionType.LessThan:
				this._ilg.Emit(leftType.IsUnsigned() ? OpCodes.Clt_Un : OpCodes.Clt);
				return;
			case ExpressionType.LessThanOrEqual:
				this._ilg.Emit((leftType.IsUnsigned() || leftType.IsFloatingPoint()) ? OpCodes.Cgt_Un : OpCodes.Cgt);
				this._ilg.Emit(OpCodes.Ldc_I4_0);
				this._ilg.Emit(OpCodes.Ceq);
				return;
			case ExpressionType.Modulo:
				this._ilg.Emit(leftType.IsUnsigned() ? OpCodes.Rem_Un : OpCodes.Rem);
				return;
			case ExpressionType.Multiply:
				this._ilg.Emit(OpCodes.Mul);
				goto IL_34E;
			case ExpressionType.MultiplyChecked:
				this._ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Mul : (leftType.IsUnsigned() ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf));
				goto IL_34E;
			default:
				switch (op)
				{
				case ExpressionType.NotEqual:
					if (leftType.GetTypeCode() == TypeCode.Boolean)
					{
						goto IL_2FD;
					}
					this._ilg.Emit(OpCodes.Ceq);
					this._ilg.Emit(OpCodes.Ldc_I4_0);
					break;
				case ExpressionType.Or:
				case ExpressionType.OrElse:
					this._ilg.Emit(OpCodes.Or);
					return;
				case ExpressionType.Parameter:
				case ExpressionType.Power:
				case ExpressionType.Quote:
					goto IL_34E;
				case ExpressionType.RightShift:
					this.EmitShiftMask(leftType);
					this._ilg.Emit(leftType.IsUnsigned() ? OpCodes.Shr_Un : OpCodes.Shr);
					return;
				case ExpressionType.Subtract:
					this._ilg.Emit(OpCodes.Sub);
					goto IL_34E;
				case ExpressionType.SubtractChecked:
					if (leftType.IsUnsigned())
					{
						this._ilg.Emit(OpCodes.Sub_Ovf_Un);
						return;
					}
					this._ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Sub : OpCodes.Sub_Ovf);
					goto IL_34E;
				default:
					goto IL_34E;
				}
				break;
			}
			this._ilg.Emit(OpCodes.Ceq);
			return;
			IL_2FD:
			this._ilg.Emit(OpCodes.Xor);
			return;
			IL_34E:
			this.EmitConvertArithmeticResult(op, leftType);
		}

		// Token: 0x060014E6 RID: 5350 RVA: 0x0004208C File Offset: 0x0004028C
		private void EmitShiftMask(Type leftType)
		{
			int value = leftType.IsInteger64() ? 63 : 31;
			this._ilg.EmitPrimitive(value);
			this._ilg.Emit(OpCodes.And);
		}

		// Token: 0x060014E7 RID: 5351 RVA: 0x000420C4 File Offset: 0x000402C4
		private void EmitConvertArithmeticResult(ExpressionType op, Type resultType)
		{
			switch (resultType.GetTypeCode())
			{
			case TypeCode.SByte:
				this._ilg.Emit(LambdaCompiler.IsChecked(op) ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
				return;
			case TypeCode.Byte:
				this._ilg.Emit(LambdaCompiler.IsChecked(op) ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
				return;
			case TypeCode.Int16:
				this._ilg.Emit(LambdaCompiler.IsChecked(op) ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
				return;
			case TypeCode.UInt16:
				this._ilg.Emit(LambdaCompiler.IsChecked(op) ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
				return;
			default:
				return;
			}
		}

		// Token: 0x060014E8 RID: 5352 RVA: 0x00042170 File Offset: 0x00040370
		private void EmitLiftedBinaryOp(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
		{
			if (op > ExpressionType.And)
			{
				switch (op)
				{
				case ExpressionType.Divide:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.LeftShift:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
					goto IL_CF;
				case ExpressionType.Equal:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.NotEqual:
					if (liftedToNull)
					{
						this.EmitLiftedToNullRelational(op, leftType);
						return;
					}
					this.EmitLiftedRelational(op, leftType);
					break;
				case ExpressionType.Invoke:
				case ExpressionType.Lambda:
				case ExpressionType.ListInit:
				case ExpressionType.MemberAccess:
				case ExpressionType.MemberInit:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.New:
				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
				case ExpressionType.Not:
					break;
				case ExpressionType.Or:
					if (leftType == typeof(bool?))
					{
						this.EmitLiftedBooleanOr();
						return;
					}
					this.EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
					return;
				default:
					if (op - ExpressionType.RightShift > 2)
					{
						return;
					}
					goto IL_CF;
				}
				return;
			}
			if (op > ExpressionType.AddChecked)
			{
				if (op != ExpressionType.And)
				{
					return;
				}
				if (leftType == typeof(bool?))
				{
					this.EmitLiftedBooleanAnd();
					return;
				}
				this.EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
				return;
			}
			IL_CF:
			this.EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
		}

		// Token: 0x060014E9 RID: 5353 RVA: 0x00042270 File Offset: 0x00040470
		private void EmitLiftedRelational(ExpressionType op, Type type)
		{
			bool flag = op == ExpressionType.NotEqual;
			if (flag)
			{
				op = ExpressionType.Equal;
			}
			LocalBuilder local = this.GetLocal(type);
			LocalBuilder local2 = this.GetLocal(type);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(type);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(type);
			Type nonNullableType = type.GetNonNullableType();
			this.EmitUnliftedBinaryOp(op, nonNullableType, nonNullableType);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(type);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitHasValue(type);
			this.FreeLocal(local);
			this.FreeLocal(local2);
			this._ilg.Emit((op == ExpressionType.Equal) ? OpCodes.Ceq : OpCodes.And);
			this._ilg.Emit(OpCodes.And);
			if (flag)
			{
				this._ilg.Emit(OpCodes.Ldc_I4_0);
				this._ilg.Emit(OpCodes.Ceq);
			}
		}

		// Token: 0x060014EA RID: 5354 RVA: 0x0004239C File Offset: 0x0004059C
		private void EmitLiftedToNullRelational(ExpressionType op, Type type)
		{
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(type);
			LocalBuilder local2 = this.GetLocal(type);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(type);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitHasValue(type);
			this._ilg.Emit(OpCodes.And);
			this._ilg.Emit(OpCodes.Brtrue_S, label);
			this._ilg.EmitDefault(typeof(bool?), this);
			this._ilg.Emit(OpCodes.Br_S, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(type);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(type);
			this.FreeLocal(local);
			this.FreeLocal(local2);
			Type nonNullableType = type.GetNonNullableType();
			this.EmitUnliftedBinaryOp(op, nonNullableType, nonNullableType);
			this._ilg.Emit(OpCodes.Newobj, CachedReflectionInfo.Nullable_Boolean_Ctor);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x060014EB RID: 5355 RVA: 0x00042500 File Offset: 0x00040700
		private void EmitLiftedBinaryArithmetic(ExpressionType op, Type leftType, Type rightType, Type resultType)
		{
			bool flag = leftType.IsNullableType();
			bool flag2 = rightType.IsNullableType();
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(leftType);
			LocalBuilder local2 = this.GetLocal(rightType);
			LocalBuilder local3 = this.GetLocal(resultType);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Stloc, local);
			if (flag)
			{
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitHasValue(leftType);
			}
			if (flag2)
			{
				this._ilg.Emit(OpCodes.Ldloca, local2);
				this._ilg.EmitHasValue(rightType);
				if (flag)
				{
					this._ilg.Emit(OpCodes.And);
				}
			}
			this._ilg.Emit(OpCodes.Brfalse_S, label);
			if (flag)
			{
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitGetValueOrDefault(leftType);
			}
			else
			{
				this._ilg.Emit(OpCodes.Ldloc, local);
			}
			if (flag2)
			{
				this._ilg.Emit(OpCodes.Ldloca, local2);
				this._ilg.EmitGetValueOrDefault(rightType);
			}
			else
			{
				this._ilg.Emit(OpCodes.Ldloc, local2);
			}
			this.FreeLocal(local);
			this.FreeLocal(local2);
			this.EmitBinaryOperator(op, leftType.GetNonNullableType(), rightType.GetNonNullableType(), resultType.GetNonNullableType(), false);
			ConstructorInfo constructor = resultType.GetConstructor(new Type[]
			{
				resultType.GetNonNullableType()
			});
			this._ilg.Emit(OpCodes.Newobj, constructor);
			this._ilg.Emit(OpCodes.Stloc, local3);
			this._ilg.Emit(OpCodes.Br_S, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloca, local3);
			this._ilg.Emit(OpCodes.Initobj, resultType);
			this._ilg.MarkLabel(label2);
			this._ilg.Emit(OpCodes.Ldloc, local3);
			this.FreeLocal(local3);
		}

		// Token: 0x060014EC RID: 5356 RVA: 0x0004270C File Offset: 0x0004090C
		private void EmitLiftedBooleanAnd()
		{
			Type typeFromHandle = typeof(bool?);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(typeFromHandle);
			LocalBuilder local2 = this.GetLocal(typeFromHandle);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Brtrue_S, label);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(typeFromHandle);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Or);
			this._ilg.Emit(OpCodes.Brfalse_S, label);
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			this._ilg.Emit(OpCodes.Br_S, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloc, local2);
			this.FreeLocal(local2);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x060014ED RID: 5357 RVA: 0x00042858 File Offset: 0x00040A58
		private void EmitLiftedBooleanOr()
		{
			Type typeFromHandle = typeof(bool?);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(typeFromHandle);
			LocalBuilder local2 = this.GetLocal(typeFromHandle);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Brtrue_S, label);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(typeFromHandle);
			this._ilg.Emit(OpCodes.Or);
			this._ilg.Emit(OpCodes.Brfalse_S, label);
			this._ilg.Emit(OpCodes.Ldloc, local2);
			this.FreeLocal(local2);
			this._ilg.Emit(OpCodes.Br_S, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x060014EE RID: 5358 RVA: 0x000429A4 File Offset: 0x00040BA4
		private LabelInfo EnsureLabel(LabelTarget node)
		{
			LabelInfo result;
			if (!this._labelInfo.TryGetValue(node, out result))
			{
				this._labelInfo.Add(node, result = new LabelInfo(this._ilg, node, false));
			}
			return result;
		}

		// Token: 0x060014EF RID: 5359 RVA: 0x000429DD File Offset: 0x00040BDD
		private LabelInfo ReferenceLabel(LabelTarget node)
		{
			LabelInfo labelInfo = this.EnsureLabel(node);
			labelInfo.Reference(this._labelBlock);
			return labelInfo;
		}

		// Token: 0x060014F0 RID: 5360 RVA: 0x000429F2 File Offset: 0x00040BF2
		private LabelInfo DefineLabel(LabelTarget node)
		{
			if (node == null)
			{
				return new LabelInfo(this._ilg, null, false);
			}
			LabelInfo labelInfo = this.EnsureLabel(node);
			labelInfo.Define(this._labelBlock);
			return labelInfo;
		}

		// Token: 0x060014F1 RID: 5361 RVA: 0x00042A18 File Offset: 0x00040C18
		private void PushLabelBlock(LabelScopeKind type)
		{
			this._labelBlock = new LabelScopeInfo(this._labelBlock, type);
		}

		// Token: 0x060014F2 RID: 5362 RVA: 0x00042A2C File Offset: 0x00040C2C
		private void PopLabelBlock(LabelScopeKind kind)
		{
			this._labelBlock = this._labelBlock.Parent;
		}

		// Token: 0x060014F3 RID: 5363 RVA: 0x00042A40 File Offset: 0x00040C40
		private void EmitLabelExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			LabelExpression labelExpression = (LabelExpression)expr;
			LabelInfo labelInfo = null;
			if (this._labelBlock.Kind == LabelScopeKind.Block)
			{
				this._labelBlock.TryGetLabelInfo(labelExpression.Target, out labelInfo);
				if (labelInfo == null && this._labelBlock.Parent.Kind == LabelScopeKind.Switch)
				{
					this._labelBlock.Parent.TryGetLabelInfo(labelExpression.Target, out labelInfo);
				}
			}
			if (labelInfo == null)
			{
				labelInfo = this.DefineLabel(labelExpression.Target);
			}
			if (labelExpression.DefaultValue != null)
			{
				if (labelExpression.Target.Type == typeof(void))
				{
					this.EmitExpressionAsVoid(labelExpression.DefaultValue, flags);
				}
				else
				{
					flags = LambdaCompiler.UpdateEmitExpressionStartFlag(flags, LambdaCompiler.CompilationFlags.EmitExpressionStart);
					this.EmitExpression(labelExpression.DefaultValue, flags);
				}
			}
			labelInfo.Mark();
		}

		// Token: 0x060014F4 RID: 5364 RVA: 0x00042B08 File Offset: 0x00040D08
		private void EmitGotoExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			GotoExpression gotoExpression = (GotoExpression)expr;
			LabelInfo labelInfo = this.ReferenceLabel(gotoExpression.Target);
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask;
			if (compilationFlags != LambdaCompiler.CompilationFlags.EmitAsNoTail)
			{
				compilationFlags = (labelInfo.CanReturn ? LambdaCompiler.CompilationFlags.EmitAsTail : LambdaCompiler.CompilationFlags.EmitAsNoTail);
				flags = LambdaCompiler.UpdateEmitAsTailCallFlag(flags, compilationFlags);
			}
			if (gotoExpression.Value != null)
			{
				if (gotoExpression.Target.Type == typeof(void))
				{
					this.EmitExpressionAsVoid(gotoExpression.Value, flags);
				}
				else
				{
					flags = LambdaCompiler.UpdateEmitExpressionStartFlag(flags, LambdaCompiler.CompilationFlags.EmitExpressionStart);
					this.EmitExpression(gotoExpression.Value, flags);
				}
			}
			labelInfo.EmitJump();
			this.EmitUnreachable(gotoExpression, flags);
		}

		// Token: 0x060014F5 RID: 5365 RVA: 0x00042BAE File Offset: 0x00040DAE
		private void EmitUnreachable(Expression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.Type != typeof(void) && (flags & LambdaCompiler.CompilationFlags.EmitAsVoidType) == (LambdaCompiler.CompilationFlags)0)
			{
				this._ilg.EmitDefault(node.Type, this);
			}
		}

		// Token: 0x060014F6 RID: 5366 RVA: 0x00042BE0 File Offset: 0x00040DE0
		private bool TryPushLabelBlock(Expression node)
		{
			ExpressionType nodeType = node.NodeType;
			if (nodeType <= ExpressionType.Convert)
			{
				if (nodeType == ExpressionType.Conditional)
				{
					goto IL_15F;
				}
				if (nodeType == ExpressionType.Convert)
				{
					if (!(node.Type != typeof(void)))
					{
						this.PushLabelBlock(LabelScopeKind.Statement);
						return true;
					}
				}
			}
			else if (nodeType != ExpressionType.Block)
			{
				switch (nodeType)
				{
				case ExpressionType.Goto:
				case ExpressionType.Loop:
					goto IL_15F;
				case ExpressionType.Label:
					if (this._labelBlock.Kind == LabelScopeKind.Block)
					{
						LabelTarget target = ((LabelExpression)node).Target;
						if (this._labelBlock.ContainsTarget(target))
						{
							return false;
						}
						if (this._labelBlock.Parent.Kind == LabelScopeKind.Switch && this._labelBlock.Parent.ContainsTarget(target))
						{
							return false;
						}
					}
					this.PushLabelBlock(LabelScopeKind.Statement);
					return true;
				case ExpressionType.Switch:
				{
					this.PushLabelBlock(LabelScopeKind.Switch);
					SwitchExpression switchExpression = (SwitchExpression)node;
					foreach (SwitchCase switchCase in switchExpression.Cases)
					{
						this.DefineBlockLabels(switchCase.Body);
					}
					this.DefineBlockLabels(switchExpression.DefaultBody);
					return true;
				}
				}
			}
			else if (!(node is SpilledExpressionBlock))
			{
				this.PushLabelBlock(LabelScopeKind.Block);
				if (this._labelBlock.Parent.Kind != LabelScopeKind.Switch)
				{
					this.DefineBlockLabels(node);
				}
				return true;
			}
			if (this._labelBlock.Kind != LabelScopeKind.Expression)
			{
				this.PushLabelBlock(LabelScopeKind.Expression);
				return true;
			}
			return false;
			IL_15F:
			this.PushLabelBlock(LabelScopeKind.Statement);
			return true;
		}

		// Token: 0x060014F7 RID: 5367 RVA: 0x00042D64 File Offset: 0x00040F64
		private void DefineBlockLabels(Expression node)
		{
			BlockExpression blockExpression = node as BlockExpression;
			if (blockExpression == null || blockExpression is SpilledExpressionBlock)
			{
				return;
			}
			int i = 0;
			int expressionCount = blockExpression.ExpressionCount;
			while (i < expressionCount)
			{
				LabelExpression labelExpression = blockExpression.GetExpression(i) as LabelExpression;
				if (labelExpression != null)
				{
					this.DefineLabel(labelExpression.Target);
				}
				i++;
			}
		}

		// Token: 0x060014F8 RID: 5368 RVA: 0x00042DB4 File Offset: 0x00040FB4
		private void AddReturnLabel(LambdaExpression lambda)
		{
			Expression expression = lambda.Body;
			ExpressionType nodeType;
			for (;;)
			{
				nodeType = expression.NodeType;
				if (nodeType != ExpressionType.Block)
				{
					break;
				}
				BlockExpression blockExpression = (BlockExpression)expression;
				if (blockExpression.ExpressionCount == 0)
				{
					return;
				}
				for (int i = blockExpression.ExpressionCount - 1; i >= 0; i--)
				{
					expression = blockExpression.GetExpression(i);
					if (LambdaCompiler.Significant(expression))
					{
						break;
					}
				}
			}
			if (nodeType != ExpressionType.Label)
			{
				return;
			}
			LabelTarget target = ((LabelExpression)expression).Target;
			this._labelInfo.Add(target, new LabelInfo(this._ilg, target, TypeUtils.AreReferenceAssignable(lambda.ReturnType, target.Type)));
		}

		// Token: 0x060014F9 RID: 5369 RVA: 0x00042E4C File Offset: 0x0004104C
		private static LambdaCompiler.CompilationFlags UpdateEmitAsTailCallFlag(LambdaCompiler.CompilationFlags flags, LambdaCompiler.CompilationFlags newValue)
		{
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask;
			return (flags ^ compilationFlags) | newValue;
		}

		// Token: 0x060014FA RID: 5370 RVA: 0x00042E68 File Offset: 0x00041068
		private static LambdaCompiler.CompilationFlags UpdateEmitExpressionStartFlag(LambdaCompiler.CompilationFlags flags, LambdaCompiler.CompilationFlags newValue)
		{
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitExpressionStartMask;
			return (flags ^ compilationFlags) | newValue;
		}

		// Token: 0x060014FB RID: 5371 RVA: 0x00042E80 File Offset: 0x00041080
		private static LambdaCompiler.CompilationFlags UpdateEmitAsTypeFlag(LambdaCompiler.CompilationFlags flags, LambdaCompiler.CompilationFlags newValue)
		{
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTypeMask;
			return (flags ^ compilationFlags) | newValue;
		}

		// Token: 0x060014FC RID: 5372 RVA: 0x00042E9A File Offset: 0x0004109A
		internal void EmitExpression(Expression node)
		{
			this.EmitExpression(node, LambdaCompiler.CompilationFlags.EmitExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x060014FD RID: 5373 RVA: 0x00042EA8 File Offset: 0x000410A8
		private void EmitExpressionAsVoid(Expression node)
		{
			this.EmitExpressionAsVoid(node, LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x060014FE RID: 5374 RVA: 0x00042EB8 File Offset: 0x000410B8
		private void EmitExpressionAsVoid(Expression node, LambdaCompiler.CompilationFlags flags)
		{
			LambdaCompiler.CompilationFlags flags2 = this.EmitExpressionStart(node);
			ExpressionType nodeType = node.NodeType;
			if (nodeType <= ExpressionType.Assign)
			{
				if (nodeType == ExpressionType.Constant || nodeType == ExpressionType.Parameter)
				{
					goto IL_D5;
				}
				if (nodeType == ExpressionType.Assign)
				{
					this.EmitAssign((AssignBinaryExpression)node, LambdaCompiler.CompilationFlags.EmitAsVoidType);
					goto IL_D5;
				}
			}
			else if (nodeType <= ExpressionType.Default)
			{
				if (nodeType == ExpressionType.Block)
				{
					this.Emit((BlockExpression)node, LambdaCompiler.UpdateEmitAsTypeFlag(flags, LambdaCompiler.CompilationFlags.EmitAsVoidType));
					goto IL_D5;
				}
				if (nodeType == ExpressionType.Default)
				{
					goto IL_D5;
				}
			}
			else
			{
				if (nodeType == ExpressionType.Goto)
				{
					this.EmitGotoExpression(node, LambdaCompiler.UpdateEmitAsTypeFlag(flags, LambdaCompiler.CompilationFlags.EmitAsVoidType));
					goto IL_D5;
				}
				if (nodeType == ExpressionType.Throw)
				{
					this.EmitThrow((UnaryExpression)node, LambdaCompiler.CompilationFlags.EmitAsVoidType);
					goto IL_D5;
				}
			}
			if (node.Type == typeof(void))
			{
				this.EmitExpression(node, LambdaCompiler.UpdateEmitExpressionStartFlag(flags, LambdaCompiler.CompilationFlags.EmitNoExpressionStart));
			}
			else
			{
				this.EmitExpression(node, LambdaCompiler.CompilationFlags.EmitNoExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
				this._ilg.Emit(OpCodes.Pop);
			}
			IL_D5:
			this.EmitExpressionEnd(flags2);
		}

		// Token: 0x060014FF RID: 5375 RVA: 0x00042FA4 File Offset: 0x000411A4
		private void EmitExpressionAsType(Expression node, Type type, LambdaCompiler.CompilationFlags flags)
		{
			if (type == typeof(void))
			{
				this.EmitExpressionAsVoid(node, flags);
				return;
			}
			if (!TypeUtils.AreEquivalent(node.Type, type))
			{
				this.EmitExpression(node);
				this._ilg.Emit(OpCodes.Castclass, type);
				return;
			}
			this.EmitExpression(node, LambdaCompiler.UpdateEmitExpressionStartFlag(flags, LambdaCompiler.CompilationFlags.EmitExpressionStart));
		}

		// Token: 0x06001500 RID: 5376 RVA: 0x00043001 File Offset: 0x00041201
		private LambdaCompiler.CompilationFlags EmitExpressionStart(Expression node)
		{
			if (this.TryPushLabelBlock(node))
			{
				return LambdaCompiler.CompilationFlags.EmitExpressionStart;
			}
			return LambdaCompiler.CompilationFlags.EmitNoExpressionStart;
		}

		// Token: 0x06001501 RID: 5377 RVA: 0x0004300F File Offset: 0x0004120F
		private void EmitExpressionEnd(LambdaCompiler.CompilationFlags flags)
		{
			if ((flags & LambdaCompiler.CompilationFlags.EmitExpressionStartMask) == LambdaCompiler.CompilationFlags.EmitExpressionStart)
			{
				this.PopLabelBlock(this._labelBlock.Kind);
			}
		}

		// Token: 0x06001502 RID: 5378 RVA: 0x0004302C File Offset: 0x0004122C
		private void EmitInvocationExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			InvocationExpression invocationExpression = (InvocationExpression)expr;
			if (invocationExpression.LambdaOperand != null)
			{
				this.EmitInlinedInvoke(invocationExpression, flags);
				return;
			}
			expr = invocationExpression.Expression;
			this.EmitMethodCall(expr, expr.Type.GetInvokeMethod(), invocationExpression, LambdaCompiler.CompilationFlags.EmitExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x06001503 RID: 5379 RVA: 0x00043074 File Offset: 0x00041274
		private void EmitInlinedInvoke(InvocationExpression invoke, LambdaCompiler.CompilationFlags flags)
		{
			LambdaExpression lambdaOperand = invoke.LambdaOperand;
			List<LambdaCompiler.WriteBack> list = this.EmitArguments(lambdaOperand.Type.GetInvokeMethod(), invoke);
			LambdaCompiler lambdaCompiler = new LambdaCompiler(this, lambdaOperand, invoke);
			if (list != null)
			{
				flags = LambdaCompiler.UpdateEmitAsTailCallFlag(flags, LambdaCompiler.CompilationFlags.EmitAsNoTail);
			}
			lambdaCompiler.EmitLambdaBody(this._scope, true, flags);
			this.EmitWriteBack(list);
		}

		// Token: 0x06001504 RID: 5380 RVA: 0x000430C8 File Offset: 0x000412C8
		private void EmitIndexExpression(Expression expr)
		{
			IndexExpression indexExpression = (IndexExpression)expr;
			Type objectType = null;
			if (indexExpression.Object != null)
			{
				this.EmitInstance(indexExpression.Object, out objectType);
			}
			int i = 0;
			int argumentCount = indexExpression.ArgumentCount;
			while (i < argumentCount)
			{
				Expression argument = indexExpression.GetArgument(i);
				this.EmitExpression(argument);
				i++;
			}
			this.EmitGetIndexCall(indexExpression, objectType);
		}

		// Token: 0x06001505 RID: 5381 RVA: 0x00043120 File Offset: 0x00041320
		private void EmitIndexAssignment(AssignBinaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			IndexExpression indexExpression = (IndexExpression)node.Left;
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTypeMask;
			Type objectType = null;
			if (indexExpression.Object != null)
			{
				this.EmitInstance(indexExpression.Object, out objectType);
			}
			int i = 0;
			int argumentCount = indexExpression.ArgumentCount;
			while (i < argumentCount)
			{
				Expression argument = indexExpression.GetArgument(i);
				this.EmitExpression(argument);
				i++;
			}
			this.EmitExpression(node.Right);
			LocalBuilder local = null;
			if (compilationFlags != LambdaCompiler.CompilationFlags.EmitAsVoidType)
			{
				this._ilg.Emit(OpCodes.Dup);
				this._ilg.Emit(OpCodes.Stloc, local = this.GetLocal(node.Type));
			}
			this.EmitSetIndexCall(indexExpression, objectType);
			if (compilationFlags != LambdaCompiler.CompilationFlags.EmitAsVoidType)
			{
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
			}
		}

		// Token: 0x06001506 RID: 5382 RVA: 0x000431EC File Offset: 0x000413EC
		private void EmitGetIndexCall(IndexExpression node, Type objectType)
		{
			if (node.Indexer != null)
			{
				MethodInfo getMethod = node.Indexer.GetGetMethod(true);
				this.EmitCall(objectType, getMethod);
				return;
			}
			this.EmitGetArrayElement(objectType);
		}

		// Token: 0x06001507 RID: 5383 RVA: 0x00043224 File Offset: 0x00041424
		private void EmitGetArrayElement(Type arrayType)
		{
			if (arrayType.IsSZArray)
			{
				this._ilg.EmitLoadElement(arrayType.GetElementType());
				return;
			}
			this._ilg.Emit(OpCodes.Call, arrayType.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public));
		}

		// Token: 0x06001508 RID: 5384 RVA: 0x00043260 File Offset: 0x00041460
		private void EmitSetIndexCall(IndexExpression node, Type objectType)
		{
			if (node.Indexer != null)
			{
				MethodInfo setMethod = node.Indexer.GetSetMethod(true);
				this.EmitCall(objectType, setMethod);
				return;
			}
			this.EmitSetArrayElement(objectType);
		}

		// Token: 0x06001509 RID: 5385 RVA: 0x00043298 File Offset: 0x00041498
		private void EmitSetArrayElement(Type arrayType)
		{
			if (arrayType.IsSZArray)
			{
				this._ilg.EmitStoreElement(arrayType.GetElementType());
				return;
			}
			this._ilg.Emit(OpCodes.Call, arrayType.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public));
		}

		// Token: 0x0600150A RID: 5386 RVA: 0x000432D4 File Offset: 0x000414D4
		private void EmitMethodCallExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			MethodCallExpression methodCallExpression = (MethodCallExpression)expr;
			this.EmitMethodCall(methodCallExpression.Object, methodCallExpression.Method, methodCallExpression, flags);
		}

		// Token: 0x0600150B RID: 5387 RVA: 0x000432FC File Offset: 0x000414FC
		private void EmitMethodCallExpression(Expression expr)
		{
			this.EmitMethodCallExpression(expr, LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x0600150C RID: 5388 RVA: 0x0004330A File Offset: 0x0004150A
		private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr)
		{
			this.EmitMethodCall(obj, method, methodCallExpr, LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x0600150D RID: 5389 RVA: 0x0004331C File Offset: 0x0004151C
		private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr, LambdaCompiler.CompilationFlags flags)
		{
			Type objectType = null;
			if (!method.IsStatic)
			{
				this.EmitInstance(obj, out objectType);
			}
			if (obj != null && obj.Type.IsValueType)
			{
				this.EmitMethodCall(method, methodCallExpr, objectType);
				return;
			}
			this.EmitMethodCall(method, methodCallExpr, objectType, flags);
		}

		// Token: 0x0600150E RID: 5390 RVA: 0x00043361 File Offset: 0x00041561
		private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type objectType)
		{
			this.EmitMethodCall(mi, args, objectType, LambdaCompiler.CompilationFlags.EmitAsNoTail);
		}

		// Token: 0x0600150F RID: 5391 RVA: 0x00043374 File Offset: 0x00041574
		private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type objectType, LambdaCompiler.CompilationFlags flags)
		{
			List<LambdaCompiler.WriteBack> writeBacks = this.EmitArguments(mi, args);
			OpCode opCode = LambdaCompiler.UseVirtual(mi) ? OpCodes.Callvirt : OpCodes.Call;
			if (opCode == OpCodes.Callvirt && objectType.IsValueType)
			{
				this._ilg.Emit(OpCodes.Constrained, objectType);
			}
			if ((flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask) == LambdaCompiler.CompilationFlags.EmitAsTail && !LambdaCompiler.MethodHasByRefParameter(mi))
			{
				this._ilg.Emit(OpCodes.Tailcall);
			}
			if (mi.CallingConvention == CallingConventions.VarArgs)
			{
				int argumentCount = args.ArgumentCount;
				Type[] array = new Type[argumentCount];
				for (int i = 0; i < argumentCount; i++)
				{
					array[i] = args.GetArgument(i).Type;
				}
				this._ilg.EmitCall(opCode, mi, array);
			}
			else
			{
				this._ilg.Emit(opCode, mi);
			}
			this.EmitWriteBack(writeBacks);
		}

		// Token: 0x06001510 RID: 5392 RVA: 0x00043448 File Offset: 0x00041648
		private static bool MethodHasByRefParameter(MethodInfo mi)
		{
			ParameterInfo[] parametersCached = mi.GetParametersCached();
			for (int i = 0; i < parametersCached.Length; i++)
			{
				if (parametersCached[i].IsByRefParameter())
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001511 RID: 5393 RVA: 0x00043478 File Offset: 0x00041678
		private void EmitCall(Type objectType, MethodInfo method)
		{
			if (method.CallingConvention == CallingConventions.VarArgs)
			{
				throw Error.UnexpectedVarArgsCall(method);
			}
			OpCode opCode = LambdaCompiler.UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call;
			if (opCode == OpCodes.Callvirt && objectType.IsValueType)
			{
				this._ilg.Emit(OpCodes.Constrained, objectType);
			}
			this._ilg.Emit(opCode, method);
		}

		// Token: 0x06001512 RID: 5394 RVA: 0x000434DD File Offset: 0x000416DD
		private static bool UseVirtual(MethodInfo mi)
		{
			return !mi.IsStatic && !mi.DeclaringType.IsValueType;
		}

		// Token: 0x06001513 RID: 5395 RVA: 0x000434F9 File Offset: 0x000416F9
		private List<LambdaCompiler.WriteBack> EmitArguments(MethodBase method, IArgumentProvider args)
		{
			return this.EmitArguments(method, args, 0);
		}

		// Token: 0x06001514 RID: 5396 RVA: 0x00043504 File Offset: 0x00041704
		private List<LambdaCompiler.WriteBack> EmitArguments(MethodBase method, IArgumentProvider args, int skipParameters)
		{
			ParameterInfo[] parametersCached = method.GetParametersCached();
			List<LambdaCompiler.WriteBack> list = null;
			int i = skipParameters;
			int num = parametersCached.Length;
			while (i < num)
			{
				ParameterInfo parameterInfo = parametersCached[i];
				Expression argument = args.GetArgument(i - skipParameters);
				Type type = parameterInfo.ParameterType;
				if (type.IsByRef)
				{
					type = type.GetElementType();
					LambdaCompiler.WriteBack writeBack = this.EmitAddressWriteBack(argument, type);
					if (writeBack != null)
					{
						if (list == null)
						{
							list = new List<LambdaCompiler.WriteBack>();
						}
						list.Add(writeBack);
					}
				}
				else
				{
					this.EmitExpression(argument);
				}
				i++;
			}
			return list;
		}

		// Token: 0x06001515 RID: 5397 RVA: 0x00043580 File Offset: 0x00041780
		private void EmitWriteBack(List<LambdaCompiler.WriteBack> writeBacks)
		{
			if (writeBacks != null)
			{
				foreach (LambdaCompiler.WriteBack writeBack in writeBacks)
				{
					writeBack(this);
				}
			}
		}

		// Token: 0x06001516 RID: 5398 RVA: 0x000435D0 File Offset: 0x000417D0
		private void EmitConstantExpression(Expression expr)
		{
			ConstantExpression constantExpression = (ConstantExpression)expr;
			this.EmitConstant(constantExpression.Value, constantExpression.Type);
		}

		// Token: 0x06001517 RID: 5399 RVA: 0x000435F6 File Offset: 0x000417F6
		private void EmitConstant(object value)
		{
			this.EmitConstant(value, value.GetType());
		}

		// Token: 0x06001518 RID: 5400 RVA: 0x00043605 File Offset: 0x00041805
		private void EmitConstant(object value, Type type)
		{
			if (!this._ilg.TryEmitConstant(value, type, this))
			{
				this._boundConstants.EmitConstant(this, value, type);
			}
		}

		// Token: 0x06001519 RID: 5401 RVA: 0x00043628 File Offset: 0x00041828
		private void EmitDynamicExpression(Expression expr)
		{
			if (!(this._method is DynamicMethod))
			{
				throw Error.CannotCompileDynamic();
			}
			IDynamicExpression dynamicExpression = (IDynamicExpression)expr;
			object obj = dynamicExpression.CreateCallSite();
			Type type = obj.GetType();
			MethodInfo invokeMethod = dynamicExpression.DelegateType.GetInvokeMethod();
			this.EmitConstant(obj, type);
			this._ilg.Emit(OpCodes.Dup);
			LocalBuilder local = this.GetLocal(type);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldfld, type.GetField("Target"));
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			List<LambdaCompiler.WriteBack> writeBacks = this.EmitArguments(invokeMethod, dynamicExpression, 1);
			this._ilg.Emit(OpCodes.Callvirt, invokeMethod);
			this.EmitWriteBack(writeBacks);
		}

		// Token: 0x0600151A RID: 5402 RVA: 0x000436F8 File Offset: 0x000418F8
		private void EmitNewExpression(Expression expr)
		{
			NewExpression newExpression = (NewExpression)expr;
			if (!(newExpression.Constructor != null))
			{
				LocalBuilder local = this.GetLocal(newExpression.Type);
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.Emit(OpCodes.Initobj, newExpression.Type);
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
				return;
			}
			if (newExpression.Constructor.DeclaringType.IsAbstract)
			{
				throw Error.NonAbstractConstructorRequired();
			}
			List<LambdaCompiler.WriteBack> writeBacks = this.EmitArguments(newExpression.Constructor, newExpression);
			this._ilg.Emit(OpCodes.Newobj, newExpression.Constructor);
			this.EmitWriteBack(writeBacks);
		}

		// Token: 0x0600151B RID: 5403 RVA: 0x000437AC File Offset: 0x000419AC
		private void EmitTypeBinaryExpression(Expression expr)
		{
			TypeBinaryExpression typeBinaryExpression = (TypeBinaryExpression)expr;
			if (typeBinaryExpression.NodeType == ExpressionType.TypeEqual)
			{
				this.EmitExpression(typeBinaryExpression.ReduceTypeEqual());
				return;
			}
			Type type = typeBinaryExpression.Expression.Type;
			AnalyzeTypeIsResult analyzeTypeIsResult = ConstantCheck.AnalyzeTypeIs(typeBinaryExpression);
			if (analyzeTypeIsResult == AnalyzeTypeIsResult.KnownTrue || analyzeTypeIsResult == AnalyzeTypeIsResult.KnownFalse)
			{
				this.EmitExpressionAsVoid(typeBinaryExpression.Expression);
				this._ilg.EmitPrimitive(analyzeTypeIsResult == AnalyzeTypeIsResult.KnownTrue);
				return;
			}
			if (analyzeTypeIsResult != AnalyzeTypeIsResult.KnownAssignable)
			{
				this.EmitExpression(typeBinaryExpression.Expression);
				if (type.IsValueType)
				{
					this._ilg.Emit(OpCodes.Box, type);
				}
				this._ilg.Emit(OpCodes.Isinst, typeBinaryExpression.TypeOperand);
				this._ilg.Emit(OpCodes.Ldnull);
				this._ilg.Emit(OpCodes.Cgt_Un);
				return;
			}
			if (type.IsNullableType())
			{
				this.EmitAddress(typeBinaryExpression.Expression, type);
				this._ilg.EmitHasValue(type);
				return;
			}
			this.EmitExpression(typeBinaryExpression.Expression);
			this._ilg.Emit(OpCodes.Ldnull);
			this._ilg.Emit(OpCodes.Cgt_Un);
		}

		// Token: 0x0600151C RID: 5404 RVA: 0x000438BC File Offset: 0x00041ABC
		private void EmitVariableAssignment(AssignBinaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			ParameterExpression parameterExpression = (ParameterExpression)node.Left;
			int num = (int)(flags & LambdaCompiler.CompilationFlags.EmitAsTypeMask);
			if (node.IsByRef)
			{
				this.EmitAddress(node.Right, node.Right.Type);
			}
			else
			{
				this.EmitExpression(node.Right);
			}
			if (num != 32)
			{
				this._ilg.Emit(OpCodes.Dup);
			}
			if (parameterExpression.IsByRef)
			{
				LocalBuilder local = this.GetLocal(parameterExpression.Type);
				this._ilg.Emit(OpCodes.Stloc, local);
				this._scope.EmitGet(parameterExpression);
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
				this._ilg.EmitStoreValueIndirect(parameterExpression.Type);
				return;
			}
			this._scope.EmitSet(parameterExpression);
		}

		// Token: 0x0600151D RID: 5405 RVA: 0x00043985 File Offset: 0x00041B85
		private void EmitAssignBinaryExpression(Expression expr)
		{
			this.EmitAssign((AssignBinaryExpression)expr, LambdaCompiler.CompilationFlags.EmitAsDefaultType);
		}

		// Token: 0x0600151E RID: 5406 RVA: 0x00043998 File Offset: 0x00041B98
		private void EmitAssign(AssignBinaryExpression node, LambdaCompiler.CompilationFlags emitAs)
		{
			ExpressionType nodeType = node.Left.NodeType;
			if (nodeType == ExpressionType.MemberAccess)
			{
				this.EmitMemberAssignment(node, emitAs);
				return;
			}
			if (nodeType == ExpressionType.Parameter)
			{
				this.EmitVariableAssignment(node, emitAs);
				return;
			}
			if (nodeType == ExpressionType.Index)
			{
				this.EmitIndexAssignment(node, emitAs);
				return;
			}
			throw ContractUtils.Unreachable;
		}

		// Token: 0x0600151F RID: 5407 RVA: 0x000439E0 File Offset: 0x00041BE0
		private void EmitParameterExpression(Expression expr)
		{
			ParameterExpression parameterExpression = (ParameterExpression)expr;
			this._scope.EmitGet(parameterExpression);
			if (parameterExpression.IsByRef)
			{
				this._ilg.EmitLoadValueIndirect(parameterExpression.Type);
			}
		}

		// Token: 0x06001520 RID: 5408 RVA: 0x00043A1C File Offset: 0x00041C1C
		private void EmitLambdaExpression(Expression expr)
		{
			LambdaExpression lambda = (LambdaExpression)expr;
			this.EmitDelegateConstruction(lambda);
		}

		// Token: 0x06001521 RID: 5409 RVA: 0x00043A38 File Offset: 0x00041C38
		private void EmitRuntimeVariablesExpression(Expression expr)
		{
			RuntimeVariablesExpression runtimeVariablesExpression = (RuntimeVariablesExpression)expr;
			this._scope.EmitVariableAccess(this, runtimeVariablesExpression.Variables);
		}

		// Token: 0x06001522 RID: 5410 RVA: 0x00043A60 File Offset: 0x00041C60
		private void EmitMemberAssignment(AssignBinaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			MemberExpression memberExpression = (MemberExpression)node.Left;
			MemberInfo member = memberExpression.Member;
			Type objectType = null;
			if (memberExpression.Expression != null)
			{
				this.EmitInstance(memberExpression.Expression, out objectType);
			}
			this.EmitExpression(node.Right);
			LocalBuilder local = null;
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTypeMask;
			if (compilationFlags != LambdaCompiler.CompilationFlags.EmitAsVoidType)
			{
				this._ilg.Emit(OpCodes.Dup);
				this._ilg.Emit(OpCodes.Stloc, local = this.GetLocal(node.Type));
			}
			if (member is FieldInfo)
			{
				this._ilg.EmitFieldSet((FieldInfo)member);
			}
			else
			{
				PropertyInfo propertyInfo = (PropertyInfo)member;
				this.EmitCall(objectType, propertyInfo.GetSetMethod(true));
			}
			if (compilationFlags != LambdaCompiler.CompilationFlags.EmitAsVoidType)
			{
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
			}
		}

		// Token: 0x06001523 RID: 5411 RVA: 0x00043B30 File Offset: 0x00041D30
		private void EmitMemberExpression(Expression expr)
		{
			MemberExpression memberExpression = (MemberExpression)expr;
			Type objectType = null;
			if (memberExpression.Expression != null)
			{
				this.EmitInstance(memberExpression.Expression, out objectType);
			}
			this.EmitMemberGet(memberExpression.Member, objectType);
		}

		// Token: 0x06001524 RID: 5412 RVA: 0x00043B6C File Offset: 0x00041D6C
		private void EmitMemberGet(MemberInfo member, Type objectType)
		{
			FieldInfo fieldInfo = member as FieldInfo;
			if (fieldInfo == null)
			{
				PropertyInfo propertyInfo = (PropertyInfo)member;
				this.EmitCall(objectType, propertyInfo.GetGetMethod(true));
				return;
			}
			if (fieldInfo.IsLiteral)
			{
				this.EmitConstant(fieldInfo.GetRawConstantValue(), fieldInfo.FieldType);
				return;
			}
			this._ilg.EmitFieldGet(fieldInfo);
		}

		// Token: 0x06001525 RID: 5413 RVA: 0x00043BC0 File Offset: 0x00041DC0
		private void EmitInstance(Expression instance, out Type type)
		{
			type = instance.Type;
			if (type.IsByRef)
			{
				type = type.GetElementType();
				this.EmitExpression(instance);
				return;
			}
			if (type.IsValueType)
			{
				this.EmitAddress(instance, type);
				return;
			}
			this.EmitExpression(instance);
		}

		// Token: 0x06001526 RID: 5414 RVA: 0x00043C00 File Offset: 0x00041E00
		private void EmitNewArrayExpression(Expression expr)
		{
			NewArrayExpression newArrayExpression = (NewArrayExpression)expr;
			ReadOnlyCollection<Expression> expressions = newArrayExpression.Expressions;
			int count = expressions.Count;
			if (newArrayExpression.NodeType == ExpressionType.NewArrayInit)
			{
				Type elementType = newArrayExpression.Type.GetElementType();
				this._ilg.EmitArray(elementType, count);
				for (int i = 0; i < count; i++)
				{
					this._ilg.Emit(OpCodes.Dup);
					this._ilg.EmitPrimitive(i);
					this.EmitExpression(expressions[i]);
					this._ilg.EmitStoreElement(elementType);
				}
				return;
			}
			for (int j = 0; j < count; j++)
			{
				Expression expression = expressions[j];
				this.EmitExpression(expression);
				this._ilg.EmitConvertToType(expression.Type, typeof(int), true, this);
			}
			this._ilg.EmitArray(newArrayExpression.Type);
		}

		// Token: 0x06001527 RID: 5415 RVA: 0x00003A59 File Offset: 0x00001C59
		private void EmitDebugInfoExpression(Expression expr)
		{
		}

		// Token: 0x06001528 RID: 5416 RVA: 0x00043CDE File Offset: 0x00041EDE
		private void EmitListInitExpression(Expression expr)
		{
			this.EmitListInit((ListInitExpression)expr);
		}

		// Token: 0x06001529 RID: 5417 RVA: 0x00043CEC File Offset: 0x00041EEC
		private void EmitMemberInitExpression(Expression expr)
		{
			this.EmitMemberInit((MemberInitExpression)expr);
		}

		// Token: 0x0600152A RID: 5418 RVA: 0x00043CFC File Offset: 0x00041EFC
		private void EmitBinding(MemberBinding binding, Type objectType)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
				this.EmitMemberAssignment((MemberAssignment)binding, objectType);
				return;
			case MemberBindingType.MemberBinding:
				this.EmitMemberMemberBinding((MemberMemberBinding)binding);
				return;
			case MemberBindingType.ListBinding:
				this.EmitMemberListBinding((MemberListBinding)binding);
				return;
			default:
				return;
			}
		}

		// Token: 0x0600152B RID: 5419 RVA: 0x00043D4C File Offset: 0x00041F4C
		private void EmitMemberAssignment(MemberAssignment binding, Type objectType)
		{
			this.EmitExpression(binding.Expression);
			FieldInfo fieldInfo = binding.Member as FieldInfo;
			if (fieldInfo != null)
			{
				this._ilg.Emit(OpCodes.Stfld, fieldInfo);
				return;
			}
			this.EmitCall(objectType, (binding.Member as PropertyInfo).GetSetMethod(true));
		}

		// Token: 0x0600152C RID: 5420 RVA: 0x00043DA0 File Offset: 0x00041FA0
		private void EmitMemberMemberBinding(MemberMemberBinding binding)
		{
			Type memberType = LambdaCompiler.GetMemberType(binding.Member);
			if (binding.Member is PropertyInfo && memberType.IsValueType)
			{
				throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(binding.Member);
			}
			if (memberType.IsValueType)
			{
				this.EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
			}
			else
			{
				this.EmitMemberGet(binding.Member, binding.Member.DeclaringType);
			}
			this.EmitMemberInit(binding.Bindings, false, memberType);
		}

		// Token: 0x0600152D RID: 5421 RVA: 0x00043E20 File Offset: 0x00042020
		private void EmitMemberListBinding(MemberListBinding binding)
		{
			Type memberType = LambdaCompiler.GetMemberType(binding.Member);
			if (binding.Member is PropertyInfo && memberType.IsValueType)
			{
				throw Error.CannotAutoInitializeValueTypeElementThroughProperty(binding.Member);
			}
			if (memberType.IsValueType)
			{
				this.EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
			}
			else
			{
				this.EmitMemberGet(binding.Member, binding.Member.DeclaringType);
			}
			this.EmitListInit(binding.Initializers, false, memberType);
		}

		// Token: 0x0600152E RID: 5422 RVA: 0x00043EA0 File Offset: 0x000420A0
		private void EmitMemberInit(MemberInitExpression init)
		{
			this.EmitExpression(init.NewExpression);
			LocalBuilder localBuilder = null;
			if (init.NewExpression.Type.IsValueType && init.Bindings.Count > 0)
			{
				localBuilder = this.GetLocal(init.NewExpression.Type);
				this._ilg.Emit(OpCodes.Stloc, localBuilder);
				this._ilg.Emit(OpCodes.Ldloca, localBuilder);
			}
			this.EmitMemberInit(init.Bindings, localBuilder == null, init.NewExpression.Type);
			if (localBuilder != null)
			{
				this._ilg.Emit(OpCodes.Ldloc, localBuilder);
				this.FreeLocal(localBuilder);
			}
		}

		// Token: 0x0600152F RID: 5423 RVA: 0x00043F48 File Offset: 0x00042148
		private void EmitMemberInit(ReadOnlyCollection<MemberBinding> bindings, bool keepOnStack, Type objectType)
		{
			int count = bindings.Count;
			if (count == 0)
			{
				if (!keepOnStack)
				{
					this._ilg.Emit(OpCodes.Pop);
					return;
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (keepOnStack || i < count - 1)
					{
						this._ilg.Emit(OpCodes.Dup);
					}
					this.EmitBinding(bindings[i], objectType);
				}
			}
		}

		// Token: 0x06001530 RID: 5424 RVA: 0x00043FA8 File Offset: 0x000421A8
		private void EmitListInit(ListInitExpression init)
		{
			this.EmitExpression(init.NewExpression);
			LocalBuilder localBuilder = null;
			if (init.NewExpression.Type.IsValueType)
			{
				localBuilder = this.GetLocal(init.NewExpression.Type);
				this._ilg.Emit(OpCodes.Stloc, localBuilder);
				this._ilg.Emit(OpCodes.Ldloca, localBuilder);
			}
			this.EmitListInit(init.Initializers, localBuilder == null, init.NewExpression.Type);
			if (localBuilder != null)
			{
				this._ilg.Emit(OpCodes.Ldloc, localBuilder);
				this.FreeLocal(localBuilder);
			}
		}

		// Token: 0x06001531 RID: 5425 RVA: 0x00044040 File Offset: 0x00042240
		private void EmitListInit(ReadOnlyCollection<ElementInit> initializers, bool keepOnStack, Type objectType)
		{
			int count = initializers.Count;
			if (count == 0)
			{
				if (!keepOnStack)
				{
					this._ilg.Emit(OpCodes.Pop);
					return;
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (keepOnStack || i < count - 1)
					{
						this._ilg.Emit(OpCodes.Dup);
					}
					this.EmitMethodCall(initializers[i].AddMethod, initializers[i], objectType);
					if (initializers[i].AddMethod.ReturnType != typeof(void))
					{
						this._ilg.Emit(OpCodes.Pop);
					}
				}
			}
		}

		// Token: 0x06001532 RID: 5426 RVA: 0x000440E0 File Offset: 0x000422E0
		private static Type GetMemberType(MemberInfo member)
		{
			FieldInfo fieldInfo = member as FieldInfo;
			if (fieldInfo == null)
			{
				return (member as PropertyInfo).PropertyType;
			}
			return fieldInfo.FieldType;
		}

		// Token: 0x06001533 RID: 5427 RVA: 0x0004410C File Offset: 0x0004230C
		private void EmitLift(ExpressionType nodeType, Type resultType, MethodCallExpression mc, ParameterExpression[] paramList, Expression[] argList)
		{
			switch (nodeType)
			{
			case ExpressionType.Equal:
				goto IL_2B5;
			case ExpressionType.ExclusiveOr:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
			case ExpressionType.Invoke:
			case ExpressionType.Lambda:
			case ExpressionType.LeftShift:
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
				break;
			default:
				if (nodeType == ExpressionType.NotEqual)
				{
					goto IL_2B5;
				}
				break;
			}
			IL_35:
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(typeof(bool));
			int i = 0;
			int num = paramList.Length;
			while (i < num)
			{
				ParameterExpression variable = paramList[i];
				Expression expression = argList[i];
				if (expression.Type.IsNullableType())
				{
					this._scope.AddLocal(this, variable);
					this.EmitAddress(expression, expression.Type);
					this._ilg.Emit(OpCodes.Dup);
					this._ilg.EmitHasValue(expression.Type);
					this._ilg.Emit(OpCodes.Ldc_I4_0);
					this._ilg.Emit(OpCodes.Ceq);
					this._ilg.Emit(OpCodes.Stloc, local);
					this._ilg.EmitGetValueOrDefault(expression.Type);
					this._scope.EmitSet(variable);
				}
				else
				{
					this._scope.AddLocal(this, variable);
					this.EmitExpression(expression);
					if (!expression.Type.IsValueType)
					{
						this._ilg.Emit(OpCodes.Dup);
						this._ilg.Emit(OpCodes.Ldnull);
						this._ilg.Emit(OpCodes.Ceq);
						this._ilg.Emit(OpCodes.Stloc, local);
					}
					this._scope.EmitSet(variable);
				}
				this._ilg.Emit(OpCodes.Ldloc, local);
				this._ilg.Emit(OpCodes.Brtrue, label2);
				i++;
			}
			this.EmitMethodCallExpression(mc);
			if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
			{
				ConstructorInfo constructor = resultType.GetConstructor(new Type[]
				{
					mc.Type
				});
				this._ilg.Emit(OpCodes.Newobj, constructor);
			}
			this._ilg.Emit(OpCodes.Br_S, label);
			this._ilg.MarkLabel(label2);
			if (TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
			{
				if (resultType.IsValueType)
				{
					LocalBuilder local2 = this.GetLocal(resultType);
					this._ilg.Emit(OpCodes.Ldloca, local2);
					this._ilg.Emit(OpCodes.Initobj, resultType);
					this._ilg.Emit(OpCodes.Ldloc, local2);
					this.FreeLocal(local2);
				}
				else
				{
					this._ilg.Emit(OpCodes.Ldnull);
				}
			}
			else
			{
				this._ilg.Emit(OpCodes.Ldc_I4_0);
			}
			this._ilg.MarkLabel(label);
			this.FreeLocal(local);
			return;
			IL_2B5:
			if (!TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
			{
				Label label3 = this._ilg.DefineLabel();
				Label label4 = this._ilg.DefineLabel();
				Label label5 = this._ilg.DefineLabel();
				LocalBuilder local3 = this.GetLocal(typeof(bool));
				LocalBuilder local4 = this.GetLocal(typeof(bool));
				this._ilg.Emit(OpCodes.Ldc_I4_0);
				this._ilg.Emit(OpCodes.Stloc, local3);
				this._ilg.Emit(OpCodes.Ldc_I4_1);
				this._ilg.Emit(OpCodes.Stloc, local4);
				int j = 0;
				int num2 = paramList.Length;
				while (j < num2)
				{
					ParameterExpression variable2 = paramList[j];
					Expression expression2 = argList[j];
					this._scope.AddLocal(this, variable2);
					if (expression2.Type.IsNullableType())
					{
						this.EmitAddress(expression2, expression2.Type);
						this._ilg.Emit(OpCodes.Dup);
						this._ilg.EmitHasValue(expression2.Type);
						this._ilg.Emit(OpCodes.Ldc_I4_0);
						this._ilg.Emit(OpCodes.Ceq);
						this._ilg.Emit(OpCodes.Dup);
						this._ilg.Emit(OpCodes.Ldloc, local3);
						this._ilg.Emit(OpCodes.Or);
						this._ilg.Emit(OpCodes.Stloc, local3);
						this._ilg.Emit(OpCodes.Ldloc, local4);
						this._ilg.Emit(OpCodes.And);
						this._ilg.Emit(OpCodes.Stloc, local4);
						this._ilg.EmitGetValueOrDefault(expression2.Type);
					}
					else
					{
						this.EmitExpression(expression2);
						if (!expression2.Type.IsValueType)
						{
							this._ilg.Emit(OpCodes.Dup);
							this._ilg.Emit(OpCodes.Ldnull);
							this._ilg.Emit(OpCodes.Ceq);
							this._ilg.Emit(OpCodes.Dup);
							this._ilg.Emit(OpCodes.Ldloc, local3);
							this._ilg.Emit(OpCodes.Or);
							this._ilg.Emit(OpCodes.Stloc, local3);
							this._ilg.Emit(OpCodes.Ldloc, local4);
							this._ilg.Emit(OpCodes.And);
							this._ilg.Emit(OpCodes.Stloc, local4);
						}
						else
						{
							this._ilg.Emit(OpCodes.Ldc_I4_0);
							this._ilg.Emit(OpCodes.Stloc, local4);
						}
					}
					this._scope.EmitSet(variable2);
					j++;
				}
				this._ilg.Emit(OpCodes.Ldloc, local4);
				this._ilg.Emit(OpCodes.Brtrue, label4);
				this._ilg.Emit(OpCodes.Ldloc, local3);
				this._ilg.Emit(OpCodes.Brtrue, label5);
				this.EmitMethodCallExpression(mc);
				if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
				{
					ConstructorInfo constructor2 = resultType.GetConstructor(new Type[]
					{
						mc.Type
					});
					this._ilg.Emit(OpCodes.Newobj, constructor2);
				}
				this._ilg.Emit(OpCodes.Br_S, label3);
				this._ilg.MarkLabel(label4);
				this._ilg.EmitPrimitive(nodeType == ExpressionType.Equal);
				this._ilg.Emit(OpCodes.Br_S, label3);
				this._ilg.MarkLabel(label5);
				this._ilg.EmitPrimitive(nodeType == ExpressionType.NotEqual);
				this._ilg.MarkLabel(label3);
				this.FreeLocal(local3);
				this.FreeLocal(local4);
				return;
			}
			goto IL_35;
		}

		// Token: 0x06001534 RID: 5428 RVA: 0x00044798 File Offset: 0x00042998
		private void EmitExpression(Expression node, LambdaCompiler.CompilationFlags flags)
		{
			if (!this._guard.TryEnterOnCurrentStack())
			{
				this._guard.RunOnEmptyStack<LambdaCompiler, Expression, LambdaCompiler.CompilationFlags>(delegate(LambdaCompiler @this, Expression n, LambdaCompiler.CompilationFlags f)
				{
					@this.EmitExpression(n, f);
				}, this, node, flags);
				return;
			}
			bool flag = (flags & LambdaCompiler.CompilationFlags.EmitExpressionStartMask) == LambdaCompiler.CompilationFlags.EmitExpressionStart;
			LambdaCompiler.CompilationFlags flags2 = flag ? this.EmitExpressionStart(node) : LambdaCompiler.CompilationFlags.EmitNoExpressionStart;
			flags &= LambdaCompiler.CompilationFlags.EmitAsTailCallMask;
			switch (node.NodeType)
			{
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
			case ExpressionType.And:
			case ExpressionType.ArrayIndex:
			case ExpressionType.Divide:
			case ExpressionType.Equal:
			case ExpressionType.ExclusiveOr:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
			case ExpressionType.LeftShift:
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
			case ExpressionType.Modulo:
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
			case ExpressionType.NotEqual:
			case ExpressionType.Or:
			case ExpressionType.Power:
			case ExpressionType.RightShift:
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
				this.EmitBinaryExpression(node, flags);
				break;
			case ExpressionType.AndAlso:
				this.EmitAndAlsoBinaryExpression(node, flags);
				break;
			case ExpressionType.ArrayLength:
			case ExpressionType.Negate:
			case ExpressionType.UnaryPlus:
			case ExpressionType.NegateChecked:
			case ExpressionType.Not:
			case ExpressionType.TypeAs:
			case ExpressionType.Decrement:
			case ExpressionType.Increment:
			case ExpressionType.OnesComplement:
			case ExpressionType.IsTrue:
			case ExpressionType.IsFalse:
				this.EmitUnaryExpression(node, flags);
				break;
			case ExpressionType.Call:
				this.EmitMethodCallExpression(node, flags);
				break;
			case ExpressionType.Coalesce:
				this.EmitCoalesceBinaryExpression(node);
				break;
			case ExpressionType.Conditional:
				this.EmitConditionalExpression(node, flags);
				break;
			case ExpressionType.Constant:
				this.EmitConstantExpression(node);
				break;
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
				this.EmitConvertUnaryExpression(node, flags);
				break;
			case ExpressionType.Invoke:
				this.EmitInvocationExpression(node, flags);
				break;
			case ExpressionType.Lambda:
				this.EmitLambdaExpression(node);
				break;
			case ExpressionType.ListInit:
				this.EmitListInitExpression(node);
				break;
			case ExpressionType.MemberAccess:
				this.EmitMemberExpression(node);
				break;
			case ExpressionType.MemberInit:
				this.EmitMemberInitExpression(node);
				break;
			case ExpressionType.New:
				this.EmitNewExpression(node);
				break;
			case ExpressionType.NewArrayInit:
			case ExpressionType.NewArrayBounds:
				this.EmitNewArrayExpression(node);
				break;
			case ExpressionType.OrElse:
				this.EmitOrElseBinaryExpression(node, flags);
				break;
			case ExpressionType.Parameter:
				this.EmitParameterExpression(node);
				break;
			case ExpressionType.Quote:
				this.EmitQuoteUnaryExpression(node);
				break;
			case ExpressionType.TypeIs:
			case ExpressionType.TypeEqual:
				this.EmitTypeBinaryExpression(node);
				break;
			case ExpressionType.Assign:
				this.EmitAssignBinaryExpression(node);
				break;
			case ExpressionType.Block:
				this.EmitBlockExpression(node, flags);
				break;
			case ExpressionType.DebugInfo:
				this.EmitDebugInfoExpression(node);
				break;
			case ExpressionType.Dynamic:
				this.EmitDynamicExpression(node);
				break;
			case ExpressionType.Default:
				this.EmitDefaultExpression(node);
				break;
			case ExpressionType.Goto:
				this.EmitGotoExpression(node, flags);
				break;
			case ExpressionType.Index:
				this.EmitIndexExpression(node);
				break;
			case ExpressionType.Label:
				this.EmitLabelExpression(node, flags);
				break;
			case ExpressionType.RuntimeVariables:
				this.EmitRuntimeVariablesExpression(node);
				break;
			case ExpressionType.Loop:
				this.EmitLoopExpression(node);
				break;
			case ExpressionType.Switch:
				this.EmitSwitchExpression(node, flags);
				break;
			case ExpressionType.Throw:
				this.EmitThrowUnaryExpression(node);
				break;
			case ExpressionType.Try:
				this.EmitTryExpression(node);
				break;
			case ExpressionType.Unbox:
				this.EmitUnboxUnaryExpression(node);
				break;
			}
			if (flag)
			{
				this.EmitExpressionEnd(flags2);
			}
		}

		// Token: 0x06001535 RID: 5429 RVA: 0x00044ADB File Offset: 0x00042CDB
		private static bool IsChecked(ExpressionType op)
		{
			if (op <= ExpressionType.MultiplyChecked)
			{
				if (op != ExpressionType.AddChecked && op != ExpressionType.ConvertChecked && op != ExpressionType.MultiplyChecked)
				{
					return false;
				}
			}
			else if (op != ExpressionType.NegateChecked && op != ExpressionType.SubtractChecked && op - ExpressionType.AddAssignChecked > 2)
			{
				return false;
			}
			return true;
		}

		// Token: 0x06001536 RID: 5430 RVA: 0x00044B08 File Offset: 0x00042D08
		internal void EmitConstantArray<T>(T[] array)
		{
			if (this._method is DynamicMethod)
			{
				this.EmitConstant(array, typeof(T[]));
				return;
			}
			if (this._typeBuilder != null)
			{
				FieldBuilder field = this.CreateStaticField("ConstantArray", typeof(T[]));
				Label label = this._ilg.DefineLabel();
				this._ilg.Emit(OpCodes.Ldsfld, field);
				this._ilg.Emit(OpCodes.Ldnull);
				this._ilg.Emit(OpCodes.Bne_Un, label);
				this._ilg.EmitArray(array, this);
				this._ilg.Emit(OpCodes.Stsfld, field);
				this._ilg.MarkLabel(label);
				this._ilg.Emit(OpCodes.Ldsfld, field);
				return;
			}
			this._ilg.EmitArray(array, this);
		}

		// Token: 0x06001537 RID: 5431 RVA: 0x00044BE4 File Offset: 0x00042DE4
		private void EmitClosureCreation(LambdaCompiler inner)
		{
			bool needsClosure = inner._scope.NeedsClosure;
			bool flag = inner._boundConstants.Count > 0;
			if (!needsClosure && !flag)
			{
				this._ilg.EmitNull();
				return;
			}
			if (flag)
			{
				this._boundConstants.EmitConstant(this, inner._boundConstants.ToArray(), typeof(object[]));
			}
			else
			{
				this._ilg.EmitNull();
			}
			if (needsClosure)
			{
				this._scope.EmitGet(this._scope.NearestHoistedLocals.SelfVariable);
			}
			else
			{
				this._ilg.EmitNull();
			}
			this._ilg.EmitNew(CachedReflectionInfo.Closure_ObjectArray_ObjectArray);
		}

		// Token: 0x06001538 RID: 5432 RVA: 0x00044C8C File Offset: 0x00042E8C
		private void EmitDelegateConstruction(LambdaCompiler inner)
		{
			Type type = inner._lambda.Type;
			DynamicMethod dynamicMethod = inner._method as DynamicMethod;
			if (dynamicMethod != null)
			{
				this._boundConstants.EmitConstant(this, dynamicMethod, typeof(MethodInfo));
				this._ilg.EmitType(type);
				this.EmitClosureCreation(inner);
				this._ilg.Emit(OpCodes.Callvirt, CachedReflectionInfo.MethodInfo_CreateDelegate_Type_Object);
				this._ilg.Emit(OpCodes.Castclass, type);
				return;
			}
			this.EmitClosureCreation(inner);
			this._ilg.Emit(OpCodes.Ldftn, inner._method);
			this._ilg.Emit(OpCodes.Newobj, (ConstructorInfo)type.GetMember(".ctor")[0]);
		}

		// Token: 0x06001539 RID: 5433 RVA: 0x00044D4C File Offset: 0x00042F4C
		private void EmitDelegateConstruction(LambdaExpression lambda)
		{
			LambdaCompiler lambdaCompiler;
			if (this._method is DynamicMethod)
			{
				lambdaCompiler = new LambdaCompiler(this._tree, lambda);
			}
			else
			{
				string name = string.IsNullOrEmpty(lambda.Name) ? LambdaCompiler.GetUniqueMethodName() : lambda.Name;
				MethodBuilder method = this._typeBuilder.DefineMethod(name, MethodAttributes.Private | MethodAttributes.Static);
				lambdaCompiler = new LambdaCompiler(this._tree, lambda, method);
			}
			lambdaCompiler.EmitLambdaBody(this._scope, false, LambdaCompiler.CompilationFlags.EmitAsNoTail);
			this.EmitDelegateConstruction(lambdaCompiler);
		}

		// Token: 0x0600153A RID: 5434 RVA: 0x00044DC8 File Offset: 0x00042FC8
		private static Type[] GetParameterTypes(LambdaExpression lambda, Type firstType)
		{
			int parameterCount = lambda.ParameterCount;
			Type[] array;
			int num;
			if (firstType != null)
			{
				array = new Type[parameterCount + 1];
				array[0] = firstType;
				num = 1;
			}
			else
			{
				array = new Type[parameterCount];
				num = 0;
			}
			int i = 0;
			while (i < parameterCount)
			{
				ParameterExpression parameter = lambda.GetParameter(i);
				array[num] = (parameter.IsByRef ? parameter.Type.MakeByRefType() : parameter.Type);
				i++;
				num++;
			}
			return array;
		}

		// Token: 0x0600153B RID: 5435 RVA: 0x00044E3C File Offset: 0x0004303C
		private static string GetUniqueMethodName()
		{
			return "<ExpressionCompilerImplementationDetails>{" + Interlocked.Increment(ref LambdaCompiler.s_counter).ToString() + "}lambda_method";
		}

		// Token: 0x0600153C RID: 5436 RVA: 0x00044E6C File Offset: 0x0004306C
		private void EmitLambdaBody()
		{
			LambdaCompiler.CompilationFlags flags = this._lambda.TailCall ? LambdaCompiler.CompilationFlags.EmitAsTail : LambdaCompiler.CompilationFlags.EmitAsNoTail;
			this.EmitLambdaBody(null, false, flags);
		}

		// Token: 0x0600153D RID: 5437 RVA: 0x00044E9C File Offset: 0x0004309C
		private void EmitLambdaBody(CompilerScope parent, bool inlined, LambdaCompiler.CompilationFlags flags)
		{
			this._scope.Enter(this, parent);
			if (inlined)
			{
				for (int i = this._lambda.ParameterCount - 1; i >= 0; i--)
				{
					this._scope.EmitSet(this._lambda.GetParameter(i));
				}
			}
			flags = LambdaCompiler.UpdateEmitExpressionStartFlag(flags, LambdaCompiler.CompilationFlags.EmitExpressionStart);
			if (this._lambda.ReturnType == typeof(void))
			{
				this.EmitExpressionAsVoid(this._lambda.Body, flags);
			}
			else
			{
				this.EmitExpression(this._lambda.Body, flags);
			}
			if (!inlined)
			{
				this._ilg.Emit(OpCodes.Ret);
			}
			this._scope.Exit();
			foreach (LabelInfo labelInfo in this._labelInfo.Values)
			{
				labelInfo.ValidateFinish();
			}
		}

		// Token: 0x0600153E RID: 5438 RVA: 0x00044F9C File Offset: 0x0004319C
		private void EmitConditionalExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			ConditionalExpression conditionalExpression = (ConditionalExpression)expr;
			Label label = this._ilg.DefineLabel();
			this.EmitExpressionAndBranch(false, conditionalExpression.Test, label);
			this.EmitExpressionAsType(conditionalExpression.IfTrue, conditionalExpression.Type, flags);
			if (LambdaCompiler.NotEmpty(conditionalExpression.IfFalse))
			{
				Label label2 = this._ilg.DefineLabel();
				if ((flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask) == LambdaCompiler.CompilationFlags.EmitAsTail)
				{
					this._ilg.Emit(OpCodes.Ret);
				}
				else
				{
					this._ilg.Emit(OpCodes.Br, label2);
				}
				this._ilg.MarkLabel(label);
				this.EmitExpressionAsType(conditionalExpression.IfFalse, conditionalExpression.Type, flags);
				this._ilg.MarkLabel(label2);
				return;
			}
			this._ilg.MarkLabel(label);
		}

		// Token: 0x0600153F RID: 5439 RVA: 0x00045060 File Offset: 0x00043260
		private static bool NotEmpty(Expression node)
		{
			DefaultExpression defaultExpression = node as DefaultExpression;
			return defaultExpression == null || defaultExpression.Type != typeof(void);
		}

		// Token: 0x06001540 RID: 5440 RVA: 0x00045094 File Offset: 0x00043294
		private static bool Significant(Expression node)
		{
			BlockExpression blockExpression = node as BlockExpression;
			if (blockExpression != null)
			{
				for (int i = 0; i < blockExpression.ExpressionCount; i++)
				{
					if (LambdaCompiler.Significant(blockExpression.GetExpression(i)))
					{
						return true;
					}
				}
				return false;
			}
			return LambdaCompiler.NotEmpty(node) && !(node is DebugInfoExpression);
		}

		// Token: 0x06001541 RID: 5441 RVA: 0x000450E4 File Offset: 0x000432E4
		private void EmitCoalesceBinaryExpression(Expression expr)
		{
			BinaryExpression binaryExpression = (BinaryExpression)expr;
			if (binaryExpression.Left.Type.IsNullableType())
			{
				this.EmitNullableCoalesce(binaryExpression);
				return;
			}
			if (binaryExpression.Conversion != null)
			{
				this.EmitLambdaReferenceCoalesce(binaryExpression);
				return;
			}
			this.EmitReferenceCoalesceWithoutConversion(binaryExpression);
		}

		// Token: 0x06001542 RID: 5442 RVA: 0x0004512C File Offset: 0x0004332C
		private void EmitNullableCoalesce(BinaryExpression b)
		{
			LocalBuilder local = this.GetLocal(b.Left.Type);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(b.Left.Type);
			this._ilg.Emit(OpCodes.Brfalse, label);
			Type nonNullableType = b.Left.Type.GetNonNullableType();
			if (b.Conversion != null)
			{
				Expression parameter = b.Conversion.GetParameter(0);
				this.EmitLambdaExpression(b.Conversion);
				if (!parameter.Type.IsAssignableFrom(b.Left.Type))
				{
					this._ilg.Emit(OpCodes.Ldloca, local);
					this._ilg.EmitGetValueOrDefault(b.Left.Type);
				}
				else
				{
					this._ilg.Emit(OpCodes.Ldloc, local);
				}
				this._ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());
			}
			else if (TypeUtils.AreEquivalent(b.Type, b.Left.Type))
			{
				this._ilg.Emit(OpCodes.Ldloc, local);
			}
			else
			{
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitGetValueOrDefault(b.Left.Type);
				if (!TypeUtils.AreEquivalent(b.Type, nonNullableType))
				{
					this._ilg.EmitConvertToType(nonNullableType, b.Type, true, this);
				}
			}
			this.FreeLocal(local);
			this._ilg.Emit(OpCodes.Br, label2);
			this._ilg.MarkLabel(label);
			this.EmitExpression(b.Right);
			if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
			{
				this._ilg.EmitConvertToType(b.Right.Type, b.Type, true, this);
			}
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x06001543 RID: 5443 RVA: 0x00045348 File Offset: 0x00043548
		private void EmitLambdaReferenceCoalesce(BinaryExpression b)
		{
			LocalBuilder local = this.GetLocal(b.Left.Type);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Brtrue, label2);
			this.EmitExpression(b.Right);
			this._ilg.Emit(OpCodes.Br, label);
			this._ilg.MarkLabel(label2);
			this.EmitLambdaExpression(b.Conversion);
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			this._ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());
			this._ilg.MarkLabel(label);
		}

		// Token: 0x06001544 RID: 5444 RVA: 0x00045438 File Offset: 0x00043638
		private void EmitReferenceCoalesceWithoutConversion(BinaryExpression b)
		{
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Brtrue, label2);
			this._ilg.Emit(OpCodes.Pop);
			this.EmitExpression(b.Right);
			if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
			{
				if (b.Right.Type.IsValueType)
				{
					this._ilg.Emit(OpCodes.Box, b.Right.Type);
				}
				this._ilg.Emit(OpCodes.Castclass, b.Type);
			}
			this._ilg.Emit(OpCodes.Br_S, label);
			this._ilg.MarkLabel(label2);
			if (!TypeUtils.AreEquivalent(b.Left.Type, b.Type))
			{
				this._ilg.Emit(OpCodes.Castclass, b.Type);
			}
			this._ilg.MarkLabel(label);
		}

		// Token: 0x06001545 RID: 5445 RVA: 0x00045558 File Offset: 0x00043758
		private void EmitLiftedAndAlso(BinaryExpression b)
		{
			Type typeFromHandle = typeof(bool?);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			Label label3 = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			LocalBuilder local = this.GetLocal(typeFromHandle);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(typeFromHandle);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Not);
			this._ilg.Emit(OpCodes.And);
			this._ilg.Emit(OpCodes.Brtrue, label);
			this.EmitExpression(b.Right);
			LocalBuilder local2 = this.GetLocal(typeFromHandle);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Brtrue_S, label2);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Brtrue_S, label);
			this._ilg.MarkLabel(label2);
			this._ilg.Emit(OpCodes.Ldloc, local2);
			this.FreeLocal(local2);
			this._ilg.Emit(OpCodes.Br_S, label3);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			this._ilg.MarkLabel(label3);
		}

		// Token: 0x06001546 RID: 5446 RVA: 0x00045718 File Offset: 0x00043918
		private void EmitMethodAndAlso(BinaryExpression b, LambdaCompiler.CompilationFlags flags)
		{
			Label label = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Dup);
			MethodInfo booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_False");
			this._ilg.Emit(OpCodes.Call, booleanOperator);
			this._ilg.Emit(OpCodes.Brtrue, label);
			this.EmitExpression(b.Right);
			if ((flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask) == LambdaCompiler.CompilationFlags.EmitAsTail)
			{
				this._ilg.Emit(OpCodes.Tailcall);
			}
			this._ilg.Emit(OpCodes.Call, b.Method);
			this._ilg.MarkLabel(label);
		}

		// Token: 0x06001547 RID: 5447 RVA: 0x000457D4 File Offset: 0x000439D4
		private void EmitUnliftedAndAlso(BinaryExpression b)
		{
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpressionAndBranch(false, b.Left, label);
			this.EmitExpression(b.Right);
			this._ilg.Emit(OpCodes.Br, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldc_I4_0);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x06001548 RID: 5448 RVA: 0x0004584C File Offset: 0x00043A4C
		private void EmitAndAlsoBinaryExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			BinaryExpression binaryExpression = (BinaryExpression)expr;
			if (binaryExpression.Method != null)
			{
				if (binaryExpression.IsLiftedLogical)
				{
					this.EmitExpression(binaryExpression.ReduceUserdefinedLifted());
					return;
				}
				this.EmitMethodAndAlso(binaryExpression, flags);
				return;
			}
			else
			{
				if (binaryExpression.Left.Type == typeof(bool?))
				{
					this.EmitLiftedAndAlso(binaryExpression);
					return;
				}
				this.EmitUnliftedAndAlso(binaryExpression);
				return;
			}
		}

		// Token: 0x06001549 RID: 5449 RVA: 0x000458B8 File Offset: 0x00043AB8
		private void EmitLiftedOrElse(BinaryExpression b)
		{
			Type typeFromHandle = typeof(bool?);
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			LocalBuilder local = this.GetLocal(typeFromHandle);
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Stloc, local);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Brtrue, label);
			this.EmitExpression(b.Right);
			LocalBuilder local2 = this.GetLocal(typeFromHandle);
			this._ilg.Emit(OpCodes.Stloc, local2);
			this._ilg.Emit(OpCodes.Ldloca, local2);
			this._ilg.EmitGetValueOrDefault(typeFromHandle);
			this._ilg.Emit(OpCodes.Ldloca, local);
			this._ilg.EmitHasValue(typeFromHandle);
			this._ilg.Emit(OpCodes.Or);
			this._ilg.Emit(OpCodes.Brfalse_S, label);
			this._ilg.Emit(OpCodes.Ldloc, local2);
			this.FreeLocal(local2);
			this._ilg.Emit(OpCodes.Br_S, label2);
			this._ilg.MarkLabel(label);
			this._ilg.Emit(OpCodes.Ldloc, local);
			this.FreeLocal(local);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x0600154A RID: 5450 RVA: 0x00045A1C File Offset: 0x00043C1C
		private void EmitUnliftedOrElse(BinaryExpression b)
		{
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpressionAndBranch(false, b.Left, label);
			this._ilg.Emit(OpCodes.Ldc_I4_1);
			this._ilg.Emit(OpCodes.Br, label2);
			this._ilg.MarkLabel(label);
			this.EmitExpression(b.Right);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x0600154B RID: 5451 RVA: 0x00045A94 File Offset: 0x00043C94
		private void EmitMethodOrElse(BinaryExpression b, LambdaCompiler.CompilationFlags flags)
		{
			Label label = this._ilg.DefineLabel();
			this.EmitExpression(b.Left);
			this._ilg.Emit(OpCodes.Dup);
			MethodInfo booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_True");
			this._ilg.Emit(OpCodes.Call, booleanOperator);
			this._ilg.Emit(OpCodes.Brtrue, label);
			this.EmitExpression(b.Right);
			if ((flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask) == LambdaCompiler.CompilationFlags.EmitAsTail)
			{
				this._ilg.Emit(OpCodes.Tailcall);
			}
			this._ilg.Emit(OpCodes.Call, b.Method);
			this._ilg.MarkLabel(label);
		}

		// Token: 0x0600154C RID: 5452 RVA: 0x00045B50 File Offset: 0x00043D50
		private void EmitOrElseBinaryExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			BinaryExpression binaryExpression = (BinaryExpression)expr;
			if (binaryExpression.Method != null)
			{
				if (binaryExpression.IsLiftedLogical)
				{
					this.EmitExpression(binaryExpression.ReduceUserdefinedLifted());
					return;
				}
				this.EmitMethodOrElse(binaryExpression, flags);
				return;
			}
			else
			{
				if (binaryExpression.Left.Type == typeof(bool?))
				{
					this.EmitLiftedOrElse(binaryExpression);
					return;
				}
				this.EmitUnliftedOrElse(binaryExpression);
				return;
			}
		}

		// Token: 0x0600154D RID: 5453 RVA: 0x00045BBC File Offset: 0x00043DBC
		private void EmitExpressionAndBranch(bool branchValue, Expression node, Label label)
		{
			LambdaCompiler.CompilationFlags flags = this.EmitExpressionStart(node);
			ExpressionType nodeType = node.NodeType;
			if (nodeType <= ExpressionType.Equal)
			{
				if (nodeType != ExpressionType.AndAlso)
				{
					if (nodeType != ExpressionType.Equal)
					{
						goto IL_7F;
					}
					goto IL_6F;
				}
			}
			else
			{
				switch (nodeType)
				{
				case ExpressionType.Not:
					this.EmitBranchNot(branchValue, (UnaryExpression)node, label);
					goto IL_93;
				case ExpressionType.NotEqual:
					goto IL_6F;
				case ExpressionType.Or:
					goto IL_7F;
				case ExpressionType.OrElse:
					break;
				default:
					if (nodeType != ExpressionType.Block)
					{
						goto IL_7F;
					}
					this.EmitBranchBlock(branchValue, (BlockExpression)node, label);
					goto IL_93;
				}
			}
			this.EmitBranchLogical(branchValue, (BinaryExpression)node, label);
			goto IL_93;
			IL_6F:
			this.EmitBranchComparison(branchValue, (BinaryExpression)node, label);
			goto IL_93;
			IL_7F:
			this.EmitExpression(node, LambdaCompiler.CompilationFlags.EmitNoExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
			this.EmitBranchOp(branchValue, label);
			IL_93:
			this.EmitExpressionEnd(flags);
		}

		// Token: 0x0600154E RID: 5454 RVA: 0x00045C63 File Offset: 0x00043E63
		private void EmitBranchOp(bool branch, Label label)
		{
			this._ilg.Emit(branch ? OpCodes.Brtrue : OpCodes.Brfalse, label);
		}

		// Token: 0x0600154F RID: 5455 RVA: 0x00045C80 File Offset: 0x00043E80
		private void EmitBranchNot(bool branch, UnaryExpression node, Label label)
		{
			if (node.Method != null)
			{
				this.EmitExpression(node, LambdaCompiler.CompilationFlags.EmitNoExpressionStart | LambdaCompiler.CompilationFlags.EmitAsNoTail);
				this.EmitBranchOp(branch, label);
				return;
			}
			this.EmitExpressionAndBranch(!branch, node.Operand, label);
		}

		// Token: 0x06001550 RID: 5456 RVA: 0x00045CB8 File Offset: 0x00043EB8
		private void EmitBranchComparison(bool branch, BinaryExpression node, Label label)
		{
			bool flag = branch == (node.NodeType == ExpressionType.Equal);
			if (node.Method != null)
			{
				this.EmitBinaryMethod(node, LambdaCompiler.CompilationFlags.EmitAsNoTail);
				this.EmitBranchOp(branch, label);
				return;
			}
			if (ConstantCheck.IsNull(node.Left))
			{
				if (node.Right.Type.IsNullableType())
				{
					this.EmitAddress(node.Right, node.Right.Type);
					this._ilg.EmitHasValue(node.Right.Type);
				}
				else
				{
					this.EmitExpression(LambdaCompiler.GetEqualityOperand(node.Right));
				}
				this.EmitBranchOp(!flag, label);
				return;
			}
			if (ConstantCheck.IsNull(node.Right))
			{
				if (node.Left.Type.IsNullableType())
				{
					this.EmitAddress(node.Left, node.Left.Type);
					this._ilg.EmitHasValue(node.Left.Type);
				}
				else
				{
					this.EmitExpression(LambdaCompiler.GetEqualityOperand(node.Left));
				}
				this.EmitBranchOp(!flag, label);
				return;
			}
			if (node.Left.Type.IsNullableType() || node.Right.Type.IsNullableType())
			{
				this.EmitBinaryExpression(node);
				this.EmitBranchOp(branch, label);
				return;
			}
			this.EmitExpression(LambdaCompiler.GetEqualityOperand(node.Left));
			this.EmitExpression(LambdaCompiler.GetEqualityOperand(node.Right));
			this._ilg.Emit(flag ? OpCodes.Beq : OpCodes.Bne_Un, label);
		}

		// Token: 0x06001551 RID: 5457 RVA: 0x00045E40 File Offset: 0x00044040
		private static Expression GetEqualityOperand(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Convert)
			{
				UnaryExpression unaryExpression = (UnaryExpression)expression;
				if (TypeUtils.AreReferenceAssignable(unaryExpression.Type, unaryExpression.Operand.Type))
				{
					return unaryExpression.Operand;
				}
			}
			return expression;
		}

		// Token: 0x06001552 RID: 5458 RVA: 0x00045E80 File Offset: 0x00044080
		private void EmitBranchLogical(bool branch, BinaryExpression node, Label label)
		{
			if (node.Method != null || node.IsLifted)
			{
				this.EmitExpression(node);
				this.EmitBranchOp(branch, label);
				return;
			}
			bool flag = node.NodeType == ExpressionType.AndAlso;
			if (branch == flag)
			{
				this.EmitBranchAnd(branch, node, label);
				return;
			}
			this.EmitBranchOr(branch, node, label);
		}

		// Token: 0x06001553 RID: 5459 RVA: 0x00045ED4 File Offset: 0x000440D4
		private void EmitBranchAnd(bool branch, BinaryExpression node, Label label)
		{
			Label label2 = this._ilg.DefineLabel();
			this.EmitExpressionAndBranch(!branch, node.Left, label2);
			this.EmitExpressionAndBranch(branch, node.Right, label);
			this._ilg.MarkLabel(label2);
		}

		// Token: 0x06001554 RID: 5460 RVA: 0x00045F18 File Offset: 0x00044118
		private void EmitBranchOr(bool branch, BinaryExpression node, Label label)
		{
			this.EmitExpressionAndBranch(branch, node.Left, label);
			this.EmitExpressionAndBranch(branch, node.Right, label);
		}

		// Token: 0x06001555 RID: 5461 RVA: 0x00045F38 File Offset: 0x00044138
		private void EmitBranchBlock(bool branch, BlockExpression node, Label label)
		{
			this.EnterScope(node);
			int expressionCount = node.ExpressionCount;
			for (int i = 0; i < expressionCount - 1; i++)
			{
				this.EmitExpressionAsVoid(node.GetExpression(i));
			}
			this.EmitExpressionAndBranch(branch, node.GetExpression(expressionCount - 1), label);
			this.ExitScope(node);
		}

		// Token: 0x06001556 RID: 5462 RVA: 0x00045F86 File Offset: 0x00044186
		private void EmitBlockExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			this.Emit((BlockExpression)expr, LambdaCompiler.UpdateEmitAsTypeFlag(flags, LambdaCompiler.CompilationFlags.EmitAsDefaultType));
		}

		// Token: 0x06001557 RID: 5463 RVA: 0x00045F9C File Offset: 0x0004419C
		private void Emit(BlockExpression node, LambdaCompiler.CompilationFlags flags)
		{
			int expressionCount = node.ExpressionCount;
			if (expressionCount == 0)
			{
				return;
			}
			this.EnterScope(node);
			LambdaCompiler.CompilationFlags compilationFlags = flags & LambdaCompiler.CompilationFlags.EmitAsTypeMask;
			LambdaCompiler.CompilationFlags compilationFlags2 = flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask;
			for (int i = 0; i < expressionCount - 1; i++)
			{
				Expression expression = node.GetExpression(i);
				Expression expression2 = node.GetExpression(i + 1);
				LambdaCompiler.CompilationFlags newValue;
				if (compilationFlags2 != LambdaCompiler.CompilationFlags.EmitAsNoTail)
				{
					GotoExpression gotoExpression = expression2 as GotoExpression;
					if (gotoExpression != null && (gotoExpression.Value == null || !LambdaCompiler.Significant(gotoExpression.Value)) && this.ReferenceLabel(gotoExpression.Target).CanReturn)
					{
						newValue = LambdaCompiler.CompilationFlags.EmitAsTail;
					}
					else
					{
						newValue = LambdaCompiler.CompilationFlags.EmitAsMiddle;
					}
				}
				else
				{
					newValue = LambdaCompiler.CompilationFlags.EmitAsNoTail;
				}
				flags = LambdaCompiler.UpdateEmitAsTailCallFlag(flags, newValue);
				this.EmitExpressionAsVoid(expression, flags);
			}
			if (compilationFlags == LambdaCompiler.CompilationFlags.EmitAsVoidType || node.Type == typeof(void))
			{
				this.EmitExpressionAsVoid(node.GetExpression(expressionCount - 1), compilationFlags2);
			}
			else
			{
				this.EmitExpressionAsType(node.GetExpression(expressionCount - 1), node.Type, compilationFlags2);
			}
			this.ExitScope(node);
		}

		// Token: 0x06001558 RID: 5464 RVA: 0x000460AC File Offset: 0x000442AC
		private void EnterScope(object node)
		{
			if (LambdaCompiler.HasVariables(node) && (this._scope.MergedScopes == null || !this._scope.MergedScopes.Contains(node)))
			{
				CompilerScope compilerScope;
				if (!this._tree.Scopes.TryGetValue(node, out compilerScope))
				{
					compilerScope = new CompilerScope(node, false)
					{
						NeedsClosure = this._scope.NeedsClosure
					};
				}
				this._scope = compilerScope.Enter(this, this._scope);
			}
		}

		// Token: 0x06001559 RID: 5465 RVA: 0x00046124 File Offset: 0x00044324
		private static bool HasVariables(object node)
		{
			BlockExpression blockExpression = node as BlockExpression;
			if (blockExpression != null)
			{
				return blockExpression.Variables.Count > 0;
			}
			return ((CatchBlock)node).Variable != null;
		}

		// Token: 0x0600155A RID: 5466 RVA: 0x00046158 File Offset: 0x00044358
		private void ExitScope(object node)
		{
			if (this._scope.Node == node)
			{
				this._scope = this._scope.Exit();
			}
		}

		// Token: 0x0600155B RID: 5467 RVA: 0x0004617C File Offset: 0x0004437C
		private void EmitDefaultExpression(Expression expr)
		{
			DefaultExpression defaultExpression = (DefaultExpression)expr;
			if (defaultExpression.Type != typeof(void))
			{
				this._ilg.EmitDefault(defaultExpression.Type, this);
			}
		}

		// Token: 0x0600155C RID: 5468 RVA: 0x000461BC File Offset: 0x000443BC
		private void EmitLoopExpression(Expression expr)
		{
			LoopExpression loopExpression = (LoopExpression)expr;
			this.PushLabelBlock(LabelScopeKind.Statement);
			LabelInfo labelInfo = this.DefineLabel(loopExpression.BreakLabel);
			LabelInfo labelInfo2 = this.DefineLabel(loopExpression.ContinueLabel);
			labelInfo2.MarkWithEmptyStack();
			this.EmitExpressionAsVoid(loopExpression.Body);
			this._ilg.Emit(OpCodes.Br, labelInfo2.Label);
			this.PopLabelBlock(LabelScopeKind.Statement);
			labelInfo.MarkWithEmptyStack();
		}

		// Token: 0x0600155D RID: 5469 RVA: 0x00046224 File Offset: 0x00044424
		private void EmitSwitchExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			SwitchExpression switchExpression = (SwitchExpression)expr;
			if (switchExpression.Cases.Count == 0)
			{
				this.EmitExpressionAsVoid(switchExpression.SwitchValue);
				if (switchExpression.DefaultBody != null)
				{
					this.EmitExpressionAsType(switchExpression.DefaultBody, switchExpression.Type, flags);
				}
				return;
			}
			if (this.TryEmitSwitchInstruction(switchExpression, flags))
			{
				return;
			}
			if (this.TryEmitHashtableSwitch(switchExpression, flags))
			{
				return;
			}
			ParameterExpression parameterExpression = Expression.Parameter(switchExpression.SwitchValue.Type, "switchValue");
			ParameterExpression parameterExpression2 = Expression.Parameter(LambdaCompiler.GetTestValueType(switchExpression), "testValue");
			this._scope.AddLocal(this, parameterExpression);
			this._scope.AddLocal(this, parameterExpression2);
			this.EmitExpression(switchExpression.SwitchValue);
			this._scope.EmitSet(parameterExpression);
			Label[] array = new Label[switchExpression.Cases.Count];
			bool[] array2 = new bool[switchExpression.Cases.Count];
			int i = 0;
			int count = switchExpression.Cases.Count;
			while (i < count)
			{
				this.DefineSwitchCaseLabel(switchExpression.Cases[i], out array[i], out array2[i]);
				foreach (Expression node in switchExpression.Cases[i].TestValues)
				{
					this.EmitExpression(node);
					this._scope.EmitSet(parameterExpression2);
					this.EmitExpressionAndBranch(true, Expression.Equal(parameterExpression, parameterExpression2, false, switchExpression.Comparison), array[i]);
				}
				i++;
			}
			Label label = this._ilg.DefineLabel();
			Label @default = (switchExpression.DefaultBody == null) ? label : this._ilg.DefineLabel();
			this.EmitSwitchCases(switchExpression, array, array2, @default, label, flags);
		}

		// Token: 0x0600155E RID: 5470 RVA: 0x000463F8 File Offset: 0x000445F8
		private static Type GetTestValueType(SwitchExpression node)
		{
			if (node.Comparison == null)
			{
				return node.Cases[0].TestValues[0].Type;
			}
			Type type = node.Comparison.GetParametersCached()[1].ParameterType.GetNonRefType();
			if (node.IsLifted)
			{
				type = type.GetNullableType();
			}
			return type;
		}

		// Token: 0x0600155F RID: 5471 RVA: 0x00046458 File Offset: 0x00044658
		private static bool FitsInBucket(List<LambdaCompiler.SwitchLabel> buckets, decimal key, int count)
		{
			decimal num = key - buckets[0].Key + 1m;
			return !(num > 2147483647m) && (buckets.Count + count) * 2 > num;
		}

		// Token: 0x06001560 RID: 5472 RVA: 0x000464AC File Offset: 0x000446AC
		private static void MergeBuckets(List<List<LambdaCompiler.SwitchLabel>> buckets)
		{
			while (buckets.Count > 1)
			{
				List<LambdaCompiler.SwitchLabel> list = buckets[buckets.Count - 2];
				List<LambdaCompiler.SwitchLabel> list2 = buckets[buckets.Count - 1];
				if (!LambdaCompiler.FitsInBucket(list, list2[list2.Count - 1].Key, list2.Count))
				{
					return;
				}
				list.AddRange(list2);
				buckets.RemoveAt(buckets.Count - 1);
			}
		}

		// Token: 0x06001561 RID: 5473 RVA: 0x0004651C File Offset: 0x0004471C
		private static void AddToBuckets(List<List<LambdaCompiler.SwitchLabel>> buckets, LambdaCompiler.SwitchLabel key)
		{
			if (buckets.Count > 0)
			{
				List<LambdaCompiler.SwitchLabel> list = buckets[buckets.Count - 1];
				if (LambdaCompiler.FitsInBucket(list, key.Key, 1))
				{
					list.Add(key);
					LambdaCompiler.MergeBuckets(buckets);
					return;
				}
			}
			buckets.Add(new List<LambdaCompiler.SwitchLabel>
			{
				key
			});
		}

		// Token: 0x06001562 RID: 5474 RVA: 0x00046570 File Offset: 0x00044770
		private static bool CanOptimizeSwitchType(Type valueType)
		{
			TypeCode typeCode = valueType.GetTypeCode();
			return typeCode - TypeCode.Char <= 8;
		}

		// Token: 0x06001563 RID: 5475 RVA: 0x00046590 File Offset: 0x00044790
		private bool TryEmitSwitchInstruction(SwitchExpression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.Comparison != null)
			{
				return false;
			}
			Type type = node.SwitchValue.Type;
			if (!LambdaCompiler.CanOptimizeSwitchType(type) || !TypeUtils.AreEquivalent(type, node.Cases[0].TestValues[0].Type))
			{
				return false;
			}
			if (!node.Cases.All((SwitchCase c) => c.TestValues.All((Expression t) => t is ConstantExpression)))
			{
				return false;
			}
			Label[] array = new Label[node.Cases.Count];
			bool[] array2 = new bool[node.Cases.Count];
			HashSet<decimal> hashSet = new HashSet<decimal>();
			List<LambdaCompiler.SwitchLabel> list = new List<LambdaCompiler.SwitchLabel>();
			for (int i = 0; i < node.Cases.Count; i++)
			{
				this.DefineSwitchCaseLabel(node.Cases[i], out array[i], out array2[i]);
				foreach (Expression expression in node.Cases[i].TestValues)
				{
					ConstantExpression constantExpression = (ConstantExpression)expression;
					decimal num = LambdaCompiler.ConvertSwitchValue(constantExpression.Value);
					if (hashSet.Add(num))
					{
						list.Add(new LambdaCompiler.SwitchLabel(num, constantExpression.Value, array[i]));
					}
				}
			}
			list.Sort((LambdaCompiler.SwitchLabel x, LambdaCompiler.SwitchLabel y) => Math.Sign(x.Key - y.Key));
			List<List<LambdaCompiler.SwitchLabel>> list2 = new List<List<LambdaCompiler.SwitchLabel>>();
			foreach (LambdaCompiler.SwitchLabel key in list)
			{
				LambdaCompiler.AddToBuckets(list2, key);
			}
			LocalBuilder local = this.GetLocal(node.SwitchValue.Type);
			this.EmitExpression(node.SwitchValue);
			this._ilg.Emit(OpCodes.Stloc, local);
			Label label = this._ilg.DefineLabel();
			Label @default = (node.DefaultBody == null) ? label : this._ilg.DefineLabel();
			LambdaCompiler.SwitchInfo info = new LambdaCompiler.SwitchInfo(node, local, @default);
			this.EmitSwitchBuckets(info, list2, 0, list2.Count - 1);
			this.EmitSwitchCases(node, array, array2, @default, label, flags);
			this.FreeLocal(local);
			return true;
		}

		// Token: 0x06001564 RID: 5476 RVA: 0x0004680C File Offset: 0x00044A0C
		private static decimal ConvertSwitchValue(object value)
		{
			if (value is char)
			{
				return (int)((char)value);
			}
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06001565 RID: 5477 RVA: 0x00046830 File Offset: 0x00044A30
		private void DefineSwitchCaseLabel(SwitchCase @case, out Label label, out bool isGoto)
		{
			GotoExpression gotoExpression = @case.Body as GotoExpression;
			if (gotoExpression != null && gotoExpression.Value == null)
			{
				LabelInfo labelInfo = this.ReferenceLabel(gotoExpression.Target);
				if (labelInfo.CanBranch)
				{
					label = labelInfo.Label;
					isGoto = true;
					return;
				}
			}
			label = this._ilg.DefineLabel();
			isGoto = false;
		}

		// Token: 0x06001566 RID: 5478 RVA: 0x00046890 File Offset: 0x00044A90
		private void EmitSwitchCases(SwitchExpression node, Label[] labels, bool[] isGoto, Label @default, Label end, LambdaCompiler.CompilationFlags flags)
		{
			this._ilg.Emit(OpCodes.Br, @default);
			int i = 0;
			int count = node.Cases.Count;
			while (i < count)
			{
				if (!isGoto[i])
				{
					this._ilg.MarkLabel(labels[i]);
					this.EmitExpressionAsType(node.Cases[i].Body, node.Type, flags);
					if (node.DefaultBody != null || i < count - 1)
					{
						if ((flags & LambdaCompiler.CompilationFlags.EmitAsTailCallMask) == LambdaCompiler.CompilationFlags.EmitAsTail)
						{
							this._ilg.Emit(OpCodes.Ret);
						}
						else
						{
							this._ilg.Emit(OpCodes.Br, end);
						}
					}
				}
				i++;
			}
			if (node.DefaultBody != null)
			{
				this._ilg.MarkLabel(@default);
				this.EmitExpressionAsType(node.DefaultBody, node.Type, flags);
			}
			this._ilg.MarkLabel(end);
		}

		// Token: 0x06001567 RID: 5479 RVA: 0x00046974 File Offset: 0x00044B74
		private void EmitSwitchBuckets(LambdaCompiler.SwitchInfo info, List<List<LambdaCompiler.SwitchLabel>> buckets, int first, int last)
		{
			while (first != last)
			{
				int num = (int)(((long)first + (long)last + 1L) / 2L);
				if (first == num - 1)
				{
					this.EmitSwitchBucket(info, buckets[first]);
				}
				else
				{
					Label label = this._ilg.DefineLabel();
					this._ilg.Emit(OpCodes.Ldloc, info.Value);
					this.EmitConstant(buckets[num - 1].Last<LambdaCompiler.SwitchLabel>().Constant);
					this._ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
					this.EmitSwitchBuckets(info, buckets, first, num - 1);
					this._ilg.MarkLabel(label);
				}
				first = num;
			}
			this.EmitSwitchBucket(info, buckets[first]);
		}

		// Token: 0x06001568 RID: 5480 RVA: 0x00046A34 File Offset: 0x00044C34
		private void EmitSwitchBucket(LambdaCompiler.SwitchInfo info, List<LambdaCompiler.SwitchLabel> bucket)
		{
			if (bucket.Count == 1)
			{
				this._ilg.Emit(OpCodes.Ldloc, info.Value);
				this.EmitConstant(bucket[0].Constant);
				this._ilg.Emit(OpCodes.Beq, bucket[0].Label);
				return;
			}
			Label? label = null;
			if (info.Is64BitSwitch)
			{
				label = new Label?(this._ilg.DefineLabel());
				this._ilg.Emit(OpCodes.Ldloc, info.Value);
				this.EmitConstant(bucket.Last<LambdaCompiler.SwitchLabel>().Constant);
				this._ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label.Value);
				this._ilg.Emit(OpCodes.Ldloc, info.Value);
				this.EmitConstant(bucket[0].Constant);
				this._ilg.Emit(info.IsUnsigned ? OpCodes.Blt_Un : OpCodes.Blt, label.Value);
			}
			this._ilg.Emit(OpCodes.Ldloc, info.Value);
			decimal num = bucket[0].Key;
			if (num != 0m)
			{
				this.EmitConstant(bucket[0].Constant);
				this._ilg.Emit(OpCodes.Sub);
			}
			if (info.Is64BitSwitch)
			{
				this._ilg.Emit(OpCodes.Conv_I4);
			}
			Label[] array = new Label[(int)(bucket[bucket.Count - 1].Key - bucket[0].Key + 1m)];
			int num2 = 0;
			foreach (LambdaCompiler.SwitchLabel switchLabel in bucket)
			{
				for (;;)
				{
					decimal num3 = num;
					num = ++num3;
					if (!(num3 != switchLabel.Key))
					{
						break;
					}
					array[num2++] = info.Default;
				}
				array[num2++] = switchLabel.Label;
			}
			this._ilg.Emit(OpCodes.Switch, array);
			if (info.Is64BitSwitch)
			{
				this._ilg.MarkLabel(label.Value);
			}
		}

		// Token: 0x06001569 RID: 5481 RVA: 0x00046C98 File Offset: 0x00044E98
		private bool TryEmitHashtableSwitch(SwitchExpression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.Comparison != CachedReflectionInfo.String_op_Equality_String_String && node.Comparison != CachedReflectionInfo.String_Equals_String_String)
			{
				return false;
			}
			int num = 0;
			foreach (SwitchCase switchCase in node.Cases)
			{
				using (IEnumerator<Expression> enumerator2 = switchCase.TestValues.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (!(enumerator2.Current is ConstantExpression))
						{
							return false;
						}
						num++;
					}
				}
			}
			if (num < 7)
			{
				return false;
			}
			List<ElementInit> list = new List<ElementInit>(num);
			ArrayBuilder<SwitchCase> builder = new ArrayBuilder<SwitchCase>(node.Cases.Count);
			int value = -1;
			MethodInfo dictionaryOfStringInt32_Add_String_Int = CachedReflectionInfo.DictionaryOfStringInt32_Add_String_Int32;
			int i = 0;
			int count = node.Cases.Count;
			while (i < count)
			{
				foreach (Expression expression in node.Cases[i].TestValues)
				{
					ConstantExpression constantExpression = (ConstantExpression)expression;
					if (constantExpression.Value != null)
					{
						list.Add(Expression.ElementInit(dictionaryOfStringInt32_Add_String_Int, new TrueReadOnlyCollection<Expression>(new Expression[]
						{
							constantExpression,
							Utils.Constant(i)
						})));
					}
					else
					{
						value = i;
					}
				}
				builder.UncheckedAdd(Expression.SwitchCase(node.Cases[i].Body, new TrueReadOnlyCollection<Expression>(new Expression[]
				{
					Utils.Constant(i)
				})));
				i++;
			}
			MemberExpression memberExpression = this.CreateLazyInitializedField<Dictionary<string, int>>("dictionarySwitch");
			Expression instance = Expression.Condition(Expression.Equal(memberExpression, Expression.Constant(null, memberExpression.Type)), Expression.Assign(memberExpression, Expression.ListInit(Expression.New(CachedReflectionInfo.DictionaryOfStringInt32_Ctor_Int32, new TrueReadOnlyCollection<Expression>(new Expression[]
			{
				Utils.Constant(list.Count)
			})), list)), memberExpression);
			ParameterExpression parameterExpression = Expression.Variable(typeof(string), "switchValue");
			ParameterExpression parameterExpression2 = Expression.Variable(typeof(int), "switchIndex");
			BlockExpression node2 = Expression.Block(new TrueReadOnlyCollection<ParameterExpression>(new ParameterExpression[]
			{
				parameterExpression2,
				parameterExpression
			}), new TrueReadOnlyCollection<Expression>(new Expression[]
			{
				Expression.Assign(parameterExpression, node.SwitchValue),
				Expression.IfThenElse(Expression.Equal(parameterExpression, Expression.Constant(null, typeof(string))), Expression.Assign(parameterExpression2, Utils.Constant(value)), Expression.IfThenElse(Expression.Call(instance, "TryGetValue", null, new Expression[]
				{
					parameterExpression,
					parameterExpression2
				}), Utils.Empty, Expression.Assign(parameterExpression2, Utils.Constant(-1)))),
				Expression.Switch(node.Type, parameterExpression2, node.DefaultBody, null, builder.ToReadOnly<SwitchCase>())
			}));
			this.EmitExpression(node2, flags);
			return true;
		}

		// Token: 0x0600156A RID: 5482 RVA: 0x00046F9C File Offset: 0x0004519C
		private void CheckRethrow()
		{
			for (LabelScopeInfo labelScopeInfo = this._labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
			{
				if (labelScopeInfo.Kind == LabelScopeKind.Catch)
				{
					return;
				}
				if (labelScopeInfo.Kind == LabelScopeKind.Finally)
				{
					break;
				}
			}
			throw Error.RethrowRequiresCatch();
		}

		// Token: 0x0600156B RID: 5483 RVA: 0x00046FD4 File Offset: 0x000451D4
		private void CheckTry()
		{
			for (LabelScopeInfo labelScopeInfo = this._labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
			{
				if (labelScopeInfo.Kind == LabelScopeKind.Filter)
				{
					throw Error.TryNotAllowedInFilter();
				}
			}
		}

		// Token: 0x0600156C RID: 5484 RVA: 0x00047003 File Offset: 0x00045203
		private void EmitSaveExceptionOrPop(CatchBlock cb)
		{
			if (cb.Variable != null)
			{
				this._scope.EmitSet(cb.Variable);
				return;
			}
			this._ilg.Emit(OpCodes.Pop);
		}

		// Token: 0x0600156D RID: 5485 RVA: 0x00047030 File Offset: 0x00045230
		private void EmitTryExpression(Expression expr)
		{
			TryExpression tryExpression = (TryExpression)expr;
			this.CheckTry();
			this.PushLabelBlock(LabelScopeKind.Try);
			this._ilg.BeginExceptionBlock();
			this.EmitExpression(tryExpression.Body);
			Type type = tryExpression.Type;
			LocalBuilder local = null;
			if (type != typeof(void))
			{
				local = this.GetLocal(type);
				this._ilg.Emit(OpCodes.Stloc, local);
			}
			foreach (CatchBlock catchBlock in tryExpression.Handlers)
			{
				this.PushLabelBlock(LabelScopeKind.Catch);
				if (catchBlock.Filter == null)
				{
					this._ilg.BeginCatchBlock(catchBlock.Test);
				}
				else
				{
					this._ilg.BeginExceptFilterBlock();
				}
				this.EnterScope(catchBlock);
				this.EmitCatchStart(catchBlock);
				this.EmitExpression(catchBlock.Body);
				if (type != typeof(void))
				{
					this._ilg.Emit(OpCodes.Stloc, local);
				}
				this.ExitScope(catchBlock);
				this.PopLabelBlock(LabelScopeKind.Catch);
			}
			if (tryExpression.Finally != null || tryExpression.Fault != null)
			{
				this.PushLabelBlock(LabelScopeKind.Finally);
				if (tryExpression.Finally != null)
				{
					this._ilg.BeginFinallyBlock();
				}
				else
				{
					this._ilg.BeginFaultBlock();
				}
				this.EmitExpressionAsVoid(tryExpression.Finally ?? tryExpression.Fault);
				this._ilg.EndExceptionBlock();
				this.PopLabelBlock(LabelScopeKind.Finally);
			}
			else
			{
				this._ilg.EndExceptionBlock();
			}
			if (type != typeof(void))
			{
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
			}
			this.PopLabelBlock(LabelScopeKind.Try);
		}

		// Token: 0x0600156E RID: 5486 RVA: 0x000471F8 File Offset: 0x000453F8
		private void EmitCatchStart(CatchBlock cb)
		{
			if (cb.Filter == null)
			{
				this.EmitSaveExceptionOrPop(cb);
				return;
			}
			Label label = this._ilg.DefineLabel();
			Label label2 = this._ilg.DefineLabel();
			this._ilg.Emit(OpCodes.Isinst, cb.Test);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Brtrue, label2);
			this._ilg.Emit(OpCodes.Pop);
			this._ilg.Emit(OpCodes.Ldc_I4_0);
			this._ilg.Emit(OpCodes.Br, label);
			this._ilg.MarkLabel(label2);
			this.EmitSaveExceptionOrPop(cb);
			this.PushLabelBlock(LabelScopeKind.Filter);
			this.EmitExpression(cb.Filter);
			this.PopLabelBlock(LabelScopeKind.Filter);
			this._ilg.MarkLabel(label);
			this._ilg.BeginCatchBlock(null);
			this._ilg.Emit(OpCodes.Pop);
		}

		// Token: 0x0600156F RID: 5487 RVA: 0x000472EA File Offset: 0x000454EA
		private void EmitQuoteUnaryExpression(Expression expr)
		{
			this.EmitQuote((UnaryExpression)expr);
		}

		// Token: 0x06001570 RID: 5488 RVA: 0x000472F8 File Offset: 0x000454F8
		private void EmitQuote(UnaryExpression quote)
		{
			this.EmitConstant(quote.Operand, quote.Type);
			if (this._scope.NearestHoistedLocals != null)
			{
				this.EmitConstant(this._scope.NearestHoistedLocals, typeof(object));
				this._scope.EmitGet(this._scope.NearestHoistedLocals.SelfVariable);
				this._ilg.Emit(OpCodes.Call, CachedReflectionInfo.RuntimeOps_Quote);
				this._ilg.Emit(OpCodes.Castclass, quote.Type);
			}
		}

		// Token: 0x06001571 RID: 5489 RVA: 0x00047385 File Offset: 0x00045585
		private void EmitThrowUnaryExpression(Expression expr)
		{
			this.EmitThrow((UnaryExpression)expr, LambdaCompiler.CompilationFlags.EmitAsDefaultType);
		}

		// Token: 0x06001572 RID: 5490 RVA: 0x00047398 File Offset: 0x00045598
		private void EmitThrow(UnaryExpression expr, LambdaCompiler.CompilationFlags flags)
		{
			if (expr.Operand == null)
			{
				this.CheckRethrow();
				this._ilg.Emit(OpCodes.Rethrow);
			}
			else
			{
				this.EmitExpression(expr.Operand);
				this._ilg.Emit(OpCodes.Throw);
			}
			this.EmitUnreachable(expr, flags);
		}

		// Token: 0x06001573 RID: 5491 RVA: 0x000473E9 File Offset: 0x000455E9
		private void EmitUnaryExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			this.EmitUnary((UnaryExpression)expr, flags);
		}

		// Token: 0x06001574 RID: 5492 RVA: 0x000473F8 File Offset: 0x000455F8
		private void EmitUnary(UnaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.Method != null)
			{
				this.EmitUnaryMethod(node, flags);
				return;
			}
			if (node.NodeType != ExpressionType.NegateChecked || !node.Operand.Type.IsInteger())
			{
				this.EmitExpression(node.Operand);
				this.EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
				return;
			}
			Type type = node.Type;
			if (type.IsNullableType())
			{
				Label label = this._ilg.DefineLabel();
				Label label2 = this._ilg.DefineLabel();
				this.EmitExpression(node.Operand);
				LocalBuilder local = this.GetLocal(type);
				this._ilg.Emit(OpCodes.Stloc, local);
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitGetValueOrDefault(type);
				this._ilg.Emit(OpCodes.Brfalse_S, label);
				Type nonNullableType = type.GetNonNullableType();
				this._ilg.EmitDefault(nonNullableType, null);
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitGetValueOrDefault(type);
				this.EmitBinaryOperator(ExpressionType.SubtractChecked, nonNullableType, nonNullableType, nonNullableType, false);
				this._ilg.Emit(OpCodes.Newobj, type.GetConstructor(new Type[]
				{
					nonNullableType
				}));
				this._ilg.Emit(OpCodes.Br_S, label2);
				this._ilg.MarkLabel(label);
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
				this._ilg.MarkLabel(label2);
				return;
			}
			this._ilg.EmitDefault(type, null);
			this.EmitExpression(node.Operand);
			this.EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type, false);
		}

		// Token: 0x06001575 RID: 5493 RVA: 0x000475B0 File Offset: 0x000457B0
		private void EmitUnaryOperator(ExpressionType op, Type operandType, Type resultType)
		{
			bool flag = operandType.IsNullableType();
			if (op == ExpressionType.ArrayLength)
			{
				this._ilg.Emit(OpCodes.Ldlen);
				return;
			}
			if (!flag)
			{
				if (op <= ExpressionType.TypeAs)
				{
					switch (op)
					{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
						this._ilg.Emit(OpCodes.Neg);
						return;
					case ExpressionType.UnaryPlus:
						return;
					case ExpressionType.New:
					case ExpressionType.NewArrayInit:
					case ExpressionType.NewArrayBounds:
						goto IL_2EB;
					case ExpressionType.Not:
						if (operandType == typeof(bool))
						{
							this._ilg.Emit(OpCodes.Ldc_I4_0);
							this._ilg.Emit(OpCodes.Ceq);
							return;
						}
						break;
					default:
						if (op != ExpressionType.TypeAs)
						{
							goto IL_2EB;
						}
						if (operandType != resultType)
						{
							if (operandType.IsValueType)
							{
								this._ilg.Emit(OpCodes.Box, operandType);
							}
							this._ilg.Emit(OpCodes.Isinst, resultType);
							if (resultType.IsNullableType())
							{
								this._ilg.Emit(OpCodes.Unbox_Any, resultType);
							}
						}
						return;
					}
				}
				else
				{
					if (op == ExpressionType.Decrement)
					{
						this.EmitConstantOne(resultType);
						this._ilg.Emit(OpCodes.Sub);
						goto IL_2EB;
					}
					if (op == ExpressionType.Increment)
					{
						this.EmitConstantOne(resultType);
						this._ilg.Emit(OpCodes.Add);
						goto IL_2EB;
					}
					switch (op)
					{
					case ExpressionType.OnesComplement:
						break;
					case ExpressionType.IsTrue:
						this._ilg.Emit(OpCodes.Ldc_I4_1);
						this._ilg.Emit(OpCodes.Ceq);
						return;
					case ExpressionType.IsFalse:
						this._ilg.Emit(OpCodes.Ldc_I4_0);
						this._ilg.Emit(OpCodes.Ceq);
						return;
					default:
						goto IL_2EB;
					}
				}
				this._ilg.Emit(OpCodes.Not);
				if (!operandType.IsUnsigned())
				{
					return;
				}
				IL_2EB:
				this.EmitConvertArithmeticResult(op, resultType);
				return;
			}
			if (op == ExpressionType.UnaryPlus)
			{
				return;
			}
			if (op != ExpressionType.TypeAs)
			{
				Label label = this._ilg.DefineLabel();
				Label label2 = this._ilg.DefineLabel();
				LocalBuilder local = this.GetLocal(operandType);
				this._ilg.Emit(OpCodes.Stloc, local);
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitHasValue(operandType);
				this._ilg.Emit(OpCodes.Brfalse_S, label);
				this._ilg.Emit(OpCodes.Ldloca, local);
				this._ilg.EmitGetValueOrDefault(operandType);
				Type nonNullableType = resultType.GetNonNullableType();
				this.EmitUnaryOperator(op, nonNullableType, nonNullableType);
				ConstructorInfo constructor = resultType.GetConstructor(new Type[]
				{
					nonNullableType
				});
				this._ilg.Emit(OpCodes.Newobj, constructor);
				this._ilg.Emit(OpCodes.Br_S, label2);
				this._ilg.MarkLabel(label);
				this._ilg.Emit(OpCodes.Ldloc, local);
				this.FreeLocal(local);
				this._ilg.MarkLabel(label2);
				return;
			}
			if (operandType != resultType)
			{
				this._ilg.Emit(OpCodes.Box, operandType);
				this._ilg.Emit(OpCodes.Isinst, resultType);
				if (resultType.IsNullableType())
				{
					this._ilg.Emit(OpCodes.Unbox_Any, resultType);
				}
			}
		}

		// Token: 0x06001576 RID: 5494 RVA: 0x000478B0 File Offset: 0x00045AB0
		private void EmitConstantOne(Type type)
		{
			switch (type.GetTypeCode())
			{
			case TypeCode.Int64:
			case TypeCode.UInt64:
				this._ilg.Emit(OpCodes.Ldc_I4_1);
				this._ilg.Emit(OpCodes.Conv_I8);
				return;
			case TypeCode.Single:
				this._ilg.Emit(OpCodes.Ldc_R4, 1f);
				return;
			case TypeCode.Double:
				this._ilg.Emit(OpCodes.Ldc_R8, 1.0);
				return;
			default:
				this._ilg.Emit(OpCodes.Ldc_I4_1);
				return;
			}
		}

		// Token: 0x06001577 RID: 5495 RVA: 0x00047940 File Offset: 0x00045B40
		private void EmitUnboxUnaryExpression(Expression expr)
		{
			UnaryExpression unaryExpression = (UnaryExpression)expr;
			this.EmitExpression(unaryExpression.Operand);
			this._ilg.Emit(OpCodes.Unbox_Any, unaryExpression.Type);
		}

		// Token: 0x06001578 RID: 5496 RVA: 0x00047976 File Offset: 0x00045B76
		private void EmitConvertUnaryExpression(Expression expr, LambdaCompiler.CompilationFlags flags)
		{
			this.EmitConvert((UnaryExpression)expr, flags);
		}

		// Token: 0x06001579 RID: 5497 RVA: 0x00047988 File Offset: 0x00045B88
		private void EmitConvert(UnaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.Method != null)
			{
				if (!node.IsLifted || (node.Type.IsValueType && node.Operand.Type.IsValueType))
				{
					this.EmitUnaryMethod(node, flags);
					return;
				}
				Type type = node.Method.GetParametersCached()[0].ParameterType;
				if (type.IsByRef)
				{
					type = type.GetElementType();
				}
				UnaryExpression arg = Expression.Convert(node.Operand, type);
				node = Expression.Convert(Expression.Call(node.Method, arg), node.Type);
			}
			if (node.Type == typeof(void))
			{
				this.EmitExpressionAsVoid(node.Operand, flags);
				return;
			}
			if (TypeUtils.AreEquivalent(node.Operand.Type, node.Type))
			{
				this.EmitExpression(node.Operand, flags);
				return;
			}
			this.EmitExpression(node.Operand);
			this._ilg.EmitConvertToType(node.Operand.Type, node.Type, node.NodeType == ExpressionType.ConvertChecked, this);
		}

		// Token: 0x0600157A RID: 5498 RVA: 0x00047A9C File Offset: 0x00045C9C
		private void EmitUnaryMethod(UnaryExpression node, LambdaCompiler.CompilationFlags flags)
		{
			if (node.IsLifted)
			{
				ParameterExpression parameterExpression = Expression.Variable(node.Operand.Type.GetNonNullableType(), null);
				MethodCallExpression methodCallExpression = Expression.Call(node.Method, parameterExpression);
				Type nullableType = methodCallExpression.Type.GetNullableType();
				this.EmitLift(node.NodeType, nullableType, methodCallExpression, new ParameterExpression[]
				{
					parameterExpression
				}, new Expression[]
				{
					node.Operand
				});
				this._ilg.EmitConvertToType(nullableType, node.Type, false, this);
				return;
			}
			this.EmitMethodCallExpression(Expression.Call(node.Method, node.Operand), flags);
		}

		// Token: 0x0600157B RID: 5499 RVA: 0x00047B38 File Offset: 0x00045D38
		private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda)
		{
			Type[] parameterTypes = LambdaCompiler.GetParameterTypes(lambda, typeof(Closure));
			DynamicMethod dynamicMethod = new DynamicMethod(lambda.Name ?? "lambda_method", lambda.ReturnType, parameterTypes, true);
			this._tree = tree;
			this._lambda = lambda;
			this._method = dynamicMethod;
			this._ilg = dynamicMethod.GetILGenerator();
			this._hasClosureArgument = true;
			this._scope = tree.Scopes[lambda];
			this._boundConstants = tree.Constants[lambda];
			this.InitializeMethod();
		}

		// Token: 0x0600157C RID: 5500 RVA: 0x00047BFC File Offset: 0x00045DFC
		private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda, MethodBuilder method)
		{
			CompilerScope compilerScope = tree.Scopes[lambda];
			bool needsClosure = compilerScope.NeedsClosure;
			Type[] parameterTypes = LambdaCompiler.GetParameterTypes(lambda, needsClosure ? typeof(Closure) : null);
			method.SetReturnType(lambda.ReturnType);
			method.SetParameters(parameterTypes);
			ReadOnlyCollection<ParameterExpression> parameters = lambda.Parameters;
			int num = needsClosure ? 2 : 1;
			int i = 0;
			int count = parameters.Count;
			while (i < count)
			{
				method.DefineParameter(i + num, ParameterAttributes.None, parameters[i].Name);
				i++;
			}
			this._tree = tree;
			this._lambda = lambda;
			this._typeBuilder = (TypeBuilder)method.DeclaringType;
			this._method = method;
			this._hasClosureArgument = needsClosure;
			this._ilg = method.GetILGenerator();
			this._scope = compilerScope;
			this._boundConstants = tree.Constants[lambda];
			this.InitializeMethod();
		}

		// Token: 0x0600157D RID: 5501 RVA: 0x00047D18 File Offset: 0x00045F18
		private LambdaCompiler(LambdaCompiler parent, LambdaExpression lambda, InvocationExpression invocation)
		{
			this._tree = parent._tree;
			this._lambda = lambda;
			this._method = parent._method;
			this._ilg = parent._ilg;
			this._hasClosureArgument = parent._hasClosureArgument;
			this._typeBuilder = parent._typeBuilder;
			this._scope = this._tree.Scopes[invocation];
			this._boundConstants = parent._boundConstants;
		}

		// Token: 0x0600157E RID: 5502 RVA: 0x00047DBF File Offset: 0x00045FBF
		private void InitializeMethod()
		{
			this.AddReturnLabel(this._lambda);
			this._boundConstants.EmitCacheConstants(this);
		}

		// Token: 0x170003BD RID: 957
		// (get) Token: 0x0600157F RID: 5503 RVA: 0x00047DD9 File Offset: 0x00045FD9
		internal ILGenerator IL
		{
			get
			{
				return this._ilg;
			}
		}

		// Token: 0x170003BE RID: 958
		// (get) Token: 0x06001580 RID: 5504 RVA: 0x00047DE1 File Offset: 0x00045FE1
		internal IParameterProvider Parameters
		{
			get
			{
				return this._lambda;
			}
		}

		// Token: 0x170003BF RID: 959
		// (get) Token: 0x06001581 RID: 5505 RVA: 0x00047DE9 File Offset: 0x00045FE9
		internal bool CanEmitBoundConstants
		{
			get
			{
				return this._method is DynamicMethod;
			}
		}

		// Token: 0x06001582 RID: 5506 RVA: 0x00047DF9 File Offset: 0x00045FF9
		internal static Delegate Compile(LambdaExpression lambda)
		{
			lambda.ValidateArgumentCount();
			LambdaCompiler lambdaCompiler = new LambdaCompiler(LambdaCompiler.AnalyzeLambda(ref lambda), lambda);
			lambdaCompiler.EmitLambdaBody();
			return lambdaCompiler.CreateDelegate();
		}

		// Token: 0x06001583 RID: 5507 RVA: 0x00047E19 File Offset: 0x00046019
		internal static void Compile(LambdaExpression lambda, MethodBuilder method)
		{
			new LambdaCompiler(LambdaCompiler.AnalyzeLambda(ref lambda), lambda, method).EmitLambdaBody();
		}

		// Token: 0x06001584 RID: 5508 RVA: 0x00047E2E File Offset: 0x0004602E
		private static AnalyzedTree AnalyzeLambda(ref LambdaExpression lambda)
		{
			lambda = StackSpiller.AnalyzeLambda(lambda);
			return VariableBinder.Bind(lambda);
		}

		// Token: 0x06001585 RID: 5509 RVA: 0x00047E40 File Offset: 0x00046040
		public LocalBuilder GetLocal(Type type)
		{
			return this._freeLocals.TryPop(type) ?? this._ilg.DeclareLocal(type);
		}

		// Token: 0x06001586 RID: 5510 RVA: 0x00047E5E File Offset: 0x0004605E
		public void FreeLocal(LocalBuilder local)
		{
			this._freeLocals.Push(local.LocalType, local);
		}

		// Token: 0x06001587 RID: 5511 RVA: 0x00047E72 File Offset: 0x00046072
		internal int GetLambdaArgument(int index)
		{
			return index + (this._hasClosureArgument ? 1 : 0) + (this._method.IsStatic ? 0 : 1);
		}

		// Token: 0x06001588 RID: 5512 RVA: 0x00047E94 File Offset: 0x00046094
		internal void EmitLambdaArgument(int index)
		{
			this._ilg.EmitLoadArg(this.GetLambdaArgument(index));
		}

		// Token: 0x06001589 RID: 5513 RVA: 0x00047EA8 File Offset: 0x000460A8
		internal void EmitClosureArgument()
		{
			this._ilg.EmitLoadArg(0);
		}

		// Token: 0x0600158A RID: 5514 RVA: 0x00047EB6 File Offset: 0x000460B6
		private Delegate CreateDelegate()
		{
			return this._method.CreateDelegate(this._lambda.Type, new Closure(this._boundConstants.ToArray(), null));
		}

		// Token: 0x0600158B RID: 5515 RVA: 0x00047EE0 File Offset: 0x000460E0
		private FieldBuilder CreateStaticField(string name, Type type)
		{
			return this._typeBuilder.DefineField("<ExpressionCompilerImplementationDetails>{" + Interlocked.Increment(ref LambdaCompiler.s_counter).ToString() + "}" + name, type, FieldAttributes.Private | FieldAttributes.Static);
		}

		// Token: 0x0600158C RID: 5516 RVA: 0x00047F20 File Offset: 0x00046120
		private MemberExpression CreateLazyInitializedField<T>(string name)
		{
			if (this._method is DynamicMethod)
			{
				return Expression.Field(Expression.Constant(new StrongBox<T>(default(T))), "Value");
			}
			return Expression.Field(null, this.CreateStaticField(name, typeof(T)));
		}

		// Token: 0x04000ADE RID: 2782
		private readonly StackGuard _guard = new StackGuard();

		// Token: 0x04000ADF RID: 2783
		private static int s_counter;

		// Token: 0x04000AE0 RID: 2784
		private readonly AnalyzedTree _tree;

		// Token: 0x04000AE1 RID: 2785
		private readonly ILGenerator _ilg;

		// Token: 0x04000AE2 RID: 2786
		private readonly TypeBuilder _typeBuilder;

		// Token: 0x04000AE3 RID: 2787
		private readonly MethodInfo _method;

		// Token: 0x04000AE4 RID: 2788
		private LabelScopeInfo _labelBlock = new LabelScopeInfo(null, LabelScopeKind.Lambda);

		// Token: 0x04000AE5 RID: 2789
		private readonly Dictionary<LabelTarget, LabelInfo> _labelInfo = new Dictionary<LabelTarget, LabelInfo>();

		// Token: 0x04000AE6 RID: 2790
		private CompilerScope _scope;

		// Token: 0x04000AE7 RID: 2791
		private readonly LambdaExpression _lambda;

		// Token: 0x04000AE8 RID: 2792
		private readonly bool _hasClosureArgument;

		// Token: 0x04000AE9 RID: 2793
		private readonly BoundConstants _boundConstants;

		// Token: 0x04000AEA RID: 2794
		private readonly KeyedStack<Type, LocalBuilder> _freeLocals = new KeyedStack<Type, LocalBuilder>();

		// Token: 0x020002BD RID: 701
		[Flags]
		internal enum CompilationFlags
		{
			// Token: 0x04000AEC RID: 2796
			EmitExpressionStart = 1,
			// Token: 0x04000AED RID: 2797
			EmitNoExpressionStart = 2,
			// Token: 0x04000AEE RID: 2798
			EmitAsDefaultType = 16,
			// Token: 0x04000AEF RID: 2799
			EmitAsVoidType = 32,
			// Token: 0x04000AF0 RID: 2800
			EmitAsTail = 256,
			// Token: 0x04000AF1 RID: 2801
			EmitAsMiddle = 512,
			// Token: 0x04000AF2 RID: 2802
			EmitAsNoTail = 1024,
			// Token: 0x04000AF3 RID: 2803
			EmitExpressionStartMask = 15,
			// Token: 0x04000AF4 RID: 2804
			EmitAsTypeMask = 240,
			// Token: 0x04000AF5 RID: 2805
			EmitAsTailCallMask = 3840
		}

		// Token: 0x020002BE RID: 702
		private sealed class SwitchLabel
		{
			// Token: 0x0600158D RID: 5517 RVA: 0x00047F6F File Offset: 0x0004616F
			internal SwitchLabel(decimal key, object constant, Label label)
			{
				this.Key = key;
				this.Constant = constant;
				this.Label = label;
			}

			// Token: 0x04000AF6 RID: 2806
			internal readonly decimal Key;

			// Token: 0x04000AF7 RID: 2807
			internal readonly Label Label;

			// Token: 0x04000AF8 RID: 2808
			internal readonly object Constant;
		}

		// Token: 0x020002BF RID: 703
		private sealed class SwitchInfo
		{
			// Token: 0x0600158E RID: 5518 RVA: 0x00047F8C File Offset: 0x0004618C
			internal SwitchInfo(SwitchExpression node, LocalBuilder value, Label @default)
			{
				this.Node = node;
				this.Value = value;
				this.Default = @default;
				this.Type = this.Node.SwitchValue.Type;
				this.IsUnsigned = this.Type.IsUnsigned();
				TypeCode typeCode = this.Type.GetTypeCode();
				this.Is64BitSwitch = (typeCode == TypeCode.UInt64 || typeCode == TypeCode.Int64);
			}

			// Token: 0x04000AF9 RID: 2809
			internal readonly SwitchExpression Node;

			// Token: 0x04000AFA RID: 2810
			internal readonly LocalBuilder Value;

			// Token: 0x04000AFB RID: 2811
			internal readonly Label Default;

			// Token: 0x04000AFC RID: 2812
			internal readonly Type Type;

			// Token: 0x04000AFD RID: 2813
			internal readonly bool IsUnsigned;

			// Token: 0x04000AFE RID: 2814
			internal readonly bool Is64BitSwitch;
		}

		// Token: 0x020002C0 RID: 704
		// (Invoke) Token: 0x06001590 RID: 5520
		private delegate void WriteBack(LambdaCompiler compiler);

		// Token: 0x020002C1 RID: 705
		[CompilerGenerated]
		private sealed class <>c__DisplayClass12_0
		{
			// Token: 0x06001593 RID: 5523 RVA: 0x00002162 File Offset: 0x00000362
			public <>c__DisplayClass12_0()
			{
			}

			// Token: 0x06001594 RID: 5524 RVA: 0x00047FFC File Offset: 0x000461FC
			internal void <AddressOfWriteBackCore>b__0(LambdaCompiler @this)
			{
				if (this.instanceLocal != null)
				{
					@this._ilg.Emit(OpCodes.Ldloc, this.instanceLocal);
					@this.FreeLocal(this.instanceLocal);
				}
				@this._ilg.Emit(OpCodes.Ldloc, this.valueLocal);
				@this.FreeLocal(this.valueLocal);
				LocalBuilder localBuilder = this.instanceLocal;
				@this.EmitCall((localBuilder != null) ? localBuilder.LocalType : null, this.pi.GetSetMethod(true));
			}

			// Token: 0x04000AFF RID: 2815
			public LocalBuilder instanceLocal;

			// Token: 0x04000B00 RID: 2816
			public LocalBuilder valueLocal;

			// Token: 0x04000B01 RID: 2817
			public PropertyInfo pi;
		}

		// Token: 0x020002C2 RID: 706
		[CompilerGenerated]
		private sealed class <>c__DisplayClass14_0
		{
			// Token: 0x06001595 RID: 5525 RVA: 0x00002162 File Offset: 0x00000362
			public <>c__DisplayClass14_0()
			{
			}

			// Token: 0x06001596 RID: 5526 RVA: 0x0004807C File Offset: 0x0004627C
			internal void <AddressOfWriteBackCore>b__0(LambdaCompiler @this)
			{
				if (this.instanceLocal != null)
				{
					@this._ilg.Emit(OpCodes.Ldloc, this.instanceLocal);
					@this.FreeLocal(this.instanceLocal);
				}
				foreach (LocalBuilder local in this.args)
				{
					@this._ilg.Emit(OpCodes.Ldloc, local);
					@this.FreeLocal(local);
				}
				@this._ilg.Emit(OpCodes.Ldloc, this.valueLocal);
				@this.FreeLocal(this.valueLocal);
				IndexExpression indexExpression = this.node;
				LocalBuilder localBuilder = this.instanceLocal;
				@this.EmitSetIndexCall(indexExpression, (localBuilder != null) ? localBuilder.LocalType : null);
			}

			// Token: 0x04000B02 RID: 2818
			public LocalBuilder instanceLocal;

			// Token: 0x04000B03 RID: 2819
			public LocalBuilder[] args;

			// Token: 0x04000B04 RID: 2820
			public LocalBuilder valueLocal;

			// Token: 0x04000B05 RID: 2821
			public IndexExpression node;
		}

		// Token: 0x020002C3 RID: 707
		[CompilerGenerated]
		[Serializable]
		private sealed class <>c
		{
			// Token: 0x06001597 RID: 5527 RVA: 0x00048124 File Offset: 0x00046324
			// Note: this type is marked as 'beforefieldinit'.
			static <>c()
			{
			}

			// Token: 0x06001598 RID: 5528 RVA: 0x00002162 File Offset: 0x00000362
			public <>c()
			{
			}

			// Token: 0x06001599 RID: 5529 RVA: 0x00048130 File Offset: 0x00046330
			internal void <EmitExpression>b__102_0(LambdaCompiler @this, Expression n, LambdaCompiler.CompilationFlags f)
			{
				@this.EmitExpression(n, f);
			}

			// Token: 0x0600159A RID: 5530 RVA: 0x0004813A File Offset: 0x0004633A
			internal bool <TryEmitSwitchInstruction>b__152_0(SwitchCase c)
			{
				return c.TestValues.All((Expression t) => t is ConstantExpression);
			}

			// Token: 0x0600159B RID: 5531 RVA: 0x00048166 File Offset: 0x00046366
			internal bool <TryEmitSwitchInstruction>b__152_2(Expression t)
			{
				return t is ConstantExpression;
			}

			// Token: 0x0600159C RID: 5532 RVA: 0x00048171 File Offset: 0x00046371
			internal int <TryEmitSwitchInstruction>b__152_1(LambdaCompiler.SwitchLabel x, LambdaCompiler.SwitchLabel y)
			{
				return Math.Sign(x.Key - y.Key);
			}

			// Token: 0x04000B06 RID: 2822
			public static readonly LambdaCompiler.<>c <>9 = new LambdaCompiler.<>c();

			// Token: 0x04000B07 RID: 2823
			public static Action<LambdaCompiler, Expression, LambdaCompiler.CompilationFlags> <>9__102_0;

			// Token: 0x04000B08 RID: 2824
			public static Func<Expression, bool> <>9__152_2;

			// Token: 0x04000B09 RID: 2825
			public static Func<SwitchCase, bool> <>9__152_0;

			// Token: 0x04000B0A RID: 2826
			public static Comparison<LambdaCompiler.SwitchLabel> <>9__152_1;
		}
	}
}
