using Godot;
using System;

public partial class Player : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Sprite2D body;
	public Sprite2D bottomLeftLegStart;
	public Sprite2D bottomLeftLegJoint;
	public Sprite2D bottomRightLegStart;
	public Sprite2D bottomRightLegJoint;
	public Sprite2D topLeftLegStart;
	public Sprite2D topLeftLegJoint;
	public Sprite2D topRightLegStart;
	public Sprite2D topRightLegJoint;
	public AnimationPlayer animationPlayer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("Sprite");

		animationPlayer = sprite.GetNode<AnimationPlayer>("AnimationPlayer");

		body = sprite.GetNode<Sprite2D>("Body");
		bottomLeftLegStart = (Sprite2D)sprite.FindChild("BottomLeftLegStart");
		bottomLeftLegJoint = (Sprite2D)sprite.FindChild("BottomLeftLegJoint");
		bottomRightLegStart = (Sprite2D)sprite.FindChild("BottomRightLegStart");
		bottomRightLegJoint = (Sprite2D)sprite.FindChild("BottomRightLegJoint");
		topLeftLegStart = (Sprite2D)sprite.FindChild("TopLeftLegStart");
		topLeftLegJoint = (Sprite2D)sprite.FindChild("TopLeftLegJoint");
		topRightLegStart = (Sprite2D)sprite.FindChild("TopRightLegStart");
		topRightLegJoint = (Sprite2D)sprite.FindChild("TopRightLegJoint");
	}

	public void Setup(Vector2 newSize)
	{
		shape.Size = newSize;
		//var newScale = shape.Size / sprite.Texture.GetSize();
		//sprite.Scale = newScale;
		sprite.Position = new Vector2(
			collisionShape.Position.X/*  - shape.Size.X / 2f */,
			collisionShape.Position.Y/*  - shape.Size.Y / 2f */
		);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
