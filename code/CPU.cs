using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using System.Collections;

namespace Chip8
{
	public class CPU
	{
		public bool[,] Display = new bool[64, 32];
		public byte[] V = new byte[16];
		public ushort I;
		public byte[] Memory = new byte[4096];
		public byte DT;
		public byte ST;
		public ushort PC = 0x200;
		public byte SP;
		public Stack<ushort> CallStack = new();
		public bool[] Keys = new bool[16];
		public float LastCycle;



		public byte[] FontSet = new byte[] {
			0xF0, 0x90, 0x90, 0x90, 0xF0,
			0x20, 0x60, 0x20, 0x20, 0x70,
			0xF0, 0x10, 0xF0, 0x80, 0xF0,
			0xF0, 0x10, 0xF0, 0x10, 0xF0,
			0x90, 0x90, 0xF0, 0x10, 0x10,
			0xF0, 0x80, 0xF0, 0x10, 0xF0,
			0xF0, 0x80, 0xF0, 0x90, 0xF0,
			0xF0, 0x10, 0x20, 0x40, 0x40,
			0xF0, 0x90, 0xF0, 0x90, 0xF0,
			0xF0, 0x90, 0xF0, 0x10, 0xF0,
			0xF0, 0x90, 0xF0, 0x90, 0x90,
			0xE0, 0x90, 0xE0, 0x90, 0xE0,
			0xF0, 0x80, 0x80, 0x80, 0xF0,
			0xE0, 0x90, 0x90, 0x90, 0xE0,
			0xF0, 0x80, 0xF0, 0x80, 0xF0,
			0xF0, 0x80, 0xF0, 0x80, 0x80
		};

		public CPU ()
		{
			Reset();
		}

		public void Reset ()
		{
			Display = new bool[64, 32];
			V = new byte[16];
			I = 0;
			Memory = new byte[4096];
			DT = 0;
			ST = 0;
			PC = 0x200;
			SP = 0;
			CallStack = new();
			Keys = new bool[16];
			LastCycle = Time.Now;

			var i = 0;
			foreach ( byte spriteRow in FontSet )
			{
				Memory[i] = spriteRow;

				i++;
			}
		}

		public void ClearROM ()
		{
			for ( var ii = 0x200; ii <= 0xFFF; ii++ )
			{
				Memory[ii] = 0x0;
			}
		}

		public void LoadROM (byte[] rom)
		{
			ClearROM();

			var i = 0;
			foreach ( byte romByte in rom )
			{
				Memory[i + 0x200] = romByte;

				i++;
			}
		}

		public void LoadROMString (string rom)
		{
			var formatted = Regex.Replace( rom, @"\s+", "" );
			Log.Info( formatted );
			LoadROM( Convert.FromHexString( formatted ));
		}

		public void Cycle ()
		{
			if ( PC > 4095 )
			{
				return;
			}

			ushort instruction = (ushort)(((ushort)(Memory[PC] << 8)) | Memory[PC + 1]);
			
			PC += 2;

			ExecuteInstruction( instruction );

			if (Time.Now - LastCycle > 1 / 60)
			{
				DT = (byte)Math.Clamp( DT - 1, 0, 255 );
				ST = (byte)Math.Clamp( ST - 1, 0, 255 );
				LastCycle = Time.Now;
			}
		}

