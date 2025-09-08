﻿using System;

namespace InControl.UnityDeviceProfiles
{
	// Token: 0x020000E6 RID: 230
	[Preserve]
	[UnityInputDeviceProfile]
	public class AppleMFiUnityProfile : InputDeviceProfile
	{
		// Token: 0x06000697 RID: 1687 RVA: 0x0002866C File Offset: 0x0002686C
		public override void Define()
		{
			base.Define();
			base.DeviceName = "Apple MFi Controller";
			base.DeviceNotes = "Apple MFi Controller on iOS";
			base.DeviceClass = InputDeviceClass.Controller;
			base.DeviceStyle = InputDeviceStyle.AppleMFi;
			base.IncludePlatforms = new string[]
			{
				"iPhone",
				"iPad"
			};
			base.LastResortMatchers = new InputDeviceMatcher[]
			{
				new InputDeviceMatcher
				{
					NamePattern = ""
				}
			};
			base.LowerDeadZone = 0.05f;
			base.UpperDeadZone = 0.95f;
			base.ButtonMappings = new InputControlMapping[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = InputDeviceProfile.Button(14)
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action2,
					Source = InputDeviceProfile.Button(13)
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action3,
					Source = InputDeviceProfile.Button(15)
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = InputDeviceProfile.Button(12)
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = InputDeviceProfile.Button(4)
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
					Source = InputDeviceProfile.Button(5)
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = InputDeviceProfile.Button(8)
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = InputDeviceProfile.Button(9)
				},
				new InputControlMapping
				{
					Name = "Pause",
					Target = InputControlType.Pause,
					Source = InputDeviceProfile.Button(0)
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
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = InputDeviceProfile.Analog(10)
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = InputDeviceProfile.Analog(11)
				}
			};
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x00028965 File Offset: 0x00026B65
		public AppleMFiUnityProfile()
		{
		}
	}
}
