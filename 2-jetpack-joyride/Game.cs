using Godot;
using System;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Player player;

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

	private void InitGame()
	{
		GD.Print($"{player.GlobalPosition} {gameBounds.GlobalPosition}");
		player.GlobalPosition = gameBounds.GlobalPosition;
		GD.Print($"{player.GlobalPosition} {gameBounds.GlobalPosition}");
	}
}
