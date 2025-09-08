﻿using System;
using UnityEngine;

namespace InControl
{
	// Token: 0x02000042 RID: 66
	public class UnityKeyboardProvider : IKeyboardProvider
	{
		// Token: 0x06000353 RID: 851 RVA: 0x0000A933 File Offset: 0x00008B33
		public void Setup()
		{
		}

		// Token: 0x06000354 RID: 852 RVA: 0x0000A935 File Offset: 0x00008B35
		public void Reset()
		{
		}

		// Token: 0x06000355 RID: 853 RVA: 0x0000A937 File Offset: 0x00008B37
		public void Update()
		{
		}

		// Token: 0x06000356 RID: 854 RVA: 0x0000A939 File Offset: 0x00008B39
		public bool AnyKeyIsPressed()
		{
			return Input.anyKey;
		}

		// Token: 0x06000357 RID: 855 RVA: 0x0000A940 File Offset: 0x00008B40
		public bool GetKeyIsPressed(Key control)
		{
			return UnityKeyboardProvider.KeyMappings[(int)control].IsPressed;
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0000A952 File Offset: 0x00008B52
		public string GetNameForKey(Key control)
		{
			return UnityKeyboardProvider.KeyMappings[(int)control].Name;
		}

		// Token: 0x06000359 RID: 857 RVA: 0x0000A964 File Offset: 0x00008B64
		public UnityKeyboardProvider()
		{
		}

		// Token: 0x0600035A RID: 858 RVA: 0x0000A96C File Offset: 0x00008B6C
		// Note: this type is marked as 'beforefieldinit'.
		static UnityKeyboardProvider()
		{
		}

		// Token: 0x04000314 RID: 788
		public static readonly UnityKeyboardProvider.KeyMapping[] KeyMappings = new UnityKeyboardProvider.KeyMapping[]
		{
			new UnityKeyboardProvider.KeyMapping(Key.None, "None", KeyCode.None),
			new UnityKeyboardProvider.KeyMapping(Key.Shift, "Shift", KeyCode.LeftShift, KeyCode.RightShift),
			new UnityKeyboardProvider.KeyMapping(Key.Alt, "Alt", "Option", KeyCode.LeftAlt, KeyCode.RightAlt),
			new UnityKeyboardProvider.KeyMapping(Key.Command, "Command", KeyCode.LeftMeta, KeyCode.RightMeta),
			new UnityKeyboardProvider.KeyMapping(Key.Control, "Control", KeyCode.LeftControl, KeyCode.RightControl),
			new UnityKeyboardProvider.KeyMapping(Key.LeftShift, "Left Shift", KeyCode.LeftShift),
			new UnityKeyboardProvider.KeyMapping(Key.LeftAlt, "Left Alt", "Left Option", KeyCode.LeftAlt),
			new UnityKeyboardProvider.KeyMapping(Key.LeftCommand, "Left Command", KeyCode.LeftMeta),
			new UnityKeyboardProvider.KeyMapping(Key.LeftControl, "Left Control", KeyCode.LeftControl),
			new UnityKeyboardProvider.KeyMapping(Key.RightShift, "Right Shift", KeyCode.RightShift),
			new UnityKeyboardProvider.KeyMapping(Key.RightAlt, "Right Alt", "Right Option", KeyCode.RightAlt),
			new UnityKeyboardProvider.KeyMapping(Key.RightCommand, "Right Command", KeyCode.RightMeta),
			new UnityKeyboardProvider.KeyMapping(Key.RightControl, "Right Control", KeyCode.RightControl),
			new UnityKeyboardProvider.KeyMapping(Key.Escape, "Escape", KeyCode.Escape),
			new UnityKeyboardProvider.KeyMapping(Key.F1, "F1", KeyCode.F1),
			new UnityKeyboardProvider.KeyMapping(Key.F2, "F2", KeyCode.F2),
			new UnityKeyboardProvider.KeyMapping(Key.F3, "F3", KeyCode.F3),
			new UnityKeyboardProvider.KeyMapping(Key.F4, "F4", KeyCode.F4),
			new UnityKeyboardProvider.KeyMapping(Key.F5, "F5", KeyCode.F5),
			new UnityKeyboardProvider.KeyMapping(Key.F6, "F6", KeyCode.F6),
			new UnityKeyboardProvider.KeyMapping(Key.F7, "F7", KeyCode.F7),
			new UnityKeyboardProvider.KeyMapping(Key.F8, "F8", KeyCode.F8),
			new UnityKeyboardProvider.KeyMapping(Key.F9, "F9", KeyCode.F9),
			new UnityKeyboardProvider.KeyMapping(Key.F10, "F10", KeyCode.F10),
			new UnityKeyboardProvider.KeyMapping(Key.F11, "F11", KeyCode.F11),
			new UnityKeyboardProvider.KeyMapping(Key.F12, "F12", KeyCode.F12),
			new UnityKeyboardProvider.KeyMapping(Key.Key0, "Num 0", KeyCode.Alpha0),
			new UnityKeyboardProvider.KeyMapping(Key.Key1, "Num 1", KeyCode.Alpha1),
			new UnityKeyboardProvider.KeyMapping(Key.Key2, "Num 2", KeyCode.Alpha2),
			new UnityKeyboardProvider.KeyMapping(Key.Key3, "Num 3", KeyCode.Alpha3),
			new UnityKeyboardProvider.KeyMapping(Key.Key4, "Num 4", KeyCode.Alpha4),
			new UnityKeyboardProvider.KeyMapping(Key.Key5, "Num 5", KeyCode.Alpha5),
			new UnityKeyboardProvider.KeyMapping(Key.Key6, "Num 6", KeyCode.Alpha6),
			new UnityKeyboardProvider.KeyMapping(Key.Key7, "Num 7", KeyCode.Alpha7),
			new UnityKeyboardProvider.KeyMapping(Key.Key8, "Num 8", KeyCode.Alpha8),
			new UnityKeyboardProvider.KeyMapping(Key.Key9, "Num 9", KeyCode.Alpha9),
			new UnityKeyboardProvider.KeyMapping(Key.A, "A", KeyCode.A),
			new UnityKeyboardProvider.KeyMapping(Key.B, "B", KeyCode.B),
			new UnityKeyboardProvider.KeyMapping(Key.C, "C", KeyCode.C),
			new UnityKeyboardProvider.KeyMapping(Key.D, "D", KeyCode.D),
			new UnityKeyboardProvider.KeyMapping(Key.E, "E", KeyCode.E),
			new UnityKeyboardProvider.KeyMapping(Key.F, "F", KeyCode.F),
			new UnityKeyboardProvider.KeyMapping(Key.G, "G", KeyCode.G),
			new UnityKeyboardProvider.KeyMapping(Key.H, "H", KeyCode.H),
			new UnityKeyboardProvider.KeyMapping(Key.I, "I", KeyCode.I),
			new UnityKeyboardProvider.KeyMapping(Key.J, "J", KeyCode.J),
			new UnityKeyboardProvider.KeyMapping(Key.K, "K", KeyCode.K),
			new UnityKeyboardProvider.KeyMapping(Key.L, "L", KeyCode.L),
			new UnityKeyboardProvider.KeyMapping(Key.M, "M", KeyCode.M),
			new UnityKeyboardProvider.KeyMapping(Key.N, "N", KeyCode.N),
			new UnityKeyboardProvider.KeyMapping(Key.O, "O", KeyCode.O),
			new UnityKeyboardProvider.KeyMapping(Key.P, "P", KeyCode.P),
			new UnityKeyboardProvider.KeyMapping(Key.Q, "Q", KeyCode.Q),
			new UnityKeyboardProvider.KeyMapping(Key.R, "R", KeyCode.R),
			new UnityKeyboardProvider.KeyMapping(Key.S, "S", KeyCode.S),
			new UnityKeyboardProvider.KeyMapping(Key.T, "T", KeyCode.T),
			new UnityKeyboardProvider.KeyMapping(Key.U, "U", KeyCode.U),
			new UnityKeyboardProvider.KeyMapping(Key.V, "V", KeyCode.V),
			new UnityKeyboardProvider.KeyMapping(Key.W, "W", KeyCode.W),
			new UnityKeyboardProvider.KeyMapping(Key.X, "X", KeyCode.X),
			new UnityKeyboardProvider.KeyMapping(Key.Y, "Y", KeyCode.Y),
			new UnityKeyboardProvider.KeyMapping(Key.Z, "Z", KeyCode.Z),
			new UnityKeyboardProvider.KeyMapping(Key.Backquote, "Backquote", KeyCode.BackQuote),
			new UnityKeyboardProvider.KeyMapping(Key.Minus, "Minus", KeyCode.Minus),
			new UnityKeyboardProvider.KeyMapping(Key.Equals, "Equals", KeyCode.Equals),
			new UnityKeyboardProvider.KeyMapping(Key.Backspace, "Backspace", "Delete", KeyCode.Backspace),
			new UnityKeyboardProvider.KeyMapping(Key.Tab, "Tab", KeyCode.Tab),
			new UnityKeyboardProvider.KeyMapping(Key.LeftBracket, "Left Bracket", KeyCode.LeftBracket),
			new UnityKeyboardProvider.KeyMapping(Key.RightBracket, "Right Bracket", KeyCode.RightBracket),
			new UnityKeyboardProvider.KeyMapping(Key.Backslash, "Backslash", KeyCode.Backslash),
			new UnityKeyboardProvider.KeyMapping(Key.Semicolon, "Semicolon", KeyCode.Semicolon),
			new UnityKeyboardProvider.KeyMapping(Key.Quote, "Quote", KeyCode.Quote),
			new UnityKeyboardProvider.KeyMapping(Key.Return, "Return", KeyCode.Return),
			new UnityKeyboardProvider.KeyMapping(Key.Comma, "Comma", KeyCode.Comma),
			new UnityKeyboardProvider.KeyMapping(Key.Period, "Period", KeyCode.Period),
			new UnityKeyboardProvider.KeyMapping(Key.Slash, "Slash", KeyCode.Slash),
			new UnityKeyboardProvider.KeyMapping(Key.Space, "Space", KeyCode.Space),
			new UnityKeyboardProvider.KeyMapping(Key.Insert, "Insert", KeyCode.Insert),
			new UnityKeyboardProvider.KeyMapping(Key.Delete, "Delete", "Forward Delete", KeyCode.Delete),
			new UnityKeyboardProvider.KeyMapping(Key.Home, "Home", KeyCode.Home),
			new UnityKeyboardProvider.KeyMapping(Key.End, "End", KeyCode.End),
			new UnityKeyboardProvider.KeyMapping(Key.PageUp, "PageUp", KeyCode.PageUp),
			new UnityKeyboardProvider.KeyMapping(Key.PageDown, "PageDown", KeyCode.PageDown),
			new UnityKeyboardProvider.KeyMapping(Key.LeftArrow, "Left Arrow", KeyCode.LeftArrow),
			new UnityKeyboardProvider.KeyMapping(Key.RightArrow, "Right Arrow", KeyCode.RightArrow),
			new UnityKeyboardProvider.KeyMapping(Key.UpArrow, "Up Arrow", KeyCode.UpArrow),
			new UnityKeyboardProvider.KeyMapping(Key.DownArrow, "Down Arrow", KeyCode.DownArrow),
			new UnityKeyboardProvider.KeyMapping(Key.Pad0, "Pad 0", KeyCode.Keypad0),
			new UnityKeyboardProvider.KeyMapping(Key.Pad1, "Pad 1", KeyCode.Keypad1),
			new UnityKeyboardProvider.KeyMapping(Key.Pad2, "Pad 2", KeyCode.Keypad2),
			new UnityKeyboardProvider.KeyMapping(Key.Pad3, "Pad 3", KeyCode.Keypad3),
			new UnityKeyboardProvider.KeyMapping(Key.Pad4, "Pad 4", KeyCode.Keypad4),
			new UnityKeyboardProvider.KeyMapping(Key.Pad5, "Pad 5", KeyCode.Keypad5),
			new UnityKeyboardProvider.KeyMapping(Key.Pad6, "Pad 6", KeyCode.Keypad6),
			new UnityKeyboardProvider.KeyMapping(Key.Pad7, "Pad 7", KeyCode.Keypad7),
			new UnityKeyboardProvider.KeyMapping(Key.Pad8, "Pad 8", KeyCode.Keypad8),
			new UnityKeyboardProvider.KeyMapping(Key.Pad9, "Pad 9", KeyCode.Keypad9),
			new UnityKeyboardProvider.KeyMapping(Key.Numlock, "Numlock", KeyCode.Numlock),
			new UnityKeyboardProvider.KeyMapping(Key.PadDivide, "Pad Divide", KeyCode.KeypadDivide),
			new UnityKeyboardProvider.KeyMapping(Key.PadMultiply, "Pad Multiply", KeyCode.KeypadMultiply),
			new UnityKeyboardProvider.KeyMapping(Key.PadMinus, "Pad Minus", KeyCode.KeypadMinus),
			new UnityKeyboardProvider.KeyMapping(Key.PadPlus, "Pad Plus", KeyCode.KeypadPlus),
			new UnityKeyboardProvider.KeyMapping(Key.PadEnter, "Pad Enter", KeyCode.KeypadEnter),
			new UnityKeyboardProvider.KeyMapping(Key.PadPeriod, "Pad Period", KeyCode.KeypadPeriod),
			new UnityKeyboardProvider.KeyMapping(Key.Clear, "Clear", KeyCode.Clear),
			new UnityKeyboardProvider.KeyMapping(Key.PadEquals, "Pad Equals", KeyCode.KeypadEquals),
			new UnityKeyboardProvider.KeyMapping(Key.F13, "F13", KeyCode.F13),
			new UnityKeyboardProvider.KeyMapping(Key.F14, "F14", KeyCode.F14),
			new UnityKeyboardProvider.KeyMapping(Key.F15, "F15", KeyCode.F15),
			new UnityKeyboardProvider.KeyMapping(Key.AltGr, "Alt Graphic", KeyCode.AltGr),
			new UnityKeyboardProvider.KeyMapping(Key.CapsLock, "Caps Lock", KeyCode.CapsLock),
			new UnityKeyboardProvider.KeyMapping(Key.ExclamationMark, "Exclamation", KeyCode.Exclaim),
			new UnityKeyboardProvider.KeyMapping(Key.Tilde, "Tilde", KeyCode.Tilde),
			new UnityKeyboardProvider.KeyMapping(Key.At, "At", KeyCode.At),
			new UnityKeyboardProvider.KeyMapping(Key.Hash, "Hash", KeyCode.Hash),
			new UnityKeyboardProvider.KeyMapping(Key.Dollar, "Dollar", KeyCode.Dollar),
			new UnityKeyboardProvider.KeyMapping(Key.Percent, "Percent", KeyCode.Percent),
			new UnityKeyboardProvider.KeyMapping(Key.Caret, "Caret", KeyCode.Caret),
			new UnityKeyboardProvider.KeyMapping(Key.Ampersand, "Ampersand", KeyCode.Ampersand),
			new UnityKeyboardProvider.KeyMapping(Key.Asterisk, "Asterisk", KeyCode.Asterisk),
			new UnityKeyboardProvider.KeyMapping(Key.LeftParen, "Left Paren", KeyCode.LeftParen),
			new UnityKeyboardProvider.KeyMapping(Key.RightParen, "Right Paren", KeyCode.RightParen),
			new UnityKeyboardProvider.KeyMapping(Key.Underscore, "Underscore", KeyCode.Underscore),
			new UnityKeyboardProvider.KeyMapping(Key.Plus, "Plus", KeyCode.Plus),
			new UnityKeyboardProvider.KeyMapping(Key.LeftBrace, "LeftBrace", KeyCode.LeftCurlyBracket),
			new UnityKeyboardProvider.KeyMapping(Key.RightBrace, "RightBrace", KeyCode.RightCurlyBracket),
			new UnityKeyboardProvider.KeyMapping(Key.Pipe, "Pipe", KeyCode.Pipe),
			new UnityKeyboardProvider.KeyMapping(Key.Colon, "Colon", KeyCode.Colon),
			new UnityKeyboardProvider.KeyMapping(Key.DoubleQuote, "Double Quote", KeyCode.DoubleQuote),
			new UnityKeyboardProvider.KeyMapping(Key.LessThan, "Less Than", KeyCode.Less),
			new UnityKeyboardProvider.KeyMapping(Key.GreaterThan, "Greater Than", KeyCode.Greater),
			new UnityKeyboardProvider.KeyMapping(Key.QuestionMark, "Question Mark", KeyCode.Question)
		};

		// Token: 0x02000212 RID: 530
		public readonly struct KeyMapping
		{
			// Token: 0x0600090E RID: 2318 RVA: 0x00052C51 File Offset: 0x00050E51
			public KeyMapping(Key source, string name, KeyCode target)
			{
				this.source = source;
				this.name = name;
				this.macName = name;
				this.target0 = target;
				this.target1 = KeyCode.None;
			}

			// Token: 0x0600090F RID: 2319 RVA: 0x00052C76 File Offset: 0x00050E76
			public KeyMapping(Key source, string name, KeyCode target0, KeyCode target1)
			{
				this.source = source;
				this.name = name;
				this.macName = name;
				this.target0 = target0;
				this.target1 = target1;
			}

			// Token: 0x06000910 RID: 2320 RVA: 0x00052C9C File Offset: 0x00050E9C
			public KeyMapping(Key source, string name, string macName, KeyCode target)
			{
				this.source = source;
				this.name = name;
				this.macName = macName;
				this.target0 = target;
				this.target1 = KeyCode.None;
			}

			// Token: 0x06000911 RID: 2321 RVA: 0x00052CC2 File Offset: 0x00050EC2
			public KeyMapping(Key source, string name, string macName, KeyCode target0, KeyCode target1)
			{
				this.source = source;
				this.name = name;
				this.macName = macName;
				this.target0 = target0;
				this.target1 = target1;
			}

			// Token: 0x1700018A RID: 394
			// (get) Token: 0x06000912 RID: 2322 RVA: 0x00052CE9 File Offset: 0x00050EE9
			public bool IsPressed
			{
				get
				{
					return (this.target0 != KeyCode.None && Input.GetKey(this.target0)) || (this.target1 != KeyCode.None && Input.GetKey(this.target1));
				}
			}

			// Token: 0x1700018B RID: 395
			// (get) Token: 0x06000913 RID: 2323 RVA: 0x00052D1A File Offset: 0x00050F1A
			public string Name
			{
				get
				{
					if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
					{
						return this.macName;
					}
					return this.name;
				}
			}

			// Token: 0x04000457 RID: 1111
			private readonly Key source;

			// Token: 0x04000458 RID: 1112
			private readonly KeyCode target0;

			// Token: 0x04000459 RID: 1113
			private readonly KeyCode target1;

			// Token: 0x0400045A RID: 1114
			private readonly string name;

			// Token: 0x0400045B RID: 1115
			private readonly string macName;
		}
	}
}
