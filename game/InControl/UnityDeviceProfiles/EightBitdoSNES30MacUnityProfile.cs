﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000CE RID: 206
	[Preserve]
	[UnityInputDeviceProfile]
	public class EightBitdoSNES30MacUnityProfile : InputDeviceProfile
	{
		// Token: 0x06000667 RID: 1639 RVA: 0x00023F88 File Offset: 0x00022188
		public override void Define()
		{
			base.Define();
			base.DeviceName = "8Bitdo SNES30 Controller";
			base.DeviceNotes = "8Bitdo SNES30 Controller on Mac";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.NintendoSNES;
			base.IncludePlatforms = new string[]
			{
				"OS X"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = "Unknown 8Bitdo SNES30 GamePad"
				},
				new InputDeviceMatcher
				{
					NameLiteral = "SNES30             SNES30 Joy    "
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
					Source = InputDeviceProfile.Button(3)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(4)
				},
				new InputControlMapping
				{
					Name = "L",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(6)
				},
				new InputControlMapping
				{
					Name = "R",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(7)
				},
				new InputControlMapping
				{
					Name = "Select",
					Target = InputControlType.Select,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(11)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				}
			};
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x0002423E File Offset: 0x0002243E
		public EightBitdoSNES30MacUnityProfile()
		{
		}
	}
}
