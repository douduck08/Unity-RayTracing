using UnityEngine;

namespace DouduckLib {
    public class FramerateCounter : MonoBehaviour {
        [SerializeField] int frameRange = 60;

        public float AverageFPS { get; private set; }
        public float HighestFPS { get; private set; }
        public float LowestFPS { get; private set; }

        float[] fpsBuffer;
        int fpsBufferIndex;

        static int fontSize = 24;
        static Texture2D background;
        static RectOffset margin = new RectOffset ();
        static Color contentColor = new Color (0.9f, 0.9f, 0.9f);
        static Color backgroundColor = new Color (0f, 0f, 0f, 0.5f);

        void Start () {
            if (frameRange < 1) {
                frameRange = 1;
            }
            fpsBuffer = new float[frameRange];
            fpsBufferIndex = 0;

            background = Texture2D.whiteTexture;
        }

        void Update () {
            UpdateBuffer ();
            CalculateFPS ();
        }

        void UpdateBuffer () {
            fpsBuffer[fpsBufferIndex] = 1f / Time.unscaledDeltaTime;

            fpsBufferIndex += 1;
            if (fpsBufferIndex >= frameRange) {
                fpsBufferIndex = 0;
            }
        }

        void CalculateFPS () {
            HighestFPS = 0f;
            LowestFPS = 1000f;

            var sum = 0f;
            var fps = 0f;
            for (int i = 0; i < frameRange; i++) {
                fps = fpsBuffer[i];
                sum += fps;
                HighestFPS = Mathf.Max (HighestFPS, fps);
                LowestFPS = Mathf.Min (LowestFPS, fps);
            }
            AverageFPS = sum / frameRange;
        }

        void OnGUI () {
            GUI.skin.label.fontSize = fontSize;
            GUI.skin.label.normal.background = background;
            GUI.skin.label.margin = margin;
            GUI.contentColor = contentColor;
            GUI.backgroundColor = backgroundColor;

            GUILayout.Label (string.Format ("Highest {0:#.}", HighestFPS));
            GUILayout.Label (string.Format ("Average {0:#.}", AverageFPS));
            GUILayout.Label (string.Format ("Lowest {0:#.}", LowestFPS));
        }
    }
}