using Godot;
using System;

public partial class GameBounds : Area2D
{

	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		GD.Print($"KEK bounds {collisionShape} {shape}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
