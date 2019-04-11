using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CloseAfterSeconds(5));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Coroutine used to fade out info messages after x seconds.
    IEnumerator CloseAfterSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene("PluxUnityInterface");
    }
}
