using Godot;
using System;

public partial class GameBounds : Area2D
{
	public RectangleShape2D shape;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		shape = (RectangleShape2D)GetNode<CollisionShape2D>("CollisionShape2D").Shape;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Vector2 Center()
	{
		return new Vector2(
				shape.Size.X / 2,
				shape.Size.Y / 2
			);
	}
}
