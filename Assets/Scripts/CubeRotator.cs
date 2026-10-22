using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    public void SetYRotation(float degrees)
    {
        transform.rotation = Quaternion.Euler(0f, degrees, 0f);
    }
}
