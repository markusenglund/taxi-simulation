using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        int startHour = 18;
        float currentTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) + startHour;
        // Get the text component from the child of this transform
        TMP_Text text = GetComponentInChildren<TMP_Text>();
        text.text = TimeUtils.ConvertSimulationHoursToTimeString(currentTime);
    }
}
