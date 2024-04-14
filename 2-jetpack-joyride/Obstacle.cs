using Godot;
using System;
using System.Collections.Generic;

public partial class Obstacle : Area2D
{
	public CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	private Node2D sprite;
	private Sprite2D body;
	private Sprite2D spikes;
	private Sprite2D eyeball;
	private CircleShape2D eyeballArea;
	private Sprite2D pupil;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("SpriteContainer");

		body = sprite.GetNode<Sprite2D>("Body");
		spikes = sprite.GetNode<Sprite2D>("Spikes");
		eyeball = sprite.GetNode<Sprite2D>("Eyeball");
		eyeballArea = (CircleShape2D)sprite.GetNode<Area2D>("EyeballArea").GetNode<CollisionShape2D>("CollisionShape2D").Shape;
		pupil = sprite.GetNode<Sprite2D>("Pupil");
	}

	public void RotateBy(float angle)
	{
		body.RotationDegrees += angle * 1.25f;
		spikes.RotationDegrees -= angle;
	}

	public void LookAtPlayer(Vector2 playerPosition)
	{
		var direction = (playerPosition - GlobalPosition).Normalized();
		var newCenter = new Vector2(
			GlobalPosition.X + eyeballArea.Radius * direction.X,
			GlobalPosition.Y + eyeballArea.Radius * direction.Y
		);
		pupil.GlobalPosition = newCenter;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
