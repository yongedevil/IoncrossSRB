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
        public const int COLWIDTH_POINTTEXTFIELD = 80;

        public const int COLWIDTH_POINTX = 80;
        public const int COLWIDTH_POINTY = 80;
        public const int COLWIDTH_POINTZ = 80;

        public const int GRAPH_HEIGHT = 200;
        public const int GRAPH_WIDTH = 400;
        public const int LABLES_Y_WIDTH = 35;
        public const int LABLES_X_HEIGHT = 35;

        public IonModuleSRB module_srb;
        public ThrustCurve thrustCurve;

        public Rect windowPos;

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
            GUIStyle BaseLabel = new GUIStyle();
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
                GUILayout.Label("Time (%)", GUILayout.Width(COLWIDTH_POINTX));
                GUILayout.Label("Thrust (%)", GUILayout.Width(COLWIDTH_POINTY));
                GUILayout.Label("Fuel (%)", GUILayout.Width(COLWIDTH_POINTZ));
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
                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTX));
                {
                    //if this is the first or last point use a label instead of a textfield
                    if (0 != point.timePortion && 1 != point.timePortion)
                    {
                        DrawTextFieldAndSlider(point_time, slide_time, timeMin, timeMax);
                    }
                    else
                    {
                        GUILayout.Label(point_time + "%");
                    }

                }
                GUILayout.EndVertical();

                //Thrust
                GUILayout.BeginVertical(GUILayout.Width(COLWIDTH_POINTY));
                {
                    DrawTextFieldAndSlider(point_thrust, slide_thrust, thrustCurve.minimumThrust, 1);
                }
                GUILayout.EndVertical();

                //Fuel
                GUILayout.Label((point.fuelPortion * 100f).ToString("F1") + "%", GUILayout.Width(COLWIDTH_POINTFIELD));
            }
            GUILayout.EndHorizontal();

            //Update values if they have been changed
            CheckAndUpdate(point_time, point_time_last, 100f, slide_time, ref point.timePortion, timeMin + 0.01, timeMax - 0.01);
            CheckAndUpdate(point_thrust, point_thrust_last, 100f, slide_thrust, ref point.thrustPortion, thrustCurve.minimumThrust, 1f);
        }


        /************************************************************************\
         * IonGUIGenerator class                                                *
         * DrawTextFieldAndSlider function                                      *
         *                                                                      *
         * Draws a textFeild and HorizontalSlider to the window.                *
         *                                                                      *
         * strVal:          String to display in the TextField.                 *
         * slideVal:        Value to set the slider to.                         *
         * minVal:          minimum value for the slider.                       *
         * maxVal:          maximum value for the slider.                       *
        \************************************************************************/
        private void DrawTextFieldAndSlider(ref string strVal, ref float slideVal, float minVal, float maxVal)
        {
            GUILayout.BeginHorizontal();
            {
                strVal = GUILayout.TextField(strVal, GUILayout.Width(COLWIDTH_POINTTEXTFIELD) * 0.5f);
                GUILayout.Label("%");
            }
            GUILayout.EndHorizontal();

            slideVal = GUILayout.HorizontalSlider(slideVal, minVal, maxVal);
        }


        /************************************************************************\
         * IonGUIGenerator class                                                *
         * CheckAndUpdate function                                              *
         *                                                                      *
         * Checks string and slider values for changes and updates them if      *
         * necessary.                                                           *
         *                                                                      *
         * strVal:          String returned by the TextField.                   *
         * strVal_origonal: String passed into the TextField.                   *
         * strModifier:     Ratio of the string value to the field value.       *
         * slideVal:        Value returned by the HorizontalSlider.             *
         * field:           reference to the variable to update.                *
         * minVal:          minimum value for this field.                       *
         * maxVal:          maximum value for this field.                       *
        \************************************************************************/
        private void CheckAndUpdate(string strVal, string strVal_origonal, float strModifier, ref float field, float slideVal, float minVal, float maxVal)
        {
            float newVal;

            if (strVal_origonal != strVal)
            {
                if(float.TryParse(strVal, out newVal))
                {
                    newVal /= strModifier;

                    if (newVal > maxVal)
                        newVal = maxVal;
                    else if (newVal < minVal)
                        newVal = minVal;

                    field = newVal;
                }
                else if (strVal == "")
                {
                    field = minVal;
                }
            }
            else if (slideVal != field)
            {
                field = (float)Math.Round(slideVal, 3);
                module_srb.thrustPercent = thrustCurve.CalculateAverageThrust() * 100f;
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
                    GUILayout.Space(5);
                    GUILayout.Box(curveTexture);
                }
                GUILayout.EndVertical();

                //right y axis labels (fuel)
                GUILayout.BeginVertical(GUILayout.Width(LABLES_Y_WIDTH));
                {
                    BaseLabel.normal.textColor = colour_fuel;
                    BaseLabel.alignment = TextAnchor.UpperCenter;

                    GUILayout.Label("Fuel", BaseLabel);
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
                        BaseLabel.normal.textColor = HighLogic.Skin.window.normal.textColor;
                        BaseLabel.alignment = TextAnchor.MiddleLeft;
                        GUILayout.Label("0 s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));


                        BaseLabel.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label((module_srb.burnTime * 0.5f).ToString("F1") + " s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));

                        BaseLabel.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(module_srb.burnTime.ToString("F1") + " s", BaseLabel, GUILayout.Width(GRAPH_WIDTH / 3));
                    }
                    GUILayout.EndHorizontal();

                    BaseLabel.alignment = TextAnchor.MiddleCenter;
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
