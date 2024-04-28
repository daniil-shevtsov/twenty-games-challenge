using Godot;
using System;
using System.Drawing;

public partial class Tile : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public ColorRect background;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		background = GetNode<ColorRect>("ColorRect");

		background.Size = shape.Size;
		background.Position = new Vector2(
			collisionShape.Position.X - background.Size.X / 2f,
			collisionShape.Position.Y - background.Size.Y / 2f
		);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
