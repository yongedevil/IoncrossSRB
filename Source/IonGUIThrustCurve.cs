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
        public const int COLWIDTH_POINTFIELD = 80;

        public const int GRAPH_HEIGHT = 200;
        public const int GRAPH_WIDTH = 400;
        public const int LABLES_Y_WIDTH = 35;
        public const int LABLES_X_HEIGHT = 35;

        public const float POINT_TIME_SEP_MIN = 0.01f;
        public const int MAX_POINTS = 10;

        public IonModuleSRB module_srb;
        public ThrustCurve thrustCurve;

        public Rect windowPos;
        public GUIStyle windowStyle;
        public GUIStyle BaseLabel;

        public Color colour_background;
        public Color colour_thrust;
        public Color colour_fuel;

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

            colour_background = new Color(0.0f, 0.0f, 0.0f);
            colour_thrust = Color.red;
            colour_fuel = Color.magenta;

            windowPos = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
            windowStyle = new GUIStyle(HighLogic.Skin.window);

            curveTexture = new Texture2D(GRAPH_WIDTH, GRAPH_HEIGHT);
            curveWidth = 5;


            //Set Styles
            BaseLabel = new GUIStyle();
            BaseLabel.normal.textColor = HighLogic.Skin.window.normal.textColor;

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

        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawInfo function                                                    *
         *                                                                      *
         * Draws the info section of the window.                                *
        \************************************************************************/
        private void DrawInfo()
        {
            DrawPoints();
        }

        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawPoints function                                                  *
         *                                                                      *
         * Draws the list of ThrustPoints info to the window.                   *
        \************************************************************************/
        private void DrawPoints ()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);
                GUILayout.Label("Point", GUILayout.Width(COLWIDTH_POINTNUM));
                GUILayout.Label("Time (%)", GUILayout.Width(COLWIDTH_POINTFIELD));
                GUILayout.Label("Thrust (%)", GUILayout.Width(COLWIDTH_POINTFIELD));
                GUILayout.Label("Fuel (%)", GUILayout.Width(COLWIDTH_POINTFIELD));
            }
            GUILayout.EndHorizontal();


            for (int i = 0; i < thrustCurve.list_points.Count; ++i)
            {
                float min = 0;
                float max = 1;
                ThrustPoint point = thrustCurve.list_points[i];

                if (i > 0)
                {
                    min = thrustCurve.list_points[i - 1].timePortion + POINT_TIME_SEP_MIN;
                    if (min > thrustCurve.list_points[i].timePortion)
                        min = thrustCurve.list_points[i].timePortion;
                }

                if (i + 1 < thrustCurve.list_points.Count)
                {
                    max = thrustCurve.list_points[i + 1].timePortion - POINT_TIME_SEP_MIN;
                    if (max < thrustCurve.list_points[i].timePortion)
                        max = thrustCurve.list_points[i].timePortion;
                }

                DrawPoint(i, point, min, max);
            }
        }

        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawPoint function                                                   *
         *                                                                      *
         * Draws a single ThrustPoint to the window.                            *
         *                                                                      *
         * num:     number of this point.                                       *
         * point:   point to draw info for.                                     *
         * timeMin: minimum timePortion value for this point.                   *
         * timeMax: maximum timePortion value for this point.                   *
        \************************************************************************/
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

                //Time
                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTFIELD));
                {
                    //if this is the first or last point use a label instead of a textfield
                    if (0 != point.timePortion && 1 != point.timePortion)
                    {
                        DrawFieldSlider(ref point.timePortion, timeMin, timeMax, true);
                    }
                    else
                    {
                        GUILayout.Label(point_time + "%");
                    }

                }
                GUILayout.EndVertical();

                //Thrust
                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTFIELD));
                {
                    DrawFieldSlider(ref point.thrustPortion, thrustCurve.minimumThrust, 1f, true);
                }
                GUILayout.EndVertical();

                //Fuel
                GUILayout.Label((point.fuelPortion * 100f).ToString("F1") + "%", GUILayout.Width(COLWIDTH_POINTFIELD));


                //remove point button
                //If not first or last point
                if (0 != point.timePortion && 1 != point.timePortion)
                {
                    if (GUILayout.Button("Del"))
                    {
                        thrustCurve.Remove(point);
                        windowPos.height = 100;
                        thrustCurve.CalculateFuelPoints();
                        UpdateCruveTexture();
                    }
                }

                //Insert point button
                //POINT_TIME_SEP_MIN is multiplied by 3 to make sure there's enough space for a new point (*2) plus a little extra
                if (point.timePortion > timeMin + POINT_TIME_SEP_MIN * 3 && thrustCurve.list_points.Count < MAX_POINTS)
                {
                    if (GUILayout.Button("Insert"))
                    {
                        float newPointTime = (point.timePortion - POINT_TIME_SEP_MIN + timeMin) * 0.5f;
                        thrustCurve.AddValue(newPointTime, thrustCurve.Evaluate(newPointTime));
                    }
                }
            }
            GUILayout.EndHorizontal();
        }


        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawTextFieldAndSlider function                                      *
         *                                                                      *
         * Draws a textFeild and HorizontalSlider to the window for field.      *
         * Checks for changes and updates fields.                               *
         *                                                                      *
         * field:       reference to the variable to update.                    *
         * minVal:      minimum value for the slider.                           *
         * maxVal:      maximum value for the slider.                           *
         * isPercent:   if true the TextField is displayed by a percent.        *
        \************************************************************************/
        private void DrawFieldSlider(ref float field, float minVal, float maxVal, bool isPercent)
        {
            string strVal = ((isPercent ? 100f : 1f) * field).ToString("F1");
            string strVal_initial = strVal;
            float slideVal = field;
            float newVal;

            GUILayout.BeginHorizontal();
            {
                strVal = GUILayout.TextField(strVal, GUILayout.Width(COLWIDTH_POINTFIELD * 0.5f));
                if(isPercent)
                    GUILayout.Label("%");
            }
            GUILayout.EndHorizontal();

            slideVal = GUILayout.HorizontalSlider(slideVal, minVal, maxVal);

            //Check for changes
            if (strVal_initial != strVal)
            {
                if (float.TryParse(strVal, out newVal))
                {
                    if(isPercent)
                        newVal /= 100f;

                    if (newVal - maxVal > 0.0001)
                        newVal = maxVal;
                    else if (minVal - newVal > 0.0001 )
                        newVal = minVal;

                    field = newVal;
                }
                else if (strVal == "")
                {
                    field = minVal;
                }

                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
                thrustCurve.CalculateFuelPoints();
                UpdateCruveTexture();
            }
            else if (slideVal != field)
            {
                field = (float)Math.Round(slideVal, 3);
                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
                thrustCurve.CalculateFuelPoints();
                UpdateCruveTexture();
            }
        }
    

        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawCruve function                                                   *
         *                                                                      *
         * Draws the cruve section of the window.                               *
        \************************************************************************/
        private void DrawCruve()
        {
            GUILayout.BeginHorizontal();
            {
                //left y axis labels (thrust)
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    BaseLabel.normal.textColor = colour_thrust;
                    BaseLabel.alignment = TextAnchor.UpperCenter;
                    GUILayout.Label("Thrust", BaseLabel);

                    BaseLabel.normal.textColor = Color.white;
                    GUILayout.Label(module_srb.module_engine.maxThrust.ToString("F0") + " kN", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));

                    BaseLabel.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label((module_srb.module_engine.maxThrust * 0.5f).ToString("F0") + " kN", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));

                    BaseLabel.alignment = TextAnchor.LowerCenter;
                    GUILayout.Label("0 kN", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();

                //Graph
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(15);
                    GUILayout.Box(curveTexture);
                }
                GUILayout.EndVertical();

                //right y axis labels (fuel)
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    BaseLabel.normal.textColor = colour_fuel;
                    BaseLabel.alignment = TextAnchor.UpperCenter;
                    GUILayout.Label("Fuel", BaseLabel);

                    BaseLabel.normal.textColor = Color.white;
                    GUILayout.Label(module_srb.fuelMass.ToString("F0") + " t", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));

                    BaseLabel.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label((module_srb.fuelMass * 0.5f).ToString("F0") + " t", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));

                    BaseLabel.alignment = TextAnchor.LowerCenter;
                    GUILayout.Label("0 t", BaseLabel, GUILayout.Height(GRAPH_HEIGHT / 3));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Height(LABLES_X_HEIGHT));
            {
                GUILayout.Space(LABLES_Y_WIDTH + 15);

                //bottom x axis labels (time)
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        BaseLabel.normal.textColor = Color.white;
                        BaseLabel.alignment = TextAnchor.MiddleLeft;
                        GUILayout.Label("0 s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));


                        BaseLabel.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label((module_srb.burnTime * 0.5f).ToString("F1") + " s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));

                        BaseLabel.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(module_srb.burnTime.ToString("F1") + " s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));
                    }
                    GUILayout.EndHorizontal();

                    BaseLabel.normal.textColor = HighLogic.Skin.window.normal.textColor;
                    BaseLabel.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Time", BaseLabel);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();


        }

        public void UpdateCruveTexture()
        {
            int x, y;
            int val;

            for (y = 0; y < curveTexture.height; ++y)
            {
                //background = new Color((float)y / curveTexture.height * 0.5f, 0.0f, 0.0f);
                for (x = 0; x < curveTexture.width; ++x)
                {
                    curveTexture.SetPixel(x, y, colour_background);
                }
            }

            for (x = 0; x < curveTexture.width; ++x)
            {

                //Add fuel curve
                val = (int)Math.Round(thrustCurve.CalculateFuelPortion((float)x / curveTexture.width) * curveTexture.height, MidpointRounding.AwayFromZero);
                for (y = val - curveWidth / 2; y < val + curveWidth / 2 && y < curveTexture.height; ++y)
                {
                    if (y >= 0)
                        curveTexture.SetPixel(x, y, colour_fuel);
                }

                //Add thrust curve
                val = (int)Math.Round(thrustCurve.Evaluate((float)x / curveTexture.width) * curveTexture.height, MidpointRounding.AwayFromZero);
                for (y = val - curveWidth / 2; y < val + curveWidth / 2 && y < curveTexture.height; ++y)
                {
                    if (y >= 0)
                        curveTexture.SetPixel(x, y, colour_thrust);
                }
            }

            curveTexture.Apply();
        }
    }
}
