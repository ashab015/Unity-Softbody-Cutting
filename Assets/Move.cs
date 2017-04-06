using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour {

    public Vector3 StartPosition = new Vector3(0, 0, 0);
    public Vector3 EndPosition = new Vector3(0, 0, 0);
    public float DeltaTime;

    // Use this for initialization
    void Start () {

        transform.position = StartPosition;
        StartCoroutine(MoveObject());
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    IEnumerator MoveObject()
    {


        float timeSinceStarted = 0f;
        while (true)
        {
            timeSinceStarted += DeltaTime / 10000;
            transform.position = Vector3.Lerp(transform.position, EndPosition, timeSinceStarted);
            if (transform.position == EndPosition || Vector3.Distance(transform.position, EndPosition) < 0.2)
            {
                transform.gameObject.GetComponent<BoxCollider>().enabled = false;
                yield break;
            }
            yield return null;
        }
        

    }


}
