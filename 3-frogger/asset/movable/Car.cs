using Godot;
using System;
using System.Collections.Generic;

public partial class Car : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;

	public List<CarPart> parts = new();

	public long id;
	public float speedMultiplier;
	public int lengthInTiles;
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
		float speedMultiplier
		)
	{
		this.id = id;
		this.speedMultiplier = speedMultiplier;
		this.lengthInTiles = lengthInTiles;

		for (int i = 0; i < lengthInTiles; ++i)
		{
			var carPart = (CarPart)carPartScene.Instantiate();
			AddChild(carPart);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			var generatedPartId = 100000 + 754 + i;
			carPart.Setup(generatedPartId);
			parts.Add(carPart);

			var totalWidth = carPart.shape.Size.X * lengthInTiles;
			carPart.Position = new Vector2(
				carPart.shape.Size.X / 2f + carPart.shape.Size.X * i - totalWidth / 2f,
				0f
			);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
