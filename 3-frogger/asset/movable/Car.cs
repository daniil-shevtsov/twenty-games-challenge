using Godot;
using System;
using System.Collections.Generic;

public partial class Car : Area2D
{
	// [Signal]
	// public delegate void CarMoveEventHandler(float amount);

	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;

	public List<CarPart> parts = new();

	public long id;
	public float speedMultiplier;
	public int lengthInTiles;

	private Tween tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape.Duplicate();
		collisionShape.Shape = shape;
	}

	public async void Setup(
		int lengthInTiles,
		PackedScene carPartScene,
		long id,
		float speedMultiplier,
		bool isDirectionRight
		)
	{
		this.id = id;
		this.speedMultiplier = speedMultiplier;
		this.lengthInTiles = lengthInTiles;

		int directionMultiplier = 1;
		if (isDirectionRight)
		{
			Scale = new Vector2(
				-Scale.X,
				Scale.Y
			);
			directionMultiplier = -1;
		}

		var animationMultiplier = new Random().NextInt64(0, 25) / 100f;

		shape.Size = Vector2.Zero;
		for (int i = 0; i < lengthInTiles; ++i)
		{
			var carPart = (CarPart)carPartScene.Instantiate();
			AddChild(carPart);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			var generatedPartId = 100000 + 754 + i;
			carPart.Setup(
				generatedPartId,
			speedMultiplier: 4 / lengthInTiles * directionMultiplier
			);

			parts.Add(carPart);

			var totalWidth = carPart.shape.Size.X * lengthInTiles;
			carPart.Position = new Vector2(
				carPart.shape.Size.X / 2f + carPart.shape.Size.X * i - totalWidth / 2f,
				0f
			);
			carPart.animationPlayer.SpeedScale = 1.0f + animationMultiplier;
			shape.Size = new Vector2(shape.Size.X + carPart.shape.Size.X, carPart.shape.Size.Y);
			collisionShape.Position = Vector2.Zero;
			carPart.CarMoved += HandleCarMoveEventHandler;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async void HandleCarMoveEventHandler(float scaledAmount)
	{
		tween = CreateTween();
		var newPosition = new Vector2(
			GlobalPosition.X - scaledAmount,
			GlobalPosition.Y
		);
		tween.TweenProperty(this, "global_position", newPosition, 0.2f);
	}
}
