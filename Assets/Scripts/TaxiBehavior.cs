using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TaxiBehavior : MonoBehaviour
{
    private float speed = 1f;

    private Queue<Vector3> waypoints = new Queue<Vector3>();


    public static Transform Create(Transform prefab, float x, float z)
    {
        Transform taxi = Instantiate(prefab, new Vector3(x, 0.05f, z), Quaternion.identity);
        taxi.name = "Taxi";
        return taxi;
    }

    void Start()
    {
        // Set up the waypoints
        waypoints.Enqueue(new Vector3(4, 0.05f, 0));
        waypoints.Enqueue(new Vector3(4, 0.05f, 4));
        waypoints.Enqueue(new Vector3(8, 0.05f, 4));
        waypoints.Enqueue(new Vector3(8, 0.05f, 0));
    }

    void Update()
    {
        // Return if waypoints is empty
        if (waypoints.Count == 0)
        {
            return;
        }
        // Read the first waypoint from the queue without dequeuing it
        Vector3 waypoint = waypoints.Peek();


        Debug.Log(waypoint);
        // Move the taxi
        transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);

        // Log the taxi's position
        Debug.Log(transform.position);
        // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
        if (transform.position == waypoint)
        {
            waypoints.Dequeue();
        }
    }


}
