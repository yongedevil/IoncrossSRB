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

        public ModuleEngines module_engine;

        public ThrustCurve thrustCurve;
        public IonGUIThrustCurve thrustCurve_guiController;

        //Flight GUI elements
        [KSPField(guiName = "Current Thrust Percent", isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public float curThrustPercent;

        public double ingitionTime;


        //Editor GUI elements
        [KSPField(guiName = "Thrust Curve", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        [UI_Toggle(disabledText = "", enabledText = "")]
        public bool editThrustCurve;

        [KSPField(guiName = "Current Thrust", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        public float thrust;

        [KSPField(guiName = "Fuel Mass (t)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        public float fuelMass;
        public float fuelMass_last;

        [KSPField(guiName = "Burn Time (s)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 300f, minValue = 0f)]
        public float burnTime;
        public float burnTime_last;
        public float burnTime_min;
        public float burnTime_max;

        //this will replace the module engine's thrust Limiter slider so the order of the modules doesn't affect the ordering of the interface
        [KSPField(guiName = "Thrust Limiter (%)", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float thrustPercent;
        public float thrustPercent_last;
        public float thrustPercent_min;





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

            module_engine = (ModuleEngines)this.part.Modules["ModuleEngines"];
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

            //setup ThrustCurve and guiController
            thrustCurve = new ThrustCurve(module_engine.thrustPercentage / 100f);
            thrustCurve_guiController = new IonGUIThrustCurve(this, thrustCurve);

            //Calculate intial burnTime and burnTime_min
            if (null != module_engine && PartModule.StartState.Editor == (PartModule.StartState.Editor & state))
            {
                //calculate initial burnTime based on default fuel load
                //result is multiplied by 2, rounded, multiplied by 0.5 to round to the nearest 0.5
                burnTime = (float)Math.Round(CalculateBurnTime(module_engine.maxThrust * module_engine.thrustPercentage / 100f) * 2f, MidpointRounding.AwayFromZero) * 0.5f;

                thrust = module_engine.maxThrust * module_engine.thrustPercentage / 100f;
                thrustPercent = module_engine.thrustPercentage;
                thrustPercent_min = module_engine.minThrust / module_engine.maxThrust * 100f;

                fuelMass = CalculateFuelMass();

#if DEBUG_SRB
                Debug.Log("IonModuleSRB.OnStart(): burnTime " + burnTime + " | engine.maxThrust " + engine.maxThrust  + " | engine.thrustPercentage " + engine.thrustPercentage);
#endif
                /************************************\
                 * Calculte minium burn time.       *
                 * uses full fuel load to calc.     *
                \************************************/
                //Set max fuel load
                foreach (Propellant propellant in module_engine.propellants)
                {
                    pResource = (PartResource)this.part.Resources[propellant.name];
                    propellInitAmounts.Add(propellant.id, pResource.amount);

                    if (propellant.ratio > 0)
                        pResource.amount = pResource.maxAmount;
                    else
                        pResource.amount = 0;
                }

                //calculate burnTime_min based on full fuel load
                burnTime_min = (float)Math.Round(CalculateBurnTime(module_engine.maxThrust), 1);

                //Reset fuel load
                foreach (Propellant propellant in module_engine.propellants)
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
                fuelMass_last = fuelMass;
                thrustPercent_last = thrustPercent;

                //Set bounds for UI sliders
                ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).minValue = burnTime_min;
                ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).maxValue = burnTime_max;
                ((UI_FloatRange)(Fields["thrustPercent"].uiControlEditor)).minValue = thrustPercent_min;

                //engine's thrust limiter slider turned off (this module has its own)
                module_engine.Fields["thrustPercentage"].guiActiveEditor = false;
#if DEBUG_SRB
                Debug.Log("IonModuleSRB.OnStart(): burnTime " + burnTime + " | burnTime_min " + burnTime_min);
#endif
            }

            else if(null != module_engine && PartModule.StartState.PreLaunch == (PartModule.StartState.PreLaunch & state))
            {
                thrustCurve.CalculateFuelPortions();
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

            if (ingitionTime < 0 && module_engine.EngineIgnited)
            {
                ingitionTime = Planetarium.GetUniversalTime();
            }

            else
            {
                //fuel mass is only calculate in OnStart and OnGUI (when in the editor)
                //it is therefore equal to the mass of fuel when the rocket is ignited
                curThrustPercent = thrustCurve.Evaluate(CalculateFuelMass() / fuelMass);
            }

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
            if (null != module_engine && HighLogic.LoadedSceneIsEditor)
            {
                fuelMass = CalculateFuelMass();

                //If burnTime or fuelMass change adjust thrustPercent
                if (burnTime != burnTime_last || fuelMass != fuelMass_last)
                {
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): burnTime " + burnTime + " | burnTime_last " + burnTime_last + " | fuelMass " + fuelMass + " | fuelMass_last " + fuelMass_last);
#endif
                    burnTime = (float)Math.Round(burnTime * 2f, MidpointRounding.AwayFromZero) / 2f;
                    AdjustThrustPercent();
                }

                //If thrustPercent changes adjust burnTime
                else if(thrustPercent != thrustPercent_last)
                {
#if DEBUG_UPDATES
                    Debug.Log("IonModuleSRB.OnGUI(): thrustPercent " + thrustPercent + " | thrustPercent_last " + thrustPercent_last);
#endif
                    module_engine.thrustPercentage = (float)Math.Round(module_engine.thrustPercentage * 2, MidpointRounding.AwayFromZero) / 2;
                    AdjustBurnTime();
                }

                //If editThrustCurve button is presses, open thrust curve window
                if(editThrustCurve)
                {
                    thrustCurve_guiController.windowPos = GUILayout.Window(this.GetHashCode(), thrustCurve_guiController.windowPos, thrustCurve_guiController.DrawGUI, "Edit Thrust Curve", thrustCurve_guiController.windowStyle);
                }
            }
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * AdjustThrustPercent function                                         *
         *                                                                      *
         * Adjusts the thrustPercent value for changes in burn time or fuelMass.*
        \************************************************************************/
        public void AdjustThrustPercent()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.AdjustThrustPercent() " + this.part.name);
#endif
            thrustPercent = CalculateThrust(burnTime) / module_engine.maxThrust * 100f;
            thrustPercent = (float)Math.Round(thrustPercent * 2f, MidpointRounding.AwayFromZero) * 0.5f;

            //If thrustPercent is below minium adjust and re-calc burn time
            //(don't use AdjustBurnTime() to avoid posibility of back and forth loop)
            if (thrustPercent < thrustPercent_min)
            {
                thrustPercent = thrustPercent_min;
                burnTime = CalculateBurnTime(thrustPercent * module_engine.maxThrust);
                burnTime = (float)Math.Round(burnTime * 2f, MidpointRounding.AwayFromZero) * 0.5f;
            }

            if (0 != thrustPercent_last)
                thrustCurve.ModifiyThrust(thrustPercent / thrustPercent_last);
            else
                thrustCurve.ResetThrust(thrustPercent);

            thrustCurve_guiController.UpdateCruveTexture();
            module_engine.thrustPercentage = thrustPercent;

            thrust = module_engine.maxThrust * thrustPercent / 100f;


            burnTime_last = burnTime;
            fuelMass_last = fuelMass;
            thrustPercent_last = thrustPercent;
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.AdjustThrustPercent(): Recalculated thrust limiter to " + thrustPercent);
#endif
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * AdjustBurnTime function                                              *
         *                                                                      *
         * Adjusts the burnTime value for changes in thrustPercent.             *
        \************************************************************************/
        public void AdjustBurnTime()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.AdjustBurnTime() " + this.part.name);
#endif
            burnTime = CalculateBurnTime(module_engine.maxThrust * thrustPercent / 100f);
            burnTime = (float)Math.Round(burnTime * 2f, MidpointRounding.AwayFromZero) * 0.5f;

            if (burnTime < burnTime_min)
            {
                burnTime = burnTime_min;
                AdjustThrustPercent();
            }
            else if (burnTime > burnTime_max)
            {
                burnTime = burnTime_max;
                AdjustThrustPercent();
            }
            else
            {
                burnTime_last = burnTime;
                fuelMass_last = fuelMass;
                thrustPercent_last = thrustPercent;
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.AdjustBurnTime(): Recalculated burn time to " + burnTime);
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateThrust function                                             *
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
                thrust = engineMassFlow * module_engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f;
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
            
            foreach (Propellant propellant in module_engine.propellants)
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

            foreach (Propellant propellant in module_engine.propellants)
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

            foreach(Propellant propellant in module_engine.propellants)
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

            if (0 != module_engine.atmosphereCurve.Evaluate(atmoDensity))
                engineMassFlow = thrust / (module_engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f);

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
