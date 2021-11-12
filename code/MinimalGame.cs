using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Chip8
{
	public partial class MinimalGame : Sandbox.Game
	{
		[Net]
		MinimalHudEntity hud { get; set; }
		public MinimalGame()
		{
			if ( IsServer )
			{
				hud = new MinimalHudEntity();
			}

			if ( IsClient )
			{
				
			}
		}

		/// <summary>
		/// A client has joined the server. Make them a pawn to play with
		/// </summary>
		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new MinimalPlayer();
			client.Pawn = player;

			player.Respawn();
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
