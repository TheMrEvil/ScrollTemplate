﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000D2 RID: 210
	[Preserve]
	[UnityInputDeviceProfile]
	public class LogitechF310ModeXMacUnityProfile : InputDeviceProfile
	{
		// Token: 0x0600066F RID: 1647 RVA: 0x00024D80 File Offset: 0x00022F80
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Logitech F310 Controller";
			base.DeviceNotes = "Logitech F310 Controller on Mac (Mode X)";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.Xbox360;
			base.IncludePlatforms = new string[]
			{
				"OS X"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Logitech Gamepad F310"
				}
			};
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(16)
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action2,
					Source = InputDeviceProfile.Button(17)
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(18)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = InputDeviceProfile.Button(19)
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(13)
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(14)
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(12)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "Back",
					Target = InputControlType.Back,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "System",
					Target = InputControlType.System,
					Source = InputDeviceProfile.Button(15)
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
				InputDeviceProfile.LeftTriggerMapping(4, "Left Trigger"),
				InputDeviceProfile.RightTriggerMapping(5, "Right Trigger")
			};
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x000250CB File Offset: 0x000232CB
		public LogitechF310ModeXMacUnityProfile()
		{
		}
	}
}
