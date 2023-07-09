using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class CrossSectionPlane : MonoBehaviour
{
    public Material mat1, mat2;
    public float KeyMoveSpeed = 1;

    private PlayerInput playerInput;

    public void Awake()
    {
        // Assign references
        playerInput = new PlayerInput();

        playerInput.Player.Enable();
    }

    // Update is called once per frame
    public void Update()
    {
        Vector3 moveInput = playerInput.Player.Move.ReadValue<Vector2>();
        print(moveInput);

        if(moveInput.x > 0)
        {
            transform.position += transform.forward * KeyMoveSpeed * Time.deltaTime;
        }
        else if(moveInput.x < 0)
        {
            transform.position -= transform.forward * KeyMoveSpeed * Time.deltaTime;
        }

        mat1.SetVector("_PlanePosition", transform.position);
        mat1.SetVector("_PlaneNormal", transform.forward);
        mat2.SetVector("_PlanePosition", transform.position);
        mat2.SetVector("_PlaneNormal", transform.forward);
    }
}
