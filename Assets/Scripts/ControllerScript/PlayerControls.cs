﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(Player))]
public class PlayerControls : MonoBehaviour
{
    //Constants used for Ground Checking and Movement
    private const float groundDepth = 0.1f;
    private const float groundRadius = 0.1f;
    private const float moveSpeed = 10.0f;
    private const float jumpSpeed = 10.0f;

    //Delegate for resetting all boolean triggers
    delegate void BooleanDel();
    BooleanDel setBools;

    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private LayerMask whatIsGround;

    private Player player;
    private ControlScheme controlMap;
    private Rigidbody rigidBody;
    private Animator anim;
    public PlayerInfo MyPlayerInfo; // Set by UIManager
    private Vector3 moveDir;
    private float maxMoveDir;
    private bool jumpFlag, attackFlag, interactFlag;

    //Knockback stuff
    [SerializeField]
    private float knockBackForce = 1;
    [SerializeField]
    public float knockBackTime = 1;
    private float knockBackCounter;

    //Variables checking for multiple inputs in attacks
    private float timePassed;
    public bool canAttack = true;
    private const float MIN_REACTION_TIME = 0.5f;
    private const float THRESHOLD = 0.6f;
    public int comboCounter
    {
        get
        {
            return _comboCounter;
        }
        set
        {
            _comboCounter = value;
        }
    }
    private int _comboCounter = 0;


    //Top and Bottom check used in the Ground Check
    private Vector3 groundCheckTop
    {
        get
        {
            return groundCheck.position;
        }
    }

    private Vector3 groundCheckBot
    {
        get
        {
            return new Vector3(
                groundCheck.position.x,
                groundCheck.position.y - groundDepth,
                groundCheck.position.z);
        }
    }

    private void Start()
    {
        setBools = () => { };
        moveDir = Vector3.zero;
        player = gameObject.GetComponent<Player>();
        rigidBody = gameObject.GetComponent<Rigidbody>();
        anim = gameObject.GetComponent<Animator>();
        controlMap = ControlScheme.createControlMap(player.EntID);
        maxMoveDir = Vector2.one.magnitude;
    }

    private void Update()
    {
        if (!canAttack)
        {
            timePassed += Time.deltaTime;
        }
        anim.SetBool("grounded", false);
        //Check if there is ground under the player, add whatIsGround as parameter to check layer
        Collider[] colliders = Physics.OverlapCapsule(groundCheckTop, groundCheckBot, groundRadius);
        for (int i = 0; i < colliders.Length; ++i)
        {
            if (colliders[i].gameObject != this.gameObject)
            {
                //Something was found
                //Debug.Log("Found Ground");
                anim.SetBool("grounded", true);
                break;
            }
        }

        //Parse Input for button press, set flag then add flag to delegate to be unset after InputParse()
        if (Input.GetKeyDown(controlMap.attack))
        {
            attackFlag = true;
            setBools += () => attackFlag = false;
        }

        if (Input.GetKeyDown(controlMap.jump))
        {
            jumpFlag = true;
            setBools += () => jumpFlag = false;
        }

        if (Input.GetKeyDown(controlMap.InventoryScrollLeft))
        {
            MyPlayerInfo.ScrollLeft();
        }

        if (Input.GetKeyDown(controlMap.InventoryScrollRight))
        {
            MyPlayerInfo.ScrollRight();
        }

        if (Input.GetKeyDown(controlMap.InventoryUseItem))
        {
            MyPlayerInfo.UseItem();
        }
        Vector3 pos = UnityEngine.Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        transform.position = UnityEngine.Camera.main.ViewportToWorldPoint(pos);

    }

    private void FixedUpdate()
    {
        if (knockBackCounter <= 0)
        {
            moveDir.x = Input.GetAxis("Horizontal" + player.EntID);
            moveDir.z = Input.GetAxis("Vertical" + player.EntID);
        }
        else
        {
            knockBackCounter -= Time.deltaTime;
        }
        InputParse();
    }

    private void InputParse()
    {
        move();
        if (attackFlag)
            attack();

        //Reset all Input flags
        setBools();
        //Set delegate to empty
        setBools = () => { };
    }

    private void move()
    {
        Vector3 dir = new Vector3(moveDir.x, 0.0f, moveDir.z);
        anim.SetFloat("speed", moveDir.magnitude / maxMoveDir);
        if (anim.GetFloat("speed") > 0 && knockBackCounter <= 0)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), .15f);
        }

        //Scale the movement by movespeed and maintain current y velocity
        dir *= moveSpeed;
        dir.y = rigidBody.velocity.y;

        if (jumpFlag && anim.GetBool("grounded"))
        {
            anim.SetBool("grounded", false);
            dir.y = jumpSpeed;
        }

        rigidBody.velocity = dir;
    }

    private void attack()
    {
        float animationLength = anim.GetCurrentAnimatorStateInfo(0).length;
        float animationWindow;
        //Checks for animations that are very small
        if (animationLength - (THRESHOLD * animationLength) < MIN_REACTION_TIME)
        {
            animationWindow = MIN_REACTION_TIME;
        }
        else
        {
            animationWindow = anim.GetCurrentAnimatorStateInfo(0).length * THRESHOLD;
        } //Checks if the player can attack after it reaches a certain point in the animation.
        if (animationWindow < timePassed || comboCounter == 0)
        {
            Debug.Log("Attack!");
            timePassed = 0;
            comboCounter++;
            anim.SetInteger("attack", comboCounter);
            canAttack = false;
        }
        else
        {
            Debug.Log("Cannot Attack Yet!");
        }
    }

    public void KnockBack(Vector3 dir)
    {
        Debug.Log("Knocking Back!");
        knockBackCounter = knockBackTime;
        moveDir = knockBackForce * dir;
        //Attempt to test knockback on y-axis
        //moveDir = knockBackForce * (Quaternion.AngleAxis(-30, Vector3.forward) * dir);
    }
}