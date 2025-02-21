using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTrack : MonoBehaviour
{
    [SerializeField]
    Transform track;
    [SerializeField]
    float trackMoveCoefficent = 1;
    // Start is called before the first frame update
    void Start()
    {
        track = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        moveTrack();
    }

    void moveTrack()
    {
        // negative to x axis
        transform.Translate(Vector3.left * trackMoveCoefficent * Time.deltaTime);
    }
}
