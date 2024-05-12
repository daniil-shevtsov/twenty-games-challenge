using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

	private long? playerTreeId = null;

	private Animation walkAnimation;

	private Tween playerMoveTween = null;

	private bool isPaused = true;
	private string animationName = "walk2";
	private float tileWalkDuration = 0.25f;

	static readonly int horizontalCount = 15;
	static readonly int verticalCount = 14;

	static readonly float treeSpeed = 50f;

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
		SetupAnimation();

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

		var minOffset = 0;
		var maxOffset = 4;
		for (var i = minOffset; i <= maxOffset; ++i)
		{
			var random = new Random();
			var speedMultiplier = random.Next(75, 125) / 100f;
			var tileCount = random.Next(2, 5);
			for (var j = 0; j < 3; ++j)
			{
				SpawnTree(offset: -i, count: j, speedMultiplier: speedMultiplier, tileCount: tileCount);
			}
		}

		isPaused = false;
	}


	private async void SpawnTree(int offset, int count, float speedMultiplier, int tileCount)
	{
		var generatedId = offset * 10000 + count;

		var tree = (Tree)treeScene.Instantiate();
		GetTree().CurrentScene.CallDeferred("add_child", tree);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var treeSize = new Vector2(tileSize.X * tileCount, tileSize.Y);
		tree.Setup(
			newSize: treeSize,
			id: generatedId,
			speedMultiplier: speedMultiplier
		);
		trees[tree.id] = tree;
		var bottomWaterTile = tiles.Values.Where(tile => tile.tileType == TileType.Water).MaxBy(tile => tile.GlobalPosition.Y);
		var defaultHorizontalPosition = bounds.GlobalPosition.X + bounds.shape.Size.X / 2f + tree.shape.Size.X / 2f;
		var distanceBetweenTrees = 150f;
		var horizontalPosition = defaultHorizontalPosition + treeSize.X * count + (distanceBetweenTrees * count);
		var treeInitialPosition = new Vector2(
			horizontalPosition,
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
		playerTreeId = null;
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
			ChoosePlayerTileByInput(horizontal: horizontal, vertical: vertical);
		}
	}

	private TileKey[] GetTreeTiles(Tree tree)
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
		})
		.Select(tile => tile.key);

		return treeTiles.ToArray();
	}

	private void UpdateTrees(float delta)
	{

		trees.Values.ToList().ForEach(tree =>
		{
			var treeMoveAmount = calculateTreeMovementAmount(tree, (float)delta);

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
			return GetTreeTiles(tree);
		}).ToHashSet();


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

		if (playerTreeId != null)
		{
			var playerTree = trees[(long)playerTreeId];
			var treeMoveAmount = calculateTreeMovementAmount(playerTree, (float)delta);
			player.GlobalPosition = new Vector2(player.GlobalPosition.X - treeMoveAmount, player.GlobalPosition.Y);
		}
	}

	private async void UpdatePlayerTile(Tile newTile)
	{
		var currentTile = GetKeyForCoordinates(player.GlobalPosition);

		if (playerMoveTween != null)
		{
			playerMoveTween.Stop();
		}
		//TODO: Don't have time to figure out how to make tween work while you are being moved by the tree
		if (playerTreeId != null)
		{
			player.GlobalPosition = newTile.GlobalPosition;
		}
		else
		{
			playerMoveTween = CreateTween();
			playerMoveTween.TweenProperty(player, "global_position", newTile.GlobalPosition, tileWalkDuration);
		}

		if (newTile.tileType == TileType.Tree)
		{

			playerTreeId = trees.First(treeKeypair => GetTreeTiles(treeKeypair.Value).Contains(newTile.key)).Value.id;
		}
		else
		{
			playerTreeId = null;
		}

		player.animationPlayer.Play(animationName);
		await ToSignal(player.animationPlayer, "animation_finished");
		player.animationPlayer.PlayBackwards(animationName);
	}

	private void ChoosePlayerTileByInput(int horizontal, int vertical)
	{
		var currentTile = GetKeyForCoordinates(player.GlobalPosition);
		var newTile = tiles[ClampKey(currentTile.Copy(newX: currentTile.X + horizontal, newY: currentTile.Y + vertical))];

		UpdatePlayerTile(newTile);

		if (horizontal != 0)
		{
			player.sprite.RotationDegrees = 90f * horizontal;
		}
		else if (vertical == 1)
		{
			player.sprite.RotationDegrees = 180f;
		}
		else
		{
			player.sprite.RotationDegrees = 0f;
		}
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
				Tile leftTile = null;
				if (currentTile.key.X > 0)
				{
					//leftTile = tiles[new TileKey(currentTile.key.X - 1, currentTile.key.Y)];
				}

				Tile rightTile = null;
				if (currentTile.key.X < tiles.Count - 1)
				{
					//rightTile = tiles[new TileKey(currentTile.key.X - 1, currentTile.key.Y)];
				}

				if (rightTile != null && rightTile.tileType == TileType.Tree)
				{
					UpdatePlayerTile(rightTile);
				}
				else if (leftTile != null && leftTile.tileType == TileType.Tree)
				{
					UpdatePlayerTile(leftTile);
				}
				else
				{
					GD.Print($"Player is dying because {ObjectToString(leftTile?.key)}:{leftTile?.tileType} {ObjectToString(currentTile.key)}:{currentTile.tileType} {ObjectToString(rightTile?.key)}:{rightTile?.tileType}");
					HandlePlayerDying(currentTile.key);
				}

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

	private float calculateTreeMovementAmount(Tree tree, float delta) => treeSpeed * tree.speedMultiplier * delta;

	private void HandlePlayerDying(TileKey playerTileKey)
	{
		Respawn();
	}

	private void Respawn()
	{
		InitPlayer();
	}

	private void SetupAnimation()
	{
		walkAnimation = new Animation();

		var animationLibrary = player.animationPlayer.GetAnimationLibrary("");
		animationLibrary.AddAnimation(animationName, walkAnimation);
		walkAnimation.Length = tileWalkDuration / 2f;

		var initialPlayerKeyFrame = new PlayerKeyFrame(bodyParts: new List<BodyPartKeyFrame>() {
				new(player.topLeftLegStart, 0f, -94f),
				new(player.topLeftLegJoint, 0f, 104f),
				new(player.topRightLegStart, 0f, 76f),
				new(player.topRightLegJoint, 0f, -75f),
				new(player.bottomLeftLegStart, 0f, 0f),
				new(player.bottomLeftLegJoint, 0f, 0f),
				new(player.bottomRightLegStart, 0f, 0f),
				new(player.bottomRightLegJoint, 0f, 0f)
			}.ToDictionary(keyFrame => keyFrame.part.id, keyFrame => keyFrame)
		);
		var finalPlayerKeyFrame = new PlayerKeyFrame(bodyParts: new List<BodyPartKeyFrame>() {
				new(player.topLeftLegStart, walkAnimation.Length, 0f),
				new(player.topLeftLegJoint, walkAnimation.Length, -13f),
				new(player.topRightLegStart, walkAnimation.Length, -13f),
				new(player.topRightLegJoint, walkAnimation.Length, 26f),
				new(player.bottomLeftLegStart, walkAnimation.Length, -96),
				new(player.bottomLeftLegJoint, walkAnimation.Length, 108f),
				new(player.bottomRightLegStart, walkAnimation.Length, 87f),
				new(player.bottomRightLegJoint, walkAnimation.Length, -96f)
			}.ToDictionary(keyFrame => keyFrame.part.id, keyFrame => keyFrame)
		);

		var playerKeyFrames = new List<PlayerKeyFrame>() {
			initialPlayerKeyFrame,
			finalPlayerKeyFrame
		};

		var addedTracks = initialPlayerKeyFrame.bodyParts.Select((bodyPartKeyFramePair) =>
		{
			var bodyPartKey = bodyPartKeyFramePair.Key;
			var bodyPartKeyFrame = bodyPartKeyFramePair.Value;
			var path = $"{player.sprite.GetPathTo(bodyPartKeyFrame.part)}:rotation_degrees";

			var trackIndex = walkAnimation.AddTrack(Animation.TrackType.Value);
			walkAnimation.TrackSetPath(trackIndex, path);
			return new KeyValuePair<string, AnimationTrack>(bodyPartKey, new AnimationTrack(trackIndex, path));
		}).ToDictionary(pair => pair.Key, pair => pair.Value);

		playerKeyFrames.ForEach(playerKeyFrame =>
		{
			playerKeyFrame.bodyParts.ToList().ForEach(bodyPartKeyFramePair =>
			{
				var bodyPartKey = bodyPartKeyFramePair.Key;
				var bodyPartKeyFrame = bodyPartKeyFramePair.Value;
				var trackIndex = addedTracks[bodyPartKey].trackIndex;
				var toInsert = bodyPartKeyFrame;
				walkAnimation.TrackInsertKey(trackIndex, toInsert.time, toInsert.value);
			});
		});
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

	private string ObjectToString(Object myObj)
	{
		if (myObj == null)
		{
			return "null";
		}

		var stringBuilder = new StringBuilder();
		stringBuilder.Append("{");
		foreach (var prop in myObj.GetType().GetProperties())
		{
			stringBuilder.Append(" " + prop.Name + ": " + prop.GetValue(myObj, null));
		}

		foreach (var field in myObj.GetType().GetFields())
		{
			stringBuilder.Append(" " + field.Name + ": " + field.GetValue(myObj));
		}
		stringBuilder.Append(" }");
		return stringBuilder.ToString();
	}

	struct BodyPartKeyFrame
	{
		public BodyPart part;
		public float time;
		public float value;

		public BodyPartKeyFrame(BodyPart part, float time, float value)
		{
			this.part = part;
			this.time = time;
			this.value = value;
		}
	}
	struct PlayerKeyFrame
	{
		public Dictionary<string, BodyPartKeyFrame> bodyParts;

		public PlayerKeyFrame(Dictionary<string, BodyPartKeyFrame> bodyParts)
		{
			this.bodyParts = bodyParts;
		}
	}

	struct AnimationTrack
	{
		public int trackIndex;
		public string trackPath;
		public AnimationTrack(int trackIndex, string trackPath)
		{
			this.trackIndex = trackIndex;
			this.trackPath = trackPath;
		}
	}
}
