using UnityEngine;

namespace Test
{
    public class Test : MonoBehaviour
    {
        int counter = 0;
        float timer = 0f;

        void FixedUpdate()
        {
            counter++;
            timer += Time.fixedDeltaTime;

            if (timer >= 1f)
            {
                Debug.Log("FixedUpdate per second: " + counter);
                counter = 0;
                timer = 0f;
            }
        }
    }
}