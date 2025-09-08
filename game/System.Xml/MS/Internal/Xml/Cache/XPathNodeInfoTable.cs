﻿using System;
using System.Text;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
	// Token: 0x02000675 RID: 1653
	internal sealed class XPathNodeInfoTable
	{
		// Token: 0x06004327 RID: 17191 RVA: 0x0016DAA3 File Offset: 0x0016BCA3
		public XPathNodeInfoTable()
		{
			this._hashTable = new XPathNodeInfoAtom[32];
			this._sizeTable = 0;
		}

		// Token: 0x06004328 RID: 17192 RVA: 0x0016DAC0 File Offset: 0x0016BCC0
		public XPathNodeInfoAtom Create(string localName, string namespaceUri, string prefix, string baseUri, XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar, XPathDocument doc, int lineNumBase, int linePosBase)
		{
			XPathNodeInfoAtom xpathNodeInfoAtom;
			if (this._infoCached == null)
			{
				xpathNodeInfoAtom = new XPathNodeInfoAtom(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
			}
			else
			{
				xpathNodeInfoAtom = this._infoCached;
				this._infoCached = xpathNodeInfoAtom.Next;
				xpathNodeInfoAtom.Init(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
			}
			return this.Atomize(xpathNodeInfoAtom);
		}

		// Token: 0x06004329 RID: 17193 RVA: 0x0016DB20 File Offset: 0x0016BD20
		private XPathNodeInfoAtom Atomize(XPathNodeInfoAtom info)
		{
			for (XPathNodeInfoAtom xpathNodeInfoAtom = this._hashTable[info.GetHashCode() & this._hashTable.Length - 1]; xpathNodeInfoAtom != null; xpathNodeInfoAtom = xpathNodeInfoAtom.Next)
			{
				if (info.Equals(xpathNodeInfoAtom))
				{
					info.Next = this._infoCached;
					this._infoCached = info;
					return xpathNodeInfoAtom;
				}
			}
			if (this._sizeTable >= this._hashTable.Length)
			{
				XPathNodeInfoAtom[] hashTable = this._hashTable;
				this._hashTable = new XPathNodeInfoAtom[hashTable.Length * 2];
				foreach (XPathNodeInfoAtom xpathNodeInfoAtom in hashTable)
				{
					while (xpathNodeInfoAtom != null)
					{
						XPathNodeInfoAtom next = xpathNodeInfoAtom.Next;
						this.AddInfo(xpathNodeInfoAtom);
						xpathNodeInfoAtom = next;
					}
				}
			}
			this.AddInfo(info);
			return info;
		}

		// Token: 0x0600432A RID: 17194 RVA: 0x0016DBC4 File Offset: 0x0016BDC4
		private void AddInfo(XPathNodeInfoAtom info)
		{
			int num = info.GetHashCode() & this._hashTable.Length - 1;
			info.Next = this._hashTable[num];
			this._hashTable[num] = info;
			this._sizeTable++;
		}

		// Token: 0x0600432B RID: 17195 RVA: 0x0016DC08 File Offset: 0x0016BE08
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < this._hashTable.Length; i++)
			{
				stringBuilder.AppendFormat("{0,4}: ", i);
				for (XPathNodeInfoAtom xpathNodeInfoAtom = this._hashTable[i]; xpathNodeInfoAtom != null; xpathNodeInfoAtom = xpathNodeInfoAtom.Next)
				{
					if (xpathNodeInfoAtom != this._hashTable[i])
					{
						stringBuilder.Append("\n      ");
					}
					stringBuilder.Append(xpathNodeInfoAtom);
				}
				stringBuilder.Append('\n');
			}
			return stringBuilder.ToString();
		}

		// Token: 0x04002F4C RID: 12108
		private XPathNodeInfoAtom[] _hashTable;

		// Token: 0x04002F4D RID: 12109
		private int _sizeTable;

		// Token: 0x04002F4E RID: 12110
		private XPathNodeInfoAtom _infoCached;

		// Token: 0x04002F4F RID: 12111
		private const int DefaultTableSize = 32;
	}
}
