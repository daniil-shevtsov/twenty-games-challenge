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
	private Vector2 ballVelocity;

	private PauseMenu menu;
	private bool isPaused = false;

	private int lastBallVelocityXSign = 1;

	private List<Node> pausables = new List<Node>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");
		menu = GetNode<PauseMenu>("PauseMenu");

		ball = GetNode<Ball>("Ball");
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
		GD.Print("PhysicsProcess}");
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

		// if (difference.X > gameBounds.shape.Size.X / 4)
		// {
		// 	if (percentageOfPaddle > 1)
		// 	{
		// 		if (currentBallPosition.Y < currentPaddlePosition.Y)
		// 		{
		// 			decision = PaddleDirection.Up;
		// 		}
		// 		else if (currentBallPosition.Y > currentPaddlePosition.Y)
		// 		{
		// 			decision = PaddleDirection.Down;
		// 		}
		// 	}
		// }
		// else
		// {
		// 	if (percentageOfPaddle > 0.75)
		// 	{
		// 		if (currentBallPosition.Y < currentPaddlePosition.Y)
		// 		{
		// 			decision = PaddleDirection.Up;
		// 		}
		// 		else if (currentBallPosition.Y > currentPaddlePosition.Y)
		// 		{
		// 			decision = PaddleDirection.Down;
		// 		}
		// 	}
		// }


		return decision;
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
			// ballVelocity = Vector2.Zero;
			GD.Print($"Pitch KEK LOL: {randomPitch}");
			collisionSound.PitchScale = randomPitch;
			collisionSound.Play();

			// var tween = CreateTween();
			// tween.TweenProperty(ball, new NodePath("scale"), new Vector2(1.5f, 0.5f), 0.15f);
			// await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
			// var tween2 = CreateTween();
			// tween2.TweenProperty(ball, new NodePath("scale"), new Vector2(1f, 1f), 0.15f);

			changeBallDirection(normalized);
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

	private async void BallTween()
	{
		var tween = CreateTween();
		// var squashed = ball.shape.Radius;
		// tween.interpolate_property(self, "transform", transform, targetTransform, .5, Tween.TRANS_LINEAR, Tween.EASE_IN);
		tween.TweenProperty(ball, new NodePath("scale"), new Vector2(1.5f, 0.5f), 0.15f);
		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		var tween2 = CreateTween();
		// GD.Print("KEEEEEEEEEEEEEEEK");
		// await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		//AudioManager.respawnSfx.Play();
		tween2.TweenProperty(ball, new NodePath("scale"), new Vector2(1f, 1f), 0.15f);
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

	private void handlePlayerScored(PlayerKey scoredPlayer)
	{
		respawnBall();
		var currentScore = scores[scoredPlayer];
		updateScore(scoredPlayer, ++currentScore);
		scoredSound.Play();
	}

	private void updateScore(PlayerKey player, int newScore)
	{
		scores[player] = newScore;
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
		// ball.Rotation = ballVelocity.Angle();
		// var newScale = new Vector2(1f, 1f) + 0.50f * newDirection.Normalized();
		// var newScale = new Vector2(2f, 1f);
		// GD.Print($"SCALE_DEBUG current scale {ball.Scale} new scale {newScale}");
		// ball.Scale = newScale;
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
