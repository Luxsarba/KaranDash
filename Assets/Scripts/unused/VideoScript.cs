//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Video;

//public class VideoScript : MonoBehaviour
//{
//    public GameObject camera;
//    public VideoClip VideoClip;
    
//    void EndReached(UnityEngine.Video.VideoPlayer vp)
//    {
//        vp.enabled = false;
//    }
    
//    void Start()
//    {
//        // VideoPlayer automatically targets the camera backplane when it is added
//        // to a camera object, no need to change videoPlayer.targetCamera.
//        var videoPlayer = camera.GetComponent<UnityEngine.Video.VideoPlayer>();
//        videoPlayer.playbackSpeed = 10f;
//        // Play on awake defaults to true. Set it to false to avoid the url set
//        // below to auto-start playback since we're in Start().
//        videoPlayer.playOnAwake = false;

//        // By default, Video Players added to a camera will use the far plane.
//        // Let's target the near plane instead.
//        videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraNearPlane;
        
//        //videoPlayer.url = "Assets/Videos/Noisestorm - Crab Rave [Monstercat Release].mp4";
//        videoPlayer.clip = VideoClip;

//        videoPlayer.isLooping = false;

//        videoPlayer.loopPointReached += EndReached;

//        // Start playback. This means the Video Player may have to prepare (reserve
//        // resources, pre-load a few frames, etc.). To better control the delays
//        // associated with this preparation one can use videoPlayer.Prepare() along with
//        // its prepareCompleted event.
//        videoPlayer.Play();
//    }

//    // Update is called once per frame
//    void Update()
//    {
        
//    }
//}
