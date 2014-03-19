//#define DEBUG_SRB
//#define DEBUG_UPDATES
//#define DEBUG_CALCULATIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using KSP;

#if DEBUG_CALCULATIONS
using System.Diagnostics;
#endif

namespace IoncrossKerbal_SRB
{
    public class ThrustPoint
    {
        public float timePortion;   //[0, 1]
        public float thrustPortion; //(0, 1]
        public float fuelPortion;   //[0, 1]

        public ThrustPoint()
        {
            timePortion = 0f;
            thrustPortion = 1f;
            fuelPortion = 1f;
        }

        public bool Load(string data)
        {
            float time;
            float thrust;
            float fuel;
            string[] dataSplit = data.Split(' ');

            if (dataSplit.Count() < 3 || !float.TryParse(dataSplit[0], out time) || !float.TryParse(dataSplit[1], out thrust) || !float.TryParse(dataSplit[2], out fuel))
                return false;

            timePortion = time;
            thrustPortion = thrust;
            fuelPortion = fuel;

            CheckBounds();

            return true;
        }

        public string Save()
        {
            return timePortion.ToString() + " " + thrustPortion.ToString() + " " + fuelPortion.ToString();
        }

        private void CheckBounds()
        {
            if (timePortion > 1f)
                timePortion = 1f;
            else if (timePortion < 0f)
                timePortion = 0f;

            if (thrustPortion > 1f)
                thrustPortion = 1f;
            else if (thrustPortion < 0f)
                thrustPortion = 0f;

            if (fuelPortion > 1f)
                fuelPortion = 1f;
            else if (fuelPortion < 0f)
                fuelPortion = 0f;

        }
    }

    public class ThrustCurve : IConfigNode
    {
        public const string THRUST_POINT = "point";
        public const string NODE_THRUST_CURVE = "THRUST_CURVE";
        public const float POINT_TIME_SEP_MIN = 0.01f;

        private float minThrust;
        public float minimumThrust
        {
            get { return minThrust; }
            set
            {
                minThrust = value;

                if (minThrust <= 0f)
                    minThrust = 0.01f;
            }
        }

        public int numPoints
        {
            get { return (null != list_points ? list_points.Count : 0);  }
        }

        public List<ThrustPoint> list_points;


        /************************************************************************\
         * IonResourceData class                                                *
         * Constructors                                                         *
        \************************************************************************/
        public ThrustCurve()
        {
            list_points = new List<ThrustPoint>();
            minThrust = 0.01f;
        }


        /************************************************************************\
         * ThrustCurve class                                                    *
         * Load function                                                        *
        \************************************************************************/
        public void Load(ConfigNode node)
        {
#if DEBUG_SRB
            UnityEngine.Debug.Log("ThrustCurve.Load()");
            UnityEngine.Debug.Log("ThrustCurve.Load(): node\n" + (null != node ? node.ToString() : "null"));
#endif
            list_points = new List<ThrustPoint>();

            string[] values;
            ThrustPoint point;

            if (null != node)
            {
                values = node.GetValues(THRUST_POINT);

                foreach (string line in values)
                {
#if DEBUG_SRB
            UnityEngine.Debug.Log("ThrustCurve.Load(): line \"" + line + "\"");
#endif
                    point = new ThrustPoint();
                    if (point.Load(line))
                        list_points.Add(point);
                }
            }

            //ensure there are at least 2 points and that the first one is at t=0 and the last one at t=1
            if(2 > list_points.Count)
            {
                if (1 == list_points.Count)
                {
                    list_points[0].timePortion = 0f;
                }
                else
                {
                    point = new ThrustPoint();
                    point.timePortion = 0f;
                    point.thrustPortion = 1f;
                    list_points.Add(point);
                }

                point = new ThrustPoint();
                point.timePortion = 1f;
                point.thrustPortion = list_points[0].thrustPortion;
                list_points.Add(point);

            }
            else
            {
                if (0f != list_points[0].timePortion)
                    list_points[0].timePortion = 0f;
                if (1f != list_points[list_points.Count - 1].timePortion)
                    list_points[list_points.Count - 1].timePortion = 1f;
            }            
        }

        /************************************************************************\
         * ThrustCurve class                                                    *
         * Save function                                                        *
        \************************************************************************/
        public void Save(ConfigNode node)
        {
            foreach(ThrustPoint point in list_points)
            {
                node.AddValue(THRUST_POINT, point.Save());
            }
        }



