using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Player player;
	private List<Obstacle> obstacles = new List<Obstacle>();

	private Vector2 playerVelocity;
	private bool isGrounded = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello World");
		gameBounds = GetNode<GameBounds>("GameBounds");
		player = GetNode<Player>("Player");
		obstacles.Add(GetNode<Obstacle>("Obstacle"));

		InitGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("jetpack"))
		{
			playerVelocity += new Vector2(0f, -jetpackForce) * (float)delta;
		}

		var playerCollision = player.MoveAndCollide(playerVelocity * (float)delta);
		if (playerCollision != null)
		{

			var collidedObject = (Node2D)playerCollision.GetCollider();
			var collidedWithFloor = playerCollision != null && collidedObject.GlobalPosition.Y >= (player.GlobalPosition.Y + player.shape.Height / 2);

			isGrounded = collidedWithFloor;
			GD.Print($"collision position: {collidedObject.GlobalPosition} isGrounded: {isGrounded}");
			playerVelocity.Y = 0f;
		}
		else
		{
			isGrounded = false;
		}


		if (!isGrounded)
		{
			playerVelocity += new Vector2(0f, gravityAcceleration) * (float)delta;
		}
		GD.Print($"player velocity: {playerVelocity} player position: {player.GlobalPosition}");

		obstacles.ForEach((obstacle) =>
		{
			obstacle.GlobalPosition = new Vector2(
									obstacle.GlobalPosition.X - obstacleSpeed * (float)delta,
									obstacle.GlobalPosition.Y
								);
			// if (obstacle.GlobalPosition.X > gameBounds.GlobalPosition.X + gameBounds.shape.Size.X)
			// {
			// 	obstacle.GlobalPosition = new Vector2(
			// 						obstacle.GlobalPosition.X - obstacleSpeed * (float)delta,
			// 						obstacle.GlobalPosition.Y
			// 					);
			// }
			GD.Print($"obstacle: {obstacle.GlobalPosition}");
		});
	}

	private void InitGame()
	{
		playerVelocity = Vector2.Zero;
		player.GlobalPosition = gameBounds.GlobalPosition;
		obstacles.ForEach((obstacle) =>
		{
			obstacle.GlobalPosition = new Vector2(gameBounds.GlobalPosition.X + gameBounds.shape.Size.X + 100, gameBounds.GlobalPosition.Y);
		});
	}

	private float gravityAcceleration = 9.8f * 20;
	private float jetpackForce = 750f;
	private float obstacleSpeed = 200f;

}
