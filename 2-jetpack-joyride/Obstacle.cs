using Godot;
using System;
using System.Collections.Generic;

public partial class Obstacle : Area2D
{
	public CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	private Polygon2D sprite;
	private Vector2 obstacleSize;

	public void SetSize(Vector2 size)
	{
		obstacleSize = size;
		shape.Size = obstacleSize;
		var newPoints = new List<Vector2>
		{
			new(-obstacleSize.X / 2, -obstacleSize.Y / 2),
			new(obstacleSize.X / 2, -obstacleSize.Y / 2),
			new(obstacleSize.X / 2, obstacleSize.Y / 2),
			new(-obstacleSize.X / 2, obstacleSize.Y / 2),
		};
		sprite.Polygon = newPoints.ToArray();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Polygon2D>("Polygon2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
