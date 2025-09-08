﻿using System;

namespace InControl
{
	// Token: 0x0200003D RID: 61
	public static class InputDeviceStyleExtensions
	{
		// Token: 0x060002CF RID: 719 RVA: 0x00008EDC File Offset: 0x000070DC
		public static InputControlType LeftCommandControl(this InputDeviceStyle deviceStyle)
		{
			switch (deviceStyle)
			{
			case InputDeviceStyle.Xbox360:
				return InputControlType.Back;
			case InputDeviceStyle.XboxOne:
			case InputDeviceStyle.XboxSeriesX:
				return InputControlType.View;
			case InputDeviceStyle.PlayStation2:
			case InputDeviceStyle.PlayStation3:
			case InputDeviceStyle.PlayStationVita:
				return InputControlType.Select;
			case InputDeviceStyle.PlayStation4:
				return InputControlType.Share;
			case InputDeviceStyle.PlayStation5:
				return InputControlType.Create;
			case InputDeviceStyle.Steam:
				return InputControlType.Back;
			case InputDeviceStyle.AppleMFi:
				return InputControlType.Menu;
			case InputDeviceStyle.AmazonFireTV:
				return InputControlType.Back;
			case InputDeviceStyle.NVIDIAShield:
				return InputControlType.Back;
			case InputDeviceStyle.NintendoNES:
			case InputDeviceStyle.NintendoSNES:
				return InputControlType.Select;
			case InputDeviceStyle.NintendoWii:
			case InputDeviceStyle.NintendoWiiU:
			case InputDeviceStyle.NintendoSwitch:
				return InputControlType.Minus;
			case InputDeviceStyle.GoogleStadia:
				return InputControlType.Options;
			}
			return InputControlType.Select;
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x00008F84 File Offset: 0x00007184
		public static InputControlType RightCommandControl(this InputDeviceStyle deviceStyle)
		{
			switch (deviceStyle)
			{
			case InputDeviceStyle.Xbox360:
				return InputControlType.Start;
			case InputDeviceStyle.XboxOne:
			case InputDeviceStyle.XboxSeriesX:
				return InputControlType.Menu;
			case InputDeviceStyle.PlayStation2:
			case InputDeviceStyle.PlayStation3:
			case InputDeviceStyle.PlayStationVita:
				return InputControlType.Start;
			case InputDeviceStyle.PlayStation4:
			case InputDeviceStyle.PlayStation5:
				return InputControlType.Options;
			case InputDeviceStyle.PlayStationMove:
				return InputControlType.None;
			case InputDeviceStyle.Steam:
				return InputControlType.Start;
			case InputDeviceStyle.AppleMFi:
				return InputControlType.Options;
			case InputDeviceStyle.AmazonFireTV:
				return InputControlType.Menu;
			case InputDeviceStyle.NVIDIAShield:
				return InputControlType.Start;
			case InputDeviceStyle.NintendoNES:
			case InputDeviceStyle.NintendoSNES:
			case InputDeviceStyle.Nintendo64:
			case InputDeviceStyle.NintendoGameCube:
				return InputControlType.Start;
			case InputDeviceStyle.NintendoWii:
			case InputDeviceStyle.NintendoWiiU:
			case InputDeviceStyle.NintendoSwitch:
				return InputControlType.Plus;
			case InputDeviceStyle.GoogleStadia:
				return InputControlType.Menu;
			}
			return InputControlType.Start;
		}

		// Token: 0x040002CE RID: 718
		private const InputControlType defaultLeftCommandControl = InputControlType.Select;

		// Token: 0x040002CF RID: 719
		private const InputControlType defaultRightCommandControl = InputControlType.Start;
	}
}
