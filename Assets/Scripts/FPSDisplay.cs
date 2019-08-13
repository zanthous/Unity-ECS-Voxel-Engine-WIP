using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{

    // Attach this to a GUIText to make a frames/second indicator.
    //
    // It calculates frames/second over each updateInterval,
    // so the display does not keep changing wildly.
    //
    // It is also fairly accurate at very low FPS counts (<10).
    // We do this not by simply counting frames per interval, but
    // by accumulating FPS for each frame. This way we end up with
    // correct overall FPS even if the interval renders something like
    // 5.5 frames.

    public float updateInterval = 5.0f;

    private float accum = 0; // FPS accumulated over the interval
    private int frames = 0; // Frames drawn over the interval
    private float timeleft; // Left time for current interval
    [SerializeField] private Text text;
    void Start()
    {
        if(!text)
        {
            Debug.Log("UtilityFramesPerSecond needs a GUIText component!");
            enabled = false;
            return;
        }
        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Interval ended - update GUI text and start new interval
        if(timeleft <= 0.0)
        {
            // display two fractional digits (f2 format)
            float fps = accum / frames;
            string format = System.String.Format("{0:F2} FPS", fps);
            text.text = format;
            
            //	DebugConsole.Log(format,level);
            timeleft = updateInterval;
            accum = 0.0F;
            frames = 0;
        }
    }
}


//using UnityEngine;
//using UnityEngine.UI;

//public class FPSDisplay : MonoBehaviour
//{
//    //float deltaTime = 0.0f;
//    float timer = 0.0f;
//    float buffer = 0.0f;
//    int frames = 0;
//    float latestAvgFps = 0.0f;
//    private float time;

//    private void Start()
//    {
//        time = Time.time;
//    }
//    [SerializeField] private Text text;
//    void Update()
//    {
//        frames++;
//        if(Time.time > time + 5.0f)
//        {
//            time = Time.time;
//            text.text = (frames / 5.0f).ToString();
//            frames = 0;
//        }
//    }
//}