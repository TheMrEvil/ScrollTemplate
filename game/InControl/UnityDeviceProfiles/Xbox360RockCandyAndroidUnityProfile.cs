﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000BA RID: 186
	[Preserve]
	[UnityInputDeviceProfile]
	public class Xbox360RockCandyAndroidUnityProfile : InputDeviceProfile
	{
		// Token: 0x0600063F RID: 1599 RVA: 0x000201FC File Offset: 0x0001E3FC
		public override void Define()
		{
			base.Define();
			base.DeviceName = "XBox 360 Rock Candy Controller";
			base.DeviceNotes = "XBox 360 Rock Candy Controller on Android";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.Xbox360;
			base.IncludePlatforms = new string[]
			{
				"Android"
			};
			base.ExcludePlatforms = new string[]
			{
				"Amazon AFT"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Generic X-Box pad"
				}
			};
			base.LastResortMatchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NamePattern = "360"
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
					Name = "Back",
					Target = InputControlType.Back,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(10)
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
				InputDeviceProfile.RightStickDownMapping(3),
				InputDeviceProfile.DPadLeftMapping(4),
				InputDeviceProfile.DPadRightMapping(4),
				InputDeviceProfile.DPadUpMapping(5),
				InputDeviceProfile.DPadDownMapping(5),
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(13)
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(14)
				}
			};
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0002050F File Offset: 0x0001E70F
		public Xbox360RockCandyAndroidUnityProfile()
		{
		}
	}
}
