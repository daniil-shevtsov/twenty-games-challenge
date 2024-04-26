using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class Game : Node2D
{

	private GameBounds gameBounds;
	private Background background;
	private Player player;
	private Vector2 defaultLegBodyLocalPosition;
	private Vector2 defaultWheelLocalPosition;
	private GpuParticles2D headParticles;
	private CanvasLayer gameMenu;
	private List<Obstacle> obstacles = new List<Obstacle>();
	private List<Coin> coins = new List<Coin>();
	private float currentScore = 0;
	private Label scoreLabel;
	private float bestScore = 0;
	private Label bestScoreLabel;
	private float previousScore = 0;
	private Label previousSCoreLabel;

	private Vector2 playerVelocity;
	private float notSpentJetpackAcceleration = 0f;
	private float jetpackForce = 0f;
	private float wheelAngularVelocity = 0f;
	private bool isGrounded = false;
	private bool isGroundedPrevious = false;
	private bool isCollidedWithCeilingPrevious = false;
	private PackedScene coinScene = null;
	private PackedScene obstacleScene = null;

	private Tween rotationTween = null;
	private Tween headTween = null;

	private bool isEasyDifficulty = false;
	private bool isPaused = true;

	private AudioStreamPlayer2D hitSound;
	private AudioStreamPlayer2D grindSound;
	private AudioStreamPlayer2D wheelSound;
	private AudioStreamPlayer2D wheelRotationSound;
	private AudioStreamPlayer2D enemySound;
	private AudioStreamPlayer2D rewardSound;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");
		background = GetNode<Background>("Background");

		hitSound = GetNode<AudioStreamPlayer2D>("HitSound");
		grindSound = GetNode<AudioStreamPlayer2D>("GrindSound");
		wheelSound = GetNode<AudioStreamPlayer2D>("WheelHitSound");
		wheelRotationSound = GetNode<AudioStreamPlayer2D>("WheelRotationSound");
		enemySound = GetNode<AudioStreamPlayer2D>("EnemySound");
		rewardSound = GetNode<AudioStreamPlayer2D>("RewardSound");

		player = GetNode<Player>("Player");
		defaultLegBodyLocalPosition = player.legBody.Position;
		defaultWheelLocalPosition = player.wheelContainer.Position;
		headParticles = GetNode<GpuParticles2D>("HeadParticles");
		headParticles.Emitting = false;
		scoreLabel = GetNode<Label>("ScoreLabel");
		bestScoreLabel = GetNode<Label>("BestScoreLabel");
		previousSCoreLabel = GetNode<Label>("PreviousScoreLabel");
		scoreLabel.GlobalPosition = new Vector2(
			gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2,
			gameBounds.GlobalPosition.Y - gameBounds.shape.Size.Y / 2
		);

		var easyDifficultySwitch = (CheckButton)FindChild("DifficultySwitch");
		easyDifficultySwitch.Pressed += onDifficultySwitched;
		var startGameButton = (Button)FindChild("StartButton");
		startGameButton.Pressed += onStartGameClicked;
		var quitGameButton = (Button)FindChild("QuitButton");
		quitGameButton.Pressed += onQuitGameClicked;

		gameMenu = GetNode<CanvasLayer>("GameMenu");

		coinScene = GD.Load<PackedScene>("res://coin.tscn");
		obstacleScene = GD.Load<PackedScene>("res://obstacle.tscn");

		SpawnCoin();

		InitGame();
		LoadGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionJustReleased("pause"))
		{
			UpdatePause(!isPaused);
		}

		var travelledDistance = obstacleSpeed * (float)delta;

		if (player.IsProcessing())
		{
			UpdatePlayer(delta);
		}

		if (background.IsProcessing())
		{
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
		}


		var coinsToRemove = new List<Coin>();
		coins.ForEach((coin) =>
		{
			if (coin.IsProcessing())
			{
				coin.GlobalPosition = new Vector2(
							coin.GlobalPosition.X - travelledDistance,
							coin.GlobalPosition.Y
						);
				coin.RotateBy(obstacleSpeed * 0.25f * (float)delta);

				if (coin.GlobalPosition.X + coin.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
				{
					coinsToRemove.Add(coin);
				}
			}
		});
		coinsToRemove.ForEach((coin) => RemoveCoin(coin));

		if (coins.Count < maxCoins)
		{
			SpawnCoin();
		}

		var obstaclesToRemove = new List<Obstacle>();
		obstacles.ForEach((obstacle) =>
		{
			if (obstacle.IsProcessing())
			{
				obstacle.GlobalPosition = new Vector2(
					obstacle.GlobalPosition.X - travelledDistance,
					obstacle.GlobalPosition.Y
				);
				obstacle.RotateBy(obstacleSpeed * 0.25f * (float)delta);

				obstacle.LookAtPlayer(player.GlobalPosition);

				if (obstacle.GlobalPosition.X + obstacle.shape.Size.X / 2 < gameBounds.GlobalPosition.X - gameBounds.shape.Size.X / 2)
				{
					obstaclesToRemove.Add(obstacle);
				}
			}
		});
		obstaclesToRemove.ForEach((obstacle) => RemoveObstacle(obstacle));

		if (obstacles.Count < maxObstacles)
		{
			SpawnObstacle();
		}

		if (scoreLabel.IsProcessing())
		{
			IncreaseScore(travelledDistance * 0.005f);
		}

		UpdatePause(isPaused);
	}

	private void UpdatePlayer(double delta)
	{
		var previousVelocty = playerVelocity;
		headParticles.GlobalPosition = new Vector2(
			player.GlobalPosition.X,
			player.GlobalPosition.Y - player.shape.Height / 2
		);
		isGroundedPrevious = isGrounded;
		if (Input.IsActionPressed("jetpack"))
		{
			playerVelocity += new Vector2(0f, -jetpackAcceleration) * (float)delta;
		}

		var playerCollision = player.MoveAndCollide(playerVelocity * (float)delta);
		var groundedDuration = 4f;
		var airDuration = 2f;

		if (playerCollision != null)
		{
			var collidedObject = (Node2D)playerCollision.GetCollider();
			var collidedWithFloor = playerCollision != null && collidedObject.GlobalPosition.Y >= (player.GlobalPosition.Y + player.shape.Height / 2);
			var collidedWithCeiling = playerCollision != null && collidedObject == gameBounds.ceiling;
			headParticles.Emitting = collidedWithCeiling;
			if (collidedWithCeiling && headParticles.Lifetime > 10f)
			{
				headParticles.Lifetime -= 150f * delta;
			}
			if (collidedWithCeiling && player.headContainer.RotationDegrees > -45f)
			{
				headTween?.Stop();
				// player.headContainer.RotationDegrees -= 100f * (float)delta;

			}

			GD.Print($"lifetime: {headParticles.Lifetime}");
			isGrounded = collidedWithFloor;
			if (collidedWithFloor)
			{
				playerVelocity.Y = 0f;
				wheelAngularVelocity = obstacleSpeed;

				if (isGrounded && !isGroundedPrevious)
				{
					if (rotationTween != null)
					{
						rotationTween.Stop();
					}
					rotationTween = CreateTween();
					GD.Print("KEK launch rotated tween");
					rotationTween.TweenProperty(player.legBodyHead, new NodePath("rotation_degrees"), 15f, groundedDuration).SetTrans(Tween.TransitionType.Spring);
					if (!wheelSound.Playing)
					{
						wheelSound.Play();
					}


					TweenWheelBounce();
				}
			}
			else if (collidedWithCeiling)
			{
				//notSpentJetpackAcceleration += jetpackAcceleration * (float)delta;
				playerVelocity.Y = 0f;
			}
			if (collidedWithCeiling)
			{
				var maxSpentJetpackAcceleration = 1000f;

				if (!grindSound.Playing)
				{
					var randomPitch = new Random().Next(45, 100) / 100f;
					grindSound.PitchScale = randomPitch;
					grindSound.Play();
				}

				if (Mathf.Abs(jetpackForce) > 0)
				{
					notSpentJetpackAcceleration = Mathf.Abs(jetpackForce);
					jetpackForce = 0f;

					var notSpentWeight2 = Mathf.Clamp(notSpentJetpackAcceleration / maxSpentJetpackAcceleration, 0, 1);

					// var degrees = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(0), Mathf.DegToRad(-45), notSpentWeight2));
					// player.headContainer.RotationDegrees = degrees;
					var offsetMin = 150f;
					var offsetMax = 150f;
					// player.legBody.Position = new Vector2(
					// 	player.legBody.Position.X,
					// 	Mathf.Lerp(defaultLegBodyLocalPosition.Y - offsetMin, defaultLegBodyLocalPosition.Y - offsetMax, notSpentWeight)
					// );
					// player.wheelContainer.Position = new Vector2(
					// 	player.wheelContainer.Position.X,
					// 	Mathf.Lerp(defaultWheelLocalPosition.Y - offsetMin, defaultWheelLocalPosition.Y - offsetMax, notSpentWeight)
					// );

					var finalOffset = offsetMin + (offsetMax - offsetMin) * notSpentWeight2;


					var duration = 0.25f;
					headTween?.Stop();
					headTween = CreateTween();
					var finalLegBodyPosition = new Vector2(
						player.legBody.Position.X,
						defaultLegBodyLocalPosition.Y - finalOffset
					);
					var transition = Tween.TransitionType.Bounce;
					headTween.TweenProperty(player.legBody, new NodePath("position"), finalLegBodyPosition, duration).SetTrans(transition);
					headTween.SetParallel();
					var finalWheelPosition = new Vector2(
						player.wheelContainer.Position.X,
						defaultWheelLocalPosition.Y - finalOffset
					);
					headTween.TweenProperty(player.wheelContainer, new NodePath("position"), finalWheelPosition, duration).SetTrans(transition);
					GD.Print($"KEK final force={notSpentJetpackAcceleration} launch tween with weight {notSpentWeight2} offset {finalOffset} {finalLegBodyPosition} {finalWheelPosition}");

					if (collidedWithCeiling && !isCollidedWithCeilingPrevious)
					{
						var pitchMin = 1f;
						var pitchMax = 10f;
						var hitPitch = pitchMin + (pitchMax - pitchMin) * (1 - notSpentWeight2);
						hitSound.PitchScale = hitPitch;
						hitSound.Play();
					}
				}

				var notSpentWeight = Mathf.Clamp(notSpentJetpackAcceleration / maxSpentJetpackAcceleration, 0, 1);

				var degrees = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(0), Mathf.DegToRad(-45), notSpentWeight));
				GD.Print($"not spent jetpack={notSpentJetpackAcceleration} weight={notSpentWeight} degrees={degrees}");
				player.headContainer.RotationDegrees = degrees;
			}
			else
			{
				GD.Print("clear jetpack acceleration inside collision");
				notSpentJetpackAcceleration = 0;
			}

			isCollidedWithCeilingPrevious = collidedWithCeiling;
		}
		else
		{
			GD.Print("clear jetpack acceleration outside collision");
			notSpentJetpackAcceleration = 0;
			headParticles.Emitting = false;
			headParticles.Lifetime = 170f;

			launchStraighteningTween();

			var playerBottom = player.GlobalPosition.Y + player.shape.Height / 2;
			var gameBoundsBottom = gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2;
			var distance = Mathf.Abs(playerBottom - gameBoundsBottom);
			if (distance > 3f)
			{
				isGrounded = false;
				if (!isGrounded && isGroundedPrevious)
				{
					if (rotationTween != null)
					{
						rotationTween.Stop();
					}
					rotationTween = CreateTween();

					GD.Print("launch straight tween");
					rotationTween.TweenProperty(player.legBodyHead, new NodePath("rotation_degrees"), 0f, airDuration).SetTrans(Tween.TransitionType.Spring);
				}
			}

			var lastAcceleration = Mathf.Abs(playerVelocity.Y - previousVelocty.Y);
			jetpackForce += playerMass * lastAcceleration * (float)delta;
			GD.Print($"LOL add {lastAcceleration} for {(float)delta}");
			GD.Print($"{jetpackForce} {lastAcceleration} {playerVelocity.Y} {previousVelocty.Y}");
			isCollidedWithCeilingPrevious = false;
		}

		if (!isCollidedWithCeilingPrevious)
		{
			grindSound.Stop();
		}


		if (!isGrounded)
		{
			playerVelocity += new Vector2(0f, gravityAcceleration) * (float)delta;
			var min = obstacleSpeed * 0.15f;
			var newVelocity = wheelAngularVelocity - (obstacleSpeed * 0.25f) * (float)delta;
			if (newVelocity > min)
			{
				wheelAngularVelocity = newVelocity;
			}
			else
			{
				wheelAngularVelocity = min;
			}
			//TODO: Can enable if I will figure out how to slow down and speed up the sound
			// if (!wheelRotationSound.Playing)
			// {
			// 	var weight = obstacleSpeed / wheelAngularVelocity;
			// 	wheelRotationSound.PitchScale = 0.3f;
			// 	wheelRotationSound.Play();
			// }
		}
		player.wheel.RotationDegrees += wheelAngularVelocity * (float)delta;
	}

	private void launchStraighteningTween()
	{
		var duration = 0.25f;
		headTween?.Stop();
		headTween = CreateTween();
		headTween.TweenProperty(player.headContainer, new NodePath("rotation_degrees"), 0f, duration);
		headTween.SetParallel();
		headTween.TweenProperty(player.legBody, new NodePath("position"), defaultLegBodyLocalPosition, duration);
		headTween.TweenProperty(player.wheelContainer, new NodePath("position"), defaultWheelLocalPosition, duration);
	}

	private async void TweenWheelBounce()
	{
		var wheelTween = CreateTween();
		var duration = 0.5f;
		var offset = 0.1f;
		wheelTween.TweenProperty(player.wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f) + new Vector2(offset, -offset * 2), duration).SetTrans(Tween.TransitionType.Bounce);
		wheelTween.SetParallel(true);
		var legBodyOffset = 75;
		wheelTween.TweenProperty(player.legBodyHead, new NodePath("position"), new Vector2(0f, legBodyOffset), duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);

		await ToSignal(wheelTween, "finished");
		var wheelTween2 = CreateTween();
		wheelTween2.TweenProperty(player.wheelContainer, new NodePath("scale"), new Vector2(1.0f, 1.0f), duration).SetTrans(Tween.TransitionType.Bounce);
		wheelTween2.SetParallel(true);
		wheelTween2.TweenProperty(player.legBodyHead, new NodePath("position"), new Vector2(0f, -legBodyOffset), duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);

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
		var allObjects = new List<Node2D>(obstacles).Concat(coins);
		var furtherstX = gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + 100f;
		if (allObjects.Count() > 1)
		{
			furtherstX = allObjects.Max(node => node.GlobalPosition.X);
		}
		var newX = furtherstX + 150f + randomSize.X;
		coinInstance.GlobalPosition = new Vector2(
			newX,
			randomY
		);
		var direction = 1;
		if (random.Next(1, 2) == 2)
		{
			direction = -1;
		}
		coinInstance.setConfig(random.Next(25, 75) / 100f, direction);

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

	private async void OnCoinOverlap(Coin coin, Node2D body)
	{
		if (body == player)
		{
			IncreaseScore(5);
			var randomPitch = new Random().Next(80, 130) / 100f;
			rewardSound.PitchScale = randomPitch;
			rewardSound.Play();
			var tween = CreateTween();
			var duration = 0.5f;
			tween.TweenProperty(coin.sprite, new NodePath("scale"), new Vector2(0.0f, 0.0f), duration);

			await ToSignal(tween, "finished");
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

		var size = instance.shape.Size;
		var randomY = random.Next(
			(int)(gameBounds.GlobalPosition.Y + size.Y / 2),
			(int)(gameBounds.GlobalPosition.Y + gameBounds.shape.Size.Y / 2 - size.Y / 2)
			);
		var allObjects = new List<Node2D>(obstacles).Concat(coins);
		var furtherstX = gameBounds.GlobalPosition.X + gameBounds.shape.Size.X / 2 + 100f;
		if (allObjects.Count() > 1)
		{
			furtherstX = allObjects.Max(node => node.GlobalPosition.X);
		}

		var newX = furtherstX + 150f + size.X;

		instance.GlobalPosition = new Vector2(
			newX,
			randomY
		);

		var direction = 1;
		if (random.Next(1, 2) == 2)
		{
			direction = -1;
		}
		instance.setConfig(random.Next(25, 75) / 100f, direction);

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

	private async void OnObstacleOverlap(Obstacle obstacle, Node2D body)
	{
		if (body == player)
		{
			UpdatePreviousScore(currentScore);
			if (currentScore > bestScore)
			{
				UpdateBestScore(currentScore);
			}

			SaveGame();
			if (!enemySound.Playing)
			{
				enemySound.Play();
			}
			var tween = CreateTween();
			var duration = 0.5f;
			tween.TweenProperty(obstacle.sprite, new NodePath("scale"), new Vector2(1.5f, 1.5f), duration).SetTrans(Tween.TransitionType.Back);

			await ToSignal(tween, "finished");
			InitGame();
		}
	}

	private void UpdateBestScore(float value)
	{
		bestScore = value;
		bestScoreLabel.Text = FormatScore(bestScore, "BEST");
	}

	private void UpdatePreviousScore(float value)
	{
		previousScore = value;
		previousSCoreLabel.Text = FormatScore(previousScore, "PREVIOUS");
	}

	private void IncreaseScore(float value)
	{
		SetScore(currentScore + value);
	}


	private void SetScore(float score)
	{
		currentScore = score;
		scoreLabel.Text = FormatScore(currentScore, "SCORE");
	}

	private string FormatScore(float value, string title)
	{
		return $"{title}: {Math.Round(value, 0)}";
	}

	private void SaveGame()
	{
		using var saveGameFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Write);
		var jsonString = Json.Stringify(new Godot.Collections.Dictionary<string, Variant>() {
			{bestScoreKey, bestScore},
			{previousScoreKey, previousScore}
		});
		saveGameFile.StoreLine(jsonString);
		GD.Print($"SAVE: SaveGame stored {bestScore} and {previousScore}");
	}

	private void LoadGame()
	{
		if (!FileAccess.FileExists(saveFilePath))
		{
			GD.PrintErr("can't load save game because save does not exist");
			return;
		}

		using var saveGameFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Read);
		var jsonString = saveGameFile.GetLine();
		var json = new Json();
		var parsedResult = json.Parse(jsonString);
		if (parsedResult != Error.Ok)
		{
			GD.PrintErr($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
			return;
		}

		var savedData = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)json.Data);

		UpdateBestScore((float)savedData[bestScoreKey]);
		UpdatePreviousScore((float)savedData[previousScoreKey]);
		GD.Print($"SAVE: LoadGame restored {bestScore} and {previousScore}");
	}

	private void UpdatePause(bool isPaused)
	{
		this.isPaused = isPaused;
		var pausables = new List<Node>() {
			player,
			background,
			scoreLabel
		}.Concat(coins).Concat(obstacles);
		foreach (Node pausable in pausables)
		{
			pausable.SetProcess(!isPaused);
		}
		if (isPaused)
		{
			gameMenu.Show();
		}
		else
		{
			gameMenu.Hide();
		}
	}

	private void UpdateDifficulty(bool isNewDifficultyEasy)
	{
		isEasyDifficulty = isNewDifficultyEasy;

		if (isNewDifficultyEasy)
		{
			obstacleSpeed = 250f;
		}
		else
		{
			obstacleSpeed = 450f;
		}
	}

	private void onStartGameClicked()
	{
		InitGame();
		UpdatePause(isPaused: false);
	}

	private void onQuitGameClicked()
	{
		GetTree().Quit();
	}

	private void onDifficultySwitched()
	{
		var isNewDifficultyEasy = !isEasyDifficulty;
		UpdateDifficulty(isNewDifficultyEasy);
	}

	private float gravityAcceleration = 9.8f * 20;
	private float jetpackAcceleration = 750f;
	private float obstacleSpeed = 450f;
	private float playerMass = 100f;
	private int maxCoins = 3;
	private int maxObstacles = 1;

	private string saveFilePath = "user://savegame.save";
	private string bestScoreKey = "best_score";
	private string previousScoreKey = "previous_score";

}
