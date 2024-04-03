using Godot;
using System;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Player player;

	private Vector2 playerVelocity;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello World");
		gameBounds = GetNode<GameBounds>("GameBounds");
		player = GetNode<Player>("Player");

		InitGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		var playerCollision = player.MoveAndCollide(playerVelocity * (float)delta);
		playerVelocity += new Vector2(0f, gravityAcceleration) * (float)delta;
	}

	private void InitGame()
	{
		playerVelocity = Vector2.Zero;
		player.GlobalPosition = gameBounds.GlobalPosition;
	}

	private float gravityAcceleration = 9.8f * 20;

}
