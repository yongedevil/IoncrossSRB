//#define DEBUG
//#define DEBUG_UPDATES
//#define DEBUG_CALCULATIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using KSP;

namespace IoncrossKerbal_SRB
{
    class IonGUIThrustCurve
    {
        public const int GRAPH_HEIGHT = 200;
        public const int GRAPH_WIDTH = 400;
        public const int LABLES_X_WIDTH = 35;
        public const int LABLES_Y_HEIGHT = 35;

        public IonModuleSRB srbManager;

        public Rect windowPos;

        public GUIStyle windowStyle;
        GUIStyle TopLabel;
        GUIStyle MidLabel;
        GUIStyle BottomLabel;
        GUIStyle LeftLable;
        GUIStyle CentreLable;
        GUIStyle RightLable;

        Texture2D curveTexture;

        public int curveWidth;

        List<Vector2> curvePoints;

        /************************************************************************\
         * IonGUIThrustCurve class                                              *
         * Constructor                                                          *
        \************************************************************************/
        public IonGUIThrustCurve(IonModuleSRB srb)
        {
            srbManager = srb;
            windowPos = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
            windowStyle = new GUIStyle(HighLogic.Skin.window);

            curveTexture = new Texture2D(GRAPH_WIDTH, GRAPH_HEIGHT);
            curveWidth = 5;



            //Set Styles
            GUIStyle BaseLabel = new GUIStyle();
            BaseLabel.normal.textColor = HighLogic.Skin.window.normal.textColor;

            TopLabel = new GUIStyle(BaseLabel);
            MidLabel = new GUIStyle(BaseLabel);
            BottomLabel = new GUIStyle(BaseLabel);
            LeftLable = new GUIStyle(BaseLabel);
            CentreLable = new GUIStyle(BaseLabel);
            RightLable = new GUIStyle(BaseLabel);

            TopLabel.alignment = TextAnchor.UpperCenter;
            MidLabel.alignment = TextAnchor.MiddleCenter;
            BottomLabel.alignment = TextAnchor.LowerCenter;
            LeftLable.alignment = TextAnchor.MiddleLeft;
            CentreLable.alignment = TextAnchor.MiddleCenter;
            RightLable.alignment = TextAnchor.MiddleRight;

            UpdateCruveTexture();
        }

        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawGUI function                                                     *
         *                                                                      *
         * Draws the GUI window.                                                *
        \************************************************************************/
        public void DrawGUI(int WindowID)
        {
            GUILayout.BeginVertical();
            {
                DrawInfo();
                DrawCruve();
            }
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawInfo()
        {
        }

        private void DrawPoints ()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("point");
                GUILayout.Label("Fuel Percent");
                GUILayout.Label("Thrust Precent");
            }
            GUILayout.EndHorizontal();

            int pointNum = 0;
            //foreach (Tuple<float, float> point in cruvePoints)
            //{
            //    DrawPoint(pointNum++, point);
            //}
        }
        //private void DrawPoint(int num, Tuple<float, float> point)
        //{
        //    GUILayout.BeginHorizontal();
        //    {
        //        GUILayout.Label("point " + num.ToString());
        //        GUILayout.Label(point.Item1.ToString());
        //        GUILayout.Label(point.Item2.ToString());
        //    }
        //    GUILayout.EndHorizontal();
        //}

        private void DrawCruve()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(LABLES_X_WIDTH));
                {
                    GUILayout.Space(10);
                    GUILayout.Label(srbManager.engine.maxThrust.ToString("F0") + " kN", TopLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label((srbManager.engine.maxThrust / 2f).ToString("F0") + " kN", MidLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label("0 kN", BottomLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();

                GUILayout.Box(curveTexture);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Height(LABLES_Y_HEIGHT));
            {
                GUILayout.Space(LABLES_X_WIDTH + 15);
                GUILayout.Label("100% fuel", LeftLable, GUILayout.Width(GRAPH_WIDTH / 3));
                GUILayout.Label("50% fuel", CentreLable, GUILayout.Width(GRAPH_WIDTH / 3));
                GUILayout.Label("0% fuel", RightLable, GUILayout.Width(GRAPH_WIDTH / 3));
            }
            GUILayout.EndHorizontal();


        }

        public void UpdateCruveTexture()
        {
            float scale = srbManager.engine.maxThrust / curveTexture.height;

            int x, y;
            int val;
            Color background;
            
            for (y = 0; y < curveTexture.height; ++y)
            {
                background = new Color((float)y / curveTexture.height *0.5f, 0.0f, 0.0f);
                for (x = 0; x < curveTexture.width; ++x)
                {
                    curveTexture.SetPixel(x, y, background);
                }
            }

            for (x = 0; x < curveTexture.width; ++x)
            {
                //val = (int)Math.Round(srbManager.thrustPercentCurve.Evaluate((cruveTexture.width - x) / cruveTexture.width) / 100f, MidpointRounding.AwayFromZero) * cruveTexture.height;
                val = (int)Math.Round(50f / 100f * curveTexture.height, MidpointRounding.AwayFromZero);
                for (y = val - curveWidth / 2; y < val + curveWidth / 2 && y < curveTexture.height; ++y)
                {
                    if (y >= 0)
                        curveTexture.SetPixel(x, y, Color.blue);
                }
            }

            curveTexture.Apply();
        }
    }
}
