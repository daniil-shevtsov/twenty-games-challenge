using Godot;
using System;
using System.Collections.Generic;

public partial class Player : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public BodyPart body;
	public BodyPart bottomLeftLegStart;
	public BodyPart bottomLeftLegJoint;
	public BodyPart bottomRightLegStart;
	public BodyPart bottomRightLegJoint;
	public BodyPart topLeftLegStart;
	public BodyPart topLeftLegJoint;
	public BodyPart topRightLegStart;
	public BodyPart topRightLegJoint;

	public List<BodyPart> bodyParts
	{
		get
		{
			return new List<BodyPart>() {
				body,
				bottomLeftLegStart,
				bottomLeftLegJoint,
				bottomRightLegStart,
				bottomRightLegJoint,
				topLeftLegStart,
				topLeftLegJoint,
				topRightLegStart,
				topRightLegJoint
			};
		}
	}
	public AnimationPlayer animationPlayer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("Sprite");

		animationPlayer = sprite.GetNode<AnimationPlayer>("AnimationPlayer");

		body = sprite.GetNode<BodyPart>("Body");
		bottomLeftLegStart = (BodyPart)sprite.FindChild("BottomLeftLegStart");
		bottomLeftLegJoint = (BodyPart)sprite.FindChild("BottomLeftLegJoint");
		bottomRightLegStart = (BodyPart)sprite.FindChild("BottomRightLegStart");
		bottomRightLegJoint = (BodyPart)sprite.FindChild("BottomRightLegJoint");
		topLeftLegStart = (BodyPart)sprite.FindChild("TopLeftLegStart");
		topLeftLegJoint = (BodyPart)sprite.FindChild("TopLeftLegJoint");
		topRightLegStart = (BodyPart)sprite.FindChild("TopRightLegStart");
		topRightLegJoint = (BodyPart)sprite.FindChild("TopRightLegJoint");

		bodyParts.ForEach(bodyPart =>
		{
			var nameOfTextureFile = bodyPart.Texture.ResourcePath[(bodyPart.Texture.ResourcePath.LastIndexOf("/") + 1)..bodyPart.Texture.ResourcePath.LastIndexOf(".")];
			bodyPart.Setup(id: nameOfTextureFile);
			GD.Print($"Set id {bodyPart.id}");
		});
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
