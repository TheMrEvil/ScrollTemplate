﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.XmlConfiguration;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt
{
	// Token: 0x0200041D RID: 1053
	internal class XsltLoader : IErrorHelper
	{
		// Token: 0x060029AB RID: 10667 RVA: 0x000F8EEC File Offset: 0x000F70EC
		public void Load(Compiler compiler, object stylesheet, XmlResolver xmlResolver)
		{
			this.compiler = compiler;
			this.xmlResolver = (xmlResolver ?? XmlNullResolver.Singleton);
			XmlReader xmlReader = stylesheet as XmlReader;
			if (xmlReader != null)
			{
				this.readerSettings = new QueryReaderSettings(xmlReader);
				this.Load(xmlReader);
			}
			else
			{
				string text = stylesheet as string;
				if (text != null)
				{
					XmlResolver xmlResolver2 = xmlResolver;
					if (xmlResolver == null || xmlResolver == XmlNullResolver.Singleton)
					{
						xmlResolver2 = new XmlUrlResolver();
					}
					Uri uri = xmlResolver2.ResolveUri(null, text);
					if (uri == null)
					{
						throw new XslLoadException("Cannot resolve the referenced document '{0}'.", new string[]
						{
							text
						});
					}
					this.readerSettings = new QueryReaderSettings(new NameTable());
					XmlReader xmlReader2;
					xmlReader = (xmlReader2 = this.CreateReader(uri, xmlResolver2));
					try
					{
						this.Load(xmlReader);
						goto IL_DF;
					}
					finally
					{
						if (xmlReader2 != null)
						{
							((IDisposable)xmlReader2).Dispose();
						}
					}
				}
				IXPathNavigable ixpathNavigable = stylesheet as IXPathNavigable;
				if (ixpathNavigable != null)
				{
					xmlReader = XPathNavigatorReader.Create(ixpathNavigable.CreateNavigator());
					this.readerSettings = new QueryReaderSettings(xmlReader.NameTable);
					this.Load(xmlReader);
				}
			}
			IL_DF:
			compiler.StartApplyTemplates = AstFactory.ApplyTemplates(XsltLoader.nullMode);
			this.ProcessOutputSettings();
			foreach (AttributeSet attSet in compiler.AttributeSets.Values)
			{
				this.CheckAttributeSetsDfs(attSet);
			}
		}

		// Token: 0x060029AC RID: 10668 RVA: 0x000F9048 File Offset: 0x000F7248
		private void Load(XmlReader reader)
		{
			this.atoms = new KeywordsTable(reader.NameTable);
			this.AtomizeAttributes();
			this.LoadStylesheet(reader, false);
		}

		// Token: 0x060029AD RID: 10669 RVA: 0x000F906C File Offset: 0x000F726C
		private void AtomizeAttributes(XsltInput.XsltAttribute[] attributes)
		{
			for (int i = 0; i < attributes.Length; i++)
			{
				attributes[i].name = this.atoms.NameTable.Add(attributes[i].name);
			}
		}

		// Token: 0x060029AE RID: 10670 RVA: 0x000F90B0 File Offset: 0x000F72B0
		private void AtomizeAttributes()
		{
			this.AtomizeAttributes(this.stylesheetAttributes);
			this.AtomizeAttributes(this.importIncludeAttributes);
			this.AtomizeAttributes(this.loadStripSpaceAttributes);
			this.AtomizeAttributes(this.outputAttributes);
			this.AtomizeAttributes(this.keyAttributes);
			this.AtomizeAttributes(this.decimalFormatAttributes);
			this.AtomizeAttributes(this.namespaceAliasAttributes);
			this.AtomizeAttributes(this.attributeSetAttributes);
			this.AtomizeAttributes(this.templateAttributes);
			this.AtomizeAttributes(this.scriptAttributes);
			this.AtomizeAttributes(this.assemblyAttributes);
			this.AtomizeAttributes(this.usingAttributes);
			this.AtomizeAttributes(this.applyTemplatesAttributes);
			this.AtomizeAttributes(this.callTemplateAttributes);
			this.AtomizeAttributes(this.copyAttributes);
			this.AtomizeAttributes(this.copyOfAttributes);
			this.AtomizeAttributes(this.ifAttributes);
			this.AtomizeAttributes(this.forEachAttributes);
			this.AtomizeAttributes(this.messageAttributes);
			this.AtomizeAttributes(this.numberAttributes);
			this.AtomizeAttributes(this.valueOfAttributes);
			this.AtomizeAttributes(this.variableAttributes);
			this.AtomizeAttributes(this.paramAttributes);
			this.AtomizeAttributes(this.withParamAttributes);
			this.AtomizeAttributes(this.commentAttributes);
			this.AtomizeAttributes(this.processingInstructionAttributes);
			this.AtomizeAttributes(this.textAttributes);
			this.AtomizeAttributes(this.elementAttributes);
			this.AtomizeAttributes(this.attributeAttributes);
			this.AtomizeAttributes(this.sortAttributes);
		}

		// Token: 0x170007F3 RID: 2035
		// (get) Token: 0x060029AF RID: 10671 RVA: 0x000F9225 File Offset: 0x000F7425
		private bool V1
		{
			get
			{
				return this.compiler.Version == 1;
			}
		}

		// Token: 0x060029B0 RID: 10672 RVA: 0x000F9238 File Offset: 0x000F7438
		private Uri ResolveUri(string relativeUri, string baseUri)
		{
			Uri baseUri2 = (baseUri.Length != 0) ? this.xmlResolver.ResolveUri(null, baseUri) : null;
			Uri uri = this.xmlResolver.ResolveUri(baseUri2, relativeUri);
			if (uri == null)
			{
				throw new XslLoadException("Cannot resolve the referenced document '{0}'.", new string[]
				{
					relativeUri
				});
			}
			return uri;
		}

		// Token: 0x060029B1 RID: 10673 RVA: 0x000F928C File Offset: 0x000F748C
		private XmlReader CreateReader(Uri uri, XmlResolver xmlResolver)
		{
			object entity = xmlResolver.GetEntity(uri, null, null);
			Stream stream = entity as Stream;
			if (stream != null)
			{
				return this.readerSettings.CreateReader(stream, uri.ToString());
			}
			XmlReader xmlReader = entity as XmlReader;
			if (xmlReader != null)
			{
				return xmlReader;
			}
			IXPathNavigable ixpathNavigable = entity as IXPathNavigable;
			if (ixpathNavigable != null)
			{
				return XPathNavigatorReader.Create(ixpathNavigable.CreateNavigator());
			}
			throw new XslLoadException("Cannot load the stylesheet object referenced by URI '{0}', because the provided XmlResolver returned an object of type '{1}'. One of Stream, XmlReader, and IXPathNavigable types was expected.", new string[]
			{
				uri.ToString(),
				(entity == null) ? "null" : entity.GetType().ToString()
			});
		}

		// Token: 0x060029B2 RID: 10674 RVA: 0x000F9314 File Offset: 0x000F7514
		private Stylesheet LoadStylesheet(Uri uri, bool include)
		{
			Stylesheet result;
			using (XmlReader xmlReader = this.CreateReader(uri, this.xmlResolver))
			{
				result = this.LoadStylesheet(xmlReader, include);
			}
			return result;
		}

		// Token: 0x060029B3 RID: 10675 RVA: 0x000F9358 File Offset: 0x000F7558
		private Stylesheet LoadStylesheet(XmlReader reader, bool include)
		{
			string baseURI = reader.BaseURI;
			this.documentUriInUse.Add(baseURI, null);
			this.compiler.AddModule(baseURI);
			Stylesheet stylesheet = this.curStylesheet;
			XsltInput xsltInput = this.input;
			Stylesheet result = include ? this.curStylesheet : this.compiler.CreateStylesheet();
			this.input = new XsltInput(reader, this.compiler, this.atoms);
			this.curStylesheet = result;
			try
			{
				this.LoadDocument();
				if (!include)
				{
					this.compiler.MergeWithStylesheet(this.curStylesheet);
					List<Uri> importHrefs = this.curStylesheet.ImportHrefs;
					this.curStylesheet.Imports = new Stylesheet[importHrefs.Count];
					int num = importHrefs.Count;
					while (0 <= --num)
					{
						this.curStylesheet.Imports[num] = this.LoadStylesheet(importHrefs[num], false);
					}
				}
			}
			catch (XslLoadException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (!XmlException.IsCatchableException(ex))
				{
					throw;
				}
				XmlException ex2 = ex as XmlException;
				ISourceLineInfo sourceLineInfo;
				if (ex2 == null || ex2.SourceUri == null)
				{
					sourceLineInfo = this.input.BuildReaderLineInfo();
				}
				else
				{
					ISourceLineInfo sourceLineInfo2 = new SourceLineInfo(ex2.SourceUri, ex2.LineNumber, ex2.LinePosition, ex2.LineNumber, ex2.LinePosition);
					sourceLineInfo = sourceLineInfo2;
				}
				ISourceLineInfo lineInfo = sourceLineInfo;
				throw new XslLoadException(ex, lineInfo);
			}
			finally
			{
				this.documentUriInUse.Remove(baseURI);
				this.input = xsltInput;
				this.curStylesheet = stylesheet;
			}
			return result;
		}

		// Token: 0x060029B4 RID: 10676 RVA: 0x000F94E8 File Offset: 0x000F76E8
		private void LoadDocument()
		{
			if (!this.input.FindStylesheetElement())
			{
				this.ReportError("Stylesheet must start either with an 'xsl:stylesheet' or an 'xsl:transform' element, or with a literal result element that has an 'xsl:version' attribute, where prefix 'xsl' denotes the 'http://www.w3.org/1999/XSL/Transform' namespace.", Array.Empty<string>());
				return;
			}
			if (this.input.IsXsltNamespace())
			{
				if (this.input.IsKeyword(this.atoms.Stylesheet) || this.input.IsKeyword(this.atoms.Transform))
				{
					this.LoadRealStylesheet();
				}
				else
				{
					this.ReportError("Stylesheet must start either with an 'xsl:stylesheet' or an 'xsl:transform' element, or with a literal result element that has an 'xsl:version' attribute, where prefix 'xsl' denotes the 'http://www.w3.org/1999/XSL/Transform' namespace.", Array.Empty<string>());
					this.input.SkipNode();
				}
			}
			else
			{
				this.LoadSimplifiedStylesheet();
			}
			this.input.Finish();
		}

		// Token: 0x060029B5 RID: 10677 RVA: 0x000F9588 File Offset: 0x000F7788
		private void LoadSimplifiedStylesheet()
		{
			this.curTemplate = AstFactory.Template(null, "/", XsltLoader.nullMode, double.NaN, this.input.XslVersion);
			this.input.CanHaveApplyImports = true;
			XslNode xslNode = this.LoadLiteralResultElement(true);
			if (xslNode != null)
			{
				XsltLoader.SetLineInfo(this.curTemplate, xslNode.SourceLine);
				List<XslNode> list = new List<XslNode>();
				list.Add(xslNode);
				XsltLoader.SetContent(this.curTemplate, list);
				this.curStylesheet.AddTemplate(this.curTemplate);
			}
			this.curTemplate = null;
		}

		// Token: 0x060029B6 RID: 10678 RVA: 0x000F961C File Offset: 0x000F781C
		private void LoadRealStylesheet()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.stylesheetAttributes);
			this.ParseValidationAttribute(2, true);
			this.ParseInputTypeAnnotationsAttribute(3);
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				bool flag = true;
				do
				{
					bool flag2 = false;
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType - XmlNodeType.Whitespace > 1)
						{
							this.ReportError("'{0}' element cannot have text node children.", new string[]
							{
								elementName
							});
						}
					}
					else
					{
						if (this.input.IsXsltNamespace())
						{
							if (this.input.IsKeyword(this.atoms.Import))
							{
								if (!flag)
								{
									this.ReportError("'{0}' element children must precede all other children of the '{1}' element.", new string[]
									{
										this.input.QualifiedName,
										elementName
									});
									this.input.SkipNode();
								}
								else
								{
									flag2 = true;
									this.LoadImport();
								}
							}
							else if (this.input.IsKeyword(this.atoms.Include))
							{
								this.LoadInclude();
							}
							else if (this.input.IsKeyword(this.atoms.StripSpace))
							{
								this.LoadStripSpace(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.PreserveSpace))
							{
								this.LoadPreserveSpace(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.Output))
							{
								this.LoadOutput();
							}
							else if (this.input.IsKeyword(this.atoms.Key))
							{
								this.LoadKey(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.DecimalFormat))
							{
								this.LoadDecimalFormat(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.NamespaceAlias))
							{
								this.LoadNamespaceAlias(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.AttributeSet))
							{
								this.LoadAttributeSet(attributes.nsList);
							}
							else if (this.input.IsKeyword(this.atoms.Variable))
							{
								this.LoadGlobalVariableOrParameter(attributes.nsList, XslNodeType.Variable);
							}
							else if (this.input.IsKeyword(this.atoms.Param))
							{
								this.LoadGlobalVariableOrParameter(attributes.nsList, XslNodeType.Param);
							}
							else if (this.input.IsKeyword(this.atoms.Template))
							{
								this.LoadTemplate(attributes.nsList);
							}
							else
							{
								this.input.GetVersionAttribute();
								if (!this.input.ForwardCompatibility)
								{
									this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
									{
										this.input.QualifiedName,
										elementName
									});
								}
								this.input.SkipNode();
							}
						}
						else if (this.input.IsNs(this.atoms.UrnMsxsl) && this.input.IsKeyword(this.atoms.Script))
						{
							this.LoadMsScript(attributes.nsList);
						}
						else
						{
							if (this.input.IsNullNamespace())
							{
								this.ReportError("Top-level element '{0}' may not have a null namespace URI.", new string[]
								{
									this.input.LocalName
								});
							}
							this.input.SkipNode();
						}
						flag = flag2;
					}
				}
				while (this.input.MoveToNextSibling());
			}
		}

		// Token: 0x060029B7 RID: 10679 RVA: 0x000F99A8 File Offset: 0x000F7BA8
		private void LoadImport()
		{
			this.input.GetAttributes(this.importIncludeAttributes);
			if (this.input.MoveToXsltAttribute(0, "href"))
			{
				Uri uri = this.ResolveUri(this.input.Value, this.input.BaseUri);
				if (this.documentUriInUse.Contains(uri.ToString()))
				{
					this.ReportError("Stylesheet '{0}' cannot directly or indirectly include or import itself.", new string[]
					{
						this.input.Value
					});
				}
				else
				{
					this.curStylesheet.ImportHrefs.Add(uri);
				}
			}
			this.CheckNoContent();
		}

		// Token: 0x060029B8 RID: 10680 RVA: 0x000F9A44 File Offset: 0x000F7C44
		private void LoadInclude()
		{
			this.input.GetAttributes(this.importIncludeAttributes);
			if (this.input.MoveToXsltAttribute(0, "href"))
			{
				Uri uri = this.ResolveUri(this.input.Value, this.input.BaseUri);
				if (this.documentUriInUse.Contains(uri.ToString()))
				{
					this.ReportError("Stylesheet '{0}' cannot directly or indirectly include or import itself.", new string[]
					{
						this.input.Value
					});
				}
				else
				{
					this.LoadStylesheet(uri, true);
				}
			}
			this.CheckNoContent();
		}

		// Token: 0x060029B9 RID: 10681 RVA: 0x000F9AD8 File Offset: 0x000F7CD8
		private void LoadStripSpace(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.loadStripSpaceAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			if (this.input.MoveToXsltAttribute(0, this.atoms.Elements))
			{
				this.ParseWhitespaceRules(this.input.Value, false);
			}
			this.CheckNoContent();
		}

		// Token: 0x060029BA RID: 10682 RVA: 0x000F9B38 File Offset: 0x000F7D38
		private void LoadPreserveSpace(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.loadStripSpaceAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			if (this.input.MoveToXsltAttribute(0, this.atoms.Elements))
			{
				this.ParseWhitespaceRules(this.input.Value, true);
			}
			this.CheckNoContent();
		}

		// Token: 0x060029BB RID: 10683 RVA: 0x000F9B98 File Offset: 0x000F7D98
		private void LoadOutput()
		{
			this.input.GetAttributes(this.outputAttributes);
			Output output = this.compiler.Output;
			XmlWriterSettings settings = output.Settings;
			int currentPrecedence = this.compiler.CurrentPrecedence;
			if (this.ParseQNameAttribute(0) != null)
			{
				this.ReportNYI("xsl:output/@name");
			}
			if (this.input.MoveToXsltAttribute(1, "method") && output.MethodPrec <= currentPrecedence)
			{
				this.compiler.EnterForwardsCompatible();
				XmlOutputMethod outputMethod;
				XmlQualifiedName xmlQualifiedName = this.ParseOutputMethod(this.input.Value, out outputMethod);
				if (this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility) && xmlQualifiedName != null)
				{
					if (currentPrecedence == output.MethodPrec && !output.Method.Equals(xmlQualifiedName))
					{
						this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
						{
							"method"
						});
					}
					settings.OutputMethod = outputMethod;
					output.Method = xmlQualifiedName;
					output.MethodPrec = currentPrecedence;
				}
			}
			if (this.ParseYesNoAttribute(2, "byte-order-mark") != TriState.Unknown)
			{
				this.ReportNYI("xsl:output/@byte-order-mark");
			}
			if (this.input.MoveToXsltAttribute(3, "cdata-section-elements"))
			{
				this.compiler.EnterForwardsCompatible();
				string[] array = XmlConvert.SplitString(this.input.Value);
				List<XmlQualifiedName> list = new List<XmlQualifiedName>();
				for (int i = 0; i < array.Length; i++)
				{
					list.Add(this.ResolveQName(false, array[i]));
				}
				if (this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
				{
					settings.CDataSectionElements.AddRange(list);
				}
			}
			if (this.input.MoveToXsltAttribute(4, "doctype-public") && output.DocTypePublicPrec <= currentPrecedence)
			{
				if (currentPrecedence == output.DocTypePublicPrec && settings.DocTypePublic != this.input.Value)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"doctype-public"
					});
				}
				settings.DocTypePublic = this.input.Value;
				output.DocTypePublicPrec = currentPrecedence;
			}
			if (this.input.MoveToXsltAttribute(5, "doctype-system") && output.DocTypeSystemPrec <= currentPrecedence)
			{
				if (currentPrecedence == output.DocTypeSystemPrec && settings.DocTypeSystem != this.input.Value)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"doctype-system"
					});
				}
				settings.DocTypeSystem = this.input.Value;
				output.DocTypeSystemPrec = currentPrecedence;
			}
			if (this.input.MoveToXsltAttribute(6, "encoding") && output.EncodingPrec <= currentPrecedence)
			{
				try
				{
					Encoding encoding = Encoding.GetEncoding(this.input.Value);
					if (currentPrecedence == output.EncodingPrec && output.Encoding != this.input.Value)
					{
						this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
						{
							"encoding"
						});
					}
					settings.Encoding = encoding;
					output.Encoding = this.input.Value;
					output.EncodingPrec = currentPrecedence;
				}
				catch (ArgumentException)
				{
					if (!this.input.ForwardCompatibility)
					{
						this.ReportWarning("'{0}' is not a supported encoding name.", new string[]
						{
							this.input.Value
						});
					}
				}
			}
			if (this.ParseYesNoAttribute(7, "escape-uri-attributes") <= TriState.False)
			{
				this.ReportNYI("xsl:output/@escape-uri-attributes == flase()");
			}
			if (this.ParseYesNoAttribute(8, "include-content-type") <= TriState.False)
			{
				this.ReportNYI("xsl:output/@include-content-type == flase()");
			}
			TriState triState = this.ParseYesNoAttribute(9, "indent");
			if (triState != TriState.Unknown && output.IndentPrec <= currentPrecedence)
			{
				bool flag = triState == TriState.True;
				if (currentPrecedence == output.IndentPrec && settings.Indent != flag)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"indent"
					});
				}
				settings.Indent = flag;
				output.IndentPrec = currentPrecedence;
			}
			if (this.input.MoveToXsltAttribute(10, "media-type") && output.MediaTypePrec <= currentPrecedence)
			{
				if (currentPrecedence == output.MediaTypePrec && settings.MediaType != this.input.Value)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"media-type"
					});
				}
				settings.MediaType = this.input.Value;
				output.MediaTypePrec = currentPrecedence;
			}
			if (this.input.MoveToXsltAttribute(11, "normalization-form"))
			{
				this.ReportNYI("xsl:output/@normalization-form");
			}
			triState = this.ParseYesNoAttribute(12, "omit-xml-declaration");
			if (triState != TriState.Unknown && output.OmitXmlDeclarationPrec <= currentPrecedence)
			{
				bool flag2 = triState == TriState.True;
				if (currentPrecedence == output.OmitXmlDeclarationPrec && settings.OmitXmlDeclaration != flag2)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"omit-xml-declaration"
					});
				}
				settings.OmitXmlDeclaration = flag2;
				output.OmitXmlDeclarationPrec = currentPrecedence;
			}
			triState = this.ParseYesNoAttribute(13, "standalone");
			if (triState != TriState.Unknown && output.StandalonePrec <= currentPrecedence)
			{
				XmlStandalone xmlStandalone = (triState == TriState.True) ? XmlStandalone.Yes : XmlStandalone.No;
				if (currentPrecedence == output.StandalonePrec && settings.Standalone != xmlStandalone)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"standalone"
					});
				}
				settings.Standalone = xmlStandalone;
				output.StandalonePrec = currentPrecedence;
			}
			if (this.ParseYesNoAttribute(14, "undeclare-prefixes") == TriState.True)
			{
				this.ReportNYI("xsl:output/@undeclare-prefixes == true()");
			}
			if (this.ParseUseCharacterMaps(15).Count != 0)
			{
				this.ReportNYI("xsl:output/@use-character-maps");
			}
			if (this.input.MoveToXsltAttribute(16, "version") && output.VersionPrec <= currentPrecedence)
			{
				if (currentPrecedence == output.VersionPrec && output.Version != this.input.Value)
				{
					this.ReportWarning("Attribute '{0}' of 'xsl:output' cannot be defined more than once with the same import precedence.", new string[]
					{
						"version"
					});
				}
				output.Version = this.input.Value;
				output.VersionPrec = currentPrecedence;
			}
			this.CheckNoContent();
		}

		// Token: 0x060029BC RID: 10684 RVA: 0x000FA170 File Offset: 0x000F8370
		private void ProcessOutputSettings()
		{
			Output output = this.compiler.Output;
			XmlWriterSettings settings = output.Settings;
			if (settings.OutputMethod == XmlOutputMethod.Html && output.IndentPrec == -2147483648)
			{
				settings.Indent = true;
			}
			if (output.MediaTypePrec == -2147483648)
			{
				settings.MediaType = ((settings.OutputMethod == XmlOutputMethod.Xml) ? "text/xml" : ((settings.OutputMethod == XmlOutputMethod.Html) ? "text/html" : ((settings.OutputMethod == XmlOutputMethod.Text) ? "text/plain" : null)));
			}
		}

		// Token: 0x060029BD RID: 10685 RVA: 0x000FA1F0 File Offset: 0x000F83F0
		private void CheckUseAttrubuteSetInList(IList<XslNode> list)
		{
			foreach (XslNode xslNode in list)
			{
				XslNodeType nodeType = xslNode.NodeType;
				if (nodeType != XslNodeType.List)
				{
					AttributeSet attSet;
					if (nodeType == XslNodeType.UseAttributeSet && this.compiler.AttributeSets.TryGetValue(xslNode.Name, out attSet))
					{
						this.CheckAttributeSetsDfs(attSet);
					}
				}
				else
				{
					this.CheckUseAttrubuteSetInList(xslNode.Content);
				}
			}
		}

		// Token: 0x060029BE RID: 10686 RVA: 0x000FA274 File Offset: 0x000F8474
		private void CheckAttributeSetsDfs(AttributeSet attSet)
		{
			CycleCheck cycleCheck = attSet.CycleCheck;
			if (cycleCheck != CycleCheck.NotStarted)
			{
				if (cycleCheck != CycleCheck.Completed)
				{
					this.compiler.ReportError(attSet.Content[0].SourceLine, "Circular reference in the definition of attribute set '{0}'.", new string[]
					{
						attSet.Name.QualifiedName
					});
				}
				return;
			}
			attSet.CycleCheck = CycleCheck.Processing;
			this.CheckUseAttrubuteSetInList(attSet.Content);
			attSet.CycleCheck = CycleCheck.Completed;
		}

		// Token: 0x060029BF RID: 10687 RVA: 0x000FA2E4 File Offset: 0x000F84E4
		private void LoadKey(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.keyAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			QilName qilName = this.ParseQNameAttribute(0);
			string match = this.ParseStringAttribute(1, "match");
			string text = this.ParseStringAttribute(2, "use");
			this.ParseCollationAttribute(3);
			this.input.MoveToElement();
			if (this.V1)
			{
				if (text == null)
				{
					this.input.SkipNode();
				}
				else
				{
					this.CheckNoContent();
				}
			}
			else
			{
				List<XslNode> list = this.LoadInstructions();
				if (list.Count != 0)
				{
					list = this.LoadEndTag(list);
				}
				if (text == null == (list.Count == 0))
				{
					this.ReportError("'xsl:key' has a 'use' attribute and has non-empty content, or it has empty content and no 'use' attribute.", Array.Empty<string>());
				}
				else if (text == null)
				{
					this.ReportNYI("xsl:key[count(@use) = 0]");
				}
			}
			Key item = (Key)XsltLoader.SetInfo(AstFactory.Key(qilName, match, text, this.input.XslVersion), null, attributes);
			if (this.compiler.Keys.Contains(qilName))
			{
				this.compiler.Keys[qilName].Add(item);
				return;
			}
			List<Key> list2 = new List<Key>();
			list2.Add(item);
			this.compiler.Keys.Add(list2);
		}

		// Token: 0x060029C0 RID: 10688 RVA: 0x000FA428 File Offset: 0x000F8628
		private void LoadDecimalFormat(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.decimalFormatAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			XmlQualifiedName xmlQualifiedName;
			if (this.input.MoveToXsltAttribute(0, "name"))
			{
				this.compiler.EnterForwardsCompatible();
				xmlQualifiedName = this.ResolveQName(true, this.input.Value);
				if (!this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
				{
					xmlQualifiedName = new XmlQualifiedName();
				}
			}
			else
			{
				xmlQualifiedName = new XmlQualifiedName();
			}
			string text = DecimalFormatDecl.Default.InfinitySymbol;
			if (this.input.MoveToXsltAttribute(1, "infinity"))
			{
				text = this.input.Value;
			}
			string text2 = DecimalFormatDecl.Default.NanSymbol;
			if (this.input.MoveToXsltAttribute(2, "NaN"))
			{
				text2 = this.input.Value;
			}
			char[] characters = DecimalFormatDecl.Default.Characters;
			char[] array = new char[8];
			for (int i = 0; i < 8; i++)
			{
				array[i] = this.ParseCharAttribute(3 + i, this.decimalFormatAttributes[3 + i].name, characters[i]);
			}
			for (int j = 0; j < 7; j++)
			{
				for (int k = j + 1; k < 7; k++)
				{
					if (array[j] == array[k])
					{
						if (!this.input.MoveToXsltAttribute(3 + k, this.decimalFormatAttributes[3 + k].name))
						{
							this.input.MoveToXsltAttribute(3 + j, this.decimalFormatAttributes[3 + j].name);
						}
						this.ReportError("The '{0}' and '{1}' attributes of 'xsl:decimal-format' must have distinct values.", new string[]
						{
							this.decimalFormatAttributes[3 + j].name,
							this.decimalFormatAttributes[3 + k].name
						});
						break;
					}
				}
			}
			if (this.compiler.DecimalFormats.Contains(xmlQualifiedName))
			{
				DecimalFormatDecl decimalFormatDecl = this.compiler.DecimalFormats[xmlQualifiedName];
				this.input.MoveToXsltAttribute(1, "infinity");
				this.CheckError(text != decimalFormatDecl.InfinitySymbol, "The '{0}' attribute of 'xsl:decimal-format' cannot be redefined with a value of '{1}'.", new string[]
				{
					"infinity",
					text
				});
				this.input.MoveToXsltAttribute(2, "NaN");
				this.CheckError(text2 != decimalFormatDecl.NanSymbol, "The '{0}' attribute of 'xsl:decimal-format' cannot be redefined with a value of '{1}'.", new string[]
				{
					"NaN",
					text2
				});
				for (int l = 0; l < 8; l++)
				{
					this.input.MoveToXsltAttribute(3 + l, this.decimalFormatAttributes[3 + l].name);
					this.CheckError(array[l] != decimalFormatDecl.Characters[l], "The '{0}' attribute of 'xsl:decimal-format' cannot be redefined with a value of '{1}'.", new string[]
					{
						this.decimalFormatAttributes[3 + l].name,
						char.ToString(array[l])
					});
				}
			}
			else
			{
				DecimalFormatDecl item = new DecimalFormatDecl(xmlQualifiedName, text, text2, new string(array));
				this.compiler.DecimalFormats.Add(item);
			}
			this.CheckNoContent();
		}

		// Token: 0x060029C1 RID: 10689 RVA: 0x000FA760 File Offset: 0x000F8960
		private void LoadNamespaceAlias(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.namespaceAliasAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			string text = null;
			string text2 = null;
			string text3 = null;
			if (this.input.MoveToXsltAttribute(0, "stylesheet-prefix"))
			{
				if (this.input.Value.Length == 0)
				{
					this.ReportError("The value of the '{0}' attribute cannot be empty. Use '#default' to specify the default namespace.", new string[]
					{
						"stylesheet-prefix"
					});
				}
				else
				{
					text = this.input.LookupXmlNamespace((this.input.Value == "#default") ? string.Empty : this.input.Value);
				}
			}
			if (this.input.MoveToXsltAttribute(1, "result-prefix"))
			{
				if (this.input.Value.Length == 0)
				{
					this.ReportError("The value of the '{0}' attribute cannot be empty. Use '#default' to specify the default namespace.", new string[]
					{
						"result-prefix"
					});
				}
				else
				{
					text2 = ((this.input.Value == "#default") ? string.Empty : this.input.Value);
					text3 = this.input.LookupXmlNamespace(text2);
				}
			}
			this.CheckNoContent();
			if (text == null || text3 == null)
			{
				return;
			}
			if (this.compiler.SetNsAlias(text, text3, text2, this.curStylesheet.ImportPrecedence))
			{
				this.input.MoveToElement();
				this.ReportWarning("Namespace URI '{0}' is declared to be an alias for multiple different namespace URIs with the same import precedence.", new string[]
				{
					text
				});
			}
		}

		// Token: 0x060029C2 RID: 10690 RVA: 0x000FA8CC File Offset: 0x000F8ACC
		private void LoadAttributeSet(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.attributeSetAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			QilName qilName = this.ParseQNameAttribute(0);
			AttributeSet attributeSet;
			if (!this.curStylesheet.AttributeSets.TryGetValue(qilName, out attributeSet))
			{
				attributeSet = AstFactory.AttributeSet(qilName);
				this.curStylesheet.AttributeSets[qilName] = attributeSet;
				if (!this.compiler.AttributeSets.ContainsKey(qilName))
				{
					this.compiler.AllTemplates.Add(attributeSet);
				}
			}
			List<XslNode> list = new List<XslNode>();
			if (this.input.MoveToXsltAttribute(1, "use-attribute-sets"))
			{
				this.AddUseAttributeSets(list);
			}
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				do
				{
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType - XmlNodeType.Whitespace > 1)
						{
							this.ReportError("'{0}' element cannot have text node children.", new string[]
							{
								elementName
							});
						}
					}
					else if (this.input.IsXsltKeyword(this.atoms.Attribute))
					{
						XsltLoader.AddInstruction(list, this.XslAttribute());
					}
					else
					{
						this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
						{
							this.input.QualifiedName,
							elementName
						});
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			attributeSet.AddContent(XsltLoader.SetInfo(AstFactory.List(), this.LoadEndTag(list), attributes));
		}

		// Token: 0x060029C3 RID: 10691 RVA: 0x000FAA50 File Offset: 0x000F8C50
		private void LoadGlobalVariableOrParameter(NsDecl stylesheetNsList, XslNodeType nodeType)
		{
			VarPar varPar = this.XslVarPar();
			varPar.Namespaces = XsltLoader.MergeNamespaces(varPar.Namespaces, stylesheetNsList);
			this.CheckError(!this.curStylesheet.AddVarPar(varPar), "The variable or parameter '{0}' was duplicated with the same import precedence.", new string[]
			{
				varPar.Name.QualifiedName
			});
		}

		// Token: 0x060029C4 RID: 10692 RVA: 0x000FAAA4 File Offset: 0x000F8CA4
		private void LoadTemplate(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.templateAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			string text = this.ParseStringAttribute(0, "match");
			QilName name = this.ParseQNameAttribute(1);
			double num = double.NaN;
			if (this.input.MoveToXsltAttribute(2, "priority"))
			{
				num = XPathConvert.StringToDouble(this.input.Value);
				if (double.IsNaN(num) && !this.input.ForwardCompatibility)
				{
					this.ReportError("'{1}' is an invalid value for the '{0}' attribute.", new string[]
					{
						"priority",
						this.input.Value
					});
				}
			}
			QilName mode = this.V1 ? this.ParseModeAttribute(3) : this.ParseModeListAttribute(3);
			if (text == null)
			{
				this.CheckError(!this.input.AttributeExists(1, "name"), "'xsl:template' must have either a 'match' attribute or a 'name' attribute, or both.", Array.Empty<string>());
				this.CheckError(this.input.AttributeExists(3, "mode"), "An 'xsl:template' element without a 'match' attribute cannot have a 'mode' attribute.", Array.Empty<string>());
				mode = XsltLoader.nullMode;
				if (this.input.AttributeExists(2, "priority"))
				{
					if (this.V1)
					{
						this.ReportWarning("An 'xsl:template' element without a 'match' attribute cannot have a 'priority' attribute.", Array.Empty<string>());
					}
					else
					{
						this.ReportError("An 'xsl:template' element without a 'match' attribute cannot have a 'priority' attribute.", Array.Empty<string>());
					}
				}
			}
			if (this.input.MoveToXsltAttribute(4, "as"))
			{
				this.ReportNYI("xsl:template/@as");
			}
			this.curTemplate = AstFactory.Template(name, text, mode, num, this.input.XslVersion);
			this.input.CanHaveApplyImports = (text != null);
			XsltLoader.SetInfo(this.curTemplate, this.LoadEndTag(this.LoadInstructions(XsltLoader.InstructionFlags.AllowParam)), attributes);
			if (!this.curStylesheet.AddTemplate(this.curTemplate))
			{
				this.ReportError("'{0}' is a duplicate template name.", new string[]
				{
					this.curTemplate.Name.QualifiedName
				});
			}
			this.curTemplate = null;
		}

		// Token: 0x060029C5 RID: 10693 RVA: 0x000FACA0 File Offset: 0x000F8EA0
		private void LoadMsScript(NsDecl stylesheetNsList)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.scriptAttributes);
			attributes.nsList = XsltLoader.MergeNamespaces(attributes.nsList, stylesheetNsList);
			string text = null;
			if (this.input.MoveToXsltAttribute(0, "implements-prefix"))
			{
				if (this.input.Value.Length == 0)
				{
					this.ReportError("The value of the '{0}' attribute cannot be empty.", new string[]
					{
						"implements-prefix",
						this.input.Value
					});
				}
				else
				{
					text = this.input.LookupXmlNamespace(this.input.Value);
					if (text == "http://www.w3.org/1999/XSL/Transform")
					{
						this.ReportError("Script block cannot implement the XSLT namespace.", Array.Empty<string>());
						text = null;
					}
				}
			}
			if (text == null)
			{
				text = this.compiler.CreatePhantomNamespace();
			}
			string text2 = this.ParseStringAttribute(1, "language");
			if (text2 == null)
			{
				text2 = "jscript";
			}
			if (!this.compiler.Settings.EnableScript)
			{
				this.compiler.Scripts.ScriptClasses[text] = null;
				this.input.SkipNode();
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string uri = this.input.Uri;
			int lineNumber = 0;
			int num = 0;
			ScriptClass scriptClass = this.compiler.Scripts.GetScriptClass(text, text2, this);
			if (scriptClass == null)
			{
				this.input.SkipNode();
				return;
			}
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				do
				{
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType == XmlNodeType.Text || stringBuilder.Length != 0)
						{
							int line = this.input.Start.Line;
							int line2 = this.input.End.Line;
							if (stringBuilder.Length == 0)
							{
								lineNumber = line;
							}
							else if (num < line)
							{
								stringBuilder.Append('\n', line - num);
							}
							stringBuilder.Append(this.input.Value);
							num = line2;
						}
					}
					else if (this.input.IsNs(this.atoms.UrnMsxsl) && (this.input.IsKeyword(this.atoms.Assembly) || this.input.IsKeyword(this.atoms.Using)))
					{
						if (stringBuilder.Length != 0)
						{
							this.ReportError("Element '{0}' must precede script code.", new string[]
							{
								this.input.QualifiedName
							});
							this.input.SkipNode();
						}
						else if (this.input.IsKeyword(this.atoms.Assembly))
						{
							this.LoadMsAssembly(scriptClass);
						}
						else if (this.input.IsKeyword(this.atoms.Using))
						{
							this.LoadMsUsing(scriptClass);
						}
					}
					else
					{
						this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
						{
							this.input.QualifiedName,
							elementName
						});
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			if (stringBuilder.Length == 0)
			{
				lineNumber = this.input.Start.Line;
			}
			scriptClass.AddScriptBlock(stringBuilder.ToString(), uri, lineNumber, this.input.Start);
		}

		// Token: 0x060029C6 RID: 10694 RVA: 0x000FAFD8 File Offset: 0x000F91D8
		private void LoadMsAssembly(ScriptClass scriptClass)
		{
			this.input.GetAttributes(this.assemblyAttributes);
			string text = this.ParseStringAttribute(0, "name");
			string text2 = this.ParseStringAttribute(1, "href");
			if (text != null == (text2 != null))
			{
				this.ReportError("'msxsl:assembly' must have either a 'name' attribute or an 'href' attribute, but not both.", Array.Empty<string>());
			}
			else
			{
				string text3 = null;
				if (text != null)
				{
					try
					{
						text3 = Assembly.Load(text).Location;
						goto IL_C5;
					}
					catch
					{
						AssemblyName assemblyName = new AssemblyName(text);
						byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
						if ((publicKeyToken == null || publicKeyToken.Length == 0) && assemblyName.Version == null)
						{
							text3 = assemblyName.Name + ".dll";
							goto IL_C5;
						}
						throw;
					}
				}
				text3 = Assembly.LoadFrom(this.ResolveUri(text2, this.input.BaseUri).ToString()).Location;
				scriptClass.refAssembliesByHref = true;
				IL_C5:
				if (text3 != null)
				{
					scriptClass.refAssemblies.Add(text3);
				}
			}
			this.CheckNoContent();
		}

		// Token: 0x060029C7 RID: 10695 RVA: 0x000FB0D0 File Offset: 0x000F92D0
		private void LoadMsUsing(ScriptClass scriptClass)
		{
			this.input.GetAttributes(this.usingAttributes);
			if (this.input.MoveToXsltAttribute(0, "namespace"))
			{
				scriptClass.nsImports.Add(this.input.Value);
			}
			this.CheckNoContent();
		}

		// Token: 0x060029C8 RID: 10696 RVA: 0x000FB11F File Offset: 0x000F931F
		private List<XslNode> LoadInstructions()
		{
			return this.LoadInstructions(new List<XslNode>(), XsltLoader.InstructionFlags.None);
		}

		// Token: 0x060029C9 RID: 10697 RVA: 0x000FB12D File Offset: 0x000F932D
		private List<XslNode> LoadInstructions(XsltLoader.InstructionFlags flags)
		{
			return this.LoadInstructions(new List<XslNode>(), flags);
		}

		// Token: 0x060029CA RID: 10698 RVA: 0x000FB13B File Offset: 0x000F933B
		private List<XslNode> LoadInstructions(List<XslNode> content)
		{
			return this.LoadInstructions(content, XsltLoader.InstructionFlags.None);
		}

		// Token: 0x060029CB RID: 10699 RVA: 0x000FB148 File Offset: 0x000F9348
		private List<XslNode> LoadInstructions(List<XslNode> content, XsltLoader.InstructionFlags flags)
		{
			int num = this.loadInstructionsDepth + 1;
			this.loadInstructionsDepth = num;
			if (num > 1024 && XsltConfigSection.LimitXPathComplexity)
			{
				throw XsltException.Create("The stylesheet is too complex.", Array.Empty<string>());
			}
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				bool flag = true;
				int num2 = 0;
				for (;;)
				{
					XmlNodeType nodeType = this.input.NodeType;
					XslNode instruction;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType != XmlNodeType.Whitespace)
						{
							if (nodeType != XmlNodeType.SignificantWhitespace)
							{
								flag = false;
							}
							instruction = XsltLoader.SetLineInfo(AstFactory.Text(this.input.Value), this.input.BuildLineInfo());
							goto IL_3E7;
						}
					}
					else
					{
						string namespaceUri = this.input.NamespaceUri;
						string localName = this.input.LocalName;
						if (namespaceUri == this.atoms.UriXsl)
						{
							XsltLoader.InstructionFlags instructionFlags = Ref.Equal(localName, this.atoms.Param) ? XsltLoader.InstructionFlags.AllowParam : (Ref.Equal(localName, this.atoms.Sort) ? XsltLoader.InstructionFlags.AllowSort : XsltLoader.InstructionFlags.None);
							if (instructionFlags != XsltLoader.InstructionFlags.None)
							{
								string text = ((flags & instructionFlags) == XsltLoader.InstructionFlags.None) ? "'{0}' cannot be a child of the '{1}' element." : ((!flag) ? "'{0}' element children must precede all other children of the '{1}' element." : null);
								if (text != null)
								{
									this.ReportError(text, new string[]
									{
										this.input.QualifiedName,
										elementName
									});
									flag = false;
									this.input.SkipNode();
									goto IL_3EF;
								}
							}
							else
							{
								flag = false;
							}
							instruction = (Ref.Equal(localName, this.atoms.ApplyImports) ? this.XslApplyImports() : (Ref.Equal(localName, this.atoms.ApplyTemplates) ? this.XslApplyTemplates() : (Ref.Equal(localName, this.atoms.CallTemplate) ? this.XslCallTemplate() : (Ref.Equal(localName, this.atoms.Copy) ? this.XslCopy() : (Ref.Equal(localName, this.atoms.CopyOf) ? this.XslCopyOf() : (Ref.Equal(localName, this.atoms.Fallback) ? this.XslFallback() : (Ref.Equal(localName, this.atoms.If) ? this.XslIf() : (Ref.Equal(localName, this.atoms.Choose) ? this.XslChoose() : (Ref.Equal(localName, this.atoms.ForEach) ? this.XslForEach() : (Ref.Equal(localName, this.atoms.Message) ? this.XslMessage() : (Ref.Equal(localName, this.atoms.Number) ? this.XslNumber() : (Ref.Equal(localName, this.atoms.ValueOf) ? this.XslValueOf() : (Ref.Equal(localName, this.atoms.Comment) ? this.XslComment() : (Ref.Equal(localName, this.atoms.ProcessingInstruction) ? this.XslProcessingInstruction() : (Ref.Equal(localName, this.atoms.Text) ? this.XslText() : (Ref.Equal(localName, this.atoms.Element) ? this.XslElement() : (Ref.Equal(localName, this.atoms.Attribute) ? this.XslAttribute() : (Ref.Equal(localName, this.atoms.Variable) ? this.XslVarPar() : (Ref.Equal(localName, this.atoms.Param) ? this.XslVarPar() : (Ref.Equal(localName, this.atoms.Sort) ? this.XslSort(num2++) : this.LoadUnknownXsltInstruction(elementName)))))))))))))))))))));
							goto IL_3E7;
						}
						flag = false;
						instruction = this.LoadLiteralResultElement(false);
						goto IL_3E7;
					}
					IL_3EF:
					if (!this.input.MoveToNextSibling())
					{
						break;
					}
					continue;
					IL_3E7:
					XsltLoader.AddInstruction(content, instruction);
					goto IL_3EF;
				}
			}
			this.loadInstructionsDepth--;
			return content;
		}

		// Token: 0x060029CC RID: 10700 RVA: 0x000FB564 File Offset: 0x000F9764
		private List<XslNode> LoadWithParams(XsltLoader.InstructionFlags flags)
		{
			XsltInput.DelayedQName elementName = this.input.ElementName;
			List<XslNode> list = new List<XslNode>();
			if (this.input.MoveToFirstChild())
			{
				int num = 0;
				do
				{
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType - XmlNodeType.Whitespace > 1)
						{
							this.ReportError("'{0}' element cannot have text node children.", new string[]
							{
								elementName
							});
						}
					}
					else if (this.input.IsXsltKeyword(this.atoms.WithParam))
					{
						XslNode xslNode = this.XslVarPar();
						this.CheckWithParam(list, xslNode);
						XsltLoader.AddInstruction(list, xslNode);
					}
					else if (flags == XsltLoader.InstructionFlags.AllowSort && this.input.IsXsltKeyword(this.atoms.Sort))
					{
						XsltLoader.AddInstruction(list, this.XslSort(num++));
					}
					else if (flags == XsltLoader.InstructionFlags.AllowFallback && this.input.IsXsltKeyword(this.atoms.Fallback))
					{
						this.XslFallback();
					}
					else
					{
						this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
						{
							this.input.QualifiedName,
							elementName
						});
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			return list;
		}

		// Token: 0x060029CD RID: 10701 RVA: 0x000FB6A0 File Offset: 0x000F98A0
		private XslNode XslApplyImports()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes();
			if (!this.input.CanHaveApplyImports)
			{
				this.ReportError("An 'xsl:apply-imports' element can only occur within an 'xsl:template' element with a 'match' attribute, and cannot occur within an 'xsl:for-each' element.", Array.Empty<string>());
				this.input.SkipNode();
				return null;
			}
			List<XslNode> list = this.LoadWithParams(XsltLoader.InstructionFlags.None);
			attributes.SaveExtendedLineInfo(this.input);
			if (this.V1)
			{
				if (list.Count != 0)
				{
					ISourceLineInfo sourceLine = list[0].SourceLine;
					if (this.input.ForwardCompatibility)
					{
						return XsltLoader.SetInfo(AstFactory.Error(XslLoadException.CreateMessage(sourceLine, "The contents of '{0}' must be empty.", new string[]
						{
							this.atoms.ApplyImports
						})), null, attributes);
					}
					this.compiler.ReportError(sourceLine, "The contents of '{0}' must be empty.", new string[]
					{
						this.atoms.ApplyImports
					});
				}
				list = null;
			}
			else
			{
				if (list.Count != 0)
				{
					this.ReportNYI("xsl:apply-imports/xsl:with-param");
				}
				list = null;
			}
			return XsltLoader.SetInfo(AstFactory.ApplyImports(this.curTemplate.Mode, this.curStylesheet, this.input.XslVersion), list, attributes);
		}

		// Token: 0x060029CE RID: 10702 RVA: 0x000FB7B8 File Offset: 0x000F99B8
		private XslNode XslApplyTemplates()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.applyTemplatesAttributes);
			string text = this.ParseStringAttribute(0, "select");
			if (text == null)
			{
				text = "node()";
			}
			QilName mode = this.ParseModeAttribute(1);
			List<XslNode> content = this.LoadWithParams(XsltLoader.InstructionFlags.AllowSort);
			attributes.SaveExtendedLineInfo(this.input);
			return XsltLoader.SetInfo(AstFactory.ApplyTemplates(mode, text, attributes, this.input.XslVersion), content, attributes);
		}

		// Token: 0x060029CF RID: 10703 RVA: 0x000FB824 File Offset: 0x000F9A24
		private XslNode XslCallTemplate()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.callTemplateAttributes);
			QilName name = this.ParseQNameAttribute(0);
			List<XslNode> content = this.LoadWithParams(XsltLoader.InstructionFlags.None);
			attributes.SaveExtendedLineInfo(this.input);
			return XsltLoader.SetInfo(AstFactory.CallTemplate(name, attributes), content, attributes);
		}

		// Token: 0x060029D0 RID: 10704 RVA: 0x000FB86C File Offset: 0x000F9A6C
		private XslNode XslCopy()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.copyAttributes);
			bool flag = this.ParseYesNoAttribute(0, "copy-namespaces") > TriState.False;
			bool flag2 = this.ParseYesNoAttribute(1, "inherit-namespaces") > TriState.False;
			if (!flag)
			{
				this.ReportNYI("xsl:copy[@copy-namespaces    = 'no']");
			}
			if (!flag2)
			{
				this.ReportNYI("xsl:copy[@inherit-namespaces = 'no']");
			}
			List<XslNode> list = new List<XslNode>();
			if (this.input.MoveToXsltAttribute(2, "use-attribute-sets"))
			{
				this.AddUseAttributeSets(list);
			}
			this.ParseTypeAttribute(3);
			this.ParseValidationAttribute(4, false);
			return XsltLoader.SetInfo(AstFactory.Copy(), this.LoadEndTag(this.LoadInstructions(list)), attributes);
		}

		// Token: 0x060029D1 RID: 10705 RVA: 0x000FB90C File Offset: 0x000F9B0C
		private XslNode XslCopyOf()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.copyOfAttributes);
			string select = this.ParseStringAttribute(0, "select");
			if (this.ParseYesNoAttribute(1, "copy-namespaces") <= TriState.False)
			{
				this.ReportNYI("xsl:copy-of[@copy-namespaces    = 'no']");
			}
			this.ParseTypeAttribute(2);
			this.ParseValidationAttribute(3, false);
			this.CheckNoContent();
			return XsltLoader.SetInfo(AstFactory.CopyOf(select, this.input.XslVersion), null, attributes);
		}

		// Token: 0x060029D2 RID: 10706 RVA: 0x000FB97F File Offset: 0x000F9B7F
		private XslNode XslFallback()
		{
			this.input.GetAttributes();
			this.input.SkipNode();
			return null;
		}

		// Token: 0x060029D3 RID: 10707 RVA: 0x000FB99C File Offset: 0x000F9B9C
		private XslNode XslIf()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.ifAttributes);
			return XsltLoader.SetInfo(AstFactory.If(this.ParseStringAttribute(0, "test"), this.input.XslVersion), this.LoadInstructions(), attributes);
		}

		// Token: 0x060029D4 RID: 10708 RVA: 0x000FB9E4 File Offset: 0x000F9BE4
		private XslNode XslChoose()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes();
			List<XslNode> content = new List<XslNode>();
			bool flag = false;
			bool flag2 = false;
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				do
				{
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType != XmlNodeType.Element)
					{
						if (nodeType - XmlNodeType.Whitespace > 1)
						{
							this.ReportError("'{0}' element cannot have text node children.", new string[]
							{
								elementName
							});
						}
					}
					else
					{
						XslNode xslNode = null;
						if (Ref.Equal(this.input.NamespaceUri, this.atoms.UriXsl))
						{
							if (Ref.Equal(this.input.LocalName, this.atoms.When))
							{
								if (flag)
								{
									this.ReportError("'xsl:when' must precede the 'xsl:otherwise' element.", Array.Empty<string>());
									this.input.SkipNode();
									goto IL_16A;
								}
								flag2 = true;
								xslNode = this.XslIf();
							}
							else if (Ref.Equal(this.input.LocalName, this.atoms.Otherwise))
							{
								if (flag)
								{
									this.ReportError("An 'xsl:choose' element can have only one 'xsl:otherwise' child.", Array.Empty<string>());
									this.input.SkipNode();
									goto IL_16A;
								}
								flag = true;
								xslNode = this.XslOtherwise();
							}
						}
						if (xslNode == null)
						{
							this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
							{
								this.input.QualifiedName,
								elementName
							});
							this.input.SkipNode();
						}
						else
						{
							XsltLoader.AddInstruction(content, xslNode);
						}
					}
					IL_16A:;
				}
				while (this.input.MoveToNextSibling());
			}
			this.CheckError(!flag2, "An 'xsl:choose' element must have at least one 'xsl:when' child.", Array.Empty<string>());
			return XsltLoader.SetInfo(AstFactory.Choose(), content, attributes);
		}

		// Token: 0x060029D5 RID: 10709 RVA: 0x000FBB8C File Offset: 0x000F9D8C
		private XslNode XslOtherwise()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes();
			return XsltLoader.SetInfo(AstFactory.Otherwise(), this.LoadInstructions(), attributes);
		}

		// Token: 0x060029D6 RID: 10710 RVA: 0x000FBBB8 File Offset: 0x000F9DB8
		private XslNode XslForEach()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.forEachAttributes);
			string select = this.ParseStringAttribute(0, "select");
			this.input.CanHaveApplyImports = false;
			List<XslNode> content = this.LoadInstructions(XsltLoader.InstructionFlags.AllowSort);
			attributes.SaveExtendedLineInfo(this.input);
			return XsltLoader.SetInfo(AstFactory.ForEach(select, attributes, this.input.XslVersion), content, attributes);
		}

		// Token: 0x060029D7 RID: 10711 RVA: 0x000FBC1C File Offset: 0x000F9E1C
		private XslNode XslMessage()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.messageAttributes);
			string text = this.ParseStringAttribute(0, "select");
			bool term = this.ParseYesNoAttribute(1, "terminate") == TriState.True;
			List<XslNode> list = this.LoadInstructions();
			if (list.Count != 0)
			{
				list = this.LoadEndTag(list);
			}
			if (text != null)
			{
				list.Insert(0, AstFactory.CopyOf(text, this.input.XslVersion));
			}
			return XsltLoader.SetInfo(AstFactory.Message(term), list, attributes);
		}

		// Token: 0x060029D8 RID: 10712 RVA: 0x000FBC98 File Offset: 0x000F9E98
		private XslNode XslNumber()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.numberAttributes);
			string value = this.ParseStringAttribute(0, "value");
			if (this.ParseStringAttribute(1, "select") != null)
			{
				this.ReportNYI("xsl:number/@select");
			}
			NumberLevel level = NumberLevel.Single;
			if (this.input.MoveToXsltAttribute(2, "level"))
			{
				string value2 = this.input.Value;
				if (!(value2 == "single"))
				{
					if (!(value2 == "multiple"))
					{
						if (!(value2 == "any"))
						{
							if (!this.input.ForwardCompatibility)
							{
								this.ReportError("'{1}' is an invalid value for the '{0}' attribute.", new string[]
								{
									"level",
									this.input.Value
								});
							}
						}
						else
						{
							level = NumberLevel.Any;
						}
					}
					else
					{
						level = NumberLevel.Multiple;
					}
				}
				else
				{
					level = NumberLevel.Single;
				}
			}
			string count = this.ParseStringAttribute(3, "count");
			string from = this.ParseStringAttribute(4, "from");
			string text = this.ParseStringAttribute(5, "format");
			string lang = this.ParseStringAttribute(6, "lang");
			string letterValue = this.ParseStringAttribute(7, "letter-value");
			if (!string.IsNullOrEmpty(this.ParseStringAttribute(8, "ordinal")))
			{
				this.ReportNYI("xsl:number/@ordinal");
			}
			string groupingSeparator = this.ParseStringAttribute(9, "grouping-separator");
			string groupingSize = this.ParseStringAttribute(10, "grouping-size");
			if (text == null)
			{
				text = "1";
			}
			this.CheckNoContent();
			return XsltLoader.SetInfo(AstFactory.Number(level, count, from, value, text, lang, letterValue, groupingSeparator, groupingSize, this.input.XslVersion), null, attributes);
		}

		// Token: 0x060029D9 RID: 10713 RVA: 0x000FBE24 File Offset: 0x000FA024
		private XslNode XslValueOf()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.valueOfAttributes);
			string text = this.ParseStringAttribute(0, "select");
			bool flag = this.ParseStringAttribute(1, "separator") != null;
			bool flag2 = this.ParseYesNoAttribute(2, "disable-output-escaping") == TriState.True;
			if (!flag)
			{
				if (!this.input.BackwardCompatibility && text == null)
				{
					string empty = string.Empty;
				}
			}
			else
			{
				this.ReportNYI("xsl:value-of/@separator");
			}
			if (this.V1)
			{
				if (text == null)
				{
					this.input.SkipNode();
					return XsltLoader.SetInfo(AstFactory.Error(XslLoadException.CreateMessage(attributes.lineInfo, "Missing mandatory attribute '{0}'.", new string[]
					{
						"select"
					})), null, attributes);
				}
				this.CheckNoContent();
			}
			else
			{
				List<XslNode> list = this.LoadContent(text != null);
				this.CheckError(text == null && list.Count == 0, "Element '{0}' must have either 'select' attribute or non-empty content.", new string[]
				{
					this.input.ElementName
				});
				if (list.Count != 0)
				{
					this.ReportNYI("xsl:value-of/*");
				}
			}
			return XsltLoader.SetInfo(AstFactory.XslNode(flag2 ? XslNodeType.ValueOfDoe : XslNodeType.ValueOf, null, text, this.input.XslVersion), null, attributes);
		}

		// Token: 0x060029DA RID: 10714 RVA: 0x000FBF54 File Offset: 0x000FA154
		private VarPar XslVarPar()
		{
			string localName = this.input.LocalName;
			XslNodeType xslNodeType = Ref.Equal(localName, this.atoms.Variable) ? XslNodeType.Variable : (Ref.Equal(localName, this.atoms.Param) ? XslNodeType.Param : (Ref.Equal(localName, this.atoms.WithParam) ? XslNodeType.WithParam : XslNodeType.Unknown));
			Ref.Equal(localName, this.atoms.Param);
			XsltInput.ContextInfo attributes = this.input.GetAttributes((xslNodeType == XslNodeType.Variable) ? this.variableAttributes : ((xslNodeType == XslNodeType.Param) ? this.paramAttributes : this.withParamAttributes));
			QilName qilName = this.ParseQNameAttribute(0);
			string text = this.ParseStringAttribute(1, "select");
			string text2 = this.ParseStringAttribute(2, "as");
			TriState triState = this.ParseYesNoAttribute(3, "required");
			if (xslNodeType == XslNodeType.Param && this.curFunction != null)
			{
				if (!this.input.ForwardCompatibility)
				{
					this.CheckError(triState != TriState.Unknown, "The 'required' attribute must not be specified for parameter '{0}'. Function parameters are always mandatory.", new string[]
					{
						qilName.ToString()
					});
				}
				triState = TriState.True;
			}
			else if (triState == TriState.True)
			{
				this.ReportNYI("xsl:param/@required == true()");
			}
			if (text2 != null)
			{
				this.ReportNYI("xsl:param/@as");
			}
			TriState triState2 = this.ParseYesNoAttribute(4, "tunnel");
			if (triState2 != TriState.Unknown)
			{
				if (xslNodeType == XslNodeType.Param && this.curTemplate == null)
				{
					if (!this.input.ForwardCompatibility)
					{
						this.ReportError("Stylesheet or function parameter '{0}' cannot have attribute 'tunnel'.", new string[]
						{
							qilName.ToString()
						});
					}
				}
				else if (triState2 == TriState.True)
				{
					this.ReportNYI("xsl:param/@tunnel == true()");
				}
			}
			List<XslNode> list = this.LoadContent(text != null);
			this.CheckError(triState == TriState.True && (text != null || list.Count != 0), "Mandatory parameter '{0}' must be empty and must not have a 'select' attribute.", new string[]
			{
				qilName.ToString()
			});
			VarPar varPar = AstFactory.VarPar(xslNodeType, qilName, text, this.input.XslVersion);
			XsltLoader.SetInfo(varPar, list, attributes);
			return varPar;
		}

		// Token: 0x060029DB RID: 10715 RVA: 0x000FC13C File Offset: 0x000FA33C
		private XslNode XslComment()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.commentAttributes);
			string text = this.ParseStringAttribute(0, "select");
			if (text != null)
			{
				this.ReportNYI("xsl:comment/@select");
			}
			return XsltLoader.SetInfo(AstFactory.Comment(), this.LoadContent(text != null), attributes);
		}

		// Token: 0x060029DC RID: 10716 RVA: 0x000FC18C File Offset: 0x000FA38C
		private List<XslNode> LoadContent(bool hasSelect)
		{
			XsltInput.DelayedQName elementName = this.input.ElementName;
			List<XslNode> list = this.LoadInstructions();
			this.CheckError(hasSelect && list.Count != 0, "The element '{0}' cannot have both a 'select' attribute and non-empty content.", new string[]
			{
				elementName
			});
			if (list.Count != 0)
			{
				list = this.LoadEndTag(list);
			}
			return list;
		}

		// Token: 0x060029DD RID: 10717 RVA: 0x000FC1E8 File Offset: 0x000FA3E8
		private XslNode XslProcessingInstruction()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.processingInstructionAttributes);
			string name = this.ParseNCNameAttribute(0);
			string text = this.ParseStringAttribute(1, "select");
			if (text != null)
			{
				this.ReportNYI("xsl:processing-instruction/@select");
			}
			return XsltLoader.SetInfo(AstFactory.PI(name, this.input.XslVersion), this.LoadContent(text != null), attributes);
		}

		// Token: 0x060029DE RID: 10718 RVA: 0x000FC24C File Offset: 0x000FA44C
		private XslNode XslText()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.textAttributes);
			SerializationHints hints = (this.ParseYesNoAttribute(0, "disable-output-escaping") == TriState.True) ? SerializationHints.DisableOutputEscaping : SerializationHints.None;
			List<XslNode> list = new List<XslNode>();
			XsltInput.DelayedQName elementName = this.input.ElementName;
			if (this.input.MoveToFirstChild())
			{
				do
				{
					XmlNodeType nodeType = this.input.NodeType;
					if (nodeType == XmlNodeType.Text || nodeType - XmlNodeType.Whitespace <= 1)
					{
						list.Add(AstFactory.Text(this.input.Value, hints));
					}
					else
					{
						this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
						{
							this.input.QualifiedName,
							elementName
						});
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			return XsltLoader.SetInfo(AstFactory.List(), list, attributes);
		}

		// Token: 0x060029DF RID: 10719 RVA: 0x000FC320 File Offset: 0x000FA520
		private XslNode XslElement()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.elementAttributes);
			string nameAvt = this.ParseNCNameAttribute(0);
			string text = this.ParseStringAttribute(1, "namespace");
			this.CheckError(text == "http://www.w3.org/2000/xmlns/", "Elements and attributes cannot belong to the reserved namespace '{0}'.", new string[]
			{
				text
			});
			if (this.ParseYesNoAttribute(2, "inherit-namespaces") <= TriState.False)
			{
				this.ReportNYI("xsl:copy[@inherit-namespaces = 'no']");
			}
			this.ParseTypeAttribute(4);
			this.ParseValidationAttribute(5, false);
			List<XslNode> list = new List<XslNode>();
			if (this.input.MoveToXsltAttribute(3, "use-attribute-sets"))
			{
				this.AddUseAttributeSets(list);
			}
			return XsltLoader.SetInfo(AstFactory.Element(nameAvt, text, this.input.XslVersion), this.LoadEndTag(this.LoadInstructions(list)), attributes);
		}

		// Token: 0x060029E0 RID: 10720 RVA: 0x000FC3E4 File Offset: 0x000FA5E4
		private XslNode XslAttribute()
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.attributeAttributes);
			string nameAvt = this.ParseNCNameAttribute(0);
			string text = this.ParseStringAttribute(1, "namespace");
			this.CheckError(text == "http://www.w3.org/2000/xmlns/", "Elements and attributes cannot belong to the reserved namespace '{0}'.", new string[]
			{
				text
			});
			string text2 = this.ParseStringAttribute(2, "select");
			if (text2 != null)
			{
				this.ReportNYI("xsl:attribute/@select");
			}
			string text3 = this.ParseStringAttribute(3, "separator");
			if (text3 != null)
			{
				this.ReportNYI("xsl:attribute/@separator");
			}
			string text4 = (text3 != null) ? text3 : ((text2 != null) ? " " : string.Empty);
			this.ParseTypeAttribute(4);
			this.ParseValidationAttribute(5, false);
			return XsltLoader.SetInfo(AstFactory.Attribute(nameAvt, text, this.input.XslVersion), this.LoadContent(text2 != null), attributes);
		}

		// Token: 0x060029E1 RID: 10721 RVA: 0x000FC4BC File Offset: 0x000FA6BC
		private XslNode XslSort(int sortNumber)
		{
			XsltInput.ContextInfo attributes = this.input.GetAttributes(this.sortAttributes);
			string text = this.ParseStringAttribute(0, "select");
			string lang = this.ParseStringAttribute(1, "lang");
			string order = this.ParseStringAttribute(2, "order");
			this.ParseCollationAttribute(3);
			int num = (int)this.ParseYesNoAttribute(4, "stable");
			string caseOrder = this.ParseStringAttribute(5, "case-order");
			string dataType = this.ParseStringAttribute(6, "data-type");
			if (num != -1)
			{
				this.CheckError(sortNumber != 0, "Only the first 'xsl:sort' element may have 'stable' attribute.", Array.Empty<string>());
			}
			if (this.V1)
			{
				this.CheckNoContent();
			}
			else if (this.LoadContent(text != null).Count != 0)
			{
				this.ReportNYI("xsl:sort/*");
			}
			if (text == null)
			{
				text = ".";
			}
			return XsltLoader.SetInfo(AstFactory.Sort(text, lang, dataType, order, caseOrder, this.input.XslVersion), null, attributes);
		}

		// Token: 0x060029E2 RID: 10722 RVA: 0x000FC59C File Offset: 0x000FA79C
		private XslNode LoadLiteralResultElement(bool asStylesheet)
		{
			string prefix = this.input.Prefix;
			string localName = this.input.LocalName;
			string namespaceUri = this.input.NamespaceUri;
			XsltInput.ContextInfo literalAttributes = this.input.GetLiteralAttributes(asStylesheet);
			if (this.input.IsExtensionNamespace(namespaceUri))
			{
				return XsltLoader.SetInfo(AstFactory.List(), this.LoadFallbacks(localName), literalAttributes);
			}
			List<XslNode> list = new List<XslNode>();
			int num = 1;
			while (this.input.MoveToLiteralAttribute(num))
			{
				if (this.input.IsXsltNamespace() && this.input.IsKeyword(this.atoms.UseAttributeSets))
				{
					this.AddUseAttributeSets(list);
				}
				num++;
			}
			int num2 = 1;
			while (this.input.MoveToLiteralAttribute(num2))
			{
				if (!this.input.IsXsltNamespace())
				{
					XslNode node = AstFactory.LiteralAttribute(AstFactory.QName(this.input.LocalName, this.input.NamespaceUri, this.input.Prefix), this.input.Value, this.input.XslVersion);
					XsltLoader.AddInstruction(list, XsltLoader.SetLineInfo(node, literalAttributes.lineInfo));
				}
				num2++;
			}
			list = this.LoadEndTag(this.LoadInstructions(list));
			return XsltLoader.SetInfo(AstFactory.LiteralElement(AstFactory.QName(localName, namespaceUri, prefix)), list, literalAttributes);
		}

		// Token: 0x060029E3 RID: 10723 RVA: 0x000FC6EC File Offset: 0x000FA8EC
		private void CheckWithParam(List<XslNode> content, XslNode withParam)
		{
			foreach (XslNode xslNode in content)
			{
				if (xslNode.NodeType == XslNodeType.WithParam && xslNode.Name.Equals(withParam.Name))
				{
					this.ReportError("Value of parameter '{0}' cannot be specified more than once within a single 'xsl:call-template' or 'xsl:apply-templates' element.", new string[]
					{
						withParam.Name.QualifiedName
					});
					break;
				}
			}
		}

		// Token: 0x060029E4 RID: 10724 RVA: 0x000FC774 File Offset: 0x000FA974
		private static void AddInstruction(List<XslNode> content, XslNode instruction)
		{
			if (instruction != null)
			{
				content.Add(instruction);
			}
		}

		// Token: 0x060029E5 RID: 10725 RVA: 0x000FC780 File Offset: 0x000FA980
		private List<XslNode> LoadEndTag(List<XslNode> content)
		{
			if (this.compiler.IsDebug && !this.input.IsEmptyElement)
			{
				XsltLoader.AddInstruction(content, XsltLoader.SetLineInfo(AstFactory.Nop(), this.input.BuildLineInfo()));
			}
			return content;
		}

		// Token: 0x060029E6 RID: 10726 RVA: 0x000FC7B8 File Offset: 0x000FA9B8
		private XslNode LoadUnknownXsltInstruction(string parentName)
		{
			this.input.GetVersionAttribute();
			if (!this.input.ForwardCompatibility)
			{
				this.ReportError("'{0}' cannot be a child of the '{1}' element.", new string[]
				{
					this.input.QualifiedName,
					parentName
				});
				this.input.SkipNode();
				return null;
			}
			XsltInput.ContextInfo attributes = this.input.GetAttributes();
			List<XslNode> content = this.LoadFallbacks(this.input.LocalName);
			return XsltLoader.SetInfo(AstFactory.List(), content, attributes);
		}

		// Token: 0x060029E7 RID: 10727 RVA: 0x000FC838 File Offset: 0x000FAA38
		private List<XslNode> LoadFallbacks(string instrName)
		{
			this.input.MoveToElement();
			ISourceLineInfo lineInfo = this.input.BuildNameLineInfo();
			List<XslNode> list = new List<XslNode>();
			if (this.input.MoveToFirstChild())
			{
				do
				{
					if (this.input.IsXsltKeyword(this.atoms.Fallback))
					{
						XsltInput.ContextInfo attributes = this.input.GetAttributes();
						list.Add(XsltLoader.SetInfo(AstFactory.List(), this.LoadInstructions(), attributes));
					}
					else
					{
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			if (list.Count == 0)
			{
				list.Add(AstFactory.Error(XslLoadException.CreateMessage(lineInfo, "'{0}' is not a recognized extension element.", new string[]
				{
					instrName
				})));
			}
			return list;
		}

		// Token: 0x060029E8 RID: 10728 RVA: 0x000FC8F0 File Offset: 0x000FAAF0
		private QilName ParseModeAttribute(int attNum)
		{
			if (!this.input.MoveToXsltAttribute(attNum, "mode"))
			{
				return XsltLoader.nullMode;
			}
			this.compiler.EnterForwardsCompatible();
			string value = this.input.Value;
			QilName result;
			if (!this.V1 && value == "#default")
			{
				result = XsltLoader.nullMode;
			}
			else if (!this.V1 && value == "#current")
			{
				this.ReportNYI("xsl:apply-templates[@mode='#current']");
				result = XsltLoader.nullMode;
			}
			else if (!this.V1 && value == "#all")
			{
				this.ReportError("List of modes in 'xsl:template' element can't contain token '#all' together with any other value.", Array.Empty<string>());
				result = XsltLoader.nullMode;
			}
			else
			{
				result = this.CreateXPathQName(value);
			}
			if (!this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
			{
				result = XsltLoader.nullMode;
			}
			return result;
		}

		// Token: 0x060029E9 RID: 10729 RVA: 0x000FC9C8 File Offset: 0x000FABC8
		private QilName ParseModeListAttribute(int attNum)
		{
			if (!this.input.MoveToXsltAttribute(attNum, "mode"))
			{
				return XsltLoader.nullMode;
			}
			string value = this.input.Value;
			if (value == "#all")
			{
				this.ReportNYI("xsl:template[@mode='#all']");
				return XsltLoader.nullMode;
			}
			string[] array = XmlConvert.SplitString(value);
			List<QilName> list = new List<QilName>(array.Length);
			this.compiler.EnterForwardsCompatible();
			if (array.Length == 0)
			{
				this.ReportError("List of modes in 'xsl:template' element can't be empty.", Array.Empty<string>());
			}
			else
			{
				foreach (string text in array)
				{
					QilName qilName;
					if (text == "#default")
					{
						qilName = XsltLoader.nullMode;
					}
					else
					{
						if (text == "#current")
						{
							this.ReportNYI("xsl:apply-templates[@mode='#current']");
							break;
						}
						if (text == "#all")
						{
							this.ReportError("List of modes in 'xsl:template' element can't contain token '#all' together with any other value.", Array.Empty<string>());
							break;
						}
						qilName = this.CreateXPathQName(text);
					}
					bool flag = false;
					foreach (QilName qilName2 in list)
					{
						flag |= qilName2.Equals(qilName);
					}
					if (flag)
					{
						this.ReportError("List of modes in 'xsl:template' element can't contain duplicates ('{0}').", new string[]
						{
							text
						});
					}
					else
					{
						list.Add(qilName);
					}
				}
			}
			if (!this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
			{
				list.Clear();
				list.Add(XsltLoader.nullMode);
			}
			if (1 < list.Count)
			{
				this.ReportNYI("Multipe modes");
				return XsltLoader.nullMode;
			}
			if (list.Count == 0)
			{
				return XsltLoader.nullMode;
			}
			return list[0];
		}

		// Token: 0x060029EA RID: 10730 RVA: 0x000FCB94 File Offset: 0x000FAD94
		private string ParseCollationAttribute(int attNum)
		{
			if (this.input.MoveToXsltAttribute(attNum, "collation"))
			{
				this.ReportNYI("@collation");
			}
			return null;
		}

		// Token: 0x060029EB RID: 10731 RVA: 0x000FCBB8 File Offset: 0x000FADB8
		private bool ResolveQName(bool ignoreDefaultNs, string qname, out string localName, out string namespaceName, out string prefix)
		{
			if (qname == null)
			{
				prefix = this.compiler.PhantomNCName;
				localName = this.compiler.PhantomNCName;
				namespaceName = this.compiler.CreatePhantomNamespace();
				return false;
			}
			if (!this.compiler.ParseQName(qname, out prefix, out localName, this))
			{
				namespaceName = this.compiler.CreatePhantomNamespace();
				return false;
			}
			if (ignoreDefaultNs && prefix.Length == 0)
			{
				namespaceName = string.Empty;
			}
			else
			{
				namespaceName = this.input.LookupXmlNamespace(prefix);
				if (namespaceName == null)
				{
					namespaceName = this.compiler.CreatePhantomNamespace();
					return false;
				}
			}
			return true;
		}

		// Token: 0x060029EC RID: 10732 RVA: 0x000FCC54 File Offset: 0x000FAE54
		private QilName ParseQNameAttribute(int attNum)
		{
			bool flag = this.input.IsRequiredAttribute(attNum);
			QilName qilName = null;
			if (!flag)
			{
				this.compiler.EnterForwardsCompatible();
			}
			string local;
			string uri;
			string prefix;
			if (this.input.MoveToXsltAttribute(attNum, "name") && this.ResolveQName(true, this.input.Value, out local, out uri, out prefix))
			{
				qilName = AstFactory.QName(local, uri, prefix);
			}
			if (!flag)
			{
				this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility);
			}
			if (qilName == null && flag)
			{
				qilName = AstFactory.QName(this.compiler.PhantomNCName, this.compiler.CreatePhantomNamespace(), this.compiler.PhantomNCName);
			}
			return qilName;
		}

		// Token: 0x060029ED RID: 10733 RVA: 0x000FCD02 File Offset: 0x000FAF02
		private string ParseNCNameAttribute(int attNum)
		{
			if (this.input.MoveToXsltAttribute(attNum, "name"))
			{
				return this.input.Value;
			}
			return this.compiler.PhantomNCName;
		}

		// Token: 0x060029EE RID: 10734 RVA: 0x000FCD30 File Offset: 0x000FAF30
		private QilName CreateXPathQName(string qname)
		{
			string local;
			string uri;
			string prefix;
			this.ResolveQName(true, qname, out local, out uri, out prefix);
			return AstFactory.QName(local, uri, prefix);
		}

		// Token: 0x060029EF RID: 10735 RVA: 0x000FCD54 File Offset: 0x000FAF54
		private XmlQualifiedName ResolveQName(bool ignoreDefaultNs, string qname)
		{
			string name;
			string ns;
			string text;
			this.ResolveQName(ignoreDefaultNs, qname, out name, out ns, out text);
			return new XmlQualifiedName(name, ns);
		}

		// Token: 0x060029F0 RID: 10736 RVA: 0x000FCD78 File Offset: 0x000FAF78
		private void ParseWhitespaceRules(string elements, bool preserveSpace)
		{
			if (elements != null && elements.Length != 0)
			{
				string[] array = XmlConvert.SplitString(elements);
				for (int i = 0; i < array.Length; i++)
				{
					string text;
					string text2;
					string text3;
					if (!this.compiler.ParseNameTest(array[i], out text, out text2, this))
					{
						text3 = this.compiler.CreatePhantomNamespace();
					}
					else if (text == null || text.Length == 0)
					{
						text3 = text;
					}
					else
					{
						text3 = this.input.LookupXmlNamespace(text);
						if (text3 == null)
						{
							text3 = this.compiler.CreatePhantomNamespace();
						}
					}
					int index = ((text2 == null) ? 1 : 0) + ((text3 == null) ? 1 : 0);
					this.curStylesheet.AddWhitespaceRule(index, new WhitespaceRule(text2, text3, preserveSpace));
				}
			}
		}

		// Token: 0x060029F1 RID: 10737 RVA: 0x000FCE2C File Offset: 0x000FB02C
		private XmlQualifiedName ParseOutputMethod(string attValue, out XmlOutputMethod method)
		{
			string text;
			string text2;
			string text3;
			this.ResolveQName(true, attValue, out text, out text2, out text3);
			method = XmlOutputMethod.AutoDetect;
			if (this.compiler.IsPhantomNamespace(text2))
			{
				return null;
			}
			if (text3.Length == 0)
			{
				if (!(text == "xml"))
				{
					if (!(text == "html"))
					{
						if (!(text == "text"))
						{
							this.ReportError("'{1}' is an invalid value for the '{0}' attribute.", new string[]
							{
								"method",
								attValue
							});
							return null;
						}
						method = XmlOutputMethod.Text;
					}
					else
					{
						method = XmlOutputMethod.Html;
					}
				}
				else
				{
					method = XmlOutputMethod.Xml;
				}
			}
			else if (!this.input.ForwardCompatibility)
			{
				this.ReportWarning("'{0}' is not a supported output method. Supported methods are 'xml', 'html', and 'text'.", new string[]
				{
					attValue
				});
			}
			return new XmlQualifiedName(text, text2);
		}

		// Token: 0x060029F2 RID: 10738 RVA: 0x000FCEE4 File Offset: 0x000FB0E4
		private void AddUseAttributeSets(List<XslNode> list)
		{
			this.compiler.EnterForwardsCompatible();
			foreach (string qname in XmlConvert.SplitString(this.input.Value))
			{
				XsltLoader.AddInstruction(list, XsltLoader.SetLineInfo(AstFactory.UseAttributeSet(this.CreateXPathQName(qname)), this.input.BuildLineInfo()));
			}
			if (!this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
			{
				list.Clear();
			}
		}

		// Token: 0x060029F3 RID: 10739 RVA: 0x000FCF60 File Offset: 0x000FB160
		private List<QilName> ParseUseCharacterMaps(int attNum)
		{
			List<QilName> list = new List<QilName>();
			if (this.input.MoveToXsltAttribute(attNum, "use-character-maps"))
			{
				this.compiler.EnterForwardsCompatible();
				foreach (string qname in XmlConvert.SplitString(this.input.Value))
				{
					list.Add(this.CreateXPathQName(qname));
				}
				if (!this.compiler.ExitForwardsCompatible(this.input.ForwardCompatibility))
				{
					list.Clear();
				}
			}
			return list;
		}

		// Token: 0x060029F4 RID: 10740 RVA: 0x000FCFE0 File Offset: 0x000FB1E0
		private string ParseStringAttribute(int attNum, string attName)
		{
			if (this.input.MoveToXsltAttribute(attNum, attName))
			{
				return this.input.Value;
			}
			return null;
		}

		// Token: 0x060029F5 RID: 10741 RVA: 0x000FD000 File Offset: 0x000FB200
		private char ParseCharAttribute(int attNum, string attName, char defVal)
		{
			if (this.input.MoveToXsltAttribute(attNum, attName))
			{
				if (this.input.Value.Length == 1)
				{
					return this.input.Value[0];
				}
				if (this.input.IsRequiredAttribute(attNum) || !this.input.ForwardCompatibility)
				{
					this.ReportError("The value of the '{0}' attribute must be a single character.", new string[]
					{
						attName
					});
				}
			}
			return defVal;
		}

		// Token: 0x060029F6 RID: 10742 RVA: 0x000FD074 File Offset: 0x000FB274
		private TriState ParseYesNoAttribute(int attNum, string attName)
		{
			if (this.input.MoveToXsltAttribute(attNum, attName))
			{
				string value = this.input.Value;
				if (value == "yes")
				{
					return TriState.True;
				}
				if (value == "no")
				{
					return TriState.False;
				}
				if (!this.input.ForwardCompatibility)
				{
					this.ReportError("The value of the '{0}' attribute must be '{1}' or '{2}'.", new string[]
					{
						attName,
						"yes",
						"no"
					});
				}
			}
			return TriState.Unknown;
		}

		// Token: 0x060029F7 RID: 10743 RVA: 0x000FD0EF File Offset: 0x000FB2EF
		private void ParseTypeAttribute(int attNum)
		{
			if (this.input.MoveToXsltAttribute(attNum, "type"))
			{
				this.CheckError(true, "Attribute '{0}' is not permitted in basic XSLT processor (http://www.w3.org/TR/xslt20/#dt-basic-xslt-processor).", new string[]
				{
					"type"
				});
			}
		}

		// Token: 0x060029F8 RID: 10744 RVA: 0x000FD120 File Offset: 0x000FB320
		private void ParseValidationAttribute(int attNum, bool defVal)
		{
			string text = defVal ? this.atoms.DefaultValidation : "validation";
			if (this.input.MoveToXsltAttribute(attNum, text))
			{
				string value = this.input.Value;
				if (!(value == "strip"))
				{
					if (value == "preserve" || (value == "strict" && !defVal) || (value == "lax" && !defVal))
					{
						this.ReportError("Value '{1}' of attribute '{0}' is not permitted in basic XSLT processor (http://www.w3.org/TR/xslt20/#dt-basic-xslt-processor).", new string[]
						{
							text,
							value
						});
						return;
					}
					if (!this.input.ForwardCompatibility)
					{
						this.ReportError("'{1}' is an invalid value for the '{0}' attribute.", new string[]
						{
							text,
							value
						});
					}
				}
			}
		}

		// Token: 0x060029F9 RID: 10745 RVA: 0x000FD1DC File Offset: 0x000FB3DC
		private void ParseInputTypeAnnotationsAttribute(int attNum)
		{
			if (this.input.MoveToXsltAttribute(attNum, "input-type-annotations"))
			{
				string value = this.input.Value;
				if (!(value == "unspecified"))
				{
					if (value == "strip" || value == "preserve")
					{
						if (this.compiler.inputTypeAnnotations == null)
						{
							this.compiler.inputTypeAnnotations = value;
							return;
						}
						this.CheckError(this.compiler.inputTypeAnnotations != value, "It is an error if there is a stylesheet module in the stylesheet that specifies 'input-type-annotations'=\"strip\" and another stylesheet module that specifies 'input-type-annotations'=\"preserve\".", Array.Empty<string>());
						return;
					}
					else if (!this.input.ForwardCompatibility)
					{
						this.ReportError("'{1}' is an invalid value for the '{0}' attribute.", new string[]
						{
							"input-type-annotations",
							value
						});
					}
				}
			}
		}

		// Token: 0x060029FA RID: 10746 RVA: 0x000FD29C File Offset: 0x000FB49C
		private void CheckNoContent()
		{
			this.input.MoveToElement();
			XsltInput.DelayedQName elementName = this.input.ElementName;
			ISourceLineInfo sourceLineInfo = this.SkipEmptyContent();
			if (sourceLineInfo != null)
			{
				this.compiler.ReportError(sourceLineInfo, "The contents of '{0}' must be empty.", new string[]
				{
					elementName
				});
			}
		}

		// Token: 0x060029FB RID: 10747 RVA: 0x000FD2EC File Offset: 0x000FB4EC
		private ISourceLineInfo SkipEmptyContent()
		{
			ISourceLineInfo sourceLineInfo = null;
			if (this.input.MoveToFirstChild())
			{
				do
				{
					if (this.input.NodeType != XmlNodeType.Whitespace)
					{
						if (sourceLineInfo == null)
						{
							sourceLineInfo = this.input.BuildNameLineInfo();
						}
						this.input.SkipNode();
					}
				}
				while (this.input.MoveToNextSibling());
			}
			return sourceLineInfo;
		}

		// Token: 0x060029FC RID: 10748 RVA: 0x000FD33F File Offset: 0x000FB53F
		private static XslNode SetLineInfo(XslNode node, ISourceLineInfo lineInfo)
		{
			node.SourceLine = lineInfo;
			return node;
		}

		// Token: 0x060029FD RID: 10749 RVA: 0x000FD349 File Offset: 0x000FB549
		private static void SetContent(XslNode node, List<XslNode> content)
		{
			if (content != null && content.Count == 0)
			{
				content = null;
			}
			node.SetContent(content);
		}

		// Token: 0x060029FE RID: 10750 RVA: 0x000FD360 File Offset: 0x000FB560
		internal static XslNode SetInfo(XslNode to, List<XslNode> content, XsltInput.ContextInfo info)
		{
			to.Namespaces = info.nsList;
			XsltLoader.SetContent(to, content);
			XsltLoader.SetLineInfo(to, info.lineInfo);
			return to;
		}

		// Token: 0x060029FF RID: 10751 RVA: 0x000FD384 File Offset: 0x000FB584
		private static NsDecl MergeNamespaces(NsDecl thisList, NsDecl parentList)
		{
			if (parentList == null)
			{
				return thisList;
			}
			if (thisList == null)
			{
				return parentList;
			}
			while (parentList != null)
			{
				bool flag = false;
				for (NsDecl nsDecl = thisList; nsDecl != null; nsDecl = nsDecl.Prev)
				{
					if (Ref.Equal(nsDecl.Prefix, parentList.Prefix) && (nsDecl.Prefix != null || nsDecl.NsUri == parentList.NsUri))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					thisList = new NsDecl(thisList, parentList.Prefix, parentList.NsUri);
				}
				parentList = parentList.Prev;
			}
			return thisList;
		}

		// Token: 0x06002A00 RID: 10752 RVA: 0x000FD400 File Offset: 0x000FB600
		public void ReportError(string res, params string[] args)
		{
			this.compiler.ReportError(this.input.BuildNameLineInfo(), res, args);
		}

		// Token: 0x06002A01 RID: 10753 RVA: 0x000FD41A File Offset: 0x000FB61A
		public void ReportWarning(string res, params string[] args)
		{
			this.compiler.ReportWarning(this.input.BuildNameLineInfo(), res, args);
		}

		// Token: 0x06002A02 RID: 10754 RVA: 0x000FD434 File Offset: 0x000FB634
		private void ReportNYI(string arg)
		{
			if (!this.input.ForwardCompatibility)
			{
				this.ReportError("'{0}' is not yet implemented.", new string[]
				{
					arg
				});
			}
		}

		// Token: 0x06002A03 RID: 10755 RVA: 0x000FD458 File Offset: 0x000FB658
		public void CheckError(bool cond, string res, params string[] args)
		{
			if (cond)
			{
				this.compiler.ReportError(this.input.BuildNameLineInfo(), res, args);
			}
		}

		// Token: 0x06002A04 RID: 10756 RVA: 0x000FD478 File Offset: 0x000FB678
		public XsltLoader()
		{
		}

		// Token: 0x06002A05 RID: 10757 RVA: 0x000FE231 File Offset: 0x000FC431
		// Note: this type is marked as 'beforefieldinit'.
		static XsltLoader()
		{
		}

		// Token: 0x040020BB RID: 8379
		private Compiler compiler;

		// Token: 0x040020BC RID: 8380
		private XmlResolver xmlResolver;

		// Token: 0x040020BD RID: 8381
		private QueryReaderSettings readerSettings;

		// Token: 0x040020BE RID: 8382
		private KeywordsTable atoms;

		// Token: 0x040020BF RID: 8383
		private XsltInput input;

		// Token: 0x040020C0 RID: 8384
		private Stylesheet curStylesheet;

		// Token: 0x040020C1 RID: 8385
		private Template curTemplate;

		// Token: 0x040020C2 RID: 8386
		private object curFunction;

		// Token: 0x040020C3 RID: 8387
		internal static QilName nullMode = AstFactory.QName(string.Empty);

		// Token: 0x040020C4 RID: 8388
		public static int V1Opt = 1;

		// Token: 0x040020C5 RID: 8389
		public static int V1Req = 2;

		// Token: 0x040020C6 RID: 8390
		public static int V2Opt = 4;

		// Token: 0x040020C7 RID: 8391
		public static int V2Req = 8;

		// Token: 0x040020C8 RID: 8392
		private HybridDictionary documentUriInUse = new HybridDictionary();

		// Token: 0x040020C9 RID: 8393
		private XsltInput.XsltAttribute[] stylesheetAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("version", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("id", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("default-validation", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("input-type-annotations", XsltLoader.V2Opt)
		};

		// Token: 0x040020CA RID: 8394
		private XsltInput.XsltAttribute[] importIncludeAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("href", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020CB RID: 8395
		private XsltInput.XsltAttribute[] loadStripSpaceAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("elements", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020CC RID: 8396
		private XsltInput.XsltAttribute[] outputAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("method", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("byte-order-mark", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("cdata-section-elements", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("doctype-public", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("doctype-system", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("encoding", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("escape-uri-attributes", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("include-content-type", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("indent", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("media-type", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("normalization-form", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("omit-xml-declaration", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("standalone", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("undeclare-prefixes", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("use-character-maps", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("version", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020CD RID: 8397
		private XsltInput.XsltAttribute[] keyAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("match", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("use", XsltLoader.V1Req | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("collation", XsltLoader.V2Opt)
		};

		// Token: 0x040020CE RID: 8398
		private XsltInput.XsltAttribute[] decimalFormatAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("infinity", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("NaN", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("decimal-separator", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("grouping-separator", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("percent", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("per-mille", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("zero-digit", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("digit", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("pattern-separator", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("minus-sign", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020CF RID: 8399
		private XsltInput.XsltAttribute[] namespaceAliasAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("stylesheet-prefix", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("result-prefix", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020D0 RID: 8400
		private XsltInput.XsltAttribute[] attributeSetAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("use-attribute-sets", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020D1 RID: 8401
		private XsltInput.XsltAttribute[] templateAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("match", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("name", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("priority", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("mode", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("as", XsltLoader.V2Opt)
		};

		// Token: 0x040020D2 RID: 8402
		private XsltInput.XsltAttribute[] scriptAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("implements-prefix", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("language", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020D3 RID: 8403
		private XsltInput.XsltAttribute[] assemblyAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("href", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020D4 RID: 8404
		private XsltInput.XsltAttribute[] usingAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("namespace", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020D5 RID: 8405
		private const int MAX_LOADINSTRUCTIONS_DEPTH = 1024;

		// Token: 0x040020D6 RID: 8406
		private int loadInstructionsDepth;

		// Token: 0x040020D7 RID: 8407
		private XsltInput.XsltAttribute[] applyTemplatesAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("mode", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020D8 RID: 8408
		private XsltInput.XsltAttribute[] callTemplateAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020D9 RID: 8409
		private XsltInput.XsltAttribute[] copyAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("copy-namespaces", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("inherit-namespaces", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("use-attribute-sets", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("type", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("validation", XsltLoader.V2Opt)
		};

		// Token: 0x040020DA RID: 8410
		private XsltInput.XsltAttribute[] copyOfAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("copy-namespaces", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("type", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("validation", XsltLoader.V2Opt)
		};

		// Token: 0x040020DB RID: 8411
		private XsltInput.XsltAttribute[] ifAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("test", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020DC RID: 8412
		private XsltInput.XsltAttribute[] forEachAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V1Req | XsltLoader.V2Req)
		};

		// Token: 0x040020DD RID: 8413
		private XsltInput.XsltAttribute[] messageAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("terminate", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020DE RID: 8414
		private XsltInput.XsltAttribute[] numberAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("value", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("select", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("level", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("count", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("from", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("format", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("lang", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("letter-value", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("ordinal", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("grouping-separator", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("grouping-size", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020DF RID: 8415
		private XsltInput.XsltAttribute[] valueOfAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V1Req | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("separator", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("disable-output-escaping", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020E0 RID: 8416
		private XsltInput.XsltAttribute[] variableAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("select", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("as", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("required", 0),
			new XsltInput.XsltAttribute("tunnel", 0)
		};

		// Token: 0x040020E1 RID: 8417
		private XsltInput.XsltAttribute[] paramAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("select", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("as", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("required", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("tunnel", XsltLoader.V2Opt)
		};

		// Token: 0x040020E2 RID: 8418
		private XsltInput.XsltAttribute[] withParamAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("select", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("as", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("required", 0),
			new XsltInput.XsltAttribute("tunnel", XsltLoader.V2Opt)
		};

		// Token: 0x040020E3 RID: 8419
		private XsltInput.XsltAttribute[] commentAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V2Opt)
		};

		// Token: 0x040020E4 RID: 8420
		private XsltInput.XsltAttribute[] processingInstructionAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("select", XsltLoader.V2Opt)
		};

		// Token: 0x040020E5 RID: 8421
		private XsltInput.XsltAttribute[] textAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("disable-output-escaping", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x040020E6 RID: 8422
		private XsltInput.XsltAttribute[] elementAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("namespace", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("inherit-namespaces", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("use-attribute-sets", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("type", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("validation", XsltLoader.V2Opt)
		};

		// Token: 0x040020E7 RID: 8423
		private XsltInput.XsltAttribute[] attributeAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("name", XsltLoader.V1Req | XsltLoader.V2Req),
			new XsltInput.XsltAttribute("namespace", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("select", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("separator", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("type", XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("validation", XsltLoader.V2Opt)
		};

		// Token: 0x040020E8 RID: 8424
		private XsltInput.XsltAttribute[] sortAttributes = new XsltInput.XsltAttribute[]
		{
			new XsltInput.XsltAttribute("select", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("lang", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("order", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("collation", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("stable", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("case-order", XsltLoader.V1Opt | XsltLoader.V2Opt),
			new XsltInput.XsltAttribute("data-type", XsltLoader.V1Opt | XsltLoader.V2Opt)
		};

		// Token: 0x0200041E RID: 1054
		private enum InstructionFlags
		{
			// Token: 0x040020EA RID: 8426
			None,
			// Token: 0x040020EB RID: 8427
			AllowParam,
			// Token: 0x040020EC RID: 8428
			AllowSort,
			// Token: 0x040020ED RID: 8429
			AllowFallback = 4
		}
	}
}
