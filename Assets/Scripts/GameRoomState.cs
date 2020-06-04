// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

public partial class GameRoomState : Schema {
	[Type(0, "map", typeof(MapSchema<Player>))]
	public MapSchema<Player> players = new MapSchema<Player>();

	[Type(1, "string")]
	public string phase = default(string);

	[Type(2, "int16")]
	public short playerTurn = default(short);

	[Type(3, "int16")]
	public short winningPlayer = default(short);

	[Type(4, "array", typeof(ArraySchema<short>), "int16")]
	public ArraySchema<short> player1Shots = new ArraySchema<short>();

	[Type(5, "array", typeof(ArraySchema<short>), "int16")]
	public ArraySchema<short> player2Shots = new ArraySchema<short>();
}

