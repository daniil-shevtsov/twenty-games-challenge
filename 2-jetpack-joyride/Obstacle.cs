using Godot;
using System;
using System.Collections.Generic;

public partial class Obstacle : Area2D
{
	public CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	private Node2D sprite;
	private Vector2 obstacleSize;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("SpriteContainer");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
