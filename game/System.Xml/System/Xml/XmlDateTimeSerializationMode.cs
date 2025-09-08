﻿using System;

namespace System.Xml
{
	/// <summary>Specifies how to treat the time value when converting between string and <see cref="T:System.DateTime" />.</summary>
	// Token: 0x02000227 RID: 551
	public enum XmlDateTimeSerializationMode
	{
		/// <summary>Treat as local time. If the <see cref="T:System.DateTime" /> object represents a Coordinated Universal Time (UTC), it is converted to the local time.</summary>
		// Token: 0x040012B3 RID: 4787
		Local,
		/// <summary>Treat as a UTC. If the <see cref="T:System.DateTime" /> object represents a local time, it is converted to a UTC.</summary>
		// Token: 0x040012B4 RID: 4788
		Utc,
		/// <summary>Treat as a local time if a <see cref="T:System.DateTime" /> is being converted to a string.</summary>
		// Token: 0x040012B5 RID: 4789
		Unspecified,
		/// <summary>Time zone information should be preserved when converting.</summary>
		// Token: 0x040012B6 RID: 4790
		RoundtripKind
	}
}
