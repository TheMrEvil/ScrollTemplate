﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000C3 RID: 195
	[Preserve]
	[UnityInputDeviceProfile]
	public class LogitechF710LinuxUnityProfile : InputDeviceProfile
	{
		// Token: 0x06000651 RID: 1617 RVA: 0x00021CCC File Offset: 0x0001FECC
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Logitech F710 Controller";
			base.DeviceNotes = "Logitech F710 Controller on Linux";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.Xbox360;
			base.IncludePlatforms = new string[]
			{
				"Linux"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Logitech Gamepad F710"
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
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "Back",
					Target = InputControlType.Back,
					Source = InputDeviceProfile.Button(6)
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
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(2)
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(5)
				},
				InputDeviceProfile.DPadLeftMapping(6),
				InputDeviceProfile.DPadRightMapping(6),
				InputDeviceProfile.DPadUpMapping(7),
				InputDeviceProfile.DPadDownMapping(7)
			};
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x00021FA0 File Offset: 0x000201A0
		public LogitechF710LinuxUnityProfile()
		{
		}
	}
}
