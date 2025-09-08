﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000F5 RID: 245
	[Preserve]
	[UnityInputDeviceProfile]
	public class Xbox360MacSafariUnityProfile : InputDeviceProfile
	{
		// Token: 0x060006B5 RID: 1717 RVA: 0x0002C458 File Offset: 0x0002A658
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Xbox 360 Controller";
			base.DeviceNotes = "Xbox 360 Controller on Mac Safari";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.Xbox360;
			base.IncludePlatforms = new string[]
			{
				"Mac Safari"
			};
			base.Matchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NameLiteral = ""
				}
			};
			base.LastResortMatchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NamePattern = ""
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
					Name = "Start",
					Target = InputControlType.Start,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Back",
					Target = InputControlType.Back,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "System",
					Target = InputControlType.System,
					Source = InputDeviceProfile.Button(10)
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Button(11)
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = InputDeviceProfile.Button(12)
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = InputDeviceProfile.Button(13)
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = InputDeviceProfile.Button(14)
				}
			};
			base.AnalogMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "Left Stick Left",
					Target = InputControlType.LeftStickLeft,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Right",
					Target = InputControlType.LeftStickRight,
					Source = InputDeviceProfile.Analog(0),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Up",
					Target = InputControlType.LeftStickUp,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Stick Down",
					Target = InputControlType.LeftStickDown,
					Source = InputDeviceProfile.Analog(1),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Left",
					Target = InputControlType.RightStickLeft,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Right",
					Target = InputControlType.RightStickRight,
					Source = InputDeviceProfile.Analog(2),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Up",
					Target = InputControlType.RightStickUp,
					Source = InputDeviceProfile.Analog(3),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Stick Down",
					Target = InputControlType.RightStickDown,
					Source = InputDeviceProfile.Analog(3),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(4),
					SourceRange = InputRangeType.MinusOneToOne,
					TargetRange = InputRangeType.ZeroToOne
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(5),
					SourceRange = InputRangeType.MinusOneToOne,
					TargetRange = InputRangeType.ZeroToOne
				}
			};
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x0002C96D File Offset: 0x0002AB6D
		public Xbox360MacSafariUnityProfile()
		{
		}
	}
}
