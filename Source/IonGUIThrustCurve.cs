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
    public class IonGUIThrustCurve
    {
        public const int COLWIDTH_POINTNUM = 75;
        public const int COLWIDTH_POINTX = 80;
        public const int COLWIDTH_POINTY = 80;

        public const int GRAPH_HEIGHT = 200;
        public const int GRAPH_WIDTH = 400;
        public const int LABLES_Y_WIDTH = 35;
        public const int LABLES_X_HEIGHT = 35;

        public IonModuleSRB module_srb;
        public ThrustCurve thrustCurve;

        public Rect windowPos;

        public GUIStyle windowStyle;
        private GUIStyle TopLabel;
        private GUIStyle MidLabel;
        private GUIStyle BottomLabel;
        private GUIStyle LeftLable;
        private GUIStyle CentreLable;
        private GUIStyle RightLable;

        Texture2D curveTexture;

        public int curveWidth;

        /************************************************************************\
         * IonGUIThrustCurve class                                              *
         * Constructor                                                          *
        \************************************************************************/
        public IonGUIThrustCurve(IonModuleSRB module, ThrustCurve curve)
        {
            module_srb = module;
            thrustCurve = curve;

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
            DrawPoints();
        }

        private void DrawPoints ()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);
                GUILayout.Label("Point", GUILayout.Width(COLWIDTH_POINTNUM));
                GUILayout.Label("Fuel (%)", GUILayout.Width(COLWIDTH_POINTX));
                GUILayout.Label("Thrust (%)", GUILayout.Width(COLWIDTH_POINTY));
            }
            GUILayout.EndHorizontal();

            int pointIndex = 0;
            foreach (ThrustPoint point in thrustCurve.list_points)
            {
                DrawPoint(pointIndex++, point);
            }
        }
        private void DrawPoint(int num, ThrustPoint point)
        {
            string point_x = (Math.Round(point.timePortion * 100f, 2)).ToString();
            string point_x_last = point_x;
            string point_y = (Math.Round(point.thrustPortion * 100f, 2)).ToString();
            string point_y_last = point_y;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);
                GUILayout.Label("point " + num.ToString(), GUILayout.Width(COLWIDTH_POINTNUM));

                point_x = GUILayout.TextField(point_x, GUILayout.Width(COLWIDTH_POINTX));
                point_y = GUILayout.TextField(point_y, GUILayout.Width(COLWIDTH_POINTY));
            }
            GUILayout.EndHorizontal();

            if (point_x_last != point_x)
            {
                float newX;
                if (float.TryParse(point_x, out newX))
                {
                    if (newX > 100f)
                        newX = 100f;
                    else if (newX < 0f)
                        newX = 0f;

                    point.timePortion = newX / 100f;
                }
                else if (point_x == "")
                    point.timePortion = 0;

                UpdateCruveTexture();
                module_srb.module_engine.thrustPercentage = thrustCurve.CalculateAverageThrust() * 100f;
            }

            if (point_y_last != point_y)
            {
                float newY;
                if (float.TryParse(point_y, out newY))
                {
                    if (newY > 100f)
                        newY = 100f;
                    else if (newY < 0f)
                        newY = 0f;

                    point.thrustPortion = newY / 100f;
                }
                else if (point_y == "")
                    point.thrustPortion = 0;

                UpdateCruveTexture();
                module_srb.module_engine.thrustPercentage = thrustCurve.CalculateAverageThrust() * 100f;
            }
        }

        private void DrawCruve()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    GUILayout.Space(10);
                    GUILayout.Label(module_srb.module_engine.maxThrust.ToString("F0") + " kN", TopLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label((module_srb.module_engine.maxThrust / 2f).ToString("F0") + " kN", MidLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label("0 kN", BottomLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();

                GUILayout.Box(curveTexture);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Height(LABLES_X_HEIGHT));
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);
                GUILayout.Label("100% fuel", LeftLable, GUILayout.Width(GRAPH_WIDTH / 3));
                GUILayout.Label("50% fuel", CentreLable, GUILayout.Width(GRAPH_WIDTH / 3));
                GUILayout.Label("0% fuel", RightLable, GUILayout.Width(GRAPH_WIDTH / 3));
            }
            GUILayout.EndHorizontal();


        }

        public void UpdateCruveTexture()
        {
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
                val = (int)Math.Round(thrustCurve.Evaluate((float)x / curveTexture.width) * curveTexture.height, MidpointRounding.AwayFromZero);
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
