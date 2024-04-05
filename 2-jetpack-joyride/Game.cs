using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Background background;
	private Player player;
	private List<Obstacle> obstacles = new List<Obstacle>();

	private Vector2 playerVelocity;
	private bool isGrounded = false;
	private bool isGroundedPrevious = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello World");
		gameBounds = GetNode<GameBounds>("GameBounds");
		background = GetNode<Background>("Background");
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
		isGroundedPrevious = isGrounded;
		if (Input.IsActionPressed("jetpack"))
		{
			playerVelocity += new Vector2(0f, -jetpackForce) * (float)delta;
		}

		GD.Print($"velocity ${playerVelocity}");
		var playerCollision = player.MoveAndCollide(playerVelocity * (float)delta);
		if (playerCollision != null)
		{

			var collidedObject = (Node2D)playerCollision.GetCollider();
			var collidedWithFloor = playerCollision != null && collidedObject.GlobalPosition.Y >= (player.GlobalPosition.Y + player.shape.Height / 2);

			isGrounded = collidedWithFloor;
			GD.Print($"collision position: {collidedObject.GlobalPosition} {player.GlobalPosition.Y + player.shape.Height / 2} isGrounded: {isGrounded}");
			GD.Print($"KEK collision set grounded to {isGrounded}");
			playerVelocity.Y = 0f;
		}
		else
		{
			var playerBottom = player.GlobalPosition.Y + player.shape.Height / 2;
			var gameBoundsBottom = gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2;
			var distance = Mathf.Abs(playerBottom - gameBoundsBottom);
			GD.Print($"ELSE {playerBottom} - {gameBoundsBottom} = {distance}");
			if (distance > 3f)
			{
				isGrounded = false;
			}
		}


		if (!isGrounded)
		{
			playerVelocity += new Vector2(0f, gravityAcceleration) * (float)delta;
		}

		if (isGrounded)
		{
			var wheel = (Node2D)player.FindChild("Wheel");
			GD.Print($"WHEEL: {wheel}");
			wheel.RotationDegrees += obstacleSpeed * (float)delta;
		}

		var groundedDuration = 4f;
		var airDuration = 2f;
		if (isGrounded && !isGroundedPrevious)
		{
			var legBody = ((Node2D)player.FindChild("LegBody"));
			var tween = CreateTween();
			tween.TweenProperty(legBody, new NodePath("rotation_degrees"), 15f, groundedDuration).SetTrans(Tween.TransitionType.Spring);


			TweenWheelBounce();

		}
		else if (!isGrounded && isGroundedPrevious)
		{
			var legBody = ((Node2D)player.FindChild("LegBody"));
			var tween = CreateTween();

			tween.TweenProperty(legBody, new NodePath("rotation_degrees"), 0f, airDuration).SetTrans(Tween.TransitionType.Spring);
		}
		// obstacles.ForEach((obstacle) =>
		// {
		// 	obstacle.GlobalPosition = new Vector2(
		// 							obstacle.GlobalPosition.X - obstacleSpeed * (float)delta,
		// 							obstacle.GlobalPosition.Y
		// 						);
		// 	GD.Print($"obstacle: {obstacle.GlobalPosition}");

		// 	if (obstacle.GlobalPosition.X + obstacle.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
		// 	{
		// 		RespawnObstacle(obstacle);
		// 	}
		// });
		background.MoveBy(-obstacleSpeed * (float)delta);
		GD.Print($"texture: {background.main.Texture.GetSize().X} multiplied by scale: {background.main.Texture.GetSize().X * background.main.Scale.X} gameBounds: {gameBounds.shape.Size.X}");
		if (background.main.GlobalPosition.X + (background.main.Texture.GetSize().X * background.main.Scale.X) / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
		{
			var backupPosition = background.backup.GlobalPosition;
			background.backup.GlobalPosition = background.main.GlobalPosition;
			background.main.GlobalPosition = backupPosition;

			background.backup.GlobalPosition = new Vector2(
				gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + background.backup.Texture.GetSize().X * background.backup.Scale.X / 2,
				background.backup.GlobalPosition.Y
			);
		}
	}

	private async void TweenWheelBounce()
	{
		var wheelContainer = (Node2D)player.FindChild("WheelContainer");
		var wheelTween = CreateTween();
		var duration = 0.5f;
		var offset = 0.1f;
		wheelTween.TweenProperty(wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f) + new Vector2(offset, -offset * 2), duration).SetTrans(Tween.TransitionType.Bounce);
		await ToSignal(wheelTween, "finished");
		var wheelTween2 = CreateTween();
		wheelTween2.TweenProperty(wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f), duration).SetTrans(Tween.TransitionType.Bounce);
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
