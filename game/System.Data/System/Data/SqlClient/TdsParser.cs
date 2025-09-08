﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Sql;
using System.Data.SqlClient.SNI;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
	// Token: 0x02000247 RID: 583
	internal sealed class TdsParser
	{
		// Token: 0x06001BD6 RID: 7126 RVA: 0x0007DDBC File Offset: 0x0007BFBC
		internal void PostReadAsyncForMars()
		{
			if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				return;
			}
			IntPtr intPtr = IntPtr.Zero;
			uint num = 0U;
			this._pMarsPhysicalConObj.IncrementPendingCallbacks();
			object sessionHandle = this._pMarsPhysicalConObj.SessionHandle;
			intPtr = (IntPtr)this._pMarsPhysicalConObj.ReadAsync(out num, ref sessionHandle);
			if (intPtr != IntPtr.Zero)
			{
				this._pMarsPhysicalConObj.ReleasePacket(intPtr);
			}
			if (997U != num)
			{
				this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
		}

		// Token: 0x06001BD7 RID: 7127 RVA: 0x0007DE54 File Offset: 0x0007C054
		private void LoadSSPILibrary()
		{
			if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				return;
			}
			if (!TdsParser.s_fSSPILoaded)
			{
				object obj = TdsParser.s_tdsParserLock;
				lock (obj)
				{
					if (!TdsParser.s_fSSPILoaded)
					{
						uint num = 0U;
						if (SNINativeMethodWrapper.SNISecInitPackage(ref num) != 0U)
						{
							this.SSPIError(SQLMessage.SSPIInitializeError(), "InitSSPIPackage");
						}
						TdsParser.s_maxSSPILength = num;
						TdsParser.s_fSSPILoaded = true;
					}
				}
			}
			if (TdsParser.s_maxSSPILength > 2147483647U)
			{
				throw SQL.InvalidSSPIPacketSize();
			}
		}

		// Token: 0x06001BD8 RID: 7128 RVA: 0x0007DEE8 File Offset: 0x0007C0E8
		private void WaitForSSLHandShakeToComplete(ref uint error)
		{
			if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				return;
			}
			error = this._physicalStateObj.WaitForSSLHandShakeToComplete();
			if (error != 0U)
			{
				this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
		}

		// Token: 0x06001BD9 RID: 7129 RVA: 0x0007DF28 File Offset: 0x0007C128
		private SNIErrorDetails GetSniErrorDetails()
		{
			SNIErrorDetails result = default(SNIErrorDetails);
			if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				SNIError lastError = SNIProxy.Singleton.GetLastError();
				result.sniErrorNumber = lastError.sniError;
				result.errorMessage = lastError.errorMessage;
				result.nativeError = lastError.nativeError;
				result.provider = (int)lastError.provider;
				result.lineNumber = lastError.lineNumber;
				result.function = lastError.function;
				result.exception = lastError.exception;
			}
			else
			{
				SNINativeMethodWrapper.SNI_Error sni_Error;
				SNINativeMethodWrapper.SNIGetLastError(out sni_Error);
				result.sniErrorNumber = sni_Error.sniError;
				result.errorMessage = sni_Error.errorMessage;
				result.nativeError = sni_Error.nativeError;
				result.provider = (int)sni_Error.provider;
				result.lineNumber = sni_Error.lineNumber;
				result.function = sni_Error.function;
			}
			return result;
		}

		// Token: 0x06001BDA RID: 7130 RVA: 0x0007E002 File Offset: 0x0007C202
		internal TdsParser(bool MARS, bool fAsynchronous)
		{
			this._fMARS = MARS;
			this._physicalStateObj = TdsParserStateObjectFactory.Singleton.CreateTdsParserStateObject(this);
		}

		// Token: 0x17000521 RID: 1313
		// (get) Token: 0x06001BDB RID: 7131 RVA: 0x0007E038 File Offset: 0x0007C238
		internal SqlInternalConnectionTds Connection
		{
			get
			{
				return this._connHandler;
			}
		}

		// Token: 0x17000522 RID: 1314
		// (get) Token: 0x06001BDC RID: 7132 RVA: 0x0007E040 File Offset: 0x0007C240
		// (set) Token: 0x06001BDD RID: 7133 RVA: 0x0007E048 File Offset: 0x0007C248
		internal SqlInternalTransaction CurrentTransaction
		{
			get
			{
				return this._currentTransaction;
			}
			set
			{
				if ((this._currentTransaction == null && value != null) || (this._currentTransaction != null && value == null))
				{
					this._currentTransaction = value;
				}
			}
		}

		// Token: 0x17000523 RID: 1315
		// (get) Token: 0x06001BDE RID: 7134 RVA: 0x0007E067 File Offset: 0x0007C267
		internal int DefaultLCID
		{
			get
			{
				return this._defaultLCID;
			}
		}

		// Token: 0x17000524 RID: 1316
		// (get) Token: 0x06001BDF RID: 7135 RVA: 0x0007E06F File Offset: 0x0007C26F
		// (set) Token: 0x06001BE0 RID: 7136 RVA: 0x0007E077 File Offset: 0x0007C277
		internal EncryptionOptions EncryptionOptions
		{
			get
			{
				return this._encryptionOption;
			}
			set
			{
				this._encryptionOption = value;
			}
		}

		// Token: 0x17000525 RID: 1317
		// (get) Token: 0x06001BE1 RID: 7137 RVA: 0x0007E080 File Offset: 0x0007C280
		internal bool IsKatmaiOrNewer
		{
			get
			{
				return this._isKatmai;
			}
		}

		// Token: 0x17000526 RID: 1318
		// (get) Token: 0x06001BE2 RID: 7138 RVA: 0x0007E088 File Offset: 0x0007C288
		internal bool MARSOn
		{
			get
			{
				return this._fMARS;
			}
		}

		// Token: 0x17000527 RID: 1319
		// (get) Token: 0x06001BE3 RID: 7139 RVA: 0x0007E090 File Offset: 0x0007C290
		// (set) Token: 0x06001BE4 RID: 7140 RVA: 0x0007E098 File Offset: 0x0007C298
		internal SqlInternalTransaction PendingTransaction
		{
			get
			{
				return this._pendingTransaction;
			}
			set
			{
				this._pendingTransaction = value;
			}
		}

		// Token: 0x17000528 RID: 1320
		// (get) Token: 0x06001BE5 RID: 7141 RVA: 0x0007E0A1 File Offset: 0x0007C2A1
		internal string Server
		{
			get
			{
				return this._server;
			}
		}

		// Token: 0x17000529 RID: 1321
		// (get) Token: 0x06001BE6 RID: 7142 RVA: 0x0007E0A9 File Offset: 0x0007C2A9
		// (set) Token: 0x06001BE7 RID: 7143 RVA: 0x0007E0B1 File Offset: 0x0007C2B1
		internal TdsParserState State
		{
			get
			{
				return this._state;
			}
			set
			{
				this._state = value;
			}
		}

		// Token: 0x1700052A RID: 1322
		// (get) Token: 0x06001BE8 RID: 7144 RVA: 0x0007E0BA File Offset: 0x0007C2BA
		// (set) Token: 0x06001BE9 RID: 7145 RVA: 0x0007E0C2 File Offset: 0x0007C2C2
		internal SqlStatistics Statistics
		{
			get
			{
				return this._statistics;
			}
			set
			{
				this._statistics = value;
			}
		}

		// Token: 0x06001BEA RID: 7146 RVA: 0x0007E0CB File Offset: 0x0007C2CB
		internal int IncrementNonTransactedOpenResultCount()
		{
			return Interlocked.Increment(ref this._nonTransactedOpenResultCount);
		}

		// Token: 0x06001BEB RID: 7147 RVA: 0x0007E0D8 File Offset: 0x0007C2D8
		internal void DecrementNonTransactedOpenResultCount()
		{
			Interlocked.Decrement(ref this._nonTransactedOpenResultCount);
		}

		// Token: 0x06001BEC RID: 7148 RVA: 0x0007E0E6 File Offset: 0x0007C2E6
		internal void ProcessPendingAck(TdsParserStateObject stateObj)
		{
			if (stateObj._attentionSent)
			{
				this.ProcessAttention(stateObj);
			}
		}

		// Token: 0x06001BED RID: 7149 RVA: 0x0007E0FC File Offset: 0x0007C2FC
		internal void Connect(ServerInfo serverInfo, SqlInternalConnectionTds connHandler, bool ignoreSniOpenTimeout, long timerExpire, bool encrypt, bool trustServerCert, bool integratedSecurity, bool withFailover)
		{
			if (this._state != TdsParserState.Closed)
			{
				return;
			}
			this._connHandler = connHandler;
			this._loginWithFailover = withFailover;
			if (TdsParserStateObjectFactory.Singleton.SNIStatus != 0U)
			{
				this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
				this._physicalStateObj.Dispose();
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
			this._sniSpnBuffer = null;
			if (integratedSecurity)
			{
				this.LoadSSPILibrary();
			}
			byte[] instanceName = null;
			this._connHandler.TimeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.PreLoginBegin);
			this._connHandler.TimeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.InitializeConnection);
			bool multiSubnetFailover = this._connHandler.ConnectionOptions.MultiSubnetFailover;
			this._physicalStateObj.CreatePhysicalSNIHandle(serverInfo.ExtendedServerName, ignoreSniOpenTimeout, timerExpire, out instanceName, ref this._sniSpnBuffer, false, true, multiSubnetFailover, integratedSecurity);
			if (this._physicalStateObj.Status != 0U)
			{
				this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
				this._physicalStateObj.Dispose();
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
			this._server = serverInfo.ResolvedServerName;
			if (connHandler.PoolGroupProviderInfo != null)
			{
				connHandler.PoolGroupProviderInfo.AliasCheck((serverInfo.PreRoutingServerName == null) ? serverInfo.ResolvedServerName : serverInfo.PreRoutingServerName);
			}
			this._state = TdsParserState.OpenNotLoggedIn;
			this._physicalStateObj.SniContext = SniContext.Snix_PreLoginBeforeSuccessfulWrite;
			this._physicalStateObj.TimeoutTime = timerExpire;
			bool flag = false;
			this._connHandler.TimeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.InitializeConnection);
			this._connHandler.TimeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.SendPreLoginHandshake);
			this._physicalStateObj.SniGetConnectionId(ref this._connHandler._clientConnectionId);
			this.SendPreLoginHandshake(instanceName, encrypt);
			this._connHandler.TimeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.SendPreLoginHandshake);
			this._connHandler.TimeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.ConsumePreLoginHandshake);
			this._physicalStateObj.SniContext = SniContext.Snix_PreLogin;
			if (this.ConsumePreLoginHandshake(encrypt, trustServerCert, integratedSecurity, out flag, out this._connHandler._fedAuthRequired) == PreLoginHandshakeStatus.InstanceFailure)
			{
				this._physicalStateObj.Dispose();
				this._physicalStateObj.SniContext = SniContext.Snix_Connect;
				this._physicalStateObj.CreatePhysicalSNIHandle(serverInfo.ExtendedServerName, ignoreSniOpenTimeout, timerExpire, out instanceName, ref this._sniSpnBuffer, true, true, multiSubnetFailover, integratedSecurity);
				if (this._physicalStateObj.Status != 0U)
				{
					this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
					this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
				}
				this._physicalStateObj.SniGetConnectionId(ref this._connHandler._clientConnectionId);
				this.SendPreLoginHandshake(instanceName, encrypt);
				if (this.ConsumePreLoginHandshake(encrypt, trustServerCert, integratedSecurity, out flag, out this._connHandler._fedAuthRequired) == PreLoginHandshakeStatus.InstanceFailure)
				{
					throw SQL.InstanceFailure();
				}
			}
			if (this._fMARS && flag)
			{
				this._sessionPool = new TdsParserSessionPool(this);
				return;
			}
			this._fMARS = false;
		}

		// Token: 0x06001BEE RID: 7150 RVA: 0x0007E3B1 File Offset: 0x0007C5B1
		internal void RemoveEncryption()
		{
			if (this._physicalStateObj.DisabeSsl() != 0U)
			{
				this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
			this._physicalStateObj.ClearAllWritePackets();
		}

		// Token: 0x06001BEF RID: 7151 RVA: 0x0007E3F0 File Offset: 0x0007C5F0
		internal void EnableMars()
		{
			if (this._fMARS)
			{
				this._pMarsPhysicalConObj = this._physicalStateObj;
				if (TdsParserStateObjectFactory.UseManagedSNI)
				{
					this._pMarsPhysicalConObj.IncrementPendingCallbacks();
				}
				uint num = 0U;
				if (this._pMarsPhysicalConObj.EnableMars(ref num) != 0U)
				{
					this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
					this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
				}
				this.PostReadAsyncForMars();
				this._physicalStateObj = this.CreateSession();
			}
		}

		// Token: 0x06001BF0 RID: 7152 RVA: 0x0007E46C File Offset: 0x0007C66C
		internal TdsParserStateObject CreateSession()
		{
			return TdsParserStateObjectFactory.Singleton.CreateSessionObject(this, this._pMarsPhysicalConObj, true);
		}

		// Token: 0x06001BF1 RID: 7153 RVA: 0x0007E480 File Offset: 0x0007C680
		internal TdsParserStateObject GetSession(object owner)
		{
			TdsParserStateObject result;
			if (this.MARSOn)
			{
				result = this._sessionPool.GetSession(owner);
			}
			else
			{
				result = this._physicalStateObj;
			}
			return result;
		}

		// Token: 0x06001BF2 RID: 7154 RVA: 0x0007E4B0 File Offset: 0x0007C6B0
		internal void PutSession(TdsParserStateObject session)
		{
			if (this.MARSOn)
			{
				this._sessionPool.PutSession(session);
				return;
			}
			if (this._state == TdsParserState.Closed || this._state == TdsParserState.Broken)
			{
				this._physicalStateObj.SniContext = SniContext.Snix_Close;
				this._physicalStateObj.Dispose();
				return;
			}
			this._physicalStateObj.Owner = null;
		}

		// Token: 0x06001BF3 RID: 7155 RVA: 0x0007E508 File Offset: 0x0007C708
		private void SendPreLoginHandshake(byte[] instanceName, bool encrypt)
		{
			this._physicalStateObj._outputMessageType = 18;
			int num = 36;
			byte[] array = new byte[1059];
			int num2 = 0;
			for (int i = 0; i < 7; i++)
			{
				int num3 = 0;
				this._physicalStateObj.WriteByte((byte)i);
				this._physicalStateObj.WriteByte((byte)((num & 65280) >> 8));
				this._physicalStateObj.WriteByte((byte)(num & 255));
				switch (i)
				{
				case 0:
				{
					Version assemblyVersion = ADP.GetAssemblyVersion();
					array[num2++] = (byte)(assemblyVersion.Major & 255);
					array[num2++] = (byte)(assemblyVersion.Minor & 255);
					array[num2++] = (byte)((assemblyVersion.Build & 65280) >> 8);
					array[num2++] = (byte)(assemblyVersion.Build & 255);
					array[num2++] = (byte)(assemblyVersion.Revision & 255);
					array[num2++] = (byte)((assemblyVersion.Revision & 65280) >> 8);
					num += 6;
					num3 = 6;
					break;
				}
				case 1:
					if (this._encryptionOption == EncryptionOptions.NOT_SUP)
					{
						array[num2] = 2;
					}
					else if (encrypt)
					{
						array[num2] = 1;
						this._encryptionOption = EncryptionOptions.ON;
					}
					else
					{
						array[num2] = 0;
						this._encryptionOption = EncryptionOptions.OFF;
					}
					num2++;
					num++;
					num3 = 1;
					break;
				case 2:
				{
					int num4 = 0;
					while (instanceName[num4] != 0)
					{
						array[num2] = instanceName[num4];
						num2++;
						num4++;
					}
					array[num2] = 0;
					num2++;
					num4++;
					num += num4;
					num3 = num4;
					break;
				}
				case 3:
				{
					int currentThreadIdForTdsLoginOnly = TdsParserStaticMethods.GetCurrentThreadIdForTdsLoginOnly();
					array[num2++] = (byte)(((ulong)-16777216 & (ulong)((long)currentThreadIdForTdsLoginOnly)) >> 24);
					array[num2++] = (byte)((16711680 & currentThreadIdForTdsLoginOnly) >> 16);
					array[num2++] = (byte)((65280 & currentThreadIdForTdsLoginOnly) >> 8);
					array[num2++] = (byte)(255 & currentThreadIdForTdsLoginOnly);
					num += 4;
					num3 = 4;
					break;
				}
				case 4:
					array[num2++] = (this._fMARS ? 1 : 0);
					num++;
					num3++;
					break;
				case 5:
				{
					Buffer.BlockCopy(this._connHandler._clientConnectionId.ToByteArray(), 0, array, num2, 16);
					num2 += 16;
					num += 16;
					num3 = 16;
					ActivityCorrelator.ActivityId activityId = ActivityCorrelator.Next();
					Buffer.BlockCopy(activityId.Id.ToByteArray(), 0, array, num2, 16);
					num2 += 16;
					array[num2++] = (byte)(255U & activityId.Sequence);
					array[num2++] = (byte)((65280U & activityId.Sequence) >> 8);
					array[num2++] = (byte)((16711680U & activityId.Sequence) >> 16);
					array[num2++] = (byte)((4278190080U & activityId.Sequence) >> 24);
					int num5 = 20;
					num += num5;
					num3 += num5;
					break;
				}
				case 6:
					array[num2++] = 1;
					num++;
					num3++;
					break;
				}
				this._physicalStateObj.WriteByte((byte)((num3 & 65280) >> 8));
				this._physicalStateObj.WriteByte((byte)(num3 & 255));
			}
			this._physicalStateObj.WriteByte(byte.MaxValue);
			this._physicalStateObj.WriteByteArray(array, num2, 0, true, null);
			this._physicalStateObj.WritePacket(1, false);
		}

		// Token: 0x06001BF4 RID: 7156 RVA: 0x0007E854 File Offset: 0x0007CA54
		private PreLoginHandshakeStatus ConsumePreLoginHandshake(bool encrypt, bool trustServerCert, bool integratedSecurity, out bool marsCapable, out bool fedAuthRequired)
		{
			marsCapable = this._fMARS;
			fedAuthRequired = false;
			bool flag = false;
			if (!this._physicalStateObj.TryReadNetworkPacket())
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			if (this._physicalStateObj._inBytesRead == 0)
			{
				this._physicalStateObj.AddError(new SqlError(0, 0, 20, this._server, SQLMessage.PreloginError(), "", 0, null));
				this._physicalStateObj.Dispose();
				this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
			}
			if (!this._physicalStateObj.TryProcessHeader())
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			if (this._physicalStateObj._inBytesPacket > 32768 || this._physicalStateObj._inBytesPacket <= 0)
			{
				throw SQL.ParsingError();
			}
			byte[] array = new byte[this._physicalStateObj._inBytesPacket];
			if (!this._physicalStateObj.TryReadByteArray(array, 0, array.Length))
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			if (array[0] == 170)
			{
				throw SQL.InvalidSQLServerVersionUnknown();
			}
			int num = 0;
			for (int num2 = (int)array[num++]; num2 != 255; num2 = (int)array[num++])
			{
				switch (num2)
				{
				case 0:
				{
					int num3 = (int)array[num++] << 8 | (int)array[num++];
					byte b = array[num++];
					byte b2 = array[num++];
					int num4 = (int)array[num3];
					byte b3 = array[num3 + 1];
					byte b4 = array[num3 + 2];
					byte b5 = array[num3 + 3];
					flag = (num4 >= 9);
					if (!flag)
					{
						marsCapable = false;
					}
					break;
				}
				case 1:
				{
					int num3 = (int)array[num++] << 8 | (int)array[num++];
					byte b6 = array[num++];
					byte b7 = array[num++];
					EncryptionOptions encryptionOptions = (EncryptionOptions)array[num3];
					switch (this._encryptionOption)
					{
					case EncryptionOptions.OFF:
						if (encryptionOptions == EncryptionOptions.OFF)
						{
							this._encryptionOption = EncryptionOptions.LOGIN;
						}
						else if (encryptionOptions == EncryptionOptions.REQ)
						{
							this._encryptionOption = EncryptionOptions.ON;
						}
						break;
					case EncryptionOptions.ON:
						if (encryptionOptions == EncryptionOptions.NOT_SUP)
						{
							this._physicalStateObj.AddError(new SqlError(20, 0, 20, this._server, SQLMessage.EncryptionNotSupportedByServer(), "", 0, null));
							this._physicalStateObj.Dispose();
							this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
						}
						break;
					case EncryptionOptions.NOT_SUP:
						if (encryptionOptions == EncryptionOptions.REQ)
						{
							this._physicalStateObj.AddError(new SqlError(20, 0, 20, this._server, SQLMessage.EncryptionNotSupportedByClient(), "", 0, null));
							this._physicalStateObj.Dispose();
							this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
						}
						break;
					}
					if (this._encryptionOption == EncryptionOptions.ON || this._encryptionOption == EncryptionOptions.LOGIN)
					{
						uint num5 = 0U;
						uint num6 = (((encrypt && !trustServerCert) || (this._connHandler._accessTokenInBytes != null && !trustServerCert)) ? 1U : 0U) | (flag ? 2U : 0U);
						if (encrypt && !integratedSecurity)
						{
							num6 |= 16U;
						}
						num5 = this._physicalStateObj.EnableSsl(ref num6);
						if (num5 != 0U)
						{
							this._physicalStateObj.AddError(this.ProcessSNIError(this._physicalStateObj));
							this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
						}
						this.WaitForSSLHandShakeToComplete(ref num5);
						this._physicalStateObj.ClearAllWritePackets();
					}
					break;
				}
				case 2:
				{
					int num3 = (int)array[num++] << 8 | (int)array[num++];
					byte b8 = array[num++];
					byte b9 = array[num++];
					byte b10 = 1;
					if (array[num3] == b10)
					{
						return PreLoginHandshakeStatus.InstanceFailure;
					}
					break;
				}
				case 3:
					num += 4;
					break;
				case 4:
				{
					int num3 = (int)array[num++] << 8 | (int)array[num++];
					byte b11 = array[num++];
					byte b12 = array[num++];
					marsCapable = (array[num3] != 0);
					break;
				}
				case 5:
					num += 4;
					break;
				case 6:
				{
					int num3 = (int)array[num++] << 8 | (int)array[num++];
					byte b13 = array[num++];
					byte b14 = array[num++];
					if (array[num3] != 0 && array[num3] != 1)
					{
						throw SQL.ParsingErrorValue(ParsingErrorState.FedAuthRequiredPreLoginResponseInvalidValue, (int)array[num3]);
					}
					if (this._connHandler.ConnectionOptions != null || this._connHandler._accessTokenInBytes != null)
					{
						fedAuthRequired = (array[num3] == 1);
					}
					break;
				}
				default:
					num += 4;
					break;
				}
				if (num >= array.Length)
				{
					break;
				}
			}
			return PreLoginHandshakeStatus.Successful;
		}

		// Token: 0x06001BF5 RID: 7157 RVA: 0x0007EC50 File Offset: 0x0007CE50
		internal void Deactivate(bool connectionIsDoomed)
		{
			if (this.MARSOn)
			{
				this._sessionPool.Deactivate();
			}
			if (!connectionIsDoomed && this._physicalStateObj != null)
			{
				if (this._physicalStateObj._pendingData)
				{
					this.DrainData(this._physicalStateObj);
				}
				if (this._physicalStateObj.HasOpenResult)
				{
					this._physicalStateObj.DecrementOpenResultCount();
				}
			}
			SqlInternalTransaction currentTransaction = this.CurrentTransaction;
			if (currentTransaction != null && currentTransaction.HasParentTransaction)
			{
				currentTransaction.CloseFromConnection();
			}
			this.Statistics = null;
		}

		// Token: 0x06001BF6 RID: 7158 RVA: 0x0007ECCC File Offset: 0x0007CECC
		internal void Disconnect()
		{
			if (this._sessionPool != null)
			{
				this._sessionPool.Dispose();
			}
			if (this._state != TdsParserState.Closed)
			{
				this._state = TdsParserState.Closed;
				try
				{
					if (!this._physicalStateObj.HasOwner)
					{
						this._physicalStateObj.SniContext = SniContext.Snix_Close;
						this._physicalStateObj.Dispose();
					}
					else
					{
						this._physicalStateObj.DecrementPendingCallbacks(false);
					}
					if (this._pMarsPhysicalConObj != null)
					{
						this._pMarsPhysicalConObj.Dispose();
					}
				}
				finally
				{
					this._pMarsPhysicalConObj = null;
				}
			}
		}

		// Token: 0x06001BF7 RID: 7159 RVA: 0x0007ED5C File Offset: 0x0007CF5C
		private void FireInfoMessageEvent(SqlConnection connection, TdsParserStateObject stateObj, SqlError error)
		{
			string serverVersion = null;
			if (this._state == TdsParserState.OpenLoggedIn)
			{
				serverVersion = this._connHandler.ServerVersion;
			}
			SqlException exception = SqlException.CreateException(new SqlErrorCollection
			{
				error
			}, serverVersion, this._connHandler, null);
			bool flag;
			connection.OnInfoMessage(new SqlInfoMessageEventArgs(exception), out flag);
			if (flag)
			{
				stateObj._syncOverAsync = true;
			}
		}

		// Token: 0x06001BF8 RID: 7160 RVA: 0x0007EDB2 File Offset: 0x0007CFB2
		internal void DisconnectTransaction(SqlInternalTransaction internalTransaction)
		{
			if (this._currentTransaction != null && this._currentTransaction == internalTransaction)
			{
				this._currentTransaction = null;
			}
		}

		// Token: 0x06001BF9 RID: 7161 RVA: 0x0007EDCC File Offset: 0x0007CFCC
		internal void RollbackOrphanedAPITransactions()
		{
			SqlInternalTransaction currentTransaction = this.CurrentTransaction;
			if (currentTransaction != null && currentTransaction.HasParentTransaction && currentTransaction.IsOrphaned)
			{
				currentTransaction.CloseFromConnection();
			}
		}

		// Token: 0x06001BFA RID: 7162 RVA: 0x0007EDFC File Offset: 0x0007CFFC
		internal void ThrowExceptionAndWarning(TdsParserStateObject stateObj, bool callerHasConnectionLock = false, bool asyncClose = false)
		{
			SqlException ex = null;
			bool flag;
			SqlErrorCollection fullErrorAndWarningCollection = stateObj.GetFullErrorAndWarningCollection(out flag);
			flag &= (this._state > TdsParserState.Closed);
			if (flag)
			{
				if (this._state == TdsParserState.OpenNotLoggedIn && (this._connHandler.ConnectionOptions.MultiSubnetFailover || this._loginWithFailover) && fullErrorAndWarningCollection.Count == 1 && (fullErrorAndWarningCollection[0].Number == -2 || (long)fullErrorAndWarningCollection[0].Number == 258L))
				{
					flag = false;
					this.Disconnect();
				}
				else
				{
					this._state = TdsParserState.Broken;
				}
			}
			if (fullErrorAndWarningCollection != null && fullErrorAndWarningCollection.Count > 0)
			{
				string serverVersion = null;
				if (this._state == TdsParserState.OpenLoggedIn)
				{
					serverVersion = this._connHandler.ServerVersion;
				}
				if (fullErrorAndWarningCollection.Count == 1 && fullErrorAndWarningCollection[0].Exception != null)
				{
					ex = SqlException.CreateException(fullErrorAndWarningCollection, serverVersion, this._connHandler, fullErrorAndWarningCollection[0].Exception);
				}
				else
				{
					ex = SqlException.CreateException(fullErrorAndWarningCollection, serverVersion, this._connHandler, null);
				}
			}
			if (ex != null)
			{
				if (flag)
				{
					TaskCompletionSource<object> networkPacketTaskSource = stateObj._networkPacketTaskSource;
					if (networkPacketTaskSource != null)
					{
						networkPacketTaskSource.TrySetException(ADP.ExceptionWithStackTrace(ex));
					}
				}
				if (asyncClose)
				{
					SqlInternalConnectionTds connHandler = this._connHandler;
					Action<Action> wrapCloseInAction = delegate(Action closeAction)
					{
						Task.Factory.StartNew(delegate()
						{
							connHandler._parserLock.Wait(false);
							connHandler.ThreadHasParserLockForClose = true;
							try
							{
								closeAction();
							}
							finally
							{
								connHandler.ThreadHasParserLockForClose = false;
								connHandler._parserLock.Release();
							}
						});
					};
					this._connHandler.OnError(ex, flag, wrapCloseInAction);
					return;
				}
				bool threadHasParserLockForClose = this._connHandler.ThreadHasParserLockForClose;
				if (callerHasConnectionLock)
				{
					this._connHandler.ThreadHasParserLockForClose = true;
				}
				try
				{
					this._connHandler.OnError(ex, flag, null);
				}
				finally
				{
					if (callerHasConnectionLock)
					{
						this._connHandler.ThreadHasParserLockForClose = threadHasParserLockForClose;
					}
				}
			}
		}

		// Token: 0x06001BFB RID: 7163 RVA: 0x0007EF8C File Offset: 0x0007D18C
		internal SqlError ProcessSNIError(TdsParserStateObject stateObj)
		{
			SNIErrorDetails sniErrorDetails = this.GetSniErrorDetails();
			if (sniErrorDetails.sniErrorNumber != 0U)
			{
				switch (sniErrorDetails.sniErrorNumber)
				{
				case 47U:
					throw SQL.MultiSubnetFailoverWithMoreThan64IPs();
				case 48U:
					throw SQL.MultiSubnetFailoverWithInstanceSpecified();
				case 49U:
					throw SQL.MultiSubnetFailoverWithNonTcpProtocol();
				}
			}
			string text = sniErrorDetails.errorMessage;
			bool useManagedSNI = TdsParserStateObjectFactory.UseManagedSNI;
			string sniContextEnumName = TdsEnums.GetSniContextEnumName(stateObj.SniContext);
			string resourceString = SR.GetResourceString(sniContextEnumName, sniContextEnumName);
			string text2 = string.Format(null, "SNI_PN{0}", sniErrorDetails.provider);
			string resourceString2 = SR.GetResourceString(text2, text2);
			if (sniErrorDetails.sniErrorNumber == 0U)
			{
				int num = text.IndexOf(':');
				if (0 <= num)
				{
					int num2 = text.Length;
					num2 -= Environment.NewLine.Length;
					num += 2;
					num2 -= num;
					if (num2 > 0)
					{
						text = text.Substring(num, num2);
					}
				}
			}
			else if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				string snierrorMessage = SQL.GetSNIErrorMessage((int)sniErrorDetails.sniErrorNumber);
				text = ((text != string.Empty) ? (snierrorMessage + ": " + text) : snierrorMessage);
			}
			else
			{
				text = SQL.GetSNIErrorMessage((int)sniErrorDetails.sniErrorNumber);
				if (sniErrorDetails.sniErrorNumber == 50U)
				{
					text += LocalDBAPI.GetLocalDBMessage((int)sniErrorDetails.nativeError);
				}
			}
			text = string.Format(null, "{0} (provider: {1}, error: {2} - {3})", new object[]
			{
				resourceString,
				resourceString2,
				(int)sniErrorDetails.sniErrorNumber,
				text
			});
			return new SqlError((int)sniErrorDetails.nativeError, 0, 20, this._server, text, sniErrorDetails.function, (int)sniErrorDetails.lineNumber, sniErrorDetails.nativeError, sniErrorDetails.exception);
		}

		// Token: 0x06001BFC RID: 7164 RVA: 0x0007F11C File Offset: 0x0007D31C
		internal void CheckResetConnection(TdsParserStateObject stateObj)
		{
			if (this._fResetConnection && !stateObj._fResetConnectionSent)
			{
				try
				{
					if (this._fMARS && !stateObj._fResetEventOwned)
					{
						stateObj._fResetEventOwned = this._resetConnectionEvent.WaitOne(stateObj.GetTimeoutRemaining());
						if (stateObj._fResetEventOwned && stateObj.TimeoutHasExpired)
						{
							stateObj._fResetEventOwned = !this._resetConnectionEvent.Set();
							stateObj.TimeoutTime = 0L;
						}
						if (!stateObj._fResetEventOwned)
						{
							stateObj.ResetBuffer();
							stateObj.AddError(new SqlError(-2, 0, 11, this._server, this._connHandler.TimeoutErrorInternal.GetErrorMessage(), "", 0, 258U, null));
							this.ThrowExceptionAndWarning(stateObj, true, false);
						}
					}
					if (this._fResetConnection)
					{
						if (this._fPreserveTransaction)
						{
							stateObj._outBuff[1] = (stateObj._outBuff[1] | 16);
						}
						else
						{
							stateObj._outBuff[1] = (stateObj._outBuff[1] | 8);
						}
						if (!this._fMARS)
						{
							this._fResetConnection = false;
							this._fPreserveTransaction = false;
						}
						else
						{
							stateObj._fResetConnectionSent = true;
						}
					}
					else if (this._fMARS && stateObj._fResetEventOwned)
					{
						stateObj._fResetEventOwned = !this._resetConnectionEvent.Set();
					}
				}
				catch (Exception)
				{
					if (this._fMARS && stateObj._fResetEventOwned)
					{
						stateObj._fResetConnectionSent = false;
						stateObj._fResetEventOwned = !this._resetConnectionEvent.Set();
					}
					throw;
				}
			}
		}

		// Token: 0x06001BFD RID: 7165 RVA: 0x0007F2D0 File Offset: 0x0007D4D0
		internal void WriteShort(int v, TdsParserStateObject stateObj)
		{
			if (stateObj._outBytesUsed + 2 > stateObj._outBuff.Length)
			{
				stateObj.WriteByte((byte)(v & 255));
				stateObj.WriteByte((byte)(v >> 8 & 255));
				return;
			}
			stateObj._outBuff[stateObj._outBytesUsed] = (byte)(v & 255);
			stateObj._outBuff[stateObj._outBytesUsed + 1] = (byte)(v >> 8 & 255);
			stateObj._outBytesUsed += 2;
		}

		// Token: 0x06001BFE RID: 7166 RVA: 0x0007F34A File Offset: 0x0007D54A
		internal void WriteUnsignedShort(ushort us, TdsParserStateObject stateObj)
		{
			this.WriteShort((int)((short)us), stateObj);
		}

		// Token: 0x06001BFF RID: 7167 RVA: 0x0007F355 File Offset: 0x0007D555
		internal void WriteUnsignedInt(uint i, TdsParserStateObject stateObj)
		{
			this.WriteInt((int)i, stateObj);
		}

		// Token: 0x06001C00 RID: 7168 RVA: 0x0007F360 File Offset: 0x0007D560
		internal void WriteInt(int v, TdsParserStateObject stateObj)
		{
			if (stateObj._outBytesUsed + 4 > stateObj._outBuff.Length)
			{
				for (int i = 0; i < 32; i += 8)
				{
					stateObj.WriteByte((byte)(v >> i & 255));
				}
				return;
			}
			stateObj._outBuff[stateObj._outBytesUsed] = (byte)(v & 255);
			stateObj._outBuff[stateObj._outBytesUsed + 1] = (byte)(v >> 8 & 255);
			stateObj._outBuff[stateObj._outBytesUsed + 2] = (byte)(v >> 16 & 255);
			stateObj._outBuff[stateObj._outBytesUsed + 3] = (byte)(v >> 24 & 255);
			stateObj._outBytesUsed += 4;
		}

		// Token: 0x06001C01 RID: 7169 RVA: 0x0007F410 File Offset: 0x0007D610
		internal void WriteFloat(float v, TdsParserStateObject stateObj)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			stateObj.WriteByteArray(bytes, bytes.Length, 0, true, null);
		}

		// Token: 0x06001C02 RID: 7170 RVA: 0x0007F434 File Offset: 0x0007D634
		internal void WriteLong(long v, TdsParserStateObject stateObj)
		{
			if (stateObj._outBytesUsed + 8 > stateObj._outBuff.Length)
			{
				for (int i = 0; i < 64; i += 8)
				{
					stateObj.WriteByte((byte)(v >> i & 255L));
				}
				return;
			}
			stateObj._outBuff[stateObj._outBytesUsed] = (byte)(v & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 1] = (byte)(v >> 8 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 2] = (byte)(v >> 16 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 3] = (byte)(v >> 24 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 4] = (byte)(v >> 32 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 5] = (byte)(v >> 40 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 6] = (byte)(v >> 48 & 255L);
			stateObj._outBuff[stateObj._outBytesUsed + 7] = (byte)(v >> 56 & 255L);
			stateObj._outBytesUsed += 8;
		}

		// Token: 0x06001C03 RID: 7171 RVA: 0x0007F558 File Offset: 0x0007D758
		internal void WritePartialLong(long v, int length, TdsParserStateObject stateObj)
		{
			if (stateObj._outBytesUsed + length > stateObj._outBuff.Length)
			{
				for (int i = 0; i < length * 8; i += 8)
				{
					stateObj.WriteByte((byte)(v >> i & 255L));
				}
				return;
			}
			for (int j = 0; j < length; j++)
			{
				stateObj._outBuff[stateObj._outBytesUsed + j] = (byte)(v >> j * 8 & 255L);
			}
			stateObj._outBytesUsed += length;
		}

		// Token: 0x06001C04 RID: 7172 RVA: 0x0007F5D3 File Offset: 0x0007D7D3
		internal void WriteUnsignedLong(ulong uv, TdsParserStateObject stateObj)
		{
			this.WriteLong((long)uv, stateObj);
		}

		// Token: 0x06001C05 RID: 7173 RVA: 0x0007F5E0 File Offset: 0x0007D7E0
		internal void WriteDouble(double v, TdsParserStateObject stateObj)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			stateObj.WriteByteArray(bytes, bytes.Length, 0, true, null);
		}

		// Token: 0x06001C06 RID: 7174 RVA: 0x0007F602 File Offset: 0x0007D802
		internal void PrepareResetConnection(bool preserveTransaction)
		{
			this._fResetConnection = true;
			this._fPreserveTransaction = preserveTransaction;
		}

		// Token: 0x06001C07 RID: 7175 RVA: 0x0007F618 File Offset: 0x0007D818
		internal bool Run(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj)
		{
			bool syncOverAsync = stateObj._syncOverAsync;
			bool result;
			try
			{
				stateObj._syncOverAsync = true;
				bool flag;
				this.TryRun(runBehavior, cmdHandler, dataStream, bulkCopyHandler, stateObj, out flag);
				result = flag;
			}
			finally
			{
				stateObj._syncOverAsync = syncOverAsync;
			}
			return result;
		}

		// Token: 0x06001C08 RID: 7176 RVA: 0x0007F664 File Offset: 0x0007D864
		internal static bool IsValidTdsToken(byte token)
		{
			return token == 170 || token == 171 || token == 173 || token == 227 || token == 172 || token == 121 || token == 160 || token == 161 || token == 129 || token == 136 || token == 164 || token == 165 || token == 169 || token == 211 || token == 209 || token == 210 || token == 253 || token == 254 || token == byte.MaxValue || token == 57 || token == 237 || token == 124 || token == 120 || token == 237 || token == 174 || token == 228;
		}

		// Token: 0x06001C09 RID: 7177 RVA: 0x0007F754 File Offset: 0x0007D954
		internal bool TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, out bool dataReady)
		{
			if (TdsParserState.Broken == this.State || this.State == TdsParserState.Closed)
			{
				dataReady = true;
				return true;
			}
			dataReady = false;
			for (;;)
			{
				if (stateObj._internalTimeout)
				{
					runBehavior = RunBehavior.Attention;
				}
				if (TdsParserState.Broken == this.State || this.State == TdsParserState.Closed)
				{
					goto IL_912;
				}
				if (!stateObj._accumulateInfoEvents && stateObj._pendingInfoEvents != null)
				{
					if (RunBehavior.Clean != (RunBehavior.Clean & runBehavior))
					{
						SqlConnection sqlConnection = null;
						if (this._connHandler != null)
						{
							sqlConnection = this._connHandler.Connection;
						}
						if (sqlConnection != null && sqlConnection.FireInfoMessageEventOnUserErrors)
						{
							using (List<SqlError>.Enumerator enumerator = stateObj._pendingInfoEvents.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									SqlError error = enumerator.Current;
									this.FireInfoMessageEvent(sqlConnection, stateObj, error);
								}
								goto IL_123;
							}
						}
						foreach (SqlError error2 in stateObj._pendingInfoEvents)
						{
							stateObj.AddWarning(error2);
						}
					}
					IL_123:
					stateObj._pendingInfoEvents = null;
				}
				byte b;
				if (!stateObj.TryReadByte(out b))
				{
					break;
				}
				if (!TdsParser.IsValidTdsToken(b))
				{
					goto Block_14;
				}
				int num;
				if (!this.TryGetTokenLength(b, stateObj, out num))
				{
					return false;
				}
				if (b <= 210)
				{
					if (b <= 129)
					{
						if (b != 121)
						{
							if (b == 129)
							{
								if (num != 65535)
								{
									_SqlMetaDataSet cleanupMetaData;
									if (!this.TryProcessMetaData(num, stateObj, out cleanupMetaData))
									{
										return false;
									}
									stateObj._cleanupMetaData = cleanupMetaData;
								}
								else if (cmdHandler != null)
								{
									stateObj._cleanupMetaData = cmdHandler.MetaData;
								}
								if (dataStream != null)
								{
									byte b2;
									if (!stateObj.TryPeekByte(out b2))
									{
										return false;
									}
									if (!dataStream.TrySetMetaData(stateObj._cleanupMetaData, 164 == b2 || 165 == b2))
									{
										return false;
									}
								}
								else if (bulkCopyHandler != null)
								{
									bulkCopyHandler.SetMetaData(stateObj._cleanupMetaData);
								}
							}
						}
						else
						{
							int status;
							if (!stateObj.TryReadInt32(out status))
							{
								return false;
							}
							if (cmdHandler != null)
							{
								cmdHandler.OnReturnStatus(status);
							}
						}
					}
					else if (b != 136)
					{
						switch (b)
						{
						case 164:
							if (dataStream != null)
							{
								MultiPartTableName[] tableNames;
								if (!this.TryProcessTableName(num, stateObj, out tableNames))
								{
									return false;
								}
								dataStream.TableNames = tableNames;
							}
							else if (!stateObj.TrySkipBytes(num))
							{
								return false;
							}
							break;
						case 165:
							if (dataStream != null)
							{
								_SqlMetaDataSet metaData;
								if (!this.TryProcessColInfo(dataStream.MetaData, dataStream, stateObj, out metaData))
								{
									return false;
								}
								if (!dataStream.TrySetMetaData(metaData, false))
								{
									return false;
								}
								dataStream.BrowseModeInfoConsumed = true;
							}
							else if (!stateObj.TrySkipBytes(num))
							{
								return false;
							}
							break;
						case 166:
						case 167:
						case 168:
							break;
						case 169:
							if (!stateObj.TrySkipBytes(num))
							{
								return false;
							}
							break;
						case 170:
						case 171:
						{
							if (b == 170)
							{
								stateObj._errorTokenReceived = true;
							}
							SqlError sqlError;
							if (!this.TryProcessError(b, stateObj, out sqlError))
							{
								return false;
							}
							if (b == 171 && stateObj._accumulateInfoEvents)
							{
								if (stateObj._pendingInfoEvents == null)
								{
									stateObj._pendingInfoEvents = new List<SqlError>();
								}
								stateObj._pendingInfoEvents.Add(sqlError);
								stateObj._syncOverAsync = true;
							}
							else if (RunBehavior.Clean != (RunBehavior.Clean & runBehavior))
							{
								SqlConnection sqlConnection2 = null;
								if (this._connHandler != null)
								{
									sqlConnection2 = this._connHandler.Connection;
								}
								if (sqlConnection2 != null && sqlConnection2.FireInfoMessageEventOnUserErrors && sqlError.Class <= 16)
								{
									this.FireInfoMessageEvent(sqlConnection2, stateObj, sqlError);
								}
								else if (sqlError.Class < 11)
								{
									stateObj.AddWarning(sqlError);
								}
								else if (sqlError.Class < 20)
								{
									stateObj.AddError(sqlError);
									if (dataStream != null && !dataStream.IsInitialized)
									{
										runBehavior = RunBehavior.UntilDone;
									}
								}
								else
								{
									stateObj.AddError(sqlError);
									runBehavior = RunBehavior.UntilDone;
								}
							}
							else if (sqlError.Class >= 20)
							{
								stateObj.AddError(sqlError);
							}
							break;
						}
						case 172:
						{
							SqlReturnValue rec;
							if (!this.TryProcessReturnValue(num, stateObj, out rec))
							{
								return false;
							}
							if (cmdHandler != null)
							{
								cmdHandler.OnReturnValue(rec, stateObj);
							}
							break;
						}
						case 173:
						{
							SqlLoginAck rec2;
							if (!this.TryProcessLoginAck(stateObj, out rec2))
							{
								return false;
							}
							this._connHandler.OnLoginAck(rec2);
							break;
						}
						case 174:
							if (!this.TryProcessFeatureExtAck(stateObj))
							{
								return false;
							}
							break;
						default:
							if (b - 209 <= 1)
							{
								if (b == 210)
								{
									if (!stateObj.TryStartNewRow(true, stateObj._cleanupMetaData.Length))
									{
										return false;
									}
								}
								else if (!stateObj.TryStartNewRow(false, 0))
								{
									return false;
								}
								if (bulkCopyHandler != null)
								{
									if (!this.TryProcessRow(stateObj._cleanupMetaData, bulkCopyHandler.CreateRowBuffer(), bulkCopyHandler.CreateIndexMap(), stateObj))
									{
										return false;
									}
								}
								else if (RunBehavior.ReturnImmediately != (RunBehavior.ReturnImmediately & runBehavior))
								{
									if (!this.TrySkipRow(stateObj._cleanupMetaData, stateObj))
									{
										return false;
									}
								}
								else
								{
									dataReady = true;
								}
								if (this._statistics != null)
								{
									this._statistics.WaitForDoneAfterRow = true;
								}
							}
							break;
						}
					}
					else
					{
						stateObj.CloneCleanupAltMetaDataSetArray();
						if (stateObj._cleanupAltMetaDataSetArray == null)
						{
							stateObj._cleanupAltMetaDataSetArray = new _SqlMetaDataSetCollection();
						}
						_SqlMetaDataSet sqlMetaDataSet;
						if (!this.TryProcessAltMetaData(num, stateObj, out sqlMetaDataSet))
						{
							return false;
						}
						stateObj._cleanupAltMetaDataSetArray.SetAltMetaData(sqlMetaDataSet);
						if (dataStream != null)
						{
							byte b3;
							if (!stateObj.TryPeekByte(out b3))
							{
								return false;
							}
							if (!dataStream.TrySetAltMetaDataSet(sqlMetaDataSet, 136 != b3))
							{
								return false;
							}
						}
					}
				}
				else if (b <= 227)
				{
					if (b != 211)
					{
						if (b == 227)
						{
							stateObj._syncOverAsync = true;
							SqlEnvChange[] array;
							if (!this.TryProcessEnvChange(num, stateObj, out array))
							{
								return false;
							}
							for (int i = 0; i < array.Length; i++)
							{
								if (array[i] != null && !this.Connection.IgnoreEnvChange)
								{
									switch (array[i].type)
									{
									case 8:
									case 11:
										this._currentTransaction = this._pendingTransaction;
										this._pendingTransaction = null;
										if (this._currentTransaction != null)
										{
											this._currentTransaction.TransactionId = array[i].newLongValue;
										}
										else
										{
											TransactionType type = TransactionType.LocalFromTSQL;
											this._currentTransaction = new SqlInternalTransaction(this._connHandler, type, null, array[i].newLongValue);
										}
										if (this._statistics != null && !this._statisticsIsInTransaction)
										{
											this._statistics.SafeIncrement(ref this._statistics._transactions);
										}
										this._statisticsIsInTransaction = true;
										this._retainedTransactionId = 0L;
										goto IL_697;
									case 9:
									case 12:
									case 17:
										this._retainedTransactionId = 0L;
										break;
									case 10:
										break;
									case 13:
									case 14:
									case 15:
									case 16:
										goto IL_687;
									default:
										goto IL_687;
									}
									if (this._currentTransaction != null)
									{
										if (9 == array[i].type)
										{
											this._currentTransaction.Completed(TransactionState.Committed);
										}
										else if (10 == array[i].type)
										{
											if (this._currentTransaction.IsDistributed && this._currentTransaction.IsActive)
											{
												this._retainedTransactionId = array[i].oldLongValue;
											}
											this._currentTransaction.Completed(TransactionState.Aborted);
										}
										else
										{
											this._currentTransaction.Completed(TransactionState.Unknown);
										}
										this._currentTransaction = null;
									}
									this._statisticsIsInTransaction = false;
									goto IL_697;
									IL_687:
									this._connHandler.OnEnvChange(array[i]);
								}
								IL_697:;
							}
						}
					}
					else
					{
						if (!stateObj.TryStartNewRow(false, 0))
						{
							return false;
						}
						if (RunBehavior.ReturnImmediately != (RunBehavior.ReturnImmediately & runBehavior))
						{
							ushort id;
							if (!stateObj.TryReadUInt16(out id))
							{
								return false;
							}
							if (!this.TrySkipRow(stateObj._cleanupAltMetaDataSetArray.GetAltMetaData((int)id), stateObj))
							{
								return false;
							}
						}
						else
						{
							dataReady = true;
						}
					}
				}
				else if (b != 228)
				{
					if (b != 237)
					{
						if (b - 253 <= 2)
						{
							if (!this.TryProcessDone(cmdHandler, dataStream, ref runBehavior, stateObj))
							{
								return false;
							}
							if (b == 254 && cmdHandler != null)
							{
								cmdHandler.OnDoneProc();
							}
						}
					}
					else
					{
						stateObj._syncOverAsync = true;
						this.ProcessSSPI(num);
					}
				}
				else if (!this.TryProcessSessionState(stateObj, num, this._connHandler._currentSessionData))
				{
					return false;
				}
				if ((!stateObj._pendingData || RunBehavior.ReturnImmediately == (RunBehavior.ReturnImmediately & runBehavior)) && (stateObj._pendingData || !stateObj._attentionSent || stateObj._attentionReceived))
				{
					goto IL_912;
				}
			}
			return false;
			Block_14:
			this._state = TdsParserState.Broken;
			this._connHandler.BreakConnection();
			throw SQL.ParsingError();
			IL_912:
			if (!stateObj._pendingData && this.CurrentTransaction != null)
			{
				this.CurrentTransaction.Activate();
			}
			if (stateObj._attentionReceived)
			{
				SpinWait.SpinUntil(() => !stateObj._attentionSending);
				if (stateObj._attentionSent)
				{
					stateObj._attentionSent = false;
					stateObj._attentionReceived = false;
					if (RunBehavior.Clean != (RunBehavior.Clean & runBehavior) && !stateObj._internalTimeout)
					{
						stateObj.AddError(new SqlError(0, 0, 11, this._server, SQLMessage.OperationCancelled(), "", 0, null));
					}
				}
			}
			if (stateObj.HasErrorOrWarning)
			{
				this.ThrowExceptionAndWarning(stateObj, false, false);
			}
			return true;
		}

		// Token: 0x06001C0A RID: 7178 RVA: 0x0008014C File Offset: 0x0007E34C
		private bool TryProcessEnvChange(int tokenLength, TdsParserStateObject stateObj, out SqlEnvChange[] sqlEnvChange)
		{
			int num = 0;
			int num2 = 0;
			SqlEnvChange[] array = new SqlEnvChange[3];
			sqlEnvChange = null;
			while (tokenLength > num)
			{
				if (num2 >= array.Length)
				{
					SqlEnvChange[] array2 = new SqlEnvChange[array.Length + 3];
					for (int i = 0; i < array.Length; i++)
					{
						array2[i] = array[i];
					}
					array = array2;
				}
				SqlEnvChange sqlEnvChange2 = new SqlEnvChange();
				if (!stateObj.TryReadByte(out sqlEnvChange2.type))
				{
					return false;
				}
				array[num2] = sqlEnvChange2;
				num2++;
				switch (sqlEnvChange2.type)
				{
				case 1:
				case 2:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					break;
				case 3:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					if (sqlEnvChange2.newValue == "iso_1")
					{
						this._defaultCodePage = 1252;
						this._defaultEncoding = Encoding.GetEncoding(this._defaultCodePage);
					}
					else
					{
						string s = sqlEnvChange2.newValue.Substring(2);
						this._defaultCodePage = int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
						this._defaultEncoding = Encoding.GetEncoding(this._defaultCodePage);
					}
					break;
				case 4:
				{
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						throw SQL.SynchronousCallMayNotPend();
					}
					int num3 = int.Parse(sqlEnvChange2.newValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
					if (this._physicalStateObj.SetPacketSize(num3))
					{
						this._physicalStateObj.ClearAllWritePackets();
						uint num4 = (uint)num3;
						this._physicalStateObj.SetConnectionBufferSize(ref num4);
					}
					break;
				}
				case 5:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					this._defaultLCID = int.Parse(sqlEnvChange2.newValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
					break;
				case 6:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					break;
				case 7:
				{
					byte b;
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					sqlEnvChange2.newLength = (int)b;
					if (sqlEnvChange2.newLength == 5)
					{
						if (!this.TryProcessCollation(stateObj, out sqlEnvChange2.newCollation))
						{
							return false;
						}
						this._defaultCollation = sqlEnvChange2.newCollation;
						int codePage = this.GetCodePage(sqlEnvChange2.newCollation, stateObj);
						if (codePage != this._defaultCodePage)
						{
							this._defaultCodePage = codePage;
							this._defaultEncoding = Encoding.GetEncoding(this._defaultCodePage);
						}
						this._defaultLCID = sqlEnvChange2.newCollation.LCID;
					}
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					sqlEnvChange2.oldLength = b;
					if (sqlEnvChange2.oldLength == 5 && !this.TryProcessCollation(stateObj, out sqlEnvChange2.oldCollation))
					{
						return false;
					}
					sqlEnvChange2.length = 3 + sqlEnvChange2.newLength + (int)sqlEnvChange2.oldLength;
					break;
				}
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
				case 17:
				{
					byte b;
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					sqlEnvChange2.newLength = (int)b;
					if (sqlEnvChange2.newLength > 0)
					{
						if (!stateObj.TryReadInt64(out sqlEnvChange2.newLongValue))
						{
							return false;
						}
					}
					else
					{
						sqlEnvChange2.newLongValue = 0L;
					}
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					sqlEnvChange2.oldLength = b;
					if (sqlEnvChange2.oldLength > 0)
					{
						if (!stateObj.TryReadInt64(out sqlEnvChange2.oldLongValue))
						{
							return false;
						}
					}
					else
					{
						sqlEnvChange2.oldLongValue = 0L;
					}
					sqlEnvChange2.length = 3 + sqlEnvChange2.newLength + (int)sqlEnvChange2.oldLength;
					break;
				}
				case 13:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					break;
				case 15:
				{
					if (!stateObj.TryReadInt32(out sqlEnvChange2.newLength))
					{
						return false;
					}
					sqlEnvChange2.newBinValue = new byte[sqlEnvChange2.newLength];
					if (!stateObj.TryReadByteArray(sqlEnvChange2.newBinValue, 0, sqlEnvChange2.newLength))
					{
						return false;
					}
					byte b;
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					sqlEnvChange2.oldLength = b;
					sqlEnvChange2.length = 5 + sqlEnvChange2.newLength;
					break;
				}
				case 16:
				case 18:
					if (!this.TryReadTwoBinaryFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					break;
				case 19:
					if (!this.TryReadTwoStringFields(sqlEnvChange2, stateObj))
					{
						return false;
					}
					break;
				case 20:
				{
					ushort newLength;
					if (!stateObj.TryReadUInt16(out newLength))
					{
						return false;
					}
					sqlEnvChange2.newLength = (int)newLength;
					byte protocol;
					if (!stateObj.TryReadByte(out protocol))
					{
						return false;
					}
					ushort port;
					if (!stateObj.TryReadUInt16(out port))
					{
						return false;
					}
					ushort length;
					if (!stateObj.TryReadUInt16(out length))
					{
						return false;
					}
					string servername;
					if (!stateObj.TryReadString((int)length, out servername))
					{
						return false;
					}
					sqlEnvChange2.newRoutingInfo = new RoutingInfo(protocol, port, servername);
					ushort num5;
					if (!stateObj.TryReadUInt16(out num5))
					{
						return false;
					}
					if (!stateObj.TrySkipBytes((int)num5))
					{
						return false;
					}
					sqlEnvChange2.length = sqlEnvChange2.newLength + (int)num5 + 5;
					break;
				}
				}
				num += sqlEnvChange2.length;
			}
			sqlEnvChange = array;
			return true;
		}

		// Token: 0x06001C0B RID: 7179 RVA: 0x000805CC File Offset: 0x0007E7CC
		private bool TryReadTwoBinaryFields(SqlEnvChange env, TdsParserStateObject stateObj)
		{
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			env.newLength = (int)b;
			env.newBinValue = new byte[env.newLength];
			if (!stateObj.TryReadByteArray(env.newBinValue, 0, env.newLength))
			{
				return false;
			}
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			env.oldLength = b;
			env.oldBinValue = new byte[(int)env.oldLength];
			if (!stateObj.TryReadByteArray(env.oldBinValue, 0, (int)env.oldLength))
			{
				return false;
			}
			env.length = 3 + env.newLength + (int)env.oldLength;
			return true;
		}

		// Token: 0x06001C0C RID: 7180 RVA: 0x00080668 File Offset: 0x0007E868
		private bool TryReadTwoStringFields(SqlEnvChange env, TdsParserStateObject stateObj)
		{
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			string newValue;
			if (!stateObj.TryReadString((int)b, out newValue))
			{
				return false;
			}
			byte b2;
			if (!stateObj.TryReadByte(out b2))
			{
				return false;
			}
			string oldValue;
			if (!stateObj.TryReadString((int)b2, out oldValue))
			{
				return false;
			}
			env.newLength = (int)b;
			env.newValue = newValue;
			env.oldLength = b2;
			env.oldValue = oldValue;
			env.length = 3 + env.newLength * 2 + (int)(env.oldLength * 2);
			return true;
		}

		// Token: 0x06001C0D RID: 7181 RVA: 0x000806E0 File Offset: 0x0007E8E0
		private bool TryProcessDone(SqlCommand cmd, SqlDataReader reader, ref RunBehavior run, TdsParserStateObject stateObj)
		{
			ushort num;
			if (!stateObj.TryReadUInt16(out num))
			{
				return false;
			}
			ushort num2;
			if (!stateObj.TryReadUInt16(out num2))
			{
				return false;
			}
			long num3;
			if (!stateObj.TryReadInt64(out num3))
			{
				return false;
			}
			int num4 = (int)num3;
			if (32 == (num & 32))
			{
				stateObj._attentionReceived = true;
			}
			if (cmd != null && 16 == (num & 16))
			{
				if (num2 != 193)
				{
					cmd.InternalRecordsAffected = num4;
				}
				if (stateObj._receivedColMetaData || num2 != 193)
				{
					cmd.OnStatementCompleted(num4);
				}
			}
			stateObj._receivedColMetaData = false;
			if (2 == (2 & num) && stateObj.ErrorCount == 0 && !stateObj._errorTokenReceived && RunBehavior.Clean != (RunBehavior.Clean & run))
			{
				stateObj.AddError(new SqlError(0, 0, 11, this._server, SQLMessage.SevereError(), "", 0, null));
				if (reader != null && !reader.IsInitialized)
				{
					run = RunBehavior.UntilDone;
				}
			}
			if (256 == (256 & num) && RunBehavior.Clean != (RunBehavior.Clean & run))
			{
				stateObj.AddError(new SqlError(0, 0, 20, this._server, SQLMessage.SevereError(), "", 0, null));
				if (reader != null && !reader.IsInitialized)
				{
					run = RunBehavior.UntilDone;
				}
			}
			this.ProcessSqlStatistics(num2, num, num4);
			if (1 != (num & 1))
			{
				stateObj._errorTokenReceived = false;
				if (stateObj._inBytesUsed >= stateObj._inBytesRead)
				{
					stateObj._pendingData = false;
				}
			}
			if (!stateObj._pendingData && stateObj._hasOpenResult)
			{
				stateObj.DecrementOpenResultCount();
			}
			return true;
		}

		// Token: 0x06001C0E RID: 7182 RVA: 0x00080840 File Offset: 0x0007EA40
		private void ProcessSqlStatistics(ushort curCmd, ushort status, int count)
		{
			if (this._statistics != null)
			{
				if (this._statistics.WaitForDoneAfterRow)
				{
					this._statistics.SafeIncrement(ref this._statistics._sumResultSets);
					this._statistics.WaitForDoneAfterRow = false;
				}
				if (16 != (status & 16))
				{
					count = 0;
				}
				if (curCmd <= 193)
				{
					if (curCmd == 32)
					{
						this._statistics.SafeIncrement(ref this._statistics._cursorOpens);
						return;
					}
					if (curCmd != 193)
					{
						return;
					}
					this._statistics.SafeIncrement(ref this._statistics._selectCount);
					this._statistics.SafeAdd(ref this._statistics._selectRows, (long)count);
					return;
				}
				else
				{
					if (curCmd - 195 > 2)
					{
						switch (curCmd)
						{
						case 210:
							this._statisticsIsInTransaction = false;
							return;
						case 211:
							return;
						case 212:
							if (!this._statisticsIsInTransaction)
							{
								this._statistics.SafeIncrement(ref this._statistics._transactions);
							}
							this._statisticsIsInTransaction = true;
							return;
						case 213:
							this._statisticsIsInTransaction = false;
							return;
						default:
							if (curCmd != 279)
							{
								return;
							}
							break;
						}
					}
					this._statistics.SafeIncrement(ref this._statistics._iduCount);
					this._statistics.SafeAdd(ref this._statistics._iduRows, (long)count);
					if (!this._statisticsIsInTransaction)
					{
						this._statistics.SafeIncrement(ref this._statistics._transactions);
						return;
					}
				}
			}
			else
			{
				switch (curCmd)
				{
				case 210:
				case 213:
					this._statisticsIsInTransaction = false;
					break;
				case 211:
					break;
				case 212:
					this._statisticsIsInTransaction = true;
					return;
				default:
					return;
				}
			}
		}

		// Token: 0x06001C0F RID: 7183 RVA: 0x000809E0 File Offset: 0x0007EBE0
		private bool TryProcessFeatureExtAck(TdsParserStateObject stateObj)
		{
			byte b;
			while (stateObj.TryReadByte(out b))
			{
				if (b != 255)
				{
					uint num;
					if (!stateObj.TryReadUInt32(out num))
					{
						return false;
					}
					byte[] array = new byte[num];
					if (num > 0U && !stateObj.TryReadByteArray(array, 0, checked((int)num)))
					{
						return false;
					}
					this._connHandler.OnFeatureExtAck((int)b, array);
				}
				if (b == 255)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001C10 RID: 7184 RVA: 0x00080A3C File Offset: 0x0007EC3C
		private bool TryProcessSessionState(TdsParserStateObject stateObj, int length, SessionData sdata)
		{
			if (length < 5)
			{
				throw SQL.ParsingError();
			}
			uint num;
			if (!stateObj.TryReadUInt32(out num))
			{
				return false;
			}
			if (num == 4294967295U)
			{
				this._connHandler.DoNotPoolThisConnection();
			}
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			if (b > 1)
			{
				throw SQL.ParsingError();
			}
			bool flag = b > 0;
			length -= 5;
			while (length > 0)
			{
				byte b2;
				if (!stateObj.TryReadByte(out b2))
				{
					return false;
				}
				byte b3;
				if (!stateObj.TryReadByte(out b3))
				{
					return false;
				}
				int num2;
				if (b3 < 255)
				{
					num2 = (int)b3;
				}
				else if (!stateObj.TryReadInt32(out num2))
				{
					return false;
				}
				byte[] array = null;
				SessionStateRecord[] delta = sdata._delta;
				checked
				{
					lock (delta)
					{
						if (sdata._delta[(int)b2] == null)
						{
							array = new byte[num2];
							sdata._delta[(int)b2] = new SessionStateRecord
							{
								_version = num,
								_dataLength = num2,
								_data = array,
								_recoverable = flag
							};
							sdata._deltaDirty = true;
							if (!flag)
							{
								sdata._unrecoverableStatesCount += 1;
							}
						}
						else if (sdata._delta[(int)b2]._version <= num)
						{
							SessionStateRecord sessionStateRecord = sdata._delta[(int)b2];
							sessionStateRecord._version = num;
							sessionStateRecord._dataLength = num2;
							if (sessionStateRecord._recoverable != flag)
							{
								if (flag)
								{
									unchecked
									{
										sdata._unrecoverableStatesCount -= 1;
									}
								}
								else
								{
									sdata._unrecoverableStatesCount += 1;
								}
								sessionStateRecord._recoverable = flag;
							}
							array = sessionStateRecord._data;
							if (array.Length < num2)
							{
								array = new byte[num2];
								sessionStateRecord._data = array;
							}
						}
					}
					if (array != null)
					{
						if (!stateObj.TryReadByteArray(array, 0, num2))
						{
							return false;
						}
					}
					else if (!stateObj.TrySkipBytes(num2))
					{
						return false;
					}
				}
				if (b3 < 255)
				{
					length -= 2 + num2;
				}
				else
				{
					length -= 6 + num2;
				}
			}
			return true;
		}

		// Token: 0x06001C11 RID: 7185 RVA: 0x00080C20 File Offset: 0x0007EE20
		private bool TryProcessLoginAck(TdsParserStateObject stateObj, out SqlLoginAck sqlLoginAck)
		{
			SqlLoginAck sqlLoginAck2 = new SqlLoginAck();
			sqlLoginAck = null;
			if (!stateObj.TrySkipBytes(1))
			{
				return false;
			}
			byte[] array = new byte[4];
			if (!stateObj.TryReadByteArray(array, 0, array.Length))
			{
				return false;
			}
			sqlLoginAck2.tdsVersion = (uint)((((int)array[0] << 8 | (int)array[1]) << 8 | (int)array[2]) << 8 | (int)array[3]);
			uint num = sqlLoginAck2.tdsVersion & 4278255615U;
			uint num2 = sqlLoginAck2.tdsVersion >> 16 & 255U;
			if (num != 1912602626U)
			{
				if (num != 1929379843U)
				{
					if (num != 1946157060U)
					{
						throw SQL.InvalidTDSVersion();
					}
					if (num2 != 0U)
					{
						throw SQL.InvalidTDSVersion();
					}
					this._isDenali = true;
				}
				else
				{
					if (num2 != 11U)
					{
						throw SQL.InvalidTDSVersion();
					}
					this._isKatmai = true;
				}
			}
			else
			{
				if (num2 != 9U)
				{
					throw SQL.InvalidTDSVersion();
				}
				this._isYukon = true;
			}
			this._isKatmai |= this._isDenali;
			this._isYukon |= this._isKatmai;
			stateObj._outBytesUsed = stateObj._outputHeaderLen;
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			if (!stateObj.TrySkipBytes((int)(b * 2)))
			{
				return false;
			}
			if (!stateObj.TryReadByte(out sqlLoginAck2.majorVersion))
			{
				return false;
			}
			if (!stateObj.TryReadByte(out sqlLoginAck2.minorVersion))
			{
				return false;
			}
			byte b2;
			if (!stateObj.TryReadByte(out b2))
			{
				return false;
			}
			byte b3;
			if (!stateObj.TryReadByte(out b3))
			{
				return false;
			}
			sqlLoginAck2.buildNum = (short)(((int)b2 << 8) + (int)b3);
			this._state = TdsParserState.OpenLoggedIn;
			if (this._fMARS)
			{
				this._resetConnectionEvent = new AutoResetEvent(true);
			}
			if (this._connHandler.ConnectionOptions.UserInstance && string.IsNullOrEmpty(this._connHandler.InstanceName))
			{
				stateObj.AddError(new SqlError(0, 0, 20, this.Server, SQLMessage.UserInstanceFailure(), "", 0, null));
				this.ThrowExceptionAndWarning(stateObj, false, false);
			}
			sqlLoginAck = sqlLoginAck2;
			return true;
		}

		// Token: 0x06001C12 RID: 7186 RVA: 0x00080DE8 File Offset: 0x0007EFE8
		internal bool TryProcessError(byte token, TdsParserStateObject stateObj, out SqlError error)
		{
			error = null;
			int infoNumber;
			if (!stateObj.TryReadInt32(out infoNumber))
			{
				return false;
			}
			byte errorState;
			if (!stateObj.TryReadByte(out errorState))
			{
				return false;
			}
			byte errorClass;
			if (!stateObj.TryReadByte(out errorClass))
			{
				return false;
			}
			ushort length;
			if (!stateObj.TryReadUInt16(out length))
			{
				return false;
			}
			string errorMessage;
			if (!stateObj.TryReadString((int)length, out errorMessage))
			{
				return false;
			}
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			string server;
			if (b == 0)
			{
				server = this._server;
			}
			else if (!stateObj.TryReadString((int)b, out server))
			{
				return false;
			}
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			string procedure;
			if (!stateObj.TryReadString((int)b, out procedure))
			{
				return false;
			}
			int num;
			if (this._isYukon)
			{
				if (!stateObj.TryReadInt32(out num))
				{
					return false;
				}
			}
			else
			{
				ushort num2;
				if (!stateObj.TryReadUInt16(out num2))
				{
					return false;
				}
				num = (int)num2;
				if (this._state == TdsParserState.OpenNotLoggedIn)
				{
					byte b2;
					if (!stateObj.TryPeekByte(out b2))
					{
						return false;
					}
					if (b2 == 0)
					{
						ushort num3;
						if (!stateObj.TryReadUInt16(out num3))
						{
							return false;
						}
						num = (num << 16) + (int)num3;
					}
				}
			}
			error = new SqlError(infoNumber, errorState, errorClass, this._server, errorMessage, procedure, num, null);
			return true;
		}

		// Token: 0x06001C13 RID: 7187 RVA: 0x00080EE0 File Offset: 0x0007F0E0
		internal bool TryProcessReturnValue(int length, TdsParserStateObject stateObj, out SqlReturnValue returnValue)
		{
			returnValue = null;
			SqlReturnValue sqlReturnValue = new SqlReturnValue();
			sqlReturnValue.length = length;
			ushort num;
			if (!stateObj.TryReadUInt16(out num))
			{
				return false;
			}
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			if (b > 0 && !stateObj.TryReadString((int)b, out sqlReturnValue.parameter))
			{
				return false;
			}
			byte b2;
			if (!stateObj.TryReadByte(out b2))
			{
				return false;
			}
			uint userType;
			if (!stateObj.TryReadUInt32(out userType))
			{
				return false;
			}
			ushort num2;
			if (!stateObj.TryReadUInt16(out num2))
			{
				return false;
			}
			byte b3;
			if (!stateObj.TryReadByte(out b3))
			{
				return false;
			}
			int num3;
			if (b3 == 241)
			{
				num3 = 65535;
			}
			else if (this.IsVarTimeTds(b3))
			{
				num3 = 0;
			}
			else if (b3 == 40)
			{
				num3 = 3;
			}
			else if (!this.TryGetTokenLength(b3, stateObj, out num3))
			{
				return false;
			}
			sqlReturnValue.metaType = MetaType.GetSqlDataType((int)b3, userType, num3);
			sqlReturnValue.type = sqlReturnValue.metaType.SqlDbType;
			sqlReturnValue.tdsType = sqlReturnValue.metaType.NullableType;
			sqlReturnValue.isNullable = true;
			if (num3 == 65535)
			{
				sqlReturnValue.metaType = MetaType.GetMaxMetaTypeFromMetaType(sqlReturnValue.metaType);
			}
			if (sqlReturnValue.type == SqlDbType.Decimal)
			{
				if (!stateObj.TryReadByte(out sqlReturnValue.precision))
				{
					return false;
				}
				if (!stateObj.TryReadByte(out sqlReturnValue.scale))
				{
					return false;
				}
			}
			if (sqlReturnValue.metaType.IsVarTime && !stateObj.TryReadByte(out sqlReturnValue.scale))
			{
				return false;
			}
			if (b3 == 240 && !this.TryProcessUDTMetaData(sqlReturnValue, stateObj))
			{
				return false;
			}
			if (sqlReturnValue.type == SqlDbType.Xml)
			{
				byte b4;
				if (!stateObj.TryReadByte(out b4))
				{
					return false;
				}
				if ((b4 & 1) != 0)
				{
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					if (b != 0 && !stateObj.TryReadString((int)b, out sqlReturnValue.xmlSchemaCollectionDatabase))
					{
						return false;
					}
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					if (b != 0 && !stateObj.TryReadString((int)b, out sqlReturnValue.xmlSchemaCollectionOwningSchema))
					{
						return false;
					}
					short num4;
					if (!stateObj.TryReadInt16(out num4))
					{
						return false;
					}
					if (num4 != 0 && !stateObj.TryReadString((int)num4, out sqlReturnValue.xmlSchemaCollectionName))
					{
						return false;
					}
				}
			}
			else if (sqlReturnValue.metaType.IsCharType)
			{
				if (!this.TryProcessCollation(stateObj, out sqlReturnValue.collation))
				{
					return false;
				}
				int codePage = this.GetCodePage(sqlReturnValue.collation, stateObj);
				if (codePage == this._defaultCodePage)
				{
					sqlReturnValue.codePage = this._defaultCodePage;
					sqlReturnValue.encoding = this._defaultEncoding;
				}
				else
				{
					sqlReturnValue.codePage = codePage;
					sqlReturnValue.encoding = Encoding.GetEncoding(sqlReturnValue.codePage);
				}
			}
			bool flag = false;
			ulong num5;
			if (!this.TryProcessColumnHeaderNoNBC(sqlReturnValue, stateObj, out flag, out num5))
			{
				return false;
			}
			int length2 = (num5 > 2147483647UL) ? int.MaxValue : ((int)num5);
			if (sqlReturnValue.metaType.IsPlp)
			{
				length2 = int.MaxValue;
			}
			if (flag)
			{
				this.GetNullSqlValue(sqlReturnValue.value, sqlReturnValue);
			}
			else if (!this.TryReadSqlValue(sqlReturnValue.value, sqlReturnValue, length2, stateObj))
			{
				return false;
			}
			returnValue = sqlReturnValue;
			return true;
		}

		// Token: 0x06001C14 RID: 7188 RVA: 0x00081198 File Offset: 0x0007F398
		internal bool TryProcessCollation(TdsParserStateObject stateObj, out SqlCollation collation)
		{
			SqlCollation sqlCollation = new SqlCollation();
			if (!stateObj.TryReadUInt32(out sqlCollation.info))
			{
				collation = null;
				return false;
			}
			if (!stateObj.TryReadByte(out sqlCollation.sortId))
			{
				collation = null;
				return false;
			}
			collation = sqlCollation;
			return true;
		}

		// Token: 0x06001C15 RID: 7189 RVA: 0x000811D8 File Offset: 0x0007F3D8
		private void WriteCollation(SqlCollation collation, TdsParserStateObject stateObj)
		{
			if (collation == null)
			{
				this._physicalStateObj.WriteByte(0);
				return;
			}
			this._physicalStateObj.WriteByte(5);
			this.WriteUnsignedInt(collation.info, this._physicalStateObj);
			this._physicalStateObj.WriteByte(collation.sortId);
		}

		// Token: 0x06001C16 RID: 7190 RVA: 0x00081224 File Offset: 0x0007F424
		internal int GetCodePage(SqlCollation collation, TdsParserStateObject stateObj)
		{
			int num = 0;
			if (collation.sortId != 0)
			{
				num = (int)TdsEnums.CODE_PAGE_FROM_SORT_ID[(int)collation.sortId];
			}
			else
			{
				int num2 = collation.LCID;
				bool flag = false;
				try
				{
					num = CultureInfo.GetCultureInfo(num2).TextInfo.ANSICodePage;
					flag = true;
				}
				catch (ArgumentException)
				{
				}
				if (!flag || num == 0)
				{
					if (num2 <= 66578)
					{
						if (num2 == 2087)
						{
							goto IL_B4;
						}
						if (num2 != 66564 && num2 - 66577 > 1)
						{
							goto IL_D1;
						}
					}
					else if (num2 <= 68612)
					{
						if (num2 != 67588 && num2 != 68612)
						{
							goto IL_D1;
						}
					}
					else if (num2 != 69636 && num2 != 70660)
					{
						goto IL_D1;
					}
					num2 &= 16383;
					try
					{
						num = new CultureInfo(num2).TextInfo.ANSICodePage;
						flag = true;
						goto IL_D1;
					}
					catch (ArgumentException)
					{
						goto IL_D1;
					}
					IL_B4:
					try
					{
						num = new CultureInfo(1063).TextInfo.ANSICodePage;
						flag = true;
					}
					catch (ArgumentException)
					{
					}
					IL_D1:
					if (!flag)
					{
						this.ThrowUnsupportedCollationEncountered(stateObj);
					}
				}
			}
			return num;
		}

		// Token: 0x06001C17 RID: 7191 RVA: 0x00081338 File Offset: 0x0007F538
		internal void DrainData(TdsParserStateObject stateObj)
		{
			try
			{
				SqlDataReader.SharedState readerState = stateObj._readerState;
				if (readerState != null && readerState._dataReady)
				{
					_SqlMetaDataSet cleanupMetaData = stateObj._cleanupMetaData;
					if (stateObj._partialHeaderBytesRead > 0 && !stateObj.TryProcessHeader())
					{
						throw SQL.SynchronousCallMayNotPend();
					}
					if (readerState._nextColumnHeaderToRead == 0)
					{
						if (!stateObj.Parser.TrySkipRow(stateObj._cleanupMetaData, stateObj))
						{
							throw SQL.SynchronousCallMayNotPend();
						}
					}
					else
					{
						if (readerState._nextColumnDataToRead < readerState._nextColumnHeaderToRead)
						{
							if (readerState._nextColumnHeaderToRead > 0 && cleanupMetaData[readerState._nextColumnHeaderToRead - 1].metaType.IsPlp)
							{
								ulong num;
								if (stateObj._longlen != 0UL && !this.TrySkipPlpValue(18446744073709551615UL, stateObj, out num))
								{
									throw SQL.SynchronousCallMayNotPend();
								}
							}
							else if (0L < readerState._columnDataBytesRemaining && !stateObj.TrySkipLongBytes(readerState._columnDataBytesRemaining))
							{
								throw SQL.SynchronousCallMayNotPend();
							}
						}
						if (!stateObj.Parser.TrySkipRow(cleanupMetaData, readerState._nextColumnHeaderToRead, stateObj))
						{
							throw SQL.SynchronousCallMayNotPend();
						}
					}
				}
				this.Run(RunBehavior.Clean, null, null, null, stateObj);
			}
			catch
			{
				this._connHandler.DoomThisConnection();
				throw;
			}
		}

		// Token: 0x06001C18 RID: 7192 RVA: 0x00081454 File Offset: 0x0007F654
		internal void ThrowUnsupportedCollationEncountered(TdsParserStateObject stateObj)
		{
			stateObj.AddError(new SqlError(0, 0, 11, this._server, SQLMessage.CultureIdError(), "", 0, null));
			if (stateObj != null)
			{
				this.DrainData(stateObj);
				stateObj._pendingData = false;
			}
			this.ThrowExceptionAndWarning(stateObj, false, false);
		}

		// Token: 0x06001C19 RID: 7193 RVA: 0x0008149C File Offset: 0x0007F69C
		internal bool TryProcessAltMetaData(int cColumns, TdsParserStateObject stateObj, out _SqlMetaDataSet metaData)
		{
			metaData = null;
			_SqlMetaDataSet sqlMetaDataSet = new _SqlMetaDataSet(cColumns);
			int[] array = new int[cColumns];
			if (!stateObj.TryReadUInt16(out sqlMetaDataSet.id))
			{
				return false;
			}
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			while (b > 0)
			{
				if (!stateObj.TrySkipBytes(2))
				{
					return false;
				}
				b -= 1;
			}
			for (int i = 0; i < cColumns; i++)
			{
				_SqlMetaData col = sqlMetaDataSet[i];
				byte b2;
				if (!stateObj.TryReadByte(out b2))
				{
					return false;
				}
				ushort num;
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				if (!this.TryCommonProcessMetaData(stateObj, col))
				{
					return false;
				}
				array[i] = i;
			}
			sqlMetaDataSet.indexMap = array;
			sqlMetaDataSet.visibleColumns = cColumns;
			metaData = sqlMetaDataSet;
			return true;
		}

		// Token: 0x06001C1A RID: 7194 RVA: 0x0008153C File Offset: 0x0007F73C
		internal bool TryProcessMetaData(int cColumns, TdsParserStateObject stateObj, out _SqlMetaDataSet metaData)
		{
			_SqlMetaDataSet sqlMetaDataSet = new _SqlMetaDataSet(cColumns);
			for (int i = 0; i < cColumns; i++)
			{
				if (!this.TryCommonProcessMetaData(stateObj, sqlMetaDataSet[i]))
				{
					metaData = null;
					return false;
				}
			}
			metaData = sqlMetaDataSet;
			return true;
		}

		// Token: 0x06001C1B RID: 7195 RVA: 0x00081575 File Offset: 0x0007F775
		private bool IsVarTimeTds(byte tdsType)
		{
			return tdsType == 41 || tdsType == 42 || tdsType == 43;
		}

		// Token: 0x06001C1C RID: 7196 RVA: 0x00081588 File Offset: 0x0007F788
		private bool TryCommonProcessMetaData(TdsParserStateObject stateObj, _SqlMetaData col)
		{
			uint userType;
			if (!stateObj.TryReadUInt32(out userType))
			{
				return false;
			}
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			col.updatability = (byte)((b & 11) >> 2);
			col.isNullable = (1 == (b & 1));
			col.isIdentity = (16 == (b & 16));
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			col.isColumnSet = (4 == (b & 4));
			byte b2;
			if (!stateObj.TryReadByte(out b2))
			{
				return false;
			}
			if (b2 == 241)
			{
				col.length = 65535;
			}
			else if (this.IsVarTimeTds(b2))
			{
				col.length = 0;
			}
			else if (b2 == 40)
			{
				col.length = 3;
			}
			else if (!this.TryGetTokenLength(b2, stateObj, out col.length))
			{
				return false;
			}
			col.metaType = MetaType.GetSqlDataType((int)b2, userType, col.length);
			col.type = col.metaType.SqlDbType;
			col.tdsType = (col.isNullable ? col.metaType.NullableType : col.metaType.TDSType);
			if (240 == b2 && !this.TryProcessUDTMetaData(col, stateObj))
			{
				return false;
			}
			byte b4;
			if (col.length == 65535)
			{
				col.metaType = MetaType.GetMaxMetaTypeFromMetaType(col.metaType);
				col.length = int.MaxValue;
				if (b2 == 241)
				{
					byte b3;
					if (!stateObj.TryReadByte(out b3))
					{
						return false;
					}
					if ((b3 & 1) != 0)
					{
						if (!stateObj.TryReadByte(out b4))
						{
							return false;
						}
						if (b4 != 0 && !stateObj.TryReadString((int)b4, out col.xmlSchemaCollectionDatabase))
						{
							return false;
						}
						if (!stateObj.TryReadByte(out b4))
						{
							return false;
						}
						if (b4 != 0 && !stateObj.TryReadString((int)b4, out col.xmlSchemaCollectionOwningSchema))
						{
							return false;
						}
						short length;
						if (!stateObj.TryReadInt16(out length))
						{
							return false;
						}
						if (b4 != 0 && !stateObj.TryReadString((int)length, out col.xmlSchemaCollectionName))
						{
							return false;
						}
					}
				}
			}
			if (col.type == SqlDbType.Decimal)
			{
				if (!stateObj.TryReadByte(out col.precision))
				{
					return false;
				}
				if (!stateObj.TryReadByte(out col.scale))
				{
					return false;
				}
			}
			if (col.metaType.IsVarTime)
			{
				if (!stateObj.TryReadByte(out col.scale))
				{
					return false;
				}
				switch (col.metaType.SqlDbType)
				{
				case SqlDbType.Time:
					col.length = MetaType.GetTimeSizeFromScale(col.scale);
					break;
				case SqlDbType.DateTime2:
					col.length = 3 + MetaType.GetTimeSizeFromScale(col.scale);
					break;
				case SqlDbType.DateTimeOffset:
					col.length = 5 + MetaType.GetTimeSizeFromScale(col.scale);
					break;
				}
			}
			if (col.metaType.IsCharType && b2 != 241)
			{
				if (!this.TryProcessCollation(stateObj, out col.collation))
				{
					return false;
				}
				int codePage = this.GetCodePage(col.collation, stateObj);
				if (codePage == this._defaultCodePage)
				{
					col.codePage = this._defaultCodePage;
					col.encoding = this._defaultEncoding;
				}
				else
				{
					col.codePage = codePage;
					col.encoding = Encoding.GetEncoding(col.codePage);
				}
			}
			if (col.metaType.IsLong && !col.metaType.IsPlp)
			{
				int num = 65535;
				if (!this.TryProcessOneTable(stateObj, ref num, out col.multiPartTableName))
				{
					return false;
				}
			}
			if (!stateObj.TryReadByte(out b4))
			{
				return false;
			}
			if (!stateObj.TryReadString((int)b4, out col.column))
			{
				return false;
			}
			stateObj._receivedColMetaData = true;
			return true;
		}

		// Token: 0x06001C1D RID: 7197 RVA: 0x000818B8 File Offset: 0x0007FAB8
		private void WriteUDTMetaData(object value, string database, string schema, string type, TdsParserStateObject stateObj)
		{
			if (string.IsNullOrEmpty(database))
			{
				stateObj.WriteByte(0);
			}
			else
			{
				stateObj.WriteByte((byte)database.Length);
				this.WriteString(database, stateObj, true);
			}
			if (string.IsNullOrEmpty(schema))
			{
				stateObj.WriteByte(0);
			}
			else
			{
				stateObj.WriteByte((byte)schema.Length);
				this.WriteString(schema, stateObj, true);
			}
			if (string.IsNullOrEmpty(type))
			{
				stateObj.WriteByte(0);
				return;
			}
			stateObj.WriteByte((byte)type.Length);
			this.WriteString(type, stateObj, true);
		}

		// Token: 0x06001C1E RID: 7198 RVA: 0x00081948 File Offset: 0x0007FB48
		internal bool TryProcessTableName(int length, TdsParserStateObject stateObj, out MultiPartTableName[] multiPartTableNames)
		{
			int num = 0;
			MultiPartTableName[] array = new MultiPartTableName[1];
			while (length > 0)
			{
				MultiPartTableName multiPartTableName;
				if (!this.TryProcessOneTable(stateObj, ref length, out multiPartTableName))
				{
					multiPartTableNames = null;
					return false;
				}
				if (num == 0)
				{
					array[num] = multiPartTableName;
				}
				else
				{
					MultiPartTableName[] array2 = new MultiPartTableName[array.Length + 1];
					Array.Copy(array, 0, array2, 0, array.Length);
					array2[array.Length] = multiPartTableName;
					array = array2;
				}
				num++;
			}
			multiPartTableNames = array;
			return true;
		}

		// Token: 0x06001C1F RID: 7199 RVA: 0x000819B0 File Offset: 0x0007FBB0
		private bool TryProcessOneTable(TdsParserStateObject stateObj, ref int length, out MultiPartTableName multiPartTableName)
		{
			multiPartTableName = default(MultiPartTableName);
			MultiPartTableName multiPartTableName2 = default(MultiPartTableName);
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			length--;
			if (b == 4)
			{
				ushort num;
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				length -= 2;
				string text;
				if (!stateObj.TryReadString((int)num, out text))
				{
					return false;
				}
				multiPartTableName2.ServerName = text;
				b -= 1;
				length -= (int)(num * 2);
			}
			if (b == 3)
			{
				ushort num;
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				length -= 2;
				string text;
				if (!stateObj.TryReadString((int)num, out text))
				{
					return false;
				}
				multiPartTableName2.CatalogName = text;
				length -= (int)(num * 2);
				b -= 1;
			}
			if (b == 2)
			{
				ushort num;
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				length -= 2;
				string text;
				if (!stateObj.TryReadString((int)num, out text))
				{
					return false;
				}
				multiPartTableName2.SchemaName = text;
				length -= (int)(num * 2);
				b -= 1;
			}
			if (b == 1)
			{
				ushort num;
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				length -= 2;
				string text;
				if (!stateObj.TryReadString((int)num, out text))
				{
					return false;
				}
				multiPartTableName2.TableName = text;
				length -= (int)(num * 2);
				b -= 1;
			}
			multiPartTableName = multiPartTableName2;
			return true;
		}

		// Token: 0x06001C20 RID: 7200 RVA: 0x00081AC8 File Offset: 0x0007FCC8
		private bool TryProcessColInfo(_SqlMetaDataSet columns, SqlDataReader reader, TdsParserStateObject stateObj, out _SqlMetaDataSet metaData)
		{
			metaData = null;
			for (int i = 0; i < columns.Length; i++)
			{
				_SqlMetaData sqlMetaData = columns[i];
				byte b;
				if (!stateObj.TryReadByte(out b))
				{
					return false;
				}
				if (!stateObj.TryReadByte(out sqlMetaData.tableNum))
				{
					return false;
				}
				byte b2;
				if (!stateObj.TryReadByte(out b2))
				{
					return false;
				}
				sqlMetaData.isDifferentName = (32 == (b2 & 32));
				sqlMetaData.isExpression = (4 == (b2 & 4));
				sqlMetaData.isKey = (8 == (b2 & 8));
				sqlMetaData.isHidden = (16 == (b2 & 16));
				if (sqlMetaData.isDifferentName)
				{
					byte length;
					if (!stateObj.TryReadByte(out length))
					{
						return false;
					}
					if (!stateObj.TryReadString((int)length, out sqlMetaData.baseColumn))
					{
						return false;
					}
				}
				if (reader.TableNames != null && sqlMetaData.tableNum > 0)
				{
					sqlMetaData.multiPartTableName = reader.TableNames[(int)(sqlMetaData.tableNum - 1)];
				}
				if (sqlMetaData.isExpression)
				{
					sqlMetaData.updatability = 0;
				}
			}
			metaData = columns;
			return true;
		}

		// Token: 0x06001C21 RID: 7201 RVA: 0x00081BB8 File Offset: 0x0007FDB8
		internal bool TryProcessColumnHeader(SqlMetaDataPriv col, TdsParserStateObject stateObj, int columnOrdinal, out bool isNull, out ulong length)
		{
			if (stateObj.IsNullCompressionBitSet(columnOrdinal))
			{
				isNull = true;
				length = 0UL;
				return true;
			}
			return this.TryProcessColumnHeaderNoNBC(col, stateObj, out isNull, out length);
		}

		// Token: 0x06001C22 RID: 7202 RVA: 0x00081BDC File Offset: 0x0007FDDC
		private bool TryProcessColumnHeaderNoNBC(SqlMetaDataPriv col, TdsParserStateObject stateObj, out bool isNull, out ulong length)
		{
			if (col.metaType.IsLong && !col.metaType.IsPlp)
			{
				byte b;
				if (!stateObj.TryReadByte(out b))
				{
					isNull = false;
					length = 0UL;
					return false;
				}
				if (b == 0)
				{
					isNull = true;
					length = 0UL;
					return true;
				}
				if (!stateObj.TrySkipBytes((int)b))
				{
					isNull = false;
					length = 0UL;
					return false;
				}
				if (!stateObj.TrySkipBytes(8))
				{
					isNull = false;
					length = 0UL;
					return false;
				}
				isNull = false;
				return this.TryGetDataLength(col, stateObj, out length);
			}
			else
			{
				ulong num;
				if (!this.TryGetDataLength(col, stateObj, out num))
				{
					isNull = false;
					length = 0UL;
					return false;
				}
				isNull = this.IsNull(col.metaType, num);
				length = (isNull ? 0UL : num);
				return true;
			}
		}

		// Token: 0x06001C23 RID: 7203 RVA: 0x00081C8C File Offset: 0x0007FE8C
		internal bool TryGetAltRowId(TdsParserStateObject stateObj, out int id)
		{
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				id = 0;
				return false;
			}
			if (!stateObj.TryStartNewRow(false, 0))
			{
				id = 0;
				return false;
			}
			ushort num;
			if (!stateObj.TryReadUInt16(out num))
			{
				id = 0;
				return false;
			}
			id = (int)num;
			return true;
		}

		// Token: 0x06001C24 RID: 7204 RVA: 0x00081CCC File Offset: 0x0007FECC
		private bool TryProcessRow(_SqlMetaDataSet columns, object[] buffer, int[] map, TdsParserStateObject stateObj)
		{
			SqlBuffer sqlBuffer = new SqlBuffer();
			for (int i = 0; i < columns.Length; i++)
			{
				_SqlMetaData sqlMetaData = columns[i];
				bool flag;
				ulong num;
				if (!this.TryProcessColumnHeader(sqlMetaData, stateObj, i, out flag, out num))
				{
					return false;
				}
				if (flag)
				{
					this.GetNullSqlValue(sqlBuffer, sqlMetaData);
					buffer[map[i]] = sqlBuffer.SqlValue;
				}
				else
				{
					if (!this.TryReadSqlValue(sqlBuffer, sqlMetaData, sqlMetaData.metaType.IsPlp ? 2147483647 : ((int)num), stateObj))
					{
						return false;
					}
					buffer[map[i]] = sqlBuffer.SqlValue;
					if (stateObj._longlen != 0UL)
					{
						throw new SqlTruncateException(SR.GetString("Data returned is larger than 2Gb in size. Use SequentialAccess command behavior in order to get all of the data."));
					}
				}
				sqlBuffer.Clear();
			}
			return true;
		}

		// Token: 0x06001C25 RID: 7205 RVA: 0x00081D7C File Offset: 0x0007FF7C
		internal object GetNullSqlValue(SqlBuffer nullVal, SqlMetaDataPriv md)
		{
			switch (md.type)
			{
			case SqlDbType.BigInt:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Int64);
				break;
			case SqlDbType.Binary:
			case SqlDbType.Image:
			case SqlDbType.VarBinary:
			case SqlDbType.Udt:
				nullVal.SqlBinary = SqlBinary.Null;
				break;
			case SqlDbType.Bit:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Boolean);
				break;
			case SqlDbType.Char:
			case SqlDbType.NChar:
			case SqlDbType.NText:
			case SqlDbType.NVarChar:
			case SqlDbType.Text:
			case SqlDbType.VarChar:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.String);
				break;
			case SqlDbType.DateTime:
			case SqlDbType.SmallDateTime:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.DateTime);
				break;
			case SqlDbType.Decimal:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Decimal);
				break;
			case SqlDbType.Float:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Double);
				break;
			case SqlDbType.Int:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Int32);
				break;
			case SqlDbType.Money:
			case SqlDbType.SmallMoney:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Money);
				break;
			case SqlDbType.Real:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Single);
				break;
			case SqlDbType.UniqueIdentifier:
				nullVal.SqlGuid = SqlGuid.Null;
				break;
			case SqlDbType.SmallInt:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Int16);
				break;
			case SqlDbType.TinyInt:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Byte);
				break;
			case SqlDbType.Variant:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Empty);
				break;
			case SqlDbType.Xml:
				nullVal.SqlCachedBuffer = SqlCachedBuffer.Null;
				break;
			case SqlDbType.Date:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Date);
				break;
			case SqlDbType.Time:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.Time);
				break;
			case SqlDbType.DateTime2:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.DateTime2);
				break;
			case SqlDbType.DateTimeOffset:
				nullVal.SetToNullOfType(SqlBuffer.StorageType.DateTimeOffset);
				break;
			}
			return nullVal;
		}

		// Token: 0x06001C26 RID: 7206 RVA: 0x00081EF3 File Offset: 0x000800F3
		internal bool TrySkipRow(_SqlMetaDataSet columns, TdsParserStateObject stateObj)
		{
			return this.TrySkipRow(columns, 0, stateObj);
		}

		// Token: 0x06001C27 RID: 7207 RVA: 0x00081F00 File Offset: 0x00080100
		internal bool TrySkipRow(_SqlMetaDataSet columns, int startCol, TdsParserStateObject stateObj)
		{
			for (int i = startCol; i < columns.Length; i++)
			{
				_SqlMetaData md = columns[i];
				if (!this.TrySkipValue(md, i, stateObj))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001C28 RID: 7208 RVA: 0x00081F34 File Offset: 0x00080134
		internal bool TrySkipValue(SqlMetaDataPriv md, int columnOrdinal, TdsParserStateObject stateObj)
		{
			if (stateObj.IsNullCompressionBitSet(columnOrdinal))
			{
				return true;
			}
			if (md.metaType.IsPlp)
			{
				ulong num;
				if (!this.TrySkipPlpValue(18446744073709551615UL, stateObj, out num))
				{
					return false;
				}
			}
			else if (md.metaType.IsLong)
			{
				byte b;
				if (!stateObj.TryReadByte(out b))
				{
					return false;
				}
				if (b != 0)
				{
					if (!stateObj.TrySkipBytes((int)(b + 8)))
					{
						return false;
					}
					int num2;
					if (!this.TryGetTokenLength(md.tdsType, stateObj, out num2))
					{
						return false;
					}
					if (!stateObj.TrySkipBytes(num2))
					{
						return false;
					}
				}
			}
			else
			{
				int num3;
				if (!this.TryGetTokenLength(md.tdsType, stateObj, out num3))
				{
					return false;
				}
				if (!this.IsNull(md.metaType, (ulong)((long)num3)) && !stateObj.TrySkipBytes(num3))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001C29 RID: 7209 RVA: 0x00081FDE File Offset: 0x000801DE
		private bool IsNull(MetaType mt, ulong length)
		{
			if (mt.IsPlp)
			{
				return ulong.MaxValue == length;
			}
			return (65535UL == length && !mt.IsLong) || (length == 0UL && !mt.IsCharType && !mt.IsBinType);
		}

		// Token: 0x06001C2A RID: 7210 RVA: 0x00082018 File Offset: 0x00080218
		private bool TryReadSqlStringValue(SqlBuffer value, byte type, int length, Encoding encoding, bool isPlp, TdsParserStateObject stateObj)
		{
			if (type <= 99)
			{
				if (type <= 39)
				{
					if (type != 35 && type != 39)
					{
						return true;
					}
				}
				else if (type != 47)
				{
					if (type != 99)
					{
						return true;
					}
					goto IL_7E;
				}
			}
			else if (type <= 175)
			{
				if (type != 167 && type != 175)
				{
					return true;
				}
			}
			else
			{
				if (type != 231 && type != 239)
				{
					return true;
				}
				goto IL_7E;
			}
			if (encoding == null)
			{
				encoding = this._defaultEncoding;
			}
			string toString;
			if (!stateObj.TryReadStringWithEncoding(length, encoding, isPlp, out toString))
			{
				return false;
			}
			value.SetToString(toString);
			return true;
			IL_7E:
			string toString2 = null;
			if (isPlp)
			{
				char[] value2 = null;
				if (!this.TryReadPlpUnicodeChars(ref value2, 0, length >> 1, stateObj, out length))
				{
					return false;
				}
				if (length > 0)
				{
					toString2 = new string(value2, 0, length);
				}
				else
				{
					toString2 = ADP.StrEmpty;
				}
			}
			else if (!stateObj.TryReadString(length >> 1, out toString2))
			{
				return false;
			}
			value.SetToString(toString2);
			return true;
		}

		// Token: 0x06001C2B RID: 7211 RVA: 0x000820F0 File Offset: 0x000802F0
		internal bool TryReadSqlValue(SqlBuffer value, SqlMetaDataPriv md, int length, TdsParserStateObject stateObj)
		{
			bool isPlp = md.metaType.IsPlp;
			byte tdsType = md.tdsType;
			if (isPlp)
			{
				length = int.MaxValue;
			}
			if (tdsType <= 165)
			{
				if (tdsType <= 99)
				{
					switch (tdsType)
					{
					case 34:
					case 37:
					case 45:
						break;
					case 35:
					case 39:
					case 47:
						goto IL_133;
					case 36:
					case 38:
					case 44:
					case 46:
						goto IL_176;
					case 40:
					case 41:
					case 42:
					case 43:
						if (!this.TryReadSqlDateTime(value, tdsType, length, md.scale, stateObj))
						{
							return false;
						}
						return true;
					default:
						if (tdsType != 99)
						{
							goto IL_176;
						}
						goto IL_133;
					}
				}
				else if (tdsType != 106 && tdsType != 108)
				{
					if (tdsType != 165)
					{
						goto IL_176;
					}
				}
				else
				{
					if (!this.TryReadSqlDecimal(value, length, md.precision, md.scale, stateObj))
					{
						return false;
					}
					return true;
				}
			}
			else if (tdsType <= 173)
			{
				if (tdsType == 167)
				{
					goto IL_133;
				}
				if (tdsType != 173)
				{
					goto IL_176;
				}
			}
			else
			{
				if (tdsType == 175 || tdsType == 231)
				{
					goto IL_133;
				}
				switch (tdsType)
				{
				case 239:
					goto IL_133;
				case 240:
					break;
				case 241:
				{
					SqlCachedBuffer sqlCachedBuffer;
					if (!SqlCachedBuffer.TryCreate(md, this, stateObj, out sqlCachedBuffer))
					{
						return false;
					}
					value.SqlCachedBuffer = sqlCachedBuffer;
					return true;
				}
				default:
					goto IL_176;
				}
			}
			byte[] array = null;
			if (isPlp)
			{
				int num;
				if (!stateObj.TryReadPlpBytes(ref array, 0, length, out num))
				{
					return false;
				}
			}
			else
			{
				array = new byte[length];
				if (!stateObj.TryReadByteArray(array, 0, length))
				{
					return false;
				}
			}
			value.SqlBinary = SqlTypeWorkarounds.SqlBinaryCtor(array, true);
			return true;
			IL_133:
			if (!this.TryReadSqlStringValue(value, tdsType, length, md.encoding, isPlp, stateObj))
			{
				return false;
			}
			return true;
			IL_176:
			if (!this.TryReadSqlValueInternal(value, tdsType, length, stateObj))
			{
				return false;
			}
			return true;
		}

		// Token: 0x06001C2C RID: 7212 RVA: 0x00082284 File Offset: 0x00080484
		private bool TryReadSqlDateTime(SqlBuffer value, byte tdsType, int length, byte scale, TdsParserStateObject stateObj)
		{
			byte[] array = new byte[length];
			if (!stateObj.TryReadByteArray(array, 0, length))
			{
				return false;
			}
			switch (tdsType)
			{
			case 40:
				value.SetToDate(array);
				break;
			case 41:
				value.SetToTime(array, length, scale);
				break;
			case 42:
				value.SetToDateTime2(array, length, scale);
				break;
			case 43:
				value.SetToDateTimeOffset(array, length, scale);
				break;
			}
			return true;
		}

		// Token: 0x06001C2D RID: 7213 RVA: 0x000822F0 File Offset: 0x000804F0
		internal bool TryReadSqlValueInternal(SqlBuffer value, byte tdsType, int length, TdsParserStateObject stateObj)
		{
			if (tdsType <= 104)
			{
				byte b;
				if (tdsType <= 62)
				{
					switch (tdsType)
					{
					case 34:
					case 37:
						goto IL_273;
					case 35:
						return true;
					case 36:
					{
						byte[] array = new byte[length];
						if (!stateObj.TryReadByteArray(array, 0, length))
						{
							return false;
						}
						value.SqlGuid = SqlTypeWorkarounds.SqlGuidCtor(array, true);
						return true;
					}
					case 38:
						if (length != 1)
						{
							if (length == 2)
							{
								goto IL_11F;
							}
							if (length == 4)
							{
								goto IL_138;
							}
							goto IL_151;
						}
						break;
					default:
						switch (tdsType)
						{
						case 45:
							goto IL_273;
						case 46:
						case 47:
						case 49:
						case 51:
						case 53:
						case 54:
						case 55:
						case 57:
							return true;
						case 48:
							break;
						case 50:
							goto IL_DC;
						case 52:
							goto IL_11F;
						case 56:
							goto IL_138;
						case 58:
							goto IL_1F7;
						case 59:
							goto IL_16E;
						case 60:
							goto IL_1A6;
						case 61:
							goto IL_226;
						case 62:
							goto IL_188;
						default:
							return true;
						}
						break;
					}
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					value.Byte = b;
					return true;
					IL_11F:
					short @int;
					if (!stateObj.TryReadInt16(out @int))
					{
						return false;
					}
					value.Int16 = @int;
					return true;
					IL_138:
					int num;
					if (!stateObj.TryReadInt32(out num))
					{
						return false;
					}
					value.Int32 = num;
					return true;
				}
				else if (tdsType != 98)
				{
					if (tdsType != 104)
					{
						return true;
					}
				}
				else
				{
					if (!this.TryReadSqlVariant(value, length, stateObj))
					{
						return false;
					}
					return true;
				}
				IL_DC:
				if (!stateObj.TryReadByte(out b))
				{
					return false;
				}
				value.Boolean = (b > 0);
				return true;
			}
			else if (tdsType <= 122)
			{
				switch (tdsType)
				{
				case 109:
					if (length == 4)
					{
						goto IL_16E;
					}
					goto IL_188;
				case 110:
					if (length != 4)
					{
						goto IL_1A6;
					}
					break;
				case 111:
					if (length == 4)
					{
						goto IL_1F7;
					}
					goto IL_226;
				default:
					if (tdsType != 122)
					{
						return true;
					}
					break;
				}
				int num;
				if (!stateObj.TryReadInt32(out num))
				{
					return false;
				}
				value.SetToMoney((long)num);
				return true;
			}
			else if (tdsType != 127)
			{
				if (tdsType != 165 && tdsType != 173)
				{
					return true;
				}
				goto IL_273;
			}
			IL_151:
			long int2;
			if (!stateObj.TryReadInt64(out int2))
			{
				return false;
			}
			value.Int64 = int2;
			return true;
			IL_16E:
			float single;
			if (!stateObj.TryReadSingle(out single))
			{
				return false;
			}
			value.Single = single;
			return true;
			IL_188:
			double @double;
			if (!stateObj.TryReadDouble(out @double))
			{
				return false;
			}
			value.Double = @double;
			return true;
			IL_1A6:
			int num2;
			if (!stateObj.TryReadInt32(out num2))
			{
				return false;
			}
			uint num3;
			if (!stateObj.TryReadUInt32(out num3))
			{
				return false;
			}
			long toMoney = ((long)num2 << 32) + (long)((ulong)num3);
			value.SetToMoney(toMoney);
			return true;
			IL_1F7:
			ushort daypart;
			if (!stateObj.TryReadUInt16(out daypart))
			{
				return false;
			}
			ushort num4;
			if (!stateObj.TryReadUInt16(out num4))
			{
				return false;
			}
			value.SetToDateTime((int)daypart, (int)num4 * SqlDateTime.SQLTicksPerMinute);
			return true;
			IL_226:
			int daypart2;
			if (!stateObj.TryReadInt32(out daypart2))
			{
				return false;
			}
			uint timepart;
			if (!stateObj.TryReadUInt32(out timepart))
			{
				return false;
			}
			value.SetToDateTime(daypart2, (int)timepart);
			return true;
			IL_273:
			byte[] array2 = new byte[length];
			if (!stateObj.TryReadByteArray(array2, 0, length))
			{
				return false;
			}
			value.SqlBinary = SqlTypeWorkarounds.SqlBinaryCtor(array2, true);
			return true;
		}

		// Token: 0x06001C2E RID: 7214 RVA: 0x000825A8 File Offset: 0x000807A8
		internal bool TryReadSqlVariant(SqlBuffer value, int lenTotal, TdsParserStateObject stateObj)
		{
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			ushort num = 0;
			byte b2;
			if (!stateObj.TryReadByte(out b2))
			{
				return false;
			}
			byte propBytes = MetaType.GetSqlDataType((int)b, 0U, 0).PropBytes;
			int num2 = (int)(2 + b2);
			int length = lenTotal - num2;
			if (b <= 127)
			{
				if (b <= 106)
				{
					switch (b)
					{
					case 36:
					case 48:
					case 50:
					case 52:
					case 56:
					case 58:
					case 59:
					case 60:
					case 61:
					case 62:
						goto IL_11E;
					case 37:
					case 38:
					case 39:
					case 44:
					case 45:
					case 46:
					case 47:
					case 49:
					case 51:
					case 53:
					case 54:
					case 55:
					case 57:
						return true;
					case 40:
						if (!this.TryReadSqlDateTime(value, b, length, 0, stateObj))
						{
							return false;
						}
						return true;
					case 41:
					case 42:
					case 43:
					{
						byte scale;
						if (!stateObj.TryReadByte(out scale))
						{
							return false;
						}
						if (b2 > propBytes && !stateObj.TrySkipBytes((int)(b2 - propBytes)))
						{
							return false;
						}
						if (!this.TryReadSqlDateTime(value, b, length, scale, stateObj))
						{
							return false;
						}
						return true;
					}
					default:
						if (b != 106)
						{
							return true;
						}
						break;
					}
				}
				else if (b != 108)
				{
					if (b != 122 && b != 127)
					{
						return true;
					}
					goto IL_11E;
				}
				byte precision;
				if (!stateObj.TryReadByte(out precision))
				{
					return false;
				}
				byte scale2;
				if (!stateObj.TryReadByte(out scale2))
				{
					return false;
				}
				if (b2 > propBytes && !stateObj.TrySkipBytes((int)(b2 - propBytes)))
				{
					return false;
				}
				if (!this.TryReadSqlDecimal(value, 17, precision, scale2, stateObj))
				{
					return false;
				}
				return true;
			}
			else
			{
				if (b <= 173)
				{
					if (b != 165)
					{
						if (b == 167)
						{
							goto IL_18B;
						}
						if (b != 173)
						{
							return true;
						}
					}
					if (!stateObj.TryReadUInt16(out num))
					{
						return false;
					}
					if (b2 > propBytes && !stateObj.TrySkipBytes((int)(b2 - propBytes)))
					{
						return false;
					}
					goto IL_11E;
				}
				else if (b != 175 && b != 231 && b != 239)
				{
					return true;
				}
				IL_18B:
				SqlCollation collation;
				if (!this.TryProcessCollation(stateObj, out collation))
				{
					return false;
				}
				if (!stateObj.TryReadUInt16(out num))
				{
					return false;
				}
				if (b2 > propBytes && !stateObj.TrySkipBytes((int)(b2 - propBytes)))
				{
					return false;
				}
				Encoding encoding = Encoding.GetEncoding(this.GetCodePage(collation, stateObj));
				if (!this.TryReadSqlStringValue(value, b, length, encoding, false, stateObj))
				{
					return false;
				}
				return true;
			}
			IL_11E:
			if (!this.TryReadSqlValueInternal(value, b, length, stateObj))
			{
				return false;
			}
			return true;
		}

		// Token: 0x06001C2F RID: 7215 RVA: 0x000827CC File Offset: 0x000809CC
		internal Task WriteSqlVariantValue(object value, int length, int offset, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			if (ADP.IsNull(value))
			{
				this.WriteInt(0, stateObj);
				this.WriteInt(0, stateObj);
				return null;
			}
			MetaType metaTypeFromValue = MetaType.GetMetaTypeFromValue(value, true);
			if (108 == metaTypeFromValue.TDSType && 8 == length)
			{
				metaTypeFromValue = MetaType.GetMetaTypeFromValue(new SqlMoney((decimal)value), true);
			}
			if (metaTypeFromValue.IsAnsiType)
			{
				length = this.GetEncodingCharLength((string)value, length, 0, this._defaultEncoding);
			}
			this.WriteInt((int)(2 + metaTypeFromValue.PropBytes) + length, stateObj);
			this.WriteInt((int)(2 + metaTypeFromValue.PropBytes) + length, stateObj);
			stateObj.WriteByte(metaTypeFromValue.TDSType);
			stateObj.WriteByte(metaTypeFromValue.PropBytes);
			byte tdstype = metaTypeFromValue.TDSType;
			if (tdstype <= 62)
			{
				if (tdstype <= 41)
				{
					if (tdstype != 36)
					{
						if (tdstype == 41)
						{
							stateObj.WriteByte(metaTypeFromValue.Scale);
							this.WriteTime((TimeSpan)value, metaTypeFromValue.Scale, length, stateObj);
						}
					}
					else
					{
						byte[] b = ((Guid)value).ToByteArray();
						stateObj.WriteByteArray(b, length, 0, true, null);
					}
				}
				else if (tdstype != 43)
				{
					switch (tdstype)
					{
					case 48:
						stateObj.WriteByte((byte)value);
						break;
					case 50:
						if ((bool)value)
						{
							stateObj.WriteByte(1);
						}
						else
						{
							stateObj.WriteByte(0);
						}
						break;
					case 52:
						this.WriteShort((int)((short)value), stateObj);
						break;
					case 56:
						this.WriteInt((int)value, stateObj);
						break;
					case 59:
						this.WriteFloat((float)value, stateObj);
						break;
					case 60:
						this.WriteCurrency((decimal)value, 8, stateObj);
						break;
					case 61:
					{
						TdsDateTime tdsDateTime = MetaType.FromDateTime((DateTime)value, 8);
						this.WriteInt(tdsDateTime.days, stateObj);
						this.WriteInt(tdsDateTime.time, stateObj);
						break;
					}
					case 62:
						this.WriteDouble((double)value, stateObj);
						break;
					}
				}
				else
				{
					stateObj.WriteByte(metaTypeFromValue.Scale);
					this.WriteDateTimeOffset((DateTimeOffset)value, metaTypeFromValue.Scale, length, stateObj);
				}
			}
			else if (tdstype <= 127)
			{
				if (tdstype != 108)
				{
					if (tdstype == 127)
					{
						this.WriteLong((long)value, stateObj);
					}
				}
				else
				{
					stateObj.WriteByte(metaTypeFromValue.Precision);
					stateObj.WriteByte((byte)((decimal.GetBits((decimal)value)[3] & 16711680) >> 16));
					this.WriteDecimal((decimal)value, stateObj);
				}
			}
			else
			{
				if (tdstype == 165)
				{
					byte[] b2 = (byte[])value;
					this.WriteShort(length, stateObj);
					return stateObj.WriteByteArray(b2, length, offset, canAccumulate, null);
				}
				if (tdstype == 167)
				{
					string s = (string)value;
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					this.WriteShort(length, stateObj);
					return this.WriteEncodingChar(s, this._defaultEncoding, stateObj, canAccumulate);
				}
				if (tdstype == 231)
				{
					string s2 = (string)value;
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					this.WriteShort(length, stateObj);
					length >>= 1;
					return this.WriteString(s2, length, offset, stateObj, canAccumulate);
				}
			}
			return null;
		}

		// Token: 0x06001C30 RID: 7216 RVA: 0x00082B60 File Offset: 0x00080D60
		internal Task WriteSqlVariantDataRowValue(object value, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			if (value == null || DBNull.Value == value)
			{
				this.WriteInt(0, stateObj);
				return null;
			}
			MetaType metaTypeFromValue = MetaType.GetMetaTypeFromValue(value, true);
			int num = 0;
			if (metaTypeFromValue.IsAnsiType)
			{
				num = this.GetEncodingCharLength((string)value, num, 0, this._defaultEncoding);
			}
			byte tdstype = metaTypeFromValue.TDSType;
			if (tdstype <= 62)
			{
				if (tdstype <= 41)
				{
					if (tdstype != 36)
					{
						if (tdstype == 41)
						{
							this.WriteSqlVariantHeader(8, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
							stateObj.WriteByte(metaTypeFromValue.Scale);
							this.WriteTime((TimeSpan)value, metaTypeFromValue.Scale, 5, stateObj);
						}
					}
					else
					{
						byte[] array = ((Guid)value).ToByteArray();
						num = array.Length;
						this.WriteSqlVariantHeader(18, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						stateObj.WriteByteArray(array, num, 0, true, null);
					}
				}
				else if (tdstype != 43)
				{
					switch (tdstype)
					{
					case 48:
						this.WriteSqlVariantHeader(3, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						stateObj.WriteByte((byte)value);
						break;
					case 50:
						this.WriteSqlVariantHeader(3, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						if ((bool)value)
						{
							stateObj.WriteByte(1);
						}
						else
						{
							stateObj.WriteByte(0);
						}
						break;
					case 52:
						this.WriteSqlVariantHeader(4, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteShort((int)((short)value), stateObj);
						break;
					case 56:
						this.WriteSqlVariantHeader(6, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteInt((int)value, stateObj);
						break;
					case 59:
						this.WriteSqlVariantHeader(6, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteFloat((float)value, stateObj);
						break;
					case 60:
						this.WriteSqlVariantHeader(10, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteCurrency((decimal)value, 8, stateObj);
						break;
					case 61:
					{
						TdsDateTime tdsDateTime = MetaType.FromDateTime((DateTime)value, 8);
						this.WriteSqlVariantHeader(10, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteInt(tdsDateTime.days, stateObj);
						this.WriteInt(tdsDateTime.time, stateObj);
						break;
					}
					case 62:
						this.WriteSqlVariantHeader(10, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteDouble((double)value, stateObj);
						break;
					}
				}
				else
				{
					this.WriteSqlVariantHeader(13, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
					stateObj.WriteByte(metaTypeFromValue.Scale);
					this.WriteDateTimeOffset((DateTimeOffset)value, metaTypeFromValue.Scale, 10, stateObj);
				}
			}
			else if (tdstype <= 127)
			{
				if (tdstype != 108)
				{
					if (tdstype == 127)
					{
						this.WriteSqlVariantHeader(10, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
						this.WriteLong((long)value, stateObj);
					}
				}
				else
				{
					this.WriteSqlVariantHeader(21, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
					stateObj.WriteByte(metaTypeFromValue.Precision);
					stateObj.WriteByte((byte)((decimal.GetBits((decimal)value)[3] & 16711680) >> 16));
					this.WriteDecimal((decimal)value, stateObj);
				}
			}
			else
			{
				if (tdstype == 165)
				{
					byte[] array2 = (byte[])value;
					num = array2.Length;
					this.WriteSqlVariantHeader(4 + num, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
					this.WriteShort(num, stateObj);
					return stateObj.WriteByteArray(array2, num, 0, canAccumulate, null);
				}
				if (tdstype == 167)
				{
					string text = (string)value;
					num = text.Length;
					this.WriteSqlVariantHeader(9 + num, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					this.WriteShort(num, stateObj);
					return this.WriteEncodingChar(text, this._defaultEncoding, stateObj, canAccumulate);
				}
				if (tdstype == 231)
				{
					string text2 = (string)value;
					num = text2.Length * 2;
					this.WriteSqlVariantHeader(9 + num, metaTypeFromValue.TDSType, metaTypeFromValue.PropBytes, stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					this.WriteShort(num, stateObj);
					num >>= 1;
					return this.WriteString(text2, num, 0, stateObj, canAccumulate);
				}
			}
			return null;
		}

		// Token: 0x06001C31 RID: 7217 RVA: 0x00082FD7 File Offset: 0x000811D7
		internal void WriteSqlVariantHeader(int length, byte tdstype, byte propbytes, TdsParserStateObject stateObj)
		{
			this.WriteInt(length, stateObj);
			stateObj.WriteByte(tdstype);
			stateObj.WriteByte(propbytes);
		}

		// Token: 0x06001C32 RID: 7218 RVA: 0x00082FF4 File Offset: 0x000811F4
		internal void WriteSqlVariantDateTime2(DateTime value, TdsParserStateObject stateObj)
		{
			SmiMetaData defaultDateTime = SmiMetaData.DefaultDateTime2;
			this.WriteSqlVariantHeader((int)(defaultDateTime.MaxLength + 3L), 42, 1, stateObj);
			stateObj.WriteByte(defaultDateTime.Scale);
			this.WriteDateTime2(value, defaultDateTime.Scale, (int)defaultDateTime.MaxLength, stateObj);
		}

		// Token: 0x06001C33 RID: 7219 RVA: 0x0008303C File Offset: 0x0008123C
		internal void WriteSqlVariantDate(DateTime value, TdsParserStateObject stateObj)
		{
			SmiMetaData defaultDate = SmiMetaData.DefaultDate;
			this.WriteSqlVariantHeader((int)(defaultDate.MaxLength + 2L), 40, 0, stateObj);
			this.WriteDate(value, stateObj);
		}

		// Token: 0x06001C34 RID: 7220 RVA: 0x0008306C File Offset: 0x0008126C
		private void WriteSqlMoney(SqlMoney value, int length, TdsParserStateObject stateObj)
		{
			int[] bits = decimal.GetBits(value.Value);
			bool flag = (bits[3] & int.MinValue) != 0;
			long num = (long)((ulong)bits[1] << 32 | (ulong)bits[0]);
			if (flag)
			{
				num = -num;
			}
			if (length != 4)
			{
				this.WriteInt((int)(num >> 32), stateObj);
				this.WriteInt((int)num, stateObj);
				return;
			}
			decimal value2 = value.Value;
			if (value2 < TdsEnums.SQL_SMALL_MONEY_MIN || value2 > TdsEnums.SQL_SMALL_MONEY_MAX)
			{
				throw SQL.MoneyOverflow(value2.ToString(CultureInfo.InvariantCulture));
			}
			this.WriteInt((int)num, stateObj);
		}

		// Token: 0x06001C35 RID: 7221 RVA: 0x000830FC File Offset: 0x000812FC
		private void WriteCurrency(decimal value, int length, TdsParserStateObject stateObj)
		{
			SqlMoney sqlMoney = new SqlMoney(value);
			int[] bits = decimal.GetBits(sqlMoney.Value);
			bool flag = (bits[3] & int.MinValue) != 0;
			long num = (long)((ulong)bits[1] << 32 | (ulong)bits[0]);
			if (flag)
			{
				num = -num;
			}
			if (length != 4)
			{
				this.WriteInt((int)(num >> 32), stateObj);
				this.WriteInt((int)num, stateObj);
				return;
			}
			if (value < TdsEnums.SQL_SMALL_MONEY_MIN || value > TdsEnums.SQL_SMALL_MONEY_MAX)
			{
				throw SQL.MoneyOverflow(value.ToString(CultureInfo.InvariantCulture));
			}
			this.WriteInt((int)num, stateObj);
		}

		// Token: 0x06001C36 RID: 7222 RVA: 0x0008318C File Offset: 0x0008138C
		private void WriteDate(DateTime value, TdsParserStateObject stateObj)
		{
			long v = (long)value.Subtract(DateTime.MinValue).Days;
			this.WritePartialLong(v, 3, stateObj);
		}

		// Token: 0x06001C37 RID: 7223 RVA: 0x000831B8 File Offset: 0x000813B8
		private void WriteTime(TimeSpan value, byte scale, int length, TdsParserStateObject stateObj)
		{
			if (0L > value.Ticks || value.Ticks >= 864000000000L)
			{
				throw SQL.TimeOverflow(value.ToString());
			}
			long v = value.Ticks / TdsEnums.TICKS_FROM_SCALE[(int)scale];
			this.WritePartialLong(v, length, stateObj);
		}

		// Token: 0x06001C38 RID: 7224 RVA: 0x00083210 File Offset: 0x00081410
		private void WriteDateTime2(DateTime value, byte scale, int length, TdsParserStateObject stateObj)
		{
			long v = value.TimeOfDay.Ticks / TdsEnums.TICKS_FROM_SCALE[(int)scale];
			this.WritePartialLong(v, length - 3, stateObj);
			this.WriteDate(value, stateObj);
		}

		// Token: 0x06001C39 RID: 7225 RVA: 0x0008324C File Offset: 0x0008144C
		private void WriteDateTimeOffset(DateTimeOffset value, byte scale, int length, TdsParserStateObject stateObj)
		{
			this.WriteDateTime2(value.UtcDateTime, scale, length - 2, stateObj);
			short num = (short)value.Offset.TotalMinutes;
			stateObj.WriteByte((byte)(num & 255));
			stateObj.WriteByte((byte)(num >> 8 & 255));
		}

		// Token: 0x06001C3A RID: 7226 RVA: 0x000832A0 File Offset: 0x000814A0
		private bool TryReadSqlDecimal(SqlBuffer value, int length, byte precision, byte scale, TdsParserStateObject stateObj)
		{
			byte b;
			if (!stateObj.TryReadByte(out b))
			{
				return false;
			}
			bool positive = 1 == b;
			checked
			{
				length--;
				int[] bits;
				if (!this.TryReadDecimalBits(length, stateObj, out bits))
				{
					return false;
				}
				value.SetToDecimal(precision, scale, positive, bits);
				return true;
			}
		}

		// Token: 0x06001C3B RID: 7227 RVA: 0x000832E0 File Offset: 0x000814E0
		private bool TryReadDecimalBits(int length, TdsParserStateObject stateObj, out int[] bits)
		{
			bits = stateObj._decimalBits;
			if (bits == null)
			{
				bits = new int[4];
				stateObj._decimalBits = bits;
			}
			else
			{
				for (int i = 0; i < bits.Length; i++)
				{
					bits[i] = 0;
				}
			}
			int num = length >> 2;
			for (int i = 0; i < num; i++)
			{
				if (!stateObj.TryReadInt32(out bits[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001C3C RID: 7228 RVA: 0x00083342 File Offset: 0x00081542
		internal static SqlDecimal AdjustSqlDecimalScale(SqlDecimal d, int newScale)
		{
			if ((int)d.Scale != newScale)
			{
				return SqlDecimal.AdjustScale(d, newScale - (int)d.Scale, false);
			}
			return d;
		}

		// Token: 0x06001C3D RID: 7229 RVA: 0x00083360 File Offset: 0x00081560
		internal static decimal AdjustDecimalScale(decimal value, int newScale)
		{
			int num = (decimal.GetBits(value)[3] & 16711680) >> 16;
			if (newScale != num)
			{
				SqlDecimal n = new SqlDecimal(value);
				n = SqlDecimal.AdjustScale(n, newScale - num, false);
				return n.Value;
			}
			return value;
		}

		// Token: 0x06001C3E RID: 7230 RVA: 0x000833A0 File Offset: 0x000815A0
		internal void WriteSqlDecimal(SqlDecimal d, TdsParserStateObject stateObj)
		{
			if (d.IsPositive)
			{
				stateObj.WriteByte(1);
			}
			else
			{
				stateObj.WriteByte(0);
			}
			uint i;
			uint i2;
			uint i3;
			uint i4;
			SqlTypeWorkarounds.SqlDecimalExtractData(d, out i, out i2, out i3, out i4);
			this.WriteUnsignedInt(i, stateObj);
			this.WriteUnsignedInt(i2, stateObj);
			this.WriteUnsignedInt(i3, stateObj);
			this.WriteUnsignedInt(i4, stateObj);
		}

		// Token: 0x06001C3F RID: 7231 RVA: 0x000833F4 File Offset: 0x000815F4
		private void WriteDecimal(decimal value, TdsParserStateObject stateObj)
		{
			stateObj._decimalBits = decimal.GetBits(value);
			if ((ulong)-2147483648 == (ulong)((long)stateObj._decimalBits[3] & (long)((ulong)-2147483648)))
			{
				stateObj.WriteByte(0);
			}
			else
			{
				stateObj.WriteByte(1);
			}
			this.WriteInt(stateObj._decimalBits[0], stateObj);
			this.WriteInt(stateObj._decimalBits[1], stateObj);
			this.WriteInt(stateObj._decimalBits[2], stateObj);
			this.WriteInt(0, stateObj);
		}

		// Token: 0x06001C40 RID: 7232 RVA: 0x0008346A File Offset: 0x0008166A
		private void WriteIdentifier(string s, TdsParserStateObject stateObj)
		{
			if (s != null)
			{
				stateObj.WriteByte(checked((byte)s.Length));
				this.WriteString(s, stateObj, true);
				return;
			}
			stateObj.WriteByte(0);
		}

		// Token: 0x06001C41 RID: 7233 RVA: 0x0008348E File Offset: 0x0008168E
		private void WriteIdentifierWithShortLength(string s, TdsParserStateObject stateObj)
		{
			if (s != null)
			{
				this.WriteShort((int)(checked((short)s.Length)), stateObj);
				this.WriteString(s, stateObj, true);
				return;
			}
			this.WriteShort(0, stateObj);
		}

		// Token: 0x06001C42 RID: 7234 RVA: 0x000834B4 File Offset: 0x000816B4
		private Task WriteString(string s, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			return this.WriteString(s, s.Length, 0, stateObj, canAccumulate);
		}

		// Token: 0x06001C43 RID: 7235 RVA: 0x000834C8 File Offset: 0x000816C8
		internal Task WriteCharArray(char[] carr, int length, int offset, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			int num = 2 * length;
			if (num < stateObj._outBuff.Length - stateObj._outBytesUsed)
			{
				TdsParser.CopyCharsToBytes(carr, offset, stateObj._outBuff, stateObj._outBytesUsed, length);
				stateObj._outBytesUsed += num;
				return null;
			}
			if (stateObj._bTmp == null || stateObj._bTmp.Length < num)
			{
				stateObj._bTmp = new byte[num];
			}
			TdsParser.CopyCharsToBytes(carr, offset, stateObj._bTmp, 0, length);
			return stateObj.WriteByteArray(stateObj._bTmp, num, 0, canAccumulate, null);
		}

		// Token: 0x06001C44 RID: 7236 RVA: 0x0008355C File Offset: 0x0008175C
		internal Task WriteString(string s, int length, int offset, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			int num = 2 * length;
			if (num < stateObj._outBuff.Length - stateObj._outBytesUsed)
			{
				TdsParser.CopyStringToBytes(s, offset, stateObj._outBuff, stateObj._outBytesUsed, length);
				stateObj._outBytesUsed += num;
				return null;
			}
			if (stateObj._bTmp == null || stateObj._bTmp.Length < num)
			{
				stateObj._bTmp = new byte[num];
			}
			TdsParser.CopyStringToBytes(s, offset, stateObj._bTmp, 0, length);
			return stateObj.WriteByteArray(stateObj._bTmp, num, 0, canAccumulate, null);
		}

		// Token: 0x06001C45 RID: 7237 RVA: 0x000835ED File Offset: 0x000817ED
		private static void CopyCharsToBytes(char[] source, int sourceOffset, byte[] dest, int destOffset, int charLength)
		{
			Buffer.BlockCopy(source, sourceOffset, dest, destOffset, charLength * 2);
		}

		// Token: 0x06001C46 RID: 7238 RVA: 0x000835FC File Offset: 0x000817FC
		private static void CopyStringToBytes(string source, int sourceOffset, byte[] dest, int destOffset, int charLength)
		{
			Encoding.Unicode.GetBytes(source, sourceOffset, charLength, dest, destOffset);
		}

		// Token: 0x06001C47 RID: 7239 RVA: 0x0008360F File Offset: 0x0008180F
		private Task WriteEncodingChar(string s, Encoding encoding, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			return this.WriteEncodingChar(s, s.Length, 0, encoding, stateObj, canAccumulate);
		}

		// Token: 0x06001C48 RID: 7240 RVA: 0x00083624 File Offset: 0x00081824
		private Task WriteEncodingChar(string s, int numChars, int offset, Encoding encoding, TdsParserStateObject stateObj, bool canAccumulate = true)
		{
			if (encoding == null)
			{
				encoding = this._defaultEncoding;
			}
			char[] array = s.ToCharArray(offset, numChars);
			int num = stateObj._outBuff.Length - stateObj._outBytesUsed;
			if (numChars <= num && encoding.GetMaxByteCount(array.Length) <= num)
			{
				int bytes = encoding.GetBytes(array, 0, array.Length, stateObj._outBuff, stateObj._outBytesUsed);
				stateObj._outBytesUsed += bytes;
				return null;
			}
			byte[] bytes2 = encoding.GetBytes(array, 0, numChars);
			return stateObj.WriteByteArray(bytes2, bytes2.Length, 0, canAccumulate, null);
		}

		// Token: 0x06001C49 RID: 7241 RVA: 0x000836B0 File Offset: 0x000818B0
		internal int GetEncodingCharLength(string value, int numChars, int charOffset, Encoding encoding)
		{
			if (value == null || value == ADP.StrEmpty)
			{
				return 0;
			}
			if (encoding == null)
			{
				if (this._defaultEncoding == null)
				{
					this.ThrowUnsupportedCollationEncountered(null);
				}
				encoding = this._defaultEncoding;
			}
			char[] chars = value.ToCharArray(charOffset, numChars);
			return encoding.GetByteCount(chars, 0, numChars);
		}

		// Token: 0x06001C4A RID: 7242 RVA: 0x00083700 File Offset: 0x00081900
		internal bool TryGetDataLength(SqlMetaDataPriv colmeta, TdsParserStateObject stateObj, out ulong length)
		{
			if (colmeta.metaType.IsPlp)
			{
				return stateObj.TryReadPlpLength(true, out length);
			}
			int num;
			if (!this.TryGetTokenLength(colmeta.tdsType, stateObj, out num))
			{
				length = 0UL;
				return false;
			}
			length = (ulong)((long)num);
			return true;
		}

		// Token: 0x06001C4B RID: 7243 RVA: 0x00083740 File Offset: 0x00081940
		internal bool TryGetTokenLength(byte token, TdsParserStateObject stateObj, out int tokenLength)
		{
			if (token == 174)
			{
				tokenLength = -1;
				return true;
			}
			if (token == 228)
			{
				return stateObj.TryReadInt32(out tokenLength);
			}
			if (token == 240)
			{
				tokenLength = -1;
				return true;
			}
			if (token == 172)
			{
				tokenLength = -1;
				return true;
			}
			if (token != 241)
			{
				int num = (int)(token & 48);
				if (num <= 16)
				{
					if (num != 0)
					{
						if (num != 16)
						{
							goto IL_D1;
						}
						tokenLength = 0;
						return true;
					}
				}
				else if (num != 32)
				{
					if (num == 48)
					{
						tokenLength = (1 << ((token & 12) >> 2) & 255);
						return true;
					}
					goto IL_D1;
				}
				if ((token & 128) != 0)
				{
					ushort num2;
					if (!stateObj.TryReadUInt16(out num2))
					{
						tokenLength = 0;
						return false;
					}
					tokenLength = (int)num2;
					return true;
				}
				else
				{
					if ((token & 12) == 0)
					{
						return stateObj.TryReadInt32(out tokenLength);
					}
					byte b;
					if (!stateObj.TryReadByte(out b))
					{
						tokenLength = 0;
						return false;
					}
					tokenLength = (int)b;
					return true;
				}
				IL_D1:
				tokenLength = 0;
				return true;
			}
			ushort num3;
			if (!stateObj.TryReadUInt16(out num3))
			{
				tokenLength = 0;
				return false;
			}
			tokenLength = (int)num3;
			return true;
		}

		// Token: 0x06001C4C RID: 7244 RVA: 0x00083824 File Offset: 0x00081A24
		private void ProcessAttention(TdsParserStateObject stateObj)
		{
			if (this._state == TdsParserState.Closed || this._state == TdsParserState.Broken)
			{
				return;
			}
			stateObj.StoreErrorAndWarningForAttention();
			try
			{
				this.Run(RunBehavior.Attention, null, null, null, stateObj);
			}
			catch (Exception e)
			{
				if (!ADP.IsCatchableExceptionType(e))
				{
					throw;
				}
				this._state = TdsParserState.Broken;
				this._connHandler.BreakConnection();
				throw;
			}
			stateObj.RestoreErrorAndWarningAfterAttention();
		}

		// Token: 0x06001C4D RID: 7245 RVA: 0x0008388C File Offset: 0x00081A8C
		private static int StateValueLength(int dataLen)
		{
			if (dataLen >= 255)
			{
				return dataLen + 5;
			}
			return dataLen + 1;
		}

		// Token: 0x06001C4E RID: 7246 RVA: 0x000838A0 File Offset: 0x00081AA0
		internal int WriteSessionRecoveryFeatureRequest(SessionData reconnectData, bool write)
		{
			int num = 1;
			if (write)
			{
				this._physicalStateObj.WriteByte(1);
			}
			if (reconnectData == null)
			{
				if (write)
				{
					this.WriteInt(0, this._physicalStateObj);
				}
				num += 4;
			}
			else
			{
				int num2 = 0;
				num2 += 1 + 2 * TdsParserStaticMethods.NullAwareStringLength(reconnectData._initialDatabase);
				num2 += 1 + 2 * TdsParserStaticMethods.NullAwareStringLength(reconnectData._initialLanguage);
				num2 += ((reconnectData._initialCollation == null) ? 1 : 6);
				for (int i = 0; i < 256; i++)
				{
					if (reconnectData._initialState[i] != null)
					{
						num2 += 1 + TdsParser.StateValueLength(reconnectData._initialState[i].Length);
					}
				}
				int num3 = 0;
				num3 += 1 + 2 * ((reconnectData._initialDatabase == reconnectData._database) ? 0 : TdsParserStaticMethods.NullAwareStringLength(reconnectData._database));
				num3 += 1 + 2 * ((reconnectData._initialLanguage == reconnectData._language) ? 0 : TdsParserStaticMethods.NullAwareStringLength(reconnectData._language));
				num3 += ((reconnectData._collation != null && !SqlCollation.AreSame(reconnectData._collation, reconnectData._initialCollation)) ? 6 : 1);
				bool[] array = new bool[256];
				for (int j = 0; j < 256; j++)
				{
					if (reconnectData._delta[j] != null)
					{
						array[j] = true;
						if (reconnectData._initialState[j] != null && reconnectData._initialState[j].Length == reconnectData._delta[j]._dataLength)
						{
							array[j] = false;
							for (int k = 0; k < reconnectData._delta[j]._dataLength; k++)
							{
								if (reconnectData._initialState[j][k] != reconnectData._delta[j]._data[k])
								{
									array[j] = true;
									break;
								}
							}
						}
						if (array[j])
						{
							num3 += 1 + TdsParser.StateValueLength(reconnectData._delta[j]._dataLength);
						}
					}
				}
				if (write)
				{
					this.WriteInt(8 + num2 + num3, this._physicalStateObj);
					this.WriteInt(num2, this._physicalStateObj);
					this.WriteIdentifier(reconnectData._initialDatabase, this._physicalStateObj);
					this.WriteCollation(reconnectData._initialCollation, this._physicalStateObj);
					this.WriteIdentifier(reconnectData._initialLanguage, this._physicalStateObj);
					for (int l = 0; l < 256; l++)
					{
						if (reconnectData._initialState[l] != null)
						{
							this._physicalStateObj.WriteByte((byte)l);
							if (reconnectData._initialState[l].Length < 255)
							{
								this._physicalStateObj.WriteByte((byte)reconnectData._initialState[l].Length);
							}
							else
							{
								this._physicalStateObj.WriteByte(byte.MaxValue);
								this.WriteInt(reconnectData._initialState[l].Length, this._physicalStateObj);
							}
							this._physicalStateObj.WriteByteArray(reconnectData._initialState[l], reconnectData._initialState[l].Length, 0, true, null);
						}
					}
					this.WriteInt(num3, this._physicalStateObj);
					this.WriteIdentifier((reconnectData._database != reconnectData._initialDatabase) ? reconnectData._database : null, this._physicalStateObj);
					this.WriteCollation(SqlCollation.AreSame(reconnectData._initialCollation, reconnectData._collation) ? null : reconnectData._collation, this._physicalStateObj);
					this.WriteIdentifier((reconnectData._language != reconnectData._initialLanguage) ? reconnectData._language : null, this._physicalStateObj);
					for (int m = 0; m < 256; m++)
					{
						if (array[m])
						{
							this._physicalStateObj.WriteByte((byte)m);
							if (reconnectData._delta[m]._dataLength < 255)
							{
								this._physicalStateObj.WriteByte((byte)reconnectData._delta[m]._dataLength);
							}
							else
							{
								this._physicalStateObj.WriteByte(byte.MaxValue);
								this.WriteInt(reconnectData._delta[m]._dataLength, this._physicalStateObj);
							}
							this._physicalStateObj.WriteByteArray(reconnectData._delta[m]._data, reconnectData._delta[m]._dataLength, 0, true, null);
						}
					}
				}
				num += num2 + num3 + 12;
			}
			return num;
		}

		// Token: 0x06001C4F RID: 7247 RVA: 0x00083CC8 File Offset: 0x00081EC8
		internal int WriteFedAuthFeatureRequest(FederatedAuthenticationFeatureExtensionData fedAuthFeatureData, bool write)
		{
			int num = 0;
			if (fedAuthFeatureData.libraryType == TdsEnums.FedAuthLibrary.SecurityToken)
			{
				num = 5 + fedAuthFeatureData.accessToken.Length;
			}
			int result = num + 5;
			if (write)
			{
				this._physicalStateObj.WriteByte(2);
				byte b = 0;
				if (fedAuthFeatureData.libraryType == TdsEnums.FedAuthLibrary.SecurityToken)
				{
					b |= 2;
				}
				b |= (fedAuthFeatureData.fedAuthRequiredPreLoginResponse ? 1 : 0);
				this.WriteInt(num, this._physicalStateObj);
				this._physicalStateObj.WriteByte(b);
				if (fedAuthFeatureData.libraryType == TdsEnums.FedAuthLibrary.SecurityToken)
				{
					this.WriteInt(fedAuthFeatureData.accessToken.Length, this._physicalStateObj);
					this._physicalStateObj.WriteByteArray(fedAuthFeatureData.accessToken, fedAuthFeatureData.accessToken.Length, 0, true, null);
				}
			}
			return result;
		}

		// Token: 0x06001C50 RID: 7248 RVA: 0x00083D74 File Offset: 0x00081F74
		internal int WriteGlobalTransactionsFeatureRequest(bool write)
		{
			int result = 5;
			if (write)
			{
				this._physicalStateObj.WriteByte(5);
				this.WriteInt(0, this._physicalStateObj);
			}
			return result;
		}

		// Token: 0x06001C51 RID: 7249 RVA: 0x00083D94 File Offset: 0x00081F94
		internal void TdsLogin(SqlLogin rec, TdsEnums.FeatureExtension requestedFeatures, SessionData recoverySessionData, FederatedAuthenticationFeatureExtensionData? fedAuthFeatureExtensionData)
		{
			this._physicalStateObj.SetTimeoutSeconds(rec.timeout);
			this._connHandler.TimeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.LoginBegin);
			this._connHandler.TimeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.ProcessConnectionAuth);
			byte[] array = null;
			byte[] array2 = null;
			bool flag = requestedFeatures > TdsEnums.FeatureExtension.None;
			string text;
			int num;
			if (rec.credential != null)
			{
				text = rec.credential.UserId;
				num = rec.credential.Password.Length * 2;
			}
			else
			{
				text = rec.userName;
				array = TdsParserStaticMethods.ObfuscatePassword(rec.password);
				num = array.Length;
			}
			int num2;
			if (rec.newSecurePassword != null)
			{
				num2 = rec.newSecurePassword.Length * 2;
			}
			else
			{
				array2 = TdsParserStaticMethods.ObfuscatePassword(rec.newPassword);
				num2 = array2.Length;
			}
			this._physicalStateObj._outputMessageType = 16;
			int num3 = 94;
			string text2 = "Core .Net SqlClient Data Provider";
			byte[] b;
			uint num4;
			int v;
			checked
			{
				num3 += (rec.hostName.Length + rec.applicationName.Length + rec.serverName.Length + text2.Length + rec.language.Length + rec.database.Length + rec.attachDBFilename.Length) * 2;
				if (flag)
				{
					num3 += 4;
				}
				b = null;
				num4 = 0U;
				if (!rec.useSSPI && !this._connHandler._federatedAuthenticationRequested)
				{
					num3 += text.Length * 2 + num + num2;
				}
				else if (rec.useSSPI)
				{
					b = new byte[TdsParser.s_maxSSPILength];
					num4 = TdsParser.s_maxSSPILength;
					this._physicalStateObj.SniContext = SniContext.Snix_LoginSspi;
					this.SSPIData(null, 0U, ref b, ref num4);
					if (num4 > 2147483647U)
					{
						throw SQL.InvalidSSPIPacketSize();
					}
					this._physicalStateObj.SniContext = SniContext.Snix_Login;
					num3 += (int)num4;
				}
				v = num3;
			}
			if (flag)
			{
				if ((requestedFeatures & TdsEnums.FeatureExtension.SessionRecovery) != TdsEnums.FeatureExtension.None)
				{
					num3 += this.WriteSessionRecoveryFeatureRequest(recoverySessionData, false);
				}
				if ((requestedFeatures & TdsEnums.FeatureExtension.GlobalTransactions) != TdsEnums.FeatureExtension.None)
				{
					num3 += this.WriteGlobalTransactionsFeatureRequest(false);
				}
				if ((requestedFeatures & TdsEnums.FeatureExtension.FedAuth) != TdsEnums.FeatureExtension.None)
				{
					num3 += this.WriteFedAuthFeatureRequest(fedAuthFeatureExtensionData.Value, false);
				}
				num3++;
			}
			try
			{
				this.WriteInt(num3, this._physicalStateObj);
				if (recoverySessionData == null)
				{
					this.WriteInt(1946157060, this._physicalStateObj);
				}
				else
				{
					this.WriteUnsignedInt(recoverySessionData._tdsVersion, this._physicalStateObj);
				}
				this.WriteInt(rec.packetSize, this._physicalStateObj);
				this.WriteInt(100663296, this._physicalStateObj);
				this.WriteInt(TdsParserStaticMethods.GetCurrentProcessIdForTdsLoginOnly(), this._physicalStateObj);
				this.WriteInt(0, this._physicalStateObj);
				int num5 = 0;
				num5 |= 32;
				num5 |= 64;
				num5 |= 128;
				num5 |= 256;
				num5 |= 512;
				if (rec.useReplication)
				{
					num5 |= 12288;
				}
				if (rec.useSSPI)
				{
					num5 |= 32768;
				}
				if (rec.readOnlyIntent)
				{
					num5 |= 2097152;
				}
				if (!string.IsNullOrEmpty(rec.newPassword) || (rec.newSecurePassword != null && rec.newSecurePassword.Length != 0))
				{
					num5 |= 16777216;
				}
				if (rec.userInstance)
				{
					num5 |= 67108864;
				}
				if (flag)
				{
					num5 |= 268435456;
				}
				this.WriteInt(num5, this._physicalStateObj);
				this.WriteInt(0, this._physicalStateObj);
				this.WriteInt(0, this._physicalStateObj);
				int num6 = 94;
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.hostName.Length, this._physicalStateObj);
				num6 += rec.hostName.Length * 2;
				if (!rec.useSSPI)
				{
					this.WriteShort(num6, this._physicalStateObj);
					this.WriteShort(text.Length, this._physicalStateObj);
					num6 += text.Length * 2;
					this.WriteShort(num6, this._physicalStateObj);
					this.WriteShort(num / 2, this._physicalStateObj);
					num6 += num;
				}
				else
				{
					this.WriteShort(0, this._physicalStateObj);
					this.WriteShort(0, this._physicalStateObj);
					this.WriteShort(0, this._physicalStateObj);
					this.WriteShort(0, this._physicalStateObj);
				}
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.applicationName.Length, this._physicalStateObj);
				num6 += rec.applicationName.Length * 2;
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.serverName.Length, this._physicalStateObj);
				num6 += rec.serverName.Length * 2;
				this.WriteShort(num6, this._physicalStateObj);
				if (flag)
				{
					this.WriteShort(4, this._physicalStateObj);
					num6 += 4;
				}
				else
				{
					this.WriteShort(0, this._physicalStateObj);
				}
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(text2.Length, this._physicalStateObj);
				num6 += text2.Length * 2;
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.language.Length, this._physicalStateObj);
				num6 += rec.language.Length * 2;
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.database.Length, this._physicalStateObj);
				num6 += rec.database.Length * 2;
				if (TdsParser.s_nicAddress == null)
				{
					TdsParser.s_nicAddress = TdsParserStaticMethods.GetNetworkPhysicalAddressForTdsLoginOnly();
				}
				this._physicalStateObj.WriteByteArray(TdsParser.s_nicAddress, TdsParser.s_nicAddress.Length, 0, true, null);
				this.WriteShort(num6, this._physicalStateObj);
				if (rec.useSSPI)
				{
					this.WriteShort((int)num4, this._physicalStateObj);
					num6 += (int)num4;
				}
				else
				{
					this.WriteShort(0, this._physicalStateObj);
				}
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(rec.attachDBFilename.Length, this._physicalStateObj);
				num6 += rec.attachDBFilename.Length * 2;
				this.WriteShort(num6, this._physicalStateObj);
				this.WriteShort(num2 / 2, this._physicalStateObj);
				this.WriteInt(0, this._physicalStateObj);
				this.WriteString(rec.hostName, this._physicalStateObj, true);
				if (!rec.useSSPI)
				{
					this.WriteString(text, this._physicalStateObj, true);
					if (rec.credential != null)
					{
						this._physicalStateObj.WriteSecureString(rec.credential.Password);
					}
					else
					{
						this._physicalStateObj.WriteByteArray(array, num, 0, true, null);
					}
				}
				this.WriteString(rec.applicationName, this._physicalStateObj, true);
				this.WriteString(rec.serverName, this._physicalStateObj, true);
				if (flag)
				{
					this.WriteInt(v, this._physicalStateObj);
				}
				this.WriteString(text2, this._physicalStateObj, true);
				this.WriteString(rec.language, this._physicalStateObj, true);
				this.WriteString(rec.database, this._physicalStateObj, true);
				if (rec.useSSPI)
				{
					this._physicalStateObj.WriteByteArray(b, (int)num4, 0, true, null);
				}
				this.WriteString(rec.attachDBFilename, this._physicalStateObj, true);
				if (!rec.useSSPI)
				{
					if (rec.newSecurePassword != null)
					{
						this._physicalStateObj.WriteSecureString(rec.newSecurePassword);
					}
					else
					{
						this._physicalStateObj.WriteByteArray(array2, num2, 0, true, null);
					}
				}
				if (flag)
				{
					if ((requestedFeatures & TdsEnums.FeatureExtension.SessionRecovery) != TdsEnums.FeatureExtension.None)
					{
						num3 += this.WriteSessionRecoveryFeatureRequest(recoverySessionData, true);
					}
					if ((requestedFeatures & TdsEnums.FeatureExtension.GlobalTransactions) != TdsEnums.FeatureExtension.None)
					{
						this.WriteGlobalTransactionsFeatureRequest(true);
					}
					if ((requestedFeatures & TdsEnums.FeatureExtension.FedAuth) != TdsEnums.FeatureExtension.None)
					{
						this.WriteFedAuthFeatureRequest(fedAuthFeatureExtensionData.Value, true);
					}
					this._physicalStateObj.WriteByte(byte.MaxValue);
				}
			}
			catch (Exception e)
			{
				if (ADP.IsCatchableExceptionType(e))
				{
					this._physicalStateObj._outputPacketNumber = 1;
					this._physicalStateObj.ResetBuffer();
				}
				throw;
			}
			this._physicalStateObj.WritePacket(1, false);
			this._physicalStateObj.ResetSecurePasswordsInformation();
			this._physicalStateObj._pendingData = true;
			this._physicalStateObj._messageStatus = 0;
		}

		// Token: 0x06001C52 RID: 7250 RVA: 0x0008459C File Offset: 0x0008279C
		private void SSPIData(byte[] receivedBuff, uint receivedLength, ref byte[] sendBuff, ref uint sendLength)
		{
			this.SNISSPIData(receivedBuff, receivedLength, ref sendBuff, ref sendLength);
		}

		// Token: 0x06001C53 RID: 7251 RVA: 0x000845AC File Offset: 0x000827AC
		private void SNISSPIData(byte[] receivedBuff, uint receivedLength, ref byte[] sendBuff, ref uint sendLength)
		{
			if (TdsParserStateObjectFactory.UseManagedSNI)
			{
				try
				{
					this._physicalStateObj.GenerateSspiClientContext(receivedBuff, receivedLength, ref sendBuff, ref sendLength, this._sniSpnBuffer);
					return;
				}
				catch (Exception ex)
				{
					this.SSPIError(ex.Message + Environment.NewLine + ex.StackTrace, "GenClientContext");
					return;
				}
			}
			if (receivedBuff == null)
			{
				receivedLength = 0U;
			}
			if (this._physicalStateObj.GenerateSspiClientContext(receivedBuff, receivedLength, ref sendBuff, ref sendLength, this._sniSpnBuffer) != 0U)
			{
				this.SSPIError(SQLMessage.SSPIGenerateError(), "GenClientContext");
			}
		}

		// Token: 0x06001C54 RID: 7252 RVA: 0x0008463C File Offset: 0x0008283C
		private void ProcessSSPI(int receivedLength)
		{
			SniContext sniContext = this._physicalStateObj.SniContext;
			this._physicalStateObj.SniContext = SniContext.Snix_ProcessSspi;
			byte[] array = new byte[receivedLength];
			if (!this._physicalStateObj.TryReadByteArray(array, 0, receivedLength))
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			byte[] b = new byte[TdsParser.s_maxSSPILength];
			uint len = TdsParser.s_maxSSPILength;
			this.SSPIData(array, (uint)receivedLength, ref b, ref len);
			this._physicalStateObj.WriteByteArray(b, (int)len, 0, true, null);
			this._physicalStateObj._outputMessageType = 17;
			this._physicalStateObj.WritePacket(1, false);
			this._physicalStateObj.SniContext = sniContext;
		}

		// Token: 0x06001C55 RID: 7253 RVA: 0x000846D8 File Offset: 0x000828D8
		private void SSPIError(string error, string procedure)
		{
			this._physicalStateObj.AddError(new SqlError(0, 0, 11, this._server, error, procedure, 0, null));
			this.ThrowExceptionAndWarning(this._physicalStateObj, false, false);
		}

		// Token: 0x06001C56 RID: 7254 RVA: 0x00084714 File Offset: 0x00082914
		internal byte[] GetDTCAddress(int timeout, TdsParserStateObject stateObj)
		{
			byte[] array = null;
			using (SqlDataReader sqlDataReader = this.TdsExecuteTransactionManagerRequest(null, TdsEnums.TransactionManagerRequestType.GetDTCAddress, null, TdsEnums.TransactionManagerIsolationLevel.Unspecified, timeout, null, stateObj, true))
			{
				if (sqlDataReader != null && sqlDataReader.Read())
				{
					long bytes = sqlDataReader.GetBytes(0, 0L, null, 0, 0);
					if (bytes <= 2147483647L)
					{
						int num = (int)bytes;
						array = new byte[num];
						sqlDataReader.GetBytes(0, 0L, array, 0, num);
					}
				}
			}
			return array;
		}

		// Token: 0x06001C57 RID: 7255 RVA: 0x00084788 File Offset: 0x00082988
		internal void PropagateDistributedTransaction(byte[] buffer, int timeout, TdsParserStateObject stateObj)
		{
			this.TdsExecuteTransactionManagerRequest(buffer, TdsEnums.TransactionManagerRequestType.Propagate, null, TdsEnums.TransactionManagerIsolationLevel.Unspecified, timeout, null, stateObj, true);
		}

		// Token: 0x06001C58 RID: 7256 RVA: 0x000847A4 File Offset: 0x000829A4
		internal SqlDataReader TdsExecuteTransactionManagerRequest(byte[] buffer, TdsEnums.TransactionManagerRequestType request, string transactionName, TdsEnums.TransactionManagerIsolationLevel isoLevel, int timeout, SqlInternalTransaction transaction, TdsParserStateObject stateObj, bool isDelegateControlRequest)
		{
			if (TdsParserState.Broken == this.State || this.State == TdsParserState.Closed)
			{
				return null;
			}
			bool threadHasParserLockForClose = this._connHandler.ThreadHasParserLockForClose;
			if (!threadHasParserLockForClose)
			{
				this._connHandler._parserLock.Wait(false);
				this._connHandler.ThreadHasParserLockForClose = true;
			}
			bool asyncWrite = this._asyncWrite;
			SqlDataReader result;
			try
			{
				this._asyncWrite = false;
				if (!isDelegateControlRequest)
				{
					this._connHandler.CheckEnlistedTransactionBinding();
				}
				stateObj._outputMessageType = 14;
				stateObj.SetTimeoutSeconds(timeout);
				stateObj.SniContext = SniContext.Snix_Execute;
				this.WriteInt(22, stateObj);
				this.WriteInt(18, stateObj);
				this.WriteMarsHeaderData(stateObj, this._currentTransaction);
				this.WriteShort((int)((short)request), stateObj);
				bool flag = false;
				switch (request)
				{
				case TdsEnums.TransactionManagerRequestType.GetDTCAddress:
					this.WriteShort(0, stateObj);
					flag = true;
					break;
				case TdsEnums.TransactionManagerRequestType.Propagate:
					if (buffer != null)
					{
						this.WriteShort(buffer.Length, stateObj);
						stateObj.WriteByteArray(buffer, buffer.Length, 0, true, null);
					}
					else
					{
						this.WriteShort(0, stateObj);
					}
					break;
				case TdsEnums.TransactionManagerRequestType.Begin:
					if (this._currentTransaction != transaction)
					{
						this.PendingTransaction = transaction;
					}
					stateObj.WriteByte((byte)isoLevel);
					stateObj.WriteByte((byte)(transactionName.Length * 2));
					this.WriteString(transactionName, stateObj, true);
					break;
				case TdsEnums.TransactionManagerRequestType.Commit:
					stateObj.WriteByte(0);
					stateObj.WriteByte(0);
					break;
				case TdsEnums.TransactionManagerRequestType.Rollback:
					stateObj.WriteByte((byte)(transactionName.Length * 2));
					this.WriteString(transactionName, stateObj, true);
					stateObj.WriteByte(0);
					break;
				case TdsEnums.TransactionManagerRequestType.Save:
					stateObj.WriteByte((byte)(transactionName.Length * 2));
					this.WriteString(transactionName, stateObj, true);
					break;
				}
				stateObj.WritePacket(1, false);
				stateObj._pendingData = true;
				stateObj._messageStatus = 0;
				SqlDataReader sqlDataReader = null;
				stateObj.SniContext = SniContext.Snix_Read;
				if (flag)
				{
					sqlDataReader = new SqlDataReader(null, CommandBehavior.Default);
					sqlDataReader.Bind(stateObj);
					_SqlMetaDataSet metaData = sqlDataReader.MetaData;
				}
				else
				{
					this.Run(RunBehavior.UntilDone, null, null, null, stateObj);
				}
				if ((request == TdsEnums.TransactionManagerRequestType.Begin || request == TdsEnums.TransactionManagerRequestType.Propagate) && (transaction == null || transaction.TransactionId != this._retainedTransactionId))
				{
					this._retainedTransactionId = 0L;
				}
				result = sqlDataReader;
			}
			catch (Exception e)
			{
				if (!ADP.IsCatchableExceptionType(e))
				{
					throw;
				}
				this.FailureCleanup(stateObj, e);
				throw;
			}
			finally
			{
				this._pendingTransaction = null;
				this._asyncWrite = asyncWrite;
				if (!threadHasParserLockForClose)
				{
					this._connHandler.ThreadHasParserLockForClose = false;
					this._connHandler._parserLock.Release();
				}
			}
			return result;
		}

		// Token: 0x06001C59 RID: 7257 RVA: 0x00084A48 File Offset: 0x00082C48
		internal void FailureCleanup(TdsParserStateObject stateObj, Exception e)
		{
			int outputPacketNumber = (int)stateObj._outputPacketNumber;
			if (stateObj.HasOpenResult)
			{
				stateObj.DecrementOpenResultCount();
			}
			stateObj.ResetBuffer();
			stateObj._outputPacketNumber = 1;
			if (outputPacketNumber != 1 && this._state == TdsParserState.OpenLoggedIn)
			{
				bool threadHasParserLockForClose = this._connHandler.ThreadHasParserLockForClose;
				try
				{
					this._connHandler.ThreadHasParserLockForClose = true;
					stateObj.SendAttention(false);
					this.ProcessAttention(stateObj);
				}
				finally
				{
					this._connHandler.ThreadHasParserLockForClose = threadHasParserLockForClose;
				}
			}
		}

		// Token: 0x06001C5A RID: 7258 RVA: 0x00084AC8 File Offset: 0x00082CC8
		internal Task TdsExecuteSQLBatch(string text, int timeout, SqlNotificationRequest notificationRequest, TdsParserStateObject stateObj, bool sync, bool callerHasConnectionLock = false)
		{
			if (TdsParserState.Broken == this.State || this.State == TdsParserState.Closed)
			{
				return null;
			}
			if (stateObj.BcpLock)
			{
				throw SQL.ConnectionLockedForBcpEvent();
			}
			bool flag = !callerHasConnectionLock && !this._connHandler.ThreadHasParserLockForClose;
			bool flag2 = false;
			if (flag)
			{
				this._connHandler._parserLock.Wait(!sync);
				flag2 = true;
			}
			this._asyncWrite = !sync;
			Task result;
			try
			{
				if (this._state == TdsParserState.Closed || this._state == TdsParserState.Broken)
				{
					throw ADP.ClosedConnectionError();
				}
				this._connHandler.CheckEnlistedTransactionBinding();
				stateObj.SetTimeoutSeconds(timeout);
				stateObj.SniContext = SniContext.Snix_Execute;
				this.WriteRPCBatchHeaders(stateObj, notificationRequest);
				stateObj._outputMessageType = 1;
				this.WriteString(text, text.Length, 0, stateObj, true);
				Task task = stateObj.ExecuteFlush();
				if (task == null)
				{
					stateObj.SniContext = SniContext.Snix_Read;
					result = null;
				}
				else
				{
					bool taskReleaseConnectionLock = flag2;
					flag2 = false;
					result = task.ContinueWith(delegate(Task t)
					{
						try
						{
							if (t.IsFaulted)
							{
								this.FailureCleanup(stateObj, t.Exception.InnerException);
								throw t.Exception.InnerException;
							}
							stateObj.SniContext = SniContext.Snix_Read;
						}
						finally
						{
							if (taskReleaseConnectionLock)
							{
								this._connHandler._parserLock.Release();
							}
						}
					}, TaskScheduler.Default);
				}
			}
			catch (Exception e)
			{
				if (!ADP.IsCatchableExceptionType(e))
				{
					throw;
				}
				this.FailureCleanup(stateObj, e);
				throw;
			}
			finally
			{
				if (flag2)
				{
					this._connHandler._parserLock.Release();
				}
			}
			return result;
		}

		// Token: 0x06001C5B RID: 7259 RVA: 0x00084C48 File Offset: 0x00082E48
		internal Task TdsExecuteRPC(_SqlRPC[] rpcArray, int timeout, bool inSchema, SqlNotificationRequest notificationRequest, TdsParserStateObject stateObj, bool isCommandProc, bool sync = true, TaskCompletionSource<object> completion = null, int startRpc = 0, int startParam = 0)
		{
			bool flag = completion == null;
			bool flag2 = false;
			Task result2;
			try
			{
				if (flag)
				{
					this._connHandler._parserLock.Wait(!sync);
					flag2 = true;
				}
				try
				{
					if (TdsParserState.Broken == this.State || this.State == TdsParserState.Closed)
					{
						throw ADP.ClosedConnectionError();
					}
					if (flag)
					{
						this._asyncWrite = !sync;
						this._connHandler.CheckEnlistedTransactionBinding();
						stateObj.SetTimeoutSeconds(timeout);
						stateObj.SniContext = SniContext.Snix_Execute;
						if (this._isYukon)
						{
							this.WriteRPCBatchHeaders(stateObj, notificationRequest);
						}
						stateObj._outputMessageType = 3;
					}
					Action<Exception> <>9__1;
					Action<Task> <>9__2;
					int num5;
					int ii;
					for (ii = startRpc; ii < rpcArray.Length; ii = num5 + 1)
					{
						_SqlRPC sqlRPC = rpcArray[ii];
						if (startParam == 0 || ii > startRpc)
						{
							if (sqlRPC.ProcID != 0)
							{
								this.WriteShort(65535, stateObj);
								this.WriteShort((int)((short)sqlRPC.ProcID), stateObj);
							}
							else
							{
								int length = sqlRPC.rpcName.Length;
								this.WriteShort(length, stateObj);
								this.WriteString(sqlRPC.rpcName, length, 0, stateObj, true);
							}
							this.WriteShort((int)((short)sqlRPC.options), stateObj);
						}
						SqlParameter[] parameters = sqlRPC.parameters;
						int i;
						for (i = ((ii == startRpc) ? startParam : 0); i < parameters.Length; i = num5 + 1)
						{
							SqlParameter sqlParameter = parameters[i];
							if (sqlParameter == null)
							{
								break;
							}
							sqlParameter.Validate(i, isCommandProc);
							MetaType internalMetaType = sqlParameter.InternalMetaType;
							if (internalMetaType.IsNewKatmaiType)
							{
								this.WriteSmiParameter(sqlParameter, i, (sqlRPC.paramoptions[i] & 2) > 0, stateObj);
							}
							else
							{
								if ((!this._isYukon && !internalMetaType.Is80Supported) || (!this._isKatmai && !internalMetaType.Is90Supported))
								{
									throw ADP.VersionDoesNotSupportDataType(internalMetaType.TypeName);
								}
								object obj = null;
								bool flag3 = true;
								bool flag4 = false;
								bool flag5 = false;
								if (sqlParameter.Direction == ParameterDirection.Output)
								{
									flag4 = sqlParameter.ParameterIsSqlType;
									sqlParameter.Value = null;
									sqlParameter.ParameterIsSqlType = flag4;
								}
								else
								{
									obj = sqlParameter.GetCoercedValue();
									flag3 = sqlParameter.IsNull;
									if (!flag3)
									{
										flag4 = sqlParameter.CoercedValueIsSqlType;
										flag5 = sqlParameter.CoercedValueIsDataFeed;
									}
								}
								this.WriteParameterName(sqlParameter.ParameterNameFixed, stateObj);
								stateObj.WriteByte(sqlRPC.paramoptions[i]);
								int num = internalMetaType.IsSizeInCharacters ? (sqlParameter.GetParameterSize() * 2) : sqlParameter.GetParameterSize();
								int num2;
								if (internalMetaType.TDSType != 240)
								{
									num2 = sqlParameter.GetActualSize();
								}
								else
								{
									num2 = 0;
								}
								byte b = 0;
								byte b2 = 0;
								if (internalMetaType.SqlDbType == SqlDbType.Decimal)
								{
									b = sqlParameter.GetActualPrecision();
									b2 = sqlParameter.GetActualScale();
									if (b > 38)
									{
										throw SQL.PrecisionValueOutOfRange(b);
									}
									if (!flag3)
									{
										if (flag4)
										{
											obj = TdsParser.AdjustSqlDecimalScale((SqlDecimal)obj, (int)b2);
											if (b != 0 && b < ((SqlDecimal)obj).Precision)
											{
												throw ADP.ParameterValueOutOfRange((SqlDecimal)obj);
											}
										}
										else
										{
											obj = TdsParser.AdjustDecimalScale((decimal)obj, (int)b2);
											SqlDecimal sqlDecimal = new SqlDecimal((decimal)obj);
											if (b != 0 && b < sqlDecimal.Precision)
											{
												throw ADP.ParameterValueOutOfRange((decimal)obj);
											}
										}
									}
								}
								stateObj.WriteByte(internalMetaType.NullableType);
								if (internalMetaType.TDSType == 98)
								{
									this.WriteSqlVariantValue(flag4 ? MetaType.GetComValueFromSqlVariant(obj) : obj, sqlParameter.GetActualSize(), sqlParameter.Offset, stateObj, true);
								}
								else
								{
									int num3 = 0;
									int num4 = 0;
									if (internalMetaType.IsAnsiType)
									{
										if (!flag3 && !flag5)
										{
											string value;
											if (flag4)
											{
												if (obj is SqlString)
												{
													value = ((SqlString)obj).Value;
												}
												else
												{
													value = new string(((SqlChars)obj).Value);
												}
											}
											else
											{
												value = (string)obj;
											}
											num3 = this.GetEncodingCharLength(value, num2, sqlParameter.Offset, this._defaultEncoding);
										}
										if (internalMetaType.IsPlp)
										{
											this.WriteShort(65535, stateObj);
										}
										else
										{
											num4 = ((num > num3) ? num : num3);
											if (num4 == 0)
											{
												if (internalMetaType.IsNCharType)
												{
													num4 = 2;
												}
												else
												{
													num4 = 1;
												}
											}
											this.WriteParameterVarLen(internalMetaType, num4, false, stateObj, false);
										}
									}
									else if (internalMetaType.SqlDbType == SqlDbType.Timestamp)
									{
										this.WriteParameterVarLen(internalMetaType, 8, false, stateObj, false);
									}
									else if (internalMetaType.SqlDbType == SqlDbType.Udt)
									{
										byte[] array = null;
										Format format = Format.Native;
										if (!flag3)
										{
											array = this._connHandler.Connection.GetBytes(obj, out format, out num4);
											num = array.Length;
											if (num < 0 || (num >= 65535 && num4 != -1))
											{
												throw new IndexOutOfRangeException();
											}
										}
										BitConverter.GetBytes((long)num);
										if (string.IsNullOrEmpty(sqlParameter.UdtTypeName))
										{
											throw SQL.MustSetUdtTypeNameForUdtParams();
										}
										string[] array2 = SqlParameter.ParseTypeName(sqlParameter.UdtTypeName, true);
										if (!string.IsNullOrEmpty(array2[0]) && 255 < array2[0].Length)
										{
											throw ADP.ArgumentOutOfRange("names");
										}
										if (!string.IsNullOrEmpty(array2[1]) && 255 < array2[array2.Length - 2].Length)
										{
											throw ADP.ArgumentOutOfRange("names");
										}
										if (255 < array2[2].Length)
										{
											throw ADP.ArgumentOutOfRange("names");
										}
										this.WriteUDTMetaData(obj, array2[0], array2[1], array2[2], stateObj);
										if (!flag3)
										{
											this.WriteUnsignedLong((ulong)((long)array.Length), stateObj);
											if (array.Length != 0)
											{
												this.WriteInt(array.Length, stateObj);
												stateObj.WriteByteArray(array, array.Length, 0, true, null);
											}
											this.WriteInt(0, stateObj);
											goto IL_CD6;
										}
										this.WriteUnsignedLong(ulong.MaxValue, stateObj);
										goto IL_CD6;
									}
									else if (internalMetaType.IsPlp)
									{
										if (internalMetaType.SqlDbType != SqlDbType.Xml)
										{
											this.WriteShort(65535, stateObj);
										}
									}
									else if (!internalMetaType.IsVarTime && internalMetaType.SqlDbType != SqlDbType.Date)
									{
										num4 = ((num > num2) ? num : num2);
										if (num4 == 0 && this._isYukon)
										{
											if (internalMetaType.IsNCharType)
											{
												num4 = 2;
											}
											else
											{
												num4 = 1;
											}
										}
										this.WriteParameterVarLen(internalMetaType, num4, false, stateObj, false);
									}
									if (internalMetaType.SqlDbType == SqlDbType.Decimal)
									{
										if (b == 0)
										{
											stateObj.WriteByte(29);
										}
										else
										{
											stateObj.WriteByte(b);
										}
										stateObj.WriteByte(b2);
									}
									else if (internalMetaType.IsVarTime)
									{
										stateObj.WriteByte(sqlParameter.GetActualScale());
									}
									if (this._isYukon && internalMetaType.SqlDbType == SqlDbType.Xml)
									{
										if ((sqlParameter.XmlSchemaCollectionDatabase != null && sqlParameter.XmlSchemaCollectionDatabase != ADP.StrEmpty) || (sqlParameter.XmlSchemaCollectionOwningSchema != null && sqlParameter.XmlSchemaCollectionOwningSchema != ADP.StrEmpty) || (sqlParameter.XmlSchemaCollectionName != null && sqlParameter.XmlSchemaCollectionName != ADP.StrEmpty))
										{
											stateObj.WriteByte(1);
											if (sqlParameter.XmlSchemaCollectionDatabase != null && sqlParameter.XmlSchemaCollectionDatabase != ADP.StrEmpty)
											{
												int length = sqlParameter.XmlSchemaCollectionDatabase.Length;
												stateObj.WriteByte((byte)length);
												this.WriteString(sqlParameter.XmlSchemaCollectionDatabase, length, 0, stateObj, true);
											}
											else
											{
												stateObj.WriteByte(0);
											}
											if (sqlParameter.XmlSchemaCollectionOwningSchema != null && sqlParameter.XmlSchemaCollectionOwningSchema != ADP.StrEmpty)
											{
												int length = sqlParameter.XmlSchemaCollectionOwningSchema.Length;
												stateObj.WriteByte((byte)length);
												this.WriteString(sqlParameter.XmlSchemaCollectionOwningSchema, length, 0, stateObj, true);
											}
											else
											{
												stateObj.WriteByte(0);
											}
											if (sqlParameter.XmlSchemaCollectionName != null && sqlParameter.XmlSchemaCollectionName != ADP.StrEmpty)
											{
												int length = sqlParameter.XmlSchemaCollectionName.Length;
												this.WriteShort((int)((short)length), stateObj);
												this.WriteString(sqlParameter.XmlSchemaCollectionName, length, 0, stateObj, true);
											}
											else
											{
												this.WriteShort(0, stateObj);
											}
										}
										else
										{
											stateObj.WriteByte(0);
										}
									}
									else if (internalMetaType.IsCharType)
									{
										SqlCollation sqlCollation = (sqlParameter.Collation != null) ? sqlParameter.Collation : this._defaultCollation;
										this.WriteUnsignedInt(sqlCollation.info, stateObj);
										stateObj.WriteByte(sqlCollation.sortId);
									}
									if (num3 == 0)
									{
										this.WriteParameterVarLen(internalMetaType, num2, flag3, stateObj, flag5);
									}
									else
									{
										this.WriteParameterVarLen(internalMetaType, num3, flag3, stateObj, flag5);
									}
									Task task = null;
									if (!flag3)
									{
										if (flag4)
										{
											task = this.WriteSqlValue(obj, internalMetaType, num2, num3, sqlParameter.Offset, stateObj);
										}
										else
										{
											task = this.WriteValue(obj, internalMetaType, sqlParameter.GetActualScale(), num2, num3, sqlParameter.Offset, stateObj, sqlParameter.Size, flag5);
										}
									}
									if (!sync)
									{
										if (task == null)
										{
											task = stateObj.WaitForAccumulatedWrites();
										}
										if (task != null)
										{
											Task task2 = null;
											if (completion == null)
											{
												completion = new TaskCompletionSource<object>();
												task2 = completion.Task;
											}
											Task task3 = task;
											TaskCompletionSource<object> completion2 = completion;
											Action onSuccess = delegate()
											{
												this.TdsExecuteRPC(rpcArray, timeout, inSchema, notificationRequest, stateObj, isCommandProc, sync, completion, ii, i + 1);
											};
											SqlInternalConnectionTds connHandler = this._connHandler;
											Action<Exception> onFailure;
											if ((onFailure = <>9__1) == null)
											{
												onFailure = (<>9__1 = delegate(Exception exc)
												{
													this.TdsExecuteRPC_OnFailure(exc, stateObj);
												});
											}
											AsyncHelper.ContinueTask(task3, completion2, onSuccess, connHandler, onFailure, null, null, null);
											if (flag2)
											{
												Task task4 = task2;
												Action<Task> continuationAction;
												if ((continuationAction = <>9__2) == null)
												{
													continuationAction = (<>9__2 = delegate(Task _)
													{
														this._connHandler._parserLock.Release();
													});
												}
												task4.ContinueWith(continuationAction, TaskScheduler.Default);
												flag2 = false;
											}
											return task2;
										}
									}
								}
							}
							IL_CD6:
							num5 = i;
						}
						if (ii < rpcArray.Length - 1)
						{
							if (this._isYukon)
							{
								stateObj.WriteByte(byte.MaxValue);
							}
							else
							{
								stateObj.WriteByte(128);
							}
						}
						num5 = ii;
					}
					Task task5 = stateObj.ExecuteFlush();
					if (task5 != null)
					{
						Task result = null;
						if (completion == null)
						{
							completion = new TaskCompletionSource<object>();
							result = completion.Task;
						}
						bool taskReleaseConnectionLock = flag2;
						task5.ContinueWith(delegate(Task tsk)
						{
							this.ExecuteFlushTaskCallback(tsk, stateObj, completion, taskReleaseConnectionLock);
						}, TaskScheduler.Default);
						flag2 = false;
						return result;
					}
				}
				catch (Exception e)
				{
					if (!ADP.IsCatchableExceptionType(e))
					{
						throw;
					}
					this.FailureCleanup(stateObj, e);
					throw;
				}
				this.FinalizeExecuteRPC(stateObj);
				if (completion != null)
				{
					completion.SetResult(null);
				}
				result2 = null;
			}
			catch (Exception exception)
			{
				this.FinalizeExecuteRPC(stateObj);
				if (completion == null)
				{
					throw;
				}
				completion.SetException(exception);
				result2 = null;
			}
			finally
			{
				if (flag2)
				{
					this._connHandler._parserLock.Release();
				}
			}
			return result2;
		}

		// Token: 0x06001C5C RID: 7260 RVA: 0x00085B20 File Offset: 0x00083D20
		private void FinalizeExecuteRPC(TdsParserStateObject stateObj)
		{
			stateObj.SniContext = SniContext.Snix_Read;
			this._asyncWrite = false;
		}

		// Token: 0x06001C5D RID: 7261 RVA: 0x00085B31 File Offset: 0x00083D31
		private void TdsExecuteRPC_OnFailure(Exception exc, TdsParserStateObject stateObj)
		{
			this.FailureCleanup(stateObj, exc);
		}

		// Token: 0x06001C5E RID: 7262 RVA: 0x00085B3C File Offset: 0x00083D3C
		private void ExecuteFlushTaskCallback(Task tsk, TdsParserStateObject stateObj, TaskCompletionSource<object> completion, bool releaseConnectionLock)
		{
			try
			{
				this.FinalizeExecuteRPC(stateObj);
				if (tsk.Exception != null)
				{
					Exception innerException = tsk.Exception.InnerException;
					try
					{
						this.FailureCleanup(stateObj, tsk.Exception);
					}
					catch (Exception innerException)
					{
					}
					completion.SetException(innerException);
				}
				else
				{
					completion.SetResult(null);
				}
			}
			finally
			{
				if (releaseConnectionLock)
				{
					this._connHandler._parserLock.Release();
				}
			}
		}

		// Token: 0x06001C5F RID: 7263 RVA: 0x00085BBC File Offset: 0x00083DBC
		private void WriteParameterName(string parameterName, TdsParserStateObject stateObj)
		{
			if (!string.IsNullOrEmpty(parameterName))
			{
				int num = parameterName.Length & 255;
				stateObj.WriteByte((byte)num);
				this.WriteString(parameterName, num, 0, stateObj, true);
				return;
			}
			stateObj.WriteByte(0);
		}

		// Token: 0x06001C60 RID: 7264 RVA: 0x00085BFC File Offset: 0x00083DFC
		private void WriteSmiParameter(SqlParameter param, int paramIndex, bool sendDefault, TdsParserStateObject stateObj)
		{
			ParameterPeekAheadValue peekAhead;
			SmiParameterMetaData smiParameterMetaData = param.MetaDataForSmi(out peekAhead);
			if (!this._isKatmai)
			{
				throw ADP.VersionDoesNotSupportDataType(MetaType.GetMetaTypeFromSqlDbType(smiParameterMetaData.SqlDbType, smiParameterMetaData.IsMultiValued).TypeName);
			}
			object value;
			ExtendedClrTypeCode typeCode;
			if (sendDefault)
			{
				if (SqlDbType.Structured == smiParameterMetaData.SqlDbType && smiParameterMetaData.IsMultiValued)
				{
					value = TdsParser.s_tvpEmptyValue;
					typeCode = ExtendedClrTypeCode.IEnumerableOfSqlDataRecord;
				}
				else
				{
					value = null;
					typeCode = ExtendedClrTypeCode.DBNull;
				}
			}
			else if (param.Direction == ParameterDirection.Output)
			{
				bool parameterIsSqlType = param.ParameterIsSqlType;
				param.Value = null;
				value = null;
				typeCode = ExtendedClrTypeCode.DBNull;
				param.ParameterIsSqlType = parameterIsSqlType;
			}
			else
			{
				value = param.GetCoercedValue();
				typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(smiParameterMetaData.SqlDbType, smiParameterMetaData.IsMultiValued, value, null);
			}
			this.WriteSmiParameterMetaData(smiParameterMetaData, sendDefault, stateObj);
			TdsParameterSetter setters = new TdsParameterSetter(stateObj, smiParameterMetaData);
			ValueUtilsSmi.SetCompatibleValueV200(new SmiEventSink_Default(), setters, 0, smiParameterMetaData, value, typeCode, param.Offset, (0 < param.Size) ? param.Size : -1, peekAhead);
		}

		// Token: 0x06001C61 RID: 7265 RVA: 0x00085CDC File Offset: 0x00083EDC
		private void WriteSmiParameterMetaData(SmiParameterMetaData metaData, bool sendDefault, TdsParserStateObject stateObj)
		{
			byte b = 0;
			if (ParameterDirection.Output == metaData.Direction || ParameterDirection.InputOutput == metaData.Direction)
			{
				b |= 1;
			}
			if (sendDefault)
			{
				b |= 2;
			}
			this.WriteParameterName(metaData.Name, stateObj);
			stateObj.WriteByte(b);
			this.WriteSmiTypeInfo(metaData, stateObj);
		}

		// Token: 0x06001C62 RID: 7266 RVA: 0x00085D28 File Offset: 0x00083F28
		private void WriteSmiTypeInfo(SmiExtendedMetaData metaData, TdsParserStateObject stateObj)
		{
			checked
			{
				switch (metaData.SqlDbType)
				{
				case SqlDbType.BigInt:
					stateObj.WriteByte(38);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Binary:
					stateObj.WriteByte(173);
					this.WriteUnsignedShort((ushort)metaData.MaxLength, stateObj);
					return;
				case SqlDbType.Bit:
					stateObj.WriteByte(104);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Char:
					stateObj.WriteByte(175);
					this.WriteUnsignedShort((ushort)metaData.MaxLength, stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.DateTime:
					stateObj.WriteByte(111);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Decimal:
					stateObj.WriteByte(108);
					stateObj.WriteByte((byte)MetaType.MetaDecimal.FixedLength);
					stateObj.WriteByte((metaData.Precision == 0) ? 1 : metaData.Precision);
					stateObj.WriteByte(metaData.Scale);
					return;
				case SqlDbType.Float:
					stateObj.WriteByte(109);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Image:
					stateObj.WriteByte(165);
					this.WriteUnsignedShort(ushort.MaxValue, stateObj);
					return;
				case SqlDbType.Int:
					stateObj.WriteByte(38);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Money:
					stateObj.WriteByte(110);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.NChar:
					stateObj.WriteByte(239);
					this.WriteUnsignedShort((ushort)(metaData.MaxLength * 2L), stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.NText:
					stateObj.WriteByte(231);
					this.WriteUnsignedShort(ushort.MaxValue, stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.NVarChar:
					stateObj.WriteByte(231);
					if (-1L == metaData.MaxLength)
					{
						this.WriteUnsignedShort(ushort.MaxValue, stateObj);
					}
					else
					{
						this.WriteUnsignedShort((ushort)(metaData.MaxLength * 2L), stateObj);
					}
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.Real:
					stateObj.WriteByte(109);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.UniqueIdentifier:
					stateObj.WriteByte(36);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.SmallDateTime:
					stateObj.WriteByte(111);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.SmallInt:
					stateObj.WriteByte(38);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.SmallMoney:
					stateObj.WriteByte(110);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.Text:
					stateObj.WriteByte(167);
					this.WriteUnsignedShort(ushort.MaxValue, stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.Timestamp:
					stateObj.WriteByte(173);
					this.WriteShort((int)metaData.MaxLength, stateObj);
					return;
				case SqlDbType.TinyInt:
					stateObj.WriteByte(38);
					stateObj.WriteByte((byte)metaData.MaxLength);
					return;
				case SqlDbType.VarBinary:
					stateObj.WriteByte(165);
					this.WriteUnsignedShort(unchecked((ushort)metaData.MaxLength), stateObj);
					return;
				case SqlDbType.VarChar:
					stateObj.WriteByte(167);
					this.WriteUnsignedShort(unchecked((ushort)metaData.MaxLength), stateObj);
					this.WriteUnsignedInt(this._defaultCollation.info, stateObj);
					stateObj.WriteByte(this._defaultCollation.sortId);
					return;
				case SqlDbType.Variant:
					stateObj.WriteByte(98);
					this.WriteInt((int)metaData.MaxLength, stateObj);
					return;
				case (SqlDbType)24:
				case (SqlDbType)26:
				case (SqlDbType)27:
				case (SqlDbType)28:
					break;
				case SqlDbType.Xml:
					stateObj.WriteByte(241);
					if (string.IsNullOrEmpty(metaData.TypeSpecificNamePart1) && string.IsNullOrEmpty(metaData.TypeSpecificNamePart2) && string.IsNullOrEmpty(metaData.TypeSpecificNamePart3))
					{
						stateObj.WriteByte(0);
						return;
					}
					stateObj.WriteByte(1);
					this.WriteIdentifier(metaData.TypeSpecificNamePart1, stateObj);
					this.WriteIdentifier(metaData.TypeSpecificNamePart2, stateObj);
					this.WriteIdentifierWithShortLength(metaData.TypeSpecificNamePart3, stateObj);
					return;
				case SqlDbType.Udt:
					stateObj.WriteByte(240);
					this.WriteIdentifier(metaData.TypeSpecificNamePart1, stateObj);
					this.WriteIdentifier(metaData.TypeSpecificNamePart2, stateObj);
					this.WriteIdentifier(metaData.TypeSpecificNamePart3, stateObj);
					return;
				case SqlDbType.Structured:
					if (metaData.IsMultiValued)
					{
						this.WriteTvpTypeInfo(metaData, stateObj);
						return;
					}
					break;
				case SqlDbType.Date:
					stateObj.WriteByte(40);
					return;
				case SqlDbType.Time:
					stateObj.WriteByte(41);
					stateObj.WriteByte(metaData.Scale);
					return;
				case SqlDbType.DateTime2:
					stateObj.WriteByte(42);
					stateObj.WriteByte(metaData.Scale);
					return;
				case SqlDbType.DateTimeOffset:
					stateObj.WriteByte(43);
					stateObj.WriteByte(metaData.Scale);
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x06001C63 RID: 7267 RVA: 0x00086210 File Offset: 0x00084410
		private void WriteTvpTypeInfo(SmiExtendedMetaData metaData, TdsParserStateObject stateObj)
		{
			stateObj.WriteByte(243);
			this.WriteIdentifier(metaData.TypeSpecificNamePart1, stateObj);
			this.WriteIdentifier(metaData.TypeSpecificNamePart2, stateObj);
			this.WriteIdentifier(metaData.TypeSpecificNamePart3, stateObj);
			if (metaData.FieldMetaData.Count == 0)
			{
				this.WriteUnsignedShort(ushort.MaxValue, stateObj);
			}
			else
			{
				this.WriteUnsignedShort(checked((ushort)metaData.FieldMetaData.Count), stateObj);
				SmiDefaultFieldsProperty smiDefaultFieldsProperty = (SmiDefaultFieldsProperty)metaData.ExtendedProperties[SmiPropertySelector.DefaultFields];
				for (int i = 0; i < metaData.FieldMetaData.Count; i++)
				{
					this.WriteTvpColumnMetaData(metaData.FieldMetaData[i], smiDefaultFieldsProperty[i], stateObj);
				}
				this.WriteTvpOrderUnique(metaData, stateObj);
			}
			stateObj.WriteByte(0);
		}

		// Token: 0x06001C64 RID: 7268 RVA: 0x000862D0 File Offset: 0x000844D0
		private void WriteTvpColumnMetaData(SmiExtendedMetaData md, bool isDefault, TdsParserStateObject stateObj)
		{
			if (SqlDbType.Timestamp == md.SqlDbType)
			{
				this.WriteUnsignedInt(80U, stateObj);
			}
			else
			{
				this.WriteUnsignedInt(0U, stateObj);
			}
			ushort num = 1;
			if (isDefault)
			{
				num |= 512;
			}
			this.WriteUnsignedShort(num, stateObj);
			this.WriteSmiTypeInfo(md, stateObj);
			this.WriteIdentifier(null, stateObj);
		}

		// Token: 0x06001C65 RID: 7269 RVA: 0x00086320 File Offset: 0x00084520
		private void WriteTvpOrderUnique(SmiExtendedMetaData metaData, TdsParserStateObject stateObj)
		{
			SmiOrderProperty smiOrderProperty = (SmiOrderProperty)metaData.ExtendedProperties[SmiPropertySelector.SortOrder];
			SmiUniqueKeyProperty smiUniqueKeyProperty = (SmiUniqueKeyProperty)metaData.ExtendedProperties[SmiPropertySelector.UniqueKey];
			List<TdsParser.TdsOrderUnique> list = new List<TdsParser.TdsOrderUnique>(metaData.FieldMetaData.Count);
			for (int i = 0; i < metaData.FieldMetaData.Count; i++)
			{
				byte b = 0;
				SmiOrderProperty.SmiColumnOrder smiColumnOrder = smiOrderProperty[i];
				if (smiColumnOrder.Order == SortOrder.Ascending)
				{
					b = 1;
				}
				else if (SortOrder.Descending == smiColumnOrder.Order)
				{
					b = 2;
				}
				if (smiUniqueKeyProperty[i])
				{
					b |= 4;
				}
				if (b != 0)
				{
					list.Add(new TdsParser.TdsOrderUnique(checked((short)(i + 1)), b));
				}
			}
			if (0 < list.Count)
			{
				stateObj.WriteByte(16);
				this.WriteShort(list.Count, stateObj);
				foreach (TdsParser.TdsOrderUnique tdsOrderUnique in list)
				{
					this.WriteShort((int)tdsOrderUnique.ColumnOrdinal, stateObj);
					stateObj.WriteByte(tdsOrderUnique.Flags);
				}
			}
		}

		// Token: 0x06001C66 RID: 7270 RVA: 0x0008643C File Offset: 0x0008463C
		internal Task WriteBulkCopyDone(TdsParserStateObject stateObj)
		{
			if (this.State != TdsParserState.OpenNotLoggedIn && this.State != TdsParserState.OpenLoggedIn)
			{
				throw ADP.ClosedConnectionError();
			}
			stateObj.WriteByte(253);
			this.WriteShort(0, stateObj);
			this.WriteShort(0, stateObj);
			this.WriteInt(0, stateObj);
			stateObj._pendingData = true;
			stateObj._messageStatus = 0;
			return stateObj.WritePacket(1, false);
		}

		// Token: 0x06001C67 RID: 7271 RVA: 0x0008649C File Offset: 0x0008469C
		internal void WriteBulkCopyMetaData(_SqlMetaDataSet metadataCollection, int count, TdsParserStateObject stateObj)
		{
			if (this.State != TdsParserState.OpenNotLoggedIn && this.State != TdsParserState.OpenLoggedIn)
			{
				throw ADP.ClosedConnectionError();
			}
			stateObj.WriteByte(129);
			this.WriteShort(count, stateObj);
			for (int i = 0; i < metadataCollection.Length; i++)
			{
				if (metadataCollection[i] != null)
				{
					_SqlMetaData sqlMetaData = metadataCollection[i];
					this.WriteInt(0, stateObj);
					ushort num = (ushort)(sqlMetaData.updatability << 2);
					num |= (sqlMetaData.isNullable ? 1 : 0);
					num |= (sqlMetaData.isIdentity ? 16 : 0);
					this.WriteShort((int)num, stateObj);
					SqlDbType type = sqlMetaData.type;
					if (type != SqlDbType.Decimal)
					{
						switch (type)
						{
						case SqlDbType.Xml:
							stateObj.WriteByteArray(TdsParser.s_xmlMetadataSubstituteSequence, TdsParser.s_xmlMetadataSubstituteSequence.Length, 0, true, null);
							goto IL_1AF;
						case SqlDbType.Udt:
							stateObj.WriteByte(165);
							this.WriteTokenLength(165, sqlMetaData.length, stateObj);
							goto IL_1AF;
						case SqlDbType.Date:
							stateObj.WriteByte(sqlMetaData.tdsType);
							goto IL_1AF;
						case SqlDbType.Time:
						case SqlDbType.DateTime2:
						case SqlDbType.DateTimeOffset:
							stateObj.WriteByte(sqlMetaData.tdsType);
							stateObj.WriteByte(sqlMetaData.scale);
							goto IL_1AF;
						}
						stateObj.WriteByte(sqlMetaData.tdsType);
						this.WriteTokenLength(sqlMetaData.tdsType, sqlMetaData.length, stateObj);
						if (sqlMetaData.metaType.IsCharType)
						{
							this.WriteUnsignedInt(sqlMetaData.collation.info, stateObj);
							stateObj.WriteByte(sqlMetaData.collation.sortId);
						}
					}
					else
					{
						stateObj.WriteByte(sqlMetaData.tdsType);
						this.WriteTokenLength(sqlMetaData.tdsType, sqlMetaData.length, stateObj);
						stateObj.WriteByte(sqlMetaData.precision);
						stateObj.WriteByte(sqlMetaData.scale);
					}
					IL_1AF:
					if (sqlMetaData.metaType.IsLong && !sqlMetaData.metaType.IsPlp)
					{
						this.WriteShort(sqlMetaData.tableName.Length, stateObj);
						this.WriteString(sqlMetaData.tableName, stateObj, true);
					}
					stateObj.WriteByte((byte)sqlMetaData.column.Length);
					this.WriteString(sqlMetaData.column, stateObj, true);
				}
			}
		}

		// Token: 0x06001C68 RID: 7272 RVA: 0x000866C4 File Offset: 0x000848C4
		internal Task WriteBulkCopyValue(object value, SqlMetaDataPriv metadata, TdsParserStateObject stateObj, bool isSqlType, bool isDataFeed, bool isNull)
		{
			Encoding defaultEncoding = this._defaultEncoding;
			SqlCollation defaultCollation = this._defaultCollation;
			int defaultCodePage = this._defaultCodePage;
			int defaultLCID = this._defaultLCID;
			Task result = null;
			Task task = null;
			if (this.State != TdsParserState.OpenNotLoggedIn && this.State != TdsParserState.OpenLoggedIn)
			{
				throw ADP.ClosedConnectionError();
			}
			try
			{
				if (metadata.encoding != null)
				{
					this._defaultEncoding = metadata.encoding;
				}
				if (metadata.collation != null)
				{
					this._defaultCollation = metadata.collation;
					this._defaultLCID = this._defaultCollation.LCID;
				}
				this._defaultCodePage = metadata.codePage;
				MetaType metaType = metadata.metaType;
				int num = 0;
				int num2 = 0;
				if (isNull)
				{
					if (metaType.IsPlp && (metaType.NullableType != 240 || metaType.IsLong))
					{
						this.WriteLong(-1L, stateObj);
					}
					else if (!metaType.IsFixed && !metaType.IsLong && !metaType.IsVarTime)
					{
						this.WriteShort(65535, stateObj);
					}
					else
					{
						stateObj.WriteByte(0);
					}
					return result;
				}
				if (!isDataFeed)
				{
					byte nullableType = metaType.NullableType;
					if (nullableType <= 167)
					{
						if (nullableType <= 99)
						{
							switch (nullableType)
							{
							case 34:
								break;
							case 35:
								goto IL_1C7;
							case 36:
								num = 16;
								goto IL_285;
							default:
								if (nullableType != 99)
								{
									goto IL_27D;
								}
								goto IL_212;
							}
						}
						else if (nullableType != 165)
						{
							if (nullableType != 167)
							{
								goto IL_27D;
							}
							goto IL_1C7;
						}
					}
					else if (nullableType <= 175)
					{
						if (nullableType != 173)
						{
							if (nullableType != 175)
							{
								goto IL_27D;
							}
							goto IL_1C7;
						}
					}
					else
					{
						if (nullableType == 231)
						{
							goto IL_212;
						}
						switch (nullableType)
						{
						case 239:
							goto IL_212;
						case 240:
							break;
						case 241:
							if (value is XmlReader)
							{
								value = MetaType.GetStringFromXml((XmlReader)value);
							}
							num = (isSqlType ? ((SqlString)value).Value.Length : ((string)value).Length) * 2;
							goto IL_285;
						default:
							goto IL_27D;
						}
					}
					num = (isSqlType ? ((SqlBinary)value).Length : ((byte[])value).Length);
					goto IL_285;
					IL_1C7:
					if (this._defaultEncoding == null)
					{
						this.ThrowUnsupportedCollationEncountered(null);
					}
					string text;
					if (isSqlType)
					{
						text = ((SqlString)value).Value;
					}
					else
					{
						text = (string)value;
					}
					num = text.Length;
					num2 = this._defaultEncoding.GetByteCount(text);
					goto IL_285;
					IL_212:
					num = (isSqlType ? ((SqlString)value).Value.Length : ((string)value).Length) * 2;
					goto IL_285;
					IL_27D:
					num = metadata.length;
				}
				IL_285:
				if (metaType.IsLong)
				{
					SqlDbType sqlDbType = metaType.SqlDbType;
					if (sqlDbType <= SqlDbType.NVarChar)
					{
						if (sqlDbType != SqlDbType.Image && sqlDbType != SqlDbType.NText)
						{
							if (sqlDbType != SqlDbType.NVarChar)
							{
								goto IL_329;
							}
							goto IL_306;
						}
					}
					else if (sqlDbType <= SqlDbType.VarChar)
					{
						if (sqlDbType != SqlDbType.Text)
						{
							if (sqlDbType - SqlDbType.VarBinary > 1)
							{
								goto IL_329;
							}
							goto IL_306;
						}
					}
					else
					{
						if (sqlDbType != SqlDbType.Xml && sqlDbType != SqlDbType.Udt)
						{
							goto IL_329;
						}
						goto IL_306;
					}
					stateObj.WriteByteArray(TdsParser.s_longDataHeader, TdsParser.s_longDataHeader.Length, 0, true, null);
					this.WriteTokenLength(metadata.tdsType, (num2 == 0) ? num : num2, stateObj);
					goto IL_329;
					IL_306:
					this.WriteUnsignedLong(18446744073709551614UL, stateObj);
				}
				else
				{
					this.WriteTokenLength(metadata.tdsType, (num2 == 0) ? num : num2, stateObj);
				}
				IL_329:
				if (isSqlType)
				{
					task = this.WriteSqlValue(value, metaType, num, num2, 0, stateObj);
				}
				else if (metaType.SqlDbType != SqlDbType.Udt || metaType.IsLong)
				{
					task = this.WriteValue(value, metaType, metadata.scale, num, num2, 0, stateObj, metadata.length, isDataFeed);
					if (task == null && this._asyncWrite)
					{
						task = stateObj.WaitForAccumulatedWrites();
					}
				}
				else
				{
					this.WriteShort(num, stateObj);
					task = stateObj.WriteByteArray((byte[])value, num, 0, true, null);
				}
				if (task != null)
				{
					result = this.WriteBulkCopyValueSetupContinuation(task, defaultEncoding, defaultCollation, defaultCodePage, defaultLCID);
				}
			}
			finally
			{
				if (task == null)
				{
					this._defaultEncoding = defaultEncoding;
					this._defaultCollation = defaultCollation;
					this._defaultCodePage = defaultCodePage;
					this._defaultLCID = defaultLCID;
				}
			}
			return result;
		}

		// Token: 0x06001C69 RID: 7273 RVA: 0x00086ACC File Offset: 0x00084CCC
		private Task WriteBulkCopyValueSetupContinuation(Task internalWriteTask, Encoding saveEncoding, SqlCollation saveCollation, int saveCodePage, int saveLCID)
		{
			return internalWriteTask.ContinueWith<Task>(delegate(Task t)
			{
				this._defaultEncoding = saveEncoding;
				this._defaultCollation = saveCollation;
				this._defaultCodePage = saveCodePage;
				this._defaultLCID = saveLCID;
				return t;
			}, TaskScheduler.Default).Unwrap();
		}

		// Token: 0x06001C6A RID: 7274 RVA: 0x00086B20 File Offset: 0x00084D20
		private void WriteMarsHeaderData(TdsParserStateObject stateObj, SqlInternalTransaction transaction)
		{
			this.WriteShort(2, stateObj);
			if (transaction != null && transaction.TransactionId != 0L)
			{
				this.WriteLong(transaction.TransactionId, stateObj);
				this.WriteInt(stateObj.IncrementAndObtainOpenResultCount(transaction), stateObj);
				return;
			}
			this.WriteLong(0L, stateObj);
			this.WriteInt(stateObj.IncrementAndObtainOpenResultCount(null), stateObj);
		}

		// Token: 0x06001C6B RID: 7275 RVA: 0x00086B74 File Offset: 0x00084D74
		private int GetNotificationHeaderSize(SqlNotificationRequest notificationRequest)
		{
			if (notificationRequest == null)
			{
				return 0;
			}
			string userData = notificationRequest.UserData;
			string options = notificationRequest.Options;
			int timeout = notificationRequest.Timeout;
			if (userData == null)
			{
				throw ADP.ArgumentNull("callbackId");
			}
			if (65535 < userData.Length)
			{
				throw ADP.ArgumentOutOfRange("callbackId");
			}
			if (options == null)
			{
				throw ADP.ArgumentNull("service");
			}
			if (65535 < options.Length)
			{
				throw ADP.ArgumentOutOfRange("service");
			}
			if (-1 > timeout)
			{
				throw ADP.ArgumentOutOfRange("timeout");
			}
			int num = 8 + userData.Length * 2 + 2 + options.Length * 2;
			if (timeout > 0)
			{
				num += 4;
			}
			return num;
		}

		// Token: 0x06001C6C RID: 7276 RVA: 0x00086C18 File Offset: 0x00084E18
		private void WriteQueryNotificationHeaderData(SqlNotificationRequest notificationRequest, TdsParserStateObject stateObj)
		{
			string userData = notificationRequest.UserData;
			string options = notificationRequest.Options;
			int timeout = notificationRequest.Timeout;
			this.WriteShort(1, stateObj);
			this.WriteShort(userData.Length * 2, stateObj);
			this.WriteString(userData, stateObj, true);
			this.WriteShort(options.Length * 2, stateObj);
			this.WriteString(options, stateObj, true);
			if (timeout > 0)
			{
				this.WriteInt(timeout, stateObj);
			}
		}

		// Token: 0x06001C6D RID: 7277 RVA: 0x00086C80 File Offset: 0x00084E80
		private void WriteRPCBatchHeaders(TdsParserStateObject stateObj, SqlNotificationRequest notificationRequest)
		{
			int notificationHeaderSize = this.GetNotificationHeaderSize(notificationRequest);
			int v = 22 + notificationHeaderSize;
			this.WriteInt(v, stateObj);
			this.WriteInt(18, stateObj);
			this.WriteMarsHeaderData(stateObj, this.CurrentTransaction);
			if (notificationHeaderSize != 0)
			{
				this.WriteInt(notificationHeaderSize, stateObj);
				this.WriteQueryNotificationHeaderData(notificationRequest, stateObj);
			}
		}

		// Token: 0x06001C6E RID: 7278 RVA: 0x00086CCC File Offset: 0x00084ECC
		private void WriteTokenLength(byte token, int length, TdsParserStateObject stateObj)
		{
			int num = 0;
			if (240 == token)
			{
				num = 8;
			}
			else if (token == 241)
			{
				num = 8;
			}
			if (num == 0)
			{
				int num2 = (int)(token & 48);
				if (num2 <= 16)
				{
					if (num2 != 0)
					{
						if (num2 != 16)
						{
							goto IL_5D;
						}
						num = 0;
						goto IL_5D;
					}
				}
				else if (num2 != 32)
				{
					if (num2 == 48)
					{
						num = 0;
						goto IL_5D;
					}
					goto IL_5D;
				}
				if ((token & 128) != 0)
				{
					num = 2;
				}
				else if ((token & 12) == 0)
				{
					num = 4;
				}
				else
				{
					num = 1;
				}
				IL_5D:
				switch (num)
				{
				case 1:
					stateObj.WriteByte((byte)length);
					return;
				case 2:
					this.WriteShort(length, stateObj);
					return;
				case 3:
					break;
				case 4:
					this.WriteInt(length, stateObj);
					return;
				default:
					if (num != 8)
					{
						return;
					}
					this.WriteShort(65535, stateObj);
					break;
				}
			}
		}

		// Token: 0x06001C6F RID: 7279 RVA: 0x00086D7C File Offset: 0x00084F7C
		private bool IsBOMNeeded(MetaType type, object value)
		{
			if (type.NullableType == 241)
			{
				Type type2 = value.GetType();
				if (type2 == typeof(SqlString))
				{
					if (!((SqlString)value).IsNull && ((SqlString)value).Value.Length > 0 && (((SqlString)value).Value[0] & 'ÿ') != 'ÿ')
					{
						return true;
					}
				}
				else if (type2 == typeof(string) && ((string)value).Length > 0)
				{
					if (value != null && (((string)value)[0] & 'ÿ') != 'ÿ')
					{
						return true;
					}
				}
				else if (type2 == typeof(SqlXml))
				{
					if (!((SqlXml)value).IsNull)
					{
						return true;
					}
				}
				else if (type2 == typeof(XmlDataFeed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001C70 RID: 7280 RVA: 0x00086E75 File Offset: 0x00085075
		private Task GetTerminationTask(Task unterminatedWriteTask, object value, MetaType type, int actualLength, TdsParserStateObject stateObj, bool isDataFeed)
		{
			if (!type.IsPlp || (actualLength <= 0 && !isDataFeed))
			{
				return unterminatedWriteTask;
			}
			if (unterminatedWriteTask == null)
			{
				this.WriteInt(0, stateObj);
				return null;
			}
			return AsyncHelper.CreateContinuationTask<int, TdsParserStateObject>(unterminatedWriteTask, new Action<int, TdsParserStateObject>(this.WriteInt), 0, stateObj, this._connHandler, null);
		}

		// Token: 0x06001C71 RID: 7281 RVA: 0x00086EB5 File Offset: 0x000850B5
		private Task WriteSqlValue(object value, MetaType type, int actualLength, int codePageByteSize, int offset, TdsParserStateObject stateObj)
		{
			return this.GetTerminationTask(this.WriteUnterminatedSqlValue(value, type, actualLength, codePageByteSize, offset, stateObj), value, type, actualLength, stateObj, false);
		}

		// Token: 0x06001C72 RID: 7282 RVA: 0x00086ED4 File Offset: 0x000850D4
		private Task WriteUnterminatedSqlValue(object value, MetaType type, int actualLength, int codePageByteSize, int offset, TdsParserStateObject stateObj)
		{
			byte nullableType = type.NullableType;
			if (nullableType <= 165)
			{
				if (nullableType <= 99)
				{
					switch (nullableType)
					{
					case 34:
						break;
					case 35:
						goto IL_22F;
					case 36:
					{
						byte[] b = ((SqlGuid)value).ToByteArray();
						stateObj.WriteByteArray(b, actualLength, 0, true, null);
						goto IL_3C6;
					}
					case 37:
						goto IL_3C6;
					case 38:
						if (type.FixedLength == 1)
						{
							stateObj.WriteByte(((SqlByte)value).Value);
							goto IL_3C6;
						}
						if (type.FixedLength == 2)
						{
							this.WriteShort((int)((SqlInt16)value).Value, stateObj);
							goto IL_3C6;
						}
						if (type.FixedLength == 4)
						{
							this.WriteInt(((SqlInt32)value).Value, stateObj);
							goto IL_3C6;
						}
						this.WriteLong(((SqlInt64)value).Value, stateObj);
						goto IL_3C6;
					default:
						if (nullableType != 99)
						{
							goto IL_3C6;
						}
						goto IL_292;
					}
				}
				else
				{
					switch (nullableType)
					{
					case 104:
						if (((SqlBoolean)value).Value)
						{
							stateObj.WriteByte(1);
							goto IL_3C6;
						}
						stateObj.WriteByte(0);
						goto IL_3C6;
					case 105:
					case 106:
					case 107:
						goto IL_3C6;
					case 108:
						this.WriteSqlDecimal((SqlDecimal)value, stateObj);
						goto IL_3C6;
					case 109:
						if (type.FixedLength == 4)
						{
							this.WriteFloat(((SqlSingle)value).Value, stateObj);
							goto IL_3C6;
						}
						this.WriteDouble(((SqlDouble)value).Value, stateObj);
						goto IL_3C6;
					case 110:
						this.WriteSqlMoney((SqlMoney)value, type.FixedLength, stateObj);
						goto IL_3C6;
					case 111:
					{
						SqlDateTime sqlDateTime = (SqlDateTime)value;
						if (type.FixedLength != 4)
						{
							this.WriteInt(sqlDateTime.DayTicks, stateObj);
							this.WriteInt(sqlDateTime.TimeTicks, stateObj);
							goto IL_3C6;
						}
						if (0 > sqlDateTime.DayTicks || sqlDateTime.DayTicks > 65535)
						{
							throw SQL.SmallDateTimeOverflow(sqlDateTime.ToString());
						}
						this.WriteShort(sqlDateTime.DayTicks, stateObj);
						this.WriteShort(sqlDateTime.TimeTicks / SqlDateTime.SQLTicksPerMinute, stateObj);
						goto IL_3C6;
					}
					default:
						if (nullableType != 165)
						{
							goto IL_3C6;
						}
						break;
					}
				}
			}
			else if (nullableType <= 173)
			{
				if (nullableType == 167)
				{
					goto IL_22F;
				}
				if (nullableType != 173)
				{
					goto IL_3C6;
				}
			}
			else
			{
				if (nullableType == 175)
				{
					goto IL_22F;
				}
				if (nullableType == 231)
				{
					goto IL_292;
				}
				switch (nullableType)
				{
				case 239:
				case 241:
					goto IL_292;
				case 240:
					throw SQL.UDTUnexpectedResult(value.GetType().AssemblyQualifiedName);
				default:
					goto IL_3C6;
				}
			}
			if (type.IsPlp)
			{
				this.WriteInt(actualLength, stateObj);
			}
			if (value is SqlBinary)
			{
				return stateObj.WriteByteArray(((SqlBinary)value).Value, actualLength, offset, false, null);
			}
			return stateObj.WriteByteArray(((SqlBytes)value).Value, actualLength, offset, false, null);
			IL_22F:
			if (type.IsPlp)
			{
				this.WriteInt(codePageByteSize, stateObj);
			}
			if (value is SqlChars)
			{
				string s = new string(((SqlChars)value).Value);
				return this.WriteEncodingChar(s, actualLength, offset, this._defaultEncoding, stateObj, false);
			}
			return this.WriteEncodingChar(((SqlString)value).Value, actualLength, offset, this._defaultEncoding, stateObj, false);
			IL_292:
			if (type.IsPlp)
			{
				if (this.IsBOMNeeded(type, value))
				{
					this.WriteInt(actualLength + 2, stateObj);
					this.WriteShort(65279, stateObj);
				}
				else
				{
					this.WriteInt(actualLength, stateObj);
				}
			}
			if (actualLength != 0)
			{
				actualLength >>= 1;
			}
			if (value is SqlChars)
			{
				return this.WriteCharArray(((SqlChars)value).Value, actualLength, offset, stateObj, false);
			}
			return this.WriteString(((SqlString)value).Value, actualLength, offset, stateObj, false);
			IL_3C6:
			return null;
		}

		// Token: 0x06001C73 RID: 7283 RVA: 0x000872A8 File Offset: 0x000854A8
		private Task WriteXmlFeed(XmlDataFeed feed, TdsParserStateObject stateObj, bool needBom, Encoding encoding, int size)
		{
			TdsParser.<WriteXmlFeed>d__208 <WriteXmlFeed>d__;
			<WriteXmlFeed>d__.<>4__this = this;
			<WriteXmlFeed>d__.feed = feed;
			<WriteXmlFeed>d__.stateObj = stateObj;
			<WriteXmlFeed>d__.needBom = needBom;
			<WriteXmlFeed>d__.encoding = encoding;
			<WriteXmlFeed>d__.size = size;
			<WriteXmlFeed>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WriteXmlFeed>d__.<>1__state = -1;
			<WriteXmlFeed>d__.<>t__builder.Start<TdsParser.<WriteXmlFeed>d__208>(ref <WriteXmlFeed>d__);
			return <WriteXmlFeed>d__.<>t__builder.Task;
		}

		// Token: 0x06001C74 RID: 7284 RVA: 0x00087318 File Offset: 0x00085518
		private Task WriteTextFeed(TextDataFeed feed, Encoding encoding, bool needBom, TdsParserStateObject stateObj, int size)
		{
			TdsParser.<WriteTextFeed>d__209 <WriteTextFeed>d__;
			<WriteTextFeed>d__.<>4__this = this;
			<WriteTextFeed>d__.feed = feed;
			<WriteTextFeed>d__.encoding = encoding;
			<WriteTextFeed>d__.needBom = needBom;
			<WriteTextFeed>d__.stateObj = stateObj;
			<WriteTextFeed>d__.size = size;
			<WriteTextFeed>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WriteTextFeed>d__.<>1__state = -1;
			<WriteTextFeed>d__.<>t__builder.Start<TdsParser.<WriteTextFeed>d__209>(ref <WriteTextFeed>d__);
			return <WriteTextFeed>d__.<>t__builder.Task;
		}

		// Token: 0x06001C75 RID: 7285 RVA: 0x00087388 File Offset: 0x00085588
		private Task WriteStreamFeed(StreamDataFeed feed, TdsParserStateObject stateObj, int len)
		{
			TdsParser.<WriteStreamFeed>d__210 <WriteStreamFeed>d__;
			<WriteStreamFeed>d__.<>4__this = this;
			<WriteStreamFeed>d__.feed = feed;
			<WriteStreamFeed>d__.stateObj = stateObj;
			<WriteStreamFeed>d__.len = len;
			<WriteStreamFeed>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WriteStreamFeed>d__.<>1__state = -1;
			<WriteStreamFeed>d__.<>t__builder.Start<TdsParser.<WriteStreamFeed>d__210>(ref <WriteStreamFeed>d__);
			return <WriteStreamFeed>d__.<>t__builder.Task;
		}

		// Token: 0x06001C76 RID: 7286 RVA: 0x000873E4 File Offset: 0x000855E4
		private Task NullIfCompletedWriteTask(Task task)
		{
			if (task == null)
			{
				return null;
			}
			switch (task.Status)
			{
			case TaskStatus.RanToCompletion:
				return null;
			case TaskStatus.Canceled:
				throw SQL.OperationCancelled();
			case TaskStatus.Faulted:
				throw task.Exception.InnerException;
			default:
				return task;
			}
		}

		// Token: 0x06001C77 RID: 7287 RVA: 0x00087428 File Offset: 0x00085628
		private Task WriteValue(object value, MetaType type, byte scale, int actualLength, int encodingByteSize, int offset, TdsParserStateObject stateObj, int paramSize, bool isDataFeed)
		{
			return this.GetTerminationTask(this.WriteUnterminatedValue(value, type, scale, actualLength, encodingByteSize, offset, stateObj, paramSize, isDataFeed), value, type, actualLength, stateObj, isDataFeed);
		}

		// Token: 0x06001C78 RID: 7288 RVA: 0x00087458 File Offset: 0x00085658
		private Task WriteUnterminatedValue(object value, MetaType type, byte scale, int actualLength, int encodingByteSize, int offset, TdsParserStateObject stateObj, int paramSize, bool isDataFeed)
		{
			byte nullableType = type.NullableType;
			if (nullableType <= 165)
			{
				if (nullableType <= 99)
				{
					switch (nullableType)
					{
					case 34:
						break;
					case 35:
						goto IL_1F8;
					case 36:
					{
						byte[] b = ((Guid)value).ToByteArray();
						stateObj.WriteByteArray(b, actualLength, 0, true, null);
						goto IL_460;
					}
					case 37:
					case 39:
						goto IL_460;
					case 38:
						if (type.FixedLength == 1)
						{
							stateObj.WriteByte((byte)value);
							goto IL_460;
						}
						if (type.FixedLength == 2)
						{
							this.WriteShort((int)((short)value), stateObj);
							goto IL_460;
						}
						if (type.FixedLength == 4)
						{
							this.WriteInt((int)value, stateObj);
							goto IL_460;
						}
						this.WriteLong((long)value, stateObj);
						goto IL_460;
					case 40:
						this.WriteDate((DateTime)value, stateObj);
						goto IL_460;
					case 41:
						if (scale > 7)
						{
							throw SQL.TimeScaleValueOutOfRange(scale);
						}
						this.WriteTime((TimeSpan)value, scale, actualLength, stateObj);
						goto IL_460;
					case 42:
						if (scale > 7)
						{
							throw SQL.TimeScaleValueOutOfRange(scale);
						}
						this.WriteDateTime2((DateTime)value, scale, actualLength, stateObj);
						goto IL_460;
					case 43:
						this.WriteDateTimeOffset((DateTimeOffset)value, scale, actualLength, stateObj);
						goto IL_460;
					default:
						if (nullableType != 99)
						{
							goto IL_460;
						}
						goto IL_287;
					}
				}
				else
				{
					switch (nullableType)
					{
					case 104:
						if ((bool)value)
						{
							stateObj.WriteByte(1);
							goto IL_460;
						}
						stateObj.WriteByte(0);
						goto IL_460;
					case 105:
					case 106:
					case 107:
						goto IL_460;
					case 108:
						this.WriteDecimal((decimal)value, stateObj);
						goto IL_460;
					case 109:
						if (type.FixedLength == 4)
						{
							this.WriteFloat((float)value, stateObj);
							goto IL_460;
						}
						this.WriteDouble((double)value, stateObj);
						goto IL_460;
					case 110:
						this.WriteCurrency((decimal)value, type.FixedLength, stateObj);
						goto IL_460;
					case 111:
					{
						TdsDateTime tdsDateTime = MetaType.FromDateTime((DateTime)value, (byte)type.FixedLength);
						if (type.FixedLength != 4)
						{
							this.WriteInt(tdsDateTime.days, stateObj);
							this.WriteInt(tdsDateTime.time, stateObj);
							goto IL_460;
						}
						if (0 > tdsDateTime.days || tdsDateTime.days > 65535)
						{
							throw SQL.SmallDateTimeOverflow(MetaType.ToDateTime(tdsDateTime.days, tdsDateTime.time, 4).ToString(CultureInfo.InvariantCulture));
						}
						this.WriteShort(tdsDateTime.days, stateObj);
						this.WriteShort(tdsDateTime.time, stateObj);
						goto IL_460;
					}
					default:
						if (nullableType != 165)
						{
							goto IL_460;
						}
						break;
					}
				}
			}
			else if (nullableType <= 173)
			{
				if (nullableType == 167)
				{
					goto IL_1F8;
				}
				if (nullableType != 173)
				{
					goto IL_460;
				}
			}
			else
			{
				if (nullableType == 175)
				{
					goto IL_1F8;
				}
				if (nullableType == 231)
				{
					goto IL_287;
				}
				switch (nullableType)
				{
				case 239:
				case 241:
					goto IL_287;
				case 240:
					break;
				default:
					goto IL_460;
				}
			}
			if (isDataFeed)
			{
				return this.NullIfCompletedWriteTask(this.WriteStreamFeed((StreamDataFeed)value, stateObj, paramSize));
			}
			if (type.IsPlp)
			{
				this.WriteInt(actualLength, stateObj);
			}
			return stateObj.WriteByteArray((byte[])value, actualLength, offset, false, null);
			IL_1F8:
			if (isDataFeed)
			{
				TextDataFeed textDataFeed = value as TextDataFeed;
				if (textDataFeed == null)
				{
					return this.NullIfCompletedWriteTask(this.WriteXmlFeed((XmlDataFeed)value, stateObj, true, this._defaultEncoding, paramSize));
				}
				return this.NullIfCompletedWriteTask(this.WriteTextFeed(textDataFeed, this._defaultEncoding, false, stateObj, paramSize));
			}
			else
			{
				if (type.IsPlp)
				{
					this.WriteInt(encodingByteSize, stateObj);
				}
				if (value is byte[])
				{
					return stateObj.WriteByteArray((byte[])value, actualLength, 0, false, null);
				}
				return this.WriteEncodingChar((string)value, actualLength, offset, this._defaultEncoding, stateObj, false);
			}
			IL_287:
			if (isDataFeed)
			{
				TextDataFeed textDataFeed2 = value as TextDataFeed;
				if (textDataFeed2 == null)
				{
					return this.NullIfCompletedWriteTask(this.WriteXmlFeed((XmlDataFeed)value, stateObj, this.IsBOMNeeded(type, value), Encoding.Unicode, paramSize));
				}
				return this.NullIfCompletedWriteTask(this.WriteTextFeed(textDataFeed2, null, this.IsBOMNeeded(type, value), stateObj, paramSize));
			}
			else
			{
				if (type.IsPlp)
				{
					if (this.IsBOMNeeded(type, value))
					{
						this.WriteInt(actualLength + 2, stateObj);
						this.WriteShort(65279, stateObj);
					}
					else
					{
						this.WriteInt(actualLength, stateObj);
					}
				}
				if (value is byte[])
				{
					return stateObj.WriteByteArray((byte[])value, actualLength, 0, false, null);
				}
				actualLength >>= 1;
				return this.WriteString((string)value, actualLength, offset, stateObj, false);
			}
			IL_460:
			return null;
		}

		// Token: 0x06001C79 RID: 7289 RVA: 0x000878C8 File Offset: 0x00085AC8
		internal void WriteParameterVarLen(MetaType type, int size, bool isNull, TdsParserStateObject stateObj, bool unknownLength = false)
		{
			if (type.IsLong)
			{
				if (isNull)
				{
					if (type.IsPlp)
					{
						this.WriteLong(-1L, stateObj);
						return;
					}
					this.WriteInt(-1, stateObj);
					return;
				}
				else
				{
					if (type.NullableType == 241 || unknownLength)
					{
						this.WriteUnsignedLong(18446744073709551614UL, stateObj);
						return;
					}
					if (type.IsPlp)
					{
						this.WriteLong((long)size, stateObj);
						return;
					}
					this.WriteInt(size, stateObj);
					return;
				}
			}
			else if (type.IsVarTime)
			{
				if (isNull)
				{
					stateObj.WriteByte(0);
					return;
				}
				stateObj.WriteByte((byte)size);
				return;
			}
			else if (!type.IsFixed)
			{
				if (isNull)
				{
					this.WriteShort(65535, stateObj);
					return;
				}
				this.WriteShort(size, stateObj);
				return;
			}
			else
			{
				if (isNull)
				{
					stateObj.WriteByte(0);
					return;
				}
				stateObj.WriteByte((byte)(type.FixedLength & 255));
				return;
			}
		}

		// Token: 0x06001C7A RID: 7290 RVA: 0x0008799C File Offset: 0x00085B9C
		private bool TryReadPlpUnicodeCharsChunk(char[] buff, int offst, int len, TdsParserStateObject stateObj, out int charsRead)
		{
			if (stateObj._longlenleft == 0UL)
			{
				charsRead = 0;
				return true;
			}
			charsRead = len;
			if (stateObj._longlenleft >> 1 < (ulong)((long)len))
			{
				charsRead = (int)(stateObj._longlenleft >> 1);
			}
			for (int i = 0; i < charsRead; i++)
			{
				if (!stateObj.TryReadChar(out buff[offst + i]))
				{
					return false;
				}
			}
			stateObj._longlenleft -= (ulong)((ulong)((long)charsRead) << 1);
			return true;
		}

		// Token: 0x06001C7B RID: 7291 RVA: 0x00087A10 File Offset: 0x00085C10
		internal int ReadPlpUnicodeChars(ref char[] buff, int offst, int len, TdsParserStateObject stateObj)
		{
			int result;
			if (!this.TryReadPlpUnicodeChars(ref buff, offst, len, stateObj, out result))
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			return result;
		}

		// Token: 0x06001C7C RID: 7292 RVA: 0x00087A34 File Offset: 0x00085C34
		internal bool TryReadPlpUnicodeChars(ref char[] buff, int offst, int len, TdsParserStateObject stateObj, out int totalCharsRead)
		{
			int num = 0;
			if (stateObj._longlen == 0UL)
			{
				totalCharsRead = 0;
				return true;
			}
			int i = len;
			if (buff == null && stateObj._longlen != 18446744073709551614UL)
			{
				buff = new char[Math.Min((int)stateObj._longlen, len)];
			}
			if (stateObj._longlenleft == 0UL)
			{
				ulong num2;
				if (!stateObj.TryReadPlpLength(false, out num2))
				{
					totalCharsRead = 0;
					return false;
				}
				if (stateObj._longlenleft == 0UL)
				{
					totalCharsRead = 0;
					return true;
				}
			}
			totalCharsRead = 0;
			while (i > 0)
			{
				num = (int)Math.Min(stateObj._longlenleft + 1UL >> 1, (ulong)((long)i));
				if (buff == null || buff.Length < offst + num)
				{
					char[] array = new char[offst + num];
					if (buff != null)
					{
						Buffer.BlockCopy(buff, 0, array, 0, offst * 2);
					}
					buff = array;
				}
				if (num > 0)
				{
					if (!this.TryReadPlpUnicodeCharsChunk(buff, offst, num, stateObj, out num))
					{
						return false;
					}
					i -= num;
					offst += num;
					totalCharsRead += num;
				}
				if (stateObj._longlenleft == 1UL && i > 0)
				{
					byte b;
					if (!stateObj.TryReadByte(out b))
					{
						return false;
					}
					stateObj._longlenleft -= 1UL;
					ulong num3;
					if (!stateObj.TryReadPlpLength(false, out num3))
					{
						return false;
					}
					byte b2;
					if (!stateObj.TryReadByte(out b2))
					{
						return false;
					}
					stateObj._longlenleft -= 1UL;
					buff[offst] = (char)(((int)(b2 & byte.MaxValue) << 8) + (int)(b & byte.MaxValue));
					checked
					{
						offst++;
					}
					num++;
					i--;
					totalCharsRead++;
				}
				ulong num4;
				if (stateObj._longlenleft == 0UL && !stateObj.TryReadPlpLength(false, out num4))
				{
					return false;
				}
				if (stateObj._longlenleft == 0UL)
				{
					break;
				}
			}
			return true;
		}

		// Token: 0x06001C7D RID: 7293 RVA: 0x00087BC0 File Offset: 0x00085DC0
		internal int ReadPlpAnsiChars(ref char[] buff, int offst, int len, SqlMetaDataPriv metadata, TdsParserStateObject stateObj)
		{
			int num = 0;
			if (stateObj._longlen == 0UL)
			{
				return 0;
			}
			int i = len;
			if (stateObj._longlenleft == 0UL)
			{
				stateObj.ReadPlpLength(false);
				if (stateObj._longlenleft == 0UL)
				{
					stateObj._plpdecoder = null;
					return 0;
				}
			}
			if (stateObj._plpdecoder == null)
			{
				Encoding encoding = metadata.encoding;
				if (encoding == null)
				{
					if (this._defaultEncoding == null)
					{
						this.ThrowUnsupportedCollationEncountered(stateObj);
					}
					encoding = this._defaultEncoding;
				}
				stateObj._plpdecoder = encoding.GetDecoder();
			}
			while (i > 0)
			{
				int num2 = (int)Math.Min(stateObj._longlenleft, (ulong)((long)i));
				if (stateObj._bTmp == null || stateObj._bTmp.Length < num2)
				{
					stateObj._bTmp = new byte[num2];
				}
				num2 = stateObj.ReadPlpBytesChunk(stateObj._bTmp, 0, num2);
				int chars = stateObj._plpdecoder.GetChars(stateObj._bTmp, 0, num2, buff, offst);
				i -= chars;
				offst += chars;
				num += chars;
				if (stateObj._longlenleft == 0UL)
				{
					stateObj.ReadPlpLength(false);
				}
				if (stateObj._longlenleft == 0UL)
				{
					stateObj._plpdecoder = null;
					break;
				}
			}
			return num;
		}

		// Token: 0x06001C7E RID: 7294 RVA: 0x00087CE4 File Offset: 0x00085EE4
		internal ulong SkipPlpValue(ulong cb, TdsParserStateObject stateObj)
		{
			ulong result;
			if (!this.TrySkipPlpValue(cb, stateObj, out result))
			{
				throw SQL.SynchronousCallMayNotPend();
			}
			return result;
		}

		// Token: 0x06001C7F RID: 7295 RVA: 0x00087D04 File Offset: 0x00085F04
		internal bool TrySkipPlpValue(ulong cb, TdsParserStateObject stateObj, out ulong totalBytesSkipped)
		{
			totalBytesSkipped = 0UL;
			ulong num;
			if (stateObj._longlenleft == 0UL && !stateObj.TryReadPlpLength(false, out num))
			{
				return false;
			}
			while (totalBytesSkipped < cb && stateObj._longlenleft > 0UL)
			{
				int num2;
				if (stateObj._longlenleft > 2147483647UL)
				{
					num2 = int.MaxValue;
				}
				else
				{
					num2 = (int)stateObj._longlenleft;
				}
				num2 = ((cb - totalBytesSkipped < (ulong)((long)num2)) ? ((int)(cb - totalBytesSkipped)) : num2);
				if (!stateObj.TrySkipBytes(num2))
				{
					return false;
				}
				stateObj._longlenleft -= (ulong)((long)num2);
				totalBytesSkipped += (ulong)((long)num2);
				ulong num3;
				if (stateObj._longlenleft == 0UL && !stateObj.TryReadPlpLength(false, out num3))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001C80 RID: 7296 RVA: 0x00087D9F File Offset: 0x00085F9F
		internal ulong PlpBytesLeft(TdsParserStateObject stateObj)
		{
			if (stateObj._longlen != 0UL && stateObj._longlenleft == 0UL)
			{
				stateObj.ReadPlpLength(false);
			}
			return stateObj._longlenleft;
		}

		// Token: 0x06001C81 RID: 7297 RVA: 0x00087DBF File Offset: 0x00085FBF
		internal bool TryPlpBytesLeft(TdsParserStateObject stateObj, out ulong left)
		{
			if (stateObj._longlen != 0UL && stateObj._longlenleft == 0UL && !stateObj.TryReadPlpLength(false, out left))
			{
				return false;
			}
			left = stateObj._longlenleft;
			return true;
		}

		// Token: 0x06001C82 RID: 7298 RVA: 0x00087DE6 File Offset: 0x00085FE6
		internal ulong PlpBytesTotalLength(TdsParserStateObject stateObj)
		{
			if (stateObj._longlen == 18446744073709551614UL)
			{
				return ulong.MaxValue;
			}
			if (stateObj._longlen == 18446744073709551615UL)
			{
				return 0UL;
			}
			return stateObj._longlen;
		}

		// Token: 0x06001C83 RID: 7299 RVA: 0x00087E0C File Offset: 0x0008600C
		private bool TryProcessUDTMetaData(SqlMetaDataPriv metaData, TdsParserStateObject stateObj)
		{
			ushort num;
			if (!stateObj.TryReadUInt16(out num))
			{
				return false;
			}
			metaData.length = (int)num;
			byte b;
			return stateObj.TryReadByte(out b) && (b == 0 || stateObj.TryReadString((int)b, out metaData.udtDatabaseName)) && stateObj.TryReadByte(out b) && (b == 0 || stateObj.TryReadString((int)b, out metaData.udtSchemaName)) && stateObj.TryReadByte(out b) && (b == 0 || stateObj.TryReadString((int)b, out metaData.udtTypeName)) && stateObj.TryReadUInt16(out num) && (num == 0 || stateObj.TryReadString((int)num, out metaData.udtAssemblyQualifiedName));
		}

		// Token: 0x06001C84 RID: 7300 RVA: 0x00087EB0 File Offset: 0x000860B0
		// Note: this type is marked as 'beforefieldinit'.
		static TdsParser()
		{
		}

		// Token: 0x04001313 RID: 4883
		private static volatile bool s_fSSPILoaded = false;

		// Token: 0x04001314 RID: 4884
		internal TdsParserStateObject _physicalStateObj;

		// Token: 0x04001315 RID: 4885
		internal TdsParserStateObject _pMarsPhysicalConObj;

		// Token: 0x04001316 RID: 4886
		private const int constBinBufferSize = 4096;

		// Token: 0x04001317 RID: 4887
		private const int constTextBufferSize = 4096;

		// Token: 0x04001318 RID: 4888
		internal TdsParserState _state;

		// Token: 0x04001319 RID: 4889
		private string _server = "";

		// Token: 0x0400131A RID: 4890
		internal volatile bool _fResetConnection;

		// Token: 0x0400131B RID: 4891
		internal volatile bool _fPreserveTransaction;

		// Token: 0x0400131C RID: 4892
		private SqlCollation _defaultCollation;

		// Token: 0x0400131D RID: 4893
		private int _defaultCodePage;

		// Token: 0x0400131E RID: 4894
		private int _defaultLCID;

		// Token: 0x0400131F RID: 4895
		internal Encoding _defaultEncoding;

		// Token: 0x04001320 RID: 4896
		private static EncryptionOptions s_sniSupportedEncryptionOption = TdsParserStateObjectFactory.Singleton.EncryptionOptions;

		// Token: 0x04001321 RID: 4897
		private EncryptionOptions _encryptionOption = TdsParser.s_sniSupportedEncryptionOption;

		// Token: 0x04001322 RID: 4898
		private SqlInternalTransaction _currentTransaction;

		// Token: 0x04001323 RID: 4899
		private SqlInternalTransaction _pendingTransaction;

		// Token: 0x04001324 RID: 4900
		private long _retainedTransactionId;

		// Token: 0x04001325 RID: 4901
		private int _nonTransactedOpenResultCount;

		// Token: 0x04001326 RID: 4902
		private SqlInternalConnectionTds _connHandler;

		// Token: 0x04001327 RID: 4903
		private bool _fMARS;

		// Token: 0x04001328 RID: 4904
		internal bool _loginWithFailover;

		// Token: 0x04001329 RID: 4905
		internal AutoResetEvent _resetConnectionEvent;

		// Token: 0x0400132A RID: 4906
		internal TdsParserSessionPool _sessionPool;

		// Token: 0x0400132B RID: 4907
		private bool _isYukon;

		// Token: 0x0400132C RID: 4908
		private bool _isKatmai;

		// Token: 0x0400132D RID: 4909
		private bool _isDenali;

		// Token: 0x0400132E RID: 4910
		private byte[] _sniSpnBuffer;

		// Token: 0x0400132F RID: 4911
		private SqlStatistics _statistics;

		// Token: 0x04001330 RID: 4912
		private bool _statisticsIsInTransaction;

		// Token: 0x04001331 RID: 4913
		private static byte[] s_nicAddress;

		// Token: 0x04001332 RID: 4914
		private static volatile uint s_maxSSPILength = 0U;

		// Token: 0x04001333 RID: 4915
		private static readonly byte[] s_longDataHeader = new byte[]
		{
			16,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue,
			byte.MaxValue
		};

		// Token: 0x04001334 RID: 4916
		private static object s_tdsParserLock = new object();

		// Token: 0x04001335 RID: 4917
		private static readonly byte[] s_xmlMetadataSubstituteSequence = new byte[]
		{
			231,
			byte.MaxValue,
			byte.MaxValue,
			0,
			0,
			0,
			0,
			0
		};

		// Token: 0x04001336 RID: 4918
		private const int GUID_SIZE = 16;

		// Token: 0x04001337 RID: 4919
		internal bool _asyncWrite;

		// Token: 0x04001338 RID: 4920
		private static readonly IEnumerable<SqlDataRecord> s_tvpEmptyValue = new SqlDataRecord[0];

		// Token: 0x04001339 RID: 4921
		private const ulong _indeterminateSize = 18446744073709551615UL;

		// Token: 0x02000248 RID: 584
		private class TdsOrderUnique
		{
			// Token: 0x06001C85 RID: 7301 RVA: 0x00087F1E File Offset: 0x0008611E
			internal TdsOrderUnique(short ordinal, byte flags)
			{
				this.ColumnOrdinal = ordinal;
				this.Flags = flags;
			}

			// Token: 0x0400133A RID: 4922
			internal short ColumnOrdinal;

			// Token: 0x0400133B RID: 4923
			internal byte Flags;
		}

		// Token: 0x02000249 RID: 585
		private class TdsOutputStream : Stream
		{
			// Token: 0x06001C86 RID: 7302 RVA: 0x00087F34 File Offset: 0x00086134
			public TdsOutputStream(TdsParser parser, TdsParserStateObject stateObj, byte[] preambleToStrip)
			{
				this._parser = parser;
				this._stateObj = stateObj;
				this._preambleToStrip = preambleToStrip;
			}

			// Token: 0x1700052B RID: 1323
			// (get) Token: 0x06001C87 RID: 7303 RVA: 0x00006D64 File Offset: 0x00004F64
			public override bool CanRead
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700052C RID: 1324
			// (get) Token: 0x06001C88 RID: 7304 RVA: 0x00006D64 File Offset: 0x00004F64
			public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700052D RID: 1325
			// (get) Token: 0x06001C89 RID: 7305 RVA: 0x00006D61 File Offset: 0x00004F61
			public override bool CanWrite
			{
				get
				{
					return true;
				}
			}

			// Token: 0x06001C8A RID: 7306 RVA: 0x00007EED File Offset: 0x000060ED
			public override void Flush()
			{
			}

			// Token: 0x1700052E RID: 1326
			// (get) Token: 0x06001C8B RID: 7307 RVA: 0x00087F51 File Offset: 0x00086151
			public override long Length
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			// Token: 0x1700052F RID: 1327
			// (get) Token: 0x06001C8C RID: 7308 RVA: 0x00087F51 File Offset: 0x00086151
			// (set) Token: 0x06001C8D RID: 7309 RVA: 0x00087F51 File Offset: 0x00086151
			public override long Position
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			// Token: 0x06001C8E RID: 7310 RVA: 0x00087F51 File Offset: 0x00086151
			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			// Token: 0x06001C8F RID: 7311 RVA: 0x00087F51 File Offset: 0x00086151
			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			// Token: 0x06001C90 RID: 7312 RVA: 0x00087F51 File Offset: 0x00086151
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			// Token: 0x06001C91 RID: 7313 RVA: 0x00087F58 File Offset: 0x00086158
			private void StripPreamble(byte[] buffer, ref int offset, ref int count)
			{
				if (this._preambleToStrip != null && count >= this._preambleToStrip.Length)
				{
					for (int i = 0; i < this._preambleToStrip.Length; i++)
					{
						if (this._preambleToStrip[i] != buffer[i])
						{
							this._preambleToStrip = null;
							return;
						}
					}
					offset += this._preambleToStrip.Length;
					count -= this._preambleToStrip.Length;
				}
				this._preambleToStrip = null;
			}

			// Token: 0x06001C92 RID: 7314 RVA: 0x00087FC2 File Offset: 0x000861C2
			public override void Write(byte[] buffer, int offset, int count)
			{
				TdsParser.TdsOutputStream.ValidateWriteParameters(buffer, offset, count);
				this.StripPreamble(buffer, ref offset, ref count);
				if (count > 0)
				{
					this._parser.WriteInt(count, this._stateObj);
					this._stateObj.WriteByteArray(buffer, count, offset, true, null);
				}
			}

			// Token: 0x06001C93 RID: 7315 RVA: 0x00088000 File Offset: 0x00086200
			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				TdsParser.TdsOutputStream.ValidateWriteParameters(buffer, offset, count);
				this.StripPreamble(buffer, ref offset, ref count);
				Task task = null;
				if (count > 0)
				{
					this._parser.WriteInt(count, this._stateObj);
					task = this._stateObj.WriteByteArray(buffer, count, offset, false, null);
				}
				return task ?? Task.CompletedTask;
			}

			// Token: 0x06001C94 RID: 7316 RVA: 0x00088054 File Offset: 0x00086254
			internal static void ValidateWriteParameters(byte[] buffer, int offset, int count)
			{
				if (buffer == null)
				{
					throw ADP.ArgumentNull("buffer");
				}
				if (offset < 0)
				{
					throw ADP.ArgumentOutOfRange("offset");
				}
				if (count < 0)
				{
					throw ADP.ArgumentOutOfRange("count");
				}
				try
				{
					if (checked(offset + count) > buffer.Length)
					{
						throw ExceptionBuilder.InvalidOffsetLength();
					}
				}
				catch (OverflowException)
				{
					throw ExceptionBuilder.InvalidOffsetLength();
				}
			}

			// Token: 0x0400133C RID: 4924
			private TdsParser _parser;

			// Token: 0x0400133D RID: 4925
			private TdsParserStateObject _stateObj;

			// Token: 0x0400133E RID: 4926
			private byte[] _preambleToStrip;
		}

		// Token: 0x0200024A RID: 586
		private class ConstrainedTextWriter : TextWriter
		{
			// Token: 0x06001C95 RID: 7317 RVA: 0x000880B8 File Offset: 0x000862B8
			public ConstrainedTextWriter(TextWriter next, int size)
			{
				this._next = next;
				this._size = size;
				this._written = 0;
				if (this._size < 1)
				{
					this._size = int.MaxValue;
				}
			}

			// Token: 0x17000530 RID: 1328
			// (get) Token: 0x06001C96 RID: 7318 RVA: 0x000880E9 File Offset: 0x000862E9
			public bool IsComplete
			{
				get
				{
					return this._size > 0 && this._written >= this._size;
				}
			}

			// Token: 0x17000531 RID: 1329
			// (get) Token: 0x06001C97 RID: 7319 RVA: 0x00088107 File Offset: 0x00086307
			public override Encoding Encoding
			{
				get
				{
					return this._next.Encoding;
				}
			}

			// Token: 0x06001C98 RID: 7320 RVA: 0x00088114 File Offset: 0x00086314
			public override void Flush()
			{
				this._next.Flush();
			}

			// Token: 0x06001C99 RID: 7321 RVA: 0x00088121 File Offset: 0x00086321
			public override Task FlushAsync()
			{
				return this._next.FlushAsync();
			}

			// Token: 0x06001C9A RID: 7322 RVA: 0x0008812E File Offset: 0x0008632E
			public override void Write(char value)
			{
				if (this._written < this._size)
				{
					this._next.Write(value);
					this._written++;
				}
			}

			// Token: 0x06001C9B RID: 7323 RVA: 0x00088158 File Offset: 0x00086358
			public override void Write(char[] buffer, int index, int count)
			{
				TdsParser.ConstrainedTextWriter.ValidateWriteParameters(buffer, index, count);
				count = Math.Min(this._size - this._written, count);
				if (count > 0)
				{
					this._next.Write(buffer, index, count);
				}
				this._written += count;
			}

			// Token: 0x06001C9C RID: 7324 RVA: 0x00088197 File Offset: 0x00086397
			public override Task WriteAsync(char value)
			{
				if (this._written < this._size)
				{
					this._written++;
					return this._next.WriteAsync(value);
				}
				return Task.CompletedTask;
			}

			// Token: 0x06001C9D RID: 7325 RVA: 0x000881C8 File Offset: 0x000863C8
			public override Task WriteAsync(char[] buffer, int index, int count)
			{
				TdsParser.ConstrainedTextWriter.ValidateWriteParameters(buffer, index, count);
				count = Math.Min(this._size - this._written, count);
				if (count > 0)
				{
					this._written += count;
					return this._next.WriteAsync(buffer, index, count);
				}
				return Task.CompletedTask;
			}

			// Token: 0x06001C9E RID: 7326 RVA: 0x00088218 File Offset: 0x00086418
			public override Task WriteAsync(string value)
			{
				return base.WriteAsync(value.ToCharArray());
			}

			// Token: 0x06001C9F RID: 7327 RVA: 0x00088228 File Offset: 0x00086428
			internal static void ValidateWriteParameters(char[] buffer, int offset, int count)
			{
				if (buffer == null)
				{
					throw ADP.ArgumentNull("buffer");
				}
				if (offset < 0)
				{
					throw ADP.ArgumentOutOfRange("offset");
				}
				if (count < 0)
				{
					throw ADP.ArgumentOutOfRange("count");
				}
				try
				{
					if (checked(offset + count) > buffer.Length)
					{
						throw ExceptionBuilder.InvalidOffsetLength();
					}
				}
				catch (OverflowException)
				{
					throw ExceptionBuilder.InvalidOffsetLength();
				}
			}

			// Token: 0x0400133F RID: 4927
			private TextWriter _next;

			// Token: 0x04001340 RID: 4928
			private int _size;

			// Token: 0x04001341 RID: 4929
			private int _written;
		}

		// Token: 0x0200024B RID: 587
		[CompilerGenerated]
		private sealed class <>c__DisplayClass83_0
		{
			// Token: 0x06001CA0 RID: 7328 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass83_0()
			{
			}

			// Token: 0x06001CA1 RID: 7329 RVA: 0x0008828C File Offset: 0x0008648C
			internal void <ThrowExceptionAndWarning>b__0(Action closeAction)
			{
				TdsParser.<>c__DisplayClass83_1 CS$<>8__locals1 = new TdsParser.<>c__DisplayClass83_1();
				CS$<>8__locals1.CS$<>8__locals1 = this;
				CS$<>8__locals1.closeAction = closeAction;
				Task.Factory.StartNew(new Action(CS$<>8__locals1.<ThrowExceptionAndWarning>b__1));
			}

			// Token: 0x04001342 RID: 4930
			public SqlInternalConnectionTds connHandler;
		}

		// Token: 0x0200024C RID: 588
		[CompilerGenerated]
		private sealed class <>c__DisplayClass83_1
		{
			// Token: 0x06001CA2 RID: 7330 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass83_1()
			{
			}

			// Token: 0x06001CA3 RID: 7331 RVA: 0x000882C4 File Offset: 0x000864C4
			internal void <ThrowExceptionAndWarning>b__1()
			{
				this.CS$<>8__locals1.connHandler._parserLock.Wait(false);
				this.CS$<>8__locals1.connHandler.ThreadHasParserLockForClose = true;
				try
				{
					this.closeAction();
				}
				finally
				{
					this.CS$<>8__locals1.connHandler.ThreadHasParserLockForClose = false;
					this.CS$<>8__locals1.connHandler._parserLock.Release();
				}
			}

			// Token: 0x04001343 RID: 4931
			public Action closeAction;

			// Token: 0x04001344 RID: 4932
			public TdsParser.<>c__DisplayClass83_0 CS$<>8__locals1;
		}

		// Token: 0x0200024D RID: 589
		[CompilerGenerated]
		private sealed class <>c__DisplayClass98_0
		{
			// Token: 0x06001CA4 RID: 7332 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass98_0()
			{
			}

			// Token: 0x06001CA5 RID: 7333 RVA: 0x0008833C File Offset: 0x0008653C
			internal bool <TryRun>b__0()
			{
				return !this.stateObj._attentionSending;
			}

			// Token: 0x04001345 RID: 4933
			public TdsParserStateObject stateObj;
		}

		// Token: 0x0200024E RID: 590
		[CompilerGenerated]
		private sealed class <>c__DisplayClass179_0
		{
			// Token: 0x06001CA6 RID: 7334 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass179_0()
			{
			}

			// Token: 0x06001CA7 RID: 7335 RVA: 0x00088350 File Offset: 0x00086550
			internal void <TdsExecuteSQLBatch>b__0(Task t)
			{
				try
				{
					if (t.IsFaulted)
					{
						this.<>4__this.FailureCleanup(this.stateObj, t.Exception.InnerException);
						throw t.Exception.InnerException;
					}
					this.stateObj.SniContext = SniContext.Snix_Read;
				}
				finally
				{
					if (this.taskReleaseConnectionLock)
					{
						this.<>4__this._connHandler._parserLock.Release();
					}
				}
			}

			// Token: 0x04001346 RID: 4934
			public TdsParser <>4__this;

			// Token: 0x04001347 RID: 4935
			public TdsParserStateObject stateObj;

			// Token: 0x04001348 RID: 4936
			public bool taskReleaseConnectionLock;
		}

		// Token: 0x0200024F RID: 591
		[CompilerGenerated]
		private sealed class <>c__DisplayClass180_0
		{
			// Token: 0x06001CA8 RID: 7336 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass180_0()
			{
			}

			// Token: 0x06001CA9 RID: 7337 RVA: 0x000883CC File Offset: 0x000865CC
			internal void <TdsExecuteRPC>b__1(Exception exc)
			{
				this.<>4__this.TdsExecuteRPC_OnFailure(exc, this.stateObj);
			}

			// Token: 0x06001CAA RID: 7338 RVA: 0x000883E0 File Offset: 0x000865E0
			internal void <TdsExecuteRPC>b__2(Task _)
			{
				this.<>4__this._connHandler._parserLock.Release();
			}

			// Token: 0x04001349 RID: 4937
			public TdsParser <>4__this;

			// Token: 0x0400134A RID: 4938
			public _SqlRPC[] rpcArray;

			// Token: 0x0400134B RID: 4939
			public int timeout;

			// Token: 0x0400134C RID: 4940
			public bool inSchema;

			// Token: 0x0400134D RID: 4941
			public SqlNotificationRequest notificationRequest;

			// Token: 0x0400134E RID: 4942
			public TdsParserStateObject stateObj;

			// Token: 0x0400134F RID: 4943
			public bool isCommandProc;

			// Token: 0x04001350 RID: 4944
			public bool sync;

			// Token: 0x04001351 RID: 4945
			public TaskCompletionSource<object> completion;

			// Token: 0x04001352 RID: 4946
			public Action<Exception> <>9__1;

			// Token: 0x04001353 RID: 4947
			public Action<Task> <>9__2;
		}

		// Token: 0x02000250 RID: 592
		[CompilerGenerated]
		private sealed class <>c__DisplayClass180_1
		{
			// Token: 0x06001CAB RID: 7339 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass180_1()
			{
			}

			// Token: 0x04001354 RID: 4948
			public int ii;

			// Token: 0x04001355 RID: 4949
			public TdsParser.<>c__DisplayClass180_0 CS$<>8__locals1;
		}

		// Token: 0x02000251 RID: 593
		[CompilerGenerated]
		private sealed class <>c__DisplayClass180_2
		{
			// Token: 0x06001CAC RID: 7340 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass180_2()
			{
			}

			// Token: 0x06001CAD RID: 7341 RVA: 0x000883F8 File Offset: 0x000865F8
			internal void <TdsExecuteRPC>b__0()
			{
				this.CS$<>8__locals2.CS$<>8__locals1.<>4__this.TdsExecuteRPC(this.CS$<>8__locals2.CS$<>8__locals1.rpcArray, this.CS$<>8__locals2.CS$<>8__locals1.timeout, this.CS$<>8__locals2.CS$<>8__locals1.inSchema, this.CS$<>8__locals2.CS$<>8__locals1.notificationRequest, this.CS$<>8__locals2.CS$<>8__locals1.stateObj, this.CS$<>8__locals2.CS$<>8__locals1.isCommandProc, this.CS$<>8__locals2.CS$<>8__locals1.sync, this.CS$<>8__locals2.CS$<>8__locals1.completion, this.CS$<>8__locals2.ii, this.i + 1);
			}

			// Token: 0x04001356 RID: 4950
			public int i;

			// Token: 0x04001357 RID: 4951
			public TdsParser.<>c__DisplayClass180_1 CS$<>8__locals2;
		}

		// Token: 0x02000252 RID: 594
		[CompilerGenerated]
		private sealed class <>c__DisplayClass180_3
		{
			// Token: 0x06001CAE RID: 7342 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass180_3()
			{
			}

			// Token: 0x06001CAF RID: 7343 RVA: 0x000884AE File Offset: 0x000866AE
			internal void <TdsExecuteRPC>b__3(Task tsk)
			{
				this.CS$<>8__locals3.<>4__this.ExecuteFlushTaskCallback(tsk, this.CS$<>8__locals3.stateObj, this.CS$<>8__locals3.completion, this.taskReleaseConnectionLock);
			}

			// Token: 0x04001358 RID: 4952
			public bool taskReleaseConnectionLock;

			// Token: 0x04001359 RID: 4953
			public TdsParser.<>c__DisplayClass180_0 CS$<>8__locals3;
		}

		// Token: 0x02000253 RID: 595
		[CompilerGenerated]
		private sealed class <>c__DisplayClass196_0
		{
			// Token: 0x06001CB0 RID: 7344 RVA: 0x00003D93 File Offset: 0x00001F93
			public <>c__DisplayClass196_0()
			{
			}

			// Token: 0x06001CB1 RID: 7345 RVA: 0x000884E0 File Offset: 0x000866E0
			internal Task <WriteBulkCopyValueSetupContinuation>b__0(Task t)
			{
				this.<>4__this._defaultEncoding = this.saveEncoding;
				this.<>4__this._defaultCollation = this.saveCollation;
				this.<>4__this._defaultCodePage = this.saveCodePage;
				this.<>4__this._defaultLCID = this.saveLCID;
				return t;
			}

			// Token: 0x0400135A RID: 4954
			public TdsParser <>4__this;

			// Token: 0x0400135B RID: 4955
			public Encoding saveEncoding;

			// Token: 0x0400135C RID: 4956
			public SqlCollation saveCollation;

			// Token: 0x0400135D RID: 4957
			public int saveCodePage;

			// Token: 0x0400135E RID: 4958
			public int saveLCID;
		}

		// Token: 0x02000254 RID: 596
		[CompilerGenerated]
		[StructLayout(LayoutKind.Auto)]
		private struct <WriteXmlFeed>d__208 : IAsyncStateMachine
		{
			// Token: 0x06001CB2 RID: 7346 RVA: 0x00088534 File Offset: 0x00086734
			void IAsyncStateMachine.MoveNext()
			{
				int num = this.<>1__state;
				TdsParser tdsParser = this.<>4__this;
				try
				{
					ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
					if (num == 0)
					{
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_15E;
					}
					if (num == 1)
					{
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_20B;
					}
					byte[] preambleToStrip = null;
					if (!this.needBom)
					{
						preambleToStrip = this.encoding.GetPreamble();
					}
					this.<writer>5__2 = new TdsParser.ConstrainedTextWriter(new StreamWriter(new TdsParser.TdsOutputStream(tdsParser, this.stateObj, preambleToStrip), this.encoding), this.size);
					XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
					xmlWriterSettings.CloseOutput = false;
					xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
					if (tdsParser._asyncWrite)
					{
						xmlWriterSettings.Async = true;
					}
					this.<ww>5__3 = XmlWriter.Create(this.<writer>5__2, xmlWriterSettings);
					if (this.feed._source.ReadState == ReadState.Initial)
					{
						this.feed._source.Read();
					}
					IL_17E:
					while (!this.feed._source.EOF && !this.<writer>5__2.IsComplete)
					{
						if (this.feed._source.NodeType == XmlNodeType.XmlDeclaration)
						{
							this.feed._source.Read();
						}
						else if (tdsParser._asyncWrite)
						{
							awaiter = this.<ww>5__3.WriteNodeAsync(this.feed._source, true).ConfigureAwait(false).GetAwaiter();
							if (!awaiter.IsCompleted)
							{
								this.<>1__state = 0;
								this.<>u__1 = awaiter;
								this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteXmlFeed>d__208>(ref awaiter, ref this);
								return;
							}
							goto IL_15E;
						}
						else
						{
							this.<ww>5__3.WriteNode(this.feed._source, true);
						}
					}
					if (!tdsParser._asyncWrite)
					{
						this.<ww>5__3.Flush();
						goto IL_21F;
					}
					awaiter = this.<ww>5__3.FlushAsync().ConfigureAwait(false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						this.<>1__state = 1;
						this.<>u__1 = awaiter;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteXmlFeed>d__208>(ref awaiter, ref this);
						return;
					}
					goto IL_20B;
					IL_15E:
					awaiter.GetResult();
					goto IL_17E;
					IL_20B:
					awaiter.GetResult();
					IL_21F:;
				}
				catch (Exception exception)
				{
					this.<>1__state = -2;
					this.<writer>5__2 = null;
					this.<ww>5__3 = null;
					this.<>t__builder.SetException(exception);
					return;
				}
				this.<>1__state = -2;
				this.<writer>5__2 = null;
				this.<ww>5__3 = null;
				this.<>t__builder.SetResult();
			}

			// Token: 0x06001CB3 RID: 7347 RVA: 0x000887C8 File Offset: 0x000869C8
			[DebuggerHidden]
			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				this.<>t__builder.SetStateMachine(stateMachine);
			}

			// Token: 0x0400135F RID: 4959
			public int <>1__state;

			// Token: 0x04001360 RID: 4960
			public AsyncTaskMethodBuilder <>t__builder;

			// Token: 0x04001361 RID: 4961
			public bool needBom;

			// Token: 0x04001362 RID: 4962
			public Encoding encoding;

			// Token: 0x04001363 RID: 4963
			public TdsParser <>4__this;

			// Token: 0x04001364 RID: 4964
			public TdsParserStateObject stateObj;

			// Token: 0x04001365 RID: 4965
			public int size;

			// Token: 0x04001366 RID: 4966
			public XmlDataFeed feed;

			// Token: 0x04001367 RID: 4967
			private TdsParser.ConstrainedTextWriter <writer>5__2;

			// Token: 0x04001368 RID: 4968
			private XmlWriter <ww>5__3;

			// Token: 0x04001369 RID: 4969
			private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter <>u__1;
		}

		// Token: 0x02000255 RID: 597
		[CompilerGenerated]
		[StructLayout(LayoutKind.Auto)]
		private struct <WriteTextFeed>d__209 : IAsyncStateMachine
		{
			// Token: 0x06001CB4 RID: 7348 RVA: 0x000887D8 File Offset: 0x000869D8
			void IAsyncStateMachine.MoveNext()
			{
				int num = this.<>1__state;
				TdsParser tdsParser = this.<>4__this;
				try
				{
					ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
					ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter awaiter2;
					switch (num)
					{
					case 0:
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						break;
					case 1:
						awaiter2 = this.<>u__2;
						this.<>u__2 = default(ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_197;
					case 2:
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_24E;
					case 3:
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_2F9;
					default:
						this.<inBuff>5__2 = new char[4096];
						this.encoding = (this.encoding ?? new UnicodeEncoding(false, false));
						this.<writer>5__3 = new TdsParser.ConstrainedTextWriter(new StreamWriter(new TdsParser.TdsOutputStream(tdsParser, this.stateObj, null), this.encoding), this.size);
						if (!this.needBom)
						{
							goto IL_107;
						}
						if (!tdsParser._asyncWrite)
						{
							this.<writer>5__3.Write('﻿');
							goto IL_107;
						}
						awaiter = this.<writer>5__3.WriteAsync('﻿').ConfigureAwait(false).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							this.<>1__state = 0;
							this.<>u__1 = awaiter;
							this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteTextFeed>d__209>(ref awaiter, ref this);
							return;
						}
						break;
					}
					awaiter.GetResult();
					IL_107:
					this.<nWritten>5__4 = 0;
					IL_10E:
					this.<nRead>5__5 = 0;
					if (!tdsParser._asyncWrite)
					{
						this.<nRead>5__5 = this.feed._source.ReadBlock(this.<inBuff>5__2, 0, 4096);
						goto IL_1CC;
					}
					awaiter2 = this.feed._source.ReadBlockAsync(this.<inBuff>5__2, 0, 4096).ConfigureAwait(false).GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						this.<>1__state = 1;
						this.<>u__2 = awaiter2;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter, TdsParser.<WriteTextFeed>d__209>(ref awaiter2, ref this);
						return;
					}
					IL_197:
					int result = awaiter2.GetResult();
					this.<nRead>5__5 = result;
					IL_1CC:
					if (this.<nRead>5__5 == 0)
					{
						goto IL_292;
					}
					if (!tdsParser._asyncWrite)
					{
						this.<writer>5__3.Write(this.<inBuff>5__2, 0, this.<nRead>5__5);
						goto IL_26F;
					}
					awaiter = this.<writer>5__3.WriteAsync(this.<inBuff>5__2, 0, this.<nRead>5__5).ConfigureAwait(false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						this.<>1__state = 2;
						this.<>u__1 = awaiter;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteTextFeed>d__209>(ref awaiter, ref this);
						return;
					}
					IL_24E:
					awaiter.GetResult();
					IL_26F:
					this.<nWritten>5__4 += this.<nRead>5__5;
					if (!this.<writer>5__3.IsComplete)
					{
						goto IL_10E;
					}
					IL_292:
					if (!tdsParser._asyncWrite)
					{
						this.<writer>5__3.Flush();
						goto IL_30D;
					}
					awaiter = this.<writer>5__3.FlushAsync().ConfigureAwait(false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						this.<>1__state = 3;
						this.<>u__1 = awaiter;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteTextFeed>d__209>(ref awaiter, ref this);
						return;
					}
					IL_2F9:
					awaiter.GetResult();
					IL_30D:;
				}
				catch (Exception exception)
				{
					this.<>1__state = -2;
					this.<inBuff>5__2 = null;
					this.<writer>5__3 = null;
					this.<>t__builder.SetException(exception);
					return;
				}
				this.<>1__state = -2;
				this.<inBuff>5__2 = null;
				this.<writer>5__3 = null;
				this.<>t__builder.SetResult();
			}

			// Token: 0x06001CB5 RID: 7349 RVA: 0x00088B58 File Offset: 0x00086D58
			[DebuggerHidden]
			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				this.<>t__builder.SetStateMachine(stateMachine);
			}

			// Token: 0x0400136A RID: 4970
			public int <>1__state;

			// Token: 0x0400136B RID: 4971
			public AsyncTaskMethodBuilder <>t__builder;

			// Token: 0x0400136C RID: 4972
			public Encoding encoding;

			// Token: 0x0400136D RID: 4973
			public TdsParser <>4__this;

			// Token: 0x0400136E RID: 4974
			public TdsParserStateObject stateObj;

			// Token: 0x0400136F RID: 4975
			public int size;

			// Token: 0x04001370 RID: 4976
			public bool needBom;

			// Token: 0x04001371 RID: 4977
			public TextDataFeed feed;

			// Token: 0x04001372 RID: 4978
			private char[] <inBuff>5__2;

			// Token: 0x04001373 RID: 4979
			private TdsParser.ConstrainedTextWriter <writer>5__3;

			// Token: 0x04001374 RID: 4980
			private int <nWritten>5__4;

			// Token: 0x04001375 RID: 4981
			private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter <>u__1;

			// Token: 0x04001376 RID: 4982
			private int <nRead>5__5;

			// Token: 0x04001377 RID: 4983
			private ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter <>u__2;
		}

		// Token: 0x02000256 RID: 598
		[CompilerGenerated]
		[StructLayout(LayoutKind.Auto)]
		private struct <WriteStreamFeed>d__210 : IAsyncStateMachine
		{
			// Token: 0x06001CB6 RID: 7350 RVA: 0x00088B68 File Offset: 0x00086D68
			void IAsyncStateMachine.MoveNext()
			{
				int num = this.<>1__state;
				TdsParser tdsParser = this.<>4__this;
				try
				{
					ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter awaiter;
					if (num == 0)
					{
						awaiter = this.<>u__1;
						this.<>u__1 = default(ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_F7;
					}
					ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter2;
					if (num == 1)
					{
						awaiter2 = this.<>u__2;
						this.<>u__2 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						this.<>1__state = -1;
						goto IL_1AE;
					}
					this.<output>5__2 = new TdsParser.TdsOutputStream(tdsParser, this.stateObj, null);
					this.<buff>5__3 = new byte[4096];
					this.<nWritten>5__4 = 0;
					IL_45:
					this.<nRead>5__5 = 0;
					int num2 = 4096;
					if (this.len > 0 && this.<nWritten>5__4 + num2 > this.len)
					{
						num2 = this.len - this.<nWritten>5__4;
					}
					if (!tdsParser._asyncWrite)
					{
						this.<nRead>5__5 = this.feed._source.Read(this.<buff>5__3, 0, num2);
						goto IL_126;
					}
					awaiter = this.feed._source.ReadAsync(this.<buff>5__3, 0, num2).ConfigureAwait(false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						this.<>1__state = 0;
						this.<>u__1 = awaiter;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter, TdsParser.<WriteStreamFeed>d__210>(ref awaiter, ref this);
						return;
					}
					IL_F7:
					int result = awaiter.GetResult();
					this.<nRead>5__5 = result;
					IL_126:
					if (this.<nRead>5__5 == 0)
					{
						goto IL_228;
					}
					if (!tdsParser._asyncWrite)
					{
						this.<output>5__2.Write(this.<buff>5__3, 0, this.<nRead>5__5);
						goto IL_1CF;
					}
					awaiter2 = this.<output>5__2.WriteAsync(this.<buff>5__3, 0, this.<nRead>5__5).ConfigureAwait(false).GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						this.<>1__state = 1;
						this.<>u__2 = awaiter2;
						this.<>t__builder.AwaitUnsafeOnCompleted<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, TdsParser.<WriteStreamFeed>d__210>(ref awaiter2, ref this);
						return;
					}
					IL_1AE:
					awaiter2.GetResult();
					IL_1CF:
					this.<nWritten>5__4 += this.<nRead>5__5;
					if (this.len <= 0 || this.<nWritten>5__4 < this.len)
					{
						goto IL_45;
					}
				}
				catch (Exception exception)
				{
					this.<>1__state = -2;
					this.<output>5__2 = null;
					this.<buff>5__3 = null;
					this.<>t__builder.SetException(exception);
					return;
				}
				IL_228:
				this.<>1__state = -2;
				this.<output>5__2 = null;
				this.<buff>5__3 = null;
				this.<>t__builder.SetResult();
			}

			// Token: 0x06001CB7 RID: 7351 RVA: 0x00088DDC File Offset: 0x00086FDC
			[DebuggerHidden]
			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				this.<>t__builder.SetStateMachine(stateMachine);
			}

			// Token: 0x04001378 RID: 4984
			public int <>1__state;

			// Token: 0x04001379 RID: 4985
			public AsyncTaskMethodBuilder <>t__builder;

			// Token: 0x0400137A RID: 4986
			public TdsParser <>4__this;

			// Token: 0x0400137B RID: 4987
			public TdsParserStateObject stateObj;

			// Token: 0x0400137C RID: 4988
			public int len;

			// Token: 0x0400137D RID: 4989
			public StreamDataFeed feed;

			// Token: 0x0400137E RID: 4990
			private TdsParser.TdsOutputStream <output>5__2;

			// Token: 0x0400137F RID: 4991
			private byte[] <buff>5__3;

			// Token: 0x04001380 RID: 4992
			private int <nWritten>5__4;

			// Token: 0x04001381 RID: 4993
			private int <nRead>5__5;

			// Token: 0x04001382 RID: 4994
			private ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter <>u__1;

			// Token: 0x04001383 RID: 4995
			private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter <>u__2;
		}
	}
}
