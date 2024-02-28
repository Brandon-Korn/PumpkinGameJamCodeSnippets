using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TileManager : MonoBehaviour
{
    // The list of tiles active in the field.
    private List<Tile> tiles;
    // Whichever tile is currently active, if any.
    private Tile activeTile;
    // The scene's main camera.
    private Camera mainCam;
    // The current position of the mouse.
    Vector3 mousePos;

    [SerializeField]
    // The prefab that represents tiles.
    private GameObject tilePrefab;

    // The object representing the grass
    [SerializeField] private GameObject grassSprite;
    // The array of possible sprites for the grass to use.
    [SerializeField]
    private Sprite[] grassSprites;

    // The upgrade Manager Reference
    [SerializeField] private Upgrades UpgradeManger;

    // Start is called before the first frame update
    void Start()
    {
        bool isPlacingField;
        tiles = new List<Tile>();
        int tileId = 0;
        // Spawn verious grass and plantable tiles
        for (float x = -12.5f; x <= 8.5f; x++)
        {
            for (float y = 4.5f; y >= -4.5f; y--)
            {
                // Spawn dirt tiles at specific points on the scene
                if ((x >= -5.5f && x <= -4.5 || x >= -1.5f && x <= -0.5 || x >= 2.5f && x <= 3.5) && // tile x-values
                        y >= -2.5f && y <= 2.5f)                                                     // tile y-values
                    isPlacingField = true;
                // Spwan grass tiles everywhere else
                else
                    isPlacingField = false;

                if (isPlacingField)
                {
                    Tile tile = Instantiate(tilePrefab, transform).GetComponent<Tile>();
                    tile.transform.position = new Vector3(x, y, 0f);
                    tile.name = "Tile" + tileId;
                    tiles.Add(tile);
                    tileId++;
                }
                else
                {
                    Instantiate(grassSprite, new Vector3(x, y, 0f), new Quaternion(0, 0, 0, 0));
                    grassSprite.GetComponent<SpriteRenderer>().sprite = GetRandomGrassTile(0.9f);
                }
            }
        }

        mainCam = Camera.main;
        mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        UnlockRow(0);
        UnlockRow(1);
    }

    // Update is called once per frame
    void Update()
    {
        if (!PauseMenu.GameIsPaused)
        {
            // Whenever the mouse moves, check to see what the current highlighted tile is.
            if (mousePos != mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue()))
            {
                mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                // If there isn't an active tile, check to see if there should be one
                if (activeTile == null)
                {
                    // Optimization to only check half of the array at a time
                    if (mousePos.x > -1.0f)
                    {
                        for (int i = tiles.Count / 2; i < tiles.Count; i++)
                        {
                            if (tiles[i].IsUnlocked &&
                                tiles[i].OverlapsMouse(mousePos))
                            {
                                activeTile = tiles[i];
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < tiles.Count / 2; i++)
                        {
                            if (tiles[i].IsUnlocked &&
                                tiles[i].OverlapsMouse(mousePos))
                            {
                                activeTile = tiles[i];
                                break;
                            }
                        }
                    }
                }
                // Otherwise, check to see if this tile should no longer be considered active
                else if (!activeTile.OverlapsMouse(mousePos))
                {
                    activeTile = null;
                }
            }
        }
    }

    /// <summary>
    /// When an active tile is clicked, either plants the crop if it's empty or harvests it if it's ready
    /// </summary>
    /// <param name="context">The CallbackContext</param>
    public void OnTileClick(InputAction.CallbackContext context)
    {
        if (!PauseMenu.GameIsPaused && activeTile != null && context.performed) 
        {
            if (activeTile.TileState == TileState.Empty)
                activeTile.PlantCrop();
            else if (activeTile.TileState == TileState.Ready)
            {
                activeTile.HarvestCrop(true);

                // If the player harvests a pumpkin, increase the score
                if (!activeTile.IsJackOLantern) 
                    UpgradeManger.IncreasePumpkin(1);
                // If the player harvests a jack-o-lantern, increase the score by 5
                else
                    UpgradeManger.IncreasePumpkin(5);
            }
            else if (activeTile.TileState == TileState.Withered)
                activeTile.HarvestCrop(false);
        }
    }

    /// <summary>
    /// Gets a random tile that is either growing or fully-grown. If no tiles meet such criteria,
    /// <b>the method will return null.</b>
    /// </summary>
    /// <returns>A random growing or grown tile if one exists, null otherwise.</returns>
    public Tile GetRandomReadyTile()
    {
        List<Tile> readyTiles = new List<Tile>();
        foreach (Tile tile in tiles) 
        {
            if (tile.TileState != TileState.Empty && tile.TileState != TileState.Withered &&
                tile.IsUnlocked)
                readyTiles.Add(tile);
        }

        if (readyTiles.Count > 0)
            return readyTiles[Random.Range(0, readyTiles.Count)];

        return null;
    }

    public void DecreaseGrowTime()
    {
        foreach(Tile tile in tiles)
        {
            tile.DecreaseGrowTime();
        }
        
    }

    /// <summary>
    /// Gets a random grass sprite from the grassSprites array, favoring
    /// the first sprite in said array, and returns it.
    /// </summary>
    /// <param name="rateOfPlainGrass">The percentage chance that the tile will be
    /// plain grass.</param>
    /// <returns>A random grass sprite.</returns>
    private Sprite GetRandomGrassTile(float rateOfPlainGrass)
    {
        if (Random.value < rateOfPlainGrass)
            return grassSprites[0];
        return grassSprites[Random.Range(1, grassSprites.Length)];
    }

    /// <summary>
    /// Unlocks the row of tiles at the given row.
    /// </summary>
    /// <param name="rowNum">The row number to unlock.</param>
    public void UnlockRow(int rowNum)
    {
        for (int i = rowNum * 6; i < (rowNum + 1) * 6; ++i) 
        {
            tiles[i].UnlockTile();
        }
    }

    /// <summary>
    /// Gets every tile of state <paramref name="state"/> and returns a list
    /// of them.
    /// </summary>
    /// <param name="state">The tile state to check.</param>
    /// <returns>A list of tiles of the given state.</returns>
    public List<Tile> GetAllTilesOfState(TileState state)
    {
        List<Tile> tilesOfState = new List<Tile>();
        foreach (Tile tile in tiles)
        {
            if (tile.TileState == state && tile.IsUnlocked)
                tilesOfState.Add(tile);
        }

        if (tilesOfState.Count > 0)
            return tilesOfState;

        return null;
    }

    /// <summary>
    /// Gets a random tile of state <paramref name="state"/> and returns it.
    /// </summary>
    /// <param name="state">The tile state to check.</param>
    /// <returns>A random tile of the given state.</returns>
    public Tile GetRandomTileOfState(TileState state)
    {
        List<Tile> tiles = GetAllTilesOfState(state);
        if (tiles != null)
            return tiles[Random.Range(0, tiles.Count)];
        return null;
    }
}