        /************************************************************************\
         * ThrustCurve class                                                    *
         * AddValue function                                                    *
         *                                                                      *
         * Adds a point to the curve.                                           *
         *                                                                      *
         * timePortion:     relative time into the burn [0, 1].                 *
         * thrustPortion:   relative thrust at t [0, 1].                        *
        \************************************************************************/
        public void AddValue(float timePortion, float thrustPortion)
        {
            ThrustPoint p = new ThrustPoint();
            p.timePortion = timePortion;
            p.thrustPortion = thrustPortion;

            int i;
            for (i = 0; i < list_points.Count && list_points[i].timePortion < timePortion; ++i) ;
            list_points.Insert(i, p);

            CalculateFuelPoints();
        }

        public void Remove(ThrustPoint point)
        {
            list_points.Remove(point);
        }



        /************************************************************************\
         * ThrustCurve class                                                    *
         * UpdateCurve function                                                 *
         *                                                                      *
         * Scales the thrust values in the curve so average thrust is equal to  *
         * thrustPortion.                                                       *
         *                                                                      *
         * thrustPortion:   relative thrust set the curve's average to [0, 1].  *
         *                                                                      *
         * Returns: average thrust.                                             *
        \************************************************************************/
        public float ScaleCurve(float thrustPortion)
        {
#if DEBUG_CALCULATIONS
            UnityEngine.Debug.Log("ThrustCurve.ScaleCurve(" + thrustPortion + ")");
#endif
            float modifier;
            float high = 0f;
            float low = 1f;
            float avgThrust = CalculateAverageThrust();

            if (thrustPortion < minThrust)
                thrustPortion = minThrust;

            if (0 != avgThrust)
            {
                modifier = thrustPortion / avgThrust;

                for (int i = 0; i < list_points.Count; ++i)
                {
                    list_points[i].thrustPortion *= modifier;
#if DEBUG_CALCULATIONS
                    UnityEngine.Debug.Log("ThrustCurve.ScaleCurve(): point " + i + " | thrustPortion " + list_points[i].thrustPortion);
#endif
                    high = Math.Max(high, list_points[i].thrustPortion);
                    low = Math.Min(low, list_points[i].thrustPortion);
                }
            }
            else
            {
                for (int i = 0; i < list_points.Count; ++i)
                {
                    list_points[i].thrustPortion = thrustPortion;
#if DEBUG_CALCULATIONS
                    UnityEngine.Debug.Log("ThrustCurve.ScaleCurve(): point " + i + " | thrustPortion " + list_points[i].thrustPortion);
#endif

                    high = Math.Max(high, list_points[i].thrustPortion);
                    low = Math.Min(low, list_points[i].thrustPortion);
                }
            }


            //Check for this moving the thrust out of bounds
            //If so move it back
            modifier = 1f;
            if (high > 1f)
                modifier = 1f / high;
            else if (low <= minThrust && 0 != low)
                modifier = minThrust / low;

#if DEBUG_CALCULATIONS
            UnityEngine.Debug.Log("ThrustCurve.ScaleCurve(): high " + high + " | low " + low + " | modifier " + modifier);
#endif
            if (Math.Abs(1f - modifier) > 0.0001)
            {
                for (int i = 0; i < list_points.Count; i++)
                {
                    list_points[i].thrustPortion *= modifier;
#if DEBUG_CALCULATIONS
                    UnityEngine.Debug.Log("ThrustCurve.ScaleCurve(): point " + i + " | thrustPortion " + list_points[i].thrustPortion);
#endif
                }
            }


            CalculateFuelPoints();

            return CalculateAverageThrust();
        }

        /************************************************************************\
         * ThrustCurve class                                                    *
         * CalculateAverageThrust function                                      *
         *                                                                      *
         * Calculates the average thrust for the whole thrust cruve.            *
         *                                                                      *
         * Returns: The average thrust of the curve over time [0, 1].           *
        \************************************************************************/
        public float CalculateAverageThrust()
        {
            float averageThrust = 0;
            float averageThrust_interval = 0;
            float weight_interval = 0;

            for (int i = 1; i < list_points.Count; ++i)
            {
                //weighted average thrust for this interval
                weight_interval = (list_points[i].timePortion - list_points[i - 1].timePortion);
                averageThrust_interval = (list_points[i].thrustPortion + list_points[i - 1].thrustPortion) * 0.5f;

                averageThrust += weight_interval * averageThrust_interval;
            }

            return averageThrust;
        }

