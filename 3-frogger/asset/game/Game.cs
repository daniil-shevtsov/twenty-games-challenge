using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

public partial class Game : Node2D
{
	private Player player;
	private Camera2D camera;
	private GameBounds bounds;

	private PackedScene tileScene;
	private PackedScene treeScene;

	private Dictionary<TileKey, Tile> tiles = new();

	private Dictionary<long, Tree> trees = new();

	private Vector2 tileSize;

	private bool isPlayerOnTree = false;

	private Animation walkAnimation;

	private bool isPaused = true;
	private string animationName = "walk2";

	static readonly int horizontalCount = 15;
	static readonly int verticalCount = 14;

	static readonly float treeSpeed = 150f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		camera = GetNode<Camera2D>("Camera2D");
		bounds = GetNode<GameBounds>("GameBounds");

		SetupEverything();
	}

	private async void SetupEverything()
	{
		walkAnimation = new Animation();

		var animationLibrary = player.animationPlayer.GetAnimationLibrary("");
		animationLibrary.AddAnimation(animationName, walkAnimation);
		walkAnimation.Length = 0.5f;

		var guiAnimation = player.animationPlayer.GetAnimation("walk");
		var track1 = walkAnimation.AddTrack(Animation.TrackType.Value);
		walkAnimation.TrackSetPath(track1, $"{player.sprite.GetPathTo(player.topLeftLegStart)}:rotation_degrees");
		walkAnimation.TrackInsertKey(track1, 0.0, -94f);
		walkAnimation.TrackInsertKey(track1, walkAnimation.Length, 0f);

		var track2 = walkAnimation.AddTrack(Animation.TrackType.Value, 1);
		walkAnimation.TrackSetPath(track2, $"{player.sprite.GetPathTo(player.topRightLegStart)}:rotation_degrees");
		walkAnimation.TrackInsertKey(track2, 0.0, 76f);
		walkAnimation.TrackInsertKey(track2, walkAnimation.Length, 0f);

		camera.GlobalPosition = bounds.GlobalPosition;

		tileScene = GD.Load<PackedScene>("res://asset/tile/tile.tscn");
		tileSize = new Vector2(
			bounds.shape.Size.X / horizontalCount,
			bounds.shape.Size.Y / verticalCount
		);
		InitTileGrid();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		treeScene = GD.Load<PackedScene>("res://asset/movable/tree.tscn");

		InitPlayer();


		SpawnTree();
		SpawnTree(offset: -1);

		isPaused = false;
	}

	private async void SpawnTree(int offset = 0)
	{
		var generatedId = offset;

		var tree = (Tree)treeScene.Instantiate();
		GetTree().CurrentScene.CallDeferred("add_child", tree);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		tree.Setup(
			newSize: new Vector2(tileSize.X * 4, tileSize.Y),
			id: generatedId
		);
		trees[tree.id] = tree;
		var bottomWaterTile = tiles.Values.Where(tile => tile.tileType == TileType.Water).MaxBy(tile => tile.GlobalPosition.Y);
		var treeInitialPosition = new Vector2(
			bounds.GlobalPosition.X + bounds.shape.Size.X / 2f + tree.shape.Size.X / 2f,
			tiles[bottomWaterTile.key.Copy(newY: bottomWaterTile.key.Y + offset)].GlobalPosition.Y
		);
		if (bottomWaterTile != null)
		{
			tree.GlobalPosition = treeInitialPosition;
		}
	}

	private void InitTileGrid()
	{
		var scene = GetTree().CurrentScene;

		for (int vertical = 0; vertical < verticalCount; vertical++)
		{
			for (int horizontal = 0; horizontal < horizontalCount; horizontal++)
			{
				var type = TileType.Ground;
				if (vertical > 1 && vertical < 7)
				{
					type = TileType.Water;
				}
				else if (vertical == 1)
				{
					if (horizontal == 0 || horizontal == 7 || horizontal == horizontalCount - 1)
					{
						type = TileType.Ground;
					}
					else
					{
						if (horizontal == horizontalCount / 2)
						{
							type = TileType.Ground;
						}
						if ((horizontal % 6 == 1) || (horizontal % 6 == 2))
						{
							type = TileType.Water;
						}
						else if ((horizontal % 6 == 3) || (horizontal % 6 == 4))
						{
							type = TileType.Ground;
						}
						else if ((horizontal % 6 == 5) || (horizontal % 6 == 6))
						{
							type = TileType.Ground;
						}
						else
						{
							type = TileType.Water;
						}
					}
				}

				SpawnTile(scene, x: horizontal, y: vertical, tileSize, type);
			}
		}
	}


	private void InitPlayer()
	{
		player.Setup(tileSize);
		var bottomCenterCoordinates = new Vector2(bounds.GlobalPosition.X, bounds.GlobalPosition.Y + bounds.shape.Size.Y / 2f);
		var bottomCenterKey = GetKeyForCoordinates(bottomCenterCoordinates);
		player.GlobalPosition = tiles[bottomCenterKey].GlobalPosition;
		isPlayerOnTree = false;
	}

	private async void SpawnTile(Node scene, int x, int y, Vector2 tileSize, TileType type)
	{
		var tile = (Tile)tileScene.Instantiate();
		scene.CallDeferred("add_child", tile);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var tileKey = new TileKey(x, y);
		tile.Setup(tileSize, tileKey, type);
		var size = tile.shape.Size;

		tile.GlobalPosition = new Vector2(
			bounds.GlobalPosition.X - bounds.shape.Size.X / 2f + size.X / 2f + size.X * x,
			bounds.GlobalPosition.Y - bounds.shape.Size.Y / 2f + size.Y / 2f + size.Y * y
		);


		tile.key = tileKey;
		tiles[tileKey] = tile;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!isPaused)
		{
			UpdatePlayerInput();
			UpdateTrees((float)delta);
			HandlePlayerState((float)delta);
		}
	}

	private void UpdatePlayerInput()
	{
		var horizontal = 0;
		var vertical = 0;

		if (Input.IsActionJustReleased("up"))
		{
			vertical = -1;
		}
		else if (Input.IsActionJustReleased("right"))
		{
			horizontal = 1;
		}
		else if (Input.IsActionJustReleased("down"))
		{
			vertical = 1;
		}
		else if (Input.IsActionJustReleased("left"))
		{
			horizontal = -1;
		}

		if (horizontal != 0 || vertical != 0)
		{
			UpdatePlayerTile(horizontal: horizontal, vertical: vertical);
		}
	}

	private void UpdateTrees(float delta)
	{
		var treeMoveAmount = treeSpeed * (float)delta;

		trees.Values.ToList().ForEach(tree =>
		{
			tree.GlobalPosition = new Vector2(tree.GlobalPosition.X - treeMoveAmount, tree.GlobalPosition.Y);

			if (tree.GlobalPosition.X + tree.shape.Size.X / 2f < bounds.GlobalPosition.X - bounds.shape.Size.X / 2f)
			{
				tree.GlobalPosition = new Vector2(
					bounds.GlobalPosition.X + bounds.shape.Size.X / 2f + tree.shape.Size.X / 2f,
					tree.GlobalPosition.Y
				);
			}
		});

		var allTreeTiles = trees.Values.ToList().SelectMany(tree =>
		{
			var treeLeftSide = tree.GlobalPosition.X - tree.shape.Size.X / 2f;
			var treeRightSide = tree.GlobalPosition.X + tree.shape.Size.X / 2f;

			var treeLeftSideTile = tiles[GetKeyForCoordinates(new Vector2(treeLeftSide, tree.GlobalPosition.Y))];
			var treeRightSideTile = tiles[GetKeyForCoordinates(new Vector2(treeRightSide, tree.GlobalPosition.Y))];

			var treeTiles = tiles.Values.ToList()
			.Where(tile =>
			{

				var tileLeftSide = tile.GlobalPosition.X - tile.shape.Size.X / 2f;
				var tileRightSide = tile.GlobalPosition.X + tile.shape.Size.X / 2f;
				return tile.GlobalPosition.Y == tree.GlobalPosition.Y
				&& (tileLeftSide >= treeLeftSideTile.GlobalPosition.X - treeLeftSideTile.shape.Size.X / 2f)
				&& (tileRightSide <= treeRightSideTile.GlobalPosition.X + treeLeftSideTile.shape.Size.X / 2f);
			});
			return treeTiles;
		}).Select(tile => tile.key).ToHashSet();



		tiles.Values.ToList().ForEach(tile =>
		{
			if (allTreeTiles.Contains(tile.key))
			{
				tile.UpdateType(TileType.Tree);
			}
			else if (tile.tileType != tile.originalTileType)
			{
				tile.ResetTypeToOriginal();
			}

		});

		if (isPlayerOnTree)
		{
			player.GlobalPosition = new Vector2(player.GlobalPosition.X - treeMoveAmount, player.GlobalPosition.Y);
		}
	}

	private async void UpdatePlayerTile(int horizontal, int vertical)
	{
		var currentTile = GetKeyForCoordinates(player.GlobalPosition);
		var newTile = tiles[ClampKey(currentTile.Copy(newX: currentTile.X + horizontal, newY: currentTile.Y + vertical))];
		//player.GlobalPosition = newTile.GlobalPosition;
		var tween = CreateTween();
		tween.TweenProperty(player, "global_position", newTile.GlobalPosition, 0.5f);

		isPlayerOnTree = newTile.tileType == TileType.Tree;

		player.animationPlayer.Play(animationName);
		// player.animationPlayer.GetAnimation("walk").Length = 0.25f;
		await ToSignal(player.animationPlayer, "animation_finished");
		player.animationPlayer.PlayBackwards(animationName);
	}

	private void HandlePlayerState(float delta)
	{
		var currentTile = tiles[GetKeyForCoordinates(player.GlobalPosition)];
		switch (currentTile.tileType)
		{
			case TileType.Ground:
				break;
			case TileType.Tree:
				break;
			case TileType.Water:
				HandlePlayerDying(currentTile.key);
				break;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsReleased())
		{
			var position = eventMouseButton.Position;

			var key = GetKeyForCoordinates(position);
			var newType = TileType.Ground;
			switch (tiles[key].tileType)
			{
				case TileType.Ground:
					newType = TileType.Water;
					break;
				case TileType.Water:
					newType = TileType.Ground;
					break;
			}
			tiles[key].UpdateType(newType);
		}
	}

	private void HandlePlayerDying(TileKey playerTileKey)
	{
		Respawn();
	}

	private void Respawn()
	{
		InitPlayer();
	}

	private TileKey GetKeyForCoordinates(Vector2 coordinates)
	{
		var epsilon = 0.0001f;
		int x = (int)Mathf.Clamp((coordinates.X - epsilon) / tileSize.X, 0, horizontalCount - 1);
		int y = (int)Mathf.Clamp((coordinates.Y - epsilon) / tileSize.Y, 0, verticalCount - 1);
		var tileKey = new TileKey(x, y);
		var finalKey = ClampKey(tileKey);
		var tile = tiles[finalKey];

		return finalKey;
	}

	private TileKey ClampKey(TileKey key)
	{
		return key.Copy(
			newX: Math.Clamp(key.X, 0, horizontalCount - 1),
			newY: Math.Clamp(key.Y, 0, verticalCount - 1)
		);
	}
}
