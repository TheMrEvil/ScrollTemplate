﻿using System;

namespace System.Data
{
	/// <summary>Returns a singleton instance of the <see cref="T:System.Data.DataRowComparer`1" /> class.</summary>
	// Token: 0x02000005 RID: 5
	public static class DataRowComparer
	{
		/// <summary>Gets a singleton instance of <see cref="T:System.Data.DataRowComparer`1" />. This property is read-only.</summary>
		/// <returns>An instance of a <see cref="T:System.Data.DataRowComparer`1" />.</returns>
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000021E7 File Offset: 0x000003E7
		public static DataRowComparer<DataRow> Default
		{
			get
			{
				return DataRowComparer<DataRow>.Default;
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000021F0 File Offset: 0x000003F0
		internal static bool AreEqual(object a, object b)
		{
			return a == b || (a != null && a != DBNull.Value && b != null && b != DBNull.Value && (a.Equals(b) || (a.GetType().IsArray && DataRowComparer.CompareArray((Array)a, b as Array))));
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002246 File Offset: 0x00000446
		private static bool AreElementEqual(object a, object b)
		{
			return a == b || (a != null && a != DBNull.Value && b != null && b != DBNull.Value && a.Equals(b));
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002270 File Offset: 0x00000470
		private static bool CompareArray(Array a, Array b)
		{
			if (b == null || 1 != a.Rank || 1 != b.Rank || a.Length != b.Length)
			{
				return false;
			}
			int i = a.GetLowerBound(0);
			int num = b.GetLowerBound(0);
			if (a.GetType() == b.GetType() && i == 0 && num == 0)
			{
				TypeCode typeCode = Type.GetTypeCode(a.GetType().GetElementType());
				switch (typeCode)
				{
				case TypeCode.Byte:
					return DataRowComparer.CompareEquatableArray<byte>((byte[])a, (byte[])b);
				case TypeCode.Int16:
					return DataRowComparer.CompareEquatableArray<short>((short[])a, (short[])b);
				case TypeCode.UInt16:
				case TypeCode.UInt32:
					break;
				case TypeCode.Int32:
					return DataRowComparer.CompareEquatableArray<int>((int[])a, (int[])b);
				case TypeCode.Int64:
					return DataRowComparer.CompareEquatableArray<long>((long[])a, (long[])b);
				default:
					if (typeCode == TypeCode.String)
					{
						return DataRowComparer.CompareEquatableArray<string>((string[])a, (string[])b);
					}
					break;
				}
			}
			int num2 = i + a.Length;
			while (i < num2)
			{
				if (!DataRowComparer.AreElementEqual(a.GetValue(i), b.GetValue(num)))
				{
					return false;
				}
				i++;
				num++;
			}
			return true;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002398 File Offset: 0x00000598
		private static bool CompareEquatableArray<TElem>(TElem[] a, TElem[] b) where TElem : IEquatable<TElem>
		{
			for (int i = 0; i < a.Length; i++)
			{
				if (!a[i].Equals(b[i]))
				{
					return false;
				}
			}
			return true;
		}
	}
}
