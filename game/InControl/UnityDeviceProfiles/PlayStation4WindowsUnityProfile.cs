﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x0200011D RID: 285
	[Preserve]
	[UnityInputDeviceProfile]
	public class PlayStation4WindowsUnityProfile : InputDeviceProfile
	{
		// Token: 0x06000705 RID: 1797 RVA: 0x00035B98 File Offset: 0x00033D98
		public override void Define()
		{
			base.Define();
			base.DeviceName = "PlayStation 4 Controller";
			base.DeviceNotes = "PlayStation 4 Controller on Windows";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.PlayStation4;
			base.IncludePlatforms = new string[]
			{
				"Windows"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Wireless Controller"
				},
				new InputDeviceMatcher
				{
					NameLiteral = "DUALSHOCK®4 USB Wireless Adaptor"
				}
			};
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "Cross",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(1)
				},
				new InputControlMapping
				{
					Name = "Circle",
					Target = InputControlType.Action2,
					Source = InputDeviceProfile.Button(2)
				},
				new InputControlMapping
				{
					Name = "Square",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(0)
				},
				new InputControlMapping
				{
					Name = "Triangle",
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
					Name = "Share",
					Target = InputControlType.Share,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Options",
					Target = InputControlType.Options,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "System",
					Target = InputControlType.System,
					Source = InputDeviceProfile.Button(12)
				},
				new InputControlMapping
				{
					Name = "TouchPad Button",
					Target = InputControlType.TouchPadButton,
					Source = InputDeviceProfile.Button(13)
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
				InputDeviceProfile.RightStickUpMapping(5),
				InputDeviceProfile.RightStickDownMapping(5),
				InputDeviceProfile.DPadLeftMapping(6),
				InputDeviceProfile.DPadRightMapping(6),
				InputDeviceProfile.DPadUpMapping2(7),
				InputDeviceProfile.DPadDownMapping2(7),
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(3),
					SourceRange = InputRangeType.MinusOneToOne,
					TargetRange = InputRangeType.ZeroToOne,
					IgnoreInitialZeroValue = true
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(4),
					SourceRange = InputRangeType.MinusOneToOne,
					TargetRange = InputRangeType.ZeroToOne,
					IgnoreInitialZeroValue = true
				}
			};
		}

		// Token: 0x06000706 RID: 1798 RVA: 0x00035F08 File Offset: 0x00034108
		public PlayStation4WindowsUnityProfile()
		{
		}
	}
}