        /************************************************************************\
         * ThrustCurve class                                                    *
         * CalculateFuelPortions function                                       *
         *                                                                      *
         * Calculates the fuel portion for each point.  The fuel portion is     *
         * what is used to evaluate the thrust durring flight (to account for   *
         * changes in ISP with altitude).                                       *
        \************************************************************************/
        public void CalculateFuelPoints()
        {
            float fuelRemaining = 1;
            float averageThrust_interval = 0;
            float time_interval = 0;
            float averageThrust = CalculateAverageThrust();

            if (list_points.Count > 0)
            {
                list_points[0].fuelPortion = fuelRemaining;

                for (int i = 1; i < list_points.Count; ++i)
                {
                    time_interval = (list_points[i].timePortion - list_points[i - 1].timePortion);
                    averageThrust_interval = (list_points[i].thrustPortion + list_points[i - 1].thrustPortion) * 0.5f;

                    //Fuel used up is the relative thrust of this interval compare with the average * the duration of this interval compared with the whole (always 1)
                    fuelRemaining -= averageThrust_interval / averageThrust * time_interval;

                    list_points[i].fuelPortion = fuelRemaining;
                }
            }
        }

        /************************************************************************\
         * ThrustCurve class                                                    *
         * CalculateFuelPortion function                                        *
         *                                                                      *
         * Calculates the portion of remaining fuel at timePortion.             *
         *                                                                      *
         * timePortion: time to calculate remaining fuel portion for [0, 1].    *
         *                                                                      *
         * Returns: The fuel portion at timePortion [0, 1].                     *
        \************************************************************************/
        public float CalculateFuelPortion(float timePortion)
        {
            int i;
            ThrustPoint p0, p1;
            float value = -1;

            if (list_points.Count > 0)
            {
                //get the indexes before and after timePortion
                //timePortion shouldn't be less than 0 or more than 1
                for (i = 1; i < list_points.Count && list_points[i].timePortion < timePortion; ++i) ;

                p1 = list_points[i];
                p0 = list_points[i - 1];

                if (i >= list_points.Count)
                    p1 = list_points[i - 1];

                /* fuelRate = intervalAverageThrust / averageThrust
                 * fuelUse = intervalAverageThrust / averageThrust * timeInterval;
                 * intervalAverageThrust = (thrust0 + thrust) / 2
                 * fuelRemaining = fuel0 - (thrust0 + thrust)  / (2 * averageThrust) * (time - time0)
                 */
                value = p0.fuelPortion - (p0.thrustPortion + EvaluateInterval(timePortion, p0, p1)) / (2 * CalculateAverageThrust()) * (timePortion - p0.timePortion);
            }

            return value;
        }


        /************************************************************************\
         * ThrustCurve class                                                    *
         * Evaluate function                                                    *
         *                                                                      *
         * Evaluates for the thrust at timePortion.                             *
         *                                                                      *
         * timePortion: Relative time into the burn [0, 1].                     *
         *                                                                      *
         * Returns: The value of the thrust curve at t [0, 1].                  *
        \************************************************************************/
        public float Evaluate(float timePortion)
        {
            int i;
            ThrustPoint p0, p1; 
            float value = -1;

            if (list_points.Count > 0)
            {
                //get the indexes before and after timePortion
                //timePortion shouldn't be less than 0 or more than 1
                for (i = 1; i < list_points.Count && list_points[i].timePortion < timePortion; ++i);

                p1 = list_points[i];
                p0 = list_points[i - 1];

                if (i >= list_points.Count)
                    p1 = list_points[i - 1];

                value = EvaluateInterval(timePortion, p0, p1);
            }

            return value;
        }

