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
			GD.Print($"obstacle: {obstacle.GlobalPosition}");

			if (obstacle.GlobalPosition.X + obstacle.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
			{
				RespawnObstacle(obstacle);
			}
		});
	}

	private void InitGame()
	{
		playerVelocity = Vector2.Zero;
		player.GlobalPosition = gameBounds.GlobalPosition;
		obstacles.ForEach((obstacle) =>
		{
			RespawnObstacle(obstacle);
		});
	}

	private void RespawnObstacle(Obstacle obstacle)
	{
		var random = new Random();
		var randomSize = new Vector2(
			random.Next(50, 100),
			random.Next(50, 100)
		);
		var randomY = random.Next(
			(int)(gameBounds.GlobalPosition.Y + randomSize.Y / 2),
			(int)(gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2 - randomSize.Y / 2)
			);
		obstacle.SetSize(randomSize);
		obstacle.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + obstacle.shape.Size.X / 2,
			randomY
		);

	}

	private float gravityAcceleration = 9.8f * 20;
	private float jetpackForce = 750f;
	private float obstacleSpeed = 400f;

}
