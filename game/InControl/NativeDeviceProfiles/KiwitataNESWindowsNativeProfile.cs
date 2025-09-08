﻿using System;

namespace InControl.NativeDeviceProfiles
{
	// Token: 0x020001EE RID: 494
	[Preserve]
	[NativeInputDeviceProfile]
	public class KiwitataNESWindowsNativeProfile : InputDeviceProfile
	{
		// Token: 0x060008CC RID: 2252 RVA: 0x0004A040 File Offset: 0x00048240
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Kiwitata NES Controller";
			base.DeviceNotes = "Kiwitata NES on Windows";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.NintendoNES;
			base.IncludePlatforms = new string[]
			{
				"Windows"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.DirectInput,
					VendorID = 121,
					ProductID = 17
				}
			};
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(2)
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action2,
					Source = InputDeviceProfile.Button(1)
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(3)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = InputDeviceProfile.Button(0)
				},
				new InputControlMapping
				{
					Name = "L1",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "R1",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "L2",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "R2",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "Select",
					Target = InputControlType.Select,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(9)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				InputDeviceProfile.DPadLeftMapping(1),
				InputDeviceProfile.DPadRightMapping(1),
				InputDeviceProfile.DPadUpMapping(0),
				InputDeviceProfile.DPadDownMapping(0)
			};
		}

		// Token: 0x060008CD RID: 2253 RVA: 0x0004A296 File Offset: 0x00048496
		public KiwitataNESWindowsNativeProfile()
		{
		}
	}
}
