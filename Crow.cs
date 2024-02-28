using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public enum CrowType
{
    Crow,
    Squirrel
}


public class Crow : MonoBehaviour
{
    //tile manager
     private TileManager tileManager;
     
    //animator
     private Animator animator;
    [SerializeField] Animator explosion;

    //audio source
   [SerializeField] private AudioEventChannel audioSource;

    //poof animator
    //veocity of the object
    private Vector3 velocity;
    public bool isCrow;
    //speed
    [SerializeField] private float speed;

    //target tile
    private Tile targetTile;

    //direction of the crow
    private Vector3 direction = Vector3.zero;

    //sprite renderer
    private SpriteRenderer spriteRenderer;

    //shadow spriterenderer
    [SerializeField] private SpriteRenderer shadow;

    //target point
    private Vector3 target;

    //clicked
    bool clicked = false;
    private int clickCount = 0;
    [SerializeField] private int maxClickedCount = 2;

    //mouse overlaps
    bool overlaps = false;

    //camera
    Camera mainCam;

    //crow picking values
    private float pickingTimer = 0.0f;
    [SerializeField] private float pickingMax = 2.0f;

    private CrowType myType;

    // How likely a crow is to leave.
    private float hunger;

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        //set the tile target for the enemy
        SetTileTarget();

        mainCam = Camera.main;
        hunger = Random.Range(0.1f, 0.7f);
    }


    void Update()
    {

        overlaps = OverlapsMouse(mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
        if ((targetTile == null || targetTile.TileState == TileState.Empty))
        {
            if (0 >= hunger) clicked = true;
            else SetTileTarget();
        }

        //check animator to see if it can move(flying)
        if(animator.GetCurrentAnimatorStateInfo(0).IsName("Crow_fly_Anim"))
            Move();
        //check if picking, if so delete the pumpkin after a few seconds
        else if(animator.GetCurrentAnimatorStateInfo(0).IsName("Crow_Pick_Anim"))
        {
            pickingTimer += Time.deltaTime;
            if(pickingTimer >= pickingMax)
            {
                pickingTimer = 0;
                animator.ResetTrigger("Trigger Landing");
                animator.SetTrigger("Trigger TakeOff");
                if (targetTile != null)
                {
                    if (!targetTile.IsJackOLantern) hunger -= 0.1f;
                    else hunger -= 0.3f;
                    targetTile.HarvestCrop(false);
                }
            }
        }

        //check position
        CheckPos();
        
    }

    //move to the target tile, or out of screen if no targets. 
    //It also flips the crows based on their target, and sets animation triggers according to distance
    private void Move()
    {
        if (targetTile == null || targetTile.TileState == TileState.Empty || clicked)
        {
            if (spriteRenderer.flipX == false)
                target = new Vector3(1000, Screen.height / 8, 0);
            else
                target = new Vector3(-1000, Screen.height / 8, 0);
            animator.ResetTrigger("Trigger Landing");
            animator.SetTrigger("Trigger TakeOff");
            direction = target - transform.position;
        }
        else
        {
            target = targetTile.transform.position;
            //use speed with deltatime to move the sprite
            if (Vector3.Distance(transform.position, target) > .3f)
            {
                direction = target - transform.position;
            }
            else if (Vector3.Distance(transform.position, target) > .1f)
            {
                direction = arriveAtTile(target);
                animator.ResetTrigger("Trigger TakeOff");
                audioSource.Raiseat();
                animator.SetTrigger("Trigger Landing");
            }
            else
            {
                direction = Vector3.zero;
            }
        }


        if (target.x > transform.position.x) spriteRenderer.flipX = false;
        else spriteRenderer.flipX = true;

        direction.Normalize();
        velocity = direction * speed * Time.deltaTime;

        transform.position += velocity;
    }

    //Arrive behavior to slow down crows a bit as they arrive
    private Vector3 arriveAtTile(Vector3 target)
    {

        Vector3 desired = target - transform.position;
        Vector3 steer;
        //The distance is the magnitude of the vector pointing from location to target.
        float d = desired.magnitude;
        desired.Normalize();
        if (d < 1)
        {
            float m = d / 3;
            desired *= m;
        }
        else
        {
            desired *= speed;
        }

        steer = desired - velocity;
        return steer;
    }

    //sets the tile manager of the crow
    public void SetTileManager(TileManager mngr)
    {
        tileManager = mngr;
    }

    /// <summary>
    /// Checks the positon and deletes if far off screen
    /// </summary>
    private void CheckPos()
    {
        if(transform.position.x > 30 || transform.position.x < -50)
        {
            Destroy(gameObject);
        }
    }


    //sets the target tile and then rotates accordingly
    private void SetTileTarget()
    {
        targetTile = tileManager.GetRandomReadyTile();       
    }


    public void OnCrowClick(InputAction.CallbackContext context)
    {

            if (clicked)
                return;
        if (context.performed)
        {
            if (overlaps)
            {
                clickCount++;
                if (clickCount >= maxClickedCount) clicked = true;
                explosion.SetTrigger("Trigger Poof");               
                if (clicked)
                {
                    if (isCrow)
                        audioSource.RaiseCrowSound();
                    else
                        audioSource.RaiseSquirrellSound();
                    animator.ResetTrigger("Trigger Landing");
                    animator.SetTrigger("Trigger TakeOff");
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                }
            }
        }
    }

    //Enables an overlay on the crow to know where it can be clicked
    public bool OverlapsMouse(Vector3 mousePos)
    {
        if (mousePos.x < transform.position.x + 0.5 && mousePos.x > transform.position.x - 0.5 &&
                mousePos.y < transform.position.y + 0.5 && mousePos.y > transform.position.y - 0.5)
        {
            shadow.enabled = true;
            transform.localScale = new Vector3(3.2f, 3.2f);
            return true;
        }
        else
        {
            shadow.enabled = false;
            transform.localScale = new Vector3(3.0f, 3.0f);
        }
        return false;
    }

    public void SetType(CrowType type)
    {
        this.myType = type;
    }
}
