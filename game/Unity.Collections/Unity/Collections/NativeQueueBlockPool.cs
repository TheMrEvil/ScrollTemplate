﻿using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
	// Token: 0x020000CB RID: 203
	internal class NativeQueueBlockPool
	{
		// Token: 0x0600079D RID: 1949 RVA: 0x0001752C File Offset: 0x0001572C
		internal unsafe static NativeQueueBlockPoolData* GetQueueBlockPool()
		{
			NativeQueueBlockPoolData** unsafeDataPointer = (NativeQueueBlockPoolData**)NativeQueueBlockPool.Data.UnsafeDataPointer;
			NativeQueueBlockPoolData* ptr = *(IntPtr*)unsafeDataPointer;
			if (ptr == null)
			{
				ptr = (NativeQueueBlockPoolData*)Memory.Unmanaged.Allocate((long)UnsafeUtility.SizeOf<NativeQueueBlockPoolData>(), 8, Allocator.Persistent);
				*(IntPtr*)unsafeDataPointer = ptr;
				ptr->m_NumBlocks = (ptr->m_MaxBlocks = 256);
				ptr->m_AllocLock = 0;
				NativeQueueBlockHeader* ptr2 = null;
				for (int i = 0; i < ptr->m_MaxBlocks; i++)
				{
					NativeQueueBlockHeader* ptr3 = (NativeQueueBlockHeader*)Memory.Unmanaged.Allocate(16384L, 16, Allocator.Persistent);
					ptr3->m_NextBlock = ptr2;
					ptr2 = ptr3;
				}
				ptr->m_FirstBlock = (IntPtr)((void*)ptr2);
				NativeQueueBlockPool.AppDomainOnDomainUnload();
			}
			return ptr;
		}

		// Token: 0x0600079E RID: 1950 RVA: 0x000175C7 File Offset: 0x000157C7
		[BurstDiscard]
		private static void AppDomainOnDomainUnload()
		{
			AppDomain.CurrentDomain.DomainUnload += NativeQueueBlockPool.OnDomainUnload;
		}

		// Token: 0x0600079F RID: 1951 RVA: 0x000175E0 File Offset: 0x000157E0
		private unsafe static void OnDomainUnload(object sender, EventArgs e)
		{
			NativeQueueBlockPoolData** unsafeDataPointer = (NativeQueueBlockPoolData**)NativeQueueBlockPool.Data.UnsafeDataPointer;
			NativeQueueBlockPoolData* ptr = *(IntPtr*)unsafeDataPointer;
			while (ptr->m_FirstBlock != IntPtr.Zero)
			{
				NativeQueueBlockHeader* ptr2 = (NativeQueueBlockHeader*)((void*)ptr->m_FirstBlock);
				ptr->m_FirstBlock = (IntPtr)((void*)ptr2->m_NextBlock);
				Memory.Unmanaged.Free<NativeQueueBlockHeader>(ptr2, Allocator.Persistent);
				ptr->m_NumBlocks--;
			}
			Memory.Unmanaged.Free<NativeQueueBlockPoolData>(ptr, Allocator.Persistent);
			*(IntPtr*)unsafeDataPointer = (IntPtr)((UIntPtr)0);
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x000020EA File Offset: 0x000002EA
		public NativeQueueBlockPool()
		{
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x00017653 File Offset: 0x00015853
		// Note: this type is marked as 'beforefieldinit'.
		static NativeQueueBlockPool()
		{
		}

		// Token: 0x040002A2 RID: 674
		private static readonly SharedStatic<IntPtr> Data = SharedStatic<IntPtr>.GetOrCreateUnsafe(0U, -1167712759576517144L, 0L);
	}
}
