using System.Runtime.InteropServices.ComTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public Canvas canvas;

    public GameObject prefab;

    public Transform parent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    int wait = 0;
    int balls = 0;

    // Update is called once per frame
    void Update()
    {
        if (wait > 0 && balls < 30)
        {
            wait--;
            return;
        }

        wait = 100;

        // Spawn a ball at a random position on the canvas
        Vector3 position = new Vector3(Random.Range(0, canvas.pixelRect.width), Random.Range(canvas.pixelRect.height, canvas.pixelRect.height * 2), 0);
        GameObject ball = Instantiate(prefab, position, Quaternion.identity, parent);
        ball.GetComponent<Ball>().canvas = canvas;
        ball.GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-100, 100), Random.Range(-100, 100)));
        balls++;
    }

    public void OnDisable()
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
