using UnityEngine;
using UnityEngine.InputSystem;

public class SideMover : MonoBehaviour
{
    public float speed = 5f;
    public float limit = 10f;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float input = 0f;
        if (kb.aKey.isPressed) input -= 1f;
        if (kb.dKey.isPressed) input += 1f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + input * speed * Time.deltaTime, -limit, limit);
        transform.position = pos;
    }
}