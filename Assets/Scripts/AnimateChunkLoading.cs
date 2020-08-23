using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateChunkLoading : MonoBehaviour
{
    float animationSpeed = 3.5f;
    Vector3 finalPos;

    float waitTimer;
    float timer;

    private void Start()
    {
        waitTimer = Random.Range(0f, 3f);
        finalPos = transform.position;
        transform.position = new Vector3(transform.position.x, -VoxelData.chunkHeight, transform.position.z);
    }

    private void Update()
    {

        if (timer < waitTimer)
        {
            timer += Time.deltaTime;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * animationSpeed);
            if ((finalPos.y - transform.position.y) < 0.05f)
            {
                transform.position = finalPos;
                Destroy(this);
            }
        }
    }
}
