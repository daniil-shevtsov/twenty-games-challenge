using Godot;
using System;
using System.Collections.Generic;

public partial class Obstacle : Area2D
{
	public CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	private Sprite2D body;
	private Sprite2D spikes;
	private Sprite2D eyeball;
	private Area2D eyeballArea;
	private CircleShape2D eyeballShape;
	private Sprite2D pupil;

	private float rotationSpeed = 0f;
	private int rotationDirection = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("SpriteContainer");

		body = sprite.GetNode<Sprite2D>("Body");
		spikes = sprite.GetNode<Sprite2D>("Spikes");
		eyeball = sprite.GetNode<Sprite2D>("Eyeball");
		eyeballArea = sprite.GetNode<Area2D>("EyeballArea");
		eyeballShape = (CircleShape2D)eyeballArea.GetNode<CollisionShape2D>("CollisionShape2D").Shape;
		pupil = sprite.GetNode<Sprite2D>("Pupil");
	}

	public void setConfig(float rotationSpeed, int rotationDirection)
	{
		this.rotationSpeed = rotationSpeed * 2;
		this.rotationDirection = rotationDirection;
	}

	public void RotateBy(float angle)
	{
		body.RotationDegrees += rotationDirection * angle * rotationSpeed * 1.25f;
		spikes.RotationDegrees -= rotationDirection * angle * rotationSpeed * 1.25f;
	}

	public void LookAtPlayer(Vector2 playerPosition)
	{
		var eyeCenter = eyeballArea.GlobalPosition;
		var direction = (playerPosition - eyeCenter).Normalized();
		var scaledRadius = eyeballShape.Radius * sprite.Scale;
		var offset = 0.35f;
		var kek = scaledRadius * offset;
		var newCenter = new Vector2(
			eyeCenter.X + scaledRadius.X * direction.X - kek.X * direction.X,
			eyeCenter.Y + scaledRadius.Y * direction.Y - kek.Y * direction.Y
		);
		pupil.GlobalPosition = newCenter;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
