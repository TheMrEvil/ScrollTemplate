﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000DC RID: 220
	[Preserve]
	[UnityInputDeviceProfile]
	public class PlayStation5BluetoothMacUnityProfile : InputDeviceProfile
	{
		// Token: 0x06000683 RID: 1667 RVA: 0x00026C7C File Offset: 0x00024E7C
		public override void Define()
		{
			base.Define();
			base.DeviceName = "PlayStation 5 Controller";
			base.DeviceNotes = "PlayStation 5 Controller on macOS";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.PlayStation5;
			base.IncludePlatforms = new string[]
			{
				"OS X"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Unknown DualSense Wireless Controller"
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
					Name = "L1",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "L2",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "R1",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "R2",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "Create",
					Target = InputControlType.Create,
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
					Name = "L3",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "R3",
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
				InputDeviceProfile.RightStickUpMapping(3),
				InputDeviceProfile.RightStickDownMapping(3),
				InputDeviceProfile.LeftTriggerMapping(4, "Left Trigger"),
				InputDeviceProfile.RightTriggerMapping(5, "Right Trigger"),
				InputDeviceProfile.DPadLeftMapping(6),
				InputDeviceProfile.DPadRightMapping(6),
				InputDeviceProfile.DPadUpMapping(7),
				InputDeviceProfile.DPadDownMapping(7)
			};
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x00026FC4 File Offset: 0x000251C4
		public PlayStation5BluetoothMacUnityProfile()
		{
		}
	}
}
