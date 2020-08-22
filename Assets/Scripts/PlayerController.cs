using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;

public class PlayerController : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    private Transform cam;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 5.5f;
    public float jumpForce = 5f;
    public float gravity = -9.807f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform selectedBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Toolbar toolbar;

    private void Start()
    {

        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {

        if (!world.uiActive) { 
            CalculateVelocity();

            if (jumpRequest)
                JumpAction();

            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
            cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);
            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            world.uiActive = !world.uiActive;
        }

        if (!world.uiActive)
        {
            GetPlayerInputs();
            PlaceSelectedBlock();
        }
    }

    private void JumpAction()
    {

        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity() //pseudo-physics
    {
        //change vertical momentum
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        //check sprinting
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

        //apply momentum
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && Front) || (velocity.z < 0 && Back))
            velocity.z = 0;
        if ((velocity.x > 0 && Right) || (velocity.x < 0 && Left))
            velocity.x = 0;
        if (velocity.y < 0)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = CheckUpSpeed(velocity.y);
    }

    private void GetPlayerInputs()
    {

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint"))
            isSprinting = true;
        if (Input.GetButtonUp("Sprint"))
            isSprinting = false;

        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;

        if(selectedBlock.gameObject.activeSelf)
        {
            //destroy block
            if (Input.GetMouseButtonDown(0))
                world.GetChunkFromPosition(selectedBlock.position).EditVoxelData(selectedBlock.position, 0);

            //create and place new block
            if (Input.GetMouseButtonDown(1))
            {
                if (toolbar.slots[toolbar.slotIndex].HasItem)
                {
                    world.GetChunkFromPosition(placeBlock.position).EditVoxelData(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.UpdateAmount(1);
                }
            }
        }
    }

    private void PlaceSelectedBlock()
    {

        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while(step < reach)
        {

            Vector3 pos = cam.position + cam.forward * step;
            if(world.CheckVoxelCollider(pos))
            {
                selectedBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                selectedBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
            step += checkIncrement;
        }

        selectedBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float downSpeed)
    {

        if (
            world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
           )
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float CheckUpSpeed(float upSpeed)
    {

        if (
            world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 1.9f, transform.position.z - playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 1.9f, transform.position.z - playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 1.9f, transform.position.z + playerWidth)) ||
            world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 1.9f, transform.position.z + playerWidth))
           )
            return 0;
        else return upSpeed;
    }

    public bool Front
    {
        get
        {
            if (
                world.CheckVoxelCollider(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckVoxelCollider(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
            )
                return true;
            else return false;
        }
    }

    public bool Back
    {
        get
        {
            if (
                world.CheckVoxelCollider(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckVoxelCollider(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
            )
                return true;
            else return false;
        }
    }

    public bool Left
    {
        get
        {
            if (
                world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckVoxelCollider(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
            )
                return true;
            else return false;
        }
    }

    public bool Right
    {
        get
        {
            if (
                world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckVoxelCollider(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
            )
                return true;
            else return false;
        }
    }
}


