using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public int maxFOV = 500;
	public int minFOV = 2;
	public float wasdSpeed = 4.0f; //regular speed
    public float rotateSpeed = 40.0f;

    private Camera m_Camera; // Used for referencing the camera.

	void Awake ()
    {
    	m_Camera = GetComponent<Camera>();
    }

	// Update is called once per frame
	void Update () 
	{	
		// QE rotation
		if(Input.GetKey (KeyCode.E))
	    {
	        transform.RotateAround(new Vector3(0,0,0), Vector3.up, rotateSpeed * Time.deltaTime);
	    }

	    if(Input.GetKey (KeyCode.Q))
	    {
	        transform.RotateAround(new Vector3(0,0,0), -Vector3.up, rotateSpeed * Time.deltaTime);
	    }
		//Keyboard commands WASD moving
        Vector3 p = GetBaseInput();
        p = p * wasdSpeed;
        p = p * Time.deltaTime;
        transform.Translate(p);

       Vector3 newPosition = transform.position;
	    // -------------------Code for Zooming Out------------
	    if (Input.GetAxis("Mouse ScrollWheel") < 0)
	        {
	            if (m_Camera.fieldOfView<=maxFOV)
	                m_Camera.fieldOfView +=2;
	        }
	    // ---------------Code for Zooming In------------------------
	     if (Input.GetAxis("Mouse ScrollWheel") > 0)
	        {
	            if (m_Camera.fieldOfView>minFOV)
	                m_Camera.fieldOfView -=2;
	        }
	}

	private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey (KeyCode.W)){
            p_Velocity += new Vector3(0, 1 , 0);
        }
        if (Input.GetKey (KeyCode.S)){
            p_Velocity += new Vector3(0, -1, 0);
        }
        if (Input.GetKey (KeyCode.A)){
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey (KeyCode.D)){
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}
