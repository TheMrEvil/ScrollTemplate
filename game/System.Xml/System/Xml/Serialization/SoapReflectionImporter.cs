﻿using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
	/// <summary>Generates mappings to SOAP-encoded messages from .NET Framework types or Web service method information. </summary>
	// Token: 0x020002B3 RID: 691
	public class SoapReflectionImporter
	{
		/// <summary>Initializes a new instance of the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> class. </summary>
		// Token: 0x060019F6 RID: 6646 RVA: 0x00094D75 File Offset: 0x00092F75
		public SoapReflectionImporter() : this(null, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> class, specifying a default XML namespace for imported type mappings. </summary>
		/// <param name="defaultNamespace">The default XML namespace to use for imported type mappings.</param>
		// Token: 0x060019F7 RID: 6647 RVA: 0x00094D7F File Offset: 0x00092F7F
		public SoapReflectionImporter(string defaultNamespace) : this(null, defaultNamespace)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> class, specifying overrides for XML serialization. </summary>
		/// <param name="attributeOverrides">A <see cref="T:System.Xml.Serialization.SoapAttributeOverrides" /> object that overrides how the <see cref="T:System.Xml.Serialization.XmlSerializer" /> class serializes mapped types using SOAP encoding.</param>
		// Token: 0x060019F8 RID: 6648 RVA: 0x00094D89 File Offset: 0x00092F89
		public SoapReflectionImporter(SoapAttributeOverrides attributeOverrides) : this(attributeOverrides, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> class, specifying XML serialization overrides and a default XML namespace. </summary>
		/// <param name="attributeOverrides">A <see cref="T:System.Xml.Serialization.SoapAttributeOverrides" /> object that overrides how the <see cref="T:System.Xml.Serialization.XmlSerializer" /> class serializes mapped types using SOAP encoding.</param>
		/// <param name="defaultNamespace">The default XML namespace to use for imported type mappings.</param>
		// Token: 0x060019F9 RID: 6649 RVA: 0x00094D94 File Offset: 0x00092F94
		public SoapReflectionImporter(SoapAttributeOverrides attributeOverrides, string defaultNamespace)
		{
			if (defaultNamespace == null)
			{
				defaultNamespace = string.Empty;
			}
			if (attributeOverrides == null)
			{
				attributeOverrides = new SoapAttributeOverrides();
			}
			this.attributeOverrides = attributeOverrides;
			this.defaultNs = defaultNamespace;
			this.typeScope = new TypeScope();
			this.modelScope = new ModelScope(this.typeScope);
		}

		/// <summary>Places mappings for derived types in the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> instance's context for later use when import methods are invoked. </summary>
		/// <param name="provider">An <see cref="T:System.Reflection.ICustomAttributeProvider" /> reflection object that contains custom attributes that are derived from the <see cref="T:System.Xml.Serialization.SoapIncludeAttribute" /> attribute.</param>
		// Token: 0x060019FA RID: 6650 RVA: 0x00094DFB File Offset: 0x00092FFB
		public void IncludeTypes(ICustomAttributeProvider provider)
		{
			this.IncludeTypes(provider, new RecursionLimiter());
		}

		// Token: 0x060019FB RID: 6651 RVA: 0x00094E0C File Offset: 0x0009300C
		private void IncludeTypes(ICustomAttributeProvider provider, RecursionLimiter limiter)
		{
			object[] customAttributes = provider.GetCustomAttributes(typeof(SoapIncludeAttribute), false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				this.IncludeType(((SoapIncludeAttribute)customAttributes[i]).Type, limiter);
			}
		}

		/// <summary>Places mappings for a type in the <see cref="T:System.Xml.Serialization.SoapReflectionImporter" /> instance's context for later use when import methods are invoked. </summary>
		/// <param name="type">The .NET Framework type for which to save type mapping information.</param>
		// Token: 0x060019FC RID: 6652 RVA: 0x00094E4D File Offset: 0x0009304D
		public void IncludeType(Type type)
		{
			this.IncludeType(type, new RecursionLimiter());
		}

		// Token: 0x060019FD RID: 6653 RVA: 0x00094E5B File Offset: 0x0009305B
		private void IncludeType(Type type, RecursionLimiter limiter)
		{
			this.ImportTypeMapping(this.modelScope.GetTypeModel(type), limiter);
		}

		/// <summary>Generates a mapping to an XML Schema element for a .NET Framework type.</summary>
		/// <param name="type">The .NET Framework type for which to generate a type mapping. </param>
		/// <returns>Internal .NET Framework mapping of a type to an XML Schema element. </returns>
		// Token: 0x060019FE RID: 6654 RVA: 0x00094E71 File Offset: 0x00093071
		public XmlTypeMapping ImportTypeMapping(Type type)
		{
			return this.ImportTypeMapping(type, null);
		}

		/// <summary>Generates a mapping to an XML Schema element for a .NET Framework type.</summary>
		/// <param name="type">The .NET Framework type for which to generate a type mapping. </param>
		/// <param name="defaultNamespace">The default XML namespace to use.</param>
		/// <returns>Internal .NET Framework mapping of a type to an XML Schema element.</returns>
		// Token: 0x060019FF RID: 6655 RVA: 0x00094E7C File Offset: 0x0009307C
		public XmlTypeMapping ImportTypeMapping(Type type, string defaultNamespace)
		{
			ElementAccessor elementAccessor = new ElementAccessor();
			elementAccessor.IsSoap = true;
			elementAccessor.Mapping = this.ImportTypeMapping(this.modelScope.GetTypeModel(type), new RecursionLimiter());
			elementAccessor.Name = elementAccessor.Mapping.DefaultElementName;
			elementAccessor.Namespace = ((elementAccessor.Mapping.Namespace == null) ? defaultNamespace : elementAccessor.Mapping.Namespace);
			elementAccessor.Form = XmlSchemaForm.Qualified;
			XmlTypeMapping xmlTypeMapping = new XmlTypeMapping(this.typeScope, elementAccessor);
			xmlTypeMapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, defaultNamespace));
			xmlTypeMapping.IsSoap = true;
			xmlTypeMapping.GenerateSerializer = true;
			return xmlTypeMapping;
		}

		/// <summary>Generates internal type mappings for information that is gathered from a Web service method. </summary>
		/// <param name="elementName">An XML element name produced from the Web service method.</param>
		/// <param name="ns">An XML element namespace produced from the Web service method.</param>
		/// <param name="members">An array of .NET Framework code entities that belong to a Web service method.</param>
		/// <returns>Internal .NET Framework type mappings to the element parts of a WSDL message definition.</returns>
		// Token: 0x06001A00 RID: 6656 RVA: 0x00094F14 File Offset: 0x00093114
		public XmlMembersMapping ImportMembersMapping(string elementName, string ns, XmlReflectionMember[] members)
		{
			return this.ImportMembersMapping(elementName, ns, members, true, true, false);
		}

		/// <summary>Generates internal type mappings for information that is gathered from a Web service method. </summary>
		/// <param name="elementName">An XML element name produced from the Web service method.</param>
		/// <param name="ns">An XML element namespace produced from the Web service method.</param>
		/// <param name="members">An array of .NET Framework code entities that belong to a Web service method.</param>
		/// <param name="hasWrapperElement">
		///       <see langword="true" /> to indicate that elements that correspond to WSDL message parts should be enclosed in an extra wrapper element in a SOAP message; otherwise, <see langword="false" />.</param>
		/// <param name="writeAccessors">
		///       <see langword="true" /> to indicate an RPC-style Web service binding; <see langword="false" /> to indicate a document-style Web service binding or a SOAP header.</param>
		/// <returns>Internal .NET Framework type mappings to the element parts of a WSDL message definition.</returns>
		// Token: 0x06001A01 RID: 6657 RVA: 0x00094F22 File Offset: 0x00093122
		public XmlMembersMapping ImportMembersMapping(string elementName, string ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors)
		{
			return this.ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, false);
		}

		/// <summary>Generates internal type mappings for information that is gathered from a Web service method. </summary>
		/// <param name="elementName">An XML element name produced from the Web service method.</param>
		/// <param name="ns">An XML element namespace produced from the Web service method.</param>
		/// <param name="members">An array of .NET Framework code entities that belong to a Web service method.</param>
		/// <param name="hasWrapperElement">
		///       <see langword="true" /> to indicate that elements that correspond to WSDL message parts should be enclosed in an extra wrapper element in a SOAP message; otherwise, <see langword="false" />.</param>
		/// <param name="writeAccessors">
		///       <see langword="true" /> to indicate an RPC-style Web service binding; <see langword="false" /> to indicate a document-style Web service binding or a SOAP header.</param>
		/// <param name="validate">
		///       <see langword="true" /> to indicate that a generated deserializer should check for the expected qualified name of the wrapper element; otherwise, <see langword="false" />. This parameter's value is relevant only if the <paramref name="hasWrapperElement" /> parameter's value is <see langword="true" />.</param>
		/// <returns>Internal .NET Framework type mappings to the element parts of a WSDL message definition.</returns>
		// Token: 0x06001A02 RID: 6658 RVA: 0x00094F32 File Offset: 0x00093132
		public XmlMembersMapping ImportMembersMapping(string elementName, string ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate)
		{
			return this.ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, validate, XmlMappingAccess.Read | XmlMappingAccess.Write);
		}

		/// <summary>Generates internal type mappings for information that is gathered from a Web service method.</summary>
		/// <param name="elementName">An XML element name produced from the Web service method.</param>
		/// <param name="ns">An XML element namespace produced from the Web service method.</param>
		/// <param name="members">An array of .NET Framework code entities that belong to a Web service method.</param>
		/// <param name="hasWrapperElement">
		///       <see langword="true" /> to indicate that elements that correspond to WSDL message parts should be enclosed in an extra wrapper element in a SOAP message; otherwise, <see langword="false" />.</param>
		/// <param name="writeAccessors">
		///       <see langword="true" /> to indicate an RPC-style Web service binding; <see langword="false" /> to indicate a document-style Web service binding or a SOAP header.</param>
		/// <param name="validate">
		///       <see langword="true" /> to indicate that a generated deserializer should check for the expected qualified name of the wrapper element; otherwise, <see langword="false" />. This parameter's value is relevant only if the <paramref name="hasWrapperElement" /> parameter's value is <see langword="true" />.</param>
		/// <param name="access">One of the <see cref="T:System.Xml.Serialization.XmlMappingAccess" /> values.</param>
		/// <returns>Internal .NET Framework type mappings to the element parts of a WSDL message definition.</returns>
		// Token: 0x06001A03 RID: 6659 RVA: 0x00094F44 File Offset: 0x00093144
		public XmlMembersMapping ImportMembersMapping(string elementName, string ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, XmlMappingAccess access)
		{
			ElementAccessor elementAccessor = new ElementAccessor();
			elementAccessor.IsSoap = true;
			elementAccessor.Name = ((elementName == null || elementName.Length == 0) ? elementName : XmlConvert.EncodeLocalName(elementName));
			elementAccessor.Mapping = this.ImportMembersMapping(members, ns, hasWrapperElement, writeAccessors, validate, new RecursionLimiter());
			elementAccessor.Mapping.TypeName = elementName;
			elementAccessor.Namespace = ((elementAccessor.Mapping.Namespace == null) ? ns : elementAccessor.Mapping.Namespace);
			elementAccessor.Form = XmlSchemaForm.Qualified;
			return new XmlMembersMapping(this.typeScope, elementAccessor, access)
			{
				IsSoap = true,
				GenerateSerializer = true
			};
		}

		// Token: 0x06001A04 RID: 6660 RVA: 0x00094FE1 File Offset: 0x000931E1
		private Exception ReflectionException(string context, Exception e)
		{
			return new InvalidOperationException(Res.GetString("There was an error reflecting '{0}'.", new object[]
			{
				context
			}), e);
		}

		// Token: 0x06001A05 RID: 6661 RVA: 0x00095000 File Offset: 0x00093200
		private SoapAttributes GetAttributes(Type type)
		{
			SoapAttributes soapAttributes = this.attributeOverrides[type];
			if (soapAttributes != null)
			{
				return soapAttributes;
			}
			return new SoapAttributes(type);
		}

		// Token: 0x06001A06 RID: 6662 RVA: 0x00095028 File Offset: 0x00093228
		private SoapAttributes GetAttributes(MemberInfo memberInfo)
		{
			SoapAttributes soapAttributes = this.attributeOverrides[memberInfo.DeclaringType, memberInfo.Name];
			if (soapAttributes != null)
			{
				return soapAttributes;
			}
			return new SoapAttributes(memberInfo);
		}

		// Token: 0x06001A07 RID: 6663 RVA: 0x00095058 File Offset: 0x00093258
		private TypeMapping ImportTypeMapping(TypeModel model, RecursionLimiter limiter)
		{
			return this.ImportTypeMapping(model, string.Empty, limiter);
		}

		// Token: 0x06001A08 RID: 6664 RVA: 0x00095068 File Offset: 0x00093268
		private TypeMapping ImportTypeMapping(TypeModel model, string dataType, RecursionLimiter limiter)
		{
			if (dataType.Length > 0)
			{
				if (!model.TypeDesc.IsPrimitive)
				{
					throw new InvalidOperationException(Res.GetString("'{0}' is an invalid value for the {1} property. The property may only be specified for primitive types.", new object[]
					{
						dataType,
						"SoapElementAttribute.DataType"
					}));
				}
				TypeDesc typeDesc = this.typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
				if (typeDesc == null)
				{
					throw new InvalidOperationException(Res.GetString("Value '{0}' cannot be used for the {1} property. The datatype '{2}' is missing.", new object[]
					{
						dataType,
						"SoapElementAttribute.DataType",
						new XmlQualifiedName(dataType, "http://www.w3.org/2001/XMLSchema").ToString()
					}));
				}
				if (model.TypeDesc.FullName != typeDesc.FullName)
				{
					throw new InvalidOperationException(Res.GetString("'{0}' is an invalid value for the {1} property. {0} cannot be converted to {2}.", new object[]
					{
						dataType,
						"SoapElementAttribute.DataType",
						model.TypeDesc.FullName
					}));
				}
			}
			if ((this.GetAttributes(model.Type).SoapFlags & (SoapAttributeFlags)(-3)) != (SoapAttributeFlags)0)
			{
				throw new InvalidOperationException(Res.GetString("XmlRoot and XmlType attributes may not be specified for the type {0}.", new object[]
				{
					model.Type.FullName
				}));
			}
			switch (model.TypeDesc.Kind)
			{
			case TypeKind.Root:
			case TypeKind.Struct:
			case TypeKind.Class:
				if (model.TypeDesc.IsOptionalValue)
				{
					TypeDesc baseTypeDesc = model.TypeDesc.BaseTypeDesc;
					SoapAttributes attributes = this.GetAttributes(baseTypeDesc.Type);
					string @namespace = this.defaultNs;
					if (attributes.SoapType != null && attributes.SoapType.Namespace != null)
					{
						@namespace = attributes.SoapType.Namespace;
					}
					TypeDesc typeDesc2 = string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc : this.typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
					string typeName = string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc.Name : dataType;
					TypeMapping typeMapping = this.GetTypeMapping(typeName, @namespace, typeDesc2);
					if (typeMapping == null)
					{
						typeMapping = this.ImportTypeMapping(this.modelScope.GetTypeModel(baseTypeDesc.Type), dataType, limiter);
					}
					return this.CreateNullableMapping(typeMapping, model.TypeDesc.Type);
				}
				return this.ImportStructLikeMapping((StructModel)model, limiter);
			case TypeKind.Primitive:
				return this.ImportPrimitiveMapping((PrimitiveModel)model, dataType);
			case TypeKind.Enum:
				return this.ImportEnumMapping((EnumModel)model);
			case TypeKind.Array:
			case TypeKind.Collection:
			case TypeKind.Enumerable:
				return this.ImportArrayLikeMapping((ArrayModel)model, limiter);
			default:
				throw new NotSupportedException(Res.GetString("The type {0} may not be serialized with SOAP-encoded messages. Set the Use for your message to Literal.", new object[]
				{
					model.TypeDesc.FullName
				}));
			}
		}

		// Token: 0x06001A09 RID: 6665 RVA: 0x000952E8 File Offset: 0x000934E8
		private StructMapping CreateRootMapping()
		{
			TypeDesc typeDesc = this.typeScope.GetTypeDesc(typeof(object));
			return new StructMapping
			{
				IsSoap = true,
				TypeDesc = typeDesc,
				Members = new MemberMapping[0],
				IncludeInSchema = false,
				TypeName = "anyType",
				Namespace = "http://www.w3.org/2001/XMLSchema"
			};
		}

		// Token: 0x06001A0A RID: 6666 RVA: 0x00095347 File Offset: 0x00093547
		private StructMapping GetRootMapping()
		{
			if (this.root == null)
			{
				this.root = this.CreateRootMapping();
				this.typeScope.AddTypeMapping(this.root);
			}
			return this.root;
		}

		// Token: 0x06001A0B RID: 6667 RVA: 0x00095374 File Offset: 0x00093574
		private TypeMapping GetTypeMapping(string typeName, string ns, TypeDesc typeDesc)
		{
			TypeMapping typeMapping = (TypeMapping)this.types[typeName, ns];
			if (typeMapping == null)
			{
				return null;
			}
			if (typeMapping.TypeDesc != typeDesc)
			{
				throw new InvalidOperationException(Res.GetString("Types '{0}' and '{1}' both use the XML type name, '{2}', from namespace '{3}'. Use XML attributes to specify a unique XML name and/or namespace for the type.", new object[]
				{
					typeDesc.FullName,
					typeMapping.TypeDesc.FullName,
					typeName,
					ns
				}));
			}
			return typeMapping;
		}

		// Token: 0x06001A0C RID: 6668 RVA: 0x000953D8 File Offset: 0x000935D8
		private NullableMapping CreateNullableMapping(TypeMapping baseMapping, Type type)
		{
			TypeDesc nullableTypeDesc = baseMapping.TypeDesc.GetNullableTypeDesc(type);
			TypeMapping typeMapping = (TypeMapping)this.nullables[baseMapping.TypeName, baseMapping.Namespace];
			NullableMapping nullableMapping;
			if (typeMapping != null)
			{
				if (typeMapping is NullableMapping)
				{
					nullableMapping = (NullableMapping)typeMapping;
					if (nullableMapping.BaseMapping is PrimitiveMapping && baseMapping is PrimitiveMapping)
					{
						return nullableMapping;
					}
					if (nullableMapping.BaseMapping == baseMapping)
					{
						return nullableMapping;
					}
					throw new InvalidOperationException(Res.GetString("Types '{0}' and '{1}' both use the XML type name, '{2}', from namespace '{3}'. Use XML attributes to specify a unique XML name and/or namespace for the type.", new object[]
					{
						nullableTypeDesc.FullName,
						typeMapping.TypeDesc.FullName,
						nullableTypeDesc.Name,
						typeMapping.Namespace
					}));
				}
				else if (!(baseMapping is PrimitiveMapping))
				{
					throw new InvalidOperationException(Res.GetString("Types '{0}' and '{1}' both use the XML type name, '{2}', from namespace '{3}'. Use XML attributes to specify a unique XML name and/or namespace for the type.", new object[]
					{
						nullableTypeDesc.FullName,
						typeMapping.TypeDesc.FullName,
						nullableTypeDesc.Name,
						typeMapping.Namespace
					}));
				}
			}
			nullableMapping = new NullableMapping();
			nullableMapping.BaseMapping = baseMapping;
			nullableMapping.TypeDesc = nullableTypeDesc;
			nullableMapping.TypeName = baseMapping.TypeName;
			nullableMapping.Namespace = baseMapping.Namespace;
			nullableMapping.IncludeInSchema = false;
			this.nullables.Add(baseMapping.TypeName, nullableMapping.Namespace, nullableMapping);
			this.typeScope.AddTypeMapping(nullableMapping);
			return nullableMapping;
		}

		// Token: 0x06001A0D RID: 6669 RVA: 0x00095524 File Offset: 0x00093724
		private StructMapping ImportStructLikeMapping(StructModel model, RecursionLimiter limiter)
		{
			if (model.TypeDesc.Kind == TypeKind.Root)
			{
				return this.GetRootMapping();
			}
			SoapAttributes attributes = this.GetAttributes(model.Type);
			string @namespace = this.defaultNs;
			if (attributes.SoapType != null && attributes.SoapType.Namespace != null)
			{
				@namespace = attributes.SoapType.Namespace;
			}
			string text = this.XsdTypeName(model.Type, attributes, model.TypeDesc.Name);
			text = XmlConvert.EncodeLocalName(text);
			StructMapping structMapping = (StructMapping)this.GetTypeMapping(text, @namespace, model.TypeDesc);
			if (structMapping == null)
			{
				structMapping = new StructMapping();
				structMapping.IsSoap = true;
				structMapping.TypeDesc = model.TypeDesc;
				structMapping.Namespace = @namespace;
				structMapping.TypeName = text;
				if (attributes.SoapType != null)
				{
					structMapping.IncludeInSchema = attributes.SoapType.IncludeInSchema;
				}
				this.typeScope.AddTypeMapping(structMapping);
				this.types.Add(text, @namespace, structMapping);
				if (limiter.IsExceededLimit)
				{
					limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, structMapping));
					return structMapping;
				}
				int depth = limiter.Depth;
				limiter.Depth = depth + 1;
				this.InitializeStructMembers(structMapping, model, limiter);
				while (limiter.DeferredWorkItems.Count > 0)
				{
					int index = limiter.DeferredWorkItems.Count - 1;
					ImportStructWorkItem importStructWorkItem = limiter.DeferredWorkItems[index];
					if (this.InitializeStructMembers(importStructWorkItem.Mapping, importStructWorkItem.Model, limiter))
					{
						limiter.DeferredWorkItems.RemoveAt(index);
					}
				}
				depth = limiter.Depth;
				limiter.Depth = depth - 1;
			}
			return structMapping;
		}

		// Token: 0x06001A0E RID: 6670 RVA: 0x000956AC File Offset: 0x000938AC
		private bool InitializeStructMembers(StructMapping mapping, StructModel model, RecursionLimiter limiter)
		{
			if (mapping.IsFullyInitialized)
			{
				return true;
			}
			if (model.TypeDesc.BaseTypeDesc != null)
			{
				StructMapping baseMapping = this.ImportStructLikeMapping((StructModel)this.modelScope.GetTypeModel(model.Type.BaseType, false), limiter);
				int num = limiter.DeferredWorkItems.IndexOf(mapping.BaseMapping);
				if (num >= 0)
				{
					if (!limiter.DeferredWorkItems.Contains(mapping))
					{
						limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, mapping));
					}
					int num2 = limiter.DeferredWorkItems.Count - 1;
					if (num < num2)
					{
						ImportStructWorkItem value = limiter.DeferredWorkItems[num];
						limiter.DeferredWorkItems[num] = limiter.DeferredWorkItems[num2];
						limiter.DeferredWorkItems[num2] = value;
					}
					return false;
				}
				mapping.BaseMapping = baseMapping;
			}
			ArrayList arrayList = new ArrayList();
			foreach (MemberInfo memberInfo in model.GetMemberInfos())
			{
				if ((memberInfo.MemberType & (MemberTypes.Field | MemberTypes.Property)) != (MemberTypes)0)
				{
					SoapAttributes attributes = this.GetAttributes(memberInfo);
					if (!attributes.SoapIgnore)
					{
						FieldModel fieldModel = model.GetFieldModel(memberInfo);
						if (fieldModel != null)
						{
							MemberMapping memberMapping = this.ImportFieldMapping(fieldModel, attributes, mapping.Namespace, limiter);
							if (memberMapping != null)
							{
								if (!memberMapping.TypeDesc.IsPrimitive && !memberMapping.TypeDesc.IsEnum && !memberMapping.TypeDesc.IsOptionalValue)
								{
									if (model.TypeDesc.IsValueType)
									{
										throw new NotSupportedException(Res.GetString("Cannot serialize {0}. References in structs are not supported with encoded SOAP.", new object[]
										{
											model.TypeDesc.FullName
										}));
									}
									if (memberMapping.TypeDesc.IsValueType)
									{
										throw new NotSupportedException(Res.GetString("Cannot serialize {0}. Nested structs are not supported with encoded SOAP.", new object[]
										{
											memberMapping.TypeDesc.FullName
										}));
									}
								}
								if (mapping.BaseMapping == null || !mapping.BaseMapping.Declares(memberMapping, mapping.TypeName))
								{
									arrayList.Add(memberMapping);
								}
							}
						}
					}
				}
			}
			mapping.Members = (MemberMapping[])arrayList.ToArray(typeof(MemberMapping));
			if (mapping.BaseMapping == null)
			{
				mapping.BaseMapping = this.GetRootMapping();
			}
			this.IncludeTypes(model.Type, limiter);
			return true;
		}

		// Token: 0x06001A0F RID: 6671 RVA: 0x000958F0 File Offset: 0x00093AF0
		private ArrayMapping ImportArrayLikeMapping(ArrayModel model, RecursionLimiter limiter)
		{
			ArrayMapping arrayMapping = new ArrayMapping();
			arrayMapping.IsSoap = true;
			TypeMapping typeMapping = this.ImportTypeMapping(model.Element, limiter);
			if (typeMapping.TypeDesc.IsValueType && !typeMapping.TypeDesc.IsPrimitive && !typeMapping.TypeDesc.IsEnum)
			{
				throw new NotSupportedException(Res.GetString("Cannot serialize {0}. Arrays of structs are not supported with encoded SOAP.", new object[]
				{
					model.TypeDesc.FullName
				}));
			}
			arrayMapping.TypeDesc = model.TypeDesc;
			arrayMapping.Elements = new ElementAccessor[]
			{
				SoapReflectionImporter.CreateElementAccessor(typeMapping, arrayMapping.Namespace)
			};
			this.SetArrayMappingType(arrayMapping);
			ArrayMapping arrayMapping2 = (ArrayMapping)this.types[arrayMapping.TypeName, arrayMapping.Namespace];
			if (arrayMapping2 != null)
			{
				ArrayMapping next = arrayMapping2;
				while (arrayMapping2 != null)
				{
					if (arrayMapping2.TypeDesc == model.TypeDesc)
					{
						return arrayMapping2;
					}
					arrayMapping2 = arrayMapping2.Next;
				}
				arrayMapping.Next = next;
				this.types[arrayMapping.TypeName, arrayMapping.Namespace] = arrayMapping;
				return arrayMapping;
			}
			this.typeScope.AddTypeMapping(arrayMapping);
			this.types.Add(arrayMapping.TypeName, arrayMapping.Namespace, arrayMapping);
			this.IncludeTypes(model.Type);
			return arrayMapping;
		}

		// Token: 0x06001A10 RID: 6672 RVA: 0x00095A24 File Offset: 0x00093C24
		private void SetArrayMappingType(ArrayMapping mapping)
		{
			bool flag = false;
			TypeMapping typeMapping;
			if (mapping.Elements.Length == 1)
			{
				typeMapping = mapping.Elements[0].Mapping;
			}
			else
			{
				typeMapping = null;
			}
			string text;
			string text2;
			if (typeMapping is EnumMapping)
			{
				text = typeMapping.Namespace;
				text2 = typeMapping.TypeName;
			}
			else if (typeMapping is PrimitiveMapping)
			{
				text = (typeMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : "http://microsoft.com/wsdl/types/");
				text2 = typeMapping.TypeDesc.DataType.Name;
				flag = true;
			}
			else if (typeMapping is StructMapping)
			{
				if (typeMapping.TypeDesc.IsRoot)
				{
					text = "http://www.w3.org/2001/XMLSchema";
					text2 = "anyType";
					flag = true;
				}
				else
				{
					text = typeMapping.Namespace;
					text2 = typeMapping.TypeName;
				}
			}
			else
			{
				if (!(typeMapping is ArrayMapping))
				{
					throw new InvalidOperationException(Res.GetString("An array of type {0} may not be used with XmlArrayType.Soap.", new object[]
					{
						mapping.TypeDesc.FullName
					}));
				}
				text = typeMapping.Namespace;
				text2 = typeMapping.TypeName;
			}
			text2 = CodeIdentifier.MakePascal(text2);
			string text3 = "ArrayOf" + text2;
			string text4 = flag ? this.defaultNs : text;
			int num = 1;
			TypeMapping typeMapping2 = (TypeMapping)this.types[text3, text4];
			while (typeMapping2 != null && (!(typeMapping2 is ArrayMapping) || !AccessorMapping.ElementsMatch(((ArrayMapping)typeMapping2).Elements, mapping.Elements)))
			{
				text3 = text2 + num.ToString(CultureInfo.InvariantCulture);
				typeMapping2 = (TypeMapping)this.types[text3, text4];
				num++;
			}
			mapping.Namespace = text4;
			mapping.TypeName = text3;
		}

		// Token: 0x06001A11 RID: 6673 RVA: 0x00095BB8 File Offset: 0x00093DB8
		private PrimitiveMapping ImportPrimitiveMapping(PrimitiveModel model, string dataType)
		{
			PrimitiveMapping primitiveMapping = new PrimitiveMapping();
			primitiveMapping.IsSoap = true;
			if (dataType.Length > 0)
			{
				primitiveMapping.TypeDesc = this.typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
				if (primitiveMapping.TypeDesc == null)
				{
					primitiveMapping.TypeDesc = this.typeScope.GetTypeDesc(dataType, "http://microsoft.com/wsdl/types/");
					if (primitiveMapping.TypeDesc == null)
					{
						throw new InvalidOperationException(Res.GetString("The type, {0}, is undeclared.", new object[]
						{
							dataType
						}));
					}
				}
			}
			else
			{
				primitiveMapping.TypeDesc = model.TypeDesc;
			}
			primitiveMapping.TypeName = primitiveMapping.TypeDesc.DataType.Name;
			primitiveMapping.Namespace = (primitiveMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : "http://microsoft.com/wsdl/types/");
			return primitiveMapping;
		}

		// Token: 0x06001A12 RID: 6674 RVA: 0x00095C78 File Offset: 0x00093E78
		private EnumMapping ImportEnumMapping(EnumModel model)
		{
			SoapAttributes attributes = this.GetAttributes(model.Type);
			string @namespace = this.defaultNs;
			if (attributes.SoapType != null && attributes.SoapType.Namespace != null)
			{
				@namespace = attributes.SoapType.Namespace;
			}
			string text = this.XsdTypeName(model.Type, attributes, model.TypeDesc.Name);
			text = XmlConvert.EncodeLocalName(text);
			EnumMapping enumMapping = (EnumMapping)this.GetTypeMapping(text, @namespace, model.TypeDesc);
			if (enumMapping == null)
			{
				enumMapping = new EnumMapping();
				enumMapping.IsSoap = true;
				enumMapping.TypeDesc = model.TypeDesc;
				enumMapping.TypeName = text;
				enumMapping.Namespace = @namespace;
				enumMapping.IsFlags = model.Type.IsDefined(typeof(FlagsAttribute), false);
				this.typeScope.AddTypeMapping(enumMapping);
				this.types.Add(text, @namespace, enumMapping);
				ArrayList arrayList = new ArrayList();
				for (int i = 0; i < model.Constants.Length; i++)
				{
					ConstantMapping constantMapping = this.ImportConstantMapping(model.Constants[i]);
					if (constantMapping != null)
					{
						arrayList.Add(constantMapping);
					}
				}
				if (arrayList.Count == 0)
				{
					throw new InvalidOperationException(Res.GetString("Cannot serialize object of type '{0}'. The object does not have serializable members.", new object[]
					{
						model.TypeDesc.FullName
					}));
				}
				enumMapping.Constants = (ConstantMapping[])arrayList.ToArray(typeof(ConstantMapping));
			}
			return enumMapping;
		}

		// Token: 0x06001A13 RID: 6675 RVA: 0x00095DD8 File Offset: 0x00093FD8
		private ConstantMapping ImportConstantMapping(ConstantModel model)
		{
			SoapAttributes attributes = this.GetAttributes(model.FieldInfo);
			if (attributes.SoapIgnore)
			{
				return null;
			}
			if ((attributes.SoapFlags & (SoapAttributeFlags)(-2)) != (SoapAttributeFlags)0)
			{
				throw new InvalidOperationException(Res.GetString("Only SoapEnum may be used on enum constants."));
			}
			if (attributes.SoapEnum == null)
			{
				attributes.SoapEnum = new SoapEnumAttribute();
			}
			return new ConstantMapping
			{
				XmlName = ((attributes.SoapEnum.Name.Length == 0) ? model.Name : attributes.SoapEnum.Name),
				Name = model.Name,
				Value = model.Value
			};
		}

		// Token: 0x06001A14 RID: 6676 RVA: 0x00095E74 File Offset: 0x00094074
		private MembersMapping ImportMembersMapping(XmlReflectionMember[] xmlReflectionMembers, string ns, bool hasWrapperElement, bool writeAccessors, bool validateWrapperElement, RecursionLimiter limiter)
		{
			MembersMapping membersMapping = new MembersMapping();
			membersMapping.TypeDesc = this.typeScope.GetTypeDesc(typeof(object[]));
			MemberMapping[] array = new MemberMapping[xmlReflectionMembers.Length];
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					XmlReflectionMember xmlReflectionMember = xmlReflectionMembers[i];
					MemberMapping memberMapping = this.ImportMemberMapping(xmlReflectionMember, ns, xmlReflectionMembers, hasWrapperElement ? XmlSchemaForm.Unqualified : XmlSchemaForm.Qualified, limiter);
					if (xmlReflectionMember.IsReturnValue && writeAccessors)
					{
						if (i > 0)
						{
							throw new InvalidOperationException(Res.GetString("The return value must be the first member."));
						}
						memberMapping.IsReturnValue = true;
					}
					array[i] = memberMapping;
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
					{
						throw;
					}
					throw this.ReflectionException(xmlReflectionMembers[i].MemberName, ex);
				}
			}
			membersMapping.Members = array;
			membersMapping.HasWrapperElement = hasWrapperElement;
			if (hasWrapperElement)
			{
				membersMapping.ValidateRpcWrapperElement = validateWrapperElement;
			}
			membersMapping.WriteAccessors = writeAccessors;
			membersMapping.IsSoap = true;
			if (hasWrapperElement && !writeAccessors)
			{
				membersMapping.Namespace = ns;
			}
			return membersMapping;
		}

		// Token: 0x06001A15 RID: 6677 RVA: 0x00095F7C File Offset: 0x0009417C
		private MemberMapping ImportMemberMapping(XmlReflectionMember xmlReflectionMember, string ns, XmlReflectionMember[] xmlReflectionMembers, XmlSchemaForm form, RecursionLimiter limiter)
		{
			SoapAttributes soapAttributes = xmlReflectionMember.SoapAttributes;
			if (soapAttributes.SoapIgnore)
			{
				return null;
			}
			MemberMapping memberMapping = new MemberMapping();
			memberMapping.IsSoap = true;
			memberMapping.Name = xmlReflectionMember.MemberName;
			bool checkSpecified = XmlReflectionImporter.FindSpecifiedMember(xmlReflectionMember.MemberName, xmlReflectionMembers) != null;
			FieldModel fieldModel = new FieldModel(xmlReflectionMember.MemberName, xmlReflectionMember.MemberType, this.typeScope.GetTypeDesc(xmlReflectionMember.MemberType), checkSpecified, false);
			memberMapping.CheckShouldPersist = fieldModel.CheckShouldPersist;
			memberMapping.CheckSpecified = fieldModel.CheckSpecified;
			memberMapping.ReadOnly = fieldModel.ReadOnly;
			this.ImportAccessorMapping(memberMapping, fieldModel, soapAttributes, ns, form, limiter);
			if (xmlReflectionMember.OverrideIsNullable)
			{
				memberMapping.Elements[0].IsNullable = false;
			}
			return memberMapping;
		}

		// Token: 0x06001A16 RID: 6678 RVA: 0x00096034 File Offset: 0x00094234
		private MemberMapping ImportFieldMapping(FieldModel model, SoapAttributes a, string ns, RecursionLimiter limiter)
		{
			if (a.SoapIgnore)
			{
				return null;
			}
			MemberMapping memberMapping = new MemberMapping();
			memberMapping.IsSoap = true;
			memberMapping.Name = model.Name;
			memberMapping.CheckShouldPersist = model.CheckShouldPersist;
			memberMapping.CheckSpecified = model.CheckSpecified;
			memberMapping.MemberInfo = model.MemberInfo;
			memberMapping.CheckSpecifiedMemberInfo = model.CheckSpecifiedMemberInfo;
			memberMapping.CheckShouldPersistMethodInfo = model.CheckShouldPersistMethodInfo;
			memberMapping.ReadOnly = model.ReadOnly;
			this.ImportAccessorMapping(memberMapping, model, a, ns, XmlSchemaForm.Unqualified, limiter);
			return memberMapping;
		}

		// Token: 0x06001A17 RID: 6679 RVA: 0x000960BC File Offset: 0x000942BC
		private void ImportAccessorMapping(MemberMapping accessor, FieldModel model, SoapAttributes a, string ns, XmlSchemaForm form, RecursionLimiter limiter)
		{
			Type fieldType = model.FieldType;
			string name = model.Name;
			accessor.TypeDesc = this.typeScope.GetTypeDesc(fieldType);
			if (accessor.TypeDesc.IsVoid)
			{
				throw new InvalidOperationException(Res.GetString("The type Void is not valid in this context."));
			}
			SoapAttributeFlags soapFlags = a.SoapFlags;
			if ((soapFlags & SoapAttributeFlags.Attribute) == SoapAttributeFlags.Attribute)
			{
				if (!accessor.TypeDesc.IsPrimitive && !accessor.TypeDesc.IsEnum)
				{
					throw new InvalidOperationException(Res.GetString("Cannot serialize member '{0}' of type {1}. SoapAttribute cannot be used to encode complex types.", new object[]
					{
						name,
						accessor.TypeDesc.FullName
					}));
				}
				if ((soapFlags & SoapAttributeFlags.Attribute) != soapFlags)
				{
					throw new InvalidOperationException(Res.GetString("Only SoapElementAttribute or SoapAttributeAttribute may be used on members."));
				}
				accessor.Attribute = new AttributeAccessor
				{
					Name = Accessor.EscapeQName((a.SoapAttribute == null || a.SoapAttribute.AttributeName.Length == 0) ? name : a.SoapAttribute.AttributeName),
					Namespace = ((a.SoapAttribute == null || a.SoapAttribute.Namespace == null) ? ns : a.SoapAttribute.Namespace),
					Form = XmlSchemaForm.Qualified,
					Mapping = this.ImportTypeMapping(this.modelScope.GetTypeModel(fieldType), (a.SoapAttribute == null) ? string.Empty : a.SoapAttribute.DataType, limiter),
					Default = this.GetDefaultValue(model.FieldTypeDesc, a)
				};
				accessor.Elements = new ElementAccessor[0];
				return;
			}
			else
			{
				if ((soapFlags & SoapAttributeFlags.Element) != soapFlags)
				{
					throw new InvalidOperationException(Res.GetString("Only SoapElementAttribute or SoapAttributeAttribute may be used on members."));
				}
				ElementAccessor elementAccessor = new ElementAccessor();
				elementAccessor.IsSoap = true;
				elementAccessor.Name = XmlConvert.EncodeLocalName((a.SoapElement == null || a.SoapElement.ElementName.Length == 0) ? name : a.SoapElement.ElementName);
				elementAccessor.Namespace = ns;
				elementAccessor.Form = form;
				elementAccessor.Mapping = this.ImportTypeMapping(this.modelScope.GetTypeModel(fieldType), (a.SoapElement == null) ? string.Empty : a.SoapElement.DataType, limiter);
				if (a.SoapElement != null)
				{
					elementAccessor.IsNullable = a.SoapElement.IsNullable;
				}
				accessor.Elements = new ElementAccessor[]
				{
					elementAccessor
				};
				return;
			}
		}

		// Token: 0x06001A18 RID: 6680 RVA: 0x000962FE File Offset: 0x000944FE
		private static ElementAccessor CreateElementAccessor(TypeMapping mapping, string ns)
		{
			return new ElementAccessor
			{
				IsSoap = true,
				Name = mapping.TypeName,
				Namespace = ns,
				Mapping = mapping
			};
		}

		// Token: 0x06001A19 RID: 6681 RVA: 0x00096328 File Offset: 0x00094528
		private object GetDefaultValue(TypeDesc fieldTypeDesc, SoapAttributes a)
		{
			if (a.SoapDefaultValue == null || a.SoapDefaultValue == DBNull.Value)
			{
				return null;
			}
			if (fieldTypeDesc.Kind != TypeKind.Primitive && fieldTypeDesc.Kind != TypeKind.Enum)
			{
				a.SoapDefaultValue = null;
				return a.SoapDefaultValue;
			}
			if (fieldTypeDesc.Kind != TypeKind.Enum)
			{
				return a.SoapDefaultValue;
			}
			if (fieldTypeDesc != this.typeScope.GetTypeDesc(a.SoapDefaultValue.GetType()))
			{
				throw new InvalidOperationException(Res.GetString("Enum {0} cannot be converted to {1}.", new object[]
				{
					a.SoapDefaultValue.GetType().FullName,
					fieldTypeDesc.FullName
				}));
			}
			string text = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "G").Replace(",", " ");
			string b = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "D");
			if (text == b)
			{
				throw new InvalidOperationException(Res.GetString("Value '{0}' cannot be converted to {1}.", new object[]
				{
					text,
					a.SoapDefaultValue.GetType().FullName
				}));
			}
			return text;
		}

		// Token: 0x06001A1A RID: 6682 RVA: 0x00096448 File Offset: 0x00094648
		internal string XsdTypeName(Type type)
		{
			if (type == typeof(object))
			{
				return "anyType";
			}
			TypeDesc typeDesc = this.typeScope.GetTypeDesc(type);
			if (typeDesc.IsPrimitive && typeDesc.DataType != null && typeDesc.DataType.Name != null && typeDesc.DataType.Name.Length > 0)
			{
				return typeDesc.DataType.Name;
			}
			return this.XsdTypeName(type, this.GetAttributes(type), typeDesc.Name);
		}

		// Token: 0x06001A1B RID: 6683 RVA: 0x000964CC File Offset: 0x000946CC
		internal string XsdTypeName(Type type, SoapAttributes a, string name)
		{
			string text = name;
			if (a.SoapType != null && a.SoapType.TypeName.Length > 0)
			{
				text = a.SoapType.TypeName;
			}
			if (type.IsGenericType && text.IndexOf('{') >= 0)
			{
				Type[] genericArguments = type.GetGenericTypeDefinition().GetGenericArguments();
				Type[] genericArguments2 = type.GetGenericArguments();
				for (int i = 0; i < genericArguments.Length; i++)
				{
					string str = "{";
					Type type2 = genericArguments[i];
					string text2 = str + ((type2 != null) ? type2.ToString() : null) + "}";
					if (text.Contains(text2))
					{
						text = text.Replace(text2, this.XsdTypeName(genericArguments2[i]));
						if (text.IndexOf('{') < 0)
						{
							break;
						}
					}
				}
			}
			return text;
		}

		// Token: 0x04001952 RID: 6482
		private TypeScope typeScope;

		// Token: 0x04001953 RID: 6483
		private SoapAttributeOverrides attributeOverrides;

		// Token: 0x04001954 RID: 6484
		private NameTable types = new NameTable();

		// Token: 0x04001955 RID: 6485
		private NameTable nullables = new NameTable();

		// Token: 0x04001956 RID: 6486
		private StructMapping root;

		// Token: 0x04001957 RID: 6487
		private string defaultNs;

		// Token: 0x04001958 RID: 6488
		private ModelScope modelScope;
	}
}
