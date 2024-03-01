using Godot;
using System;

public partial class Game : Node2D
{
	private float playerSpeed = 300f;
	private float paddleOffset = 40f;

	private GameBounds gameBounds;
	private Player leftPlayer;
	private Player rightPlayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");
		var gameBoundsCenter = gameBounds.shape.Size.Y / 2;
		leftPlayer = GetNode<Player>("LeftPlayer");
		leftPlayer.GlobalPosition = new Vector2(paddleOffset, gameBoundsCenter);

		rightPlayer = GetNode<Player>("RightPlayer");
		rightPlayer.GlobalPosition = new Vector2(gameBounds.shape.Size.X - paddleOffset, gameBoundsCenter);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		var leftPlayerDirection = 0;

		if (Input.IsActionPressed("left_player_up"))
		{
			leftPlayerDirection = -1;

		}
		else if (Input.IsActionPressed("left_player_down"))
		{
			leftPlayerDirection = 1;
		}
		handlePlayerInput(leftPlayer, leftPlayerDirection, delta);

		var rightPlayerDirection = 0;

		if (Input.IsActionPressed("right_player_up"))
		{
			rightPlayerDirection = -1;

		}
		else if (Input.IsActionPressed("right_player_down"))
		{
			rightPlayerDirection = 1;
		}
		handlePlayerInput(rightPlayer, rightPlayerDirection, delta);
	}

	private void handlePlayerInput(Player player, int direction, double delta)
	{
		if (direction != 0)
		{
			var currentPosition = player.GlobalPosition;
			var newPosition = new Vector2(
				currentPosition.X,
				currentPosition.Y + direction * playerSpeed * (float)delta
			);
			player.GlobalPosition = newPosition;
		}
	}
}
