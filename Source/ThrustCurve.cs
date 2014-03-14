//#define DEBUG_SRB
#define DEBUG_UPDATES
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
    public class ThrustPoint
    {
        public float fuelPortion;
        public float timePortion;
        public float thrustPortion;

        public ThrustPoint(float t, float thrust)
        {
            fuelPortion = 0;
            timePortion = t;
            thrustPortion = thrust;

            if (timePortion > 1f)
                timePortion = 1f;
            else if (timePortion < 0f)
                timePortion = 0f;

            if (thrustPortion > 1f)
                thrustPortion = 1f;
            else if (thrustPortion < 0f)
                thrustPortion = 0f;
        }
    }

    public class ThrustCurve
    {
        public List<ThrustPoint> list_points;


        public ThrustCurve(float initalThrust)
        {
            list_points = new List<ThrustPoint>();

            AddValue(0f, initalThrust);
            AddValue(1f, initalThrust);
        }


        /************************************************************************\
         * ThrustCurve class                                                    *
         * AddValue function                                                    *
         *                                                                      *
         * Adds a point to the curve.                                           *
         *                                                                      *
         * t:       relative time into the burn [0, 1].                         *
         * thrust:  relative thrust at t [0, 1].                                *
        \************************************************************************/
        public void AddValue(float t, float thrust)
        {
            ThrustPoint p = new ThrustPoint(t, thrust);

            int i;
            for (i = 0; i < list_points.Count && list_points[i].timePortion < t; ++i) ;
            list_points.Insert(i, p);
        }

        public void ModifiyThrust(float modifier)
        {
            for (int i = 0; i < list_points.Count; i++)
            {
                list_points[i].thrustPortion *= modifier;
                if (list_points[i].thrustPortion > 1f)
                    list_points[i].thrustPortion = 1f;
                else if (list_points[i].thrustPortion < 0f)
                    list_points[i].thrustPortion = 0f;
            }
        }

        public void ResetThrust(float value)
        {
            for (int i = 0; i < list_points.Count; i++)
            {
                list_points[i].thrustPortion = value;
                if (list_points[i].thrustPortion > 1f)
                    list_points[i].thrustPortion = 1f;
                else if (list_points[i].thrustPortion < 0f)
                    list_points[i].thrustPortion = 0f;
            }
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
        public void CalculateFuelPortions()
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
         * Evaluate function                                                    *
         *                                                                      *
         * Evaluates t based on the thrust curve.                               *
         *                                                                      *
         * t: relative time into the burn [0, 1].                               *
         *                                                                      *
         * Returns: The value of the thrust curve at t [0, 1].                  *
        \************************************************************************/
        public float Evaluate(float timePortion)
        {
            int i;
            int i1, i2;
            float value = -1;

            i1 = i2 = 0;

            if (list_points.Count > 0)
            {
                //get the indexes before and after timePortion
                //timePortion shouldn't be less than 0 or more than 1
                for (i = 1; i < list_points.Count && list_points[i].timePortion < timePortion; ++i);

                i2 = i;
                i1 = i - 1;

                if (i2 >= list_points.Count)
                    --i2;

                //slope = (list_points[i].thrustPortion - list_points[i - 1].thrustPortion) / (list_points[i].timePortion - list_points[i - 1].timePortion)
                //value = timePortion * slope + list_points[i - 1].thrustPortion;
                value = timePortion * (list_points[i].thrustPortion - list_points[i - 1].thrustPortion) / (list_points[i].timePortion - list_points[i - 1].timePortion) + list_points[i - 1].thrustPortion;
            }

            return value;
        }


        public float EvaluateFuel(float fuelPortion)
        {
            int i;
            int i1, i2;
            float value = -1;

            i1 = i2 = 0;

            if (list_points.Count > 0)
            {
                /*
                 * Fuel used up is the relative thrust of this interval compare with the average * the duration of this interval compared with the whole (always 1)
                 * fuelUsed_interval = averageThrust_interval / averageThrust * time_interval;
                 * fuelRate = thrust / averageThrust
                 * 
                 * thrust slope = (thrust1 - thrust0) / (time1 - time0)
                 * thrust0 = thrust slope * time0 + thrust intercept
                 * thrust intercept = thrust0 - thrust slope * time0
                 * 
                 * thrust = time * thrust slope + thrust intercept
                 * thrust = time * (thrust1 - thrust0) / (time1 - time0) + thrust0 - (thrust1 - thrust0) / (time1 - time0) * time0
                 * thrust = (time - time0) (thrust1 - thrust0) / (time1 - time0) + thrust0
                 * 
                 * fuelRate = ((time - time0) (thrust1 - thrust0) / (time1 - time0) + thrust0) / averageThrust
                 * fuelUsed = intergal of fuelRate with respect to time
                 * 
                 * fuelRate = (time - thrust0) * (thrust1 - thrust0) / ((time1 - time0) * averageThrust) + thrust0 / averageThrust
                 * fuelRemaining = fuel0 - ( (thrust1 - thrust0) * time * time / (2 * (time1-time0) * averageThrust) ) + ( (thrust1-thrust0) * time0 * time / ((time1 - time0) * averageThrust) ) - thrust0 * time / averageThrust
                 * 
                 * time = fuelUsed
                 *
                 * fuel = fuelStart - fuelUsed
                 * 
                 */
            }

            return value;
        }
    }
}
