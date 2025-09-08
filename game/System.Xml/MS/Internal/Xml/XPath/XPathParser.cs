﻿using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
	// Token: 0x0200065F RID: 1631
	internal class XPathParser
	{
		// Token: 0x06004204 RID: 16900 RVA: 0x00168FD3 File Offset: 0x001671D3
		private XPathParser(XPathScanner scanner)
		{
			this._scanner = scanner;
		}

		// Token: 0x06004205 RID: 16901 RVA: 0x00168FE4 File Offset: 0x001671E4
		public static AstNode ParseXPathExpression(string xpathExpression)
		{
			XPathScanner xpathScanner = new XPathScanner(xpathExpression);
			AstNode result = new XPathParser(xpathScanner).ParseExpression(null);
			if (xpathScanner.Kind != XPathScanner.LexKind.Eof)
			{
				throw XPathException.Create("'{0}' has an invalid token.", xpathScanner.SourceText);
			}
			return result;
		}

		// Token: 0x06004206 RID: 16902 RVA: 0x00169020 File Offset: 0x00167220
		public static AstNode ParseXPathPattern(string xpathPattern)
		{
			XPathScanner xpathScanner = new XPathScanner(xpathPattern);
			AstNode result = new XPathParser(xpathScanner).ParsePattern();
			if (xpathScanner.Kind != XPathScanner.LexKind.Eof)
			{
				throw XPathException.Create("'{0}' has an invalid token.", xpathScanner.SourceText);
			}
			return result;
		}

		// Token: 0x06004207 RID: 16903 RVA: 0x0016905C File Offset: 0x0016725C
		private AstNode ParseExpression(AstNode qyInput)
		{
			int num = this._parseDepth + 1;
			this._parseDepth = num;
			if (num > 200)
			{
				throw XPathException.Create("The xpath query is too complex.");
			}
			AstNode result = this.ParseOrExpr(qyInput);
			this._parseDepth--;
			return result;
		}

		// Token: 0x06004208 RID: 16904 RVA: 0x001690A4 File Offset: 0x001672A4
		private AstNode ParseOrExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseAndExpr(qyInput);
			while (this.TestOp("or"))
			{
				this.NextLex();
				astNode = new Operator(Operator.Op.OR, astNode, this.ParseAndExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x06004209 RID: 16905 RVA: 0x001690E0 File Offset: 0x001672E0
		private AstNode ParseAndExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseEqualityExpr(qyInput);
			while (this.TestOp("and"))
			{
				this.NextLex();
				astNode = new Operator(Operator.Op.AND, astNode, this.ParseEqualityExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x0600420A RID: 16906 RVA: 0x0016911C File Offset: 0x0016731C
		private AstNode ParseEqualityExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseRelationalExpr(qyInput);
			for (;;)
			{
				Operator.Op op = (this._scanner.Kind == XPathScanner.LexKind.Eq) ? Operator.Op.EQ : ((this._scanner.Kind == XPathScanner.LexKind.Ne) ? Operator.Op.NE : Operator.Op.INVALID);
				if (op == Operator.Op.INVALID)
				{
					break;
				}
				this.NextLex();
				astNode = new Operator(op, astNode, this.ParseRelationalExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x0600420B RID: 16907 RVA: 0x00169174 File Offset: 0x00167374
		private AstNode ParseRelationalExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseAdditiveExpr(qyInput);
			for (;;)
			{
				Operator.Op op = (this._scanner.Kind == XPathScanner.LexKind.Lt) ? Operator.Op.LT : ((this._scanner.Kind == XPathScanner.LexKind.Le) ? Operator.Op.LE : ((this._scanner.Kind == XPathScanner.LexKind.Gt) ? Operator.Op.GT : ((this._scanner.Kind == XPathScanner.LexKind.Ge) ? Operator.Op.GE : Operator.Op.INVALID)));
				if (op == Operator.Op.INVALID)
				{
					break;
				}
				this.NextLex();
				astNode = new Operator(op, astNode, this.ParseAdditiveExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x0600420C RID: 16908 RVA: 0x001691F0 File Offset: 0x001673F0
		private AstNode ParseAdditiveExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseMultiplicativeExpr(qyInput);
			for (;;)
			{
				Operator.Op op = (this._scanner.Kind == XPathScanner.LexKind.Plus) ? Operator.Op.PLUS : ((this._scanner.Kind == XPathScanner.LexKind.Minus) ? Operator.Op.MINUS : Operator.Op.INVALID);
				if (op == Operator.Op.INVALID)
				{
					break;
				}
				this.NextLex();
				astNode = new Operator(op, astNode, this.ParseMultiplicativeExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x0600420D RID: 16909 RVA: 0x00169248 File Offset: 0x00167448
		private AstNode ParseMultiplicativeExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParseUnaryExpr(qyInput);
			for (;;)
			{
				Operator.Op op = (this._scanner.Kind == XPathScanner.LexKind.Star) ? Operator.Op.MUL : (this.TestOp("div") ? Operator.Op.DIV : (this.TestOp("mod") ? Operator.Op.MOD : Operator.Op.INVALID));
				if (op == Operator.Op.INVALID)
				{
					break;
				}
				this.NextLex();
				astNode = new Operator(op, astNode, this.ParseUnaryExpr(qyInput));
			}
			return astNode;
		}

		// Token: 0x0600420E RID: 16910 RVA: 0x001692B0 File Offset: 0x001674B0
		private AstNode ParseUnaryExpr(AstNode qyInput)
		{
			bool flag = false;
			while (this._scanner.Kind == XPathScanner.LexKind.Minus)
			{
				this.NextLex();
				flag = !flag;
			}
			if (flag)
			{
				return new Operator(Operator.Op.MUL, this.ParseUnionExpr(qyInput), new Operand(-1.0));
			}
			return this.ParseUnionExpr(qyInput);
		}

		// Token: 0x0600420F RID: 16911 RVA: 0x00169304 File Offset: 0x00167504
		private AstNode ParseUnionExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParsePathExpr(qyInput);
			while (this._scanner.Kind == XPathScanner.LexKind.Union)
			{
				this.NextLex();
				AstNode astNode2 = this.ParsePathExpr(qyInput);
				this.CheckNodeSet(astNode.ReturnType);
				this.CheckNodeSet(astNode2.ReturnType);
				astNode = new Operator(Operator.Op.UNION, astNode, astNode2);
			}
			return astNode;
		}

		// Token: 0x06004210 RID: 16912 RVA: 0x0016935C File Offset: 0x0016755C
		private static bool IsNodeType(XPathScanner scaner)
		{
			return scaner.Prefix.Length == 0 && (scaner.Name == "node" || scaner.Name == "text" || scaner.Name == "processing-instruction" || scaner.Name == "comment");
		}

		// Token: 0x06004211 RID: 16913 RVA: 0x001693C0 File Offset: 0x001675C0
		private AstNode ParsePathExpr(AstNode qyInput)
		{
			AstNode astNode;
			if (XPathParser.IsPrimaryExpr(this._scanner))
			{
				astNode = this.ParseFilterExpr(qyInput);
				if (this._scanner.Kind == XPathScanner.LexKind.Slash)
				{
					this.NextLex();
					astNode = this.ParseRelativeLocationPath(astNode);
				}
				else if (this._scanner.Kind == XPathScanner.LexKind.SlashSlash)
				{
					this.NextLex();
					astNode = this.ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, astNode));
				}
			}
			else
			{
				astNode = this.ParseLocationPath(null);
			}
			return astNode;
		}

		// Token: 0x06004212 RID: 16914 RVA: 0x00169430 File Offset: 0x00167630
		private AstNode ParseFilterExpr(AstNode qyInput)
		{
			AstNode astNode = this.ParsePrimaryExpr(qyInput);
			while (this._scanner.Kind == XPathScanner.LexKind.LBracket)
			{
				astNode = new Filter(astNode, this.ParsePredicate(astNode));
			}
			return astNode;
		}

		// Token: 0x06004213 RID: 16915 RVA: 0x00169465 File Offset: 0x00167665
		private AstNode ParsePredicate(AstNode qyInput)
		{
			this.CheckNodeSet(qyInput.ReturnType);
			this.PassToken(XPathScanner.LexKind.LBracket);
			AstNode result = this.ParseExpression(qyInput);
			this.PassToken(XPathScanner.LexKind.RBracket);
			return result;
		}

		// Token: 0x06004214 RID: 16916 RVA: 0x0016948C File Offset: 0x0016768C
		private AstNode ParseLocationPath(AstNode qyInput)
		{
			if (this._scanner.Kind == XPathScanner.LexKind.Slash)
			{
				this.NextLex();
				AstNode astNode = new Root();
				if (XPathParser.IsStep(this._scanner.Kind))
				{
					astNode = this.ParseRelativeLocationPath(astNode);
				}
				return astNode;
			}
			if (this._scanner.Kind == XPathScanner.LexKind.SlashSlash)
			{
				this.NextLex();
				return this.ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, new Root()));
			}
			return this.ParseRelativeLocationPath(qyInput);
		}

		// Token: 0x06004215 RID: 16917 RVA: 0x00169500 File Offset: 0x00167700
		private AstNode ParseRelativeLocationPath(AstNode qyInput)
		{
			AstNode astNode = qyInput;
			for (;;)
			{
				astNode = this.ParseStep(astNode);
				if (XPathScanner.LexKind.SlashSlash == this._scanner.Kind)
				{
					this.NextLex();
					astNode = new Axis(Axis.AxisType.DescendantOrSelf, astNode);
				}
				else
				{
					if (XPathScanner.LexKind.Slash != this._scanner.Kind)
					{
						break;
					}
					this.NextLex();
				}
			}
			return astNode;
		}

		// Token: 0x06004216 RID: 16918 RVA: 0x0016954E File Offset: 0x0016774E
		private static bool IsStep(XPathScanner.LexKind lexKind)
		{
			return lexKind == XPathScanner.LexKind.Dot || lexKind == XPathScanner.LexKind.DotDot || lexKind == XPathScanner.LexKind.At || lexKind == XPathScanner.LexKind.Axe || lexKind == XPathScanner.LexKind.Star || lexKind == XPathScanner.LexKind.Name;
		}

		// Token: 0x06004217 RID: 16919 RVA: 0x00169570 File Offset: 0x00167770
		private AstNode ParseStep(AstNode qyInput)
		{
			AstNode astNode;
			if (XPathScanner.LexKind.Dot == this._scanner.Kind)
			{
				this.NextLex();
				astNode = new Axis(Axis.AxisType.Self, qyInput);
			}
			else if (XPathScanner.LexKind.DotDot == this._scanner.Kind)
			{
				this.NextLex();
				astNode = new Axis(Axis.AxisType.Parent, qyInput);
			}
			else
			{
				Axis.AxisType axisType = Axis.AxisType.Child;
				XPathScanner.LexKind kind = this._scanner.Kind;
				if (kind != XPathScanner.LexKind.At)
				{
					if (kind == XPathScanner.LexKind.Axe)
					{
						axisType = this.GetAxis();
						this.NextLex();
					}
				}
				else
				{
					axisType = Axis.AxisType.Attribute;
					this.NextLex();
				}
				XPathNodeType nodeType = (axisType == Axis.AxisType.Attribute) ? XPathNodeType.Attribute : XPathNodeType.Element;
				astNode = this.ParseNodeTest(qyInput, axisType, nodeType);
				while (XPathScanner.LexKind.LBracket == this._scanner.Kind)
				{
					astNode = new Filter(astNode, this.ParsePredicate(astNode));
				}
			}
			return astNode;
		}

		// Token: 0x06004218 RID: 16920 RVA: 0x00169624 File Offset: 0x00167824
		private AstNode ParseNodeTest(AstNode qyInput, Axis.AxisType axisType, XPathNodeType nodeType)
		{
			XPathScanner.LexKind kind = this._scanner.Kind;
			string prefix;
			string text;
			if (kind != XPathScanner.LexKind.Star)
			{
				if (kind != XPathScanner.LexKind.Name)
				{
					throw XPathException.Create("Expression must evaluate to a node-set.", this._scanner.SourceText);
				}
				if (this._scanner.CanBeFunction && XPathParser.IsNodeType(this._scanner))
				{
					prefix = string.Empty;
					text = string.Empty;
					nodeType = ((this._scanner.Name == "comment") ? XPathNodeType.Comment : ((this._scanner.Name == "text") ? XPathNodeType.Text : ((this._scanner.Name == "node") ? XPathNodeType.All : ((this._scanner.Name == "processing-instruction") ? XPathNodeType.ProcessingInstruction : XPathNodeType.Root))));
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.LParens);
					if (nodeType == XPathNodeType.ProcessingInstruction && this._scanner.Kind != XPathScanner.LexKind.RParens)
					{
						this.CheckToken(XPathScanner.LexKind.String);
						text = this._scanner.StringValue;
						this.NextLex();
					}
					this.PassToken(XPathScanner.LexKind.RParens);
				}
				else
				{
					prefix = this._scanner.Prefix;
					text = this._scanner.Name;
					this.NextLex();
					if (text == "*")
					{
						text = string.Empty;
					}
				}
			}
			else
			{
				prefix = string.Empty;
				text = string.Empty;
				this.NextLex();
			}
			return new Axis(axisType, qyInput, prefix, text, nodeType);
		}

		// Token: 0x06004219 RID: 16921 RVA: 0x00169794 File Offset: 0x00167994
		private static bool IsPrimaryExpr(XPathScanner scanner)
		{
			return scanner.Kind == XPathScanner.LexKind.String || scanner.Kind == XPathScanner.LexKind.Number || scanner.Kind == XPathScanner.LexKind.Dollar || scanner.Kind == XPathScanner.LexKind.LParens || (scanner.Kind == XPathScanner.LexKind.Name && scanner.CanBeFunction && !XPathParser.IsNodeType(scanner));
		}

		// Token: 0x0600421A RID: 16922 RVA: 0x001697E8 File Offset: 0x001679E8
		private AstNode ParsePrimaryExpr(AstNode qyInput)
		{
			AstNode astNode = null;
			XPathScanner.LexKind kind = this._scanner.Kind;
			if (kind <= XPathScanner.LexKind.LParens)
			{
				if (kind != XPathScanner.LexKind.Dollar)
				{
					if (kind == XPathScanner.LexKind.LParens)
					{
						this.NextLex();
						astNode = this.ParseExpression(qyInput);
						if (astNode.Type != AstNode.AstType.ConstantOperand)
						{
							astNode = new Group(astNode);
						}
						this.PassToken(XPathScanner.LexKind.RParens);
					}
				}
				else
				{
					this.NextLex();
					this.CheckToken(XPathScanner.LexKind.Name);
					astNode = new Variable(this._scanner.Name, this._scanner.Prefix);
					this.NextLex();
				}
			}
			else if (kind != XPathScanner.LexKind.Number)
			{
				if (kind != XPathScanner.LexKind.Name)
				{
					if (kind == XPathScanner.LexKind.String)
					{
						astNode = new Operand(this._scanner.StringValue);
						this.NextLex();
					}
				}
				else if (this._scanner.CanBeFunction && !XPathParser.IsNodeType(this._scanner))
				{
					astNode = this.ParseMethod(null);
				}
			}
			else
			{
				astNode = new Operand(this._scanner.NumberValue);
				this.NextLex();
			}
			return astNode;
		}

		// Token: 0x0600421B RID: 16923 RVA: 0x001698E4 File Offset: 0x00167AE4
		private AstNode ParseMethod(AstNode qyInput)
		{
			List<AstNode> list = new List<AstNode>();
			string name = this._scanner.Name;
			string prefix = this._scanner.Prefix;
			this.PassToken(XPathScanner.LexKind.Name);
			this.PassToken(XPathScanner.LexKind.LParens);
			if (this._scanner.Kind != XPathScanner.LexKind.RParens)
			{
				for (;;)
				{
					list.Add(this.ParseExpression(qyInput));
					if (this._scanner.Kind == XPathScanner.LexKind.RParens)
					{
						break;
					}
					this.PassToken(XPathScanner.LexKind.Comma);
				}
			}
			this.PassToken(XPathScanner.LexKind.RParens);
			XPathParser.ParamInfo paramInfo;
			if (prefix.Length != 0 || !XPathParser.s_functionTable.TryGetValue(name, out paramInfo))
			{
				return new Function(prefix, name, list);
			}
			int num = list.Count;
			if (num < paramInfo.Minargs)
			{
				throw XPathException.Create("Function '{0}' in '{1}' has an invalid number of arguments.", name, this._scanner.SourceText);
			}
			if (paramInfo.FType == Function.FunctionType.FuncConcat)
			{
				for (int i = 0; i < num; i++)
				{
					AstNode astNode = list[i];
					if (astNode.ReturnType != XPathResultType.String)
					{
						astNode = new Function(Function.FunctionType.FuncString, astNode);
					}
					list[i] = astNode;
				}
			}
			else
			{
				if (paramInfo.Maxargs < num)
				{
					throw XPathException.Create("Function '{0}' in '{1}' has an invalid number of arguments.", name, this._scanner.SourceText);
				}
				if (paramInfo.ArgTypes.Length < num)
				{
					num = paramInfo.ArgTypes.Length;
				}
				for (int j = 0; j < num; j++)
				{
					AstNode astNode2 = list[j];
					if (paramInfo.ArgTypes[j] != XPathResultType.Any && paramInfo.ArgTypes[j] != astNode2.ReturnType)
					{
						switch (paramInfo.ArgTypes[j])
						{
						case XPathResultType.Number:
							astNode2 = new Function(Function.FunctionType.FuncNumber, astNode2);
							break;
						case XPathResultType.String:
							astNode2 = new Function(Function.FunctionType.FuncString, astNode2);
							break;
						case XPathResultType.Boolean:
							astNode2 = new Function(Function.FunctionType.FuncBoolean, astNode2);
							break;
						case XPathResultType.NodeSet:
							if (!(astNode2 is Variable) && (!(astNode2 is Function) || astNode2.ReturnType != XPathResultType.Any))
							{
								throw XPathException.Create("The argument to function '{0}' in '{1}' cannot be converted to a node-set.", name, this._scanner.SourceText);
							}
							break;
						}
						list[j] = astNode2;
					}
				}
			}
			return new Function(paramInfo.FType, list);
		}

		// Token: 0x0600421C RID: 16924 RVA: 0x00169B00 File Offset: 0x00167D00
		private AstNode ParsePattern()
		{
			AstNode astNode = this.ParseLocationPathPattern();
			while (this._scanner.Kind == XPathScanner.LexKind.Union)
			{
				this.NextLex();
				astNode = new Operator(Operator.Op.UNION, astNode, this.ParseLocationPathPattern());
			}
			return astNode;
		}

		// Token: 0x0600421D RID: 16925 RVA: 0x00169B3C File Offset: 0x00167D3C
		private AstNode ParseLocationPathPattern()
		{
			AstNode astNode = null;
			XPathScanner.LexKind kind = this._scanner.Kind;
			if (kind != XPathScanner.LexKind.Slash)
			{
				if (kind != XPathScanner.LexKind.SlashSlash)
				{
					if (kind == XPathScanner.LexKind.Name)
					{
						if (this._scanner.CanBeFunction)
						{
							astNode = this.ParseIdKeyPattern();
							if (astNode != null)
							{
								XPathScanner.LexKind kind2 = this._scanner.Kind;
								if (kind2 != XPathScanner.LexKind.Slash)
								{
									if (kind2 != XPathScanner.LexKind.SlashSlash)
									{
										return astNode;
									}
									this.NextLex();
									astNode = new Axis(Axis.AxisType.DescendantOrSelf, astNode);
								}
								else
								{
									this.NextLex();
								}
							}
						}
					}
				}
				else
				{
					this.NextLex();
					astNode = new Axis(Axis.AxisType.DescendantOrSelf, new Root());
				}
			}
			else
			{
				this.NextLex();
				astNode = new Root();
				if (this._scanner.Kind == XPathScanner.LexKind.Eof || this._scanner.Kind == XPathScanner.LexKind.Union)
				{
					return astNode;
				}
			}
			return this.ParseRelativePathPattern(astNode);
		}

		// Token: 0x0600421E RID: 16926 RVA: 0x00169BFC File Offset: 0x00167DFC
		private AstNode ParseIdKeyPattern()
		{
			List<AstNode> list = new List<AstNode>();
			if (this._scanner.Prefix.Length == 0)
			{
				if (this._scanner.Name == "id")
				{
					XPathParser.ParamInfo paramInfo = XPathParser.s_functionTable["id"];
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.LParens);
					this.CheckToken(XPathScanner.LexKind.String);
					list.Add(new Operand(this._scanner.StringValue));
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.RParens);
					return new Function(paramInfo.FType, list);
				}
				if (this._scanner.Name == "key")
				{
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.LParens);
					this.CheckToken(XPathScanner.LexKind.String);
					list.Add(new Operand(this._scanner.StringValue));
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.Comma);
					this.CheckToken(XPathScanner.LexKind.String);
					list.Add(new Operand(this._scanner.StringValue));
					this.NextLex();
					this.PassToken(XPathScanner.LexKind.RParens);
					return new Function("", "key", list);
				}
			}
			return null;
		}

		// Token: 0x0600421F RID: 16927 RVA: 0x00169D20 File Offset: 0x00167F20
		private AstNode ParseRelativePathPattern(AstNode qyInput)
		{
			AstNode astNode = this.ParseStepPattern(qyInput);
			if (XPathScanner.LexKind.SlashSlash == this._scanner.Kind)
			{
				this.NextLex();
				astNode = this.ParseRelativePathPattern(new Axis(Axis.AxisType.DescendantOrSelf, astNode));
			}
			else if (XPathScanner.LexKind.Slash == this._scanner.Kind)
			{
				this.NextLex();
				astNode = this.ParseRelativePathPattern(astNode);
			}
			return astNode;
		}

		// Token: 0x06004220 RID: 16928 RVA: 0x00169D78 File Offset: 0x00167F78
		private AstNode ParseStepPattern(AstNode qyInput)
		{
			Axis.AxisType axisType = Axis.AxisType.Child;
			XPathScanner.LexKind kind = this._scanner.Kind;
			if (kind != XPathScanner.LexKind.At)
			{
				if (kind == XPathScanner.LexKind.Axe)
				{
					axisType = this.GetAxis();
					if (axisType != Axis.AxisType.Child && axisType != Axis.AxisType.Attribute)
					{
						throw XPathException.Create("'{0}' has an invalid token.", this._scanner.SourceText);
					}
					this.NextLex();
				}
			}
			else
			{
				axisType = Axis.AxisType.Attribute;
				this.NextLex();
			}
			XPathNodeType nodeType = (axisType == Axis.AxisType.Attribute) ? XPathNodeType.Attribute : XPathNodeType.Element;
			AstNode astNode = this.ParseNodeTest(qyInput, axisType, nodeType);
			while (XPathScanner.LexKind.LBracket == this._scanner.Kind)
			{
				astNode = new Filter(astNode, this.ParsePredicate(astNode));
			}
			return astNode;
		}

		// Token: 0x06004221 RID: 16929 RVA: 0x00169E07 File Offset: 0x00168007
		private void CheckToken(XPathScanner.LexKind t)
		{
			if (this._scanner.Kind != t)
			{
				throw XPathException.Create("'{0}' has an invalid token.", this._scanner.SourceText);
			}
		}

		// Token: 0x06004222 RID: 16930 RVA: 0x00169E2D File Offset: 0x0016802D
		private void PassToken(XPathScanner.LexKind t)
		{
			this.CheckToken(t);
			this.NextLex();
		}

		// Token: 0x06004223 RID: 16931 RVA: 0x00169E3C File Offset: 0x0016803C
		private void NextLex()
		{
			this._scanner.NextLex();
		}

		// Token: 0x06004224 RID: 16932 RVA: 0x00169E4A File Offset: 0x0016804A
		private bool TestOp(string op)
		{
			return this._scanner.Kind == XPathScanner.LexKind.Name && this._scanner.Prefix.Length == 0 && this._scanner.Name.Equals(op);
		}

		// Token: 0x06004225 RID: 16933 RVA: 0x00169E80 File Offset: 0x00168080
		private void CheckNodeSet(XPathResultType t)
		{
			if (t != XPathResultType.NodeSet && t != XPathResultType.Any)
			{
				throw XPathException.Create("Expression must evaluate to a node-set.", this._scanner.SourceText);
			}
		}

		// Token: 0x06004226 RID: 16934 RVA: 0x00169EA0 File Offset: 0x001680A0
		private static Dictionary<string, XPathParser.ParamInfo> CreateFunctionTable()
		{
			return new Dictionary<string, XPathParser.ParamInfo>(36)
			{
				{
					"last",
					new XPathParser.ParamInfo(Function.FunctionType.FuncLast, 0, 0, XPathParser.s_temparray1)
				},
				{
					"position",
					new XPathParser.ParamInfo(Function.FunctionType.FuncPosition, 0, 0, XPathParser.s_temparray1)
				},
				{
					"name",
					new XPathParser.ParamInfo(Function.FunctionType.FuncName, 0, 1, XPathParser.s_temparray2)
				},
				{
					"namespace-uri",
					new XPathParser.ParamInfo(Function.FunctionType.FuncNameSpaceUri, 0, 1, XPathParser.s_temparray2)
				},
				{
					"local-name",
					new XPathParser.ParamInfo(Function.FunctionType.FuncLocalName, 0, 1, XPathParser.s_temparray2)
				},
				{
					"count",
					new XPathParser.ParamInfo(Function.FunctionType.FuncCount, 1, 1, XPathParser.s_temparray2)
				},
				{
					"id",
					new XPathParser.ParamInfo(Function.FunctionType.FuncID, 1, 1, XPathParser.s_temparray3)
				},
				{
					"string",
					new XPathParser.ParamInfo(Function.FunctionType.FuncString, 0, 1, XPathParser.s_temparray3)
				},
				{
					"concat",
					new XPathParser.ParamInfo(Function.FunctionType.FuncConcat, 2, 100, XPathParser.s_temparray4)
				},
				{
					"starts-with",
					new XPathParser.ParamInfo(Function.FunctionType.FuncStartsWith, 2, 2, XPathParser.s_temparray5)
				},
				{
					"contains",
					new XPathParser.ParamInfo(Function.FunctionType.FuncContains, 2, 2, XPathParser.s_temparray5)
				},
				{
					"substring-before",
					new XPathParser.ParamInfo(Function.FunctionType.FuncSubstringBefore, 2, 2, XPathParser.s_temparray5)
				},
				{
					"substring-after",
					new XPathParser.ParamInfo(Function.FunctionType.FuncSubstringAfter, 2, 2, XPathParser.s_temparray5)
				},
				{
					"substring",
					new XPathParser.ParamInfo(Function.FunctionType.FuncSubstring, 2, 3, XPathParser.s_temparray6)
				},
				{
					"string-length",
					new XPathParser.ParamInfo(Function.FunctionType.FuncStringLength, 0, 1, XPathParser.s_temparray4)
				},
				{
					"normalize-space",
					new XPathParser.ParamInfo(Function.FunctionType.FuncNormalize, 0, 1, XPathParser.s_temparray4)
				},
				{
					"translate",
					new XPathParser.ParamInfo(Function.FunctionType.FuncTranslate, 3, 3, XPathParser.s_temparray7)
				},
				{
					"boolean",
					new XPathParser.ParamInfo(Function.FunctionType.FuncBoolean, 1, 1, XPathParser.s_temparray3)
				},
				{
					"not",
					new XPathParser.ParamInfo(Function.FunctionType.FuncNot, 1, 1, XPathParser.s_temparray8)
				},
				{
					"true",
					new XPathParser.ParamInfo(Function.FunctionType.FuncTrue, 0, 0, XPathParser.s_temparray8)
				},
				{
					"false",
					new XPathParser.ParamInfo(Function.FunctionType.FuncFalse, 0, 0, XPathParser.s_temparray8)
				},
				{
					"lang",
					new XPathParser.ParamInfo(Function.FunctionType.FuncLang, 1, 1, XPathParser.s_temparray4)
				},
				{
					"number",
					new XPathParser.ParamInfo(Function.FunctionType.FuncNumber, 0, 1, XPathParser.s_temparray3)
				},
				{
					"sum",
					new XPathParser.ParamInfo(Function.FunctionType.FuncSum, 1, 1, XPathParser.s_temparray2)
				},
				{
					"floor",
					new XPathParser.ParamInfo(Function.FunctionType.FuncFloor, 1, 1, XPathParser.s_temparray9)
				},
				{
					"ceiling",
					new XPathParser.ParamInfo(Function.FunctionType.FuncCeiling, 1, 1, XPathParser.s_temparray9)
				},
				{
					"round",
					new XPathParser.ParamInfo(Function.FunctionType.FuncRound, 1, 1, XPathParser.s_temparray9)
				}
			};
		}

		// Token: 0x06004227 RID: 16935 RVA: 0x0016A150 File Offset: 0x00168350
		private static Dictionary<string, Axis.AxisType> CreateAxesTable()
		{
			return new Dictionary<string, Axis.AxisType>(13)
			{
				{
					"ancestor",
					Axis.AxisType.Ancestor
				},
				{
					"ancestor-or-self",
					Axis.AxisType.AncestorOrSelf
				},
				{
					"attribute",
					Axis.AxisType.Attribute
				},
				{
					"child",
					Axis.AxisType.Child
				},
				{
					"descendant",
					Axis.AxisType.Descendant
				},
				{
					"descendant-or-self",
					Axis.AxisType.DescendantOrSelf
				},
				{
					"following",
					Axis.AxisType.Following
				},
				{
					"following-sibling",
					Axis.AxisType.FollowingSibling
				},
				{
					"namespace",
					Axis.AxisType.Namespace
				},
				{
					"parent",
					Axis.AxisType.Parent
				},
				{
					"preceding",
					Axis.AxisType.Preceding
				},
				{
					"preceding-sibling",
					Axis.AxisType.PrecedingSibling
				},
				{
					"self",
					Axis.AxisType.Self
				}
			};
		}

		// Token: 0x06004228 RID: 16936 RVA: 0x0016A204 File Offset: 0x00168404
		private Axis.AxisType GetAxis()
		{
			Axis.AxisType result;
			if (!XPathParser.s_AxesTable.TryGetValue(this._scanner.Name, out result))
			{
				throw XPathException.Create("'{0}' has an invalid token.", this._scanner.SourceText);
			}
			return result;
		}

		// Token: 0x06004229 RID: 16937 RVA: 0x0016A244 File Offset: 0x00168444
		// Note: this type is marked as 'beforefieldinit'.
		static XPathParser()
		{
			XPathResultType[] array = new XPathResultType[3];
			array[0] = XPathResultType.String;
			XPathParser.s_temparray6 = array;
			XPathParser.s_temparray7 = new XPathResultType[]
			{
				XPathResultType.String,
				XPathResultType.String,
				XPathResultType.String
			};
			XPathParser.s_temparray8 = new XPathResultType[]
			{
				XPathResultType.Boolean
			};
			XPathParser.s_temparray9 = new XPathResultType[1];
			XPathParser.s_functionTable = XPathParser.CreateFunctionTable();
			XPathParser.s_AxesTable = XPathParser.CreateAxesTable();
		}

		// Token: 0x04002EB0 RID: 11952
		private XPathScanner _scanner;

		// Token: 0x04002EB1 RID: 11953
		private int _parseDepth;

		// Token: 0x04002EB2 RID: 11954
		private const int MaxParseDepth = 200;

		// Token: 0x04002EB3 RID: 11955
		private static readonly XPathResultType[] s_temparray1 = Array.Empty<XPathResultType>();

		// Token: 0x04002EB4 RID: 11956
		private static readonly XPathResultType[] s_temparray2 = new XPathResultType[]
		{
			XPathResultType.NodeSet
		};

		// Token: 0x04002EB5 RID: 11957
		private static readonly XPathResultType[] s_temparray3 = new XPathResultType[]
		{
			XPathResultType.Any
		};

		// Token: 0x04002EB6 RID: 11958
		private static readonly XPathResultType[] s_temparray4 = new XPathResultType[]
		{
			XPathResultType.String
		};

		// Token: 0x04002EB7 RID: 11959
		private static readonly XPathResultType[] s_temparray5 = new XPathResultType[]
		{
			XPathResultType.String,
			XPathResultType.String
		};

		// Token: 0x04002EB8 RID: 11960
		private static readonly XPathResultType[] s_temparray6;

		// Token: 0x04002EB9 RID: 11961
		private static readonly XPathResultType[] s_temparray7;

		// Token: 0x04002EBA RID: 11962
		private static readonly XPathResultType[] s_temparray8;

		// Token: 0x04002EBB RID: 11963
		private static readonly XPathResultType[] s_temparray9;

		// Token: 0x04002EBC RID: 11964
		private static Dictionary<string, XPathParser.ParamInfo> s_functionTable;

		// Token: 0x04002EBD RID: 11965
		private static Dictionary<string, Axis.AxisType> s_AxesTable;

		// Token: 0x02000660 RID: 1632
		private class ParamInfo
		{
			// Token: 0x17000C9B RID: 3227
			// (get) Token: 0x0600422A RID: 16938 RVA: 0x0016A2EE File Offset: 0x001684EE
			public Function.FunctionType FType
			{
				get
				{
					return this._ftype;
				}
			}

			// Token: 0x17000C9C RID: 3228
			// (get) Token: 0x0600422B RID: 16939 RVA: 0x0016A2F6 File Offset: 0x001684F6
			public int Minargs
			{
				get
				{
					return this._minargs;
				}
			}

			// Token: 0x17000C9D RID: 3229
			// (get) Token: 0x0600422C RID: 16940 RVA: 0x0016A2FE File Offset: 0x001684FE
			public int Maxargs
			{
				get
				{
					return this._maxargs;
				}
			}

			// Token: 0x17000C9E RID: 3230
			// (get) Token: 0x0600422D RID: 16941 RVA: 0x0016A306 File Offset: 0x00168506
			public XPathResultType[] ArgTypes
			{
				get
				{
					return this._argTypes;
				}
			}

			// Token: 0x0600422E RID: 16942 RVA: 0x0016A30E File Offset: 0x0016850E
			internal ParamInfo(Function.FunctionType ftype, int minargs, int maxargs, XPathResultType[] argTypes)
			{
				this._ftype = ftype;
				this._minargs = minargs;
				this._maxargs = maxargs;
				this._argTypes = argTypes;
			}

			// Token: 0x04002EBE RID: 11966
			private Function.FunctionType _ftype;

			// Token: 0x04002EBF RID: 11967
			private int _minargs;

			// Token: 0x04002EC0 RID: 11968
			private int _maxargs;

			// Token: 0x04002EC1 RID: 11969
			private XPathResultType[] _argTypes;
		}
	}
}
