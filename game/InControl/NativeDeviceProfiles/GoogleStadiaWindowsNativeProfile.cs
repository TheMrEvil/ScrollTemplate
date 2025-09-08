﻿using System;

namespace InControl.NativeDeviceProfiles
{
	// Token: 0x020001ED RID: 493
	[Preserve]
	[NativeInputDeviceProfile]
	public class GoogleStadiaWindowsNativeProfile : InputDeviceProfile
	{
		// Token: 0x060008CA RID: 2250 RVA: 0x00049A7C File Offset: 0x00047C7C
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Google Stadia Controller";
			base.DeviceNotes = "Google Stadia Controller on Windows";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.GoogleStadia;
			base.IncludePlatforms = new string[]
			{
				"Windows"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.DirectInput,
					VendorID = 26214,
					ProductID = 37889
				},
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.DirectInput,
					VendorID = 6353,
					ProductID = 37888
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
					Source = InputDeviceProfile.Button(2)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = InputDeviceProfile.Button(3)
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "Options",
					Target = InputControlType.Options,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Stadia",
					Target = InputControlType.System,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "Menu",
					Target = InputControlType.Menu,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Button(12)
				},
				new InputControlMapping
				{
					Name = "Assistant",
					Target = InputControlType.Assistant,
					Source = InputDeviceProfile.Button(13)
				},
				new InputControlMapping
				{
					Name = "Capture",
					Target = InputControlType.Capture,
					Source = InputDeviceProfile.Button(14)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "Left Stick Up",
					Target = InputControlType.LeftStickUp,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Down",
					Target = InputControlType.LeftStickDown,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Left",
					Target = InputControlType.LeftStickLeft,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Right",
					Target = InputControlType.LeftStickRight,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Up",
					Target = InputControlType.RightStickUp,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Down",
					Target = InputControlType.RightStickDown,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Left",
					Target = InputControlType.RightStickLeft,
					Source = InputDeviceProfile.Analog(3),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Right",
					Target = InputControlType.RightStickRight,
					Source = InputDeviceProfile.Analog(3),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = InputDeviceProfile.Analog(6),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = InputDeviceProfile.Analog(6),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Analog(7),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = InputDeviceProfile.Analog(7),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				}
			};
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x0004A038 File Offset: 0x00048238
		public GoogleStadiaWindowsNativeProfile()
		{
		}
	}
}
