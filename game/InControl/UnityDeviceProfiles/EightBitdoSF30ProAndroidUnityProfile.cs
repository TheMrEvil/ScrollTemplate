﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x02000091 RID: 145
	[Preserve]
	[UnityInputDeviceProfile]
	public class EightBitdoSF30ProAndroidUnityProfile : InputDeviceProfile
	{
		// Token: 0x060005ED RID: 1517 RVA: 0x00019210 File Offset: 0x00017410
		public override void Define()
		{
			base.Define();
			base.DeviceName = "8Bitdo SF30 Pro Controller";
			base.DeviceNotes = "8Bitdo SF30 Pro Controller on Android";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.NintendoSNES;
			base.IncludePlatforms = new string[]
			{
				"Android"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "8Bitdo SF30 Pro"
				}
			};
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action2,
					Source = InputDeviceProfile.Button(0)
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(1)
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action4,
					Source = InputDeviceProfile.Button(2)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(3)
				},
				new InputControlMapping
				{
					Name = "Select",
					Target = InputControlType.Select,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(10)
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
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "R2",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Button(7)
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
				InputDeviceProfile.DPadDownMapping(5)
			};
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x00019496 File Offset: 0x00017696
		public EightBitdoSF30ProAndroidUnityProfile()
		{
		}
	}
}
