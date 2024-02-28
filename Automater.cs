using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum automatorType
{
    Harvester, 
    Planter
}

public class Automater : MonoBehaviour
{
    // ~~~ FIELDS ~~~
    [SerializeField]
    // The current TileManager.
    private TileManager tileManager;
    [SerializeField]
    // The current upgrade manager.
    private Upgrades upgradeManager;
    [SerializeField]
    // The field state that this automater will move towards.
    private TileState targetState;
    // The current targeted tile.
    private Tile targetTile;
    // The direction that this automater is going towards.
    private Vector3 targetDirection;
    // How long it will take for this automater to complete an interaction with a tile.
    private float interactionTimer;
    // The Gravestone position to move back to
    [SerializeField] private GameObject gravestone;

    //Animator for the swing
    private Animator exectutionerAnim;

    [SerializeField]
    // How fast this automater moves towards its target.
    private float movementSpeed = 1.0f;
    [SerializeField]
    // The default delay before the automater changes targets.
    private float defaultTargetChangeDelay = 3.0f;
    // The current target change delay.
    private float targetChangeDelay;

    private automatorType myType;

    // ~~~ PROPERTIES ~~~
    private bool IsTileCorrectState
    {
        get
        {
            return (targetTile.TileState == targetState) || 
                // Account for rototillers harvesting withered crops
                (targetState == TileState.Ready && targetTile.TileState == TileState.Withered);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        exectutionerAnim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (targetTile != null)
        {
            if (!IsTileCorrectState)
            {
                targetTile = null;
                targetDirection = new Vector3(0, 0, 0);
                targetChangeDelay = 0.5f;
            }
            else if (Vector3.Distance(targetTile.transform.position, transform.position) > 0.05f)
            {
                Vector3 deltaPos = movementSpeed * Time.deltaTime * targetDirection;
                transform.position += deltaPos;
            }
            else if (interactionTimer <= 0)
            {
                interactionTimer = 1.0f;
                exectutionerAnim.ResetTrigger("Start Moving");
                exectutionerAnim.SetTrigger("Start Interacting");
                targetDirection = new Vector3(0, 0, 0);
                targetDirection.Normalize();
            }
            else
            {
                interactionTimer -= Time.deltaTime;
                if (interactionTimer <= 0)
                    InteractWithTile();
            }
        }
        else
        { 
            transform.position += targetDirection * movementSpeed * Time.deltaTime;
            
            targetChangeDelay -= Time.deltaTime;
            if (targetChangeDelay <= 0)
                SeekNewTile();
        }
    }

    /// <summary>
    /// Finds a new tile based on the seeker's target tile state.
    /// </summary>
    public void SeekNewTile()
    {
        targetTile = tileManager.GetRandomTileOfState(targetState);
        // If this object harvests pumpkins but none are available, do an
        // additional check to see if any withered pumpkins can be harvested instead
        if (targetTile == null && targetState == TileState.Ready)
            targetTile = tileManager.GetRandomTileOfState(TileState.Withered);

        // If no tiles are of the target state, wait a second and try again
        if (targetTile == null)
        {
            targetChangeDelay = 1.0f;
            exectutionerAnim.ResetTrigger("Start Interacting");
            exectutionerAnim.SetTrigger("Start Idle");
            if (myType == automatorType.Harvester) { targetDirection = gravestone.transform.position - transform.position; targetDirection.Normalize(); }
            
            if (targetDirection.x < 0)
                GetComponent<SpriteRenderer>().flipX = true;
            else
                GetComponent<SpriteRenderer>().flipX = false;
        }
        // Otherwise, start moving in that tile's direction
        else
        {
            exectutionerAnim.ResetTrigger("Start Idle");
            exectutionerAnim.SetTrigger("Start Moving");
            targetDirection = targetTile.transform.position - transform.position;
            targetDirection.Normalize();
            if (targetDirection.x < 0)
                GetComponent<SpriteRenderer>().flipX = true;
            else
                GetComponent<SpriteRenderer>().flipX = false;
        }
    }

    /// <summary>
    /// Interacts with the target based on its state.
    /// </summary>
    public void InteractWithTile()
    {
        switch (targetTile.TileState)
        {
            case TileState.Empty:
                targetTile.PlantCrop();
                break;
            case TileState.Ready:
                targetTile.HarvestCrop(true);
                if (targetTile.IsJackOLantern)
                    upgradeManager.IncreasePumpkin(5);
                else
                    upgradeManager.IncreasePumpkin(1);
                break;
            case TileState.Withered:
                targetTile.HarvestCrop(false);
                break;
            default:
                break;
        }
        targetTile = null;
        targetChangeDelay = defaultTargetChangeDelay + Random.value - 0.5f;
        exectutionerAnim.ResetTrigger("Start Interacting");
        exectutionerAnim.SetTrigger("Start Idle");
    }

    /// <summary>
    /// Assigns the tile manager and upgrade manager to this object because Unity is stinky
    /// and I can't do it in the prefab editor
    /// </summary>
    /// <param name="tileManager">The current TileManager.</param>
    /// <param name="upgradeManager">The current upgrade manager.</param>
    public void AssignManagers(TileManager tileManager, Upgrades upgradeManager, GameObject gravestone, automatorType type)
    {
        this.tileManager = tileManager;
        this.upgradeManager = upgradeManager;
        this.gravestone = gravestone;
        this.myType = type;
    }
}
