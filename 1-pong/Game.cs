using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private float playerSpeed = 600f;
	private float ballSpeed = 650;
	private float paddleOffset = 40f;

	private float lastPitch = 0f;
	private Vector2 ballDefaultDirection = new Vector2(0.5f, 0.5f);

	private AudioStreamPlayer collisionSound;
	private AudioStreamPlayer scoredSound;

	private bool isAiActivated = true;
	private AiState currentAiState = AiState.Idle;

	private Dictionary<string, Tuple<PlayerKey, PaddleDirection>> paddleInputs = new Dictionary<string, Tuple<PlayerKey, PaddleDirection>>
	{
			{ "left_player_up", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Left, PaddleDirection.Up) },
			{ "left_player_down", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Left, PaddleDirection.Down) },
			{ "right_player_up", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Right, PaddleDirection.Up) },
			{ "right_player_down", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Right, PaddleDirection.Down) },
	};

	private GameBounds gameBounds;

	private Dictionary<PlayerKey, Player> players = new Dictionary<PlayerKey, Player>();
	private Dictionary<PlayerKey, Score> scoreViews = new Dictionary<PlayerKey, Score>();
	private Dictionary<PlayerKey, int> scores = new Dictionary<PlayerKey, int>();

	private Ball ball;
	private Camera2D camera;
	private Vector2 ballVelocity;

	private PauseMenu menu;
	private bool isPaused = false;

	private int lastBallVelocityXSign = 1;

	private List<Node> pausables = new List<Node>();

	private bool shouldUpdatePhysics = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");
		menu = GetNode<PauseMenu>("PauseMenu");

		ball = GetNode<Ball>("Ball");
		camera = GetNode<Camera2D>("Camera2D");
		camera.GlobalPosition = gameBounds.Center();
		collisionSound = GetNode<AudioStreamPlayer>("CollisionSound");
		scoredSound = GetNode<AudioStreamPlayer>("ScoredSound");
		ball.GlobalPosition = gameBounds.Center();
		respawnBall();

		players = new Dictionary<PlayerKey, Player>() {
			{ PlayerKey.Left, GetNode<Player>("LeftPlayer") },
			{ PlayerKey.Right, GetNode<Player>("RightPlayer") }
		};

		scoreViews = new Dictionary<PlayerKey, Score>() {
			{ PlayerKey.Left, GetNode<Score>("LeftScore") },
			{ PlayerKey.Right, GetNode<Score>("RightScore") }
		};

		updatePause(isPaused: false);

		var scoreOffsetY = gameBounds.shape.Size.Y * 0.15f;
		var screenQuarterX = gameBounds.shape.Size.X / 4f;
		var leftScoreTopLeft = new Vector2(
				gameBounds.Center().X - screenQuarterX,
				scoreOffsetY
		);
		var rightScoreTopLeft = new Vector2(
				gameBounds.Center().X + screenQuarterX,
				scoreOffsetY
		);
		scoreViews[PlayerKey.Left].GlobalPosition = new Vector2(
				leftScoreTopLeft.X - scoreViews[PlayerKey.Left].Size.X / 2,
				leftScoreTopLeft.Y - scoreViews[PlayerKey.Left].Size.Y / 2
		);
		scoreViews[PlayerKey.Right].GlobalPosition = new Vector2(
				rightScoreTopLeft.X - scoreViews[PlayerKey.Right].Size.X / 2,
				rightScoreTopLeft.Y - scoreViews[PlayerKey.Right].Size.Y / 2
		);

		var divider = GetNode<Divider>("Divider");
		divider.GlobalPosition = gameBounds.Center();

		pausables.Add(ball);
		pausables.Add(players[PlayerKey.Left]);
		pausables.Add(players[PlayerKey.Right]);

		var startGameButton = (Button)FindChild("StartGame");
		startGameButton.Pressed += onStartClicked;
		var quitGameButton = (Button)FindChild("QuitGame");
		quitGameButton.Pressed += onQuitClicked;

		initGame();
	}

	private void initGame()
	{
		players[PlayerKey.Left].GlobalPosition = new Vector2(paddleOffset, gameBounds.Center().Y);
		players[PlayerKey.Right].GlobalPosition = new Vector2(gameBounds.shape.Size.X - paddleOffset, gameBounds.Center().Y);
		currentAiState = AiState.Idle;

		updateScore(PlayerKey.Left, 0);
		updateScore(PlayerKey.Right, 0);

		lastBallVelocityXSign = 1;
		respawnBall();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _PhysicsProcess(double delta)
	{
		GD.Print($"PhysicsProcess should update:{shouldUpdatePhysics}");

		if (shouldUpdatePhysics)
		{
			if (Input.IsActionJustPressed("pause"))
			{
				updatePause(!isPaused);
			}

			if (players[PlayerKey.Left].IsProcessing() || players[PlayerKey.Right].IsProcessing())
			{
				foreach (var entry in paddleInputs)
				{
					var (playerKey, pressedDirection) = entry.Value;
					var shouldHandlePlayerInput = playerKey != PlayerKey.Right! || !isAiActivated;
					if (Input.IsActionPressed(entry.Key) && shouldHandlePlayerInput)
					{
						handlePaddleInput(playerKey, pressedDirection, delta);
					}

				}
			}

			if (isAiActivated && players[PlayerKey.Right].IsProcessing())
			{
				var aiDecidedDirection = decideAiDirection();
				if (aiDecidedDirection != PaddleDirection.Stop)
				{
					handlePaddleInput(PlayerKey.Right, aiDecidedDirection, delta);
				}
			}

			if (ball.IsProcessing())
			{
				updateBall((float)delta);
			}
		}

	}

	private PaddleDirection decideAiDirection()
	{
		var currentBallPosition = ball.GlobalPosition;
		var currentPaddle = players[PlayerKey.Right];
		var currentPaddlePosition = currentPaddle.GlobalPosition;
		var currentBallVelocity = ballVelocity;

		var paddleTop = currentPaddlePosition.Y - currentPaddle.shape.Size.Y / 2;
		var paddleBottom = currentPaddlePosition.Y + currentPaddle.shape.Size.Y / 2;

		var difference = new Vector2(
			Mathf.Abs(currentPaddlePosition.X - currentBallPosition.X),
			Mathf.Abs(currentPaddlePosition.Y - currentBallPosition.Y)
		);
		var percentageOfPaddle = difference.Y / currentPaddle.shape.Size.Y;

		if (percentageOfPaddle > 2)
		{
			currentAiState = AiState.Seeking;
		}
		else if (ballVelocity.X < 0)
		{
			currentAiState = AiState.Idle;
		}

		var decision = PaddleDirection.Stop;
		if (currentAiState == AiState.Seeking && Mathf.Abs(currentBallPosition.Y - currentPaddlePosition.Y) > ball.shape.Radius)
		{
			if (currentBallPosition.Y < currentPaddlePosition.Y)
			{
				decision = PaddleDirection.Up;
			}
			else if (currentBallPosition.Y > currentPaddlePosition.Y)
			{
				decision = PaddleDirection.Down;
			}
		}
		else if (currentAiState == AiState.Idle && Mathf.Abs(players[PlayerKey.Left].GlobalPosition.Y - currentPaddlePosition.Y) > currentPaddle.shape.Size.Y * 0.5)
		{
			if (players[PlayerKey.Left].GlobalPosition.Y < currentPaddlePosition.Y)
			{
				decision = PaddleDirection.Up;
			}
			else if (players[PlayerKey.Left].GlobalPosition.Y > currentPaddlePosition.Y)
			{
				decision = PaddleDirection.Down;
			}
		}
		GD.Print($" decided {decision} {currentAiState} ball difference = {difference} percentage = {percentageOfPaddle}");

		return decision;
	}

	private async void screenShake(
		Vector2? offsetPower,
		Vector2? offsetDirection,
		float duration = 0.15f,
		float anglePower = 0f,
		int angleSign = 1
	)
	{
		if (offsetPower == null)
		{
			offsetPower = Vector2.Zero;
		}
		if (offsetDirection == null)
		{
			offsetDirection = Vector2.Zero;
		}

		var shakeDuration = duration;
		var cameraTween = CreateTween();
		var shakeOffset = (Vector2)offsetPower * (Vector2)offsetDirection;
		var shakeAngle = anglePower * angleSign;

		camera.IgnoreRotation = false;

		cameraTween.TweenProperty(camera, new NodePath("offset"), shakeOffset, shakeDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		cameraTween.SetParallel(true);
		if (anglePower > 0)
		{
			cameraTween.TweenProperty(camera, new NodePath("rotation_degrees"), shakeAngle, shakeDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			cameraTween.Chain().TweenProperty(camera, new NodePath("rotation_degrees"), -shakeAngle, shakeDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}

		cameraTween.Chain().TweenProperty(camera, new NodePath("offset"), -shakeOffset, shakeDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		await ToSignal(cameraTween, "finished");
	}

	private async void updateBall(float delta)
	{
		var ballCollision = ball.MoveAndCollide(ballVelocity * delta);
		if (ballCollision != null)
		{
			var oldDirection = ballVelocity.Normalized();
			var normal = ballCollision.GetNormal();
			var bounced = ballVelocity.Bounce(normal);
			var normalized = bounced.Normalized();
			changeBallDirection(normalized);
			var newDirection = ballVelocity.Normalized();
			GD.Print($"Collision: {oldDirection} {normal} {bounced} {normalized} {newDirection}");
			var minPitch = Mathf.Max(25, 100 - (int)(lastPitch * 100));
			var maxPitch = 100 + (int)(lastPitch * 100);
			var randomPitch = new Random().Next(minPitch, maxPitch) / 100f;
			lastPitch = randomPitch;

			GD.Print($"Pitch KEK LOL: {randomPitch}");
			collisionSound.PitchScale = randomPitch;
			collisionSound.Play();

			var reflected = normal;
			var vector = ballVelocity;
			var difference = reflected - vector;
			var normalForScale = normal.Abs();

			var newScale = new Vector2(1f, 1f) - 0.25f * normalForScale + 0.75f * new Vector2(-normalForScale.Y, normalForScale.X);

			var isHorizontalSquash = (normalForScale - new Vector2(0, 1)).Abs() < new Vector2(0.0001f, 0.0001f);
			var isVerticalSquash = (normalForScale - new Vector2(1, 0)).Abs() < new Vector2(0.001f, 0.001f);
			var translation = Vector2.Zero;
			if (isHorizontalSquash)
			{
				newScale = new Vector2(1.5f, 0.5f);
				translation = new Vector2(
					0,
					ball.shape.Radius * -Mathf.Sign(normal.Y)
				);
			}
			else if (isVerticalSquash)
			{
				newScale = new Vector2(0.5f, 1.5f);
				translation = new Vector2(
					ball.shape.Radius * -Mathf.Sign(normal.X),
					0
				);
			}

			GD.Print($"NORMAL_DEBUG normal = {normalForScale} new scale = {newScale}");

			var shakeDuration = 0.15f;
			var shakeOffset = new Vector2(5f, 5f);
			var shakeDirection = oldDirection;
			camera.IgnoreRotation = false;

			var collidedObject = ballCollision.GetCollider();
			if (collidedObject is Player)
			{
				var player = (Player)collidedObject;
				int angleSign;
				if (oldDirection.X > 0)
				{
					angleSign = -MathF.Sign(gameBounds.Center().Y - player.GlobalPosition.Y);
				}
				else
				{
					angleSign = MathF.Sign(gameBounds.Center().Y - player.GlobalPosition.Y);
				}
				var distancePercentage = Mathf.Abs(gameBounds.Center().Y - player.GlobalPosition.Y) / (gameBounds.shape.Size.Y / 2f);
				var shakeAnglePower = 5f * distancePercentage;

				screenShake(
					offsetPower: shakeOffset,
					offsetDirection: shakeDirection,
					duration: shakeDuration,
					anglePower: shakeAnglePower,
					angleSign: angleSign
				);
			}
			else
			{
				screenShake(offsetPower: shakeOffset, offsetDirection: shakeDirection, duration: shakeDuration);
			}

			ballVelocity = Vector2.Zero;
			var tween = CreateTween();
			var duration = 0.05f;

			shouldUpdatePhysics = false;
			ball.sprite.Rotation = 0;

			tween.TweenProperty(ball.sprite, new NodePath("scale"), newScale, duration).SetTrans(Tween.TransitionType.Bounce);
			tween.SetParallel(true);
			tween.TweenProperty(ball.sprite, new NodePath("position"), translation, duration).SetTrans(Tween.TransitionType.Bounce);
			await ToSignal(tween, "finished");
			var tween2 = CreateTween();
			tween2.TweenProperty(ball.sprite, new NodePath("scale"), new Vector2(1f, 1f), duration).SetTrans(Tween.TransitionType.Bounce);
			tween2.SetParallel(true);
			tween2.TweenProperty(ball.sprite, new NodePath("position"), Vector2.Zero, duration).SetTrans(Tween.TransitionType.Bounce);
			await ToSignal(tween2, "finished");
			shouldUpdatePhysics = true;
			changeBallDirection(normalized);
		}
		else
		{
			ball.sprite.Scale = new Vector2(1.2f, 0.75f);
			ball.sprite.Rotation = ballVelocity.Angle();
		}

		var ballLeft = ball.GlobalPosition.X - ball.shape.Radius;
		var ballRight = ball.GlobalPosition.X + ball.shape.Radius;
		if (ballRight <= 0)
		{
			// a little bit unintuitive that it's left part of screen but right player 
			handlePlayerScored(scoredPlayer: PlayerKey.Right);
		}
		else if (ballLeft >= gameBounds.shape.Size.X)
		{
			handlePlayerScored(scoredPlayer: PlayerKey.Left);
		}
	}

	private void handlePaddleInput(PlayerKey key, PaddleDirection pressedDirection, double delta)
	{
		var directionSign = 0;

		if (pressedDirection == PaddleDirection.Up)
		{
			directionSign = -1;

		}
		else if (pressedDirection == PaddleDirection.Down)
		{
			directionSign = 1;
		}

		if (directionSign != 0)
		{
			var player = players[key];
			var direction = new Vector2(0f, directionSign);

			var collision = player.MoveAndCollide(direction * playerSpeed * (float)delta);
		}
	}

	private async void handlePlayerScored(PlayerKey scoredPlayer)
	{
		var currentScore = scores[scoredPlayer];
		updateScore(scoredPlayer, ++currentScore);
		screenShake(
			offsetPower: new Vector2(0f, 30f),
			offsetDirection: new Vector2(0f, 1f),
			duration: 0.25f
		);
		scoredSound.Play();
		respawnBall();

	}

	private async void updateScore(PlayerKey player, int newScore)
	{
		scores[player] = newScore;

		var tween = CreateTween();
		var duration = 0.15f;
		var jumpOffset = new Vector2(0f, -15f);
		tween.TweenProperty(scoreViews[player], new NodePath("position"), jumpOffset, duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);
		tween.TweenProperty(scoreViews[player], new NodePath("position"), -jumpOffset, duration).AsRelative().SetTrans(Tween.TransitionType.Bounce);
		await ToSignal(tween, "finished");
		scoreViews[player].Text = scores[player].ToString();
	}

	private void respawnBall()
	{
		lastBallVelocityXSign = -lastBallVelocityXSign;
		ball.GlobalPosition = gameBounds.Center();
		changeBallDirection(new Vector2(ballDefaultDirection.X * lastBallVelocityXSign, ballDefaultDirection.Y));
	}

	private void changeBallDirection(Vector2 newDirection)
	{

		ballVelocity = newDirection * ballSpeed;
	}

	private void updatePause(bool isPaused)
	{
		this.isPaused = isPaused;
		foreach (Node pausable in pausables)
		{
			pausable.SetProcess(!isPaused);
		}
		if (isPaused)
		{
			menu.Show();
		}
		else
		{
			menu.Hide();
		}
	}

	private void onStartClicked()
	{
		initGame();
		updatePause(isPaused: false);
	}

	private void onQuitClicked()
	{
		GetTree().Quit();
	}


	enum PlayerKey
	{
		Left,
		Right
	}

	enum PaddleDirection
	{
		Up,
		Down,
		Stop
	}

	enum AiState
	{
		Idle,
		Seeking
	}
}
