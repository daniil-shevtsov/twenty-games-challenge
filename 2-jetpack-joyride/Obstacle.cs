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
	private Sprite2D testSprite;

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
		testSprite = sprite.GetNode<Sprite2D>("TestSprite");
	}

	public void RotateBy(float angle)
	{
		body.RotationDegrees += angle * 1.25f;
		spikes.RotationDegrees -= angle;
	}

	public void LookAtPlayer(Vector2 playerPosition)
	{
		GD.Print($"eyeball texture size = {eyeball.Texture.GetSize().X} scaled: {eyeball.Texture.GetSize().X * sprite.Scale.X} area radius = {eyeballArea.Radius} scaled = {eyeballArea.Radius * sprite.Scale.X}");
		var direction = (playerPosition - GlobalPosition).Normalized();
		var scaledRadius = eyeballArea.Radius * sprite.Scale;
		var offset = 0.25f;
		var kek = scaledRadius * offset;
		var newCenter = new Vector2(
			GlobalPosition.X + scaledRadius.X * direction.X - kek.X * direction.X,
			GlobalPosition.Y + scaledRadius.Y * direction.Y - kek.Y * direction.Y
		);
		pupil.GlobalPosition = newCenter;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
