﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Text.RegularExpressions
{
	// Token: 0x02000209 RID: 521
	internal sealed class RegexParser
	{
		// Token: 0x06000EB8 RID: 3768 RVA: 0x00040968 File Offset: 0x0003EB68
		public static RegexTree Parse(string re, RegexOptions op)
		{
			RegexParser regexParser = new RegexParser(((op & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
			regexParser._options = op;
			regexParser.SetPattern(re);
			regexParser.CountCaptures();
			regexParser.Reset(op);
			RegexNode root = regexParser.ScanRegex();
			string[] capsList;
			if (regexParser._capnamelist == null)
			{
				capsList = null;
			}
			else
			{
				capsList = regexParser._capnamelist.ToArray();
			}
			return new RegexTree(root, regexParser._caps, regexParser._capnumlist, regexParser._captop, regexParser._capnames, capsList, op);
		}

		// Token: 0x06000EB9 RID: 3769 RVA: 0x000409E8 File Offset: 0x0003EBE8
		public static RegexReplacement ParseReplacement(string rep, Hashtable caps, int capsize, Hashtable capnames, RegexOptions op)
		{
			RegexParser regexParser = new RegexParser(((op & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
			regexParser._options = op;
			regexParser.NoteCaptures(caps, capsize, capnames);
			regexParser.SetPattern(rep);
			RegexNode concat = regexParser.ScanReplacement();
			return new RegexReplacement(rep, concat, caps);
		}

		// Token: 0x06000EBA RID: 3770 RVA: 0x00040A38 File Offset: 0x0003EC38
		public static string Escape(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (RegexParser.IsMetachar(input[i]))
				{
					StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
					char c = input[i];
					stringBuilder.Append(input, 0, i);
					do
					{
						stringBuilder.Append('\\');
						switch (c)
						{
						case '\t':
							c = 't';
							break;
						case '\n':
							c = 'n';
							break;
						case '\f':
							c = 'f';
							break;
						case '\r':
							c = 'r';
							break;
						}
						stringBuilder.Append(c);
						i++;
						int num = i;
						while (i < input.Length)
						{
							c = input[i];
							if (RegexParser.IsMetachar(c))
							{
								break;
							}
							i++;
						}
						stringBuilder.Append(input, num, i - num);
					}
					while (i < input.Length);
					return StringBuilderCache.GetStringAndRelease(stringBuilder);
				}
			}
			return input;
		}

		// Token: 0x06000EBB RID: 3771 RVA: 0x00040B0C File Offset: 0x0003ED0C
		public static string Unescape(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] == '\\')
				{
					StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
					RegexParser regexParser = new RegexParser(CultureInfo.InvariantCulture);
					regexParser.SetPattern(input);
					stringBuilder.Append(input, 0, i);
					do
					{
						i++;
						regexParser.Textto(i);
						if (i < input.Length)
						{
							stringBuilder.Append(regexParser.ScanCharEscape());
						}
						i = regexParser.Textpos();
						int num = i;
						while (i < input.Length && input[i] != '\\')
						{
							i++;
						}
						stringBuilder.Append(input, num, i - num);
					}
					while (i < input.Length);
					return StringBuilderCache.GetStringAndRelease(stringBuilder);
				}
			}
			return input;
		}

		// Token: 0x06000EBC RID: 3772 RVA: 0x00040BC3 File Offset: 0x0003EDC3
		private RegexParser(CultureInfo culture)
		{
			this._culture = culture;
			this._optionsStack = new List<RegexOptions>();
			this._caps = new Hashtable();
		}

		// Token: 0x06000EBD RID: 3773 RVA: 0x00040BE8 File Offset: 0x0003EDE8
		private void SetPattern(string Re)
		{
			if (Re == null)
			{
				Re = string.Empty;
			}
			this._pattern = Re;
			this._currentPos = 0;
		}

		// Token: 0x06000EBE RID: 3774 RVA: 0x00040C04 File Offset: 0x0003EE04
		private void Reset(RegexOptions topopts)
		{
			this._currentPos = 0;
			this._autocap = 1;
			this._ignoreNextParen = false;
			if (this._optionsStack.Count > 0)
			{
				this._optionsStack.RemoveRange(0, this._optionsStack.Count - 1);
			}
			this._options = topopts;
			this._stack = null;
		}

		// Token: 0x06000EBF RID: 3775 RVA: 0x00040C5C File Offset: 0x0003EE5C
		private RegexNode ScanRegex()
		{
			bool flag = false;
			this.StartGroup(new RegexNode(28, this._options, 0, -1));
			while (this.CharsRight() > 0)
			{
				bool flag2 = flag;
				flag = false;
				this.ScanBlank();
				int num = this.Textpos();
				char c;
				if (this.UseOptionX())
				{
					while (this.CharsRight() > 0)
					{
						if (RegexParser.IsStopperX(c = this.RightChar()))
						{
							if (c != '{')
							{
								break;
							}
							if (this.IsTrueQuantifier())
							{
								break;
							}
						}
						this.MoveRight();
					}
				}
				else
				{
					while (this.CharsRight() > 0 && (!RegexParser.IsSpecial(c = this.RightChar()) || (c == '{' && !this.IsTrueQuantifier())))
					{
						this.MoveRight();
					}
				}
				int num2 = this.Textpos();
				this.ScanBlank();
				if (this.CharsRight() == 0)
				{
					c = '!';
				}
				else if (RegexParser.IsSpecial(c = this.RightChar()))
				{
					flag = RegexParser.IsQuantifier(c);
					this.MoveRight();
				}
				else
				{
					c = ' ';
				}
				if (num < num2)
				{
					int num3 = num2 - num - (flag ? 1 : 0);
					flag2 = false;
					if (num3 > 0)
					{
						this.AddConcatenate(num, num3, false);
					}
					if (flag)
					{
						this.AddUnitOne(this.CharAt(num2 - 1));
					}
				}
				if (c <= '?')
				{
					switch (c)
					{
					case ' ':
						continue;
					case '!':
						goto IL_414;
					case '"':
					case '#':
					case '%':
					case '&':
					case '\'':
					case ',':
					case '-':
						goto IL_2A3;
					case '$':
						this.AddUnitType(this.UseOptionM() ? 15 : 20);
						break;
					case '(':
					{
						this.PushOptions();
						RegexNode openGroup;
						if ((openGroup = this.ScanGroupOpen()) == null)
						{
							this.PopKeepOptions();
							continue;
						}
						this.PushGroup();
						this.StartGroup(openGroup);
						continue;
					}
					case ')':
						if (this.EmptyStack())
						{
							throw this.MakeException("Too many )'s.");
						}
						this.AddGroup();
						this.PopGroup();
						this.PopOptions();
						if (this.Unit() == null)
						{
							continue;
						}
						break;
					case '*':
					case '+':
						goto IL_271;
					case '.':
						if (this.UseOptionS())
						{
							this.AddUnitSet("\0\u0001\0\0");
						}
						else
						{
							this.AddUnitNotone('\n');
						}
						break;
					default:
						if (c != '?')
						{
							goto IL_2A3;
						}
						goto IL_271;
					}
				}
				else
				{
					switch (c)
					{
					case '[':
						this.AddUnitSet(this.ScanCharClass(this.UseOptionI(), false).ToStringClass());
						break;
					case '\\':
						this.AddUnitNode(this.ScanBackslash(false));
						break;
					case ']':
						goto IL_2A3;
					case '^':
						this.AddUnitType(this.UseOptionM() ? 14 : 18);
						break;
					default:
						if (c == '{')
						{
							goto IL_271;
						}
						if (c != '|')
						{
							goto IL_2A3;
						}
						this.AddAlternate();
						continue;
					}
				}
				IL_2AF:
				this.ScanBlank();
				if (this.CharsRight() == 0 || !(flag = this.IsTrueQuantifier()))
				{
					this.AddConcatenate();
					continue;
				}
				c = this.RightCharMoveRight();
				while (this.Unit() != null)
				{
					int num4;
					int num5;
					if (c <= '+')
					{
						if (c != '*')
						{
							if (c != '+')
							{
								goto IL_3AD;
							}
							num4 = 1;
							num5 = int.MaxValue;
						}
						else
						{
							num4 = 0;
							num5 = int.MaxValue;
						}
					}
					else if (c != '?')
					{
						if (c != '{')
						{
							goto IL_3AD;
						}
						num = this.Textpos();
						num4 = (num5 = this.ScanDecimal());
						if (num < this.Textpos() && this.CharsRight() > 0 && this.RightChar() == ',')
						{
							this.MoveRight();
							if (this.CharsRight() == 0 || this.RightChar() == '}')
							{
								num5 = int.MaxValue;
							}
							else
							{
								num5 = this.ScanDecimal();
							}
						}
						if (num == this.Textpos() || this.CharsRight() == 0 || this.RightCharMoveRight() != '}')
						{
							this.AddConcatenate();
							this.Textto(num - 1);
							break;
						}
					}
					else
					{
						num4 = 0;
						num5 = 1;
					}
					this.ScanBlank();
					bool lazy;
					if (this.CharsRight() == 0 || this.RightChar() != '?')
					{
						lazy = false;
					}
					else
					{
						this.MoveRight();
						lazy = true;
					}
					if (num4 > num5)
					{
						throw this.MakeException("Illegal {x,y} with x > y.");
					}
					this.AddConcatenate(lazy, num4, num5);
					continue;
					IL_3AD:
					throw this.MakeException("Internal error in ScanRegex.");
				}
				continue;
				IL_271:
				if (this.Unit() == null)
				{
					throw this.MakeException(flag2 ? SR.Format("Nested quantifier {0}.", c.ToString()) : "Quantifier {x,y} following nothing.");
				}
				this.MoveLeft();
				goto IL_2AF;
				IL_2A3:
				throw this.MakeException("Internal error in ScanRegex.");
			}
			IL_414:
			if (!this.EmptyStack())
			{
				throw this.MakeException("Not enough )'s.");
			}
			this.AddGroup();
			return this.Unit();
		}

		// Token: 0x06000EC0 RID: 3776 RVA: 0x000410A0 File Offset: 0x0003F2A0
		private RegexNode ScanReplacement()
		{
			this._concatenation = new RegexNode(25, this._options);
			for (;;)
			{
				int num = this.CharsRight();
				if (num == 0)
				{
					break;
				}
				int num2 = this.Textpos();
				while (num > 0 && this.RightChar() != '$')
				{
					this.MoveRight();
					num--;
				}
				this.AddConcatenate(num2, this.Textpos() - num2, true);
				if (num > 0)
				{
					if (this.RightCharMoveRight() == '$')
					{
						this.AddUnitNode(this.ScanDollar());
					}
					this.AddConcatenate();
				}
			}
			return this._concatenation;
		}

		// Token: 0x06000EC1 RID: 3777 RVA: 0x00041124 File Offset: 0x0003F324
		private RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
		{
			char c = '\0';
			bool flag = false;
			bool flag2 = true;
			bool flag3 = false;
			RegexCharClass regexCharClass = scanOnly ? null : new RegexCharClass();
			if (this.CharsRight() > 0 && this.RightChar() == '^')
			{
				this.MoveRight();
				if (!scanOnly)
				{
					regexCharClass.Negate = true;
				}
			}
			while (this.CharsRight() > 0)
			{
				bool flag4 = false;
				char c2 = this.RightCharMoveRight();
				if (c2 == ']')
				{
					if (!flag2)
					{
						flag3 = true;
						break;
					}
					goto IL_261;
				}
				else
				{
					if (c2 == '\\' && this.CharsRight() > 0)
					{
						char c3;
						c2 = (c3 = this.RightCharMoveRight());
						if (c3 <= 'S')
						{
							if (c3 <= 'D')
							{
								if (c3 != '-')
								{
									if (c3 != 'D')
									{
										goto IL_1FA;
									}
								}
								else
								{
									if (!scanOnly)
									{
										regexCharClass.AddRange(c2, c2);
										goto IL_371;
									}
									goto IL_371;
								}
							}
							else
							{
								if (c3 == 'P')
								{
									goto IL_19B;
								}
								if (c3 != 'S')
								{
									goto IL_1FA;
								}
								goto IL_12B;
							}
						}
						else
						{
							if (c3 <= 'd')
							{
								if (c3 != 'W')
								{
									if (c3 != 'd')
									{
										goto IL_1FA;
									}
									goto IL_ED;
								}
							}
							else
							{
								if (c3 == 'p')
								{
									goto IL_19B;
								}
								if (c3 == 's')
								{
									goto IL_12B;
								}
								if (c3 != 'w')
								{
									goto IL_1FA;
								}
							}
							if (scanOnly)
							{
								goto IL_371;
							}
							if (flag)
							{
								throw this.MakeException(SR.Format("Cannot include class \\{0} in character range.", c2.ToString()));
							}
							regexCharClass.AddWord(this.UseOptionE(), c2 == 'W');
							goto IL_371;
						}
						IL_ED:
						if (scanOnly)
						{
							goto IL_371;
						}
						if (flag)
						{
							throw this.MakeException(SR.Format("Cannot include class \\{0} in character range.", c2.ToString()));
						}
						regexCharClass.AddDigit(this.UseOptionE(), c2 == 'D', this._pattern);
						goto IL_371;
						IL_12B:
						if (scanOnly)
						{
							goto IL_371;
						}
						if (flag)
						{
							throw this.MakeException(SR.Format("Cannot include class \\{0} in character range.", c2.ToString()));
						}
						regexCharClass.AddSpace(this.UseOptionE(), c2 == 'S');
						goto IL_371;
						IL_19B:
						if (scanOnly)
						{
							this.ParseProperty();
							goto IL_371;
						}
						if (flag)
						{
							throw this.MakeException(SR.Format("Cannot include class \\{0} in character range.", c2.ToString()));
						}
						regexCharClass.AddCategoryFromName(this.ParseProperty(), c2 != 'p', caseInsensitive, this._pattern);
						goto IL_371;
						IL_1FA:
						this.MoveLeft();
						c2 = this.ScanCharEscape();
						flag4 = true;
						goto IL_261;
					}
					if (c2 != '[' || this.CharsRight() <= 0 || this.RightChar() != ':' || flag)
					{
						goto IL_261;
					}
					int pos = this.Textpos();
					this.MoveRight();
					this.ScanCapname();
					if (this.CharsRight() < 2 || this.RightCharMoveRight() != ':' || this.RightCharMoveRight() != ']')
					{
						this.Textto(pos);
						goto IL_261;
					}
					goto IL_261;
				}
				IL_371:
				flag2 = false;
				continue;
				IL_261:
				if (flag)
				{
					flag = false;
					if (scanOnly)
					{
						goto IL_371;
					}
					if (c2 == '[' && !flag4 && !flag2)
					{
						regexCharClass.AddChar(c);
						regexCharClass.AddSubtraction(this.ScanCharClass(caseInsensitive, scanOnly));
						if (this.CharsRight() > 0 && this.RightChar() != ']')
						{
							throw this.MakeException("A subtraction must be the last element in a character class.");
						}
						goto IL_371;
					}
					else
					{
						if (c > c2)
						{
							throw this.MakeException("[x-y] range in reverse order.");
						}
						regexCharClass.AddRange(c, c2);
						goto IL_371;
					}
				}
				else
				{
					if (this.CharsRight() >= 2 && this.RightChar() == '-' && this.RightChar(1) != ']')
					{
						c = c2;
						flag = true;
						this.MoveRight();
						goto IL_371;
					}
					if (this.CharsRight() >= 1 && c2 == '-' && !flag4 && this.RightChar() == '[' && !flag2)
					{
						if (scanOnly)
						{
							this.MoveRight(1);
							this.ScanCharClass(caseInsensitive, scanOnly);
							goto IL_371;
						}
						this.MoveRight(1);
						regexCharClass.AddSubtraction(this.ScanCharClass(caseInsensitive, scanOnly));
						if (this.CharsRight() > 0 && this.RightChar() != ']')
						{
							throw this.MakeException("A subtraction must be the last element in a character class.");
						}
						goto IL_371;
					}
					else
					{
						if (!scanOnly)
						{
							regexCharClass.AddRange(c2, c2);
							goto IL_371;
						}
						goto IL_371;
					}
				}
			}
			if (!flag3)
			{
				throw this.MakeException("Unterminated [] set.");
			}
			if (!scanOnly && caseInsensitive)
			{
				regexCharClass.AddLowercase(this._culture);
			}
			return regexCharClass;
		}

		// Token: 0x06000EC2 RID: 3778 RVA: 0x000414D8 File Offset: 0x0003F6D8
		private RegexNode ScanGroupOpen()
		{
			char c = '>';
			if (this.CharsRight() != 0 && this.RightChar() == '?' && (this.RightChar() != '?' || this.CharsRight() <= 1 || this.RightChar(1) != ')'))
			{
				this.MoveRight();
				if (this.CharsRight() != 0)
				{
					char c2 = this.RightCharMoveRight();
					int type;
					char c3;
					if (c2 <= '\'')
					{
						if (c2 == '!')
						{
							this._options &= ~RegexOptions.RightToLeft;
							type = 31;
							goto IL_4FA;
						}
						if (c2 != '\'')
						{
							goto IL_4C1;
						}
						c = '\'';
					}
					else if (c2 != '(')
					{
						switch (c2)
						{
						case ':':
							type = 29;
							goto IL_4FA;
						case ';':
							goto IL_4C1;
						case '<':
							break;
						case '=':
							this._options &= ~RegexOptions.RightToLeft;
							type = 30;
							goto IL_4FA;
						case '>':
							type = 32;
							goto IL_4FA;
						default:
							goto IL_4C1;
						}
					}
					else
					{
						int num = this.Textpos();
						if (this.CharsRight() > 0)
						{
							c3 = this.RightChar();
							if (c3 >= '0' && c3 <= '9')
							{
								int num2 = this.ScanDecimal();
								if (this.CharsRight() <= 0 || this.RightCharMoveRight() != ')')
								{
									throw this.MakeException(SR.Format("(?({0}) ) malformed.", num2.ToString(CultureInfo.CurrentCulture)));
								}
								if (this.IsCaptureSlot(num2))
								{
									return new RegexNode(33, this._options, num2);
								}
								throw this.MakeException(SR.Format("(?({0}) ) reference to undefined group.", num2.ToString(CultureInfo.CurrentCulture)));
							}
							else if (RegexCharClass.IsWordChar(c3))
							{
								string capname = this.ScanCapname();
								if (this.IsCaptureName(capname) && this.CharsRight() > 0 && this.RightCharMoveRight() == ')')
								{
									return new RegexNode(33, this._options, this.CaptureSlotFromName(capname));
								}
							}
						}
						type = 34;
						this.Textto(num - 1);
						this._ignoreNextParen = true;
						int num3 = this.CharsRight();
						if (num3 < 3 || this.RightChar(1) != '?')
						{
							goto IL_4FA;
						}
						char c4 = this.RightChar(2);
						if (c4 == '#')
						{
							throw this.MakeException("Alternation conditions cannot be comments.");
						}
						if (c4 == '\'')
						{
							throw this.MakeException("Alternation conditions do not capture and cannot be named.");
						}
						if (num3 >= 4 && c4 == '<' && this.RightChar(3) != '!' && this.RightChar(3) != '=')
						{
							throw this.MakeException("Alternation conditions do not capture and cannot be named.");
						}
						goto IL_4FA;
					}
					if (this.CharsRight() == 0)
					{
						goto IL_507;
					}
					char c5;
					c3 = (c5 = this.RightCharMoveRight());
					if (c5 != '!')
					{
						if (c5 == '=')
						{
							if (c != '\'')
							{
								this._options |= RegexOptions.RightToLeft;
								type = 30;
								goto IL_4FA;
							}
							goto IL_507;
						}
						else
						{
							this.MoveLeft();
							int num4 = -1;
							int num5 = -1;
							bool flag = false;
							if (c3 >= '0' && c3 <= '9')
							{
								num4 = this.ScanDecimal();
								if (!this.IsCaptureSlot(num4))
								{
									num4 = -1;
								}
								if (this.CharsRight() > 0 && this.RightChar() != c && this.RightChar() != '-')
								{
									throw this.MakeException("Invalid group name: Group names must begin with a word character.");
								}
								if (num4 == 0)
								{
									throw this.MakeException("Capture number cannot be zero.");
								}
							}
							else if (RegexCharClass.IsWordChar(c3))
							{
								string capname2 = this.ScanCapname();
								if (this.IsCaptureName(capname2))
								{
									num4 = this.CaptureSlotFromName(capname2);
								}
								if (this.CharsRight() > 0 && this.RightChar() != c && this.RightChar() != '-')
								{
									throw this.MakeException("Invalid group name: Group names must begin with a word character.");
								}
							}
							else
							{
								if (c3 != '-')
								{
									throw this.MakeException("Invalid group name: Group names must begin with a word character.");
								}
								flag = true;
							}
							if ((num4 != -1 || flag) && this.CharsRight() > 1 && this.RightChar() == '-')
							{
								this.MoveRight();
								c3 = this.RightChar();
								if (c3 >= '0' && c3 <= '9')
								{
									num5 = this.ScanDecimal();
									if (!this.IsCaptureSlot(num5))
									{
										throw this.MakeException(SR.Format("Reference to undefined group number {0}.", num5));
									}
									if (this.CharsRight() > 0 && this.RightChar() != c)
									{
										throw this.MakeException("Invalid group name: Group names must begin with a word character.");
									}
								}
								else
								{
									if (!RegexCharClass.IsWordChar(c3))
									{
										throw this.MakeException("Invalid group name: Group names must begin with a word character.");
									}
									string text = this.ScanCapname();
									if (!this.IsCaptureName(text))
									{
										throw this.MakeException(SR.Format("Reference to undefined group name {0}.", text));
									}
									num5 = this.CaptureSlotFromName(text);
									if (this.CharsRight() > 0 && this.RightChar() != c)
									{
										throw this.MakeException("Invalid group name: Group names must begin with a word character.");
									}
								}
							}
							if ((num4 != -1 || num5 != -1) && this.CharsRight() > 0 && this.RightCharMoveRight() == c)
							{
								return new RegexNode(28, this._options, num4, num5);
							}
							goto IL_507;
						}
					}
					else
					{
						if (c != '\'')
						{
							this._options |= RegexOptions.RightToLeft;
							type = 31;
							goto IL_4FA;
						}
						goto IL_507;
					}
					IL_4C1:
					this.MoveLeft();
					type = 29;
					if (this._group.NType != 34)
					{
						this.ScanOptions();
					}
					if (this.CharsRight() == 0)
					{
						goto IL_507;
					}
					if ((c3 = this.RightCharMoveRight()) == ')')
					{
						return null;
					}
					if (c3 != ':')
					{
						goto IL_507;
					}
					IL_4FA:
					return new RegexNode(type, this._options);
				}
				IL_507:
				throw this.MakeException("Unrecognized grouping construct.");
			}
			if (this.UseOptionN() || this._ignoreNextParen)
			{
				this._ignoreNextParen = false;
				return new RegexNode(29, this._options);
			}
			int type2 = 28;
			RegexOptions options = this._options;
			int autocap = this._autocap;
			this._autocap = autocap + 1;
			return new RegexNode(type2, options, autocap, -1);
		}

		// Token: 0x06000EC3 RID: 3779 RVA: 0x000419F8 File Offset: 0x0003FBF8
		private void ScanBlank()
		{
			if (this.UseOptionX())
			{
				for (;;)
				{
					if (this.CharsRight() <= 0 || !RegexParser.IsSpace(this.RightChar()))
					{
						if (this.CharsRight() == 0)
						{
							return;
						}
						if (this.RightChar() == '#')
						{
							while (this.CharsRight() > 0)
							{
								if (this.RightChar() == '\n')
								{
									break;
								}
								this.MoveRight();
							}
						}
						else
						{
							if (this.CharsRight() < 3 || this.RightChar(2) != '#' || this.RightChar(1) != '?' || this.RightChar() != '(')
							{
								return;
							}
							while (this.CharsRight() > 0 && this.RightChar() != ')')
							{
								this.MoveRight();
							}
							if (this.CharsRight() == 0)
							{
								break;
							}
							this.MoveRight();
						}
					}
					else
					{
						this.MoveRight();
					}
				}
				throw this.MakeException("Unterminated (?#...) comment.");
			}
			while (this.CharsRight() >= 3 && this.RightChar(2) == '#' && this.RightChar(1) == '?' && this.RightChar() == '(')
			{
				while (this.CharsRight() > 0 && this.RightChar() != ')')
				{
					this.MoveRight();
				}
				if (this.CharsRight() == 0)
				{
					throw this.MakeException("Unterminated (?#...) comment.");
				}
				this.MoveRight();
			}
		}

		// Token: 0x06000EC4 RID: 3780 RVA: 0x00041B30 File Offset: 0x0003FD30
		private RegexNode ScanBackslash(bool scanOnly)
		{
			if (this.CharsRight() == 0)
			{
				throw this.MakeException("Illegal \\ at end of pattern.");
			}
			char c2;
			char c = c2 = this.RightChar();
			if (c2 <= 'Z')
			{
				if (c2 <= 'P')
				{
					switch (c2)
					{
					case 'A':
					case 'B':
					case 'G':
						break;
					case 'C':
					case 'E':
					case 'F':
						goto IL_274;
					case 'D':
						this.MoveRight();
						if (scanOnly)
						{
							return null;
						}
						if (this.UseOptionE())
						{
							return new RegexNode(11, this._options, "\u0001\u0002\00:");
						}
						return new RegexNode(11, this._options, RegexCharClass.NotDigitClass);
					default:
						if (c2 != 'P')
						{
							goto IL_274;
						}
						goto IL_21B;
					}
				}
				else if (c2 != 'S')
				{
					if (c2 != 'W')
					{
						if (c2 != 'Z')
						{
							goto IL_274;
						}
					}
					else
					{
						this.MoveRight();
						if (scanOnly)
						{
							return null;
						}
						if (this.UseOptionE())
						{
							return new RegexNode(11, this._options, "\u0001\n\00:A[_`a{İı");
						}
						return new RegexNode(11, this._options, RegexCharClass.NotWordClass);
					}
				}
				else
				{
					this.MoveRight();
					if (scanOnly)
					{
						return null;
					}
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\u0001\u0004\0\t\u000e !");
					}
					return new RegexNode(11, this._options, RegexCharClass.NotSpaceClass);
				}
			}
			else if (c2 <= 'p')
			{
				if (c2 != 'b')
				{
					if (c2 != 'd')
					{
						if (c2 != 'p')
						{
							goto IL_274;
						}
						goto IL_21B;
					}
					else
					{
						this.MoveRight();
						if (scanOnly)
						{
							return null;
						}
						if (this.UseOptionE())
						{
							return new RegexNode(11, this._options, "\0\u0002\00:");
						}
						return new RegexNode(11, this._options, RegexCharClass.DigitClass);
					}
				}
			}
			else if (c2 != 's')
			{
				if (c2 != 'w')
				{
					if (c2 != 'z')
					{
						goto IL_274;
					}
				}
				else
				{
					this.MoveRight();
					if (scanOnly)
					{
						return null;
					}
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\0\n\00:A[_`a{İı");
					}
					return new RegexNode(11, this._options, RegexCharClass.WordClass);
				}
			}
			else
			{
				this.MoveRight();
				if (scanOnly)
				{
					return null;
				}
				if (this.UseOptionE())
				{
					return new RegexNode(11, this._options, "\0\u0004\0\t\u000e !");
				}
				return new RegexNode(11, this._options, RegexCharClass.SpaceClass);
			}
			this.MoveRight();
			if (scanOnly)
			{
				return null;
			}
			return new RegexNode(this.TypeFromCode(c), this._options);
			IL_21B:
			this.MoveRight();
			if (scanOnly)
			{
				return null;
			}
			RegexCharClass regexCharClass = new RegexCharClass();
			regexCharClass.AddCategoryFromName(this.ParseProperty(), c != 'p', this.UseOptionI(), this._pattern);
			if (this.UseOptionI())
			{
				regexCharClass.AddLowercase(this._culture);
			}
			return new RegexNode(11, this._options, regexCharClass.ToStringClass());
			IL_274:
			return this.ScanBasicBackslash(scanOnly);
		}

		// Token: 0x06000EC5 RID: 3781 RVA: 0x00041DB8 File Offset: 0x0003FFB8
		private RegexNode ScanBasicBackslash(bool scanOnly)
		{
			if (this.CharsRight() == 0)
			{
				throw this.MakeException("Illegal \\ at end of pattern.");
			}
			bool flag = false;
			char c = '\0';
			int pos = this.Textpos();
			char c2 = this.RightChar();
			if (c2 == 'k')
			{
				if (this.CharsRight() >= 2)
				{
					this.MoveRight();
					c2 = this.RightCharMoveRight();
					if (c2 == '<' || c2 == '\'')
					{
						flag = true;
						c = ((c2 == '\'') ? '\'' : '>');
					}
				}
				if (!flag || this.CharsRight() <= 0)
				{
					throw this.MakeException("Malformed \\k<...> named back reference.");
				}
				c2 = this.RightChar();
			}
			else if ((c2 == '<' || c2 == '\'') && this.CharsRight() > 1)
			{
				flag = true;
				c = ((c2 == '\'') ? '\'' : '>');
				this.MoveRight();
				c2 = this.RightChar();
			}
			if (flag && c2 >= '0' && c2 <= '9')
			{
				int num = this.ScanDecimal();
				if (this.CharsRight() > 0 && this.RightCharMoveRight() == c)
				{
					if (scanOnly)
					{
						return null;
					}
					if (this.IsCaptureSlot(num))
					{
						return new RegexNode(13, this._options, num);
					}
					throw this.MakeException(SR.Format("Reference to undefined group number {0}.", num.ToString(CultureInfo.CurrentCulture)));
				}
			}
			else if (!flag && c2 >= '1' && c2 <= '9')
			{
				if (this.UseOptionE())
				{
					int num2 = -1;
					int i = (int)(c2 - '0');
					int num3 = this.Textpos() - 1;
					while (i <= this._captop)
					{
						if (this.IsCaptureSlot(i) && (this._caps == null || (int)this._caps[i] < num3))
						{
							num2 = i;
						}
						this.MoveRight();
						if (this.CharsRight() == 0 || (c2 = this.RightChar()) < '0' || c2 > '9')
						{
							break;
						}
						i = i * 10 + (int)(c2 - '0');
					}
					if (num2 >= 0)
					{
						if (!scanOnly)
						{
							return new RegexNode(13, this._options, num2);
						}
						return null;
					}
				}
				else
				{
					int num4 = this.ScanDecimal();
					if (scanOnly)
					{
						return null;
					}
					if (this.IsCaptureSlot(num4))
					{
						return new RegexNode(13, this._options, num4);
					}
					if (num4 <= 9)
					{
						throw this.MakeException(SR.Format("Reference to undefined group number {0}.", num4.ToString(CultureInfo.CurrentCulture)));
					}
				}
			}
			else if (flag && RegexCharClass.IsWordChar(c2))
			{
				string text = this.ScanCapname();
				if (this.CharsRight() > 0 && this.RightCharMoveRight() == c)
				{
					if (scanOnly)
					{
						return null;
					}
					if (this.IsCaptureName(text))
					{
						return new RegexNode(13, this._options, this.CaptureSlotFromName(text));
					}
					throw this.MakeException(SR.Format("Reference to undefined group name {0}.", text));
				}
			}
			this.Textto(pos);
			c2 = this.ScanCharEscape();
			if (this.UseOptionI())
			{
				c2 = this._culture.TextInfo.ToLower(c2);
			}
			if (!scanOnly)
			{
				return new RegexNode(9, this._options, c2);
			}
			return null;
		}

		// Token: 0x06000EC6 RID: 3782 RVA: 0x00042074 File Offset: 0x00040274
		private RegexNode ScanDollar()
		{
			if (this.CharsRight() == 0)
			{
				return new RegexNode(9, this._options, '$');
			}
			char c = this.RightChar();
			int num = this.Textpos();
			int pos = num;
			bool flag;
			if (c == '{' && this.CharsRight() > 1)
			{
				flag = true;
				this.MoveRight();
				c = this.RightChar();
			}
			else
			{
				flag = false;
			}
			if (c >= '0' && c <= '9')
			{
				if (!flag && this.UseOptionE())
				{
					int num2 = -1;
					int num3 = (int)(c - '0');
					this.MoveRight();
					if (this.IsCaptureSlot(num3))
					{
						num2 = num3;
						pos = this.Textpos();
					}
					while (this.CharsRight() > 0 && (c = this.RightChar()) >= '0' && c <= '9')
					{
						int num4 = (int)(c - '0');
						if (num3 > 214748364 || (num3 == 214748364 && num4 > 7))
						{
							throw this.MakeException("Capture group numbers must be less than or equal to Int32.MaxValue.");
						}
						num3 = num3 * 10 + num4;
						this.MoveRight();
						if (this.IsCaptureSlot(num3))
						{
							num2 = num3;
							pos = this.Textpos();
						}
					}
					this.Textto(pos);
					if (num2 >= 0)
					{
						return new RegexNode(13, this._options, num2);
					}
				}
				else
				{
					int num5 = this.ScanDecimal();
					if ((!flag || (this.CharsRight() > 0 && this.RightCharMoveRight() == '}')) && this.IsCaptureSlot(num5))
					{
						return new RegexNode(13, this._options, num5);
					}
				}
			}
			else if (flag && RegexCharClass.IsWordChar(c))
			{
				string capname = this.ScanCapname();
				if (this.CharsRight() > 0 && this.RightCharMoveRight() == '}' && this.IsCaptureName(capname))
				{
					return new RegexNode(13, this._options, this.CaptureSlotFromName(capname));
				}
			}
			else if (!flag)
			{
				int num6 = 1;
				if (c <= '+')
				{
					switch (c)
					{
					case '$':
						this.MoveRight();
						return new RegexNode(9, this._options, '$');
					case '%':
						break;
					case '&':
						num6 = 0;
						break;
					case '\'':
						num6 = -2;
						break;
					default:
						if (c == '+')
						{
							num6 = -3;
						}
						break;
					}
				}
				else if (c != '_')
				{
					if (c == '`')
					{
						num6 = -1;
					}
				}
				else
				{
					num6 = -4;
				}
				if (num6 != 1)
				{
					this.MoveRight();
					return new RegexNode(13, this._options, num6);
				}
			}
			this.Textto(num);
			return new RegexNode(9, this._options, '$');
		}

		// Token: 0x06000EC7 RID: 3783 RVA: 0x000422C0 File Offset: 0x000404C0
		private string ScanCapname()
		{
			int num = this.Textpos();
			while (this.CharsRight() > 0)
			{
				if (!RegexCharClass.IsWordChar(this.RightCharMoveRight()))
				{
					this.MoveLeft();
					break;
				}
			}
			return this._pattern.Substring(num, this.Textpos() - num);
		}

		// Token: 0x06000EC8 RID: 3784 RVA: 0x00042308 File Offset: 0x00040508
		private char ScanOctal()
		{
			int num = 3;
			if (num > this.CharsRight())
			{
				num = this.CharsRight();
			}
			int num2 = 0;
			int num3;
			while (num > 0 && (num3 = (int)(this.RightChar() - '0')) <= 7)
			{
				this.MoveRight();
				num2 *= 8;
				num2 += num3;
				if (this.UseOptionE() && num2 >= 32)
				{
					break;
				}
				num--;
			}
			num2 &= 255;
			return (char)num2;
		}

		// Token: 0x06000EC9 RID: 3785 RVA: 0x00042368 File Offset: 0x00040568
		private int ScanDecimal()
		{
			int num = 0;
			int num2;
			while (this.CharsRight() > 0 && (num2 = (int)((ushort)(this.RightChar() - '0'))) <= 9)
			{
				this.MoveRight();
				if (num > 214748364 || (num == 214748364 && num2 > 7))
				{
					throw this.MakeException("Capture group numbers must be less than or equal to Int32.MaxValue.");
				}
				num *= 10;
				num += num2;
			}
			return num;
		}

		// Token: 0x06000ECA RID: 3786 RVA: 0x000423C4 File Offset: 0x000405C4
		private char ScanHex(int c)
		{
			int num = 0;
			if (this.CharsRight() >= c)
			{
				int num2;
				while (c > 0 && (num2 = RegexParser.HexDigit(this.RightCharMoveRight())) >= 0)
				{
					num *= 16;
					num += num2;
					c--;
				}
			}
			if (c > 0)
			{
				throw this.MakeException("Insufficient hexadecimal digits.");
			}
			return (char)num;
		}

		// Token: 0x06000ECB RID: 3787 RVA: 0x00042414 File Offset: 0x00040614
		private static int HexDigit(char ch)
		{
			int num;
			if ((num = (int)(ch - '0')) <= 9)
			{
				return num;
			}
			if ((num = (int)(ch - 'a')) <= 5)
			{
				return num + 10;
			}
			if ((num = (int)(ch - 'A')) <= 5)
			{
				return num + 10;
			}
			return -1;
		}

		// Token: 0x06000ECC RID: 3788 RVA: 0x0004244C File Offset: 0x0004064C
		private char ScanControl()
		{
			if (this.CharsRight() <= 0)
			{
				throw this.MakeException("Missing control character.");
			}
			char c = this.RightCharMoveRight();
			if (c >= 'a' && c <= 'z')
			{
				c -= ' ';
			}
			if ((c -= '@') < ' ')
			{
				return c;
			}
			throw this.MakeException("Unrecognized control character.");
		}

		// Token: 0x06000ECD RID: 3789 RVA: 0x0004249D File Offset: 0x0004069D
		private bool IsOnlyTopOption(RegexOptions option)
		{
			return option == RegexOptions.RightToLeft || option == RegexOptions.CultureInvariant || option == RegexOptions.ECMAScript;
		}

		// Token: 0x06000ECE RID: 3790 RVA: 0x000424B8 File Offset: 0x000406B8
		private void ScanOptions()
		{
			bool flag = false;
			while (this.CharsRight() > 0)
			{
				char c = this.RightChar();
				if (c == '-')
				{
					flag = true;
				}
				else if (c == '+')
				{
					flag = false;
				}
				else
				{
					RegexOptions regexOptions = RegexParser.OptionFromCode(c);
					if (regexOptions == RegexOptions.None || this.IsOnlyTopOption(regexOptions))
					{
						return;
					}
					if (flag)
					{
						this._options &= ~regexOptions;
					}
					else
					{
						this._options |= regexOptions;
					}
				}
				this.MoveRight();
			}
		}

		// Token: 0x06000ECF RID: 3791 RVA: 0x00042528 File Offset: 0x00040728
		private char ScanCharEscape()
		{
			char c = this.RightCharMoveRight();
			if (c >= '0' && c <= '7')
			{
				this.MoveLeft();
				return this.ScanOctal();
			}
			switch (c)
			{
			case 'a':
				return '\a';
			case 'b':
				return '\b';
			case 'c':
				return this.ScanControl();
			case 'd':
				break;
			case 'e':
				return '\u001b';
			case 'f':
				return '\f';
			default:
				switch (c)
				{
				case 'n':
					return '\n';
				case 'r':
					return '\r';
				case 't':
					return '\t';
				case 'u':
					return this.ScanHex(4);
				case 'v':
					return '\v';
				case 'x':
					return this.ScanHex(2);
				}
				break;
			}
			if (!this.UseOptionE() && RegexCharClass.IsWordChar(c))
			{
				throw this.MakeException(SR.Format("Unrecognized escape sequence \\{0}.", c.ToString()));
			}
			return c;
		}

		// Token: 0x06000ED0 RID: 3792 RVA: 0x00042604 File Offset: 0x00040804
		private string ParseProperty()
		{
			if (this.CharsRight() < 3)
			{
				throw this.MakeException("Incomplete \\p{X} character escape.");
			}
			char c = this.RightCharMoveRight();
			if (c != '{')
			{
				throw this.MakeException("Malformed \\p{X} character escape.");
			}
			int num = this.Textpos();
			while (this.CharsRight() > 0)
			{
				c = this.RightCharMoveRight();
				if (!RegexCharClass.IsWordChar(c) && c != '-')
				{
					this.MoveLeft();
					break;
				}
			}
			string result = this._pattern.Substring(num, this.Textpos() - num);
			if (this.CharsRight() == 0 || this.RightCharMoveRight() != '}')
			{
				throw this.MakeException("Incomplete \\p{X} character escape.");
			}
			return result;
		}

		// Token: 0x06000ED1 RID: 3793 RVA: 0x000426A0 File Offset: 0x000408A0
		private int TypeFromCode(char ch)
		{
			if (ch <= 'G')
			{
				if (ch == 'A')
				{
					return 18;
				}
				if (ch != 'B')
				{
					if (ch == 'G')
					{
						return 19;
					}
				}
				else
				{
					if (!this.UseOptionE())
					{
						return 17;
					}
					return 42;
				}
			}
			else
			{
				if (ch == 'Z')
				{
					return 20;
				}
				if (ch != 'b')
				{
					if (ch == 'z')
					{
						return 21;
					}
				}
				else
				{
					if (!this.UseOptionE())
					{
						return 16;
					}
					return 41;
				}
			}
			return 22;
		}

		// Token: 0x06000ED2 RID: 3794 RVA: 0x00042700 File Offset: 0x00040900
		private static RegexOptions OptionFromCode(char ch)
		{
			if (ch >= 'A' && ch <= 'Z')
			{
				ch += ' ';
			}
			if (ch <= 'i')
			{
				if (ch == 'e')
				{
					return RegexOptions.ECMAScript;
				}
				if (ch == 'i')
				{
					return RegexOptions.IgnoreCase;
				}
			}
			else
			{
				switch (ch)
				{
				case 'm':
					return RegexOptions.Multiline;
				case 'n':
					return RegexOptions.ExplicitCapture;
				case 'o':
				case 'p':
				case 'q':
					break;
				case 'r':
					return RegexOptions.RightToLeft;
				case 's':
					return RegexOptions.Singleline;
				default:
					if (ch == 'x')
					{
						return RegexOptions.IgnorePatternWhitespace;
					}
					break;
				}
			}
			return RegexOptions.None;
		}

		// Token: 0x06000ED3 RID: 3795 RVA: 0x00042774 File Offset: 0x00040974
		private void CountCaptures()
		{
			this.NoteCaptureSlot(0, 0);
			this._autocap = 1;
			while (this.CharsRight() > 0)
			{
				int pos = this.Textpos();
				char c = this.RightCharMoveRight();
				if (c <= '(')
				{
					if (c != '#')
					{
						if (c == '(')
						{
							if (this.CharsRight() >= 2 && this.RightChar(1) == '#' && this.RightChar() == '?')
							{
								this.MoveLeft();
								this.ScanBlank();
							}
							else
							{
								this.PushOptions();
								if (this.CharsRight() > 0 && this.RightChar() == '?')
								{
									this.MoveRight();
									if (this.CharsRight() > 1 && (this.RightChar() == '<' || this.RightChar() == '\''))
									{
										this.MoveRight();
										c = this.RightChar();
										if (c != '0' && RegexCharClass.IsWordChar(c))
										{
											if (c >= '1' && c <= '9')
											{
												this.NoteCaptureSlot(this.ScanDecimal(), pos);
											}
											else
											{
												this.NoteCaptureName(this.ScanCapname(), pos);
											}
										}
									}
									else
									{
										this.ScanOptions();
										if (this.CharsRight() > 0)
										{
											if (this.RightChar() == ')')
											{
												this.MoveRight();
												this.PopKeepOptions();
											}
											else if (this.RightChar() == '(')
											{
												this._ignoreNextParen = true;
												continue;
											}
										}
									}
								}
								else if (!this.UseOptionN() && !this._ignoreNextParen)
								{
									int autocap = this._autocap;
									this._autocap = autocap + 1;
									this.NoteCaptureSlot(autocap, pos);
								}
							}
							this._ignoreNextParen = false;
						}
					}
					else if (this.UseOptionX())
					{
						this.MoveLeft();
						this.ScanBlank();
					}
				}
				else if (c != ')')
				{
					if (c != '[')
					{
						if (c == '\\' && this.CharsRight() > 0)
						{
							this.ScanBackslash(true);
						}
					}
					else
					{
						this.ScanCharClass(false, true);
					}
				}
				else if (!this.EmptyOptionsStack())
				{
					this.PopOptions();
				}
			}
			this.AssignNameSlots();
		}

		// Token: 0x06000ED4 RID: 3796 RVA: 0x00042958 File Offset: 0x00040B58
		private void NoteCaptureSlot(int i, int pos)
		{
			if (!this._caps.ContainsKey(i))
			{
				this._caps.Add(i, pos);
				this._capcount++;
				if (this._captop <= i)
				{
					if (i == 2147483647)
					{
						this._captop = i;
						return;
					}
					this._captop = i + 1;
				}
			}
		}

		// Token: 0x06000ED5 RID: 3797 RVA: 0x000429C0 File Offset: 0x00040BC0
		private void NoteCaptureName(string name, int pos)
		{
			if (this._capnames == null)
			{
				this._capnames = new Hashtable();
				this._capnamelist = new List<string>();
			}
			if (!this._capnames.ContainsKey(name))
			{
				this._capnames.Add(name, pos);
				this._capnamelist.Add(name);
			}
		}

		// Token: 0x06000ED6 RID: 3798 RVA: 0x00042A17 File Offset: 0x00040C17
		private void NoteCaptures(Hashtable caps, int capsize, Hashtable capnames)
		{
			this._caps = caps;
			this._capsize = capsize;
			this._capnames = capnames;
		}

		// Token: 0x06000ED7 RID: 3799 RVA: 0x00042A30 File Offset: 0x00040C30
		private void AssignNameSlots()
		{
			if (this._capnames != null)
			{
				for (int i = 0; i < this._capnamelist.Count; i++)
				{
					while (this.IsCaptureSlot(this._autocap))
					{
						this._autocap++;
					}
					string key = this._capnamelist[i];
					int pos = (int)this._capnames[key];
					this._capnames[key] = this._autocap;
					this.NoteCaptureSlot(this._autocap, pos);
					this._autocap++;
				}
			}
			if (this._capcount < this._captop)
			{
				this._capnumlist = new int[this._capcount];
				int num = 0;
				IDictionaryEnumerator enumerator = this._caps.GetEnumerator();
				while (enumerator.MoveNext())
				{
					this._capnumlist[num++] = (int)enumerator.Key;
				}
				Array.Sort<int>(this._capnumlist, Comparer<int>.Default);
			}
			if (this._capnames != null || this._capnumlist != null)
			{
				int num2 = 0;
				List<string> list;
				int num3;
				if (this._capnames == null)
				{
					list = null;
					this._capnames = new Hashtable();
					this._capnamelist = new List<string>();
					num3 = -1;
				}
				else
				{
					list = this._capnamelist;
					this._capnamelist = new List<string>();
					num3 = (int)this._capnames[list[0]];
				}
				for (int j = 0; j < this._capcount; j++)
				{
					int num4 = (this._capnumlist == null) ? j : this._capnumlist[j];
					if (num3 == num4)
					{
						this._capnamelist.Add(list[num2++]);
						num3 = ((num2 == list.Count) ? -1 : ((int)this._capnames[list[num2]]));
					}
					else
					{
						string text = Convert.ToString(num4, this._culture);
						this._capnamelist.Add(text);
						this._capnames[text] = num4;
					}
				}
			}
		}

		// Token: 0x06000ED8 RID: 3800 RVA: 0x00042C41 File Offset: 0x00040E41
		private int CaptureSlotFromName(string capname)
		{
			return (int)this._capnames[capname];
		}

		// Token: 0x06000ED9 RID: 3801 RVA: 0x00042C54 File Offset: 0x00040E54
		private bool IsCaptureSlot(int i)
		{
			if (this._caps != null)
			{
				return this._caps.ContainsKey(i);
			}
			return i >= 0 && i < this._capsize;
		}

		// Token: 0x06000EDA RID: 3802 RVA: 0x00042C7F File Offset: 0x00040E7F
		private bool IsCaptureName(string capname)
		{
			return this._capnames != null && this._capnames.ContainsKey(capname);
		}

		// Token: 0x06000EDB RID: 3803 RVA: 0x00042C97 File Offset: 0x00040E97
		private bool UseOptionN()
		{
			return (this._options & RegexOptions.ExplicitCapture) > RegexOptions.None;
		}

		// Token: 0x06000EDC RID: 3804 RVA: 0x00042CA4 File Offset: 0x00040EA4
		private bool UseOptionI()
		{
			return (this._options & RegexOptions.IgnoreCase) > RegexOptions.None;
		}

		// Token: 0x06000EDD RID: 3805 RVA: 0x00042CB1 File Offset: 0x00040EB1
		private bool UseOptionM()
		{
			return (this._options & RegexOptions.Multiline) > RegexOptions.None;
		}

		// Token: 0x06000EDE RID: 3806 RVA: 0x00042CBE File Offset: 0x00040EBE
		private bool UseOptionS()
		{
			return (this._options & RegexOptions.Singleline) > RegexOptions.None;
		}

		// Token: 0x06000EDF RID: 3807 RVA: 0x00042CCC File Offset: 0x00040ECC
		private bool UseOptionX()
		{
			return (this._options & RegexOptions.IgnorePatternWhitespace) > RegexOptions.None;
		}

		// Token: 0x06000EE0 RID: 3808 RVA: 0x00042CDA File Offset: 0x00040EDA
		private bool UseOptionE()
		{
			return (this._options & RegexOptions.ECMAScript) > RegexOptions.None;
		}

		// Token: 0x06000EE1 RID: 3809 RVA: 0x00042CEB File Offset: 0x00040EEB
		private static bool IsSpecial(char ch)
		{
			return ch <= '|' && RegexParser.s_category[(int)ch] >= 4;
		}

		// Token: 0x06000EE2 RID: 3810 RVA: 0x00042D01 File Offset: 0x00040F01
		private static bool IsStopperX(char ch)
		{
			return ch <= '|' && RegexParser.s_category[(int)ch] >= 2;
		}

		// Token: 0x06000EE3 RID: 3811 RVA: 0x00042D17 File Offset: 0x00040F17
		private static bool IsQuantifier(char ch)
		{
			return ch <= '{' && RegexParser.s_category[(int)ch] >= 5;
		}

		// Token: 0x06000EE4 RID: 3812 RVA: 0x00042D30 File Offset: 0x00040F30
		private bool IsTrueQuantifier()
		{
			int num = this.CharsRight();
			if (num == 0)
			{
				return false;
			}
			int num2 = this.Textpos();
			char c = this.CharAt(num2);
			if (c != '{')
			{
				return c <= '{' && RegexParser.s_category[(int)c] >= 5;
			}
			int num3 = num2;
			while (--num > 0 && (c = this.CharAt(++num3)) >= '0' && c <= '9')
			{
			}
			if (num == 0 || num3 - num2 == 1)
			{
				return false;
			}
			if (c == '}')
			{
				return true;
			}
			if (c != ',')
			{
				return false;
			}
			while (--num > 0 && (c = this.CharAt(++num3)) >= '0' && c <= '9')
			{
			}
			return num > 0 && c == '}';
		}

		// Token: 0x06000EE5 RID: 3813 RVA: 0x00042DD4 File Offset: 0x00040FD4
		private static bool IsSpace(char ch)
		{
			return ch <= ' ' && RegexParser.s_category[(int)ch] == 2;
		}

		// Token: 0x06000EE6 RID: 3814 RVA: 0x00042DE7 File Offset: 0x00040FE7
		private static bool IsMetachar(char ch)
		{
			return ch <= '|' && RegexParser.s_category[(int)ch] >= 1;
		}

		// Token: 0x06000EE7 RID: 3815 RVA: 0x00042E00 File Offset: 0x00041000
		private void AddConcatenate(int pos, int cch, bool isReplacement)
		{
			if (cch == 0)
			{
				return;
			}
			RegexNode newChild;
			if (cch > 1)
			{
				string text = this._pattern.Substring(pos, cch);
				if (this.UseOptionI() && !isReplacement)
				{
					StringBuilder stringBuilder = StringBuilderCache.Acquire(text.Length);
					for (int i = 0; i < text.Length; i++)
					{
						stringBuilder.Append(this._culture.TextInfo.ToLower(text[i]));
					}
					text = StringBuilderCache.GetStringAndRelease(stringBuilder);
				}
				newChild = new RegexNode(12, this._options, text);
			}
			else
			{
				char c = this._pattern[pos];
				if (this.UseOptionI() && !isReplacement)
				{
					c = this._culture.TextInfo.ToLower(c);
				}
				newChild = new RegexNode(9, this._options, c);
			}
			this._concatenation.AddChild(newChild);
		}

		// Token: 0x06000EE8 RID: 3816 RVA: 0x00042ECC File Offset: 0x000410CC
		private void PushGroup()
		{
			this._group.Next = this._stack;
			this._alternation.Next = this._group;
			this._concatenation.Next = this._alternation;
			this._stack = this._concatenation;
		}

		// Token: 0x06000EE9 RID: 3817 RVA: 0x00042F18 File Offset: 0x00041118
		private void PopGroup()
		{
			this._concatenation = this._stack;
			this._alternation = this._concatenation.Next;
			this._group = this._alternation.Next;
			this._stack = this._group.Next;
			if (this._group.Type() == 34 && this._group.ChildCount() == 0)
			{
				if (this._unit == null)
				{
					throw this.MakeException("Illegal conditional (?(...)) expression.");
				}
				this._group.AddChild(this._unit);
				this._unit = null;
			}
		}

		// Token: 0x06000EEA RID: 3818 RVA: 0x00042FAC File Offset: 0x000411AC
		private bool EmptyStack()
		{
			return this._stack == null;
		}

		// Token: 0x06000EEB RID: 3819 RVA: 0x00042FB7 File Offset: 0x000411B7
		private void StartGroup(RegexNode openGroup)
		{
			this._group = openGroup;
			this._alternation = new RegexNode(24, this._options);
			this._concatenation = new RegexNode(25, this._options);
		}

		// Token: 0x06000EEC RID: 3820 RVA: 0x00042FE8 File Offset: 0x000411E8
		private void AddAlternate()
		{
			if (this._group.Type() == 34 || this._group.Type() == 33)
			{
				this._group.AddChild(this._concatenation.ReverseLeft());
			}
			else
			{
				this._alternation.AddChild(this._concatenation.ReverseLeft());
			}
			this._concatenation = new RegexNode(25, this._options);
		}

		// Token: 0x06000EED RID: 3821 RVA: 0x00043054 File Offset: 0x00041254
		private void AddConcatenate()
		{
			this._concatenation.AddChild(this._unit);
			this._unit = null;
		}

		// Token: 0x06000EEE RID: 3822 RVA: 0x0004306E File Offset: 0x0004126E
		private void AddConcatenate(bool lazy, int min, int max)
		{
			this._concatenation.AddChild(this._unit.MakeQuantifier(lazy, min, max));
			this._unit = null;
		}

		// Token: 0x06000EEF RID: 3823 RVA: 0x00043090 File Offset: 0x00041290
		private RegexNode Unit()
		{
			return this._unit;
		}

		// Token: 0x06000EF0 RID: 3824 RVA: 0x00043098 File Offset: 0x00041298
		private void AddUnitOne(char ch)
		{
			if (this.UseOptionI())
			{
				ch = this._culture.TextInfo.ToLower(ch);
			}
			this._unit = new RegexNode(9, this._options, ch);
		}

		// Token: 0x06000EF1 RID: 3825 RVA: 0x000430C9 File Offset: 0x000412C9
		private void AddUnitNotone(char ch)
		{
			if (this.UseOptionI())
			{
				ch = this._culture.TextInfo.ToLower(ch);
			}
			this._unit = new RegexNode(10, this._options, ch);
		}

		// Token: 0x06000EF2 RID: 3826 RVA: 0x000430FA File Offset: 0x000412FA
		private void AddUnitSet(string cc)
		{
			this._unit = new RegexNode(11, this._options, cc);
		}

		// Token: 0x06000EF3 RID: 3827 RVA: 0x00043110 File Offset: 0x00041310
		private void AddUnitNode(RegexNode node)
		{
			this._unit = node;
		}

		// Token: 0x06000EF4 RID: 3828 RVA: 0x00043119 File Offset: 0x00041319
		private void AddUnitType(int type)
		{
			this._unit = new RegexNode(type, this._options);
		}

		// Token: 0x06000EF5 RID: 3829 RVA: 0x00043130 File Offset: 0x00041330
		private void AddGroup()
		{
			if (this._group.Type() == 34 || this._group.Type() == 33)
			{
				this._group.AddChild(this._concatenation.ReverseLeft());
				if ((this._group.Type() == 33 && this._group.ChildCount() > 2) || this._group.ChildCount() > 3)
				{
					throw this.MakeException("Too many | in (?()|).");
				}
			}
			else
			{
				this._alternation.AddChild(this._concatenation.ReverseLeft());
				this._group.AddChild(this._alternation);
			}
			this._unit = this._group;
		}

		// Token: 0x06000EF6 RID: 3830 RVA: 0x000431DB File Offset: 0x000413DB
		private void PushOptions()
		{
			this._optionsStack.Add(this._options);
		}

		// Token: 0x06000EF7 RID: 3831 RVA: 0x000431EE File Offset: 0x000413EE
		private void PopOptions()
		{
			this._options = this._optionsStack[this._optionsStack.Count - 1];
			this._optionsStack.RemoveAt(this._optionsStack.Count - 1);
		}

		// Token: 0x06000EF8 RID: 3832 RVA: 0x00043226 File Offset: 0x00041426
		private bool EmptyOptionsStack()
		{
			return this._optionsStack.Count == 0;
		}

		// Token: 0x06000EF9 RID: 3833 RVA: 0x00043236 File Offset: 0x00041436
		private void PopKeepOptions()
		{
			this._optionsStack.RemoveAt(this._optionsStack.Count - 1);
		}

		// Token: 0x06000EFA RID: 3834 RVA: 0x00043250 File Offset: 0x00041450
		private ArgumentException MakeException(string message)
		{
			return new ArgumentException(SR.Format("parsing \"{0}\" - {1}", this._pattern, message));
		}

		// Token: 0x06000EFB RID: 3835 RVA: 0x00043268 File Offset: 0x00041468
		private int Textpos()
		{
			return this._currentPos;
		}

		// Token: 0x06000EFC RID: 3836 RVA: 0x00043270 File Offset: 0x00041470
		private void Textto(int pos)
		{
			this._currentPos = pos;
		}

		// Token: 0x06000EFD RID: 3837 RVA: 0x0004327C File Offset: 0x0004147C
		private char RightCharMoveRight()
		{
			string pattern = this._pattern;
			int currentPos = this._currentPos;
			this._currentPos = currentPos + 1;
			return pattern[currentPos];
		}

		// Token: 0x06000EFE RID: 3838 RVA: 0x000432A5 File Offset: 0x000414A5
		private void MoveRight()
		{
			this.MoveRight(1);
		}

		// Token: 0x06000EFF RID: 3839 RVA: 0x000432AE File Offset: 0x000414AE
		private void MoveRight(int i)
		{
			this._currentPos += i;
		}

		// Token: 0x06000F00 RID: 3840 RVA: 0x000432BE File Offset: 0x000414BE
		private void MoveLeft()
		{
			this._currentPos--;
		}

		// Token: 0x06000F01 RID: 3841 RVA: 0x000432CE File Offset: 0x000414CE
		private char CharAt(int i)
		{
			return this._pattern[i];
		}

		// Token: 0x06000F02 RID: 3842 RVA: 0x000432DC File Offset: 0x000414DC
		internal char RightChar()
		{
			return this._pattern[this._currentPos];
		}

		// Token: 0x06000F03 RID: 3843 RVA: 0x000432EF File Offset: 0x000414EF
		private char RightChar(int i)
		{
			return this._pattern[this._currentPos + i];
		}

		// Token: 0x06000F04 RID: 3844 RVA: 0x00043304 File Offset: 0x00041504
		private int CharsRight()
		{
			return this._pattern.Length - this._currentPos;
		}

		// Token: 0x06000F05 RID: 3845 RVA: 0x00043318 File Offset: 0x00041518
		// Note: this type is marked as 'beforefieldinit'.
		static RegexParser()
		{
		}

		// Token: 0x04000942 RID: 2370
		private const int MaxValueDiv10 = 214748364;

		// Token: 0x04000943 RID: 2371
		private const int MaxValueMod10 = 7;

		// Token: 0x04000944 RID: 2372
		private RegexNode _stack;

		// Token: 0x04000945 RID: 2373
		private RegexNode _group;

		// Token: 0x04000946 RID: 2374
		private RegexNode _alternation;

		// Token: 0x04000947 RID: 2375
		private RegexNode _concatenation;

		// Token: 0x04000948 RID: 2376
		private RegexNode _unit;

		// Token: 0x04000949 RID: 2377
		private string _pattern;

		// Token: 0x0400094A RID: 2378
		private int _currentPos;

		// Token: 0x0400094B RID: 2379
		private CultureInfo _culture;

		// Token: 0x0400094C RID: 2380
		private int _autocap;

		// Token: 0x0400094D RID: 2381
		private int _capcount;

		// Token: 0x0400094E RID: 2382
		private int _captop;

		// Token: 0x0400094F RID: 2383
		private int _capsize;

		// Token: 0x04000950 RID: 2384
		private Hashtable _caps;

		// Token: 0x04000951 RID: 2385
		private Hashtable _capnames;

		// Token: 0x04000952 RID: 2386
		private int[] _capnumlist;

		// Token: 0x04000953 RID: 2387
		private List<string> _capnamelist;

		// Token: 0x04000954 RID: 2388
		private RegexOptions _options;

		// Token: 0x04000955 RID: 2389
		private List<RegexOptions> _optionsStack;

		// Token: 0x04000956 RID: 2390
		private bool _ignoreNextParen;

		// Token: 0x04000957 RID: 2391
		private const byte Q = 5;

		// Token: 0x04000958 RID: 2392
		private const byte S = 4;

		// Token: 0x04000959 RID: 2393
		private const byte Z = 3;

		// Token: 0x0400095A RID: 2394
		private const byte X = 2;

		// Token: 0x0400095B RID: 2395
		private const byte E = 1;

		// Token: 0x0400095C RID: 2396
		private static readonly byte[] s_category = new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			2,
			2,
			0,
			2,
			2,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			2,
			0,
			0,
			3,
			4,
			0,
			0,
			0,
			4,
			4,
			5,
			5,
			0,
			0,
			4,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			5,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			4,
			4,
			0,
			4,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			5,
			4,
			0,
			0,
			0
		};
	}
}
