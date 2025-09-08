﻿using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace System.Xml.Serialization
{
	// Token: 0x020002FD RID: 765
	internal class ReflectionAwareCodeGen
	{
		// Token: 0x06001F77 RID: 8055 RVA: 0x000C8881 File Offset: 0x000C6A81
		internal ReflectionAwareCodeGen(IndentedWriter writer)
		{
			this.writer = writer;
		}

		// Token: 0x06001F78 RID: 8056 RVA: 0x000C8890 File Offset: 0x000C6A90
		internal void WriteReflectionInit(TypeScope scope)
		{
			foreach (object obj in scope.Types)
			{
				Type type = (Type)obj;
				TypeDesc typeDesc = scope.GetTypeDesc(type);
				if (typeDesc.UseReflection)
				{
					this.WriteTypeInfo(scope, typeDesc, type);
				}
			}
		}

		// Token: 0x06001F79 RID: 8057 RVA: 0x000C88FC File Offset: 0x000C6AFC
		private string WriteTypeInfo(TypeScope scope, TypeDesc typeDesc, Type type)
		{
			this.InitTheFirstTime();
			string csharpName = typeDesc.CSharpName;
			string text = (string)this.reflectionVariables[csharpName];
			if (text != null)
			{
				return text;
			}
			if (type.IsArray)
			{
				text = this.GenerateVariableName("array", typeDesc.CSharpName);
				TypeDesc arrayElementTypeDesc = typeDesc.ArrayElementTypeDesc;
				if (arrayElementTypeDesc.UseReflection)
				{
					string text2 = this.WriteTypeInfo(scope, arrayElementTypeDesc, scope.GetTypeFromTypeDesc(arrayElementTypeDesc));
					this.writer.WriteLine(string.Concat(new string[]
					{
						"static ",
						typeof(Type).FullName,
						" ",
						text,
						" = ",
						text2,
						".MakeArrayType();"
					}));
				}
				else
				{
					string text3 = this.WriteAssemblyInfo(type);
					this.writer.Write(string.Concat(new string[]
					{
						"static ",
						typeof(Type).FullName,
						" ",
						text,
						" = ",
						text3,
						".GetType("
					}));
					this.WriteQuotedCSharpString(type.FullName);
					this.writer.WriteLine(");");
				}
			}
			else
			{
				text = this.GenerateVariableName("type", typeDesc.CSharpName);
				Type underlyingType = Nullable.GetUnderlyingType(type);
				if (underlyingType != null)
				{
					string text4 = this.WriteTypeInfo(scope, scope.GetTypeDesc(underlyingType), underlyingType);
					this.writer.WriteLine(string.Concat(new string[]
					{
						"static ",
						typeof(Type).FullName,
						" ",
						text,
						" = typeof(System.Nullable<>).MakeGenericType(new ",
						typeof(Type).FullName,
						"[] {",
						text4,
						"});"
					}));
				}
				else
				{
					string text5 = this.WriteAssemblyInfo(type);
					this.writer.Write(string.Concat(new string[]
					{
						"static ",
						typeof(Type).FullName,
						" ",
						text,
						" = ",
						text5,
						".GetType("
					}));
					this.WriteQuotedCSharpString(type.FullName);
					this.writer.WriteLine(");");
				}
			}
			this.reflectionVariables.Add(csharpName, text);
			TypeMapping typeMappingFromTypeDesc = scope.GetTypeMappingFromTypeDesc(typeDesc);
			if (typeMappingFromTypeDesc != null)
			{
				this.WriteMappingInfo(typeMappingFromTypeDesc, text, type);
			}
			if (typeDesc.IsCollection || typeDesc.IsEnumerable)
			{
				TypeDesc arrayElementTypeDesc2 = typeDesc.ArrayElementTypeDesc;
				if (arrayElementTypeDesc2.UseReflection)
				{
					this.WriteTypeInfo(scope, arrayElementTypeDesc2, scope.GetTypeFromTypeDesc(arrayElementTypeDesc2));
				}
				this.WriteCollectionInfo(text, typeDesc, type);
			}
			return text;
		}

		// Token: 0x06001F7A RID: 8058 RVA: 0x000C8BB8 File Offset: 0x000C6DB8
		private void InitTheFirstTime()
		{
			if (this.reflectionVariables == null)
			{
				this.reflectionVariables = new Hashtable();
				this.writer.Write(string.Format(CultureInfo.InvariantCulture, ReflectionAwareCodeGen.helperClassesForUseReflection, new object[]
				{
					"object",
					"string",
					typeof(Type).FullName,
					typeof(FieldInfo).FullName,
					typeof(PropertyInfo).FullName,
					typeof(MemberInfo).FullName,
					typeof(MemberTypes).FullName
				}));
				this.WriteDefaultIndexerInit(typeof(IList), typeof(Array).FullName, false, false);
			}
		}

		// Token: 0x06001F7B RID: 8059 RVA: 0x000C8C88 File Offset: 0x000C6E88
		private void WriteMappingInfo(TypeMapping mapping, string typeVariable, Type type)
		{
			string csharpName = mapping.TypeDesc.CSharpName;
			if (mapping is StructMapping)
			{
				StructMapping structMapping = mapping as StructMapping;
				for (int i = 0; i < structMapping.Members.Length; i++)
				{
					MemberMapping memberMapping = structMapping.Members[i];
					this.WriteMemberInfo(type, csharpName, typeVariable, memberMapping.Name);
					if (memberMapping.CheckShouldPersist)
					{
						string memberName = "ShouldSerialize" + memberMapping.Name;
						this.WriteMethodInfo(csharpName, typeVariable, memberName, false, Array.Empty<string>());
					}
					if (memberMapping.CheckSpecified != SpecifiedAccessor.None)
					{
						string memberName2 = memberMapping.Name + "Specified";
						this.WriteMemberInfo(type, csharpName, typeVariable, memberName2);
					}
					if (memberMapping.ChoiceIdentifier != null)
					{
						string memberName3 = memberMapping.ChoiceIdentifier.MemberName;
						this.WriteMemberInfo(type, csharpName, typeVariable, memberName3);
					}
				}
				return;
			}
			if (mapping is EnumMapping)
			{
				FieldInfo[] fields = type.GetFields();
				for (int j = 0; j < fields.Length; j++)
				{
					this.WriteMemberInfo(type, csharpName, typeVariable, fields[j].Name);
				}
			}
		}

		// Token: 0x06001F7C RID: 8060 RVA: 0x000C8D90 File Offset: 0x000C6F90
		private void WriteCollectionInfo(string typeVariable, TypeDesc typeDesc, Type type)
		{
			string csharpName = CodeIdentifier.GetCSharpName(type);
			string csharpName2 = typeDesc.ArrayElementTypeDesc.CSharpName;
			bool useReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
			if (typeDesc.IsCollection)
			{
				this.WriteDefaultIndexerInit(type, csharpName, typeDesc.UseReflection, useReflection);
			}
			else if (typeDesc.IsEnumerable)
			{
				if (typeDesc.IsGenericInterface)
				{
					this.WriteMethodInfo(csharpName, typeVariable, "System.Collections.Generic.IEnumerable*", true, Array.Empty<string>());
				}
				else if (!typeDesc.IsPrivateImplementation)
				{
					this.WriteMethodInfo(csharpName, typeVariable, "GetEnumerator", true, Array.Empty<string>());
				}
			}
			this.WriteMethodInfo(csharpName, typeVariable, "Add", false, new string[]
			{
				this.GetStringForTypeof(csharpName2, useReflection)
			});
		}

		// Token: 0x06001F7D RID: 8061 RVA: 0x000C8E38 File Offset: 0x000C7038
		private string WriteAssemblyInfo(Type type)
		{
			string fullName = type.Assembly.FullName;
			string text = (string)this.reflectionVariables[fullName];
			if (text == null)
			{
				int num = fullName.IndexOf(',');
				string fullName2 = (num > -1) ? fullName.Substring(0, num) : fullName;
				text = this.GenerateVariableName("assembly", fullName2);
				this.writer.Write(string.Concat(new string[]
				{
					"static ",
					typeof(Assembly).FullName,
					" ",
					text,
					" = ResolveDynamicAssembly("
				}));
				this.WriteQuotedCSharpString(DynamicAssemblies.GetName(type.Assembly));
				this.writer.WriteLine(");");
				this.reflectionVariables.Add(fullName, text);
			}
			return text;
		}

		// Token: 0x06001F7E RID: 8062 RVA: 0x000C8F04 File Offset: 0x000C7104
		private string WriteMemberInfo(Type type, string escapedName, string typeVariable, string memberName)
		{
			MemberInfo[] member = type.GetMember(memberName);
			for (int i = 0; i < member.Length; i++)
			{
				MemberTypes memberType = member[i].MemberType;
				if (memberType == MemberTypes.Property)
				{
					string text = this.GenerateVariableName("prop", memberName);
					this.writer.Write(string.Concat(new string[]
					{
						"static XSPropInfo ",
						text,
						" = new XSPropInfo(",
						typeVariable,
						", "
					}));
					this.WriteQuotedCSharpString(memberName);
					this.writer.WriteLine(");");
					this.reflectionVariables.Add(memberName + ":" + escapedName, text);
					return text;
				}
				if (memberType == MemberTypes.Field)
				{
					string text2 = this.GenerateVariableName("field", memberName);
					this.writer.Write(string.Concat(new string[]
					{
						"static XSFieldInfo ",
						text2,
						" = new XSFieldInfo(",
						typeVariable,
						", "
					}));
					this.WriteQuotedCSharpString(memberName);
					this.writer.WriteLine(");");
					this.reflectionVariables.Add(memberName + ":" + escapedName, text2);
					return text2;
				}
			}
			throw new InvalidOperationException(Res.GetString("{0} is an unsupported type. Please use [XmlIgnore] attribute to exclude members of this type from serialization graph.", new object[]
			{
				member[0].ToString()
			}));
		}

		// Token: 0x06001F7F RID: 8063 RVA: 0x000C9054 File Offset: 0x000C7254
		private string WriteMethodInfo(string escapedName, string typeVariable, string memberName, bool isNonPublic, params string[] paramTypes)
		{
			string text = this.GenerateVariableName("method", memberName);
			this.writer.Write(string.Concat(new string[]
			{
				"static ",
				typeof(MethodInfo).FullName,
				" ",
				text,
				" = ",
				typeVariable,
				".GetMethod("
			}));
			this.WriteQuotedCSharpString(memberName);
			this.writer.Write(", ");
			string fullName = typeof(BindingFlags).FullName;
			this.writer.Write(fullName);
			this.writer.Write(".Public | ");
			this.writer.Write(fullName);
			this.writer.Write(".Instance | ");
			this.writer.Write(fullName);
			this.writer.Write(".Static");
			if (isNonPublic)
			{
				this.writer.Write(" | ");
				this.writer.Write(fullName);
				this.writer.Write(".NonPublic");
			}
			this.writer.Write(", null, ");
			this.writer.Write("new " + typeof(Type).FullName + "[] { ");
			for (int i = 0; i < paramTypes.Length; i++)
			{
				this.writer.Write(paramTypes[i]);
				if (i < paramTypes.Length - 1)
				{
					this.writer.Write(", ");
				}
			}
			this.writer.WriteLine("}, null);");
			this.reflectionVariables.Add(memberName + ":" + escapedName, text);
			return text;
		}

		// Token: 0x06001F80 RID: 8064 RVA: 0x000C9204 File Offset: 0x000C7404
		private string WriteDefaultIndexerInit(Type type, string escapedName, bool collectionUseReflection, bool elementUseReflection)
		{
			string text = this.GenerateVariableName("item", escapedName);
			PropertyInfo defaultIndexer = TypeScope.GetDefaultIndexer(type, null);
			this.writer.Write("static XSArrayInfo ");
			this.writer.Write(text);
			this.writer.Write("= new XSArrayInfo(");
			this.writer.Write(this.GetStringForTypeof(CodeIdentifier.GetCSharpName(type), collectionUseReflection));
			this.writer.Write(".GetProperty(");
			this.WriteQuotedCSharpString(defaultIndexer.Name);
			this.writer.Write(",");
			this.writer.Write(this.GetStringForTypeof(CodeIdentifier.GetCSharpName(defaultIndexer.PropertyType), elementUseReflection));
			this.writer.Write(",new ");
			this.writer.Write(typeof(Type[]).FullName);
			this.writer.WriteLine("{typeof(int)}));");
			this.reflectionVariables.Add("0:" + escapedName, text);
			return text;
		}

		// Token: 0x06001F81 RID: 8065 RVA: 0x000C9306 File Offset: 0x000C7506
		private string GenerateVariableName(string prefix, string fullName)
		{
			this.nextReflectionVariableNumber++;
			return prefix + this.nextReflectionVariableNumber.ToString() + "_" + CodeIdentifier.MakeValidInternal(fullName.Replace('.', '_'));
		}

		// Token: 0x06001F82 RID: 8066 RVA: 0x000C933C File Offset: 0x000C753C
		internal string GetReflectionVariable(string typeFullName, string memberName)
		{
			string key;
			if (memberName == null)
			{
				key = typeFullName;
			}
			else
			{
				key = memberName + ":" + typeFullName;
			}
			return (string)this.reflectionVariables[key];
		}

		// Token: 0x06001F83 RID: 8067 RVA: 0x000C9370 File Offset: 0x000C7570
		internal string GetStringForMethodInvoke(string obj, string escapedTypeName, string methodName, bool useReflection, params string[] args)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (useReflection)
			{
				stringBuilder.Append(this.GetReflectionVariable(escapedTypeName, methodName));
				stringBuilder.Append(".Invoke(");
				stringBuilder.Append(obj);
				stringBuilder.Append(", new object[] {");
			}
			else
			{
				stringBuilder.Append(obj);
				stringBuilder.Append(".@");
				stringBuilder.Append(methodName);
				stringBuilder.Append("(");
			}
			for (int i = 0; i < args.Length; i++)
			{
				if (i != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(args[i]);
			}
			if (useReflection)
			{
				stringBuilder.Append("})");
			}
			else
			{
				stringBuilder.Append(")");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06001F84 RID: 8068 RVA: 0x000C9430 File Offset: 0x000C7630
		internal string GetStringForEnumCompare(EnumMapping mapping, string memberName, bool useReflection)
		{
			if (!useReflection)
			{
				CodeIdentifier.CheckValidIdentifier(memberName);
				return mapping.TypeDesc.CSharpName + ".@" + memberName;
			}
			string stringForEnumMember = this.GetStringForEnumMember(mapping.TypeDesc.CSharpName, memberName, useReflection);
			return this.GetStringForEnumLongValue(stringForEnumMember, useReflection);
		}

		// Token: 0x06001F85 RID: 8069 RVA: 0x000C947C File Offset: 0x000C767C
		internal string GetStringForEnumLongValue(string variable, bool useReflection)
		{
			if (useReflection)
			{
				return typeof(Convert).FullName + ".ToInt64(" + variable + ")";
			}
			return string.Concat(new string[]
			{
				"((",
				typeof(long).FullName,
				")",
				variable,
				")"
			});
		}

		// Token: 0x06001F86 RID: 8070 RVA: 0x000C94E5 File Offset: 0x000C76E5
		internal string GetStringForTypeof(string typeFullName, bool useReflection)
		{
			if (useReflection)
			{
				return this.GetReflectionVariable(typeFullName, null);
			}
			return "typeof(" + typeFullName + ")";
		}

		// Token: 0x06001F87 RID: 8071 RVA: 0x000C9504 File Offset: 0x000C7704
		internal string GetStringForMember(string obj, string memberName, TypeDesc typeDesc)
		{
			if (!typeDesc.UseReflection)
			{
				return obj + ".@" + memberName;
			}
			while (typeDesc != null)
			{
				string csharpName = typeDesc.CSharpName;
				string reflectionVariable = this.GetReflectionVariable(csharpName, memberName);
				if (reflectionVariable != null)
				{
					return reflectionVariable + "[" + obj + "]";
				}
				typeDesc = typeDesc.BaseTypeDesc;
				if (typeDesc != null && !typeDesc.UseReflection)
				{
					return string.Concat(new string[]
					{
						"((",
						typeDesc.CSharpName,
						")",
						obj,
						").@",
						memberName
					});
				}
			}
			return "[" + obj + "]";
		}

		// Token: 0x06001F88 RID: 8072 RVA: 0x000C95A6 File Offset: 0x000C77A6
		internal string GetStringForEnumMember(string typeFullName, string memberName, bool useReflection)
		{
			if (!useReflection)
			{
				return typeFullName + ".@" + memberName;
			}
			return this.GetReflectionVariable(typeFullName, memberName) + "[null]";
		}

		// Token: 0x06001F89 RID: 8073 RVA: 0x000C95CC File Offset: 0x000C77CC
		internal string GetStringForArrayMember(string arrayName, string subscript, TypeDesc arrayTypeDesc)
		{
			if (!arrayTypeDesc.UseReflection)
			{
				return arrayName + "[" + subscript + "]";
			}
			string typeFullName = arrayTypeDesc.IsCollection ? arrayTypeDesc.CSharpName : typeof(Array).FullName;
			string reflectionVariable = this.GetReflectionVariable(typeFullName, "0");
			return string.Concat(new string[]
			{
				reflectionVariable,
				"[",
				arrayName,
				", ",
				subscript,
				"]"
			});
		}

		// Token: 0x06001F8A RID: 8074 RVA: 0x000C964F File Offset: 0x000C784F
		internal string GetStringForMethod(string obj, string typeFullName, string memberName, bool useReflection)
		{
			if (!useReflection)
			{
				return obj + "." + memberName + "(";
			}
			return this.GetReflectionVariable(typeFullName, memberName) + ".Invoke(" + obj + ", new object[]{";
		}

		// Token: 0x06001F8B RID: 8075 RVA: 0x000C967F File Offset: 0x000C787F
		internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast)
		{
			return this.GetStringForCreateInstance(escapedTypeName, useReflection, ctorInaccessible, cast, string.Empty);
		}

		// Token: 0x06001F8C RID: 8076 RVA: 0x000C9694 File Offset: 0x000C7894
		internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast, string arg)
		{
			if (!useReflection && !ctorInaccessible)
			{
				return string.Concat(new string[]
				{
					"new ",
					escapedTypeName,
					"(",
					arg,
					")"
				});
			}
			return this.GetStringForCreateInstance(this.GetStringForTypeof(escapedTypeName, useReflection), (cast && !useReflection) ? escapedTypeName : null, ctorInaccessible, arg);
		}

		// Token: 0x06001F8D RID: 8077 RVA: 0x000C96F0 File Offset: 0x000C78F0
		internal string GetStringForCreateInstance(string type, string cast, bool nonPublic, string arg)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (cast != null && cast.Length > 0)
			{
				stringBuilder.Append("(");
				stringBuilder.Append(cast);
				stringBuilder.Append(")");
			}
			stringBuilder.Append(typeof(Activator).FullName);
			stringBuilder.Append(".CreateInstance(");
			stringBuilder.Append(type);
			stringBuilder.Append(", ");
			string fullName = typeof(BindingFlags).FullName;
			stringBuilder.Append(fullName);
			stringBuilder.Append(".Instance | ");
			stringBuilder.Append(fullName);
			stringBuilder.Append(".Public | ");
			stringBuilder.Append(fullName);
			stringBuilder.Append(".CreateInstance");
			if (nonPublic)
			{
				stringBuilder.Append(" | ");
				stringBuilder.Append(fullName);
				stringBuilder.Append(".NonPublic");
			}
			if (arg == null || arg.Length == 0)
			{
				stringBuilder.Append(", null, new object[0], null)");
			}
			else
			{
				stringBuilder.Append(", null, new object[] { ");
				stringBuilder.Append(arg);
				stringBuilder.Append(" }, null)");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06001F8E RID: 8078 RVA: 0x000C9818 File Offset: 0x000C7A18
		internal void WriteLocalDecl(string typeFullName, string variableName, string initValue, bool useReflection)
		{
			if (useReflection)
			{
				typeFullName = "object";
			}
			this.writer.Write(typeFullName);
			this.writer.Write(" ");
			this.writer.Write(variableName);
			if (initValue != null)
			{
				this.writer.Write(" = ");
				if (!useReflection && initValue != "null")
				{
					this.writer.Write("(" + typeFullName + ")");
				}
				this.writer.Write(initValue);
			}
			this.writer.WriteLine(";");
		}

		// Token: 0x06001F8F RID: 8079 RVA: 0x000C98B4 File Offset: 0x000C7AB4
		internal void WriteCreateInstance(string escapedName, string source, bool useReflection, bool ctorInaccessible)
		{
			this.writer.Write(useReflection ? "object" : escapedName);
			this.writer.Write(" ");
			this.writer.Write(source);
			this.writer.Write(" = ");
			this.writer.Write(this.GetStringForCreateInstance(escapedName, useReflection, ctorInaccessible, !useReflection && ctorInaccessible));
			this.writer.WriteLine(";");
		}

		// Token: 0x06001F90 RID: 8080 RVA: 0x000C9930 File Offset: 0x000C7B30
		internal void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
		{
			if (!useReflection)
			{
				this.writer.Write(source);
				this.writer.Write(" is ");
				this.writer.Write(escapedTypeName);
				return;
			}
			this.writer.Write(this.GetReflectionVariable(escapedTypeName, null));
			this.writer.Write(".IsAssignableFrom(");
			this.writer.Write(source);
			this.writer.Write(".GetType())");
		}

		// Token: 0x06001F91 RID: 8081 RVA: 0x000C99A8 File Offset: 0x000C7BA8
		internal void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
		{
			if (arrayTypeDesc.UseReflection)
			{
				if (arrayTypeDesc.IsEnumerable)
				{
					typeName = typeof(IEnumerable).FullName;
				}
				else if (arrayTypeDesc.IsCollection)
				{
					typeName = typeof(ICollection).FullName;
				}
				else
				{
					typeName = typeof(Array).FullName;
				}
			}
			this.writer.Write(typeName);
			this.writer.Write(" ");
			this.writer.Write(variableName);
			if (initValue != null)
			{
				this.writer.Write(" = ");
				if (initValue != "null")
				{
					this.writer.Write("(" + typeName + ")");
				}
				this.writer.Write(initValue);
			}
			this.writer.WriteLine(";");
		}

		// Token: 0x06001F92 RID: 8082 RVA: 0x000C9A88 File Offset: 0x000C7C88
		internal void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
		{
			this.writer.Write("case ");
			if (useReflection)
			{
				this.writer.Write(c.Value.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				this.writer.Write(fullTypeName);
				this.writer.Write(".@");
				CodeIdentifier.CheckValidIdentifier(c.Name);
				this.writer.Write(c.Name);
			}
			this.writer.Write(": ");
		}

		// Token: 0x06001F93 RID: 8083 RVA: 0x000C9B10 File Offset: 0x000C7D10
		internal void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
		{
			this.writer.Write(variable);
			this.writer.Write(" == ");
			this.writer.Write(this.GetStringForTypeof(escapedTypeName, useReflection));
		}

		// Token: 0x06001F94 RID: 8084 RVA: 0x000C9B44 File Offset: 0x000C7D44
		internal void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
		{
			if (!useReflection)
			{
				this.writer.Write(variable);
				this.writer.Write(" == typeof(");
				this.writer.Write(escapedTypeName);
				this.writer.Write(")");
				return;
			}
			this.writer.Write(variable);
			this.writer.Write(".IsArray ");
			this.writer.Write(" && ");
			this.WriteTypeCompare(variable + ".GetElementType()", elementTypeName, useReflection);
		}

		// Token: 0x06001F95 RID: 8085 RVA: 0x000C9BD0 File Offset: 0x000C7DD0
		internal static void WriteQuotedCSharpString(IndentedWriter writer, string value)
		{
			if (value == null)
			{
				writer.Write("null");
				return;
			}
			writer.Write("@\"");
			foreach (char c in value)
			{
				if (c < ' ')
				{
					if (c == '\r')
					{
						writer.Write("\\r");
					}
					else if (c == '\n')
					{
						writer.Write("\\n");
					}
					else if (c == '\t')
					{
						writer.Write("\\t");
					}
					else
					{
						byte b = (byte)c;
						writer.Write("\\x");
						writer.Write("0123456789ABCDEF"[b >> 4]);
						writer.Write("0123456789ABCDEF"[(int)(b & 15)]);
					}
				}
				else if (c == '"')
				{
					writer.Write("\"\"");
				}
				else
				{
					writer.Write(c);
				}
			}
			writer.Write("\"");
		}

		// Token: 0x06001F96 RID: 8086 RVA: 0x000C9CAE File Offset: 0x000C7EAE
		internal void WriteQuotedCSharpString(string value)
		{
			ReflectionAwareCodeGen.WriteQuotedCSharpString(this.writer, value);
		}

		// Token: 0x06001F97 RID: 8087 RVA: 0x000C9CBC File Offset: 0x000C7EBC
		// Note: this type is marked as 'beforefieldinit'.
		static ReflectionAwareCodeGen()
		{
		}

		// Token: 0x04001B07 RID: 6919
		private const string hexDigits = "0123456789ABCDEF";

		// Token: 0x04001B08 RID: 6920
		private const string arrayMemberKey = "0";

		// Token: 0x04001B09 RID: 6921
		private Hashtable reflectionVariables;

		// Token: 0x04001B0A RID: 6922
		private int nextReflectionVariableNumber;

		// Token: 0x04001B0B RID: 6923
		private IndentedWriter writer;

		// Token: 0x04001B0C RID: 6924
		private static string helperClassesForUseReflection = "\n    sealed class XSFieldInfo {{\n       {3} fieldInfo;\n        public XSFieldInfo({2} t, {1} memberName){{\n            fieldInfo = t.GetField(memberName);\n        }}\n        public {0} this[{0} o] {{\n            get {{\n                return fieldInfo.GetValue(o);\n            }}\n            set {{\n                fieldInfo.SetValue(o, value);\n            }}\n        }}\n\n    }}\n    sealed class XSPropInfo {{\n        {4} propInfo;\n        public XSPropInfo({2} t, {1} memberName){{\n            propInfo = t.GetProperty(memberName);\n        }}\n        public {0} this[{0} o] {{\n            get {{\n                return propInfo.GetValue(o, null);\n            }}\n            set {{\n                propInfo.SetValue(o, value, null);\n            }}\n        }}\n    }}\n    sealed class XSArrayInfo {{\n        {4} propInfo;\n        public XSArrayInfo({4} propInfo){{\n            this.propInfo = propInfo;\n        }}\n        public {0} this[{0} a, int i] {{\n            get {{\n                return propInfo.GetValue(a, new {0}[]{{i}});\n            }}\n            set {{\n                propInfo.SetValue(a, value, new {0}[]{{i}});\n            }}\n        }}\n    }}\n";
	}
}
