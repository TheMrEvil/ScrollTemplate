﻿using System;

namespace InControl.NativeDeviceProfiles
{
	// Token: 0x02000140 RID: 320
	[Preserve]
	[NativeInputDeviceProfile]
	public class HoriEX2ControllerMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		// Token: 0x0600074B RID: 1867 RVA: 0x00039434 File Offset: 0x00037634
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Hori EX2 Controller";
			base.DeviceNotes = "Hori EX2 Controller on Mac";
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.HID,
					VendorID = 3853,
					ProductID = 13
				},
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.HID,
					VendorID = 7085,
					ProductID = 62721
				},
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.HID,
					VendorID = 9414,
					ProductID = 21760
				}
			};
		}

		// Token: 0x0600074C RID: 1868 RVA: 0x00039523 File Offset: 0x00037723
		public HoriEX2ControllerMacNativeProfile()
		{
		}
	}
}
