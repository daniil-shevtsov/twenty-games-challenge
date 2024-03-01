using Godot;
using System;

public partial class Ball : CharacterBody2D
{
	public CollisionShape2D collisionShape = null;
	public CircleShape2D shape = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (CircleShape2D)collisionShape.Shape;
	}

	public override void _Process(double delta)
	{
		this.QueueRedraw();
	}

	public override void _Draw()
	{
		var center = collisionShape.Position;
		float radius = shape.Radius;
		var color = new Color(1, 1, 1);
		DrawCircle(center, radius, color);
	}
}
