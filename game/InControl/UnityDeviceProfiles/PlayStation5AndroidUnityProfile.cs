﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000B2 RID: 178
	[Preserve]
	[UnityInputDeviceProfile]
	public class PlayStation5AndroidUnityProfile : InputDeviceProfile
	{
		// Token: 0x0600062F RID: 1583 RVA: 0x0001EB28 File Offset: 0x0001CD28
		public override void Define()
		{
			base.Define();
			base.DeviceName = "PlayStation 5 Controller";
			base.DeviceNotes = "PlayStation 5 Controller on Android";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.PlayStation5;
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
					NameLiteral = "Wireless Controller"
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
					Source = InputDeviceProfile.Button(13)
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
					Source = InputDeviceProfile.Button(2)
				},
				new InputControlMapping
				{
					Name = "L1",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(3)
				},
				new InputControlMapping
				{
					Name = "L2",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "R1",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(14)
				},
				new InputControlMapping
				{
					Name = "R2",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "TouchPad Button",
					Target = InputControlType.TouchPadButton,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Create",
					Target = InputControlType.Create,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "Options",
					Target = InputControlType.Options,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "L3",
					Target = InputControlType.LeftStickButton,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "R3",
					Target = InputControlType.RightStickButton,
					Source = InputDeviceProfile.Button(10)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				InputDeviceProfile.LeftStickLeftMapping(0),
				InputDeviceProfile.LeftStickRightMapping(0),
				InputDeviceProfile.LeftStickUpMapping(1),
				InputDeviceProfile.LeftStickDownMapping(1),
				InputDeviceProfile.RightStickLeftMapping(13),
				InputDeviceProfile.RightStickRightMapping(13),
				InputDeviceProfile.RightStickUpMapping(14),
				InputDeviceProfile.RightStickDownMapping(14),
				InputDeviceProfile.DPadLeftMapping(4),
				InputDeviceProfile.DPadRightMapping(4),
				InputDeviceProfile.DPadUpMapping(5),
				InputDeviceProfile.DPadDownMapping(5)
			};
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x0001EE41 File Offset: 0x0001D041
		public PlayStation5AndroidUnityProfile()
		{
		}
	}
}
