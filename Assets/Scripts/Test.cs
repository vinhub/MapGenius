using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LateStart(1f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator LateStart(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        RunTest();
    }

    private void RunTest()
    {
        PopupMessage.ShowMessage(PopupMessageType.FirstLandmarkCrossed, string.Format(Strings.OtherLandmarkCrossedMessageFormat, "Post Office"));
        //GameSystem.Instance.ShowInfoMessage("Test message", 3f);
    }
}