		public void ExecuteInstruction (ushort instruction)
		{
			var op = instruction & 0xF000;
			var x = (instruction & 0x0F00) >> 8;
			var y = (instruction & 0x00F0) >> 4;
			var n = instruction & 0x000F;
			byte nn = (byte)(instruction & 0x00FF);
			ushort nnn = (ushort)(instruction & 0x0FFF);

			switch (op)
			{
				case 0x0000:
					if (instruction == 0x00E0)
					{
						Display = new bool[64, 32];
					}
					else if (instruction == 0x00EE) 
					{
						PC = CallStack.Pop();
						SP--;
					}
					break;
				case 0x1000:
					PC = nnn;
					break;
				case 0x2000:
					SP++;
					CallStack.Push( PC );
					PC = nnn;
					break;
				case 0x3000:
					if (V[x] == nn)
					{
						PC += 2;
					}
					break;
				case 0x4000:
					if ( V[x] != nn )
					{
						PC += 2;
					}
					break;
				case 0x5000:
					if ( V[x] == V[y] )
					{
						PC += 2;
					}
					break;
				case 0x6000:
					V[x] = nn;
					break;
				case 0x7000:
					V[x] += nn;
					break;
				case 0x8000:
					switch (n)
					{
						case 0:
							V[x] = V[y];
							break;
						case 1:
							V[x] = (byte)(V[x] | V[y]);
							break;
						case 2:
							V[x] = (byte)(V[x] & V[y]);
							break;
						case 3:
							V[x] = (byte)(V[x] ^ V[y]);
							break;
						case 4:
							var result = V[x] + V[y];
							V[0xF] = 0;
							if ( result > 255 )
							{
								V[0xF] = 1;
							}
							V[x] = (byte)result;

							break;
						case 5:
							V[0xF] = 0;
							if ( V[x] > V[y] )
							{
								V[0xF] = 1;
							}
							V[x] -= V[y];
							break;
						case 6:
							V[0xF] = 0;
							if ( V[x] % 2 != 0 )
							{
								V[0xF] = 1;
							}
							V[x] = (byte)(V[x] >> 1);
							break;
						case 7:
							V[0xF] = 0;
							if (V[y] > V[x])
							{
								V[0xF] = 1;
							}
							V[x] = (byte)(V[y] - V[x]);
							break;
						case 0xE:
							V[0xF] = 0;
							if (V[x] >= 128)
							{
								V[0xF] = 1;
							}
							V[x] = (byte)(V[x] << 1);
							break;

					}
					
					break;
				case 0x9000:
					if (V[x] != V[y])
					{
						PC += 2;
					}
					break;
				case 0xA000:
					I = nnn;
					break;
				case 0xB000:
					PC = (ushort)(nnn + V[0]);
					break;
				case 0xC000:
					V[x] = (byte)(Rand.Int( 0, 255 ) & nn);
					break;
				case 0xD000:
					var drawX = V[x];
					var drawY = V[y];
					var rows = new byte[n];

					var ii = 0;
					for (var i = I; i < I + n; i++ )
					{
						rows[ii] = Memory[i];
						ii++;
					}

					V[0xF] = 0;

					var rowNum = 0;
					foreach (byte rowData in rows)
					{
						for (var i = 0; i <= 7; i++ )
						{
							var thisX = (drawX + i) % 64;
							var thisY = (drawY + rowNum) % 32;
							
							var bit = ((rowData >> (7 - i)) & 1) == 1;
							var prevValue = Display[thisX, thisY];
							var newValue = Display[thisX, thisY] ^ bit;

							Display[thisX, thisY] = newValue;
							
							if (prevValue && !newValue) {
								V[0xF] = 1;
							}
						}
						rowNum++;
					}

					break;
				case 0xE000:
					if (nn == 0x9E)
					{
						if ( Keys[V[x]] )
						{
							PC += 2;
						}
					}
					else
					{
						if ( !Keys[V[x]] )
						{
							PC += 2;
						}
					}
					
					break;
				case 0xF000:
					switch (nn)
					{
						case 0x07:
							V[x] = DT;
							break;
						case 0x0A:
							var i = 0;
							var stay = true;
							foreach (bool state in Keys)
							{
								if (state)
								{
									V[x] = (byte)i;
									stay = false;
									break;
								}
								i++;
							}
							
							if (stay)
							{
								PC -= 2;
							}

							break;
						case 0x15:
							DT = V[x];
							break;
						case 0x18:
							ST = V[x];
							break;
						case 0x1E:
							I += V[x];
							break;
						case 0x29:
							I = (ushort)(5 * V[x]);
							break;
						case 0x33:
							var val = Convert.ToString( V[x] );
							if (val.Length == 2)
							{
								val = "0" + val;
							}
							if (val.Length == 1)
							{
								val = "00" + val;
							}

							Memory[I] = Convert.ToByte( val.Substring(0, 1) );
							Memory[I + 1] = Convert.ToByte( val.Substring( 1, 1 ) );
							Memory[I + 2] = Convert.ToByte( val.Substring( 2, 1 ) );
							break;
						case 0x55:
							for (var iii = 0; iii <= x; iii++ )
							{
								Memory[I + iii] = V[iii];
							}
							break;
						case 0x65:
							for ( var iii = 0; iii <= x; iii++ )
							{
								V[iii] = Memory[I + iii];
							}
							break;

					}

					break;
			}
		}
	}
}
