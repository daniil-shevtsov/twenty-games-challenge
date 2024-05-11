using Godot;
using System;

public partial class BodyPart : Sprite2D
{
	public string id;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	public void Setup(string id)
	{
		this.id = id;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
