﻿using System;

namespace InControl.NativeDeviceProfiles
{
	// Token: 0x020001CC RID: 460
	[Preserve]
	[NativeInputDeviceProfile]
	public class XboxSeriesXBluetoothMacNativeProfile : InputDeviceProfile
	{
		// Token: 0x06000863 RID: 2147 RVA: 0x000430C0 File Offset: 0x000412C0
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Microsoft Xbox Series X Controller";
			base.DeviceNotes = "Microsoft Xbox Series X Controller on Mac";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.XboxSeriesX;
			base.IncludePlatforms = new string[]
			{
				"OS X"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.HID,
					VendorID = 1118,
					ProductID = 2835
				}
			};
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(0)
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
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Button(15)
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = InputDeviceProfile.Button(16)
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = InputDeviceProfile.Button(17)
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = InputDeviceProfile.Button(18)
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(13)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(14)
				},
				new InputControlMapping
				{
					Name = "Menu",
					Target = InputControlType.Menu,
					Source = InputDeviceProfile.Button(11)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				InputDeviceProfile.LeftStickLeftMapping(0),
				InputDeviceProfile.LeftStickRightMapping(0),
				InputDeviceProfile.LeftStickUpMapping(1),
				InputDeviceProfile.LeftStickDownMapping(1),
				InputDeviceProfile.RightStickLeftMapping(2),
				InputDeviceProfile.RightStickRightMapping(2),
				InputDeviceProfile.RightStickUpMapping(3),
				InputDeviceProfile.RightStickDownMapping(3)
			};
		}

		// Token: 0x06000864 RID: 2148 RVA: 0x000433BC File Offset: 0x000415BC
		public XboxSeriesXBluetoothMacNativeProfile()
		{
		}
	}
}
