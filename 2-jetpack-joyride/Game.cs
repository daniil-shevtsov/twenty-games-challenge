using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Background background;
	private Player player;
	private Node2D legBody;
	private Node2D wheel;
	private Node2D wheelContainer;
	private List<Obstacle> obstacles = new List<Obstacle>();
	private List<Coin> coins = new List<Coin>();
	private float currentScore = 0;
	private float previousScore = 0;
	private float bestScore = 0;
	private Label scoreLabel;

	private Vector2 playerVelocity;
	private float wheelAngularVelocity = 0f;
	private bool isGrounded = false;
	private bool isGroundedPrevious = false;
	private PackedScene coinScene = null;
	private PackedScene obstacleScene = null;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");
		background = GetNode<Background>("Background");
		player = GetNode<Player>("Player");
		legBody = (Node2D)player.FindChild("LegBody");
		wheel = (Node2D)player.FindChild("Wheel");
		wheelContainer = (Node2D)player.FindChild("WheelContainer");
		scoreLabel = GetNode<Label>("ScoreLabel");
		scoreLabel.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2,
			gameBounds.GlobalPosition.Y - gameBounds.shape.Size.Y / 2
		);

		coinScene = GD.Load<PackedScene>("res://coin.tscn");
		obstacleScene = GD.Load<PackedScene>("res://obstacle.tscn");
		SpawnCoin();

		InitGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		GD.Print($"velocity {playerVelocity}");
		var travelledDistance = obstacleSpeed * (float)delta;

		isGroundedPrevious = isGrounded;
		if (Input.IsActionPressed("jetpack"))
		{
			playerVelocity += new Vector2(0f, -jetpackForce) * (float)delta;
		}

		var playerCollision = player.MoveAndCollide(playerVelocity * (float)delta);
		var groundedDuration = 4f;
		var airDuration = 2f;
		if (playerCollision != null)
		{
			var collidedObject = (Node2D)playerCollision.GetCollider();
			var collidedWithFloor = playerCollision != null && collidedObject.GlobalPosition.Y >= (player.GlobalPosition.Y + player.shape.Height / 2);
			var collidedWithCeiling = playerCollision != null && collidedObject == gameBounds.ceiling;

			isGrounded = collidedWithFloor;
			if (collidedWithFloor)
			{
				playerVelocity.Y = 0f;
				wheelAngularVelocity = obstacleSpeed;

				if (isGrounded && !isGroundedPrevious)
				{
					var tween = CreateTween();
					tween.TweenProperty(legBody, new NodePath("rotation_degrees"), 15f, groundedDuration).SetTrans(Tween.TransitionType.Spring);


					TweenWheelBounce();
				}
			}
			else if (collidedWithCeiling)
			{
				playerVelocity.Y = 0f;
			}
		}
		else
		{
			var playerBottom = player.GlobalPosition.Y + player.shape.Height / 2;
			var gameBoundsBottom = gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2;
			var distance = Mathf.Abs(playerBottom - gameBoundsBottom);
			if (distance > 3f)
			{
				isGrounded = false;
				if (!isGrounded && isGroundedPrevious)
				{
					var tween = CreateTween();

					tween.TweenProperty(legBody, new NodePath("rotation_degrees"), 0f, airDuration).SetTrans(Tween.TransitionType.Spring);
				}
			}
		}


		if (!isGrounded)
		{
			playerVelocity += new Vector2(0f, gravityAcceleration) * (float)delta;
			wheelAngularVelocity = wheelAngularVelocity - (obstacleSpeed * 0.25f) * (float)delta;
			if (wheelAngularVelocity < 0)
			{
				wheelAngularVelocity = 0f;
			}
		}
		wheel.RotationDegrees += wheelAngularVelocity * (float)delta;

		background.MoveBy(-travelledDistance);
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

		var coinsToRemove = new List<Coin>();
		coins.ForEach((coin) =>
		{
			coin.GlobalPosition = new Vector2(
				coin.GlobalPosition.X - travelledDistance,
				coin.GlobalPosition.Y
			);
			if (coin.GlobalPosition.X + coin.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
			{
				coinsToRemove.Add(coin);
			}
		});
		coinsToRemove.ForEach((coin) => RemoveCoin(coin));

		if (coins.Count < 1)
		{
			SpawnCoin();
		}

		var obstaclesToRemove = new List<Obstacle>();
		obstacles.ForEach((obstacle) =>
		{
			obstacle.GlobalPosition = new Vector2(
				obstacle.GlobalPosition.X - travelledDistance,
				obstacle.GlobalPosition.Y
			);
			if (obstacle.GlobalPosition.X + obstacle.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
			{
				obstaclesToRemove.Add(obstacle);
			}
		});
		obstaclesToRemove.ForEach((obstacle) => RemoveObstacle(obstacle));

		if (obstacles.Count < 1)
		{
			SpawnObstacle();
		}

		IncreaseScore(travelledDistance * 0.005f);
	}

	private async void TweenWheelBounce()
	{
		var wheelTween = CreateTween();
		var duration = 0.5f;
		var offset = 0.1f;
		wheelTween.TweenProperty(wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f) + new Vector2(offset, -offset * 2), duration).SetTrans(Tween.TransitionType.Bounce);
		wheelTween.SetParallel(true);
		var legBodyOffset = 75;
		wheelTween.TweenProperty(legBody, new NodePath("position"), new Vector2(0f, legBodyOffset), duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);

		await ToSignal(wheelTween, "finished");
		var wheelTween2 = CreateTween();
		wheelTween2.TweenProperty(wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f), duration).SetTrans(Tween.TransitionType.Bounce);
		wheelTween2.SetParallel(true);
		wheelTween2.TweenProperty(legBody, new NodePath("position"), new Vector2(0f, -legBodyOffset), duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);

	}

	private void InitGame()
	{
		playerVelocity = Vector2.Zero;
		player.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X,
			gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2 - player.shape.Height / 2
		);

		var obstaclesToRemove = new List<Obstacle>(obstacles);
		obstaclesToRemove.ForEach((obstacle) => RemoveObstacle(obstacle));
		var coinsToRemove = new List<Coin>(coins);
		coinsToRemove.ForEach((coin) => RemoveCoin(coin));

		SetScore(0);
	}

	private async void SpawnCoin()
	{
		var random = new Random();
		var coinInstance = (Coin)coinScene.Instantiate();

		var scene = GetTree().CurrentScene;
		scene.CallDeferred("add_child", coinInstance);
		await ToSignal(GetTree(), "process_frame");
		var randomSize = coinInstance.shape.Size;
		var randomY = random.Next(
			(int)(gameBounds.GlobalPosition.Y + randomSize.Y / 2),
			(int)(gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2 - randomSize.Y / 2)
			);
		coinInstance.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + 50f,
			randomY
		);

		coins.Add(coinInstance);

		// For some reason if I don't do this, bodyentered is being triggered by the player the moment coin is spawned.
		coinInstance.Monitoring = false;
		coinInstance.BodyEntered += (Node2D body) =>
		{
			OnCoinOverlap(coinInstance, body);
		};
		coinInstance.Monitoring = true;
	}

	private void RemoveCoin(Coin coin)
	{
		coins.Remove(coin);
		coin.QueueFree();
	}

	private void OnCoinOverlap(Coin coin, Node2D body)
	{
		if (body == player)
		{
			IncreaseScore(5);
			RemoveCoin(coin);
		}
	}

	private async void SpawnObstacle()
	{
		var random = new Random();
		var instance = (Obstacle)obstacleScene.Instantiate();

		var scene = GetTree().CurrentScene;
		scene.CallDeferred("add_child", instance);
		await ToSignal(GetTree(), "process_frame");

		var randomSize = random.Next(25, 50);
		var size = new Vector2(randomSize, randomSize);
		instance.SetSize(size);
		var randomY = random.Next(
			(int)(gameBounds.GlobalPosition.Y + size.Y / 2),
			(int)(gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2 - size.Y / 2)
			);
		instance.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + 100f,
			randomY
		);

		obstacles.Add(instance);

		// For some reason if I don't do this, bodyentered is being triggered by the player the moment obstacle is spawned.
		instance.Monitoring = false;
		instance.BodyEntered += (Node2D body) =>
		{
			OnObstacleOverlap(instance, body);
		};
		instance.Monitoring = true;
	}


	private void RemoveObstacle(Obstacle obstacle)
	{
		obstacles.Remove(obstacle);
		obstacle.QueueFree();
	}

	private void OnObstacleOverlap(Obstacle obstacle, Node2D body)
	{
		if (body == player)
		{
			previousScore = currentScore;
			if (currentScore > bestScore)
			{
				bestScore = currentScore;
			}
			InitGame();
		}
	}

	private void IncreaseScore(float value)
	{
		SetScore(currentScore + value);
	}


	private void SetScore(float score)
	{
		currentScore = score;
		scoreLabel.Text = Math.Round(currentScore, 0).ToString();
		GD.Print($"SCORE: {currentScore}");
	}

	private float gravityAcceleration = 9.8f * 20;
	private float jetpackForce = 750f;
	private float obstacleSpeed = 400f;

}
