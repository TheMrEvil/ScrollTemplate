﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;

namespace Microsoft.SqlServer.Server
{
	// Token: 0x02000026 RID: 38
	internal class MetaDataUtilsSmi
	{
		// Token: 0x0600016A RID: 362 RVA: 0x00005A0C File Offset: 0x00003C0C
		private static Dictionary<Type, ExtendedClrTypeCode> CreateTypeToExtendedTypeCodeMap()
		{
			return new Dictionary<Type, ExtendedClrTypeCode>(42)
			{
				{
					typeof(bool),
					ExtendedClrTypeCode.Boolean
				},
				{
					typeof(byte),
					ExtendedClrTypeCode.Byte
				},
				{
					typeof(char),
					ExtendedClrTypeCode.Char
				},
				{
					typeof(DateTime),
					ExtendedClrTypeCode.DateTime
				},
				{
					typeof(DBNull),
					ExtendedClrTypeCode.DBNull
				},
				{
					typeof(decimal),
					ExtendedClrTypeCode.Decimal
				},
				{
					typeof(double),
					ExtendedClrTypeCode.Double
				},
				{
					typeof(short),
					ExtendedClrTypeCode.Int16
				},
				{
					typeof(int),
					ExtendedClrTypeCode.Int32
				},
				{
					typeof(long),
					ExtendedClrTypeCode.Int64
				},
				{
					typeof(sbyte),
					ExtendedClrTypeCode.SByte
				},
				{
					typeof(float),
					ExtendedClrTypeCode.Single
				},
				{
					typeof(string),
					ExtendedClrTypeCode.String
				},
				{
					typeof(ushort),
					ExtendedClrTypeCode.UInt16
				},
				{
					typeof(uint),
					ExtendedClrTypeCode.UInt32
				},
				{
					typeof(ulong),
					ExtendedClrTypeCode.UInt64
				},
				{
					typeof(object),
					ExtendedClrTypeCode.Object
				},
				{
					typeof(byte[]),
					ExtendedClrTypeCode.ByteArray
				},
				{
					typeof(char[]),
					ExtendedClrTypeCode.CharArray
				},
				{
					typeof(Guid),
					ExtendedClrTypeCode.Guid
				},
				{
					typeof(SqlBinary),
					ExtendedClrTypeCode.SqlBinary
				},
				{
					typeof(SqlBoolean),
					ExtendedClrTypeCode.SqlBoolean
				},
				{
					typeof(SqlByte),
					ExtendedClrTypeCode.SqlByte
				},
				{
					typeof(SqlDateTime),
					ExtendedClrTypeCode.SqlDateTime
				},
				{
					typeof(SqlDouble),
					ExtendedClrTypeCode.SqlDouble
				},
				{
					typeof(SqlGuid),
					ExtendedClrTypeCode.SqlGuid
				},
				{
					typeof(SqlInt16),
					ExtendedClrTypeCode.SqlInt16
				},
				{
					typeof(SqlInt32),
					ExtendedClrTypeCode.SqlInt32
				},
				{
					typeof(SqlInt64),
					ExtendedClrTypeCode.SqlInt64
				},
				{
					typeof(SqlMoney),
					ExtendedClrTypeCode.SqlMoney
				},
				{
					typeof(SqlDecimal),
					ExtendedClrTypeCode.SqlDecimal
				},
				{
					typeof(SqlSingle),
					ExtendedClrTypeCode.SqlSingle
				},
				{
					typeof(SqlString),
					ExtendedClrTypeCode.SqlString
				},
				{
					typeof(SqlChars),
					ExtendedClrTypeCode.SqlChars
				},
				{
					typeof(SqlBytes),
					ExtendedClrTypeCode.SqlBytes
				},
				{
					typeof(SqlXml),
					ExtendedClrTypeCode.SqlXml
				},
				{
					typeof(DataTable),
					ExtendedClrTypeCode.DataTable
				},
				{
					typeof(DbDataReader),
					ExtendedClrTypeCode.DbDataReader
				},
				{
					typeof(IEnumerable<SqlDataRecord>),
					ExtendedClrTypeCode.IEnumerableOfSqlDataRecord
				},
				{
					typeof(TimeSpan),
					ExtendedClrTypeCode.TimeSpan
				},
				{
					typeof(DateTimeOffset),
					ExtendedClrTypeCode.DateTimeOffset
				}
			};
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00005CFA File Offset: 0x00003EFA
		internal static bool IsCharOrXmlType(SqlDbType type)
		{
			return MetaDataUtilsSmi.IsUnicodeType(type) || MetaDataUtilsSmi.IsAnsiType(type) || type == SqlDbType.Xml;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00005D13 File Offset: 0x00003F13
		internal static bool IsUnicodeType(SqlDbType type)
		{
			return type == SqlDbType.NChar || type == SqlDbType.NVarChar || type == SqlDbType.NText;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00005D26 File Offset: 0x00003F26
		internal static bool IsAnsiType(SqlDbType type)
		{
			return type == SqlDbType.Char || type == SqlDbType.VarChar || type == SqlDbType.Text;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00005D38 File Offset: 0x00003F38
		internal static bool IsBinaryType(SqlDbType type)
		{
			return type == SqlDbType.Binary || type == SqlDbType.VarBinary || type == SqlDbType.Image;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00005D49 File Offset: 0x00003F49
		internal static bool IsPlpFormat(SmiMetaData metaData)
		{
			return metaData.MaxLength == -1L || metaData.SqlDbType == SqlDbType.Image || metaData.SqlDbType == SqlDbType.NText || metaData.SqlDbType == SqlDbType.Text || metaData.SqlDbType == SqlDbType.Udt;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00005D80 File Offset: 0x00003F80
		internal static ExtendedClrTypeCode DetermineExtendedTypeCodeForUseWithSqlDbType(SqlDbType dbType, bool isMultiValued, object value, Type udtType)
		{
			ExtendedClrTypeCode extendedClrTypeCode = ExtendedClrTypeCode.Invalid;
			if (value == null)
			{
				extendedClrTypeCode = ExtendedClrTypeCode.Empty;
			}
			else if (DBNull.Value == value)
			{
				extendedClrTypeCode = ExtendedClrTypeCode.DBNull;
			}
			else
			{
				switch (dbType)
				{
				case SqlDbType.BigInt:
					if (value.GetType() == typeof(long))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Int64;
					}
					else if (value.GetType() == typeof(SqlInt64))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlInt64;
					}
					break;
				case SqlDbType.Binary:
				case SqlDbType.Image:
				case SqlDbType.Timestamp:
				case SqlDbType.VarBinary:
					if (value.GetType() == typeof(byte[]))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.ByteArray;
					}
					else if (value.GetType() == typeof(SqlBinary))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlBinary;
					}
					else if (value.GetType() == typeof(SqlBytes))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlBytes;
					}
					else if (value.GetType() == typeof(StreamDataFeed))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Stream;
					}
					break;
				case SqlDbType.Bit:
					if (value.GetType() == typeof(bool))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Boolean;
					}
					else if (value.GetType() == typeof(SqlBoolean))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlBoolean;
					}
					break;
				case SqlDbType.Char:
				case SqlDbType.NChar:
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
				case SqlDbType.Text:
				case SqlDbType.VarChar:
					if (value.GetType() == typeof(string))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.String;
					}
					if (value.GetType() == typeof(TextDataFeed))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.TextReader;
					}
					else if (value.GetType() == typeof(SqlString))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlString;
					}
					else if (value.GetType() == typeof(char[]))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.CharArray;
					}
					else if (value.GetType() == typeof(SqlChars))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlChars;
					}
					else if (value.GetType() == typeof(char))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Char;
					}
					break;
				case SqlDbType.DateTime:
				case SqlDbType.SmallDateTime:
				case SqlDbType.Date:
				case SqlDbType.DateTime2:
					if (value.GetType() == typeof(DateTime))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.DateTime;
					}
					else if (value.GetType() == typeof(SqlDateTime))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlDateTime;
					}
					break;
				case SqlDbType.Decimal:
					if (value.GetType() == typeof(decimal))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Decimal;
					}
					else if (value.GetType() == typeof(SqlDecimal))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlDecimal;
					}
					break;
				case SqlDbType.Float:
					if (value.GetType() == typeof(SqlDouble))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlDouble;
					}
					else if (value.GetType() == typeof(double))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Double;
					}
					break;
				case SqlDbType.Int:
					if (value.GetType() == typeof(int))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Int32;
					}
					else if (value.GetType() == typeof(SqlInt32))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlInt32;
					}
					break;
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					if (value.GetType() == typeof(SqlMoney))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlMoney;
					}
					else if (value.GetType() == typeof(decimal))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Decimal;
					}
					break;
				case SqlDbType.Real:
					if (value.GetType() == typeof(float))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Single;
					}
					else if (value.GetType() == typeof(SqlSingle))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlSingle;
					}
					break;
				case SqlDbType.UniqueIdentifier:
					if (value.GetType() == typeof(SqlGuid))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlGuid;
					}
					else if (value.GetType() == typeof(Guid))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Guid;
					}
					break;
				case SqlDbType.SmallInt:
					if (value.GetType() == typeof(short))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Int16;
					}
					else if (value.GetType() == typeof(SqlInt16))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlInt16;
					}
					break;
				case SqlDbType.TinyInt:
					if (value.GetType() == typeof(byte))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Byte;
					}
					else if (value.GetType() == typeof(SqlByte))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlByte;
					}
					break;
				case SqlDbType.Variant:
					extendedClrTypeCode = MetaDataUtilsSmi.DetermineExtendedTypeCode(value);
					if (ExtendedClrTypeCode.SqlXml == extendedClrTypeCode)
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Invalid;
					}
					break;
				case SqlDbType.Xml:
					if (value.GetType() == typeof(SqlXml))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.SqlXml;
					}
					if (value.GetType() == typeof(XmlDataFeed))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.XmlReader;
					}
					else if (value.GetType() == typeof(string))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.String;
					}
					break;
				case SqlDbType.Udt:
					if (null == udtType || value.GetType() == udtType)
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Object;
					}
					else
					{
						extendedClrTypeCode = ExtendedClrTypeCode.Invalid;
					}
					break;
				case SqlDbType.Structured:
					if (isMultiValued)
					{
						if (value is DataTable)
						{
							extendedClrTypeCode = ExtendedClrTypeCode.DataTable;
						}
						else if (value is IEnumerable<SqlDataRecord>)
						{
							extendedClrTypeCode = ExtendedClrTypeCode.IEnumerableOfSqlDataRecord;
						}
						else if (value is DbDataReader)
						{
							extendedClrTypeCode = ExtendedClrTypeCode.DbDataReader;
						}
					}
					break;
				case SqlDbType.Time:
					if (value.GetType() == typeof(TimeSpan))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.TimeSpan;
					}
					break;
				case SqlDbType.DateTimeOffset:
					if (value.GetType() == typeof(DateTimeOffset))
					{
						extendedClrTypeCode = ExtendedClrTypeCode.DateTimeOffset;
					}
					break;
				}
			}
			return extendedClrTypeCode;
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00006330 File Offset: 0x00004530
		internal static ExtendedClrTypeCode DetermineExtendedTypeCodeFromType(Type clrType)
		{
			ExtendedClrTypeCode result;
			if (!MetaDataUtilsSmi.s_typeToExtendedTypeCodeMap.TryGetValue(clrType, out result))
			{
				return ExtendedClrTypeCode.Invalid;
			}
			return result;
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000634F File Offset: 0x0000454F
		internal static ExtendedClrTypeCode DetermineExtendedTypeCode(object value)
		{
			if (value == null)
			{
				return ExtendedClrTypeCode.Empty;
			}
			return MetaDataUtilsSmi.DetermineExtendedTypeCodeFromType(value.GetType());
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00006361 File Offset: 0x00004561
		internal static SqlDbType InferSqlDbTypeFromTypeCode(ExtendedClrTypeCode typeCode)
		{
			return MetaDataUtilsSmi.s_extendedTypeCodeToSqlDbTypeMap[(int)(typeCode + 1)];
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000636C File Offset: 0x0000456C
		internal static SqlDbType InferSqlDbTypeFromType(Type type)
		{
			ExtendedClrTypeCode extendedClrTypeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeFromType(type);
			SqlDbType result;
			if (ExtendedClrTypeCode.Invalid == extendedClrTypeCode)
			{
				result = (SqlDbType)(-1);
			}
			else
			{
				result = MetaDataUtilsSmi.InferSqlDbTypeFromTypeCode(extendedClrTypeCode);
			}
			return result;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00006390 File Offset: 0x00004590
		internal static SqlDbType InferSqlDbTypeFromType_Katmai(Type type)
		{
			SqlDbType sqlDbType = MetaDataUtilsSmi.InferSqlDbTypeFromType(type);
			if (SqlDbType.DateTime == sqlDbType)
			{
				sqlDbType = SqlDbType.DateTime2;
			}
			return sqlDbType;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x000063AC File Offset: 0x000045AC
		internal static SqlMetaData SmiExtendedMetaDataToSqlMetaData(SmiExtendedMetaData source)
		{
			if (SqlDbType.Xml == source.SqlDbType)
			{
				return new SqlMetaData(source.Name, source.SqlDbType, source.MaxLength, source.Precision, source.Scale, source.LocaleId, source.CompareOptions, source.TypeSpecificNamePart1, source.TypeSpecificNamePart2, source.TypeSpecificNamePart3, true, source.Type);
			}
			return new SqlMetaData(source.Name, source.SqlDbType, source.MaxLength, source.Precision, source.Scale, source.LocaleId, source.CompareOptions, null);
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000643C File Offset: 0x0000463C
		internal static SmiExtendedMetaData SqlMetaDataToSmiExtendedMetaData(SqlMetaData source)
		{
			string text = null;
			string text2 = null;
			string text3 = null;
			if (SqlDbType.Xml == source.SqlDbType)
			{
				text = source.XmlSchemaCollectionDatabase;
				text2 = source.XmlSchemaCollectionOwningSchema;
				text3 = source.XmlSchemaCollectionName;
			}
			else if (SqlDbType.Udt == source.SqlDbType)
			{
				string serverTypeName = source.ServerTypeName;
				if (serverTypeName != null)
				{
					string[] array = SqlParameter.ParseTypeName(serverTypeName, true);
					if (1 == array.Length)
					{
						text3 = array[0];
					}
					else if (2 == array.Length)
					{
						text2 = array[0];
						text3 = array[1];
					}
					else
					{
						if (3 != array.Length)
						{
							throw ADP.ArgumentOutOfRange("typeName");
						}
						text = array[0];
						text2 = array[1];
						text3 = array[2];
					}
					if ((!string.IsNullOrEmpty(text) && 255 < text.Length) || (!string.IsNullOrEmpty(text2) && 255 < text2.Length) || (!string.IsNullOrEmpty(text3) && 255 < text3.Length))
					{
						throw ADP.ArgumentOutOfRange("typeName");
					}
				}
			}
			return new SmiExtendedMetaData(source.SqlDbType, source.MaxLength, source.Precision, source.Scale, source.LocaleId, source.CompareOptions, null, source.Name, text, text2, text3);
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00006558 File Offset: 0x00004758
		internal static bool IsCompatible(SmiMetaData firstMd, SqlMetaData secondMd)
		{
			return firstMd.SqlDbType == secondMd.SqlDbType && firstMd.MaxLength == secondMd.MaxLength && firstMd.Precision == secondMd.Precision && firstMd.Scale == secondMd.Scale && firstMd.CompareOptions == secondMd.CompareOptions && firstMd.LocaleId == secondMd.LocaleId && firstMd.SqlDbType != SqlDbType.Structured && !firstMd.IsMultiValued;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x000065D0 File Offset: 0x000047D0
		internal static SmiExtendedMetaData SmiMetaDataFromDataColumn(DataColumn column, DataTable parent)
		{
			SqlDbType sqlDbType = MetaDataUtilsSmi.InferSqlDbTypeFromType_Katmai(column.DataType);
			if ((SqlDbType)(-1) == sqlDbType)
			{
				throw SQL.UnsupportedColumnTypeForSqlProvider(column.ColumnName, column.DataType.Name);
			}
			long num = MetaDataUtilsSmi.AdjustMaxLength(sqlDbType, (long)column.MaxLength);
			if (-2L == num)
			{
				throw SQL.InvalidColumnMaxLength(column.ColumnName, num);
			}
			byte b;
			byte b4;
			CultureInfo cultureInfo;
			checked
			{
				if (column.DataType == typeof(SqlDecimal))
				{
					b = 0;
					byte b2 = 0;
					foreach (object obj in parent.Rows)
					{
						object obj2 = ((DataRow)obj)[column];
						if (!(obj2 is DBNull))
						{
							SqlDecimal sqlDecimal = (SqlDecimal)obj2;
							if (!sqlDecimal.IsNull)
							{
								byte b3 = sqlDecimal.Precision - sqlDecimal.Scale;
								if (b3 > b2)
								{
									b2 = b3;
								}
								if (sqlDecimal.Scale > b)
								{
									b = sqlDecimal.Scale;
								}
							}
						}
					}
					b4 = b2 + b;
					if (SqlDecimal.MaxPrecision < b4)
					{
						throw SQL.InvalidTableDerivedPrecisionForTvp(column.ColumnName, b4);
					}
					if (b4 == 0)
					{
						b4 = 1;
					}
				}
				else if (sqlDbType == SqlDbType.DateTime2 || sqlDbType == SqlDbType.DateTimeOffset || sqlDbType == SqlDbType.Time)
				{
					b4 = 0;
					b = SmiMetaData.DefaultTime.Scale;
				}
				else if (sqlDbType == SqlDbType.Decimal)
				{
					b = 0;
					byte b5 = 0;
					foreach (object obj3 in parent.Rows)
					{
						object obj4 = ((DataRow)obj3)[column];
						if (!(obj4 is DBNull))
						{
							SqlDecimal sqlDecimal2 = (decimal)obj4;
							byte b6 = sqlDecimal2.Precision - sqlDecimal2.Scale;
							if (b6 > b5)
							{
								b5 = b6;
							}
							if (sqlDecimal2.Scale > b)
							{
								b = sqlDecimal2.Scale;
							}
						}
					}
					b4 = b5 + b;
					if (SqlDecimal.MaxPrecision < b4)
					{
						throw SQL.InvalidTableDerivedPrecisionForTvp(column.ColumnName, b4);
					}
					if (b4 == 0)
					{
						b4 = 1;
					}
				}
				else
				{
					b4 = 0;
					b = 0;
				}
				cultureInfo = ((parent != null) ? parent.Locale : CultureInfo.CurrentCulture);
			}
			return new SmiExtendedMetaData(sqlDbType, num, b4, b, (long)cultureInfo.LCID, SmiMetaData.DefaultNVarChar.CompareOptions, null, false, null, null, column.ColumnName, null, null, null);
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000682C File Offset: 0x00004A2C
		internal static long AdjustMaxLength(SqlDbType dbType, long maxLength)
		{
			if (-1L != maxLength)
			{
				if (maxLength < 0L)
				{
					maxLength = -2L;
				}
				if (dbType <= SqlDbType.NChar)
				{
					if (dbType != SqlDbType.Binary)
					{
						if (dbType != SqlDbType.Char)
						{
							if (dbType == SqlDbType.NChar)
							{
								if (maxLength > 4000L)
								{
									maxLength = -2L;
								}
							}
						}
						else if (maxLength > 8000L)
						{
							maxLength = -2L;
						}
					}
					else if (maxLength > 8000L)
					{
						maxLength = -2L;
					}
				}
				else if (dbType != SqlDbType.NVarChar)
				{
					if (dbType != SqlDbType.VarBinary)
					{
						if (dbType == SqlDbType.VarChar)
						{
							if (8000L < maxLength)
							{
								maxLength = -1L;
							}
						}
					}
					else if (8000L < maxLength)
					{
						maxLength = -1L;
					}
				}
				else if (4000L < maxLength)
				{
					maxLength = -1L;
				}
			}
			return maxLength;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x000068CC File Offset: 0x00004ACC
		internal static SmiExtendedMetaData SmiMetaDataFromSchemaTableRow(DataRow schemaRow)
		{
			string text = "";
			object obj = schemaRow[SchemaTableColumn.ColumnName];
			if (DBNull.Value != obj)
			{
				text = (string)obj;
			}
			obj = schemaRow[SchemaTableColumn.DataType];
			if (DBNull.Value == obj)
			{
				throw SQL.NullSchemaTableDataTypeNotSupported(text);
			}
			Type type = (Type)obj;
			SqlDbType sqlDbType = MetaDataUtilsSmi.InferSqlDbTypeFromType_Katmai(type);
			if ((SqlDbType)(-1) == sqlDbType)
			{
				if (!(typeof(object) == type))
				{
					throw SQL.UnsupportedColumnTypeForSqlProvider(text, type.ToString());
				}
				sqlDbType = SqlDbType.VarBinary;
			}
			long num = 0L;
			byte b = 0;
			byte b2 = 0;
			switch (sqlDbType)
			{
			case SqlDbType.BigInt:
			case SqlDbType.Bit:
			case SqlDbType.DateTime:
			case SqlDbType.Float:
			case SqlDbType.Image:
			case SqlDbType.Int:
			case SqlDbType.Money:
			case SqlDbType.NText:
			case SqlDbType.Real:
			case SqlDbType.UniqueIdentifier:
			case SqlDbType.SmallDateTime:
			case SqlDbType.SmallInt:
			case SqlDbType.SmallMoney:
			case SqlDbType.Text:
			case SqlDbType.Timestamp:
			case SqlDbType.TinyInt:
			case SqlDbType.Variant:
			case SqlDbType.Xml:
			case SqlDbType.Date:
				goto IL_315;
			case SqlDbType.Binary:
			case SqlDbType.VarBinary:
				obj = schemaRow[SchemaTableColumn.ColumnSize];
				if (DBNull.Value == obj)
				{
					if (SqlDbType.Binary == sqlDbType)
					{
						num = 8000L;
						goto IL_315;
					}
					num = -1L;
					goto IL_315;
				}
				else
				{
					num = Convert.ToInt64(obj, null);
					if (num > 8000L)
					{
						num = -1L;
					}
					if (num < 0L && (num != -1L || SqlDbType.Binary == sqlDbType))
					{
						throw SQL.InvalidColumnMaxLength(text, num);
					}
					goto IL_315;
				}
				break;
			case SqlDbType.Char:
			case SqlDbType.VarChar:
				obj = schemaRow[SchemaTableColumn.ColumnSize];
				if (DBNull.Value == obj)
				{
					if (SqlDbType.Char == sqlDbType)
					{
						num = 8000L;
						goto IL_315;
					}
					num = -1L;
					goto IL_315;
				}
				else
				{
					num = Convert.ToInt64(obj, null);
					if (num > 8000L)
					{
						num = -1L;
					}
					if (num < 0L && (num != -1L || SqlDbType.Char == sqlDbType))
					{
						throw SQL.InvalidColumnMaxLength(text, num);
					}
					goto IL_315;
				}
				break;
			case SqlDbType.Decimal:
				obj = schemaRow[SchemaTableColumn.NumericPrecision];
				if (DBNull.Value == obj)
				{
					b = SmiMetaData.DefaultDecimal.Precision;
				}
				else
				{
					b = Convert.ToByte(obj, null);
				}
				obj = schemaRow[SchemaTableColumn.NumericScale];
				if (DBNull.Value == obj)
				{
					b2 = SmiMetaData.DefaultDecimal.Scale;
				}
				else
				{
					b2 = Convert.ToByte(obj, null);
				}
				if (b < 1 || b > SqlDecimal.MaxPrecision || b2 < 0 || b2 > SqlDecimal.MaxScale || b2 > b)
				{
					throw SQL.InvalidColumnPrecScale();
				}
				goto IL_315;
			case SqlDbType.NChar:
			case SqlDbType.NVarChar:
				obj = schemaRow[SchemaTableColumn.ColumnSize];
				if (DBNull.Value == obj)
				{
					if (SqlDbType.NChar == sqlDbType)
					{
						num = 4000L;
						goto IL_315;
					}
					num = -1L;
					goto IL_315;
				}
				else
				{
					num = Convert.ToInt64(obj, null);
					if (num > 4000L)
					{
						num = -1L;
					}
					if (num < 0L && (num != -1L || SqlDbType.NChar == sqlDbType))
					{
						throw SQL.InvalidColumnMaxLength(text, num);
					}
					goto IL_315;
				}
				break;
			case SqlDbType.Time:
			case SqlDbType.DateTime2:
			case SqlDbType.DateTimeOffset:
				obj = schemaRow[SchemaTableColumn.NumericScale];
				if (DBNull.Value == obj)
				{
					b2 = SmiMetaData.DefaultTime.Scale;
				}
				else
				{
					b2 = Convert.ToByte(obj, null);
				}
				if (b2 > 7)
				{
					throw SQL.InvalidColumnPrecScale();
				}
				if (b2 < 0)
				{
					b2 = SmiMetaData.DefaultTime.Scale;
					goto IL_315;
				}
				goto IL_315;
			}
			throw SQL.UnsupportedColumnTypeForSqlProvider(text, type.ToString());
			IL_315:
			return new SmiExtendedMetaData(sqlDbType, num, b, b2, (long)CultureInfo.CurrentCulture.LCID, SmiMetaData.GetDefaultForType(sqlDbType).CompareOptions, null, false, null, null, text, null, null, null);
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00003D93 File Offset: 0x00001F93
		public MetaDataUtilsSmi()
		{
		}

		// Token: 0x0600017D RID: 381 RVA: 0x00006C18 File Offset: 0x00004E18
		// Note: this type is marked as 'beforefieldinit'.
		static MetaDataUtilsSmi()
		{
		}

		// Token: 0x0400044B RID: 1099
		internal const SqlDbType InvalidSqlDbType = (SqlDbType)(-1);

		// Token: 0x0400044C RID: 1100
		internal const long InvalidMaxLength = -2L;

		// Token: 0x0400044D RID: 1101
		private static readonly SqlDbType[] s_extendedTypeCodeToSqlDbTypeMap = new SqlDbType[]
		{
			(SqlDbType)(-1),
			SqlDbType.Bit,
			SqlDbType.TinyInt,
			SqlDbType.NVarChar,
			SqlDbType.DateTime,
			(SqlDbType)(-1),
			SqlDbType.Decimal,
			SqlDbType.Float,
			(SqlDbType)(-1),
			SqlDbType.SmallInt,
			SqlDbType.Int,
			SqlDbType.BigInt,
			(SqlDbType)(-1),
			SqlDbType.Real,
			SqlDbType.NVarChar,
			(SqlDbType)(-1),
			(SqlDbType)(-1),
			(SqlDbType)(-1),
			(SqlDbType)(-1),
			SqlDbType.VarBinary,
			SqlDbType.NVarChar,
			SqlDbType.UniqueIdentifier,
			SqlDbType.VarBinary,
			SqlDbType.Bit,
			SqlDbType.TinyInt,
			SqlDbType.DateTime,
			SqlDbType.Float,
			SqlDbType.UniqueIdentifier,
			SqlDbType.SmallInt,
			SqlDbType.Int,
			SqlDbType.BigInt,
			SqlDbType.Money,
			SqlDbType.Decimal,
			SqlDbType.Real,
			SqlDbType.NVarChar,
			SqlDbType.NVarChar,
			SqlDbType.VarBinary,
			SqlDbType.Xml,
			SqlDbType.Structured,
			SqlDbType.Structured,
			SqlDbType.Structured,
			SqlDbType.Time,
			SqlDbType.DateTimeOffset
		};

		// Token: 0x0400044E RID: 1102
		private static readonly Dictionary<Type, ExtendedClrTypeCode> s_typeToExtendedTypeCodeMap = MetaDataUtilsSmi.CreateTypeToExtendedTypeCodeMap();
	}
}
