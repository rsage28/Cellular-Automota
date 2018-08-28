using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public float scrollSpeed;
    public float panSpeed;
	
	// Update is called once per frame
	void Update () {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0) {
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - (scroll * scrollSpeed), 0.01f, 250);
        }

        if (Input.GetKey(KeyCode.W)) {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + panSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A)) {
            transform.position = new Vector3(transform.position.x - panSpeed * Time.deltaTime, transform.position.y, transform.position.z);
        }
        if (Input.GetKey(KeyCode.S)) {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - panSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D)) {
            transform.position = new Vector3(transform.position.x + panSpeed * Time.deltaTime, transform.position.y, transform.position.z);
        }
    }
}
