using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using KSP;

namespace IoncrossKerbal_SRB
{
    public class ParseHelper
    {
        /************************************************************************\
         * ParseHelper class                                                    *
         * ReadValue function                                                   *
         *                                                                      *
         * Attempts to read valName from node.  If successful it returns the    *
         * value read.  Otherwise it returns valDefault.                        *
         *                                                                      *
         * If successful val is set to the value read from node.                *
         * Otherwise val is left unchanged.                                     *
         *                                                                      *
         * node:        ConfigNode to read from.                                *
         * valName:     Value to read from node.                                *
         * valNames:    Array of strings to attempt to read. Reads the first    *
         *              one found successfully.                                 *
         * val:         Varabile to store read data.                            *
         *                                                                      *
         * Returns: True if successful.                                         *
         *          False otherwise.                                            *
        \************************************************************************/
        public static bool ReadValue(ConfigNode node, string valName, ref bool val)
        {
            if (node.HasValue(valName))
            {
                val = "TRUE" == node.GetValue(valName).ToUpper();
                return true;
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string valName, ref int val)
        {
            int valOut;
            if (node.HasValue(valName) && int.TryParse(node.GetValue(valName), out valOut))
            {
                val = valOut;
                return true;
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string valName, ref float val)
        {
            float valOut;
            if (node.HasValue(valName) && float.TryParse(node.GetValue(valName), out valOut))
            {
                val = valOut;
                return true;
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string valName, ref double val)
        {
            double valOut;
            if (node.HasValue(valName) && double.TryParse(node.GetValue(valName), out valOut))
            {
                val = valOut;
                return true;
            }
            return false;
        }

        public static bool ReadValue(ConfigNode node, string[] valNames, ref bool val)
        {
            foreach (string valName in valNames)
            {
                if (node.HasValue(valName))
                {
                    val = "TRUE" == node.GetValue(valName).ToUpper();
                    return true;
                }
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string[] valNames, ref int val)
        {
            int valOut;
            foreach (string valName in valNames)
            {
                if (node.HasValue(valName) && int.TryParse(node.GetValue(valName), out valOut))
                {
                    val = valOut;
                    return true;
                }
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string[] valNames, ref float val)
        {
            float valOut;
            foreach (string valName in valNames)
            {
                if (node.HasValue(valName) && float.TryParse(node.GetValue(valName), out valOut))
                {
                    val = valOut;
                    return true;
                }
            }
            return false;
        }
        public static bool ReadValue(ConfigNode node, string[] valNames, ref double val)
        {
            double valOut;
            foreach (string valName in valNames)
            {
                if (node.HasValue(valName) && double.TryParse(node.GetValue(valName), out valOut))
                {
                    val = valOut;
                    return true;
                }
            }
            return false;
        }


        /************************************************************************\
         * ParseHelper class                                                    *
         * FormatValue function                                                 *
         *                                                                      *
         * Converts value into a string formated so it has numDecimal decimal   *
         * places.  If value is smaller than can be displayed with numDecimals  *
         * then it is converted to scientific notiation.                        *
         *                                                                      *
         * value:       number to be converted to string.                       *
         * numDecimals: number of decimal places for the string to have.        *
         *                                                                      *
         * Returns: string version of value.                                    *
        \************************************************************************/
        public static string FormatValue(float value, int numDecimals = 2)
        {
            string strVal;
            float cutoffVal;

            numDecimals = Math.Max(0, numDecimals);

            if (0 != numDecimals)
                cutoffVal = 1 / (10 * numDecimals);
            else
                cutoffVal = 0;

            if (Math.Abs(value) < cutoffVal)
            {
                numDecimals = Math.Max(1, numDecimals);
                strVal = value.ToString("E" + numDecimals.ToString());
            }
            else
            {
                strVal = value.ToString("F" + numDecimals.ToString());
            }

            return strVal;
        }
    }
}