        /************************************************************************\
         * ThrustCurve class                                                    *
         * EvaluateInterval function                                            *
         *                                                                      *
         * Evaluates for the thrust at timePortion using the function defined   *
         * by p0 and p1.                                                        *
         *                                                                      *
         * timePortion: Relative time into the burn [0, 1].                     *
         * p0:          First point.  Should be below timePortion.              *
         * p1:          Second point.  Should be above timePortion.             *
         *                                                                      *
         * Returns: The value of the thrust curve at timePortion [0, 1].        *
        \************************************************************************/
        private float EvaluateInterval(float timePortion, ThrustPoint p0, ThrustPoint p1)
        {
            //slope = (p0.thrustPortion - p1.thrustPortion) / (p0.timePortion - p1.timePortion)
            //value = (timePortion - p0.timePortion) * slope + p0.thrustPortion;
            //if time interval is 0, slope is set to 0 and forumula evaluates to p0.thrustPortion
            return (timePortion - p0.timePortion) * (p0.timePortion == p1.timePortion ? 0 : (p0.thrustPortion - p1.thrustPortion) / (p0.timePortion - p1.timePortion)) + p0.thrustPortion;
        }


        /************************************************************************\
         * ThrustCurve class                                                    *
         * EvaluateFuel function                                                *
         *                                                                      *
         * Evaluates for the thrust when remaining fuel equals fuelPortion.     *
         *                                                                      *
         * fuelPortion: Relative fuel remaining [0, 1].                         *
         *                                                                      *
         * Returns: The value of the thrust curve when fuel = fuelPortion [0, 1]*
        \************************************************************************/
        public float EvaluateFuel(float fuelPortion)
        {
#if DEBUG_CALCULATIONS
            UnityEngine.Debug.Log("ThrustCurve.EvaluateFuel(" + fuelPortion + ")");
#endif
            int i;
            ThrustPoint p0, p1;
            float a, b, c;
            float slope, averageThrust;
            float sqrt;
            float timePortion;
            float value = -1;



            if (list_points.Count > 0)
            {
                //get the indexes before and after fuelPortion
                //fuelPortion shouldn't be less than 0 or more than 1
                for (i = 1; i < list_points.Count && list_points[i].fuelPortion > fuelPortion; ++i);
                
                p1 = list_points[i];
                p0 = list_points[i - 1];

                if (i >= list_points.Count)
                    p1 = list_points[i - 1];

                if(p0.timePortion != p1.timePortion)
                    slope = (p0.thrustPortion - p1.thrustPortion) / (p0.timePortion - p1.timePortion);
                else
                    slope = 0;

                //if slope is zero or points are on top of each other (invalid but check just in case)
                if (p0.timePortion == p1.timePortion || p0.thrustPortion == p1.thrustPortion)
                {
                    /* fuelRate = thrust / averageThrust
                     * thrust = thrust0
                     * 
                     * fuelRemaining = fuel0 - fuelUsed
                     * fuelUsed = thrust0/averageThrust * (time - time0)
                     * fuelRemaining = fuel0 - thrust0/averageThrust * (time - time0)
                     * time = (fuel0 - fuelRemaining) * averageThrust/thrust0 + time0
                     */

                    value = EvaluateInterval((p0.fuelPortion - fuelPortion) * CalculateAverageThrust() / p0.thrustPortion + p0.timePortion, p0, p1);
#if DEBUG_CALCULATIONS
                    UnityEngine.Debug.Log("ThrustCurve.EvaluateFuel(): p0 (" + p0.timePortion + ", " + p0.thrustPortion + ") | p1 (" + p1.timePortion + ", " + p1.thrustPortion + ") | timePortion " + (p0.fuelPortion - fuelPortion) * CalculateAverageThrust() / p0.thrustPortion + p0.timePortion + " | value " + value);
#endif
                }
                else
                {
                    slope = (p0.thrustPortion - p1.thrustPortion) / (p0.timePortion - p1.timePortion);

                    /*
                     * fuelRate = thrust / averageThrust
                     * 
                     * thrustSlope = (thrust1 - thrust0) / (time1 - time0)
                     * thrust0 = thrustSlope * time0 + thrustIntercept
                     * thrustIntercept = thrust0 - thrustSlope * time0
                     * 
                     * thrust = time * thrustSlope + thrustIntercept
                     * thrust = time * thrustSlope + thrust0 - thrustSlope * time0
                     * thrust = (time - time0) * thrustSlope + thrust0
                     * 
                     * fuelRate = ((time - time0) * thrustSlope + thrust0) / averageThrust
                     * fuelUsed = intergal of fuelRate with respect to time
                     * 
                     * fuelRate = ((time - time0) * thrustSlope + thrust0) / averageThrust
                     * fuelRate = (time * thrustSlope - time0 * thrustSlope + thrust0) / averageThrust
                     * fuelRate = time*thrustSlope/averageThrust + (thrust0 - time0*thrustSlope)/averageThrust
                     * 
                     * taking the definite intergal here so it's intergal at time - intergal at time0
                     * fuelUsed = time*time * thrustSlope/(2*averageThrust)  +  time * (thrust0 - time0*thrustSlope)/averageThrust - time0*time0 * thrustSlope/(2*averageThrust)  -  time0 * (thrust0 - time0*thrustSlope)/averageThrust
                     *                                                                                                             - time0*time0 * thrustSlope/(2*averageThrust)  -  time0 * (thrust0 - time0*thrustSlope)/averageThrust
                     *                                                                                                             - time0/averageThrust * time0 * thrustSlope/2  -  time0/averageThrust * (thrust0 - time0*thrustSlope)
                     *                                                                                                             - time0/averageThrust * (time0 * thrustSlope/2  +  (thrust0 - time0*thrustSlope))
                     *                                                                                                             - time0/averageThrust * (time0 * thrustSlope/2  +  thrust0 - time0*thrustSlope)
                     *                                                                                                             - time0/averageThrust * (time0 * thrustSlope * -1/2  +  thrust0)
                     *                                                                                                             - time0/averageThrust * (thrust0 - time0 * thrustSlope / 2)
                     *
                     * fuelRemaining = fuel0 - fuelUsed
                     * fuelRemaining = fuel0 - time*time * thrustSlope/(2*averageThrust)  -  time * (thrust0 - time0*thrustSlope)/averageThrust + time0/averageThrust * (thrust0 - time0 * thrustSlope / 2)
                     * 0 = time*time * -thrustSlope/(2*averageThrust)  +  time * (thrustSlope*time0 - thrust0)/averageThrust  +  fuel0 - fuelRemaining + time0/averageThrust * (thrust0 - time0 * thrustSlope / 2)
                     * 0 = time*time * -thrustSlope/2  +  time * (thrustSlope*time0 - thrust0)  +  fuel0*averageThrust - fuelRemaining*averageThrust + time0 * (thrust0 - time0 * thrustSlope / 2)
                     * 0 = time*time * -thrustSlope/2  +  time * (thrustSlope*time0 - thrust0)  +  averageThrust(fuel0 - fuelRemaining) + time0 * (thrust0 - time0 * thrustSlope / 2)
                     * 
                     * 
                     * time = ( (thrustSlope*time0 - thrust0) +/- sqrt((thrustSlope*time0 - thrust0) * (thrustSlope*time0 - thrust0) - 4 * -thrustSlope/2 * averageThrust(fuel0 - fuelRemaining) + time0 * (thrust0 - time0 * thrustSlope / 2) )) / (-thrustSlope)
                     * 
                     * 
                     */

#if DEBUG_CALCULATIONS
                    Stopwatch sqrtTimer = Stopwatch.StartNew();
#endif
                    averageThrust = CalculateAverageThrust();
                    a = -slope * 0.5f ;
                    b = slope * p0.timePortion - p0.thrustPortion;
                    c = averageThrust * (p0.fuelPortion - fuelPortion) + p0.timePortion * (p0.thrustPortion - p0.timePortion * slope * 0.5f);

                    sqrt = (float)Math.Sqrt(b * b - 4 * a * c);
                    timePortion = (-b - sqrt) / (2 * a);

                    if (timePortion < p0.timePortion || timePortion > p1.timePortion)
                        timePortion = (-b + sqrt) / (2 * a);

                    value = EvaluateInterval(timePortion, p0, p1);

#if DEBUG_CALCULATIONS
                    sqrtTimer.Stop();
                    UnityEngine.Debug.Log("ThrustCurve.EvaluateFuel(): calculation time: " + sqrtTimer.ElapsedMilliseconds + " ms | value " + value);
                    UnityEngine.Debug.Log("ThrustCurve.EvaluateFuel(): p0 (" + p0.timePortion + ", " + p0.thrustPortion + ", " + p0.fuelPortion + ") | p1 (" + p1.timePortion + ", " + p1.thrustPortion + ", " + p1.fuelPortion + ") | slope " + slope);
                    UnityEngine.Debug.Log("ThrustCurve.EvaluateFuel(): averageThrust " + averageThrust + " | a " + a + " | b " + b + " | c " + c + " | sqrt " + sqrt + " | timePortion " + timePortion);
#endif
                }
            }

            return value;
        }
    }
}
