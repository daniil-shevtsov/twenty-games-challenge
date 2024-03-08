using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private float playerSpeed = 600f;
	private float ballSpeed = 650;
	private float paddleOffset = 40f;
	private Vector2 ballDefaultDirection = new Vector2(0.5f, 0.5f);


	private bool isAiActivated = true;

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

	private int lastBallVelocityXSign = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gameBounds = GetNode<GameBounds>("GameBounds");

		var leftPlayer = GetNode<Player>("LeftPlayer");
		leftPlayer.GlobalPosition = new Vector2(paddleOffset, gameBounds.Center().Y);

		var rightPlayer = GetNode<Player>("RightPlayer");
		rightPlayer.GlobalPosition = new Vector2(gameBounds.shape.Size.X - paddleOffset, gameBounds.Center().Y);


		ball = GetNode<Ball>("Ball");
		ball.GlobalPosition = gameBounds.Center();
		respawnBall();

		players = new Dictionary<PlayerKey, Player>() {
			{ PlayerKey.Left, leftPlayer },
			{ PlayerKey.Right, rightPlayer }
		};

		scoreViews = new Dictionary<PlayerKey, Score>() {
			{ PlayerKey.Left, GetNode<Score>("LeftScore") },
			{ PlayerKey.Right, GetNode<Score>("RightScore") }
		};

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
		scores[PlayerKey.Left] = 0;
		scores[PlayerKey.Right] = 0;
		GD.Print($"KEK bounds center = {gameBounds.Center()} scores = {scoreViews[PlayerKey.Left].GlobalPosition} {scoreViews[PlayerKey.Right].GlobalPosition} gameBounds size = {gameBounds.shape.Size}");
		var divider = GetNode<Divider>("Divider");
		divider.GlobalPosition = gameBounds.Center();
		GD.Print($"KEK {gameBounds.shape.Size} {GetViewport().GetVisibleRect().Size} {GetViewportRect().Size}");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (var entry in paddleInputs)
		{
			var (playerKey, pressedDirection) = entry.Value;
			var shouldHandlePlayerInput = playerKey != PlayerKey.Right! || !isAiActivated;
			if (Input.IsActionPressed(entry.Key) && shouldHandlePlayerInput)
			{
				handlePaddleInput(playerKey, pressedDirection, delta);
			}
			else if (isAiActivated)
			{
				var aiDecidedDirection = decideAiDirection();
				if (aiDecidedDirection != PaddleDirection.Stop)
				{
					GD.Print($"decided: {aiDecidedDirection}");
					handlePaddleInput(PlayerKey.Right, aiDecidedDirection, delta);
				}
			}
		}

		updateBall((float)delta);
	}

	private PaddleDirection decideAiDirection()
	{
		var currentBallPosition = ball.GlobalPosition;
		var currentPaddle = players[PlayerKey.Right];
		var currentPaddlePosition = currentPaddle.GlobalPosition;
		var currentBallVelocity = ballVelocity;

		var paddleTop = currentPaddlePosition.Y - currentPaddle.shape.Size.Y / 2;
		var paddleBottom = currentPaddlePosition.Y + currentPaddle.shape.Size.Y / 2;

		var decision = PaddleDirection.Stop;
		if (currentBallPosition.Y < currentPaddlePosition.Y)
		{
			decision = PaddleDirection.Up;
		}
		else if (currentBallPosition.Y > currentPaddlePosition.Y)
		{
			decision = PaddleDirection.Down;
		}

		return decision;
	}

	private void updateBall(float delta)
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

	private void handlePlayerScored(PlayerKey scoredPlayer)
	{
		respawnBall();
		scores[scoredPlayer]++;
		scoreViews[scoredPlayer].Text = scores[scoredPlayer].ToString();
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
}
