﻿using System;

namespace System.ComponentModel
{
	/// <summary>Represents the method that will handle the <see cref="E:System.ComponentModel.INotifyPropertyChanged.PropertyChanged" /> event raised when a property is changed on a component.</summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="T:System.ComponentModel.PropertyChangedEventArgs" /> that contains the event data.</param>
	// Token: 0x02000404 RID: 1028
	// (Invoke) Token: 0x0600214C RID: 8524
	public delegate void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e);
}
