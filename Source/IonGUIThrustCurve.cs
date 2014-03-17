//#define DEBUG
//#define DEBUG_UPDATES
#define DEBUG_CALCULATIONS

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

        public Color colour_thrust;
        public Color colour_fuel;

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

            colour_thrust = Color.red;
            colour_fuel = Color.magenta;

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
                GUILayout.Label("Time (%)", GUILayout.Width(COLWIDTH_POINTX));
                GUILayout.Label("Thrust (%)", GUILayout.Width(COLWIDTH_POINTY));
                GUILayout.Label("Fuel (%)", GUILayout.Width(COLWIDTH_POINTY));
            }
            GUILayout.EndHorizontal();


            for (int i = 0; i < thrustCurve.list_points.Count; ++i)
            {
                float min = 0;
                float max = 1;
                ThrustPoint point = thrustCurve.list_points[i];

                if (i > 0)
                    min = thrustCurve.list_points[i - 1].timePortion;

                if (i + 1 < thrustCurve.list_points.Count)
                    max = thrustCurve.list_points[i + 1].timePortion;

                DrawPoint(i, point, min, max);
            }
        }
        private void DrawPoint(int num, ThrustPoint point, float timeMin, float timeMax)
        {
            float slide_time = point.timePortion;
            float slide_thrust = point.thrustPortion;
            string point_time = Math.Round(point.timePortion * 100f, 1).ToString();
            string point_time_last = point_time;
            string point_thrust = Math.Round(point.thrustPortion * 100f, 1).ToString();
            string point_thrust_last = point_thrust;


            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);
                GUILayout.Label("point " + num.ToString(), GUILayout.Width(COLWIDTH_POINTNUM));

                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTX));
                {
                    //if this is the first or last point use a label instead of a textfield
                    if (0 != point.timePortion && 1 != point.timePortion)
                    {
                        point_time = GUILayout.TextField(point_time);
                        slide_time = GUILayout.HorizontalSlider(slide_time, timeMin, timeMax);
                    }
                    else
                    {
                        GUILayout.Label(point_time);
                    }

                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTY));
                {
                    point_thrust = GUILayout.TextField(point_thrust);
                    slide_thrust = GUILayout.HorizontalSlider(slide_thrust, thrustCurve.minimumThrust, 1);
                }
                GUILayout.EndVertical();

                GUILayout.Label(point.fuelPortion.ToString(), GUILayout.Width(COLWIDTH_POINTY));
            }
            GUILayout.EndHorizontal();

            if (point_time_last != point_time)
            {
                float newTime;
                if (float.TryParse(point_time, out newTime))
                {
                    if (newTime > 100f)
                        newTime = 100f;
                    else if (newTime < 0f)
                        newTime = 0f;

                    point.timePortion = newTime / 100f;
                }
                else if (point_time == "")
                    point.timePortion = 0;

                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
            }
            else if (slide_time != point.timePortion)
            {
                point.timePortion = (float)Math.Round(slide_time, 3);
                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
            }

            if (point_thrust_last != point_thrust)
            {
                float newThrust;
                if (float.TryParse(point_thrust, out newThrust))
                {
                    if (newThrust > 100f)
                        newThrust = 100f;
                    else if (newThrust < thrustCurve.minimumThrust * 100f)
                        newThrust = thrustCurve.minimumThrust * 100f;

                    point.thrustPortion = newThrust / 100f;
                }
                else if (point_thrust == "")
                    point.thrustPortion = 0;

                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
            }
            else if (slide_thrust != point.thrustPortion)
            {
                point.thrustPortion = (float)Math.Round(slide_thrust, 3);
                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
            }
        }

        private void DrawCruve()
        {
            GUILayout.BeginHorizontal();
            {
                //left y axis (thrust)
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    GUILayout.Label("Thrust", TopLabel);
                    GUILayout.Label(module_srb.module_engine.maxThrust.ToString("F0") + " kN", TopLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label((module_srb.module_engine.maxThrust * 0.5f).ToString("F0") + " kN", MidLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label("0 kN", BottomLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();

                //Graph
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(5);
                    GUILayout.Box(curveTexture);
                }
                GUILayout.EndVertical();

                //right y axis (fuel)
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    GUILayout.Label("Fuel", TopLabel);
                    GUILayout.Label(module_srb.fuelMass.ToString("F0") + " t", TopLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label((module_srb.fuelMass * 0.5f).ToString("F0") + " t", MidLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                    GUILayout.Label("0 t", BottomLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Height(LABLES_X_HEIGHT));
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);

                //x axis (time)
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("0 s", LeftLable, GUILayout.Width(GRAPH_WIDTH / 3));
                        GUILayout.Label((module_srb.burnTime * 0.5f).ToString("F1") + " s", CentreLable, GUILayout.Width(GRAPH_WIDTH / 3));
                        GUILayout.Label(module_srb.burnTime.ToString("F1") + " s", RightLable, GUILayout.Width(GRAPH_WIDTH / 3));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Time", CentreLable);
                }
                GUILayout.EndVertical();
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
                //background = new Color((float)y / curveTexture.height * 0.5f, 0.0f, 0.0f);
                background = new Color(0.0f, 0.0f, 0.0f);
                for (x = 0; x < curveTexture.width; ++x)
                {
                    curveTexture.SetPixel(x, y, background);
                }
            }

            for (x = 0; x < curveTexture.width; ++x)
            {
                //Add thrust curve
                val = (int)Math.Round(thrustCurve.Evaluate((float)x / curveTexture.width) * curveTexture.height, MidpointRounding.AwayFromZero);
                for (y = val - curveWidth / 2; y < val + curveWidth / 2 && y < curveTexture.height; ++y)
                {
                    if (y >= 0)
                        curveTexture.SetPixel(x, y, colour_thrust);
                }

                //Add fuel curve
                val = (int)Math.Round(thrustCurve.CalculateFuelPortion((float)x / curveTexture.width) * curveTexture.height, MidpointRounding.AwayFromZero);
                for (y = val - curveWidth / 2; y < val + curveWidth / 2 && y < curveTexture.height; ++y)
                {
                    if (y >= 0)
                        curveTexture.SetPixel(x, y, colour_fuel);
                }
            }

            curveTexture.Apply();
        }
    }
}
