﻿using System;
using System.Collections;
using System.Configuration;
using System.Xml;

namespace System.Net.Configuration
{
	// Token: 0x02000766 RID: 1894
	internal class DefaultProxyHandler : IConfigurationSectionHandler
	{
		// Token: 0x06003BB7 RID: 15287 RVA: 0x000CCA20 File Offset: 0x000CAC20
		public virtual object Create(object parent, object configContext, XmlNode section)
		{
			IWebProxy webProxy = parent as IWebProxy;
			if (section.Attributes != null && section.Attributes.Count != 0)
			{
				HandlersUtil.ThrowException("Unrecognized attribute", section);
			}
			foreach (object obj in section.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				XmlNodeType nodeType = xmlNode.NodeType;
				if (nodeType != XmlNodeType.Whitespace && nodeType != XmlNodeType.Comment)
				{
					if (nodeType != XmlNodeType.Element)
					{
						HandlersUtil.ThrowException("Only elements allowed", xmlNode);
					}
					string name = xmlNode.Name;
					if (name == "proxy")
					{
						string text = HandlersUtil.ExtractAttributeValue("usesystemdefault", xmlNode, true);
						string text2 = HandlersUtil.ExtractAttributeValue("bypassonlocal", xmlNode, true);
						string text3 = HandlersUtil.ExtractAttributeValue("proxyaddress", xmlNode, true);
						if (xmlNode.Attributes != null && xmlNode.Attributes.Count != 0)
						{
							HandlersUtil.ThrowException("Unrecognized attribute", xmlNode);
						}
						webProxy = new WebProxy();
						bool flag = text2 != null && string.Compare(text2, "true", true) == 0;
						if (!flag && text2 != null && string.Compare(text2, "false", true) != 0)
						{
							HandlersUtil.ThrowException("Invalid boolean value", xmlNode);
						}
						if (!(webProxy is WebProxy))
						{
							continue;
						}
						((WebProxy)webProxy).BypassProxyOnLocal = flag;
						if (text3 != null)
						{
							try
							{
								((WebProxy)webProxy).Address = new Uri(text3);
								continue;
							}
							catch (UriFormatException)
							{
							}
						}
						if (text == null || string.Compare(text, "true", true) != 0)
						{
							continue;
						}
						text3 = Environment.GetEnvironmentVariable("http_proxy");
						if (text3 == null)
						{
							text3 = Environment.GetEnvironmentVariable("HTTP_PROXY");
						}
						if (text3 == null)
						{
							continue;
						}
						try
						{
							Uri uri = new Uri(text3);
							IPAddress obj2;
							if (IPAddress.TryParse(uri.Host, out obj2))
							{
								if (IPAddress.Any.Equals(obj2))
								{
									uri = new UriBuilder(uri)
									{
										Host = "127.0.0.1"
									}.Uri;
								}
								else if (IPAddress.IPv6Any.Equals(obj2))
								{
									uri = new UriBuilder(uri)
									{
										Host = "[::1]"
									}.Uri;
								}
							}
							((WebProxy)webProxy).Address = uri;
							continue;
						}
						catch (UriFormatException)
						{
							continue;
						}
					}
					if (name == "bypasslist")
					{
						if (webProxy is WebProxy)
						{
							DefaultProxyHandler.FillByPassList(xmlNode, (WebProxy)webProxy);
						}
					}
					else
					{
						if (name == "module")
						{
							HandlersUtil.ThrowException("WARNING: module not implemented yet", xmlNode);
						}
						HandlersUtil.ThrowException("Unexpected element", xmlNode);
					}
				}
			}
			return webProxy;
		}

		// Token: 0x06003BB8 RID: 15288 RVA: 0x000CCCE0 File Offset: 0x000CAEE0
		private static void FillByPassList(XmlNode node, WebProxy proxy)
		{
			ArrayList arrayList = new ArrayList(proxy.BypassArrayList);
			if (node.Attributes != null && node.Attributes.Count != 0)
			{
				HandlersUtil.ThrowException("Unrecognized attribute", node);
			}
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				XmlNodeType nodeType = xmlNode.NodeType;
				if (nodeType != XmlNodeType.Whitespace && nodeType != XmlNodeType.Comment)
				{
					if (nodeType != XmlNodeType.Element)
					{
						HandlersUtil.ThrowException("Only elements allowed", xmlNode);
					}
					string name = xmlNode.Name;
					if (name == "add")
					{
						string text = HandlersUtil.ExtractAttributeValue("address", xmlNode);
						if (!arrayList.Contains(text))
						{
							arrayList.Add(text);
						}
					}
					else if (name == "remove")
					{
						string obj2 = HandlersUtil.ExtractAttributeValue("address", xmlNode);
						arrayList.Remove(obj2);
					}
					else if (name == "clear")
					{
						if (node.Attributes != null && node.Attributes.Count != 0)
						{
							HandlersUtil.ThrowException("Unrecognized attribute", node);
						}
						arrayList.Clear();
					}
					else
					{
						HandlersUtil.ThrowException("Unexpected element", xmlNode);
					}
				}
			}
			proxy.BypassList = (string[])arrayList.ToArray(typeof(string));
		}

		// Token: 0x06003BB9 RID: 15289 RVA: 0x0000219B File Offset: 0x0000039B
		public DefaultProxyHandler()
		{
		}
	}
}
