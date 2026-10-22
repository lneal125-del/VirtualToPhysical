using UnityEngine;

public class LowTrigger : MonoBehaviour
{
    public SendToArduinoScript sender;
    public float lowY = 1f;
    public float threshold = 0.05f;

    private bool wasAtLow = false;

    void Update()
    {
        bool atLow = transform.position.y <= lowY + threshold;

        if (atLow && !wasAtLow && sender != null)
        {
            sender.SendTone();
        }

        wasAtLow = atLow;
    }
}