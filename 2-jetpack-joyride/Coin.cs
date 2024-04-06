using Godot;
using System;

public partial class Coin : StaticBody2D
{
	public CollisionShape2D collisionShape = null;
	public RectangleShape2D shape = null;
	private Polygon2D sprite = null;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print($"KEK coin ready");
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Polygon2D>("Polygon2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
