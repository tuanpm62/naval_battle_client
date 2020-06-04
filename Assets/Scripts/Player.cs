// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

public partial class Player : Schema {
	[Type(0, "int16")]
	public short seat = default(short);

	[Type(1, "string")]
	public string sessionId = default(string);

	[Type(2, "boolean")]
	public bool connected = default(bool);
}

