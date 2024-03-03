using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private float playerSpeed = 300f;
	private float paddleOffset = 40f;

	private Dictionary<string, Tuple<PlayerKey, PaddleDirection>> paddleInputs = new Dictionary<string, Tuple<PlayerKey, PaddleDirection>>
	{
			{ "left_player_up", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Left, PaddleDirection.Up) },
			{ "left_player_down", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Left, PaddleDirection.Down) },
			{ "right_player_up", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Right, PaddleDirection.Up) },
			{ "right_player_down", new Tuple<PlayerKey, PaddleDirection>(PlayerKey.Right, PaddleDirection.Down) },
	};

	private GameBounds gameBounds;

	private Dictionary<PlayerKey, Player> players = new Dictionary<PlayerKey, Player>();
	private Dictionary<PlayerKey, Score> scores = new Dictionary<PlayerKey, Score>();
	private Ball ball;
	private Vector2 ballVelocity = new Vector2(0.5f, 0.5f) * 300f;

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

		scores = new Dictionary<PlayerKey, Score>() {
			{ PlayerKey.Left, GetNode<Score>("LeftScore") },
			{ PlayerKey.Right, GetNode<Score>("RightScore") }
		};

		var scoreOffsetY = gameBounds.shape.Size.Y * 0.15f;
		var screenQuarterX = gameBounds.shape.Size.X / 4f;
		var leftScoreTopLeft = new Vector2(
				screenQuarterX,
				scoreOffsetY
		);
		var rightScoreTopLeft = new Vector2(
				gameBounds.shape.Size.X - screenQuarterX,
				scoreOffsetY
		);
		scores[PlayerKey.Left].GlobalPosition = new Vector2(
				leftScoreTopLeft.X - scores[PlayerKey.Left].Size.X / 2,
				leftScoreTopLeft.Y - scores[PlayerKey.Left].Size.Y / 2
		);
		scores[PlayerKey.Right].GlobalPosition = new Vector2(
				rightScoreTopLeft.X - scores[PlayerKey.Right].Size.X / 2,
				rightScoreTopLeft.Y - scores[PlayerKey.Right].Size.Y / 2
		);
		GD.Print($"KEK bounds center = {gameBounds.Center()} scores = {scores[PlayerKey.Left].GlobalPosition} {scores[PlayerKey.Right].GlobalPosition} gameBounds size = {gameBounds.shape.Size}");
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
			if (Input.IsActionPressed(entry.Key))
			{
				handlePaddleInput(entry.Value.Item1, entry.Value.Item2, delta);
			}
		}


		var ballCollision = ball.MoveAndCollide(ballVelocity * (float)delta);
		if (ballCollision != null)
		{
			GD.Print("ball Collision");
			ballVelocity = ballVelocity.Bounce(ballCollision.GetNormal());
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

	private void handlePaddleInput(PlayerKey key, PaddleDirection direction, double delta)
	{
		var directionSign = 0;

		if (direction == PaddleDirection.Up)
		{
			directionSign = -1;

		}
		else if (direction == PaddleDirection.Down)
		{
			directionSign = 1;
		}

		if (directionSign != 0)
		{
			var player = players[key];
			var currentPosition = player.GlobalPosition;
			var newPosition = new Vector2(
				currentPosition.X,
				currentPosition.Y + directionSign * playerSpeed * (float)delta
			);
			player.GlobalPosition = newPosition;
		}
	}

	private void handlePlayerScored(PlayerKey scoredPlayer)
	{
		respawnBall();
	}

	private void respawnBall()
	{
		lastBallVelocityXSign = -lastBallVelocityXSign;
		ball.GlobalPosition = gameBounds.Center();
		ballVelocity = new Vector2(0.5f * lastBallVelocityXSign, 0.5f) * 300f;
	}


	enum PlayerKey
	{
		Left,
		Right
	}

	enum PaddleDirection
	{
		Up,
		Down
	}
}
