using UnityEngine;

public class UpDownMover : MonoBehaviour
{
    public float lowY = -3f;
    public float highY = 3f;
    public float speed = 2f;

    void Update()
    {
        float t = Mathf.PingPong(Time.time * speed, 1f);
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(lowY, highY, t);
        transform.position = pos;
    }
}
