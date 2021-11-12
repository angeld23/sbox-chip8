using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Chip8
{
	public class ROMInput : TextEntry	
	{
		public ROMInput ()
		{
			StyleSheet.Load( "/Chip8.scss" );
			SetClass( "romInput", true );
			Style.Dirty();
			Text = "00 E0 A2 48 60 00 61 1E 62 00 D2 02 D2 12 72 08 32 40 12 0A 60 00 61 3E 62 02 A2 4A D0 2E D1 2E 72 0E D0 2E D1 2E A2 58 60 0B 61 08 D0 1F 70 0A A2 67 D0 1F 70 0A A2 76 D0 1F 70 03 A2 85 D0 1F 70 0A A2 94 D0 1F 12 46 FF FF C0 C0 C0 C0 C0 C0 C0 C0 C0 C0 C0 C0 C0 C0 FF 80 80 80 80 80 80 80 80 80 80 80 80 80 FF 81 81 81 81 81 81 81 FF 81 81 81 81 81 81 81 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 FF 81 81 81 81 81 81 FF 80 80 80 80 80 80 80 FF 81 81 81 81 81 81 FF 81 81 81 81 81 81 FF FF";
		}


	}
	public class ClickyButton : Button
	{
		public ClickyButton ()
		{
			SetClass( "clickyButton", true );
		}
		public override void OnButtonEvent( ButtonEvent e )
		{
			base.OnButtonEvent( e );
			if ( e.Pressed )
			{
				Sound.FromScreen( "buttondown" );
			}
			else
			{
				Sound.FromScreen( "buttonup" );
			}
		}

		public override void Tick()
		{
			base.Tick();
			SetClass( "isHolding", HasActive );
		}
	}
	public class Keypad : Panel
	{
		private readonly int[] _buttonOrder = new int[16] {
			0x1, 0x2, 0x3, 0xC, 0x4, 0x5, 0x6, 0xD, 0x7, 0x8, 0x9, 0xE, 0xA, 0x0, 0xB, 0xF
		};
		public Action<int, bool> SetKeyStateListener;
		public Keypad ()
		{
			StyleSheet.Load( "/Chip8.scss" );
			SetClass( "keypad", true );
			Style.Dirty();

			AddChild<Label>("tipLabel keypadLabel").Text = "Keypad";

			var i = 0;
			foreach (var buttonValue in _buttonOrder)
			{
				var buttonContainer = AddChild<Panel>("keypadButtonContainer");
				var button = buttonContainer.AddChild<ClickyButton>("keypadButton");
				button.Style.Dirty();
				button.Text = Convert.ToHexString(new byte[] { (byte)buttonValue } ).Substring(1, 1);

				button.AddEventListener( "onmousedown", delegate() {
					if (SetKeyStateListener != null)
					{
						SetKeyStateListener.Invoke( buttonValue, true );
					}
					//MinimalHudEntity.cpu.Keys[buttonValue] = true;
				} );
				button.AddEventListener( "onmouseup", delegate () {
					if ( SetKeyStateListener != null )
					{
						SetKeyStateListener.Invoke( buttonValue, false );
					}
					//MinimalHudEntity.cpu.Keys[buttonValue] = false;
				} );

				i++;
			}

			//button.Style.Width = Length.Pixels( 200 );
			//button.Style.Width = Length.Pixels( 100 );
		}
	}
	public class Chip8Pixel : Panel
	{
		public bool PixelActive { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }

		public Chip8Pixel ()
		{
			StyleSheet.Load( "/Chip8.scss" );
			SetClass("chip8Pixel", true);
			Style.Dirty();

			Style.Width = Length.Fraction( 1f / 64f );
			Style.Height = Length.Fraction( 1f / 32f );
		}

		public override void Tick()
		{
			base.Tick();

			SetClass( "pixelActive", PixelActive );

			Style.Left = Length.Fraction( PosX );
			Style.Top = Length.Fraction(PosY);
		}
	}

	public class Chip8Display : Panel
	{
		public Chip8Pixel[,] Pixels = new Chip8Pixel[64, 32];
		public CPU CPUToCycle;
		public Sound Beep;
		public bool SoundPlaying;

		public Chip8Display ()
		{
			StyleSheet.Load( "/Chip8.scss" );
			SetClass( "displayRoot", true );
			Style.Dirty();

			AddChild<Label>( "tipLabel displayLabel" ).Text = "Display";

			for (var x = 0; x <= 63; x++ )
			{
				for ( var y = 0; y <= 31; y++ )
				{
					var newPixel = AddChild<Chip8Pixel>(); 
					newPixel.PosX = (float)x / 64;
					newPixel.PosY = (float)y / 32;
					Pixels[x, y] = newPixel;
				}
			}

			
		}

		public override void Tick()
		{
			base.Tick();
			if (CPUToCycle != null)
			{
				if (CPUToCycle.ST > 0)
				{
					if (!SoundPlaying)
					{
						Beep = Sound.FromScreen( "tone" );
						SoundPlaying = true;
					}
					
				}
				else
				{
					Beep.Stop();
					SoundPlaying = false;	
				}
				for (var i = 0; i < Math.Floor(Global.TickInterval * 500); i++)
				{
					CPUToCycle.Cycle();
					//Log.Info( cpu.PC );

					SetDisplay( CPUToCycle.Display );
				}
			}
		}

		public void SetDisplay (bool[,] pixelStates)
		{
			for ( var x = 0; x <= 63; x++ )
			{
				for ( var y = 0; y <= 31; y++ )
				{
					Pixels[x, y].PixelActive = pixelStates[x, y];
				}
			}
		}
	}
	public partial class MinimalHudEntity : HudEntity<RootPanel>
	{
		public CPU cpu;
		public Chip8Display display { get; set; }
		public Keypad keypad { get; set; }
		public ROMInput romInput { get; set; }
		public MinimalHudEntity()
		{
			if ( IsClient )
			{
				cpu = new CPU();
				cpu.LoadROMString( "12 14 52 45 56 49 56 41 4C 53 54 55 44 49 4F 53 32 30 30 38 00 E0 6D 20 FD 15 23 BE 23 C6 6D 40 FD 15 23 BE 23 C6 6D 20 FD 15 23 BE A4 83 24 48 6D 80 FD 15 23 BE A4 83 24 48 A5 83 24 48 6D 00 6B 00 22 C6 4B 00 22 E4 4B 01 23 86 4B 02 22 EC 4B 03 23 86 4B 04 22 F4 4B 05 23 86 60 01 F0 15 23 BE 7D 01 60 3F 8C D0 8C 02 4C 00 22 70 12 44 4B 00 22 90 4B 01 22 CC 4B 02 22 A2 4B 03 22 D4 4B 04 22 B4 4B 05 22 DC 7B 01 4B 06 6B 00 00 EE 23 08 C9 03 89 94 89 94 89 94 89 94 89 94 23 66 00 EE 22 FC C9 03 89 94 89 94 89 94 89 94 89 94 23 66 00 EE 23 18 C9 03 89 94 89 94 89 94 89 94 89 94 23 66 00 EE 6E 00 23 08 00 EE 23 66 6E 00 22 FC 00 EE 23 66 6E 00 23 18 00 EE 23 66 6E 00 23 08 00 EE 23 08 7E 03 23 08 00 EE 22 FC 7E 02 22 FC 00 EE 23 18 7E 02 23 18 00 EE 6C 00 23 3A 23 3A 23 3A 23 3A 00 EE 6C 00 23 24 23 24 23 24 23 24 23 24 23 24 00 EE 6C 00 23 50 23 50 23 50 23 50 00 EE A6 83 FE 1E FE 1E FE 1E FE 1E FC 1E F1 65 A4 7C D0 14 7C 02 00 EE A9 83 FE 1E FE 1E FE 1E FE 1E FC 1E F1 65 A4 7C D0 14 7C 02 00 EE AB 83 FE 1E FE 1E FE 1E FE 1E FC 1E F1 65 A4 7C D0 14 7C 02 00 EE 6C 00 60 1F 8A D0 8A C4 8A 02 8A 94 AD 83 FA 1E FA 1E F1 65 A4 80 D0 13 7C 01 3C 08 13 68 00 EE 60 1F 8A D0 8A 02 8A 94 AD 83 FA 1E FA 1E F1 65 A4 80 D0 13 60 1F 8A D0 7A 08 8A 02 8A 94 AD 83 FA 1E FA 1E F1 65 A4 80 D0 13 00 EE A6 83 FD 1E F0 65 30 00 F0 18 00 EE F0 07 30 00 13 BE 00 EE 6D 04 61 0C 60 1C 62 12 A4 1E F2 1E D0 16 FD 15 23 BE 60 14 62 0C A4 1E F2 1E D0 16 60 24 62 18 A4 1E F2 1E D0 16 FD 15 23 BE 60 0C 62 06 A4 1E F2 1E D0 16 60 2C 62 1E A4 1E F2 1E D0 16 FD 15 23 BE A4 1E 60 04 D0 16 60 34 62 24 A4 1E F2 1E D0 16 FD 15 23 BE 00 EE 00 00 0C 11 11 10 00 00 95 55 95 CD 00 00 53 55 55 33 40 40 44 42 41 46 00 40 6A 4A 4A 46 00 20 69 AA AA 69 00 00 20 90 88 30 64 01 65 07 62 00 63 00 60 00 81 30 D0 11 71 08 F4 1E D0 11 71 08 F4 1E D0 11 71 08 F4 1E D0 11 F4 1E 70 08 30 40 14 52 73 03 83 52 72 01 32 08 14 50 00 EE 60 B0 F0 60 40 A0 40 00 00 00 00 00 00 00 00 00 00 06 00 00 00 C6 00 00 00 DB 00 00 00 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 5F 06 00 00 FE C6 00 00 D3 FB 00 00 F0 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 00 00 00 F6 00 00 00 FB E0 00 00 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 06 00 00 00 C6 00 00 00 DB 00 00 00 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 06 00 00 00 C6 00 00 03 F1 00 00 30 E0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 00 00 00 C6 00 00 00 D9 00 00 00 E0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 2F 06 00 00 FF C6 00 00 69 DB 00 00 E0 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 00 00 00 76 00 00 00 F3 E0 00 00 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 1F 07 0F 00 FF FE FC 7E 00 00 3E 7C 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 2F 1B 07 00 FF F0 FB 1F 00 00 FE B0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 17 0F 00 00 FF F8 7E 0F 00 0C 14 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 2F 0B 0F 00 FE E0 FC 3F 00 00 7E FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 17 1F 03 00 FF F0 FF 1F 80 00 FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0B 0F 00 00 FE F8 7E 0F 00 1C 3E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 17 17 0F 00 FE C0 F8 3F 00 00 FE FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 2B 1F 00 00 FF E0 7F 1F 80 04 1C 3C 04 05 1B 05 1B 17 04 17 07 08 17 08 1C 08 15 1A 06 03 00 14 1B 0A 16 16 1A 0A 0E 1A 08 02 00 10 1E 0C 15 17 18 0B 08 19 0B 01 00 0D 20 0F 13 19 15 0A 03 15 20 12 0E 01 10 1A 01 0B 14 08 00 11 1F 14 0D 1B 12 01 04 09 15 06 00 0D 1D 16 0A 1B 15 03 06 08 18 05 01 09 1C 16 06 19 17 05 07 08 1B 05 04 05 1B 17 04 17 17 08 08 08 04 14 09 02 1A 18 1E 07 05 13 08 07 05 12 0E 01 19 19 1F 0A 02 12 08 06 08 12 14 02 18 1A 20 0C 00 10 09 05 0A 12 19 04 15 1B 00 0E 1F 0E 0B 03 0C 14 1D 08 00 0B 0E 01 11 1B 1D 11 0B 17 1F 0D 00 08 12 01 0D 1A 1B 12 08 18 1E 12 02 07 17 02 0A 17 19 13 1B 17 1B 05 04 17 04 05 17 08 17 14 1D 0A 06 04 15 1A 01 15 1B 09 0B 05 1B 0E 08 03 0E 1B 00 11 1E 0C 10 04 17 12 0A 02 08 1A 1F 0F 00 0D 16 04 10 14 0D 02 1E 14 03 16 1B 06 02 09 09 13 10 01 1A 18 1F 0A 01 12 07 06 04 0F 14 02 13 1B 20 0E 01 0E 0D 05 02 0A 18 03 0B 1B 1F 13 04 0A 12 05 04 17 04 05 1B 17 1B 05 08 14 08 08 15 19 1E 08 00 11 09 02 14 17 1B 0C 0E 17 1F 0B 00 0B 0F 01 10 1A 1C 10 09 13 1F 0E 02 06 15 02 0A 1A 1A 15 08 0E 1F 0F 08 02 05 18 1A 05 17 19 0A 09 1E 12 00 14 0E 01 11 1B 1C 09 0F 05 1D 14 00 0E 0C 1B 13 02 1D 0E 15 04 1C 15 00 0A 07 1A 16 05 1B 11 1B 05 04 05 04 17 1B 17 18 08 08 08 03 14 0A 02 19 19 1F 08 04 12 09 06 05 12 11 02 17 1A 20 0C 01 10 0A 05 07 11 17 03 14 1B 00 0D 20 0F 0C 03 0A 12 1C 07 00 0A 11 1B 0F 02 1E 11 0B 14 1F 0B 00 08 12 01 0D 1B 1B 13 0A 16 1F 0F 02 06 15 01 0A 19 19 14 07 17 1E 13 03 06 19 03 08 17 18 14 1B 05 04 05 1B 17 04 17 17 08 08 08 1B 08 16 1A 05 04 01 15 1A 09 17 15 1A 0A 11 1B 06 03 00 12 1D 0A 17 16 17 0A 0B 1A 07 02 00 10 1F 0C 16 17 15 0A 06 18 0A 01 20 0E 00 0E 14 19 13 08 02 14 20 11 0E 01 11 1B 02 0B 14 05 00 0F 1F 14 0D 1B 12 02 04 0A 17 04 01 0A 1D 15 08 1A 15 05 06 09 1B 17 1B 05 04 17 04 05 18 08 18 14 02 12 19 18 0A 02 1E 07 04 13 14 17 04 0E 17 19 11 01 20 0B 01 10 0F 18 08 0A 15 1A 17 02 20 0F 00 0D 09 18 0F 08 12 1A 01 08 1C 06 04 16 1D 13 16 09 0F 1B 05 04 00 12 1E 0A 18 16 1B 0D 0B 1A 0C 01 00 0E 1E 0E 12 17 1D 12 07 19 14 01 00 09 1B 12 0D 17 04 17 04 05 1B 17 1B 05 08 14 08 08 0A 03 01 14 1F 0B 16 1A 0B 05 04 10 11 05 00 11 20 11 10 1B 0F 02 03 0C 16 09 00 0E 1D 16 0A 1A 15 02 05 07 17 0E 00 0D 17 1A 1A 04 05 17 08 03 15 13 01 0A 1F 08 0E 01 11 1B 03 13 10 17 02 08 20 0E 13 01 0C 1A 02 0E 0A 18 03 07 1F 12 18 02 09 17 04 0B 04 05 1B 05 1B 17 04 17 1C 08 15 1A 06 03 00 14 1A 0A 0E 1A 08 02 00 10 18 0B 08 19 0B 01 00 0D 15 0A 03 15 0E 01 01 0B 14 08 00 11 12 01 14 12 15 06 00 0D 13 14 15 03 18 05 01 09 11 15 17 05 1B 05 04 05 10 15 17 08 09 02 1E 07 0E 15 08 07 0E 01 1F 0A 0C 15 08 06 14 02 0B 14 20 0C 09 05 19 04 0A 14 1F 0E 0B 03 1D 08 08 13 0E 01 1D 11 1F 0D 12 01 07 11 1B 12 1E 12 17 02 06 10 19 13 1B 17 1B 05 06 0E 17 08 1D 0A 15 1A 07 0C 1B 09 1B 0E 0E 1B 08 0A 1E 0C 17 12 08 1A 1F 0F 0B 08 10 14 1E 14 03 16 0F 07 09 13 1A 18 01 12 12 08 04 0F 13 1B 15 09 01 0E 02 0A 0B 1B 18 0B 04 0A 04 17 04 05 19 0E 08 14 00 11 09 02 18 10 06 12 00 0B 0F 01 16 13 05 10 02 06 14 14 15 02 05 0E 11 14 08 02 1A 05 05 0D 0E 13 0E 01 1C 09 06 0C 0D 11 13 02 1D 0E 06 0A 0E 0F 16 05 1B 11 07 09 10 0E 18 08 08 08 08 14 12 0E 04 12 09 06 13 16 14 0E 01 10 0A 05 0F 16 16 10 00 0D 0C 03 0A 15 16 12 00 0A 0F 02 07 13 00 08 15 13 12 01 05 10 02 06 15 01 13 15 05 0E 03 06 19 03 11 15 05 0B 1B 05 04 05 0F 15 17 08 1B 08 05 04 0E 15 1A 09 1A 0A 06 03 0C 14 1D 0A 17 0A 07 02 1F 0C 0B 14 15 0A 0A 01 20 0E 0A 13 13 08 20 11 0E 01 09 12 14 05 1F 14 08 11 12 02 17 04 1D 15 07 10 15 05 1B 17 1B 05 06 0E 18 08 19 18 1E 07 07 0C 14 17 17 19 20 0B 08 0A 0F 18 15 1A 0B 08 20 0F 09 18 12 1A 0E 07 04 16 1D 13 0F 1B 12 07 00 12 18 16 0B 1A 00 0E 16 09 12 17 07 19 00 09 18 0B 0D 17 04 17 04 05 19 0E 08 14 0A 03 01 14 18 10 0B 05 11 05 00 11 16 12 0F 02 16 09 00 0E 15 02 13 13 17 0E 00 0D 1A 04 08 03 15 13 01 0A 1F 08 0E 01 10 17 02 08 20 0E 13 01 0A 18 03 07 1F 12 18 02 10 0E 06 07 19 07 19 15 0D 0E 1B 09 16 17 09 05 0B 0E 1C 0C 12 18 0D 04 1C 0E 09 0C 0E 19 11 04 1B 0E 09 0A 0B 18 14 05 1A 0E 07 16 0A 09 14 12 1A 0E 05 14 0C 07 13 14 1A 0E 04 11 0E 07 11 15 1B 0E 04 0E 10 15 10 07 06 0C 1C 0F 0E 15 11 07 0A 0A 1D 10 0C 15 13 08 0E 0A 1C 12 0B 14 14 08 12 0B 0A 14 1A 14 05 08 15 0E 07 06 08 13 17 16 15 11 0A 04 07 11 17 0B 13 15 0D 04 06 10 18 0C 10 17 10 05 06 0E 19 0E 11 07 0B 17 18 10 07 0C 11 09 06 16 17 12 08 0A 10 0A 03 13 14 14 1A 0A 0E 0B 11 15 1D 0D 03 0F 0D 0A 1D 10 0D 15 12 08 0C 08 1A 14 09 13 15 09 0D 06 15 17 07 11 18 0B 10 17 10 05 19 0E 06 0E 0A 15 13 05 18 10 07 0C 06 10 17 05 16 13 09 0A 05 0C 1A 07 14 14 0C 09 08 07 11 14 1C 0A 05 11 0C 04 0E 13 1D 0D 04 0F 0D 11 11 03 1D 10 03 0D 0E 0F 16 04 1B 13 04 09 10 0E 19 07 06 07 06 15 12 0E 04 13 09 05 16 17 14 0E 03 10 0D 04 12 18 03 0E 16 10 11 03 0E 18 04 0E 16 12 14 04 0B 17 05 0E 18 06 15 13 0B 0A 05 0E 1A 08 13 15 0C 08 05 0E 1B 0B 11 15 0E 07 1B 0E 04 0E 0F 15 0F 07 19 10 03 0D 11 07 0E 15 15 12 02 0C 13 07 0C 14 11 12 03 0A 14 08 0B 14 0D 11 15 08 05 08 1A 14 0A 0E 18 16 17 09 08 06 0A 0B 15 18 18 0B 08 11 0C 07 12 18 19 0C 07 10 10 17 10 05 06 0E 19 0E 0E 15 14 05 07 0C 18 10 0E 13 19 06 08 0A 17 12 0F 12 1C 09 0B 08 05 12 11 11 0E 07 02 0F 1C 0D 12 12 02 0C 12 07 0D 14 13 14 05 08 16 09 0A 13 12 16 0A 05 18 0B 07 11 0F 17 10 05 19 0E 06 0E 15 07 0C 17 07 0C 18 10 19 0C 08 17 09 09 16 12 1A 10 05 15 0B 08 13 13 17 15 0E 08 03 12 1A 0B 13 18 11 09 02 0F 1B 0D 12 0B 0E 19 02 0C 1C 0F 11 0D 09 18 04 09 1B 13 10 10 0C 14 07 17 04 1A 03 1C 03 1D 05 1D 08 1B 0C 19 10 16 14 13 17 10 1A 0D 1B 0A 1B 08 19 06 17 05 13 06 10 07 0D 08 0A 0B 07 0E 06 10 06 12 08 15 0A 17 0D 18 10 19 13 19 16 18 18 17 19 14 10 19 0F 17 0D 17 0A 19 06 1A 06 16 09 13 09 11 07 10 03 0E 03 0B 08 0B 0B 0B 0C 0A 0C 06 0E 02 10 04 11 08 12 0A 14 09 19 07 1B 09 19 0D 17 0F 17 10 1A 12 1D 15 1A 16 15 15 14 16 13 18 12 1D 10 19 0E 17 0D 17 0B 17 08 17 05 19 02 19 03 17 08 14 0B 12 0E 11 10 10 11 10 14 0E 19 0B 1C 09 1C 09 19 09 17 0A 16 0A 14 09 13 07 11 04 0F 02 0B 03 09 06 09 09 09 0A 09 0B 08 0B 07 0B 07 0B 10 19 13 18 15 17 16 16 18 15 18 15 18 14 15 12 10 10 0B 0E 09 0C 08 0C 09 0B 0A 0A 0B 09 0D 08 10 06 14 04 1A 03 1D 04 1C 08 19 0B 15 0D 12 0F 10 10 0D 11 0A 13 06 16 03 19 03 1C 07 1C 0C 1A" );
				//cpu.LoadROM( new byte[] { 0x00, 0xE0, 0xA2, 0x2A, 0x60, 0x0C, 0x61, 0x08, 0xD0, 0x1F, 0x70, 0x09, 0xA2, 0x39, 0xD0, 0x1F, 0xA2, 0x48, 0x70, 0x08, 0xD0, 0x1F, 0x70, 0x04, 0xA2, 0x57, 0xD0, 0x1F, 0x70, 0x08, 0xA2, 0x66, 0xD0, 0x1F, 0x70, 0x08, 0xA2, 0x75, 0xD0, 0x1F, 0x12, 0x28, 0xFF, 0x00, 0xFF, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0x3C, 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0x38, 0x00, 0x3F, 0x00, 0x3F, 0x00, 0x38, 0x00, 0xFF, 0x00, 0xFF, 0x80, 0x00, 0xE0, 0x00, 0xE0, 0x00, 0x80, 0x00, 0x80, 0x00, 0xE0, 0x00, 0xE0, 0x00, 0x80, 0xF8, 0x00, 0xFC, 0x00, 0x3E, 0x00, 0x3F, 0x00, 0x3B, 0x00, 0x39, 0x00, 0xF8, 0x00, 0xF8, 0x03, 0x00, 0x07, 0x00, 0x0F, 0x00, 0xBF, 0x00, 0xFB, 0x00, 0xF3, 0x00, 0xE3, 0x00, 0x43, 0xE0, 0x00, 0xE0, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0xE0, 0x00, 0xE0 } );

				RootPanel.AddChild<NameTags>();
				RootPanel.AddChild<CrosshairCanvas>();
				CrosshairCanvas.SetCrosshair( RootPanel.AddChild<StandardCrosshair>() );
				RootPanel.AddChild<ChatBox>();
				RootPanel.AddChild<VoiceList>();
				RootPanel.AddChild<KillFeed>();
				RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();



				var back = RootPanel.AddChild<Panel>( "chip8Background" );
				back.StyleSheet.Load( "/Chip8.scss" );
				display = back.AddChild<Chip8Display>();
				display.CPUToCycle = cpu;
				keypad = back.AddChild<Keypad>();
				keypad.SetKeyStateListener = delegate ( int keyValue, bool state ) { cpu.Keys[keyValue] = state; };
				var romInputContainer = back.AddChild<Panel>( "romInputContainer" );
				romInput = romInputContainer.AddChild<ROMInput>();
				romInputContainer.AddChild<Label>( "tipLabel romInputLabel" ).Text = "ROM Input";
				var runButton = romInputContainer.AddChild<ClickyButton>( "runButton" );
				runButton.Text = "Run ROM";
				runButton.AddEventListener( "onclick", delegate () { cpu.Reset(); cpu.LoadROMString( romInput.Text ); } );
			}
		}
		
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if (IsClient)
			{
				
			}
		}
	}

}
