﻿using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
	// Token: 0x020002FB RID: 763
	internal class XmlSerializationWriterCodeGen : XmlSerializationCodeGen
	{
		// Token: 0x06001F3E RID: 7998 RVA: 0x000C2C83 File Offset: 0x000C0E83
		internal XmlSerializationWriterCodeGen(IndentedWriter writer, TypeScope[] scopes, string access, string className) : base(writer, scopes, access, className)
		{
		}

		// Token: 0x06001F3F RID: 7999 RVA: 0x000C2C90 File Offset: 0x000C0E90
		internal void GenerateBegin()
		{
			base.Writer.Write(base.Access);
			base.Writer.Write(" class ");
			base.Writer.Write(base.ClassName);
			base.Writer.Write(" : ");
			base.Writer.Write(typeof(XmlSerializationWriter).FullName);
			base.Writer.WriteLine(" {");
			IndentedWriter writer = base.Writer;
			int i = writer.Indent;
			writer.Indent = i + 1;
			foreach (TypeScope typeScope in base.Scopes)
			{
				foreach (object obj in typeScope.TypeMappings)
				{
					TypeMapping typeMapping = (TypeMapping)obj;
					if (typeMapping is StructMapping || typeMapping is EnumMapping)
					{
						base.MethodNames.Add(typeMapping, this.NextMethodName(typeMapping.TypeDesc.Name));
					}
				}
				base.RaCodeGen.WriteReflectionInit(typeScope);
			}
			TypeScope[] scopes = base.Scopes;
			for (i = 0; i < scopes.Length; i++)
			{
				foreach (object obj2 in scopes[i].TypeMappings)
				{
					TypeMapping typeMapping2 = (TypeMapping)obj2;
					if (typeMapping2.IsSoap)
					{
						if (typeMapping2 is StructMapping)
						{
							this.WriteStructMethod((StructMapping)typeMapping2);
						}
						else if (typeMapping2 is EnumMapping)
						{
							this.WriteEnumMethod((EnumMapping)typeMapping2);
						}
					}
				}
			}
		}

		// Token: 0x06001F40 RID: 8000 RVA: 0x000C2E54 File Offset: 0x000C1054
		internal override void GenerateMethod(TypeMapping mapping)
		{
			if (base.GeneratedMethods.Contains(mapping))
			{
				return;
			}
			base.GeneratedMethods[mapping] = mapping;
			if (mapping is StructMapping)
			{
				this.WriteStructMethod((StructMapping)mapping);
				return;
			}
			if (mapping is EnumMapping)
			{
				this.WriteEnumMethod((EnumMapping)mapping);
			}
		}

		// Token: 0x06001F41 RID: 8001 RVA: 0x000C2EA8 File Offset: 0x000C10A8
		internal void GenerateEnd()
		{
			base.GenerateReferencedMethods();
			this.GenerateInitCallbacksMethod();
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F42 RID: 8002 RVA: 0x000C2EE8 File Offset: 0x000C10E8
		internal string GenerateElement(XmlMapping xmlMapping)
		{
			if (!xmlMapping.IsWriteable)
			{
				return null;
			}
			if (!xmlMapping.GenerateSerializer)
			{
				throw new ArgumentException(Res.GetString("Internal error."), "xmlMapping");
			}
			if (xmlMapping is XmlTypeMapping)
			{
				return this.GenerateTypeElement((XmlTypeMapping)xmlMapping);
			}
			if (xmlMapping is XmlMembersMapping)
			{
				return this.GenerateMembersElement((XmlMembersMapping)xmlMapping);
			}
			throw new ArgumentException(Res.GetString("Internal error."), "xmlMapping");
		}

		// Token: 0x06001F43 RID: 8003 RVA: 0x000C2F5C File Offset: 0x000C115C
		private void GenerateInitCallbacksMethod()
		{
			base.Writer.WriteLine();
			base.Writer.WriteLine("protected override void InitCallbacks() {");
			IndentedWriter writer = base.Writer;
			int i = writer.Indent;
			writer.Indent = i + 1;
			TypeScope[] scopes = base.Scopes;
			for (i = 0; i < scopes.Length; i++)
			{
				foreach (object obj in scopes[i].TypeMappings)
				{
					TypeMapping typeMapping = (TypeMapping)obj;
					if (typeMapping.IsSoap && (typeMapping is StructMapping || typeMapping is EnumMapping) && !typeMapping.TypeDesc.IsRoot)
					{
						string s = (string)base.MethodNames[typeMapping];
						base.Writer.Write("AddWriteCallback(");
						base.Writer.Write(base.RaCodeGen.GetStringForTypeof(typeMapping.TypeDesc.CSharpName, typeMapping.TypeDesc.UseReflection));
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(typeMapping.TypeName);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(typeMapping.Namespace);
						base.Writer.Write(", new ");
						base.Writer.Write(typeof(XmlSerializationWriteCallback).FullName);
						base.Writer.Write("(this.");
						base.Writer.Write(s);
						base.Writer.WriteLine("));");
					}
				}
			}
			IndentedWriter writer2 = base.Writer;
			i = writer2.Indent;
			writer2.Indent = i - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F44 RID: 8004 RVA: 0x000C3144 File Offset: 0x000C1344
		private void WriteQualifiedNameElement(string name, string ns, object defaultValue, string source, bool nullable, bool IsSoap, TypeMapping mapping)
		{
			bool flag = defaultValue != null && defaultValue != DBNull.Value;
			if (flag)
			{
				this.WriteCheckDefault(source, defaultValue, nullable);
				base.Writer.WriteLine(" {");
				IndentedWriter writer = base.Writer;
				int indent = writer.Indent;
				writer.Indent = indent + 1;
			}
			string str = IsSoap ? "Encoded" : "Literal";
			base.Writer.Write(nullable ? ("WriteNullableQualifiedName" + str) : "WriteElementQualifiedName");
			base.Writer.Write("(");
			base.WriteQuotedCSharpString(name);
			if (ns != null)
			{
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(ns);
			}
			base.Writer.Write(", ");
			base.Writer.Write(source);
			if (IsSoap)
			{
				base.Writer.Write(", new System.Xml.XmlQualifiedName(");
				base.WriteQuotedCSharpString(mapping.TypeName);
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(mapping.Namespace);
				base.Writer.Write(")");
			}
			base.Writer.WriteLine(");");
			if (flag)
			{
				IndentedWriter writer2 = base.Writer;
				int indent = writer2.Indent;
				writer2.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
		}

		// Token: 0x06001F45 RID: 8005 RVA: 0x000C329C File Offset: 0x000C149C
		private void WriteEnumValue(EnumMapping mapping, string source)
		{
			string s = base.ReferenceMapping(mapping);
			base.Writer.Write(s);
			base.Writer.Write("(");
			base.Writer.Write(source);
			base.Writer.Write(")");
		}

		// Token: 0x06001F46 RID: 8006 RVA: 0x000C32EC File Offset: 0x000C14EC
		private void WritePrimitiveValue(TypeDesc typeDesc, string source, bool isElement)
		{
			if (typeDesc == base.StringTypeDesc || typeDesc.FormatterName == "String")
			{
				base.Writer.Write(source);
				return;
			}
			if (!typeDesc.HasCustomFormatter)
			{
				base.Writer.Write(typeof(XmlConvert).FullName);
				base.Writer.Write(".ToString((");
				base.Writer.Write(typeDesc.CSharpName);
				base.Writer.Write(")");
				base.Writer.Write(source);
				base.Writer.Write(")");
				return;
			}
			base.Writer.Write("From");
			base.Writer.Write(typeDesc.FormatterName);
			base.Writer.Write("(");
			base.Writer.Write(source);
			base.Writer.Write(")");
		}

		// Token: 0x06001F47 RID: 8007 RVA: 0x000C33E0 File Offset: 0x000C15E0
		private void WritePrimitive(string method, string name, string ns, object defaultValue, string source, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
		{
			TypeDesc typeDesc = mapping.TypeDesc;
			bool flag = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc.HasDefaultSupport;
			if (flag)
			{
				if (mapping is EnumMapping)
				{
					base.Writer.Write("if (");
					if (mapping.TypeDesc.UseReflection)
					{
						base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue(source, mapping.TypeDesc.UseReflection));
					}
					else
					{
						base.Writer.Write(source);
					}
					base.Writer.Write(" != ");
					if (((EnumMapping)mapping).IsFlags)
					{
						base.Writer.Write("(");
						string[] array = ((string)defaultValue).Split(null);
						for (int i = 0; i < array.Length; i++)
						{
							if (array[i] != null && array[i].Length != 0)
							{
								if (i > 0)
								{
									base.Writer.WriteLine(" | ");
								}
								base.Writer.Write(base.RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, array[i], mapping.TypeDesc.UseReflection));
							}
						}
						base.Writer.Write(")");
					}
					else
					{
						base.Writer.Write(base.RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, (string)defaultValue, mapping.TypeDesc.UseReflection));
					}
					base.Writer.Write(")");
				}
				else
				{
					this.WriteCheckDefault(source, defaultValue, isNullable);
				}
				base.Writer.WriteLine(" {");
				IndentedWriter writer = base.Writer;
				int indent = writer.Indent;
				writer.Indent = indent + 1;
			}
			base.Writer.Write(method);
			base.Writer.Write("(");
			base.WriteQuotedCSharpString(name);
			if (ns != null)
			{
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(ns);
			}
			base.Writer.Write(", ");
			if (mapping is EnumMapping)
			{
				this.WriteEnumValue((EnumMapping)mapping, source);
			}
			else
			{
				this.WritePrimitiveValue(typeDesc, source, isElement);
			}
			if (writeXsiType)
			{
				base.Writer.Write(", new System.Xml.XmlQualifiedName(");
				base.WriteQuotedCSharpString(mapping.TypeName);
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(mapping.Namespace);
				base.Writer.Write(")");
			}
			base.Writer.WriteLine(");");
			if (flag)
			{
				IndentedWriter writer2 = base.Writer;
				int indent = writer2.Indent;
				writer2.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
		}

		// Token: 0x06001F48 RID: 8008 RVA: 0x000C3694 File Offset: 0x000C1894
		private void WriteTag(string methodName, string name, string ns)
		{
			base.Writer.Write(methodName);
			base.Writer.Write("(");
			base.WriteQuotedCSharpString(name);
			base.Writer.Write(", ");
			if (ns == null)
			{
				base.Writer.Write("null");
			}
			else
			{
				base.WriteQuotedCSharpString(ns);
			}
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F49 RID: 8009 RVA: 0x000C3700 File Offset: 0x000C1900
		private void WriteTag(string methodName, string name, string ns, bool writePrefixed)
		{
			base.Writer.Write(methodName);
			base.Writer.Write("(");
			base.WriteQuotedCSharpString(name);
			base.Writer.Write(", ");
			if (ns == null)
			{
				base.Writer.Write("null");
			}
			else
			{
				base.WriteQuotedCSharpString(ns);
			}
			base.Writer.Write(", null, ");
			if (writePrefixed)
			{
				base.Writer.Write("true");
			}
			else
			{
				base.Writer.Write("false");
			}
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F4A RID: 8010 RVA: 0x000C37A2 File Offset: 0x000C19A2
		private void WriteStartElement(string name, string ns, bool writePrefixed)
		{
			this.WriteTag("WriteStartElement", name, ns, writePrefixed);
		}

		// Token: 0x06001F4B RID: 8011 RVA: 0x000C37B2 File Offset: 0x000C19B2
		private void WriteEndElement()
		{
			base.Writer.WriteLine("WriteEndElement();");
		}

		// Token: 0x06001F4C RID: 8012 RVA: 0x000C37C4 File Offset: 0x000C19C4
		private void WriteEndElement(string source)
		{
			base.Writer.Write("WriteEndElement(");
			base.Writer.Write(source);
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F4D RID: 8013 RVA: 0x000C37F2 File Offset: 0x000C19F2
		private void WriteEncodedNullTag(string name, string ns)
		{
			this.WriteTag("WriteNullTagEncoded", name, ns);
		}

		// Token: 0x06001F4E RID: 8014 RVA: 0x000C3801 File Offset: 0x000C1A01
		private void WriteLiteralNullTag(string name, string ns)
		{
			this.WriteTag("WriteNullTagLiteral", name, ns);
		}

		// Token: 0x06001F4F RID: 8015 RVA: 0x000C3810 File Offset: 0x000C1A10
		private void WriteEmptyTag(string name, string ns)
		{
			this.WriteTag("WriteEmptyTag", name, ns);
		}

		// Token: 0x06001F50 RID: 8016 RVA: 0x000C3820 File Offset: 0x000C1A20
		private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
		{
			ElementAccessor accessor = xmlMembersMapping.Accessor;
			MembersMapping membersMapping = (MembersMapping)accessor.Mapping;
			bool hasWrapperElement = membersMapping.HasWrapperElement;
			bool writeAccessors = membersMapping.WriteAccessors;
			bool flag = xmlMembersMapping.IsSoap && writeAccessors;
			string text = this.NextMethodName(accessor.Name);
			base.Writer.WriteLine();
			base.Writer.Write("public void ");
			base.Writer.Write(text);
			base.Writer.WriteLine("(object[] p) {");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			base.Writer.WriteLine("WriteStartDocument();");
			if (!membersMapping.IsSoap)
			{
				base.Writer.WriteLine("TopLevelElement();");
			}
			base.Writer.WriteLine("int pLength = p.Length;");
			if (hasWrapperElement)
			{
				this.WriteStartElement(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "", membersMapping.IsSoap);
				int num = this.FindXmlnsIndex(membersMapping.Members);
				if (num >= 0)
				{
					MemberMapping memberMapping = membersMapping.Members[num];
					string source = string.Concat(new string[]
					{
						"((",
						typeof(XmlSerializerNamespaces).FullName,
						")p[",
						num.ToString(CultureInfo.InvariantCulture),
						"])"
					});
					base.Writer.Write("if (pLength > ");
					base.Writer.Write(num.ToString(CultureInfo.InvariantCulture));
					base.Writer.WriteLine(") {");
					IndentedWriter writer2 = base.Writer;
					indent = writer2.Indent;
					writer2.Indent = indent + 1;
					this.WriteNamespaces(source);
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				for (int i = 0; i < membersMapping.Members.Length; i++)
				{
					MemberMapping memberMapping2 = membersMapping.Members[i];
					if (memberMapping2.Attribute != null && !memberMapping2.Ignore)
					{
						string source2 = "p[" + i.ToString(CultureInfo.InvariantCulture) + "]";
						string text2 = null;
						int num2 = 0;
						if (memberMapping2.CheckSpecified != SpecifiedAccessor.None)
						{
							string b = memberMapping2.Name + "Specified";
							for (int j = 0; j < membersMapping.Members.Length; j++)
							{
								if (membersMapping.Members[j].Name == b)
								{
									text2 = "((bool) p[" + j.ToString(CultureInfo.InvariantCulture) + "])";
									num2 = j;
									break;
								}
							}
						}
						base.Writer.Write("if (pLength > ");
						base.Writer.Write(i.ToString(CultureInfo.InvariantCulture));
						base.Writer.WriteLine(") {");
						IndentedWriter writer4 = base.Writer;
						indent = writer4.Indent;
						writer4.Indent = indent + 1;
						if (text2 != null)
						{
							base.Writer.Write("if (pLength <= ");
							base.Writer.Write(num2.ToString(CultureInfo.InvariantCulture));
							base.Writer.Write(" || ");
							base.Writer.Write(text2);
							base.Writer.WriteLine(") {");
							IndentedWriter writer5 = base.Writer;
							indent = writer5.Indent;
							writer5.Indent = indent + 1;
						}
						this.WriteMember(source2, memberMapping2.Attribute, memberMapping2.TypeDesc, "p");
						if (text2 != null)
						{
							IndentedWriter writer6 = base.Writer;
							indent = writer6.Indent;
							writer6.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
						IndentedWriter writer7 = base.Writer;
						indent = writer7.Indent;
						writer7.Indent = indent - 1;
						base.Writer.WriteLine("}");
					}
				}
			}
			for (int k = 0; k < membersMapping.Members.Length; k++)
			{
				MemberMapping memberMapping3 = membersMapping.Members[k];
				if (memberMapping3.Xmlns == null && !memberMapping3.Ignore)
				{
					string text3 = null;
					int num3 = 0;
					if (memberMapping3.CheckSpecified != SpecifiedAccessor.None)
					{
						string b2 = memberMapping3.Name + "Specified";
						for (int l = 0; l < membersMapping.Members.Length; l++)
						{
							if (membersMapping.Members[l].Name == b2)
							{
								text3 = "((bool) p[" + l.ToString(CultureInfo.InvariantCulture) + "])";
								num3 = l;
								break;
							}
						}
					}
					base.Writer.Write("if (pLength > ");
					base.Writer.Write(k.ToString(CultureInfo.InvariantCulture));
					base.Writer.WriteLine(") {");
					IndentedWriter writer8 = base.Writer;
					indent = writer8.Indent;
					writer8.Indent = indent + 1;
					if (text3 != null)
					{
						base.Writer.Write("if (pLength <= ");
						base.Writer.Write(num3.ToString(CultureInfo.InvariantCulture));
						base.Writer.Write(" || ");
						base.Writer.Write(text3);
						base.Writer.WriteLine(") {");
						IndentedWriter writer9 = base.Writer;
						indent = writer9.Indent;
						writer9.Indent = indent + 1;
					}
					string source3 = "p[" + k.ToString(CultureInfo.InvariantCulture) + "]";
					string choiceSource = null;
					if (memberMapping3.ChoiceIdentifier != null)
					{
						int m = 0;
						while (m < membersMapping.Members.Length)
						{
							if (membersMapping.Members[m].Name == memberMapping3.ChoiceIdentifier.MemberName)
							{
								if (memberMapping3.ChoiceIdentifier.Mapping.TypeDesc.UseReflection)
								{
									choiceSource = "p[" + m.ToString(CultureInfo.InvariantCulture) + "]";
									break;
								}
								choiceSource = string.Concat(new string[]
								{
									"((",
									membersMapping.Members[m].TypeDesc.CSharpName,
									")p[",
									m.ToString(CultureInfo.InvariantCulture),
									"])"
								});
								break;
							}
							else
							{
								m++;
							}
						}
					}
					if (flag && memberMapping3.IsReturnValue && memberMapping3.Elements.Length != 0)
					{
						base.Writer.Write("WriteRpcResult(");
						base.WriteQuotedCSharpString(memberMapping3.Elements[0].Name);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString("");
						base.Writer.WriteLine(");");
					}
					this.WriteMember(source3, choiceSource, memberMapping3.ElementsSortedByDerivation, memberMapping3.Text, memberMapping3.ChoiceIdentifier, memberMapping3.TypeDesc, writeAccessors || hasWrapperElement);
					if (text3 != null)
					{
						IndentedWriter writer10 = base.Writer;
						indent = writer10.Indent;
						writer10.Indent = indent - 1;
						base.Writer.WriteLine("}");
					}
					IndentedWriter writer11 = base.Writer;
					indent = writer11.Indent;
					writer11.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
			}
			if (hasWrapperElement)
			{
				this.WriteEndElement();
			}
			if (accessor.IsSoap)
			{
				if (!hasWrapperElement && !writeAccessors)
				{
					base.Writer.Write("if (pLength > ");
					base.Writer.Write(membersMapping.Members.Length.ToString(CultureInfo.InvariantCulture));
					base.Writer.WriteLine(") {");
					IndentedWriter writer12 = base.Writer;
					indent = writer12.Indent;
					writer12.Indent = indent + 1;
					this.WriteExtraMembers(membersMapping.Members.Length.ToString(CultureInfo.InvariantCulture), "pLength");
					IndentedWriter writer13 = base.Writer;
					indent = writer13.Indent;
					writer13.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				base.Writer.WriteLine("WriteReferencedElements();");
			}
			IndentedWriter writer14 = base.Writer;
			indent = writer14.Indent;
			writer14.Indent = indent - 1;
			base.Writer.WriteLine("}");
			return text;
		}

		// Token: 0x06001F51 RID: 8017 RVA: 0x000C4034 File Offset: 0x000C2234
		private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
		{
			ElementAccessor accessor = xmlTypeMapping.Accessor;
			TypeMapping mapping = accessor.Mapping;
			string text = this.NextMethodName(accessor.Name);
			base.Writer.WriteLine();
			base.Writer.Write("public void ");
			base.Writer.Write(text);
			base.Writer.WriteLine("(object o) {");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			base.Writer.WriteLine("WriteStartDocument();");
			base.Writer.WriteLine("if (o == null) {");
			IndentedWriter writer2 = base.Writer;
			indent = writer2.Indent;
			writer2.Indent = indent + 1;
			if (accessor.IsNullable)
			{
				if (mapping.IsSoap)
				{
					this.WriteEncodedNullTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
				}
				else
				{
					this.WriteLiteralNullTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
				}
			}
			else
			{
				this.WriteEmptyTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
			}
			base.Writer.WriteLine("return;");
			IndentedWriter writer3 = base.Writer;
			indent = writer3.Indent;
			writer3.Indent = indent - 1;
			base.Writer.WriteLine("}");
			if (!mapping.IsSoap && !mapping.TypeDesc.IsValueType && !mapping.TypeDesc.Type.IsPrimitive)
			{
				base.Writer.WriteLine("TopLevelElement();");
			}
			this.WriteMember("o", null, new ElementAccessor[]
			{
				accessor
			}, null, null, mapping.TypeDesc, !accessor.IsSoap);
			if (mapping.IsSoap)
			{
				base.Writer.WriteLine("WriteReferencedElements();");
			}
			IndentedWriter writer4 = base.Writer;
			indent = writer4.Indent;
			writer4.Indent = indent - 1;
			base.Writer.WriteLine("}");
			return text;
		}

		// Token: 0x06001F52 RID: 8018 RVA: 0x000C4228 File Offset: 0x000C2428
		private string NextMethodName(string name)
		{
			string str = "Write";
			int nextMethodNumber = base.NextMethodNumber + 1;
			base.NextMethodNumber = nextMethodNumber;
			return str + nextMethodNumber.ToString(null, NumberFormatInfo.InvariantInfo) + "_" + CodeIdentifier.MakeValidInternal(name);
		}

		// Token: 0x06001F53 RID: 8019 RVA: 0x000C4268 File Offset: 0x000C2468
		private void WriteEnumMethod(EnumMapping mapping)
		{
			string s = (string)base.MethodNames[mapping];
			base.Writer.WriteLine();
			string csharpName = mapping.TypeDesc.CSharpName;
			if (mapping.IsSoap)
			{
				base.Writer.Write("void ");
				base.Writer.Write(s);
				base.Writer.WriteLine("(object e) {");
				this.WriteLocalDecl(csharpName, "v", "e", mapping.TypeDesc.UseReflection);
			}
			else
			{
				base.Writer.Write("string ");
				base.Writer.Write(s);
				base.Writer.Write("(");
				base.Writer.Write(mapping.TypeDesc.UseReflection ? "object" : csharpName);
				base.Writer.WriteLine(" v) {");
			}
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			base.Writer.WriteLine("string s = null;");
			ConstantMapping[] constants = mapping.Constants;
			if (constants.Length != 0)
			{
				Hashtable hashtable = new Hashtable();
				if (mapping.TypeDesc.UseReflection)
				{
					base.Writer.WriteLine("switch (" + base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection) + " ){");
				}
				else
				{
					base.Writer.WriteLine("switch (v) {");
				}
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent + 1;
				foreach (ConstantMapping constantMapping in constants)
				{
					if (hashtable[constantMapping.Value] == null)
					{
						this.WriteEnumCase(csharpName, constantMapping, mapping.TypeDesc.UseReflection);
						base.Writer.Write("s = ");
						base.WriteQuotedCSharpString(constantMapping.XmlName);
						base.Writer.WriteLine("; break;");
						hashtable.Add(constantMapping.Value, constantMapping.Value);
					}
				}
				if (mapping.IsFlags)
				{
					base.Writer.Write("default: s = FromEnum(");
					base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
					base.Writer.Write(", new string[] {");
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent + 1;
					for (int j = 0; j < constants.Length; j++)
					{
						ConstantMapping constantMapping2 = constants[j];
						if (j > 0)
						{
							base.Writer.WriteLine(", ");
						}
						base.WriteQuotedCSharpString(constantMapping2.XmlName);
					}
					base.Writer.Write("}, new ");
					base.Writer.Write(typeof(long).FullName);
					base.Writer.Write("[] {");
					for (int k = 0; k < constants.Length; k++)
					{
						ConstantMapping constantMapping3 = constants[k];
						if (k > 0)
						{
							base.Writer.WriteLine(", ");
						}
						base.Writer.Write("(long)");
						if (mapping.TypeDesc.UseReflection)
						{
							base.Writer.Write(constantMapping3.Value.ToString(CultureInfo.InvariantCulture));
						}
						else
						{
							base.Writer.Write(csharpName);
							base.Writer.Write(".@");
							CodeIdentifier.CheckValidIdentifier(constantMapping3.Name);
							base.Writer.Write(constantMapping3.Name);
						}
					}
					IndentedWriter writer4 = base.Writer;
					indent = writer4.Indent;
					writer4.Indent = indent - 1;
					base.Writer.Write("}, ");
					base.WriteQuotedCSharpString(mapping.TypeDesc.FullName);
					base.Writer.WriteLine("); break;");
				}
				else
				{
					base.Writer.Write("default: throw CreateInvalidEnumValueException(");
					base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
					base.Writer.Write(".ToString(System.Globalization.CultureInfo.InvariantCulture), ");
					base.WriteQuotedCSharpString(mapping.TypeDesc.FullName);
					base.Writer.WriteLine(");");
				}
				IndentedWriter writer5 = base.Writer;
				indent = writer5.Indent;
				writer5.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			if (mapping.IsSoap)
			{
				base.Writer.Write("WriteXsiType(");
				base.WriteQuotedCSharpString(mapping.TypeName);
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(mapping.Namespace);
				base.Writer.WriteLine(");");
				base.Writer.WriteLine("Writer.WriteString(s);");
			}
			else
			{
				base.Writer.WriteLine("return s;");
			}
			IndentedWriter writer6 = base.Writer;
			indent = writer6.Indent;
			writer6.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F54 RID: 8020 RVA: 0x000C4774 File Offset: 0x000C2974
		private void WriteDerivedTypes(StructMapping mapping)
		{
			for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
			{
				string csharpName = structMapping.TypeDesc.CSharpName;
				base.Writer.Write("else if (");
				this.WriteTypeCompare("t", csharpName, structMapping.TypeDesc.UseReflection);
				base.Writer.WriteLine(") {");
				IndentedWriter writer = base.Writer;
				int indent = writer.Indent;
				writer.Indent = indent + 1;
				string s = base.ReferenceMapping(structMapping);
				base.Writer.Write(s);
				base.Writer.Write("(n, ns,");
				if (!structMapping.TypeDesc.UseReflection)
				{
					base.Writer.Write("(" + csharpName + ")");
				}
				base.Writer.Write("o");
				if (structMapping.TypeDesc.IsNullable)
				{
					base.Writer.Write(", isNullable");
				}
				base.Writer.Write(", true");
				base.Writer.WriteLine(");");
				base.Writer.WriteLine("return;");
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent - 1;
				base.Writer.WriteLine("}");
				this.WriteDerivedTypes(structMapping);
			}
		}

		// Token: 0x06001F55 RID: 8021 RVA: 0x000C48C8 File Offset: 0x000C2AC8
		private void WriteEnumAndArrayTypes()
		{
			TypeScope[] scopes = base.Scopes;
			for (int i = 0; i < scopes.Length; i++)
			{
				foreach (object obj in scopes[i].TypeMappings)
				{
					Mapping mapping = (Mapping)obj;
					if (mapping is EnumMapping && !mapping.IsSoap)
					{
						EnumMapping enumMapping = (EnumMapping)mapping;
						string csharpName = enumMapping.TypeDesc.CSharpName;
						base.Writer.Write("else if (");
						this.WriteTypeCompare("t", csharpName, enumMapping.TypeDesc.UseReflection);
						base.Writer.WriteLine(") {");
						IndentedWriter writer = base.Writer;
						int indent = writer.Indent;
						writer.Indent = indent + 1;
						string s = base.ReferenceMapping(enumMapping);
						base.Writer.WriteLine("Writer.WriteStartElement(n, ns);");
						base.Writer.Write("WriteXsiType(");
						base.WriteQuotedCSharpString(enumMapping.TypeName);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(enumMapping.Namespace);
						base.Writer.WriteLine(");");
						base.Writer.Write("Writer.WriteString(");
						base.Writer.Write(s);
						base.Writer.Write("(");
						if (!enumMapping.TypeDesc.UseReflection)
						{
							base.Writer.Write("(" + csharpName + ")");
						}
						base.Writer.WriteLine("o));");
						base.Writer.WriteLine("Writer.WriteEndElement();");
						base.Writer.WriteLine("return;");
						IndentedWriter writer2 = base.Writer;
						indent = writer2.Indent;
						writer2.Indent = indent - 1;
						base.Writer.WriteLine("}");
					}
					else if (mapping is ArrayMapping && !mapping.IsSoap)
					{
						ArrayMapping arrayMapping = mapping as ArrayMapping;
						if (arrayMapping != null && !mapping.IsSoap)
						{
							string csharpName2 = arrayMapping.TypeDesc.CSharpName;
							base.Writer.Write("else if (");
							if (arrayMapping.TypeDesc.IsArray)
							{
								this.WriteArrayTypeCompare("t", csharpName2, arrayMapping.TypeDesc.ArrayElementTypeDesc.CSharpName, arrayMapping.TypeDesc.UseReflection);
							}
							else
							{
								this.WriteTypeCompare("t", csharpName2, arrayMapping.TypeDesc.UseReflection);
							}
							base.Writer.WriteLine(") {");
							IndentedWriter writer3 = base.Writer;
							int indent = writer3.Indent;
							writer3.Indent = indent + 1;
							base.Writer.WriteLine("Writer.WriteStartElement(n, ns);");
							base.Writer.Write("WriteXsiType(");
							base.WriteQuotedCSharpString(arrayMapping.TypeName);
							base.Writer.Write(", ");
							base.WriteQuotedCSharpString(arrayMapping.Namespace);
							base.Writer.WriteLine(");");
							this.WriteMember("o", null, arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, true);
							base.Writer.WriteLine("Writer.WriteEndElement();");
							base.Writer.WriteLine("return;");
							IndentedWriter writer4 = base.Writer;
							indent = writer4.Indent;
							writer4.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
					}
				}
			}
		}

		// Token: 0x06001F56 RID: 8022 RVA: 0x000C4C68 File Offset: 0x000C2E68
		private void WriteStructMethod(StructMapping mapping)
		{
			if (mapping.IsSoap && mapping.TypeDesc.IsRoot)
			{
				return;
			}
			string s = (string)base.MethodNames[mapping];
			base.Writer.WriteLine();
			base.Writer.Write("void ");
			base.Writer.Write(s);
			string csharpName = mapping.TypeDesc.CSharpName;
			int indent;
			if (mapping.IsSoap)
			{
				base.Writer.WriteLine("(object s) {");
				IndentedWriter writer = base.Writer;
				indent = writer.Indent;
				writer.Indent = indent + 1;
				this.WriteLocalDecl(csharpName, "o", "s", mapping.TypeDesc.UseReflection);
			}
			else
			{
				base.Writer.Write("(string n, string ns, ");
				base.Writer.Write(mapping.TypeDesc.UseReflection ? "object" : csharpName);
				base.Writer.Write(" o");
				if (mapping.TypeDesc.IsNullable)
				{
					base.Writer.Write(", bool isNullable");
				}
				base.Writer.WriteLine(", bool needType) {");
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent + 1;
				if (mapping.TypeDesc.IsNullable)
				{
					base.Writer.WriteLine("if ((object)o == null) {");
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent + 1;
					base.Writer.WriteLine("if (isNullable) WriteNullTagLiteral(n, ns);");
					base.Writer.WriteLine("return;");
					IndentedWriter writer4 = base.Writer;
					indent = writer4.Indent;
					writer4.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				base.Writer.WriteLine("if (!needType) {");
				IndentedWriter writer5 = base.Writer;
				indent = writer5.Indent;
				writer5.Indent = indent + 1;
				base.Writer.Write(typeof(Type).FullName);
				base.Writer.WriteLine(" t = o.GetType();");
				base.Writer.Write("if (");
				this.WriteTypeCompare("t", csharpName, mapping.TypeDesc.UseReflection);
				base.Writer.WriteLine(") {");
				base.Writer.WriteLine("}");
				this.WriteDerivedTypes(mapping);
				if (mapping.TypeDesc.IsRoot)
				{
					this.WriteEnumAndArrayTypes();
				}
				base.Writer.WriteLine("else {");
				IndentedWriter writer6 = base.Writer;
				indent = writer6.Indent;
				writer6.Indent = indent + 1;
				if (mapping.TypeDesc.IsRoot)
				{
					base.Writer.WriteLine("WriteTypedPrimitive(n, ns, o, true);");
					base.Writer.WriteLine("return;");
				}
				else
				{
					base.Writer.WriteLine("throw CreateUnknownTypeException(o);");
				}
				IndentedWriter writer7 = base.Writer;
				indent = writer7.Indent;
				writer7.Indent = indent - 1;
				base.Writer.WriteLine("}");
				IndentedWriter writer8 = base.Writer;
				indent = writer8.Indent;
				writer8.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			if (!mapping.TypeDesc.IsAbstract)
			{
				if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
				{
					base.Writer.WriteLine("EscapeName = false;");
				}
				string text = null;
				MemberMapping[] allMembers = TypeScope.GetAllMembers(mapping);
				int num = this.FindXmlnsIndex(allMembers);
				if (num >= 0)
				{
					MemberMapping memberMapping = allMembers[num];
					CodeIdentifier.CheckValidIdentifier(memberMapping.Name);
					text = base.RaCodeGen.GetStringForMember("o", memberMapping.Name, mapping.TypeDesc);
					if (mapping.TypeDesc.UseReflection)
					{
						text = string.Concat(new string[]
						{
							"((",
							memberMapping.TypeDesc.CSharpName,
							")",
							text,
							")"
						});
					}
				}
				if (!mapping.IsSoap)
				{
					base.Writer.Write("WriteStartElement(n, ns, o, false, ");
					if (text == null)
					{
						base.Writer.Write("null");
					}
					else
					{
						base.Writer.Write(text);
					}
					base.Writer.WriteLine(");");
					if (!mapping.TypeDesc.IsRoot)
					{
						base.Writer.Write("if (needType) WriteXsiType(");
						base.WriteQuotedCSharpString(mapping.TypeName);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(mapping.Namespace);
						base.Writer.WriteLine(");");
					}
				}
				else if (text != null)
				{
					this.WriteNamespaces(text);
				}
				foreach (MemberMapping memberMapping2 in allMembers)
				{
					if (memberMapping2.Attribute != null)
					{
						CodeIdentifier.CheckValidIdentifier(memberMapping2.Name);
						if (memberMapping2.CheckShouldPersist)
						{
							base.Writer.Write("if (");
							string text2 = base.RaCodeGen.GetStringForMethodInvoke("o", csharpName, "ShouldSerialize" + memberMapping2.Name, mapping.TypeDesc.UseReflection, Array.Empty<string>());
							if (mapping.TypeDesc.UseReflection)
							{
								text2 = string.Concat(new string[]
								{
									"((",
									typeof(bool).FullName,
									")",
									text2,
									")"
								});
							}
							base.Writer.Write(text2);
							base.Writer.WriteLine(") {");
							IndentedWriter writer9 = base.Writer;
							indent = writer9.Indent;
							writer9.Indent = indent + 1;
						}
						if (memberMapping2.CheckSpecified != SpecifiedAccessor.None)
						{
							base.Writer.Write("if (");
							string text3 = base.RaCodeGen.GetStringForMember("o", memberMapping2.Name + "Specified", mapping.TypeDesc);
							if (mapping.TypeDesc.UseReflection)
							{
								text3 = string.Concat(new string[]
								{
									"((",
									typeof(bool).FullName,
									")",
									text3,
									")"
								});
							}
							base.Writer.Write(text3);
							base.Writer.WriteLine(") {");
							IndentedWriter writer10 = base.Writer;
							indent = writer10.Indent;
							writer10.Indent = indent + 1;
						}
						this.WriteMember(base.RaCodeGen.GetStringForMember("o", memberMapping2.Name, mapping.TypeDesc), memberMapping2.Attribute, memberMapping2.TypeDesc, "o");
						if (memberMapping2.CheckSpecified != SpecifiedAccessor.None)
						{
							IndentedWriter writer11 = base.Writer;
							indent = writer11.Indent;
							writer11.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
						if (memberMapping2.CheckShouldPersist)
						{
							IndentedWriter writer12 = base.Writer;
							indent = writer12.Indent;
							writer12.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
					}
				}
				foreach (MemberMapping memberMapping3 in allMembers)
				{
					if (memberMapping3.Xmlns == null)
					{
						CodeIdentifier.CheckValidIdentifier(memberMapping3.Name);
						bool flag = memberMapping3.CheckShouldPersist && (memberMapping3.Elements.Length != 0 || memberMapping3.Text != null);
						if (flag)
						{
							base.Writer.Write("if (");
							string text4 = base.RaCodeGen.GetStringForMethodInvoke("o", csharpName, "ShouldSerialize" + memberMapping3.Name, mapping.TypeDesc.UseReflection, Array.Empty<string>());
							if (mapping.TypeDesc.UseReflection)
							{
								text4 = string.Concat(new string[]
								{
									"((",
									typeof(bool).FullName,
									")",
									text4,
									")"
								});
							}
							base.Writer.Write(text4);
							base.Writer.WriteLine(") {");
							IndentedWriter writer13 = base.Writer;
							indent = writer13.Indent;
							writer13.Indent = indent + 1;
						}
						if (memberMapping3.CheckSpecified != SpecifiedAccessor.None)
						{
							base.Writer.Write("if (");
							string text5 = base.RaCodeGen.GetStringForMember("o", memberMapping3.Name + "Specified", mapping.TypeDesc);
							if (mapping.TypeDesc.UseReflection)
							{
								text5 = string.Concat(new string[]
								{
									"((",
									typeof(bool).FullName,
									")",
									text5,
									")"
								});
							}
							base.Writer.Write(text5);
							base.Writer.WriteLine(") {");
							IndentedWriter writer14 = base.Writer;
							indent = writer14.Indent;
							writer14.Indent = indent + 1;
						}
						string choiceSource = null;
						if (memberMapping3.ChoiceIdentifier != null)
						{
							CodeIdentifier.CheckValidIdentifier(memberMapping3.ChoiceIdentifier.MemberName);
							choiceSource = base.RaCodeGen.GetStringForMember("o", memberMapping3.ChoiceIdentifier.MemberName, mapping.TypeDesc);
						}
						this.WriteMember(base.RaCodeGen.GetStringForMember("o", memberMapping3.Name, mapping.TypeDesc), choiceSource, memberMapping3.ElementsSortedByDerivation, memberMapping3.Text, memberMapping3.ChoiceIdentifier, memberMapping3.TypeDesc, true);
						if (memberMapping3.CheckSpecified != SpecifiedAccessor.None)
						{
							IndentedWriter writer15 = base.Writer;
							indent = writer15.Indent;
							writer15.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
						if (flag)
						{
							IndentedWriter writer16 = base.Writer;
							indent = writer16.Indent;
							writer16.Indent = indent - 1;
							base.Writer.WriteLine("}");
						}
					}
				}
				if (!mapping.IsSoap)
				{
					this.WriteEndElement("o");
				}
			}
			IndentedWriter writer17 = base.Writer;
			indent = writer17.Indent;
			writer17.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F57 RID: 8023 RVA: 0x000C5649 File Offset: 0x000C3849
		private bool CanOptimizeWriteListSequence(TypeDesc listElementTypeDesc)
		{
			return listElementTypeDesc != null && listElementTypeDesc != base.QnameTypeDesc;
		}

		// Token: 0x06001F58 RID: 8024 RVA: 0x000C565C File Offset: 0x000C385C
		private void WriteMember(string source, AttributeAccessor attribute, TypeDesc memberTypeDesc, string parent)
		{
			if (memberTypeDesc.IsAbstract)
			{
				return;
			}
			if (memberTypeDesc.IsArrayLike)
			{
				base.Writer.WriteLine("{");
				IndentedWriter writer = base.Writer;
				int indent = writer.Indent;
				writer.Indent = indent + 1;
				string csharpName = memberTypeDesc.CSharpName;
				this.WriteArrayLocalDecl(csharpName, "a", source, memberTypeDesc);
				if (memberTypeDesc.IsNullable)
				{
					base.Writer.WriteLine("if (a != null) {");
					IndentedWriter writer2 = base.Writer;
					indent = writer2.Indent;
					writer2.Indent = indent + 1;
				}
				if (attribute.IsList)
				{
					if (this.CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
					{
						base.Writer.Write("Writer.WriteStartAttribute(null, ");
						base.WriteQuotedCSharpString(attribute.Name);
						base.Writer.Write(", ");
						string text = (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty;
						if (text != null)
						{
							base.WriteQuotedCSharpString(text);
						}
						else
						{
							base.Writer.Write("null");
						}
						base.Writer.WriteLine(");");
					}
					else
					{
						base.Writer.Write(typeof(StringBuilder).FullName);
						base.Writer.Write(" sb = new ");
						base.Writer.Write(typeof(StringBuilder).FullName);
						base.Writer.WriteLine("();");
					}
				}
				TypeDesc arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
				if (memberTypeDesc.IsEnumerable)
				{
					base.Writer.Write(" e = ");
					base.Writer.Write(typeof(IEnumerator).FullName);
					if (memberTypeDesc.IsPrivateImplementation)
					{
						base.Writer.Write("((");
						base.Writer.Write(typeof(IEnumerable).FullName);
						base.Writer.WriteLine(").GetEnumerator();");
					}
					else if (memberTypeDesc.IsGenericInterface)
					{
						if (memberTypeDesc.UseReflection)
						{
							base.Writer.Write("(");
							base.Writer.Write(typeof(IEnumerator).FullName);
							base.Writer.Write(")");
							base.Writer.Write(base.RaCodeGen.GetReflectionVariable(memberTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
							base.Writer.WriteLine(".Invoke(a, new object[0]);");
						}
						else
						{
							base.Writer.Write("((System.Collections.Generic.IEnumerable<");
							base.Writer.Write(arrayElementTypeDesc.CSharpName);
							base.Writer.WriteLine(">)a).GetEnumerator();");
						}
					}
					else
					{
						if (memberTypeDesc.UseReflection)
						{
							base.Writer.Write("(");
							base.Writer.Write(typeof(IEnumerator).FullName);
							base.Writer.Write(")");
						}
						base.Writer.Write(base.RaCodeGen.GetStringForMethodInvoke("a", memberTypeDesc.CSharpName, "GetEnumerator", memberTypeDesc.UseReflection, Array.Empty<string>()));
						base.Writer.WriteLine(";");
					}
					base.Writer.WriteLine("if (e != null)");
					base.Writer.WriteLine("while (e.MoveNext()) {");
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent + 1;
					string csharpName2 = arrayElementTypeDesc.CSharpName;
					this.WriteLocalDecl(csharpName2, "ai", "e.Current", arrayElementTypeDesc.UseReflection);
				}
				else
				{
					base.Writer.Write("for (int i = 0; i < ");
					if (memberTypeDesc.IsArray)
					{
						base.Writer.WriteLine("a.Length; i++) {");
					}
					else
					{
						base.Writer.Write("((");
						base.Writer.Write(typeof(ICollection).FullName);
						base.Writer.WriteLine(")a).Count; i++) {");
					}
					IndentedWriter writer4 = base.Writer;
					indent = writer4.Indent;
					writer4.Indent = indent + 1;
					string csharpName3 = arrayElementTypeDesc.CSharpName;
					this.WriteLocalDecl(csharpName3, "ai", base.RaCodeGen.GetStringForArrayMember("a", "i", memberTypeDesc), arrayElementTypeDesc.UseReflection);
				}
				if (attribute.IsList)
				{
					if (this.CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
					{
						base.Writer.WriteLine("if (i != 0) Writer.WriteString(\" \");");
						base.Writer.Write("WriteValue(");
					}
					else
					{
						base.Writer.WriteLine("if (i != 0) sb.Append(\" \");");
						base.Writer.Write("sb.Append(");
					}
					if (attribute.Mapping is EnumMapping)
					{
						this.WriteEnumValue((EnumMapping)attribute.Mapping, "ai");
					}
					else
					{
						this.WritePrimitiveValue(arrayElementTypeDesc, "ai", true);
					}
					base.Writer.WriteLine(");");
				}
				else
				{
					this.WriteAttribute("ai", attribute, parent);
				}
				IndentedWriter writer5 = base.Writer;
				indent = writer5.Indent;
				writer5.Indent = indent - 1;
				base.Writer.WriteLine("}");
				if (attribute.IsList)
				{
					if (this.CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
					{
						base.Writer.WriteLine("Writer.WriteEndAttribute();");
					}
					else
					{
						base.Writer.WriteLine("if (sb.Length != 0) {");
						IndentedWriter writer6 = base.Writer;
						indent = writer6.Indent;
						writer6.Indent = indent + 1;
						base.Writer.Write("WriteAttribute(");
						base.WriteQuotedCSharpString(attribute.Name);
						base.Writer.Write(", ");
						string text2 = (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty;
						if (text2 != null)
						{
							base.WriteQuotedCSharpString(text2);
							base.Writer.Write(", ");
						}
						base.Writer.WriteLine("sb.ToString());");
						IndentedWriter writer7 = base.Writer;
						indent = writer7.Indent;
						writer7.Indent = indent - 1;
						base.Writer.WriteLine("}");
					}
				}
				if (memberTypeDesc.IsNullable)
				{
					IndentedWriter writer8 = base.Writer;
					indent = writer8.Indent;
					writer8.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				IndentedWriter writer9 = base.Writer;
				indent = writer9.Indent;
				writer9.Indent = indent - 1;
				base.Writer.WriteLine("}");
				return;
			}
			this.WriteAttribute(source, attribute, parent);
		}

		// Token: 0x06001F59 RID: 8025 RVA: 0x000C5C9C File Offset: 0x000C3E9C
		private void WriteAttribute(string source, AttributeAccessor attribute, string parent)
		{
			if (!(attribute.Mapping is SpecialMapping))
			{
				TypeDesc typeDesc = attribute.Mapping.TypeDesc;
				if (!typeDesc.UseReflection)
				{
					source = string.Concat(new string[]
					{
						"((",
						typeDesc.CSharpName,
						")",
						source,
						")"
					});
				}
				this.WritePrimitive("WriteAttribute", attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : "", attribute.Default, source, attribute.Mapping, false, false, false);
				return;
			}
			SpecialMapping specialMapping = (SpecialMapping)attribute.Mapping;
			if (specialMapping.TypeDesc.Kind == TypeKind.Attribute || specialMapping.TypeDesc.CanBeAttributeValue)
			{
				base.Writer.Write("WriteXmlAttribute(");
				base.Writer.Write(source);
				base.Writer.Write(", ");
				base.Writer.Write(parent);
				base.Writer.WriteLine(");");
				return;
			}
			throw new InvalidOperationException(Res.GetString("Internal error."));
		}

		// Token: 0x06001F5A RID: 8026 RVA: 0x000C5DB8 File Offset: 0x000C3FB8
		private void WriteMember(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc memberTypeDesc, bool writeAccessors)
		{
			if (memberTypeDesc.IsArrayLike && (elements.Length != 1 || !(elements[0].Mapping is ArrayMapping)))
			{
				this.WriteArray(source, choiceSource, elements, text, choice, memberTypeDesc);
				return;
			}
			this.WriteElements(source, choiceSource, elements, text, choice, "a", writeAccessors, memberTypeDesc.IsNullable);
		}

		// Token: 0x06001F5B RID: 8027 RVA: 0x000C5E10 File Offset: 0x000C4010
		private void WriteArray(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc)
		{
			if (elements.Length == 0 && text == null)
			{
				return;
			}
			base.Writer.WriteLine("{");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			string csharpName = arrayTypeDesc.CSharpName;
			this.WriteArrayLocalDecl(csharpName, "a", source, arrayTypeDesc);
			if (arrayTypeDesc.IsNullable)
			{
				base.Writer.WriteLine("if (a != null) {");
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent + 1;
			}
			if (choice != null)
			{
				bool useReflection = choice.Mapping.TypeDesc.UseReflection;
				string csharpName2 = choice.Mapping.TypeDesc.CSharpName;
				this.WriteArrayLocalDecl(csharpName2 + "[]", "c", choiceSource, choice.Mapping.TypeDesc);
				base.Writer.WriteLine("if (c == null || c.Length < a.Length) {");
				IndentedWriter writer3 = base.Writer;
				indent = writer3.Indent;
				writer3.Indent = indent + 1;
				base.Writer.Write("throw CreateInvalidChoiceIdentifierValueException(");
				base.WriteQuotedCSharpString(choice.Mapping.TypeDesc.FullName);
				base.Writer.Write(", ");
				base.WriteQuotedCSharpString(choice.MemberName);
				base.Writer.Write(");");
				IndentedWriter writer4 = base.Writer;
				indent = writer4.Indent;
				writer4.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			this.WriteArrayItems(elements, text, choice, arrayTypeDesc, "a", "c");
			if (arrayTypeDesc.IsNullable)
			{
				IndentedWriter writer5 = base.Writer;
				indent = writer5.Indent;
				writer5.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			IndentedWriter writer6 = base.Writer;
			indent = writer6.Indent;
			writer6.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F5C RID: 8028 RVA: 0x000C5FE8 File Offset: 0x000C41E8
		private void WriteArrayItems(ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc, string arrayName, string choiceName)
		{
			TypeDesc arrayElementTypeDesc = arrayTypeDesc.ArrayElementTypeDesc;
			int indent;
			if (arrayTypeDesc.IsEnumerable)
			{
				base.Writer.Write(typeof(IEnumerator).FullName);
				base.Writer.Write(" e = ");
				if (arrayTypeDesc.IsPrivateImplementation)
				{
					base.Writer.Write("((");
					base.Writer.Write(typeof(IEnumerable).FullName);
					base.Writer.Write(")");
					base.Writer.Write(arrayName);
					base.Writer.WriteLine(").GetEnumerator();");
				}
				else if (arrayTypeDesc.IsGenericInterface)
				{
					if (arrayTypeDesc.UseReflection)
					{
						base.Writer.Write("(");
						base.Writer.Write(typeof(IEnumerator).FullName);
						base.Writer.Write(")");
						base.Writer.Write(base.RaCodeGen.GetReflectionVariable(arrayTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
						base.Writer.Write(".Invoke(");
						base.Writer.Write(arrayName);
						base.Writer.WriteLine(", new object[0]);");
					}
					else
					{
						base.Writer.Write("((System.Collections.Generic.IEnumerable<");
						base.Writer.Write(arrayElementTypeDesc.CSharpName);
						base.Writer.Write(">)");
						base.Writer.Write(arrayName);
						base.Writer.WriteLine(").GetEnumerator();");
					}
				}
				else
				{
					if (arrayTypeDesc.UseReflection)
					{
						base.Writer.Write("(");
						base.Writer.Write(typeof(IEnumerator).FullName);
						base.Writer.Write(")");
					}
					base.Writer.Write(base.RaCodeGen.GetStringForMethodInvoke(arrayName, arrayTypeDesc.CSharpName, "GetEnumerator", arrayTypeDesc.UseReflection, Array.Empty<string>()));
					base.Writer.WriteLine(";");
				}
				base.Writer.WriteLine("if (e != null)");
				base.Writer.WriteLine("while (e.MoveNext()) {");
				IndentedWriter writer = base.Writer;
				indent = writer.Indent;
				writer.Indent = indent + 1;
				string csharpName = arrayElementTypeDesc.CSharpName;
				this.WriteLocalDecl(csharpName, arrayName + "i", "e.Current", arrayElementTypeDesc.UseReflection);
				this.WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", true, true);
			}
			else
			{
				base.Writer.Write("for (int i");
				base.Writer.Write(arrayName);
				base.Writer.Write(" = 0; i");
				base.Writer.Write(arrayName);
				base.Writer.Write(" < ");
				if (arrayTypeDesc.IsArray)
				{
					base.Writer.Write(arrayName);
					base.Writer.Write(".Length");
				}
				else
				{
					base.Writer.Write("((");
					base.Writer.Write(typeof(ICollection).FullName);
					base.Writer.Write(")");
					base.Writer.Write(arrayName);
					base.Writer.Write(").Count");
				}
				base.Writer.Write("; i");
				base.Writer.Write(arrayName);
				base.Writer.WriteLine("++) {");
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent + 1;
				if (elements.Length + ((text == null) ? 0 : 1) > 1)
				{
					string csharpName2 = arrayElementTypeDesc.CSharpName;
					this.WriteLocalDecl(csharpName2, arrayName + "i", base.RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), arrayElementTypeDesc.UseReflection);
					if (choice != null)
					{
						string csharpName3 = choice.Mapping.TypeDesc.CSharpName;
						this.WriteLocalDecl(csharpName3, choiceName + "i", base.RaCodeGen.GetStringForArrayMember(choiceName, "i" + arrayName, choice.Mapping.TypeDesc), choice.Mapping.TypeDesc.UseReflection);
					}
					this.WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", true, arrayElementTypeDesc.IsNullable);
				}
				else
				{
					this.WriteElements(base.RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), elements, text, choice, arrayName + "a", true, arrayElementTypeDesc.IsNullable);
				}
			}
			IndentedWriter writer3 = base.Writer;
			indent = writer3.Indent;
			writer3.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F5D RID: 8029 RVA: 0x000C64E8 File Offset: 0x000C46E8
		private void WriteElements(string source, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
		{
			this.WriteElements(source, null, elements, text, choice, arrayName, writeAccessors, isNullable);
		}

		// Token: 0x06001F5E RID: 8030 RVA: 0x000C6508 File Offset: 0x000C4708
		private void WriteElements(string source, string enumSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
		{
			if (elements.Length == 0 && text == null)
			{
				return;
			}
			if (elements.Length == 1 && text == null)
			{
				TypeDesc typeDesc = elements[0].IsUnbounded ? elements[0].Mapping.TypeDesc.CreateArrayTypeDesc() : elements[0].Mapping.TypeDesc;
				if (!elements[0].Any && !elements[0].Mapping.TypeDesc.UseReflection && !elements[0].Mapping.TypeDesc.IsOptionalValue)
				{
					source = string.Concat(new string[]
					{
						"((",
						typeDesc.CSharpName,
						")",
						source,
						")"
					});
				}
				this.WriteElement(source, elements[0], arrayName, writeAccessors);
				return;
			}
			if (isNullable && choice == null)
			{
				base.Writer.Write("if ((object)(");
				base.Writer.Write(source);
				base.Writer.Write(") != null)");
			}
			base.Writer.WriteLine("{");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			int num = 0;
			ArrayList arrayList = new ArrayList();
			ElementAccessor elementAccessor = null;
			bool flag = false;
			string str = (choice == null) ? null : choice.Mapping.TypeDesc.FullName;
			foreach (ElementAccessor elementAccessor2 in elements)
			{
				if (elementAccessor2.Any)
				{
					num++;
					if (elementAccessor2.Name != null && elementAccessor2.Name.Length > 0)
					{
						arrayList.Add(elementAccessor2);
					}
					else if (elementAccessor == null)
					{
						elementAccessor = elementAccessor2;
					}
				}
				else if (choice != null)
				{
					bool useReflection = elementAccessor2.Mapping.TypeDesc.UseReflection;
					string csharpName = elementAccessor2.Mapping.TypeDesc.CSharpName;
					bool useReflection2 = choice.Mapping.TypeDesc.UseReflection;
					string text2 = (useReflection2 ? "" : (str + ".@")) + this.FindChoiceEnumValue(elementAccessor2, (EnumMapping)choice.Mapping, useReflection2);
					if (flag)
					{
						base.Writer.Write("else ");
					}
					else
					{
						flag = true;
					}
					base.Writer.Write("if (");
					base.Writer.Write(useReflection2 ? base.RaCodeGen.GetStringForEnumLongValue(enumSource, useReflection2) : enumSource);
					base.Writer.Write(" == ");
					base.Writer.Write(text2);
					if (isNullable && !elementAccessor2.IsNullable)
					{
						base.Writer.Write(" && ((object)(");
						base.Writer.Write(source);
						base.Writer.Write(") != null)");
					}
					base.Writer.WriteLine(") {");
					IndentedWriter writer2 = base.Writer;
					indent = writer2.Indent;
					writer2.Indent = indent + 1;
					this.WriteChoiceTypeCheck(source, csharpName, useReflection, choice, text2, elementAccessor2.Mapping.TypeDesc);
					string text3 = source;
					if (!useReflection)
					{
						text3 = string.Concat(new string[]
						{
							"((",
							csharpName,
							")",
							source,
							")"
						});
					}
					this.WriteElement(elementAccessor2.Any ? source : text3, elementAccessor2, arrayName, writeAccessors);
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				else
				{
					bool useReflection3 = elementAccessor2.Mapping.TypeDesc.UseReflection;
					string csharpName2 = (elementAccessor2.IsUnbounded ? elementAccessor2.Mapping.TypeDesc.CreateArrayTypeDesc() : elementAccessor2.Mapping.TypeDesc).CSharpName;
					if (flag)
					{
						base.Writer.Write("else ");
					}
					else
					{
						flag = true;
					}
					base.Writer.Write("if (");
					this.WriteInstanceOf(source, csharpName2, useReflection3);
					base.Writer.WriteLine(") {");
					IndentedWriter writer4 = base.Writer;
					indent = writer4.Indent;
					writer4.Indent = indent + 1;
					string text4 = source;
					if (!useReflection3)
					{
						text4 = string.Concat(new string[]
						{
							"((",
							csharpName2,
							")",
							source,
							")"
						});
					}
					this.WriteElement(elementAccessor2.Any ? source : text4, elementAccessor2, arrayName, writeAccessors);
					IndentedWriter writer5 = base.Writer;
					indent = writer5.Indent;
					writer5.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
			}
			if (num > 0)
			{
				if (elements.Length - num > 0)
				{
					base.Writer.Write("else ");
				}
				string fullName = typeof(XmlElement).FullName;
				base.Writer.Write("if (");
				base.Writer.Write(source);
				base.Writer.Write(" is ");
				base.Writer.Write(fullName);
				base.Writer.WriteLine(") {");
				IndentedWriter writer6 = base.Writer;
				indent = writer6.Indent;
				writer6.Indent = indent + 1;
				base.Writer.Write(fullName);
				base.Writer.Write(" elem = (");
				base.Writer.Write(fullName);
				base.Writer.Write(")");
				base.Writer.Write(source);
				base.Writer.WriteLine(";");
				int num2 = 0;
				foreach (object obj in arrayList)
				{
					ElementAccessor elementAccessor3 = (ElementAccessor)obj;
					if (num2++ > 0)
					{
						base.Writer.Write("else ");
					}
					string text5 = null;
					bool useReflection4 = elementAccessor3.Mapping.TypeDesc.UseReflection;
					if (choice != null)
					{
						bool useReflection5 = choice.Mapping.TypeDesc.UseReflection;
						text5 = (useReflection5 ? "" : (str + ".@")) + this.FindChoiceEnumValue(elementAccessor3, (EnumMapping)choice.Mapping, useReflection5);
						base.Writer.Write("if (");
						base.Writer.Write(useReflection5 ? base.RaCodeGen.GetStringForEnumLongValue(enumSource, useReflection5) : enumSource);
						base.Writer.Write(" == ");
						base.Writer.Write(text5);
						if (isNullable && !elementAccessor3.IsNullable)
						{
							base.Writer.Write(" && ((object)(");
							base.Writer.Write(source);
							base.Writer.Write(") != null)");
						}
						base.Writer.WriteLine(") {");
						IndentedWriter writer7 = base.Writer;
						indent = writer7.Indent;
						writer7.Indent = indent + 1;
					}
					base.Writer.Write("if (elem.Name == ");
					base.WriteQuotedCSharpString(elementAccessor3.Name);
					base.Writer.Write(" && elem.NamespaceURI == ");
					base.WriteQuotedCSharpString(elementAccessor3.Namespace);
					base.Writer.WriteLine(") {");
					IndentedWriter writer8 = base.Writer;
					indent = writer8.Indent;
					writer8.Indent = indent + 1;
					this.WriteElement("elem", elementAccessor3, arrayName, writeAccessors);
					if (choice != null)
					{
						IndentedWriter writer9 = base.Writer;
						indent = writer9.Indent;
						writer9.Indent = indent - 1;
						base.Writer.WriteLine("}");
						base.Writer.WriteLine("else {");
						IndentedWriter writer10 = base.Writer;
						indent = writer10.Indent;
						writer10.Indent = indent + 1;
						base.Writer.WriteLine("// throw Value '{0}' of the choice identifier '{1}' does not match element '{2}' from namespace '{3}'.");
						base.Writer.Write("throw CreateChoiceIdentifierValueException(");
						base.WriteQuotedCSharpString(text5);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(choice.MemberName);
						base.Writer.WriteLine(", elem.Name, elem.NamespaceURI);");
						IndentedWriter writer11 = base.Writer;
						indent = writer11.Indent;
						writer11.Indent = indent - 1;
						base.Writer.WriteLine("}");
					}
					IndentedWriter writer12 = base.Writer;
					indent = writer12.Indent;
					writer12.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				if (num2 > 0)
				{
					base.Writer.WriteLine("else {");
					IndentedWriter writer13 = base.Writer;
					indent = writer13.Indent;
					writer13.Indent = indent + 1;
				}
				if (elementAccessor != null)
				{
					this.WriteElement("elem", elementAccessor, arrayName, writeAccessors);
				}
				else
				{
					base.Writer.WriteLine("throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);");
				}
				if (num2 > 0)
				{
					IndentedWriter writer14 = base.Writer;
					indent = writer14.Indent;
					writer14.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				IndentedWriter writer15 = base.Writer;
				indent = writer15.Indent;
				writer15.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			if (text != null)
			{
				bool useReflection6 = text.Mapping.TypeDesc.UseReflection;
				string csharpName3 = text.Mapping.TypeDesc.CSharpName;
				if (elements.Length != 0)
				{
					base.Writer.Write("else ");
					base.Writer.Write("if (");
					this.WriteInstanceOf(source, csharpName3, useReflection6);
					base.Writer.WriteLine(") {");
					IndentedWriter writer16 = base.Writer;
					indent = writer16.Indent;
					writer16.Indent = indent + 1;
					string source2 = source;
					if (!useReflection6)
					{
						source2 = string.Concat(new string[]
						{
							"((",
							csharpName3,
							")",
							source,
							")"
						});
					}
					this.WriteText(source2, text);
					IndentedWriter writer17 = base.Writer;
					indent = writer17.Indent;
					writer17.Indent = indent - 1;
					base.Writer.WriteLine("}");
				}
				else
				{
					string source3 = source;
					if (!useReflection6)
					{
						source3 = string.Concat(new string[]
						{
							"((",
							csharpName3,
							")",
							source,
							")"
						});
					}
					this.WriteText(source3, text);
				}
			}
			if (elements.Length != 0)
			{
				base.Writer.Write("else ");
				if (isNullable)
				{
					base.Writer.Write(" if ((object)(");
					base.Writer.Write(source);
					base.Writer.Write(") != null)");
				}
				base.Writer.WriteLine("{");
				IndentedWriter writer18 = base.Writer;
				indent = writer18.Indent;
				writer18.Indent = indent + 1;
				base.Writer.Write("throw CreateUnknownTypeException(");
				base.Writer.Write(source);
				base.Writer.WriteLine(");");
				IndentedWriter writer19 = base.Writer;
				indent = writer19.Indent;
				writer19.Indent = indent - 1;
				base.Writer.WriteLine("}");
			}
			IndentedWriter writer20 = base.Writer;
			indent = writer20.Indent;
			writer20.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F5F RID: 8031 RVA: 0x000C7024 File Offset: 0x000C5224
		private void WriteText(string source, TextAccessor text)
		{
			if (text.Mapping is PrimitiveMapping)
			{
				PrimitiveMapping primitiveMapping = (PrimitiveMapping)text.Mapping;
				base.Writer.Write("WriteValue(");
				if (text.Mapping is EnumMapping)
				{
					this.WriteEnumValue((EnumMapping)text.Mapping, source);
				}
				else
				{
					this.WritePrimitiveValue(primitiveMapping.TypeDesc, source, false);
				}
				base.Writer.WriteLine(");");
				return;
			}
			if (!(text.Mapping is SpecialMapping))
			{
				return;
			}
			if (((SpecialMapping)text.Mapping).TypeDesc.Kind == TypeKind.Node)
			{
				base.Writer.Write(source);
				base.Writer.WriteLine(".WriteTo(Writer);");
				return;
			}
			throw new InvalidOperationException(Res.GetString("Internal error."));
		}

		// Token: 0x06001F60 RID: 8032 RVA: 0x000C70F0 File Offset: 0x000C52F0
		private void WriteElement(string source, ElementAccessor element, string arrayName, bool writeAccessor)
		{
			string text = writeAccessor ? element.Name : element.Mapping.TypeName;
			string text2 = (element.Any && element.Name.Length == 0) ? null : ((element.Form == XmlSchemaForm.Qualified) ? (writeAccessor ? element.Namespace : element.Mapping.Namespace) : "");
			if (element.Mapping is NullableMapping)
			{
				base.Writer.Write("if (");
				base.Writer.Write(source);
				base.Writer.WriteLine(" != null) {");
				IndentedWriter writer = base.Writer;
				int indent = writer.Indent;
				writer.Indent = indent + 1;
				string csharpName = element.Mapping.TypeDesc.BaseTypeDesc.CSharpName;
				string text3 = source;
				if (!element.Mapping.TypeDesc.BaseTypeDesc.UseReflection)
				{
					text3 = string.Concat(new string[]
					{
						"((",
						csharpName,
						")",
						source,
						")"
					});
				}
				ElementAccessor elementAccessor = element.Clone();
				elementAccessor.Mapping = ((NullableMapping)element.Mapping).BaseMapping;
				this.WriteElement(elementAccessor.Any ? source : text3, elementAccessor, arrayName, writeAccessor);
				IndentedWriter writer2 = base.Writer;
				indent = writer2.Indent;
				writer2.Indent = indent - 1;
				base.Writer.WriteLine("}");
				if (element.IsNullable)
				{
					base.Writer.WriteLine("else {");
					IndentedWriter writer3 = base.Writer;
					indent = writer3.Indent;
					writer3.Indent = indent + 1;
					this.WriteLiteralNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified) ? element.Namespace : "");
					IndentedWriter writer4 = base.Writer;
					indent = writer4.Indent;
					writer4.Indent = indent - 1;
					base.Writer.WriteLine("}");
					return;
				}
				return;
			}
			else if (element.Mapping is ArrayMapping)
			{
				ArrayMapping arrayMapping = (ArrayMapping)element.Mapping;
				if (arrayMapping.IsSoap)
				{
					base.Writer.Write("WritePotentiallyReferencingElement(");
					base.WriteQuotedCSharpString(text);
					base.Writer.Write(", ");
					base.WriteQuotedCSharpString(text2);
					base.Writer.Write(", ");
					base.Writer.Write(source);
					if (!writeAccessor)
					{
						base.Writer.Write(", ");
						base.Writer.Write(base.RaCodeGen.GetStringForTypeof(arrayMapping.TypeDesc.CSharpName, arrayMapping.TypeDesc.UseReflection));
						base.Writer.Write(", true, ");
					}
					else
					{
						base.Writer.Write(", null, false, ");
					}
					this.WriteValue(element.IsNullable);
					base.Writer.WriteLine(");");
					return;
				}
				int indent;
				if (element.IsUnbounded)
				{
					TypeDesc typeDesc = arrayMapping.TypeDesc.CreateArrayTypeDesc();
					string csharpName2 = typeDesc.CSharpName;
					string text4 = "el" + arrayName;
					string text5 = "c" + text4;
					base.Writer.WriteLine("{");
					IndentedWriter writer5 = base.Writer;
					indent = writer5.Indent;
					writer5.Indent = indent + 1;
					this.WriteArrayLocalDecl(csharpName2, text4, source, arrayMapping.TypeDesc);
					if (element.IsNullable)
					{
						this.WriteNullCheckBegin(text4, element);
					}
					else
					{
						if (arrayMapping.TypeDesc.IsNullable)
						{
							base.Writer.Write("if (");
							base.Writer.Write(text4);
							base.Writer.Write(" != null)");
						}
						base.Writer.WriteLine("{");
						IndentedWriter writer6 = base.Writer;
						indent = writer6.Indent;
						writer6.Indent = indent + 1;
					}
					base.Writer.Write("for (int ");
					base.Writer.Write(text5);
					base.Writer.Write(" = 0; ");
					base.Writer.Write(text5);
					base.Writer.Write(" < ");
					if (typeDesc.IsArray)
					{
						base.Writer.Write(text4);
						base.Writer.Write(".Length");
					}
					else
					{
						base.Writer.Write("((");
						base.Writer.Write(typeof(ICollection).FullName);
						base.Writer.Write(")");
						base.Writer.Write(text4);
						base.Writer.Write(").Count");
					}
					base.Writer.Write("; ");
					base.Writer.Write(text5);
					base.Writer.WriteLine("++) {");
					IndentedWriter writer7 = base.Writer;
					indent = writer7.Indent;
					writer7.Indent = indent + 1;
					element.IsUnbounded = false;
					this.WriteElement(text4 + "[" + text5 + "]", element, arrayName, writeAccessor);
					element.IsUnbounded = true;
					IndentedWriter writer8 = base.Writer;
					indent = writer8.Indent;
					writer8.Indent = indent - 1;
					base.Writer.WriteLine("}");
					IndentedWriter writer9 = base.Writer;
					indent = writer9.Indent;
					writer9.Indent = indent - 1;
					base.Writer.WriteLine("}");
					IndentedWriter writer10 = base.Writer;
					indent = writer10.Indent;
					writer10.Indent = indent - 1;
					base.Writer.WriteLine("}");
					return;
				}
				string csharpName3 = arrayMapping.TypeDesc.CSharpName;
				base.Writer.WriteLine("{");
				IndentedWriter writer11 = base.Writer;
				indent = writer11.Indent;
				writer11.Indent = indent + 1;
				this.WriteArrayLocalDecl(csharpName3, arrayName, source, arrayMapping.TypeDesc);
				if (element.IsNullable)
				{
					this.WriteNullCheckBegin(arrayName, element);
				}
				else
				{
					if (arrayMapping.TypeDesc.IsNullable)
					{
						base.Writer.Write("if (");
						base.Writer.Write(arrayName);
						base.Writer.Write(" != null)");
					}
					base.Writer.WriteLine("{");
					IndentedWriter writer12 = base.Writer;
					indent = writer12.Indent;
					writer12.Indent = indent + 1;
				}
				this.WriteStartElement(text, text2, false);
				this.WriteArrayItems(arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, arrayName, null);
				this.WriteEndElement();
				IndentedWriter writer13 = base.Writer;
				indent = writer13.Indent;
				writer13.Indent = indent - 1;
				base.Writer.WriteLine("}");
				IndentedWriter writer14 = base.Writer;
				indent = writer14.Indent;
				writer14.Indent = indent - 1;
				base.Writer.WriteLine("}");
				return;
			}
			else if (element.Mapping is EnumMapping)
			{
				if (element.Mapping.IsSoap)
				{
					string s = (string)base.MethodNames[element.Mapping];
					base.Writer.Write("Writer.WriteStartElement(");
					base.WriteQuotedCSharpString(text);
					base.Writer.Write(", ");
					base.WriteQuotedCSharpString(text2);
					base.Writer.WriteLine(");");
					base.Writer.Write(s);
					base.Writer.Write("(");
					base.Writer.Write(source);
					base.Writer.WriteLine(");");
					this.WriteEndElement();
					return;
				}
				this.WritePrimitive("WriteElementString", text, text2, element.Default, source, element.Mapping, false, true, element.IsNullable);
				return;
			}
			else if (element.Mapping is PrimitiveMapping)
			{
				PrimitiveMapping primitiveMapping = (PrimitiveMapping)element.Mapping;
				if (primitiveMapping.TypeDesc == base.QnameTypeDesc)
				{
					this.WriteQualifiedNameElement(text, text2, element.Default, source, element.IsNullable, primitiveMapping.IsSoap, primitiveMapping);
					return;
				}
				string str = primitiveMapping.IsSoap ? "Encoded" : "Literal";
				string text6 = primitiveMapping.TypeDesc.XmlEncodingNotRequired ? "Raw" : "";
				this.WritePrimitive(element.IsNullable ? ("WriteNullableString" + str + text6) : ("WriteElementString" + text6), text, text2, element.Default, source, primitiveMapping, primitiveMapping.IsSoap, true, element.IsNullable);
				return;
			}
			else
			{
				if (element.Mapping is StructMapping)
				{
					StructMapping structMapping = (StructMapping)element.Mapping;
					if (structMapping.IsSoap)
					{
						base.Writer.Write("WritePotentiallyReferencingElement(");
						base.WriteQuotedCSharpString(text);
						base.Writer.Write(", ");
						base.WriteQuotedCSharpString(text2);
						base.Writer.Write(", ");
						base.Writer.Write(source);
						if (!writeAccessor)
						{
							base.Writer.Write(", ");
							base.Writer.Write(base.RaCodeGen.GetStringForTypeof(structMapping.TypeDesc.CSharpName, structMapping.TypeDesc.UseReflection));
							base.Writer.Write(", true, ");
						}
						else
						{
							base.Writer.Write(", null, false, ");
						}
						this.WriteValue(element.IsNullable);
					}
					else
					{
						string s2 = base.ReferenceMapping(structMapping);
						base.Writer.Write(s2);
						base.Writer.Write("(");
						base.WriteQuotedCSharpString(text);
						base.Writer.Write(", ");
						if (text2 == null)
						{
							base.Writer.Write("null");
						}
						else
						{
							base.WriteQuotedCSharpString(text2);
						}
						base.Writer.Write(", ");
						base.Writer.Write(source);
						if (structMapping.TypeDesc.IsNullable)
						{
							base.Writer.Write(", ");
							this.WriteValue(element.IsNullable);
						}
						base.Writer.Write(", false");
					}
					base.Writer.WriteLine(");");
					return;
				}
				if (!(element.Mapping is SpecialMapping))
				{
					throw new InvalidOperationException(Res.GetString("Internal error."));
				}
				SpecialMapping specialMapping = (SpecialMapping)element.Mapping;
				bool useReflection = specialMapping.TypeDesc.UseReflection;
				string csharpName4 = specialMapping.TypeDesc.CSharpName;
				if (element.Mapping is SerializableMapping)
				{
					this.WriteElementCall("WriteSerializable", typeof(IXmlSerializable), source, text, text2, element.IsNullable, !element.Any);
					return;
				}
				base.Writer.Write("if ((");
				base.Writer.Write(source);
				base.Writer.Write(") is ");
				base.Writer.Write(typeof(XmlNode).FullName);
				base.Writer.Write(" || ");
				base.Writer.Write(source);
				base.Writer.Write(" == null");
				base.Writer.WriteLine(") {");
				IndentedWriter writer15 = base.Writer;
				int indent = writer15.Indent;
				writer15.Indent = indent + 1;
				this.WriteElementCall("WriteElementLiteral", typeof(XmlNode), source, text, text2, element.IsNullable, element.Any);
				IndentedWriter writer16 = base.Writer;
				indent = writer16.Indent;
				writer16.Indent = indent - 1;
				base.Writer.WriteLine("}");
				base.Writer.WriteLine("else {");
				IndentedWriter writer17 = base.Writer;
				indent = writer17.Indent;
				writer17.Indent = indent + 1;
				base.Writer.Write("throw CreateInvalidAnyTypeException(");
				base.Writer.Write(source);
				base.Writer.WriteLine(");");
				IndentedWriter writer18 = base.Writer;
				indent = writer18.Indent;
				writer18.Indent = indent - 1;
				base.Writer.WriteLine("}");
				return;
			}
		}

		// Token: 0x06001F61 RID: 8033 RVA: 0x000C7CC0 File Offset: 0x000C5EC0
		private void WriteElementCall(string func, Type cast, string source, string name, string ns, bool isNullable, bool isAny)
		{
			base.Writer.Write(func);
			base.Writer.Write("((");
			base.Writer.Write(cast.FullName);
			base.Writer.Write(")");
			base.Writer.Write(source);
			base.Writer.Write(", ");
			base.WriteQuotedCSharpString(name);
			base.Writer.Write(", ");
			base.WriteQuotedCSharpString(ns);
			base.Writer.Write(", ");
			this.WriteValue(isNullable);
			base.Writer.Write(", ");
			this.WriteValue(isAny);
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F62 RID: 8034 RVA: 0x000C7D90 File Offset: 0x000C5F90
		private void WriteCheckDefault(string source, object value, bool isNullable)
		{
			base.Writer.Write("if (");
			if (value is string && ((string)value).Length == 0)
			{
				base.Writer.Write("(");
				base.Writer.Write(source);
				if (isNullable)
				{
					base.Writer.Write(" == null) || (");
				}
				else
				{
					base.Writer.Write(" != null) && (");
				}
				base.Writer.Write(source);
				base.Writer.Write(".Length != 0)");
			}
			else
			{
				base.Writer.Write(source);
				base.Writer.Write(" != ");
				this.WriteValue(value);
			}
			base.Writer.Write(")");
		}

		// Token: 0x06001F63 RID: 8035 RVA: 0x000C7E54 File Offset: 0x000C6054
		private void WriteChoiceTypeCheck(string source, string fullTypeName, bool useReflection, ChoiceIdentifierAccessor choice, string enumName, TypeDesc typeDesc)
		{
			base.Writer.Write("if (((object)");
			base.Writer.Write(source);
			base.Writer.Write(") != null && !(");
			this.WriteInstanceOf(source, fullTypeName, useReflection);
			base.Writer.Write(")) throw CreateMismatchChoiceException(");
			base.WriteQuotedCSharpString(typeDesc.FullName);
			base.Writer.Write(", ");
			base.WriteQuotedCSharpString(choice.MemberName);
			base.Writer.Write(", ");
			base.WriteQuotedCSharpString(enumName);
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F64 RID: 8036 RVA: 0x000C7EF8 File Offset: 0x000C60F8
		private void WriteNullCheckBegin(string source, ElementAccessor element)
		{
			base.Writer.Write("if ((object)(");
			base.Writer.Write(source);
			base.Writer.WriteLine(") == null) {");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			this.WriteLiteralNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified) ? element.Namespace : "");
			IndentedWriter writer2 = base.Writer;
			indent = writer2.Indent;
			writer2.Indent = indent - 1;
			base.Writer.WriteLine("}");
			base.Writer.WriteLine("else {");
			IndentedWriter writer3 = base.Writer;
			indent = writer3.Indent;
			writer3.Indent = indent + 1;
		}

		// Token: 0x06001F65 RID: 8037 RVA: 0x000C7FB4 File Offset: 0x000C61B4
		private void WriteValue(object value)
		{
			if (value == null)
			{
				base.Writer.Write("null");
				return;
			}
			Type type = value.GetType();
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				base.Writer.Write(((bool)value) ? "true" : "false");
				return;
			case TypeCode.Char:
			{
				base.Writer.Write('\'');
				char c = (char)value;
				if (c == '\'')
				{
					base.Writer.Write("'");
				}
				else
				{
					base.Writer.Write(c);
				}
				base.Writer.Write('\'');
				return;
			}
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
				base.Writer.Write("(");
				base.Writer.Write(type.FullName);
				base.Writer.Write(")");
				base.Writer.Write("(");
				base.Writer.Write(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
				base.Writer.Write(")");
				return;
			case TypeCode.Int32:
				base.Writer.Write(((int)value).ToString(null, NumberFormatInfo.InvariantInfo));
				return;
			case TypeCode.Single:
				base.Writer.Write(((float)value).ToString("R", NumberFormatInfo.InvariantInfo));
				base.Writer.Write("f");
				return;
			case TypeCode.Double:
				base.Writer.Write(((double)value).ToString("R", NumberFormatInfo.InvariantInfo));
				return;
			case TypeCode.Decimal:
				base.Writer.Write(((decimal)value).ToString(null, NumberFormatInfo.InvariantInfo));
				base.Writer.Write("m");
				return;
			case TypeCode.DateTime:
				base.Writer.Write(" new ");
				base.Writer.Write(type.FullName);
				base.Writer.Write("(");
				base.Writer.Write(((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture));
				base.Writer.Write(")");
				return;
			case TypeCode.String:
			{
				string value2 = (string)value;
				base.WriteQuotedCSharpString(value2);
				return;
			}
			}
			if (type.IsEnum)
			{
				base.Writer.Write(((int)value).ToString(null, NumberFormatInfo.InvariantInfo));
				return;
			}
			if (type == typeof(TimeSpan) && LocalAppContextSwitches.EnableTimeSpanSerialization)
			{
				base.Writer.Write(" new ");
				base.Writer.Write(type.FullName);
				base.Writer.Write("(");
				base.Writer.Write(((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture));
				base.Writer.Write(")");
				return;
			}
			throw new InvalidOperationException(Res.GetString("The default value type, {0}, is unsupported.", new object[]
			{
				type.FullName
			}));
		}

		// Token: 0x06001F66 RID: 8038 RVA: 0x000C82EC File Offset: 0x000C64EC
		private void WriteNamespaces(string source)
		{
			base.Writer.Write("WriteNamespaceDeclarations(");
			base.Writer.Write(source);
			base.Writer.WriteLine(");");
		}

		// Token: 0x06001F67 RID: 8039 RVA: 0x000C831C File Offset: 0x000C651C
		private int FindXmlnsIndex(MemberMapping[] members)
		{
			for (int i = 0; i < members.Length; i++)
			{
				if (members[i].Xmlns != null)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06001F68 RID: 8040 RVA: 0x000C8344 File Offset: 0x000C6544
		private void WriteExtraMembers(string loopStartSource, string loopEndSource)
		{
			base.Writer.Write("for (int i = ");
			base.Writer.Write(loopStartSource);
			base.Writer.Write("; i < ");
			base.Writer.Write(loopEndSource);
			base.Writer.WriteLine("; i++) {");
			IndentedWriter writer = base.Writer;
			int indent = writer.Indent;
			writer.Indent = indent + 1;
			base.Writer.WriteLine("if (p[i] != null) {");
			IndentedWriter writer2 = base.Writer;
			indent = writer2.Indent;
			writer2.Indent = indent + 1;
			base.Writer.WriteLine("WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);");
			IndentedWriter writer3 = base.Writer;
			indent = writer3.Indent;
			writer3.Indent = indent - 1;
			base.Writer.WriteLine("}");
			IndentedWriter writer4 = base.Writer;
			indent = writer4.Indent;
			writer4.Indent = indent - 1;
			base.Writer.WriteLine("}");
		}

		// Token: 0x06001F69 RID: 8041 RVA: 0x000B895A File Offset: 0x000B6B5A
		private void WriteLocalDecl(string typeName, string variableName, string initValue, bool useReflection)
		{
			base.RaCodeGen.WriteLocalDecl(typeName, variableName, initValue, useReflection);
		}

		// Token: 0x06001F6A RID: 8042 RVA: 0x000B8936 File Offset: 0x000B6B36
		private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
		{
			base.RaCodeGen.WriteArrayLocalDecl(typeName, variableName, initValue, arrayTypeDesc);
		}

		// Token: 0x06001F6B RID: 8043 RVA: 0x000C842D File Offset: 0x000C662D
		private void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
		{
			base.RaCodeGen.WriteTypeCompare(variable, escapedTypeName, useReflection);
		}

		// Token: 0x06001F6C RID: 8044 RVA: 0x000C843D File Offset: 0x000C663D
		private void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
		{
			base.RaCodeGen.WriteInstanceOf(source, escapedTypeName, useReflection);
		}

		// Token: 0x06001F6D RID: 8045 RVA: 0x000C844D File Offset: 0x000C664D
		private void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
		{
			base.RaCodeGen.WriteArrayTypeCompare(variable, escapedTypeName, elementTypeName, useReflection);
		}

		// Token: 0x06001F6E RID: 8046 RVA: 0x000C845F File Offset: 0x000C665F
		private void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
		{
			base.RaCodeGen.WriteEnumCase(fullTypeName, c, useReflection);
		}

		// Token: 0x06001F6F RID: 8047 RVA: 0x000C8470 File Offset: 0x000C6670
		private string FindChoiceEnumValue(ElementAccessor element, EnumMapping choiceMapping, bool useReflection)
		{
			string text = null;
			for (int i = 0; i < choiceMapping.Constants.Length; i++)
			{
				string xmlName = choiceMapping.Constants[i].XmlName;
				if (element.Any && element.Name.Length == 0)
				{
					if (xmlName == "##any:")
					{
						if (useReflection)
						{
							text = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture);
							break;
						}
						text = choiceMapping.Constants[i].Name;
						break;
					}
				}
				else
				{
					int num = xmlName.LastIndexOf(':');
					string text2 = (num < 0) ? choiceMapping.Namespace : xmlName.Substring(0, num);
					string b = (num < 0) ? xmlName : xmlName.Substring(num + 1);
					if (element.Name == b && ((element.Form == XmlSchemaForm.Unqualified && string.IsNullOrEmpty(text2)) || element.Namespace == text2))
					{
						if (useReflection)
						{
							text = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture);
							break;
						}
						text = choiceMapping.Constants[i].Name;
						break;
					}
				}
			}
			if (text != null && text.Length != 0)
			{
				if (!useReflection)
				{
					CodeIdentifier.CheckValidIdentifier(text);
				}
				return text;
			}
			if (element.Any && element.Name.Length == 0)
			{
				throw new InvalidOperationException(Res.GetString("Type {0} is missing enumeration value '##any:' corresponding to XmlAnyElementAttribute.", new object[]
				{
					choiceMapping.TypeDesc.FullName
				}));
			}
			throw new InvalidOperationException(Res.GetString("Type {0} is missing enumeration value '{1}' for element '{2}' from namespace '{3}'.", new object[]
			{
				choiceMapping.TypeDesc.FullName,
				element.Namespace + ":" + element.Name,
				element.Name,
				element.Namespace
			}));
		}
	}
}
