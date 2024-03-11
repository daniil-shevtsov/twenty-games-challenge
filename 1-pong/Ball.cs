using Godot;
using System;

public partial class Ball : CharacterBody2D
{
	public CollisionShape2D collisionShape = null;
	public CircleShape2D shape = null;
	public BallSprite sprite = null;
	private float drawRadius;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (CircleShape2D)collisionShape.Shape;

		sprite = GetNode<BallSprite>("BallSprite");
		drawRadius = shape.Radius;
		sprite.setRadius(drawRadius);
		// var drawLambda = () =>
		// {
		// 	var center = collisionShape.Position;
		// 	float radius = drawRadius;
		// 	var color = new Color(1, 1, 1);
		// 	DrawCircle(center, radius, color);
		// };
		// sprite.Connect(MethodName._Draw, Callable.From(drawLambda));
	}

	public override void _Process(double delta)
	{
		this.QueueRedraw();
	}

	public override void _Draw()
	{
		// var center = collisionShape.Position;
		// float radius = drawRadius;
		// var color = new Color(1, 1, 1);
		// DrawCircle(center, radius, color);
	}
}
