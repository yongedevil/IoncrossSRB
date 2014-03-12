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
    public class IonModuleSRB : PartModule
    {
        public const string BURN_TIME_MAX = "burnTime_max";

        public ModuleEngines engine;

        IonGUIThrustCurve guiThrustCurve;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public FloatCurve thrustPercentCurve;

        //Flight GUI elements
        [KSPField(guiName = "Current Thrust Percent", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public float curThrustPercent;

        //Editor GUI elements
        [KSPField(guiName = "Thrust Curve", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        [UI_Toggle(disabledText = "", enabledText = "")]
        public bool editThrustCurve;

        [KSPField(guiName = "Current Thrust", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        public float curThrust;
        public float thrustPercent_last;

        [KSPField(guiName = "Fuel Mass (t)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        public float fuelMass;
        public float fuelMass_last;

        [KSPField(guiName = "Burn Time (s)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 300f, minValue = 0f)]
        public float burnTime;
        public float burnTime_min;
        public float burnTime_max;
        public float burnTime_last;




        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnAwake function override                                            *
         *                                                                      *
        \************************************************************************/
        public override void OnAwake()
        {
            base.OnAwake();
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnAwake() " + this.part.name);
#endif

            engine = (ModuleEngines)this.part.Modules["ModuleEngines"];
        }
        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnLoad function                                                      *
         *                                                                      *
        \************************************************************************/
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnLoad() " + this.part.name);
            Debug.Log("IonModuleSRB.OnLoad(): node\n" + node.ToString());
#endif
            burnTime_max = 300;
            ParseHelper.ReadValue(node, BURN_TIME_MAX, ref burnTime_max);
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnSave function                                                      *
         *                                                                      *
        \************************************************************************/
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnSave() " + this.part.name);
#endif
            node.AddValue(BURN_TIME_MAX, burnTime_max);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnSave(): node\n" + node.ToString());
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnStart function                                                     *
         *                                                                      *
        \************************************************************************/
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnStart() " + this.part.name);
#endif
            PartResource pResource;
            Dictionary<int, double> propellInitAmounts = new Dictionary<int, double>();

            guiThrustCurve = new IonGUIThrustCurve(this);

            //Calculate intial burnTime and burnTime_min
            if (null != engine && PartModule.StartState.Editor == (PartModule.StartState.Editor & state))
            {
                //calculate initial burnTime based on default fuel load
                burnTime = (float)Math.Round(CalculateBurnTime(engine.maxThrust * engine.thrustPercentage / 100f) * 2f, MidpointRounding.AwayFromZero) / 2f;
                curThrust = engine.maxThrust * engine.thrustPercentage / 100f;

                //set thrust curve to constant
                thrustPercentCurve.Add(0, engine.thrustPercentage);
                thrustPercentCurve.Add(1, engine.thrustPercentage);
#if DEBUG_SRB
                Debug.Log("IonModuleSRB.OnStart(): burnTime " + burnTime + " | engine.maxThrust " + engine.maxThrust  + " | engine.thrustPercentage " + engine.thrustPercentage);
#endif
                //Set max fuel load
                foreach (Propellant propellant in engine.propellants)
                {
                    pResource = (PartResource)this.part.Resources[propellant.name];
                    propellInitAmounts.Add(propellant.id, pResource.amount);

                    if (propellant.ratio > 0)
                        pResource.amount = pResource.maxAmount;
                    else
                        pResource.amount = 0;
                }

                //calculate burnTime_min based on full fuel load
                burnTime_min = (float)Math.Round(CalculateBurnTime(engine.maxThrust), 1);

                //Reset fuel load
                foreach (Propellant propellant in engine.propellants)
                {
                    double amount;
                    if (propellInitAmounts.TryGetValue(propellant.id, out amount))
                    {
                        pResource = (PartResource)this.part.Resources[propellant.name];
                        pResource.amount = amount;
                    }
                }

                //set previous values
                burnTime_last = burnTime;
                thrustPercent_last = engine.thrustPercentage;
                fuelMass_last = fuelMass = CalculateFuelMass();

                //Set bounds for UI slider
                ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).minValue = burnTime_min;
                ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).maxValue = burnTime_max;
                ((UI_FloatRange)(engine.Fields["thrustPercentage"].uiControlEditor)).stepIncrement = 0.5f;
#if DEBUG_SRB
                Debug.Log("IonModuleSRB.OnStart(): burnTime " + burnTime + " | burnTime_min " + burnTime_min);
#endif
            }
            //Turn off burnTime slider
            else
            {
                //Fields["fuelMass"].guiActive = false;
                //Fields["curThrust"].guiActive = false;
                //Fields["burnTime"].guiActive = false;
            }
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnUpdate function                                                    *
         *                                                                      *
        \************************************************************************/
        public override void OnUpdate()
        {
            base.OnUpdate();

            curThrustPercent = thrustPercentCurve.Evaluate(CalculateFuelMass() / fuelMass);
            //engine.thrustPercentage = curThrustPercent;

#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnUpdate(): calculaating thrustPercentCurve for " + CalculateFuelMass() / fuelMass);
                    Debug.Log("IonModuleSRB.OnUpdate(): curThrustPercent " + curThrustPercent);
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnGUI function                                                       *
         *                                                                      *
         * OnGUI is used because OnUpate is not called in the editor.           *
        \************************************************************************/
        public void OnGUI()
        {
            if (null != engine && HighLogic.LoadedSceneIsEditor)
            {
                fuelMass = CalculateFuelMass();

                //If burnTime or fuelMass change adjust thrust
                if (burnTime != burnTime_last || fuelMass != fuelMass_last)
                {
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): burnTime " + burnTime + " | burnTime_last " + burnTime_last + " | fuelMass " + fuelMass + " | fuelMass_last " + fuelMass_last);
#endif
                    burnTime = (float)Math.Round(burnTime * 2f, MidpointRounding.AwayFromZero) / 2f;
                    engine.thrustPercentage = (float)Math.Round(CalculateThrust(burnTime) / engine.maxThrust * 200f, MidpointRounding.AwayFromZero) / 2f;
                    
                    burnTime_last = burnTime;
                    fuelMass_last = fuelMass;
                    thrustPercent_last = engine.thrustPercentage;
                    curThrust = engine.maxThrust * engine.thrustPercentage / 100f;
                    guiThrustCurve.UpdateCruveTexture();
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): Recalculated thrust limiter to " + engine.thrustPercentage);
#endif
                }

                //If thrust changes adjust burnTime
                else if(engine.thrustPercentage != thrustPercent_last)
                {
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): engine.thrustPercentage " + engine.thrustPercentage + " | thrustPercent_last " + thrustPercent_last);
#endif
                    engine.thrustPercentage = (float)Math.Round(engine.thrustPercentage * 2, MidpointRounding.AwayFromZero) / 2;
                    burnTime = (float)Math.Round(CalculateBurnTime(engine.maxThrust * engine.thrustPercentage / 100f) * 2f, MidpointRounding.AwayFromZero) / 2f;

                    if(burnTime < burnTime_min)
                    {
                        burnTime = burnTime_min;
                        engine.thrustPercentage = (float)Math.Round(CalculateThrust(burnTime) / engine.maxThrust * 200f, MidpointRounding.AwayFromZero) / 2f;
                    }
                    else if(burnTime > burnTime_max)
                    {
                        burnTime = burnTime_max;
                        engine.thrustPercentage = (float)Math.Round(CalculateThrust(burnTime) / engine.maxThrust * 200f, MidpointRounding.AwayFromZero) / 2f;
                    }

                    burnTime_last = burnTime;
                    thrustPercent_last = engine.thrustPercentage;
                    curThrust = engine.maxThrust * engine.thrustPercentage / 100f;
                    guiThrustCurve.UpdateCruveTexture();
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): Recalculated burn time to " + burnTime);
#endif
                }

                if(editThrustCurve)
                {
                    //create window
                    guiThrustCurve.windowPos = GUILayout.Window(this.GetHashCode(), guiThrustCurve.windowPos, guiThrustCurve.DrawGUI, "Edit Thrust Curve", guiThrustCurve.windowStyle);
                }
            }
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateThrustActual function                                       *
         *                                                                      *
         * Calculates and returns the actual thrust need to generate the given  *
         * burn time with the current amount of fuel onboard at sea level.      *
        \************************************************************************/
        private float CalculateThrust(float burnTime, float atmoDensity = 1.0f)
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateThrust(" + burnTime + ") " + this.part.name);
#endif
            float thrust = 0;
            float engineMassFlow = 0;
            float mixtureProtion;

            Propellant limitingPropellant = FindLimitingPropellant();
            PartResource limitingResource = (PartResource)this.part.Resources[limitingPropellant.name];

            mixtureProtion = limitingPropellant.ratio * limitingResource.info.density / CalculateMixtureMass();

            if (0 != burnTime && 0 != mixtureProtion)
            {
                engineMassFlow = (1 / mixtureProtion) * (float)limitingResource.amount * limitingResource.info.density / burnTime;
                thrust = engineMassFlow * engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f;
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateThrust(): limitingPropellant = " + limitingPropellant.name + " | mixtureProtion = " + mixtureProtion + " | engineMassFlow = " + engineMassFlow + " | thrust = " + thrust);
            Debug.Log("IonModuleSRB.CalculateThrust(): limitingPropellant.ratio = " + limitingPropellant.ratio + " | limitingResource.info.density = " + limitingResource.info.density);
#endif
            return thrust;
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * FindLimitingPropellant function                                      *
         *                                                                      *
         * Finds and resturns the propellant which will burn up first given     *
         * current quantities onboard.                                          *
         *                                                                      *
         * Returns: Propellant that will burn up first.                         *
        \************************************************************************/
        private Propellant FindLimitingPropellant()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.FindLimitingPropellant() " + this.part.name);
#endif
            Propellant limitingPropellant = null;
            PartResource pResource;

            float limitValue = -1;
            float propellantLimitValue;
            float mixtureMass = CalculateMixtureMass();
            
            foreach (Propellant propellant in engine.propellants)
            {
                pResource = (PartResource)this.part.Resources[propellant.name];

                //calculate propellant mixture as follows:
                //mixtureProtion = (propellant.ratio * pResource.info.density) / mixtureMass;
                //propellantLimitValue = (1 / mixtureProtion) * (float)pResource.amount * pResource.info.density;

                //the lower this value is the more the engine is limited by this resource
                propellantLimitValue = (mixtureMass / propellant.ratio) * (float)pResource.amount;

                if (limitValue < 0 || propellantLimitValue < limitValue)
                {
                    limitValue = propellantLimitValue;
                    limitingPropellant = propellant;
                }
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.FindLimitingPropellant(): limitingPropellant = " + limitingPropellant.name);
#endif
            return limitingPropellant;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateMixtureMass function                                        *
         *                                                                      *
         * Calculates and returns the cumulative mass of all propellants at     *
         * their ratio of use in the engine.                                    *
         *                                                                      *
         * Returns: total mass of all propellant at their ratio.                *
        \************************************************************************/
        private float CalculateMixtureMass()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateMixtureMass() " + this.part.name);
#endif
            float mixtureMass = 0;

            foreach (Propellant propellant in engine.propellants)
                mixtureMass += propellant.ratio * ((PartResource)this.part.Resources[propellant.name]).info.density;

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateMixtureMass(): mixtureMass = " + mixtureMass);
#endif
            return mixtureMass;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateFuelMass function                                           *
         *                                                                      *
         * Calculates and returns the total mass of all propellants.            *
         *                                                                      *
         * Returns: total mass of all propellants.                              *
        \************************************************************************/
        private float CalculateFuelMass()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateFuelMass() " + this.part.name);
#endif
            float fuelMass = 0.0f;
            PartResource pResource;

            foreach(Propellant propellant in engine.propellants)
            {
                pResource = this.part.Resources[propellant.name];
                fuelMass += (float)pResource.amount * pResource.info.density;
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateFuelMass(): fuelMass = " + fuelMass);
#endif
            return fuelMass;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateBurnTime function                                           *
         *                                                                      *
         * Calculates and returns the burn time for the current fuel onboard    *
         * at the given thrust and atmosphere density.                          *
         *                                                                      *
         * Returns: Predicted burn time of the SRB.                             *
        \************************************************************************/
        private float CalculateBurnTime(float thrust, float atmoDensity = 1.0f)
        {

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTime(" + thrust + ", " + atmoDensity + ") " + this.part.name);
#endif
            float timeBurn = 0;
            float engineMassFlow = 0;

            if (0 != engine.atmosphereCurve.Evaluate(atmoDensity))
                engineMassFlow = thrust / (engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f);

            Propellant limitingPropellant = FindLimitingPropellant();
            PartResource limitingResource = (PartResource)this.part.Resources[limitingPropellant.name];

            if (0 != engineMassFlow)
            {
                //calculate timeBurn as follows:
                //mixtureProtion = (limitingPropellant.ratio * limitingResource.info.density) / CalculateMixtureMass();
                //timeBurn = (float)limitingResource.amount * limitingResource.info.density / (engineMassFlow * mixtureProtion);
                timeBurn = (float)limitingResource.amount * CalculateMixtureMass() / (engineMassFlow * limitingPropellant.ratio);
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTime(): limitingPropellant = " + limitingPropellant.name + " | engineMassFlow = " + engineMassFlow + " | timeBurn = " + timeBurn);
            Debug.Log("IonModuleSRB.CalculateBurnTime(): limitingPropellant.ratio = " + limitingPropellant.ratio + " | limitingResource.info.density = " + limitingResource.info.density);
#endif
            return timeBurn;
        }
    }
}
