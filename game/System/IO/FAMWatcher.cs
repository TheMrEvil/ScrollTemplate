﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
	// Token: 0x02000510 RID: 1296
	internal class FAMWatcher : IFileWatcher
	{
		// Token: 0x060029F1 RID: 10737 RVA: 0x0000219B File Offset: 0x0000039B
		private FAMWatcher()
		{
		}

		// Token: 0x060029F2 RID: 10738 RVA: 0x00090298 File Offset: 0x0008E498
		public static bool GetInstance(out IFileWatcher watcher, bool gamin)
		{
			if (FAMWatcher.failed)
			{
				watcher = null;
				return false;
			}
			if (FAMWatcher.instance != null)
			{
				watcher = FAMWatcher.instance;
				return true;
			}
			FAMWatcher.use_gamin = gamin;
			FAMWatcher.watches = Hashtable.Synchronized(new Hashtable());
			FAMWatcher.requests = Hashtable.Synchronized(new Hashtable());
			if (FAMWatcher.FAMOpen(out FAMWatcher.conn) == -1)
			{
				FAMWatcher.failed = true;
				watcher = null;
				return false;
			}
			FAMWatcher.instance = new FAMWatcher();
			watcher = FAMWatcher.instance;
			return true;
		}

		// Token: 0x060029F3 RID: 10739 RVA: 0x00090310 File Offset: 0x0008E510
		public void StartDispatching(object handle)
		{
			FileSystemWatcher fileSystemWatcher = handle as FileSystemWatcher;
			FAMWatcher obj = this;
			FAMData famdata;
			lock (obj)
			{
				if (FAMWatcher.thread == null)
				{
					FAMWatcher.thread = new Thread(new ThreadStart(this.Monitor));
					FAMWatcher.thread.IsBackground = true;
					FAMWatcher.thread.Start();
				}
				famdata = (FAMData)FAMWatcher.watches[fileSystemWatcher];
			}
			if (famdata == null)
			{
				famdata = new FAMData();
				famdata.FSW = fileSystemWatcher;
				famdata.Directory = fileSystemWatcher.FullPath;
				famdata.FileMask = fileSystemWatcher.MangledFilter;
				famdata.IncludeSubdirs = fileSystemWatcher.IncludeSubdirectories;
				if (famdata.IncludeSubdirs)
				{
					famdata.SubDirs = new Hashtable();
				}
				famdata.Enabled = true;
				FAMWatcher.StartMonitoringDirectory(famdata, false);
				obj = this;
				lock (obj)
				{
					FAMWatcher.watches[fileSystemWatcher] = famdata;
					FAMWatcher.requests[famdata.Request.ReqNum] = famdata;
					FAMWatcher.stop = false;
				}
			}
		}

		// Token: 0x060029F4 RID: 10740 RVA: 0x00090438 File Offset: 0x0008E638
		private static void StartMonitoringDirectory(FAMData data, bool justcreated)
		{
			FAMRequest request;
			if (FAMWatcher.FAMMonitorDirectory(ref FAMWatcher.conn, data.Directory, out request, IntPtr.Zero) == -1)
			{
				throw new Win32Exception();
			}
			FileSystemWatcher fsw = data.FSW;
			data.Request = request;
			if (data.IncludeSubdirs)
			{
				foreach (string text in Directory.GetDirectories(data.Directory))
				{
					FAMData famdata = new FAMData();
					famdata.FSW = data.FSW;
					famdata.Directory = text;
					famdata.FileMask = data.FSW.MangledFilter;
					famdata.IncludeSubdirs = true;
					famdata.SubDirs = new Hashtable();
					famdata.Enabled = true;
					if (justcreated)
					{
						FileSystemWatcher obj = fsw;
						lock (obj)
						{
							RenamedEventArgs renamedEventArgs = null;
							fsw.DispatchEvents(FileAction.Added, text, ref renamedEventArgs);
							if (fsw.Waiting)
							{
								fsw.Waiting = false;
								System.Threading.Monitor.PulseAll(fsw);
							}
						}
					}
					FAMWatcher.StartMonitoringDirectory(famdata, justcreated);
					data.SubDirs[text] = famdata;
					FAMWatcher.requests[famdata.Request.ReqNum] = famdata;
				}
			}
			if (justcreated)
			{
				foreach (string filename in Directory.GetFiles(data.Directory))
				{
					FileSystemWatcher obj = fsw;
					lock (obj)
					{
						RenamedEventArgs renamedEventArgs2 = null;
						fsw.DispatchEvents(FileAction.Added, filename, ref renamedEventArgs2);
						fsw.DispatchEvents(FileAction.Modified, filename, ref renamedEventArgs2);
						if (fsw.Waiting)
						{
							fsw.Waiting = false;
							System.Threading.Monitor.PulseAll(fsw);
						}
					}
				}
			}
		}

		// Token: 0x060029F5 RID: 10741 RVA: 0x000905F0 File Offset: 0x0008E7F0
		public void StopDispatching(object handle)
		{
			FileSystemWatcher key = handle as FileSystemWatcher;
			lock (this)
			{
				FAMData famdata = (FAMData)FAMWatcher.watches[key];
				if (famdata != null)
				{
					FAMWatcher.StopMonitoringDirectory(famdata);
					FAMWatcher.watches.Remove(key);
					FAMWatcher.requests.Remove(famdata.Request.ReqNum);
					if (FAMWatcher.watches.Count == 0)
					{
						FAMWatcher.stop = true;
					}
					if (famdata.IncludeSubdirs)
					{
						foreach (object obj in famdata.SubDirs.Values)
						{
							FAMData famdata2 = (FAMData)obj;
							FAMWatcher.StopMonitoringDirectory(famdata2);
							FAMWatcher.requests.Remove(famdata2.Request.ReqNum);
						}
					}
				}
			}
		}

		// Token: 0x060029F6 RID: 10742 RVA: 0x000906FC File Offset: 0x0008E8FC
		private static void StopMonitoringDirectory(FAMData data)
		{
			if (FAMWatcher.FAMCancelMonitor(ref FAMWatcher.conn, ref data.Request) == -1)
			{
				throw new Win32Exception();
			}
		}

		// Token: 0x060029F7 RID: 10743 RVA: 0x00090718 File Offset: 0x0008E918
		private void Monitor()
		{
			FAMWatcher obj;
			while (!FAMWatcher.stop)
			{
				obj = this;
				int num;
				lock (obj)
				{
					num = FAMWatcher.FAMPending(ref FAMWatcher.conn);
				}
				if (num > 0)
				{
					this.ProcessEvents();
				}
				else
				{
					Thread.Sleep(500);
				}
			}
			obj = this;
			lock (obj)
			{
				FAMWatcher.thread = null;
				FAMWatcher.stop = false;
			}
		}

		// Token: 0x060029F8 RID: 10744 RVA: 0x000907A8 File Offset: 0x0008E9A8
		private void ProcessEvents()
		{
			ArrayList arrayList = null;
			lock (this)
			{
				string text;
				int num;
				int num2;
				while (FAMWatcher.InternalFAMNextEvent(ref FAMWatcher.conn, out text, out num, out num2) == 1)
				{
					bool flag2;
					switch (num)
					{
					case 1:
					case 2:
					case 5:
						flag2 = FAMWatcher.requests.ContainsKey(num2);
						break;
					case 3:
					case 4:
					case 6:
					case 7:
					case 8:
					case 9:
						goto IL_70;
					default:
						goto IL_70;
					}
					IL_73:
					if (flag2)
					{
						FAMData famdata = (FAMData)FAMWatcher.requests[num2];
						if (famdata.Enabled)
						{
							FileSystemWatcher fsw = famdata.FSW;
							NotifyFilters notifyFilter = fsw.NotifyFilter;
							RenamedEventArgs renamedEventArgs = null;
							FileAction fileAction = (FileAction)0;
							if (num == 1 && (notifyFilter & (NotifyFilters.Attributes | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size)) != (NotifyFilters)0)
							{
								fileAction = FileAction.Modified;
							}
							else if (num == 2)
							{
								fileAction = FileAction.Removed;
							}
							else if (num == 5)
							{
								fileAction = FileAction.Added;
							}
							if (fileAction != (FileAction)0)
							{
								if (fsw.IncludeSubdirectories)
								{
									string fullPath = fsw.FullPath;
									string text2 = famdata.Directory;
									if (text2 != fullPath)
									{
										int length = fullPath.Length;
										int num3 = 1;
										if (length > 1 && fullPath[length - 1] == Path.DirectorySeparatorChar)
										{
											num3 = 0;
										}
										string path = text2.Substring(fullPath.Length + num3);
										text2 = Path.Combine(text2, text);
										text = Path.Combine(path, text);
									}
									else
									{
										text2 = Path.Combine(fullPath, text);
									}
									if (fileAction == FileAction.Added && Directory.Exists(text2))
									{
										if (arrayList == null)
										{
											arrayList = new ArrayList(4);
										}
										arrayList.Add(new FAMData
										{
											FSW = fsw,
											Directory = text2,
											FileMask = fsw.MangledFilter,
											IncludeSubdirs = true,
											SubDirs = new Hashtable(),
											Enabled = true
										});
										arrayList.Add(famdata);
									}
								}
								if (!(text != famdata.Directory) || fsw.Pattern.IsMatch(text))
								{
									FileSystemWatcher obj = fsw;
									lock (obj)
									{
										fsw.DispatchEvents(fileAction, text, ref renamedEventArgs);
										if (fsw.Waiting)
										{
											fsw.Waiting = false;
											System.Threading.Monitor.PulseAll(fsw);
										}
									}
								}
							}
						}
					}
					if (FAMWatcher.FAMPending(ref FAMWatcher.conn) <= 0)
					{
						goto IL_24A;
					}
					continue;
					IL_70:
					flag2 = false;
					goto IL_73;
				}
				return;
			}
			IL_24A:
			if (arrayList != null)
			{
				int count = arrayList.Count;
				for (int i = 0; i < count; i += 2)
				{
					FAMData famdata2 = (FAMData)arrayList[i];
					FAMData famdata3 = (FAMData)arrayList[i + 1];
					FAMWatcher.StartMonitoringDirectory(famdata2, true);
					FAMWatcher.requests[famdata2.Request.ReqNum] = famdata2;
					FAMData obj2 = famdata3;
					lock (obj2)
					{
						famdata3.SubDirs[famdata2.Directory] = famdata2;
					}
				}
				arrayList.Clear();
			}
		}

		// Token: 0x060029F9 RID: 10745 RVA: 0x00090AEC File Offset: 0x0008ECEC
		~FAMWatcher()
		{
			FAMWatcher.FAMClose(ref FAMWatcher.conn);
		}

		// Token: 0x060029FA RID: 10746 RVA: 0x00090B20 File Offset: 0x0008ED20
		private static int FAMOpen(out FAMConnection fc)
		{
			if (FAMWatcher.use_gamin)
			{
				return FAMWatcher.gamin_Open(out fc);
			}
			return FAMWatcher.fam_Open(out fc);
		}

		// Token: 0x060029FB RID: 10747 RVA: 0x00090B36 File Offset: 0x0008ED36
		private static int FAMClose(ref FAMConnection fc)
		{
			if (FAMWatcher.use_gamin)
			{
				return FAMWatcher.gamin_Close(ref fc);
			}
			return FAMWatcher.fam_Close(ref fc);
		}

		// Token: 0x060029FC RID: 10748 RVA: 0x00090B4C File Offset: 0x0008ED4C
		private static int FAMMonitorDirectory(ref FAMConnection fc, string filename, out FAMRequest fr, IntPtr user_data)
		{
			if (FAMWatcher.use_gamin)
			{
				return FAMWatcher.gamin_MonitorDirectory(ref fc, filename, out fr, user_data);
			}
			return FAMWatcher.fam_MonitorDirectory(ref fc, filename, out fr, user_data);
		}

		// Token: 0x060029FD RID: 10749 RVA: 0x00090B68 File Offset: 0x0008ED68
		private static int FAMCancelMonitor(ref FAMConnection fc, ref FAMRequest fr)
		{
			if (FAMWatcher.use_gamin)
			{
				return FAMWatcher.gamin_CancelMonitor(ref fc, ref fr);
			}
			return FAMWatcher.fam_CancelMonitor(ref fc, ref fr);
		}

		// Token: 0x060029FE RID: 10750 RVA: 0x00090B80 File Offset: 0x0008ED80
		private static int FAMPending(ref FAMConnection fc)
		{
			if (FAMWatcher.use_gamin)
			{
				return FAMWatcher.gamin_Pending(ref fc);
			}
			return FAMWatcher.fam_Pending(ref fc);
		}

		// Token: 0x060029FF RID: 10751 RVA: 0x00003917 File Offset: 0x00001B17
		public void Dispose(object handle)
		{
		}

		// Token: 0x06002A00 RID: 10752
		[DllImport("libfam.so.0", EntryPoint = "FAMOpen")]
		private static extern int fam_Open(out FAMConnection fc);

		// Token: 0x06002A01 RID: 10753
		[DllImport("libfam.so.0", EntryPoint = "FAMClose")]
		private static extern int fam_Close(ref FAMConnection fc);

		// Token: 0x06002A02 RID: 10754
		[DllImport("libfam.so.0", EntryPoint = "FAMMonitorDirectory")]
		private static extern int fam_MonitorDirectory(ref FAMConnection fc, string filename, out FAMRequest fr, IntPtr user_data);

		// Token: 0x06002A03 RID: 10755
		[DllImport("libfam.so.0", EntryPoint = "FAMCancelMonitor")]
		private static extern int fam_CancelMonitor(ref FAMConnection fc, ref FAMRequest fr);

		// Token: 0x06002A04 RID: 10756
		[DllImport("libfam.so.0", EntryPoint = "FAMPending")]
		private static extern int fam_Pending(ref FAMConnection fc);

		// Token: 0x06002A05 RID: 10757
		[DllImport("libgamin-1.so.0", EntryPoint = "FAMOpen")]
		private static extern int gamin_Open(out FAMConnection fc);

		// Token: 0x06002A06 RID: 10758
		[DllImport("libgamin-1.so.0", EntryPoint = "FAMClose")]
		private static extern int gamin_Close(ref FAMConnection fc);

		// Token: 0x06002A07 RID: 10759
		[DllImport("libgamin-1.so.0", EntryPoint = "FAMMonitorDirectory")]
		private static extern int gamin_MonitorDirectory(ref FAMConnection fc, string filename, out FAMRequest fr, IntPtr user_data);

		// Token: 0x06002A08 RID: 10760
		[DllImport("libgamin-1.so.0", EntryPoint = "FAMCancelMonitor")]
		private static extern int gamin_CancelMonitor(ref FAMConnection fc, ref FAMRequest fr);

		// Token: 0x06002A09 RID: 10761
		[DllImport("libgamin-1.so.0", EntryPoint = "FAMPending")]
		private static extern int gamin_Pending(ref FAMConnection fc);

		// Token: 0x06002A0A RID: 10762
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int InternalFAMNextEvent(ref FAMConnection fc, out string filename, out int code, out int reqnum);

		// Token: 0x0400164C RID: 5708
		private static bool failed;

		// Token: 0x0400164D RID: 5709
		private static FAMWatcher instance;

		// Token: 0x0400164E RID: 5710
		private static Hashtable watches;

		// Token: 0x0400164F RID: 5711
		private static Hashtable requests;

		// Token: 0x04001650 RID: 5712
		private static FAMConnection conn;

		// Token: 0x04001651 RID: 5713
		private static Thread thread;

		// Token: 0x04001652 RID: 5714
		private static bool stop;

		// Token: 0x04001653 RID: 5715
		private static bool use_gamin;

		// Token: 0x04001654 RID: 5716
		private const NotifyFilters changed = NotifyFilters.Attributes | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size;
	}
}
