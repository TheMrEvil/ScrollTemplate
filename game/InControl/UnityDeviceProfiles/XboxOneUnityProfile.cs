﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000EF RID: 239
	[Preserve]
	[UnityInputDeviceProfile]
	public class XboxOneUnityProfile : InputDeviceProfile
	{
		// Token: 0x060006A9 RID: 1705 RVA: 0x0002A4BC File Offset: 0x000286BC
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Xbox One Controller";
			base.DeviceNotes = "Xbox One Controller on Xbox One";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.XboxOne;
			base.IncludePlatforms = new string[]
			{
				"XBOXONE",
				"DURANGOOS"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Windows.Xbox.Input.Gamepad"
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
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "View",
					Target = InputControlType.View,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "Menu",
					Target = InputControlType.Menu,
					Source = InputDeviceProfile.Button(7)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				InputDeviceProfile.LeftStickLeftMapping(0),
				InputDeviceProfile.LeftStickRightMapping(0),
				InputDeviceProfile.LeftStickUpMapping(1),
				InputDeviceProfile.LeftStickDownMapping(1),
				InputDeviceProfile.RightStickLeftMapping(3),
				InputDeviceProfile.RightStickRightMapping(3),
				InputDeviceProfile.RightStickUpMapping(4),
				InputDeviceProfile.RightStickDownMapping(4),
				InputDeviceProfile.DPadLeftMapping(5),
				InputDeviceProfile.DPadRightMapping(5),
				InputDeviceProfile.DPadUpMapping2(6),
				InputDeviceProfile.DPadDownMapping2(6),
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(8),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(9),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				}
			};
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x0002A820 File Offset: 0x00028A20
		public XboxOneUnityProfile()
		{
		}
	}
}
